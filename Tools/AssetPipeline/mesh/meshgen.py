#!/usr/bin/env python3
"""Mesh stage of the CLONE-Z asset pipeline: gives every item textured meshes, generated on the DGX Spark.

Every item gets --variants different versions (3 by default), each with its own reference photo and mesh; the game
picks one per item on the ground. For each item the local vision model (gemma3:12b in the Spark's Ollama) describes
that many different real-world versions of it. For each version, Z-Image-Turbo paints a large reference photo, the
model rejects photos that don't show the item, BiRefNet cuts the item out at full resolution, TRELLIS.2 turns the
cutout into a textured mesh, and meshpost.py fits that mesh onto the item's existing primitive model and bakes the
full mesh's detail into a normal map. The game copy (two levels of detail and their maps) goes to
Assets/Resources/GeneratedItemMeshes, which GeneratedItemMeshes.cs draws in place of the primitives; the originals
(every photo at full size, the cutout, the full-detail mesh as a GLB, and how each was made) go to the NAS.

Runs in the clonez-mesh container (Dockerfile, spark.sh). Everything runs on the Spark; only `prepare` goes online,
once, for anonymous downloads of the pinned open weights. `status` needs nothing but the standard library, so the
Spark host runs it without the container.

  python meshgen.py prepare
  python meshgen.py generate [--pilot] [--ids A,B] [--category Firearm] [--limit N] [--force] [--no-nas]
  python meshgen.py refit [--ids A,B]
  python meshgen.py status
"""
import argparse
import base64
import faulthandler
import html
import io
import json
import os
import shutil
import signal
import socket
import statistics
import subprocess
import sys
import threading
import time
import traceback
import urllib.parse
import zlib
from pathlib import Path

HERE = Path(__file__).resolve().parent
PIPELINE = HERE.parent
sys.path.insert(0, str(PIPELINE))
sys.path.insert(0, str(HERE))
import catalog  # noqa: E402  (standard library only)

ROOT_DEFAULT = PIPELINE.parent.parent
MODELS = Path(os.environ.get("CLONEZ_MODELS", "/models"))
OUT_FOLDER = "GeneratedItemMeshes"
TRELLIS_CONFIG = "pipeline.clonez.json"

# Revisions are pinned so a rerun months later downloads exactly the same weights. None of them needs an account.
PINS = {
    "trellis": ("microsoft/TRELLIS.2-4B", "af44b45f2e35a493886929c6d786e563ec68364d"),           # MIT
    "trellis_v1": ("microsoft/TRELLIS-image-large", "25e0d31ffbebe4b5a97464dd851910efc3002d96"),  # MIT
    "birefnet": ("ZhengPeng7/BiRefNet", "e2bf8e4460fc8fa32bba5ea4d94b3233d367b0e4"),              # MIT
    "zimage": ("Tongyi-MAI/Z-Image-Turbo", "f332072aa78be7aecdf3ee76d5c247082da564a6"),          # Apache-2.0
    # Meta's DINOv3 licence; timm's copy of the weights is not gated, unlike facebook/dinov3-vitl16-pretrain-lvd1689m.
    "dinov3": ("timm/vit_large_patch16_dinov3.lvd1689m", "30c1109559f65dea34316b0d4842d35c5771fe11"),
}

# A spread of categories and shapes, all with a primitive model to fit onto; --pilot runs these.
PILOT = [
    "BaseballBat", "Axe", "BallPeenHammer", "AssaultRifle", "Apple", "Banana", "Baguette", "Avocado", "BeerCan",
    "BeerBottle", "Bandage", "AntibioticsBox", "308Box", "BarbedWire", "AdhesiveTapeBox", "Apron_BBQ", "BathTowel",
    "Battery", "Banjo", "Basketball", "BakingPan", "BarBell", "Bellows", "Badge",
]

STYLES = {
    "realistic": "Realistic, used and worn, muted colours, light grime, scuffs and wear, real materials.",
    "stylized": "Stylised hand-painted game asset, clean simplified shapes, soft painted colours.",
    "lowpoly": "Low-poly game asset with flat solid colours and simple faceted shapes.",
}
SHOT = ("Product photograph of the item alone, centred on a plain light grey background, the whole item in frame "
        "with space around it, three-quarter view from slightly above, soft even studio lighting, sharp focus. "
        "No text overlays, no people, no hands, nothing else in the picture.")
# Garments the primitive models lay flat on the floor; a picture of one worn or on a hanger would fit badly.
FLAT_TRAITS = ("Clothing worn on the torso", "Clothing worn on the legs")
FLAT_SHOT = ("Product photograph of the item laid out flat on a plain light grey floor, seen from above at a slight "
             "angle, the whole item in frame, soft even studio lighting, sharp focus. No text overlays, no people, "
             "no hands, nothing else in the picture.")
# Flat things with a broad face (by the item's model: the thinnest side under THIN of the middle one, the middle over
# BROAD of the longest: a badge, a plate, a packet) photographed face on give TRELLIS.2 nothing to tell their thickness
# by; it made the test badge a cube. A low angle that shows the edge settles it. Long flat things (a rifle on its side,
# a saw blade) keep the usual shot, which shows their profile; a low angle would foreshorten it.
THIN = 0.35
BROAD = 0.5
THIN_SHOT = ("Product photograph of the item alone, lying flat on a plain light grey surface, photographed from a low "
             "angle of about 30 degrees so its top face, its thickness and its side edges all show, the whole item in "
             "frame with space around it, soft even studio lighting, sharp focus. No text overlays, nothing else in "
             "the picture.")


def is_thin(parts):
    import geometry

    lo, hi = geometry.bounds(parts)
    e = sorted(float(x) for x in hi - lo)
    return e[1] > 0 and e[0] / e[1] < THIN and e[1] / e[2] > BROAD

VARIANTS = 3
NAS_ITEMS = "assets/item-meshes"  # under the NAS share: <id>/v<variant>/ (see Masters)
MAX_VARIANTS = 8                  # GeneratedItemMeshes.MaxVariants: the game looks no further

# Worn items, by the slot trait catalog.py gives them: whether the thing is rigid (one mesh serves the floor and the
# body: its photo shows it as worn, and a later fitting step places it on the bone) or soft (a garment: a second photo
# and mesh show it worn, for the fitting step that shapes it to the body); where the camera stands relative to the
# wearer in the worn photo, which tells that step which side of the mesh faces where; and what the photo shows.
# Garments are shot "ghost mannequin" style (hollow, holding the shape they have when worn), in words that never mention
# a person: Z-Image-Turbo has no negative prompt, and "worn by an invisible person ... no people" painted people (a man
# in the T-shirt, a woman in the dress, legs in the socks, hands in the gloves: 11 of 18 photos of 9 garments in a
# prompt test), where this wording painted none; adding "no person, no mannequin" let fingers back in once. The game's
# bind pose has the arms hanging straight down, hence the sleeves and gloves pointing down. Head items never showed a
# head with the "invisible head" wording. The entries no catalogued item uses yet (eyewear to thigh bag: code-defined
# items) follow the same pattern untested.
GHOST = ("ghost mannequin product photograph of the garment alone: hollow and empty, holding the rounded "
         "three-dimensional shape it has when worn")
ON_HEAD = ("studio product photograph of the item worn on an invisible head, seen from the front, nothing of the "
           "wearer visible")
WORN = {
    "Clothing worn on the head": ("rigid", "front", ON_HEAD),
    "Head-mounted eyewear or optics": ("rigid", "front", ON_HEAD),
    "Mask worn over the face": ("rigid", "front", "product photograph of the mask alone, hollow and empty, in the "
                                                  "position and shape it has when worn, seen straight from the front"),
    "Backpack worn on the back": ("rigid", "back", "product photograph of the backpack alone, packed and standing "
                                                   "upright as when worn, the shoulder straps curving forward away from "
                                                   "the camera, seen straight from behind"),
    "Chest rig (load-bearing vest with pouches)": ("rigid", "front", f"{GHOST}, seen straight from the front"),
    "Body armour (vest or plate carrier)": ("rigid", "front", f"{GHOST}, seen straight from the front"),
    "Thigh bag strapped to the leg": ("rigid", "right", "product photograph of the thigh bag alone, its leg straps "
                                                        "closed in loops around empty space as when worn, seen from "
                                                        "its outer side"),
    "Clothing worn on the torso": ("soft", "front", f"{GHOST}, the sleeves hanging straight down at its sides, the "
                                                    "inside of the back of the collar showing through the empty neck "
                                                    "opening, seen straight from the front"),
    "Clothing worn on the legs": ("soft", "front", f"{GHOST}, standing upright with the legs straight and slightly "
                                                   "apart, the inside of the back of the waistband showing through the "
                                                   "empty waist opening, seen straight from the front"),
    "Clothing worn on the feet": ("soft", "front", "product photograph of the pair alone, standing upright side by "
                                                   "side and slightly apart, filled out in the shape they have when "
                                                   "worn, fastened, the empty openings at the top, seen from the "
                                                   "front"),
    "Clothing worn on the hands": ("soft", "front", "ghost mannequin product photograph of the pair alone: hollow and "
                                                    "empty, filled out in the shape they have when worn, side by side "
                                                    "with the fingers pointing straight down and the empty cuff "
                                                    "openings at the top, the backs facing the camera"),
}
# What a photo scores at most when a plain yes-or-no question finds it wrong: part of a person shows in a worn photo, or
# the picture holds several copies of the item. The critic's 1-10 score lets both through (7 for a man in the T-shirt,
# 9 for a stack of sugar packets); asked on their own, it answers them reliably.
GATE_SCORE = 2
PERSON_SCHEMA = {"type": "object", "properties": {"person": {"type": "boolean"}, "what": {"type": "string"}},
                 "required": ["person", "what"]}
PERSON_PROMPT = ("Look closely at this picture. Does it show any part of a real person or a mannequin: a head, face, "
                 "neck, bare skin, arm, hand, real fingers, leg or foot, or a mannequin, dress form or model? Clothing "
                 "shaped as if worn but empty does not count, nor do the fingers of a glove. Answer in JSON: 'person' "
                 "true if any such part is visible, and 'what' naming it, or 'nothing'.")
COPIES_SCHEMA = {"type": "object", "properties": {"copies": {"type": "boolean"}, "what": {"type": "string"}},
                 "required": ["copies", "what"]}
# Asked "how many objects", the critic counts a pair as two, the glasses' lenses and arms apart and the background;
# this question it answered right for all 40 photos it was tried on, the three stacks of sugar packets among them. It
# also says yes to a tin of nails, though, so a yes is followed by INSIDE_PROMPT: things in a container are the item.
COPIES_PROMPT = ("Does this picture show several copies of the same object, such as a stack, a pile or a row of them? "
                 "A pair of shoes, socks or gloves does not count, nor do the parts of a single object, nor its "
                 "background. Answer in JSON: 'copies' true or false, and 'what' saying what is repeated, or "
                 "'nothing'.")
INSIDE_SCHEMA = {"type": "object", "properties": {"inside": {"type": "boolean"}}, "required": ["inside"]}
INSIDE_PROMPT = ("In this picture, are the repeated things ({what}) held inside a box, tin, bag, jar or other "
                 "container, rather than lying loose? Answer in JSON: 'inside' true or false.")


def worn_kind(item):
    """(kind, view, look) for an item worn on the body, or None."""
    for t in item.traits:
        if t in WORN:
            return WORN[t]
    return None


def worn_shot(look):
    """The photo's framing for a worn item; look (from WORN) says what it shows."""
    return (f"{look[0].upper()}{look[1:]}, the whole item in frame with space around it, on a plain light grey "
            "background, soft even studio lighting, sharp focus. No text overlays, nothing else in the picture.")


def budget(parts, args):
    """Map sizes and triangle targets for the game copy, by the item's real size (its model's longest side): about
    1,000 texels per metre, so a 7 cm badge doesn't carry a rifle's maps. Each is capped by its command-line option.
    The full-detail master on the NAS keeps everything, so a bigger copy can be baked from it later."""
    import geometry

    lo, hi = geometry.bounds(parts)
    size = float((hi - lo).max())
    tier = 2 if size > 0.5 else 1 if size > 0.25 else 0
    near = (256, 512, 1024)[tier]
    return {
        "size_m": round(size, 3),
        "near_tex": min(near, args.near_tex), "normal_tex": min(near, args.normal_tex),
    # Metal and roughness change slowly across a surface; a quarter of the colour map's size is plenty.
    "mask_tex": min(max(64, near // 4), args.mask_tex), "far_tex": min(max(64, near // 4), args.far_tex),
    "near_verts": min((4000, 8000, 16000)[tier], args.near_verts),
    "far_verts": min((400, 800, 1500)[tier], args.far_verts),
    }


def describe_schema(n):
    return {"type": "object",
            "properties": {"versions": {"type": "array", "minItems": n, "maxItems": n, "items": {"type": "string"}}},
            "required": ["versions"]}


def stem_of(item_id, variant):
    """The game files' name for one variant: <id>@<variant>.bytes and its maps beside it."""
    return f"{item_id}@{variant}"


def variant_seed(item_id, variant):
    return (zlib.crc32(item_id.encode("utf-8")) + 1_000_003 * variant) & 0x3FFFFFFF

_stop = False


def _on_signal(signum, frame):
    global _stop
    if _stop:
        raise KeyboardInterrupt
    _stop = True
    print("\nStopping after the item in progress (send again to stop at once).", flush=True)


# ── Paths, items and reference models ───────────────────────────────────────────────────────────

class Layout:
    def __init__(self, root):
        self.root = Path(root)
        self.resources = self.root / "Assets" / "Resources"
        self.meshes = self.resources / OUT_FOLDER
        self.generated = self.resources / "GeneratedItemModels"
        self.item_models_cs = self.root / "Assets" / "Scripts" / "World" / "ItemModels.cs"
        self.handbuilt = PIPELINE / "out" / "handbuilt"
        self.out = PIPELINE / "out" / "mesh"
        self.previews = self.out / "previews"
        self.refs = self.out / "refs"
        self.log = self.out / "log.jsonl"
        self.descriptions = self.out / "descriptions.json"


def reference_path(lay, item_id, handmade):
    """The primitive model a mesh is fitted to: the hand-built one (exported from Unity) for hand-built items, the
    LLM-generated one otherwise."""
    if item_id in handmade:
        p = lay.handbuilt / f"{item_id}.json"
    else:
        p = lay.generated / f"{item_id}.json"
    return p if p.exists() else None


def load_reference(path):
    data = json.loads(path.read_text(encoding="utf-8"))
    parts = [p for p in data.get("parts") or [] if p.get("shape") in ("cube", "sphere", "cylinder")]
    return parts or None


def read_log(lay):
    """Latest log entry per variant (key <id>@<variant>)."""
    latest = {}
    if lay.log.exists():
        for line in lay.log.read_text(encoding="utf-8").splitlines():
            try:
                e = json.loads(line)
            except ValueError:
                continue
            if isinstance(e, dict) and e.get("id"):
                latest[e.get("key") or e["id"]] = e
    return latest


RECENT = 12


def _brief(e):
    """A log entry cut down to what the live page shows."""
    keep = ("id", "key", "variant", "name", "category", "status", "picture_score", "mesh_score", "mesh_problems", "fit",
            "secs", "tris", "error", "ts", "refit")
    return {k: e[k] for k in keep if k in e}


def recent_entries(lay, n=RECENT):
    """The latest n log entries, newest first, one per variant."""
    out, seen = [], set()
    if lay.log.exists():
        for line in reversed(lay.log.read_text(encoding="utf-8").splitlines()):
            try:
                e = json.loads(line)
            except ValueError:
                continue
            key = e.get("key") or e.get("id") if isinstance(e, dict) else None
            if not key or key in seen:
                continue
            seen.add(key)
            out.append(_brief(e))
            if len(out) >= n:
                break
    return out


def item_owners(items, handmade):
    """lower-case id -> the item that owns it. Ids that differ only by case (curated "baseballbat", PZ "BaseballBat")
    share one set of mesh files: Windows can't hold both, and Unity's Resources.Load doesn't tell them apart. The
    hand-built item's model is the one fitted to."""
    owner = {}
    for it in sorted(items, key=lambda it: it.id not in handmade):
        owner.setdefault(it.id.lower(), it)
    return owner


def done_variants(lay):
    """Lower-case stems of the variants that have a game copy."""
    return {p.stem.lower() for p in lay.meshes.glob("*@*.bytes")} if lay.meshes.exists() else set()


# A version that failed, or took the whole run down (the kernel's out-of-memory killer, say), this many times in a row
# is left out of later runs until --retry-failed, so one bad item can't stop the run every time it comes up.
GIVE_UP = 2
# generate's exit code when there is nothing left to make (every version asked for has its mesh or was given up on):
# spark.sh's keep-running stops starting the run again then.
NOTHING_TO_DO = 3
# generate's exit code when a step has run past --stage-timeout: stuck in native code that no signal reaches (xatlas's
# chart search once spun for good on one mesh). spark.sh starts the run again, as after a crash, and the try counts.
STUCK = 4
# Steps that only wait on Ollama, which gives up by itself (llm.Ollama's timeout and retries); the watchdog leaves them.
OLLAMA_STEPS = ("describe", "judge_picture", "judge_mesh")
# Seconds after the watchdog that its backstop, for a hang that holds the GIL and so stops the watchdog too, ends it.
STUCK_GRACE = 300


def read_attempts(lay):
    """stem -> tries in a row that didn't end with a mesh. Counted before each try starts, so a crash counts."""
    try:
        data = json.loads((lay.out / "attempts.json").read_text(encoding="utf-8"))
        return data if isinstance(data, dict) else {}
    except (OSError, ValueError):
        return {}


def write_attempts(lay, attempts):
    lay.out.mkdir(parents=True, exist_ok=True)
    tmp = lay.out / f"attempts.json.{os.getpid()}.tmp"
    tmp.write_text(json.dumps(attempts, indent=0, sort_keys=True), encoding="utf-8")
    os.replace(tmp, lay.out / "attempts.json")


def choose(lay, args):
    """(item, reference model, variants still to make) for every item asked for, with the items left out and the
    versions given up on (GIVE_UP)."""
    items = catalog.load_items(lay.resources)
    handmade = catalog.handmade_ids(lay.item_models_cs)
    by_id = {it.id: it for it in items}
    owner = item_owners(items, handmade)
    if args.ids:
        wanted = [i.strip() for i in args.ids.split(",") if i.strip()]
        unknown = [i for i in wanted if i not in by_id]
        if unknown:
            print("Not in the catalogue: " + ", ".join(unknown))
        items = [owner[i.lower()] for i in wanted if i in by_id]
    elif args.pilot:
        items = [owner[i.lower()] for i in PILOT if i in by_id]
    if args.category:
        items = [it for it in items if it.category.lower() == args.category.lower()]

    done = done_variants(lay)
    attempts = {} if args.force or args.retry_failed else read_attempts(lay)
    todo, no_ref, gave_up, seen = [], [], [], set()
    for it in items:
        low = it.id.lower()
        if owner[low] is not it or low in seen:
            continue
        seen.add(low)
        missing = [k for k in range(1, args.variants + 1) if args.force or stem_of(it.id, k).lower() not in done]
        tried_out = [k for k in missing if attempts.get(stem_of(it.id, k), 0) >= GIVE_UP]
        gave_up += [stem_of(it.id, k) for k in tried_out]
        missing = [k for k in missing if k not in tried_out]
        if not missing:
            continue
        ref = reference_path(lay, it.id, handmade)
        if ref is None:
            no_ref.append(it.id)
            continue
        todo.append((it, ref, missing))
    # Plain items first, then rigid worn ones, then garments (two meshes per version), each kind in catalogue order:
    # the ones that cost most come last.
    rank = {None: 0, "rigid": 1, "soft": 2}
    todo.sort(key=lambda t: rank[(worn_kind(t[0]) or (None,))[0]])
    if args.limit:
        todo = todo[: args.limit]
    return todo, no_ref, handmade, gave_up


# ── prepare ─────────────────────────────────────────────────────────────────────────────────────

def _download(key, local, **kw):
    from huggingface_hub import snapshot_download

    repo, rev = PINS[key]
    print(f"Downloading {repo}@{rev[:10]} -> {local}", flush=True)
    return Path(snapshot_download(repo, revision=rev, local_dir=str(local), **kw))


def convert_dinov3(src, dst):
    """Rewrites timm's DINOv3 ViT-L/16 as the transformers DINOv3ViTModel TRELLIS.2 loads, and proves the two give
    the same features. Returns the parity numbers."""
    import re

    import timm
    import torch
    import torch.nn.functional as F
    from transformers import DINOv3ViTConfig, DINOv3ViTModel

    weights = next(Path(src).glob("*.safetensors"))
    tm = timm.create_model("vit_large_patch16_dinov3.lvd1689m", pretrained=True, dynamic_img_size=True,
                           pretrained_cfg_overlay={"file": str(weights)}).eval()
    sd = tm.state_dict()
    hidden = sd["cls_token"].shape[-1]
    reg_key = next((k for k in sd if "reg_token" in k or "register" in k), None)
    layers = len({int(k.split(".")[1]) for k in sd if k.startswith("blocks.")})
    fc1 = sd.get("blocks.0.mlp.fc1.weight")
    if fc1 is None:
        raise RuntimeError("Unexpected timm MLP layout (gated?): " + ", ".join(k for k in sd if k.startswith("blocks.0.mlp")))
    rope = getattr(tm, "rope", None)
    theta = next((float(getattr(rope, a)) for a in ("temperature", "theta", "base") if rope is not None and hasattr(rope, a)), 100.0)
    # The distilled ViT-B/L/H came out of training with all-zero q/k/v biases, which timm leaves out
    # (vit_large_patch16_dinov3 has qkv_bias=False); the transformers model then simply has none either.
    qkv_bias = any(k.endswith(("attn.q_bias", "attn.qkv.bias", "attn.q_proj.bias")) for k in sd)
    cfg = DINOv3ViTConfig(
        patch_size=16, hidden_size=hidden, intermediate_size=fc1.shape[0], num_hidden_layers=layers,
        num_attention_heads=tm.blocks[0].attn.num_heads, layer_norm_eps=getattr(tm.norm, "eps", 1e-5), rope_theta=theta,
        num_register_tokens=sd[reg_key].shape[1] if reg_key else 0, query_bias=qkv_bias, key_bias=False,
        value_bias=qkv_bias, proj_bias=True, mlp_bias=True, use_gated_mlp=False, layerscale_value=1.0,
    )
    hf = DINOv3ViTModel(cfg).eval()

    rules = [
        (r"cls_token", "embeddings.cls_token"),
        (r"reg_token", "embeddings.register_tokens"),
        (r"mask_token", "embeddings.mask_token"),
        (r"patch_embed\.proj\.(weight|bias)", r"embeddings.patch_embeddings.\1"),
        (r"blocks\.(\d+)\.norm([12])\.(weight|bias)", r"layer.\1.norm\2.\3"),
        (r"blocks\.(\d+)\.attn\.proj\.(weight|bias)", r"layer.\1.attention.o_proj.\2"),
        (r"blocks\.(\d+)\.attn\.([qkv])_proj\.(weight|bias)", r"layer.\1.attention.\2_proj.\3"),
        (r"blocks\.(\d+)\.attn\.([qv])_bias", r"layer.\1.attention.\2_proj.bias"),
        (r"blocks\.(\d+)\.(?:gamma_1|ls1\.gamma)", r"layer.\1.layer_scale1.lambda1"),
        (r"blocks\.(\d+)\.(?:gamma_2|ls2\.gamma)", r"layer.\1.layer_scale2.lambda1"),
        (r"blocks\.(\d+)\.mlp\.fc1\.(weight|bias)", r"layer.\1.mlp.up_proj.\2"),
        (r"blocks\.(\d+)\.mlp\.fc2\.(weight|bias)", r"layer.\1.mlp.down_proj.\2"),
        (r"norm\.(weight|bias)", r"norm.\1"),
    ]
    out, skipped = {}, []
    for k, t in sd.items():
        m = re.fullmatch(r"blocks\.(\d+)\.attn\.qkv\.(weight|bias)", k)
        if m:
            q, kk, v = t.chunk(3, dim=0)
            pre = f"layer.{m.group(1)}.attention."
            out[pre + "q_proj." + m.group(2)] = q
            out[pre + "v_proj." + m.group(2)] = v
            if m.group(2) == "weight":
                out[pre + "k_proj.weight"] = kk
            elif kk.abs().max() > 1e-6:
                raise RuntimeError(f"{k} has a non-zero key bias, which DINOv3ViTModel (key_bias=False) can't hold")
            continue
        for pat, rep in rules:
            if re.fullmatch(pat, k):
                out[re.sub(pat, rep, k)] = t
                break
        else:
            skipped.append(k)
    missing, unexpected = hf.load_state_dict(out, strict=False)
    missing = [k for k in missing if k != "embeddings.mask_token"]
    if missing or unexpected or [k for k in skipped if "rope" not in k and not k.endswith("k_bias")]:
        raise RuntimeError(f"DINOv3 conversion incomplete.\n missing: {missing}\n unexpected: {unexpected}\n"
                           f" unmapped timm keys: {skipped}")

    # The final norm's input is what TRELLIS.2 normalises itself (without the affine part), so it is caught for the
    # second comparison below.
    pre_norm = []
    hook = tm.norm.register_forward_hook(lambda module, inputs, output: pre_norm.append(inputs[0].detach()))
    torch.manual_seed(0)
    x = torch.randn(2, 3, 512, 512)
    with torch.no_grad():
        a = tm.forward_features(x)
        b = hf(pixel_values=x).last_hidden_state
    hook.remove()
    if a.shape != b.shape:
        raise RuntimeError(f"DINOv3 parity: timm gives {tuple(a.shape)}, transformers {tuple(b.shape)}")
    final = float((a - b).abs().max() / a.abs().max().clamp(min=1.0))
    Path(dst).mkdir(parents=True, exist_ok=True)
    hf.save_pretrained(str(dst))

    # TRELLIS.2's own feature path, on the saved copy.
    from trellis2.modules.image_feature_extractor import DinoV3FeatureExtractor

    ext = DinoV3FeatureExtractor(str(dst))
    ext.model.float()
    with torch.no_grad():
        c = ext.extract_features(x)
        expect = F.layer_norm(pre_norm[0], pre_norm[0].shape[-1:])
    trellis = float((c - expect).abs().max() / expect.abs().max().clamp(min=1.0))
    print(f"DINOv3 parity (max difference relative to the largest value): final tokens {final:.2e}, "
          f"TRELLIS.2 features {trellis:.2e}", flush=True)
    if final > 1e-3 or trellis > 1e-3:
        raise RuntimeError("The converted DINOv3 does not match timm's; TRELLIS.2 would get the wrong features.")
    return {"final_rel_diff": final, "trellis_features_rel_diff": trellis}


def cmd_prepare(args):
    MODELS.mkdir(parents=True, exist_ok=True)
    trellis = _download("trellis", MODELS / "TRELLIS.2-4B")
    v1 = _download("trellis_v1", MODELS / "TRELLIS-image-large", allow_patterns=["ckpts/ss_dec_conv3d_16l8_fp16.*"])
    for f in (v1 / "ckpts").glob("ss_dec_conv3d_16l8_fp16.*"):
        shutil.copy2(f, trellis / "ckpts" / f.name)
    _download("birefnet", MODELS / "BiRefNet")
    _download("zimage", MODELS / "Z-Image-Turbo")
    dino_src = _download("dinov3", MODELS / "dinov3-timm")
    parity = convert_dinov3(dino_src, MODELS / "dinov3-vitl16-hf")

    # A config of our own beside the downloaded one: everything local, and the two gated models swapped for the
    # converted DINOv3 and BiRefNet (MIT; TRELLIS.2's default background remover, RMBG-2.0, is non-commercial).
    cfg = json.loads((trellis / "pipeline.json").read_text(encoding="utf-8"))
    a = cfg["args"]
    a["models"]["sparse_structure_decoder"] = "ckpts/ss_dec_conv3d_16l8_fp16"
    a["image_cond_model"]["args"]["model_name"] = str(MODELS / "dinov3-vitl16-hf")
    a["rembg_model"]["args"]["model_name"] = str(MODELS / "BiRefNet")
    (trellis / TRELLIS_CONFIG).write_text(json.dumps(cfg, indent=2), encoding="utf-8")

    (MODELS / "prepared.json").write_text(json.dumps({
        "pins": PINS, "dinov3_parity": parity, "when": time.strftime("%Y-%m-%d %H:%M:%S"),
    }, indent=2), encoding="utf-8")
    print("Prepared: the weights are in place and nothing else needs the internet.", flush=True)


# ── generate ────────────────────────────────────────────────────────────────────────────────────

def _b64(img, size=512, fmt="JPEG"):
    from PIL import Image

    im = img.convert("RGB")
    im.thumbnail((size, size), Image.LANCZOS)
    buf = io.BytesIO()
    im.save(buf, format=fmt, quality=90)
    return base64.b64encode(buf.getvalue()).decode()


class Live:
    """What a run is doing right now, for the live page (live.html, served from the Spark by spark.sh web):
    out/mesh/status.json, rewritten at every step and every few seconds besides (GPU load and memory), and
    out/mesh/current.jpg, the reference picture being judged. Both are replaced whole, so the page never reads half a
    file. It is also the run's watchdog: a step that runs past --stage-timeout ends the run (see _stuck)."""

    def __init__(self, lay, total, args):
        self.path = lay.out / "status.json"
        self.picture_path = lay.out / "current.jpg"
        self.log = lay.log
        self.lock = threading.Lock()
        self.memory = None  # once the models are loaded: a function giving (free, total) bytes of GPU memory
        self.closed = False
        self.limit = max(0.0, args.stage_timeout) * 60  # seconds; 0: no watchdog
        self.armed = None  # the stage_started the backstop was set for (_arm)
        self.item_started = None
        self.smi = shutil.which("nvidia-smi")
        now = time.time()
        self.data = {
            "state": "loading", "started": now, "updated": now, "total": total, "index": 0, "ok": 0, "failed": 0,
            "item": None, "step": None, "stage": "Loading the models onto the GPU", "stage_started": now,
            "picture": None, "recent": recent_entries(lay), "median_secs": None, "eta_hours": None,
            "gpu_util": None, "memory_gb": None, "res": args.res, "style": args.style, "critic": args.critic_model,
            "image_size": args.image_size, "variants": args.variants, "nas": bool(args.nas),
        }
        with self.lock:
            self._write()
        threading.Thread(target=self._sample, daemon=True).start()

    @staticmethod
    def idle(lay, message):
        """A status for when nothing is running (after a refit, say): the latest results and a line about them."""
        now = time.time()
        Live._dump(lay.out / "status.json", {"state": "idle", "started": now, "updated": now, "stage": message,
                                             "stage_started": now, "recent": recent_entries(lay)})

    @staticmethod
    def running(lay):
        """Whether a run is reporting right now (its status was rewritten in the last minute)."""
        try:
            data = json.loads((lay.out / "status.json").read_text(encoding="utf-8"))
        except (OSError, ValueError):
            return False
        return data.get("state") in ("running", "loading") and time.time() - float(data.get("updated") or 0) < 60

    @staticmethod
    def _dump(path, data):
        path.parent.mkdir(parents=True, exist_ok=True)
        tmp = path.with_name(f"{path.name}.{os.getpid()}.tmp")  # per process: a refit may write beside a run
        tmp.write_text(json.dumps(data, default=str), encoding="utf-8")
        os.replace(tmp, path)

    def _write(self):
        self.data["updated"] = time.time()
        self._dump(self.path, self.data)
        self._arm()

    def _arm(self):
        """Sets the watchdog's backstop for the step just begun. A hang that holds the GIL stops _sample as well, so
        faulthandler's own thread prints every thread's stack and ends the process (exit code 1) STUCK_GRACE after the
        watchdog would have. Steps waiting on Ollama get none, as the watchdog leaves them."""
        if not self.limit or self.closed or self.armed == self.data["stage_started"]:
            return
        self.armed = self.data["stage_started"]
        if self.data.get("step") in OLLAMA_STEPS:
            faulthandler.cancel_dump_traceback_later()
        else:
            faulthandler.dump_traceback_later(max(1.0, self.armed + self.limit + STUCK_GRACE - time.time()), exit=True)

    def _sample(self):
        while True:
            time.sleep(3)
            util = self._gpu_util()
            with self.lock:
                if self.closed:
                    return
                self.data["gpu_util"] = util
                self._read_memory()
                now = time.time()
                if (self.limit and self.data.get("step") not in OLLAMA_STEPS
                        and now - self.data["stage_started"] > self.limit):
                    self._stuck(now)
                self._write()

    def _stuck(self, now):
        """The step has run past --stage-timeout: native code that never returns, which no signal can interrupt
        (xatlas's chart search once spun on one mesh for good), so the run would wait for ever. Logs the version as
        failed, says so here and in run.log with every thread's stack, and ends the process with STUCK, for spark.sh to
        start the run again. The try was counted in attempts.json before it began, so a version that hangs twice is
        given up on, like one that crashes the run twice. Called with the lock held; doesn't return."""
        faulthandler.cancel_dump_traceback_later()
        text = f"Stuck for {(now - self.data['stage_started']) / 60:.0f} min at: {self.data.get('stage')}"
        print(f"\n{time.strftime('%Y-%m-%d %H:%M:%S')} {text}. Ending the run so that it starts again; where each "
              "thread was:", flush=True)
        faulthandler.dump_traceback(all_threads=True)
        item = self.data.get("item")
        if item:
            key = stem_of(item["id"], item["variant"])
            entry = {"id": item["id"], "key": key, "variant": item["variant"], "name": item["name"],
                     "category": item["category"], "status": "failed", "error": text[:500],
                     "secs": round(now - (self.item_started or now)), "ts": time.strftime("%Y-%m-%d %H:%M:%S")}
            with self.log.open("a", encoding="utf-8") as f:
                f.write(json.dumps(entry) + "\n")
            others = [e for e in self.data["recent"] if (e.get("key") or e.get("id")) != key]
            self.data["recent"] = ([_brief(entry)] + others)[:RECENT]
            self.data["failed"] += 1
        self.closed = True
        self.data.update(state="restarting", step=None, item=None, picture=None, stage_started=now,
                         stage=f"{text}. The run ended so that spark.sh starts it again; that version counts as a "
                               "failed try.")
        self._write()
        os._exit(STUCK)

    def _gpu_util(self):
        if not self.smi:
            return None
        try:
            out = subprocess.run([self.smi, "--query-gpu=utilization.gpu", "--format=csv,noheader,nounits"],
                                 capture_output=True, text=True, timeout=5).stdout
            return int(out.split()[0])
        except (OSError, ValueError, IndexError, subprocess.SubprocessError):
            return None

    def _read_memory(self):
        if self.memory is None:
            return
        try:
            free, total = self.memory()
            self.data["memory_gb"] = [round((total - free) / 2**30, 1), round(total / 2**30, 1)]
        except Exception:
            pass  # a GPU query failing is no reason to stop a run

    def _update(self, **changes):
        with self.lock:
            self.data.update(changes)
            self._write()

    def ready(self, memory):
        with self.lock:
            self.memory = memory
            self._read_memory()
            self.data.update(state="running", stage="Starting", stage_started=time.time())
            self._write()

    def item(self, n, item, variant, variants):
        self.item_started = time.time()
        self._update(index=n, item={"id": item.id, "name": item.name, "category": item.category, "description": None,
                                    "variant": variant, "variants": variants},
                     step=None, stage="Starting", stage_started=self.item_started, picture=None)

    def describe(self, text):
        with self.lock:
            if self.data.get("item"):
                self.data["item"]["description"] = text
            self._write()

    def stage(self, step, text):
        self._update(step=step, stage=text, stage_started=time.time())

    def picture(self, img, n, tries, score=None, problems=None):
        """Shows a reference picture; a fresh one has no critic score yet."""
        tmp = self.picture_path.with_name(f"current.{os.getpid()}.tmp.jpg")
        img.convert("RGB").save(tmp, quality=85)
        os.replace(tmp, self.picture_path)
        # A new number every time, so the browser can't show a cached earlier picture.
        self._update(picture={"n": n, "tries": tries, "score": score, "problems": problems, "version": time.time_ns()})

    def judged(self, score, problems):
        with self.lock:
            if self.data.get("picture"):
                self.data["picture"].update(score=score, problems=problems)
            self._write()

    def finished(self, entry, median_secs, eta_hours):
        with self.lock:
            self.data["ok" if entry.get("status") == "ok" else "failed"] += 1
            key = entry.get("key") or entry.get("id")
            others = [e for e in self.data["recent"] if (e.get("key") or e.get("id")) != key]
            self.data.update(recent=([_brief(entry)] + others)[:RECENT], median_secs=round(median_secs, 1),
                             eta_hours=round(eta_hours, 2), step=None, stage="Next", stage_started=time.time())
            self._write()

    def end(self, state, message=None):
        text = {"done": "Finished.", "stopped": "Stopped. Starting again carries on where it left off."}
        with self.lock:
            self.closed = True
            self.data.update(state=state, step=None, item=None, picture=None, stage_started=time.time(),
                             stage=message or text.get(state, state))
            self._write()
        faulthandler.cancel_dump_traceback_later()


class Generator:
    def __init__(self, args, live, masters):
        import torch
        from diffusers import ZImagePipeline
        from trellis2.pipelines import Trellis2ImageTo3DPipeline

        import llm

        self.args = args
        self.live = live
        self.masters = masters
        self.torch = torch
        t = time.time()
        self.zimage = ZImagePipeline.from_pretrained(str(MODELS / "Z-Image-Turbo"), torch_dtype=torch.bfloat16)
        self.zimage.to("cuda")
        self.zimage.set_progress_bar_config(disable=True)
        # PyTorch's own fast attention kernels don't run on the GB10 (sm_121), so its default falls back to the
        # slow reference path; the flash-attn build in the image does run, about 3.5 times faster at 1024 px.
        if args.zimage_attention != "native":
            try:
                self.zimage.transformer.set_attention_backend(args.zimage_attention)
            except Exception as e:
                print(f"Z-Image-Turbo keeps PyTorch's attention ({args.zimage_attention} unavailable: {e}).", flush=True)
        self.trellis = Trellis2ImageTo3DPipeline.from_pretrained(str(MODELS / "TRELLIS.2-4B"), config_file=TRELLIS_CONFIG)
        self.trellis.low_vram = False  # the run has the Spark to itself: keep every model resident
        self.trellis.cuda()
        self.ollama = llm.Ollama(args.ollama, args.critic_model, think=None, num_ctx=8192, keep_alive="30m", timeout=300)
        self.critic_schema = llm.CRITIC_SCHEMA
        print(f"Models loaded in {time.time() - t:.0f} s.", flush=True)

    # Language and vision model (Ollama on the Spark)

    def describe(self, item, n):
        """n one-sentence descriptions of clearly different real-world versions of the item, the first the most
        typical, each keeping the item's shape so they all fit its one primitive model."""
        msgs = [
            {"role": "system", "content": (
                f"You describe real-world objects for a 3D artist. Write {n} descriptions of {n} clearly different "
                "real-world versions of the item the user names, as a survivor might find them. Each is one sentence "
                "of at most 40 words describing only how that version looks: its shape, parts, materials and colours. "
                "The first is the most typical version. Make them differ in colour, material, finish, design details "
                "or wear, but keep every one unmistakably this item, with the same overall shape and proportions. "
                "Describe one of the item alone: never several of them (a stack, pile or row), and never with a "
                "case, stand, holder or anything else that isn't part of it. "
                "If the item is normally sold packaged (a can, box, bottle or blister pack), describe the "
                "package. If the name is a game item id, describe the real object it stands for. No brand names, "
                "text, backstory or usage.")},
            {"role": "user", "content": item.prompt_line()},
        ]
        out, _, _ = self.ollama.chat_json(msgs, describe_schema(n), temperature=0.6, think=False)
        texts = [" ".join(str(x).split())[:400] for x in out.get("versions") or [] if str(x).strip()]
        if len(texts) < n:
            raise RuntimeError(f"the description model gave {len(texts)} of the {n} versions asked for")
        return texts[:n]

    def judge_picture(self, img, item, visual, worn=None):
        if worn:
            what = (f"This picture should show one {item.name} (a {item.category.lower()} item in a survival game), "
                    f"as a reference for a 3D artist: {visual}\nIt should be a {worn}.\nNothing of the wearer may "
                    "show: no skin, no head, no hands, no body, no mannequin; the item alone, keeping the shape it has "
                    "when worn.\n")
        else:
            what = (f"This picture should show one {item.name} (a {item.category.lower()} item in a survival game; "
                    f"a pair counts as one item), as a reference for a 3D artist: {visual}\n")
        prompt = (
            what + "Score 1-10 how well it does that: the right object, the whole of it in frame and not cut off, "
            "alone on a plain background, with no text, hands, people or other objects. In 'problems' say what is "
            "wrong, or 'none'.")
        msgs = [{"role": "user", "content": prompt, "images": [_b64(img)]}]
        out, _, _ = self.ollama.chat_json(msgs, self.critic_schema, temperature=0.2, think=False)
        return int(out.get("score", 0)), str(out.get("problems", "")).strip()[:300]

    def judge_mesh(self, views, item, worn=None):
        from PIL import Image

        pair = Image.new("RGB", (views[0].width * 2, views[0].height))
        pair.paste(views[0], (0, 0))
        pair.paste(views[1], (views[0].width, 0))
        where = "shaped as when it is worn, with nothing of the wearer" if worn else "lying on the floor"
        prompt = (
            f"These are two renders (a three-quarter view and a view from above) of a textured 3D game asset, "
            f"{where}. It is meant to be: {item.prompt_line()}\nScore 1-10 how clearly it reads as that item (shape, "
            "proportions, surface detail, sensible colours). In 'problems' list what is wrong, or 'none'.")
        msgs = [{"role": "user", "content": prompt, "images": [_b64(pair, size=768)]}]
        out, _, _ = self.ollama.chat_json(msgs, self.critic_schema, temperature=0.2, think=False)
        return int(out.get("score", 0)), str(out.get("problems", "")).strip()[:300]

    def person_check(self, img):
        """(whether any part of a person or mannequin shows in a worn photo, what). A plain yes-or-no question: the
        critic answers it reliably, where folding the same flaw into a 1-10 score let a photo of a man in the T-shirt
        through at 7."""
        msgs = [{"role": "user", "content": PERSON_PROMPT, "images": [_b64(img, size=768)]}]
        out, _, _ = self.ollama.chat_json(msgs, PERSON_SCHEMA, temperature=0.0, think=False)
        return bool(out.get("person")), " ".join(str(out.get("what", "")).split())[:120]

    def copies_check(self, img):
        """(whether the photo shows several loose copies of the item, a stack of packets say, what is repeated). Many
        things in a box or tin (a box of nails) are the item itself."""
        b64 = _b64(img, size=768)
        out, _, _ = self.ollama.chat_json([{"role": "user", "content": COPIES_PROMPT, "images": [b64]}], COPIES_SCHEMA,
                                          temperature=0.0, think=False)
        what = " ".join(str(out.get("what", "")).split())[:120]
        if not out.get("copies"):
            return False, what
        inside, _, _ = self.ollama.chat_json(
            [{"role": "user", "content": INSIDE_PROMPT.format(what=what), "images": [b64]}], INSIDE_SCHEMA,
            temperature=0.0, think=False)
        return not inside.get("inside"), what

    # GPU models

    def picture(self, prompt, seed):
        g = self.torch.Generator("cuda").manual_seed(seed)
        size = self.args.image_size
        return self.zimage(prompt=prompt, height=size, width=size, num_inference_steps=9, guidance_scale=0.0,
                           generator=g).images[0]

    def cutout(self, photo):
        """The photo with its background removed (BiRefNet), cropped to the item at full resolution and scaled so its
        long side is at most --cond-size, as RGBA, which TRELLIS.2 then takes as it is. Left to itself, TRELLIS.2
        would shrink the whole photo to 1024 first and crop afterwards, throwing away most of a large photo's pixels
        on the item. None when no item was found."""
        import numpy as np
        from PIL import Image

        rgba = self.trellis.rembg_model(photo.convert("RGB"))
        ys, xs = np.nonzero(np.asarray(rgba)[:, :, 3] > 127)
        if len(xs) < 256:
            return None
        x0, x1, y0, y1 = int(xs.min()), int(xs.max()) + 1, int(ys.min()), int(ys.max()) + 1
        pad = int(0.04 * max(x1 - x0, y1 - y0))
        crop = rgba.crop((max(0, x0 - pad), max(0, y0 - pad), min(rgba.width, x1 + pad), min(rgba.height, y1 + pad)))
        s = self.args.cond_size / max(crop.size)
        if s < 1.0:
            crop = crop.resize((max(1, round(crop.width * s)), max(1, round(crop.height * s))), Image.LANCZOS)
        return crop

    def mesh(self, cut, seed, size, pre=""):
        """TRELLIS.2 on the cutout, then to_glb for the full-detail master (when the NAS keeps one) and the game copy's
        two levels of detail at the item's budget (see budget). Returns (master, near, far, high): to_glb's meshes, and
        TRELLIS.2's own full-detail mesh on the GPU (meshpost.high_res_unity) for the normal bake."""
        import o_voxel

        import meshpost

        args = self.args
        m = self.trellis.run(cut, seed=seed, pipeline_type=args.res, max_num_tokens=args.max_tokens)[0]
        m.simplify(16777216)  # nvdiffrast's limit, as in TRELLIS.2's example
        # What the sampler cached is no use to the bakes; GPU and system share the Spark's memory, so hand it back.
        self.torch.cuda.empty_cache()

        def lod(target, tex):
            glb = o_voxel.postprocess.to_glb(
                vertices=m.vertices.clone(), faces=m.faces.clone(), attr_volume=m.attrs, coords=m.coords,
                attr_layout=m.layout, voxel_size=m.voxel_size, aabb=[[-0.5, -0.5, -0.5], [0.5, 0.5, 0.5]],
                decimation_target=target, texture_size=tex, remesh=True, remesh_band=1, remesh_project=0,
                verbose=False)
            self.torch.cuda.empty_cache()
            return glb

        master = None
        if self.masters:
            self.live.stage("bake", f"{pre}Baking the full-detail master ({args.master_tex} px textures)")
            master = lod(args.master_verts, args.master_tex)
        self.live.stage("bake", f"{pre}Baking the game copy's two levels of detail ({size['near_tex']} px)")
        near, far = lod(size["near_verts"], size["near_tex"]), lod(size["far_verts"], size["far_tex"])
        return master, near, far, meshpost.high_res_unity(m)

    # One variant

    def photos(self, item, visual, prompt, seed, pre, worn=None):
        """Paints up to --image-tries photos, each judged by the critic, stopping at the first good one. Returns
        (all of them, the best). Each photo is also asked whether it shows several copies of the item (copies_check),
        and a worn item's whether any part of a person shows (person_check); one that fails either scores at most
        GATE_SCORE."""
        args = self.args
        live = self.live
        pictures = []
        for i in range(args.image_tries):
            s = seed + 7919 * i
            live.stage("picture", f"{pre}Painting reference photo {i + 1} ({args.image_size} px, Z-Image-Turbo)")
            img = self.picture(prompt, s)
            live.picture(img, i + 1, args.image_tries)
            live.stage("judge_picture", f"{pre}Judging the photo ({args.critic_model})")
            score, problems = self.judge_picture(img, item, visual, worn)
            copies, repeated = self.copies_check(img)
            if copies:
                score, problems = min(score, GATE_SCORE), f"several copies ({repeated}); {problems}"[:300]
            person = False
            if worn:
                person, what = self.person_check(img)
                if person:
                    score, problems = min(score, GATE_SCORE), f"part of a person shows ({what}); {problems}"[:300]
            live.judged(score, problems)
            pictures.append({"seed": s, "score": score, "problems": problems, "person": person, "copies": copies,
                             "image": img})
            if score >= args.min_picture_score:
                break
        best = max(pictures, key=lambda p: p["score"])
        if best is not pictures[-1]:
            live.picture(best["image"], pictures.index(best) + 1, args.image_tries, best["score"], best["problems"])
        return pictures, best

    def attempt(self, item, parts, visual, prompt, seed, k=0, worn=None):
        """Photos -> cutout -> mesh -> fitted LODs, maps and renders, with the critics' scores, as a dict. When no photo
        reaches --min-picture-score the best of them is used anyway; its score in the log shows it."""
        import numpy as np
        from PIL import Image

        import meshpost

        args = self.args
        live = self.live
        pre = f"Try {k + 1} of {args.mesh_tries} (the last mesh scored low) · " if k else ""
        pictures, best = self.photos(item, visual, prompt, seed, pre, worn)
        result = {"pictures": pictures, "picture": best}

        live.stage("cutout", f"{pre}Cutting the item out of the photo (BiRefNet)")
        cut = self.cutout(best["image"])
        if cut is None:
            raise RuntimeError("the background remover found no item in the photo")
        size = budget(parts, args)
        live.stage("mesh", f"{pre}Turning the photo into a 3D mesh (TRELLIS.2, {args.res})")
        master, near, far, high = self.mesh(cut, best["seed"], size, pre)

        live.stage("normals", f"{pre}Baking the full mesh's detail into a normal map")
        near_l = meshpost.drop_inner_shells(meshpost.unity_arrays(near))
        far_l = meshpost.drop_inner_shells(meshpost.unity_arrays(far))
        near_l, far_l = (*near_l, meshpost.tangents(*near_l)), (*far_l, meshpost.tangents(*far_l))
        # The bake reads the full mesh through the game mesh's frame; check the two line up before trusting it.
        hlo, hhi = (x.cpu().numpy() for x in (high[0].min(0).values, high[0].max(0).values))
        llo, lhi = near_l[0].min(axis=0), near_l[0].max(axis=0)
        if max(np.abs(hlo - llo).max(), np.abs(hhi - lhi).max()) > 0.05 * float((lhi - llo).max()):
            raise RuntimeError("the full-detail mesh and the game mesh don't line up; the normal bake would be wrong")
        normal_map = Image.fromarray(meshpost.bake_normal_map(near_l, high, size["normal_tex"]))
        del high
        self.torch.cuda.empty_cache()

        live.stage("fit", f"{pre}Fitting the mesh onto the item's model")
        fitted = meshpost.fit(near_l[0], near_l[1], parts)
        lods, base_height, offset = meshpost.place([near_l, far_l], fitted)
        # Where TRELLIS.2's up and front ended up, so a later refit can still keep the item upright and face up.
        turned = {"up": [round(float(x), 4) for x in fitted[1] @ (0.0, 1.0, 0.0)],
                  "front": [round(float(x), 4) for x in fitted[1] @ (0.0, 0.0, 1.0)]}
        smooth, metal = meshpost.surface(near)
        mask = meshpost.mask_map(near)
        if mask is None:  # no PBR map: a flat one, so every item's near copy uses the same material set-up
            mask = Image.new("RGBA", (8, 8), (round(metal * 255), 0, 0, round(smooth * 255)))
        mask = mask.resize((size["mask_tex"], size["mask_tex"]), Image.LANCZOS)

        live.stage("render", f"{pre}Rendering previews on the GPU (nvdiffrast)")
        textures = [meshpost.base_color(near), meshpost.base_color(far)]
        views = meshpost.render_pair(lods[0], textures[0], normal_map=normal_map)
        live.stage("judge_mesh", f"{pre}Judging the mesh ({args.critic_model})")
        mesh_score, mesh_problems = self.judge_mesh(views, item)
        result.update(lods=lods, textures=textures, normal_map=normal_map, mask=mask, base_height=base_height,
                      surface=(smooth, metal), fit=round(fitted[0], 3), views=views, mesh_score=mesh_score,
                      mesh_problems=mesh_problems, cutout=cut, master=master, budget=size,
                      placement={"rotation": np.round(fitted[1], 6).tolist(), "scale": round(float(fitted[2]), 6),
                                 "centre": np.round(fitted[3], 6).tolist(), "offset": np.round(offset, 6).tolist()},
                      **turned)
        return result

    def worn_attempt(self, item, visual, prompt, seed, wording, k=0):
        """A garment as worn (ghost mannequin): photos -> cutout -> TRELLIS.2 -> the full-detail mesh, with renders and
        the critics' scores. Only the NAS keeps it, for the step that fits garments to the body."""
        from PIL import Image

        import meshpost

        args = self.args
        live = self.live
        pre = "Worn on the body · " + (f"try {k + 1} of {args.mesh_tries} · " if k else "")
        pictures, best = self.photos(item, visual, prompt, seed, pre, wording)
        if best["person"]:
            # A garment mesh with a body inside would mislead the fitting step more than no mesh at all.
            return {"pictures": pictures, "picture": best, "skipped": "every worn photo showed part of a person"}
        live.stage("cutout", f"{pre}Cutting the garment out of the photo (BiRefNet)")
        cut = self.cutout(best["image"])
        if cut is None:
            raise RuntimeError("the background remover found no garment in the worn photo")
        live.stage("mesh", f"{pre}Turning the photo into a 3D mesh (TRELLIS.2, {args.res})")
        m = self.trellis.run(cut, seed=best["seed"], pipeline_type=args.res, max_num_tokens=args.max_tokens)[0]
        m.simplify(16777216)
        self.torch.cuda.empty_cache()
        live.stage("bake", f"{pre}Baking the full-detail mesh ({args.master_tex} px textures)")
        import o_voxel

        master = o_voxel.postprocess.to_glb(
            vertices=m.vertices, faces=m.faces, attr_volume=m.attrs, coords=m.coords, attr_layout=m.layout,
            voxel_size=m.voxel_size, aabb=[[-0.5, -0.5, -0.5], [0.5, 0.5, 0.5]], decimation_target=args.master_verts,
            texture_size=args.master_tex, remesh=True, remesh_band=1, remesh_project=0, verbose=False)
        del m
        self.torch.cuda.empty_cache()
        live.stage("render", f"{pre}Rendering previews on the GPU (nvdiffrast)")
        lod = meshpost.drop_inner_shells(meshpost.unity_arrays(master))
        views = meshpost.render_pair(lod, meshpost.base_color(master).resize((1024, 1024), Image.LANCZOS))
        live.stage("judge_mesh", f"{pre}Judging the mesh ({args.critic_model})")
        score, problems = self.judge_mesh(views, item, wording)
        return {"pictures": pictures, "picture": best, "cutout": cut, "master": master, "views": views,
                "mesh_score": score, "mesh_problems": problems}

    def make(self, item, ref, lay, descriptions, variant):
        import meshpost

        args = self.args
        t0 = time.time()
        stem = stem_of(item.id, variant)
        base = {"id": item.id, "key": stem, "variant": variant, "name": item.name, "category": item.category}
        parts = load_reference(ref)
        if not parts:
            return {**base, "status": "failed", "error": f"reference model {ref.name} has no parts"}

        visuals = descriptions.get(item.id)
        if args.redescribe or not isinstance(visuals, list) or len(visuals) < args.variants:
            self.live.stage("describe", f"Describing {args.variants} versions of the item ({args.critic_model})")
            visuals = self.describe(item, args.variants)
            descriptions[item.id] = visuals
        visual = visuals[variant - 1]
        self.live.describe(visual)
        kind, view, wording = worn_kind(item) or (None, None, None)
        flat = any(t in item.traits for t in FLAT_TRAITS)
        # A rigid worn thing's one photo shows it as worn, so the fitting step can tell which way it faces on the body.
        shot = (worn_shot(wording) if kind == "rigid" else FLAT_SHOT if flat else THIN_SHOT if is_thin(parts)
                else SHOT)
        prompt = f"{item.name}: {visual} {STYLES[args.style]} {shot}"
        seed = variant_seed(item.id, variant)

        best = None
        for k in range(args.mesh_tries):
            r = self.attempt(item, parts, visual, prompt, seed + 104_729 * k, k, wording if kind == "rigid" else None)
            if best is None or r["mesh_score"] > best["mesh_score"]:
                best = r
            if r["mesh_score"] >= args.min_mesh_score:
                break

        # A garment also gets a second photo and mesh showing it worn, kept on the NAS for the step that fits it to
        # the body; without the NAS nothing would keep it, so it isn't made.
        worn, worn_prompt, worn_skipped = None, None, None
        if kind == "soft" and self.masters:
            worn_prompt = f"{item.name}: {visual} {STYLES[args.style]} {worn_shot(wording)}"
            for k in range(args.mesh_tries):
                w = self.worn_attempt(item, visual, worn_prompt, seed + 7_777_777 + 104_729 * k, wording, k)
                if w.get("skipped"):
                    worn_skipped = w["skipped"]
                    continue
                if worn is None or w["mesh_score"] > worn["mesh_score"]:
                    worn = w
                if w["mesh_score"] >= args.min_mesh_score:
                    break

        pictures = best["pictures"]
        chosen = pictures.index(best["picture"]) + 1
        keep = ("seed", "score", "problems", "person", "copies")
        entry = {
            **base, "status": "ok", "picture_score": best["picture"]["score"],
            "picture_problems": best["picture"]["problems"],
            "pictures": [{k: p[k] for k in keep} for p in pictures],
            "mesh_score": best["mesh_score"], "mesh_problems": best["mesh_problems"], "fit": best["fit"],
            "up": best["up"], "front": best["front"], "tris": [int(len(l[1])) for l in best["lods"]],
            "verts": [int(len(l[0])) for l in best["lods"]], "base_height": best["base_height"],
            "smoothness": best["surface"][0], "metallic": best["surface"][1], "res": args.res, "style": args.style,
            "image_size": args.image_size, "budget": best["budget"], "description": visual,
        }
        if kind:
            entry["worn"] = {"kind": kind, "view": view}
        if worn:
            entry["worn"].update(picture_score=worn["picture"]["score"], mesh_score=worn["mesh_score"],
                                 mesh_problems=worn["mesh_problems"])
        elif worn_skipped:
            entry["worn"]["skipped"] = worn_skipped

        # The originals first: the game copy's .bytes, written last, is what marks the variant done.
        if self.masters:
            self.live.stage("save", "Saving the photos and the full-detail mesh to the NAS")
            meta = {**{k: v for k, v in entry.items() if k != "status"}, "prompt": prompt, "chosen_photo": chosen,
                    "placement": best["placement"], "cond_size": args.cond_size,
                    "master": {"texture": args.master_tex, "decimation_target": args.master_verts,
                               "frame": "glTF: Y up, metres of TRELLIS.2's unit box, as generated; 'placement' turns, "
                                        "scales and moves the game copy, made in Unity's frame (X mirrored)"},
                    "models": PINS, "machine": socket.gethostname(), "made": time.strftime("%Y-%m-%d %H:%M:%S")}
            if kind == "rigid" or worn:
                meta["worn"]["camera"] = (f"The {'worn/' if kind == 'soft' else ''}photo was taken from the wearer's "
                                          f"{view}.")
            if worn:
                meta["worn"].update(prompt=worn_prompt, chosen_photo=worn["pictures"].index(worn["picture"]) + 1,
                                    pictures=[{k: p[k] for k in keep} for p in worn["pictures"]])
            entry["nas"] = str(self.masters.write(item, variant, best, meta, worn).relative_to(self.masters.share))

        self.live.stage("save", "Saving the game copy and the preview")
        lay.meshes.mkdir(parents=True, exist_ok=True)
        lay.previews.mkdir(parents=True, exist_ok=True)
        lay.refs.mkdir(parents=True, exist_ok=True)
        meshpost.save_image(best["textures"][0], lay.meshes / f"{stem}_tex0.jpg", quality=88)
        meshpost.save_image(best["textures"][1], lay.meshes / f"{stem}_tex1.jpg", quality=88)
        # Full chroma: normal maps keep their direction in red and green, which 4:2:0 subsampling would smear. Below
        # quality 92 the normals shift by degrees for a tenth less file.
        meshpost.save_image(best["normal_map"], lay.meshes / f"{stem}_nrm0.jpg", quality=92, subsampling=0)
        meshpost.save_image(best["mask"], lay.meshes / f"{stem}_msk0.png", optimize=True)
        smooth, metal = best["surface"]
        meshpost.write_mesh_file(lay.meshes / f"{stem}.bytes", best["lods"], best["base_height"], smooth, metal,
                                 maps=True)
        thumb = best["picture"]["image"].convert("RGB")
        thumb.thumbnail((1024, 1024))
        meshpost.save_image(thumb, lay.refs / f"{stem}.jpg", quality=90)
        caption = (f"{item.name} [{stem}]  photo {best['picture']['score']}/10  mesh {best['mesh_score']}/10  "
                   f"fit {best['fit']:.2f}")
        meshpost.save_image(meshpost.sheet(best["picture"]["image"], best["views"], caption),
                            lay.previews / f"{stem}.jpg", quality=85)
        if worn:
            caption = (f"{item.name} [{stem}] worn  photo {worn['picture']['score']}/10  mesh {worn['mesh_score']}/10")
            meshpost.save_image(meshpost.sheet(worn["picture"]["image"], worn["views"], caption),
                                lay.previews / f"{stem}_worn.jpg", quality=85)
        entry["secs"] = round(time.time() - t0, 1)
        return entry


class Masters:
    """The originals on the NAS share (\\\\192.168.1.170\\CLONE-Z on Windows, /mnt/clonez on the Spark, /nas in the
    container), under assets/item-meshes/<id>/v<variant>/:
      photo.png          the reference photo the mesh was made from, full size
      candidates/<n>.png every photo painted for this variant, the rejected ones included (scores in meta.json)
      cutout.png         what TRELLIS.2 was given: the item cut out of the photo and cropped
      master.glb         TRELLIS.2's mesh at full detail with its PBR textures (base colour, metallic-roughness)
      worn/              for a garment, the same four for its photo and mesh as worn (ghost mannequin)
      meta.json          the description, prompts, seeds, scores, fit, worn view and models it was made with
    As the share's README asks, each variant is written into a temporary folder that replaces the old one only when
    complete."""

    def __init__(self, share):
        self.share = Path(share)
        if not (self.share / "README.md").exists():
            raise RuntimeError(f"{self.share} isn't the NAS share (its README.md is missing); is it mounted?")
        self.root = self.share / NAS_ITEMS
        self.root.mkdir(parents=True, exist_ok=True)
        probe = self.root / f".write-test-{socket.gethostname()}-{os.getpid()}"
        probe.write_text("ok", encoding="utf-8")
        probe.unlink()

    @staticmethod
    def _originals(folder, result):
        import meshpost

        (folder / "candidates").mkdir(parents=True)
        for i, p in enumerate(result["pictures"], 1):
            p["image"].save(folder / "candidates" / f"{i}.png")
        result["picture"]["image"].save(folder / "photo.png")
        result["cutout"].save(folder / "cutout.png")
        return meshpost.export_master(result["master"], folder / "master.glb") if result.get("master") else None

    def write(self, item, variant, result, meta, worn=None):
        final = self.root / item.id / f"v{variant}"
        tmp = final.with_name(f"{final.name}.tmp-{os.getpid()}")
        if tmp.exists():
            shutil.rmtree(tmp)
        faces = self._originals(tmp, result)
        if faces is not None:
            meta["master"]["faces"] = faces
        if worn:
            meta["worn"]["master_faces"] = self._originals(tmp / "worn", worn)
        (tmp / "meta.json").write_text(json.dumps(meta, indent=1), encoding="utf-8")
        old = None
        if final.exists():
            old = final.with_name(f"{final.name}.old-{os.getpid()}")
            os.replace(final, old)
        os.replace(tmp, final)
        if old is not None:
            shutil.rmtree(old, ignore_errors=True)
        return final


def write_gallery(lay):
    entries = sorted(read_log(lay).values(), key=lambda e: (e["id"].lower(), e.get("variant") or 0))
    ok = [e for e in entries if e.get("status") == "ok"]
    rows = []
    for e in entries:
        key = e.get("key") or e["id"]
        if e.get("status") == "ok":
            variant = f"version {e['variant']} · " if e.get("variant") else ""
            note = html.escape(f"{variant}photo {e.get('picture_score')}/10 · mesh {e.get('mesh_score')}/10 · fit "
                               f"{e.get('fit')} · {e.get('tris', ['?'])[0]} tris · {e.get('secs')} s")
            problems = html.escape(e.get("mesh_problems") or "")
            # The version makes a browser fetch a tile again after a refit redraws it.
            src = f"previews/{urllib.parse.quote(key)}.jpg?v={zlib.crc32(str(e.get('refit') or e.get('ts')).encode())}"
            worn = e.get("worn") or {}
            extra = ""
            if worn.get("mesh_score") is not None:
                extra = (f'<img loading="lazy" src="previews/{html.escape(urllib.parse.quote(key))}_worn.jpg">'
                         f'<br><small>as worn: photo {worn.get("picture_score")}/10 · mesh {worn["mesh_score"]}/10 · '
                         f'{html.escape(worn.get("mesh_problems") or "")}</small>')
            rows.append(f'<figure><img loading="lazy" src="{html.escape(src)}">'
                        f'<figcaption>{note}<br><small>{problems}</small>{extra}</figcaption></figure>')
        else:
            rows.append(f'<figure class="bad"><figcaption>{html.escape(key)}: '
                        f'{html.escape(str(e.get("error", "failed")))[:300]}</figcaption></figure>')
    lay.out.mkdir(parents=True, exist_ok=True)
    tmp = lay.out / f"index.html.{os.getpid()}.tmp"  # per process: a refit may write beside a run
    tmp.write_text(
        "<!doctype html><meta charset=utf-8><title>CLONE-Z item meshes</title><style>"
        "body{background:#1e1f24;color:#ddd;font:13px sans-serif}figure{display:inline-block;margin:6px;width:960px}"
        "img{width:960px}figure.bad{color:#f88;width:auto}small{color:#aaa}a{color:#8ab4f8}</style>"
        f"<h1>Item meshes: {len(ok)} done, {len(entries) - len(ok)} failed</h1>"
        "<p>Each tile: the reference photo, then the mesh as the game will see it (three-quarter and top-down views, "
        "with its normal map, rendered on the Spark's GPU). <a href=\"live.html\">Watch the run live</a></p>"
        + "\n".join(rows), encoding="utf-8")
    os.replace(tmp, lay.out / "index.html")


def cmd_generate(args):
    os.environ.setdefault("HF_HUB_OFFLINE", "1")  # everything was downloaded by prepare
    lay = Layout(args.root)
    if not (MODELS / "prepared.json").exists():
        sys.exit("The weights aren't prepared; run 'prepare' first.")
    if not 1 <= args.variants <= MAX_VARIANTS:
        sys.exit(f"--variants must be 1 to {MAX_VARIANTS}; the game looks for no more than that.")
    masters = None
    if args.nas:
        try:
            masters = Masters(args.nas)
        except Exception as e:
            sys.exit(f"Can't keep the originals on the NAS ({e}). Fix the share, or run with --no-nas.")
    todo, no_ref, handmade, gave_up = choose(lay, args)
    if no_ref:
        hand = [i for i in no_ref if i in handmade]
        print(f"{len(no_ref)} item(s) have no primitive model to fit onto and are skipped"
              + (f" ({len(hand)} hand-built: export them from Unity with CloneZ > Asset Pipeline)" if hand else "")
              + ".", flush=True)
    if gave_up:
        print(f"{len(gave_up)} version(s) failed {GIVE_UP} times in a row and are skipped (--retry-failed tries them "
              f"again): {', '.join(gave_up[:10])}{' ...' if len(gave_up) > 10 else ''}", flush=True)
    units = [(item, ref, k) for item, ref, missing in todo for k in missing]
    if not units:
        print("Nothing to do.", flush=True)
        write_gallery(lay)
        Live.idle(lay, "Nothing is running: every item already has its meshes.")
        sys.exit(NOTHING_TO_DO)

    signal.signal(signal.SIGINT, _on_signal)
    signal.signal(signal.SIGTERM, _on_signal)
    print(f"{len(units)} mesh(es) to make for {len(todo)} item(s) ({args.variants} versions each) · photos "
          f"{args.image_size} px · TRELLIS.2 {args.res} · {args.style} · critic {args.critic_model} · originals "
          f"{'to ' + str(masters.root) if masters else 'not kept'}", flush=True)
    live = Live(lay, len(units), args)
    try:
        gen = Generator(args, live, masters)
    except BaseException as e:
        live.end("stopped" if isinstance(e, KeyboardInterrupt) else "failed",
                 f"The models didn't load ({type(e).__name__}: {e}); see run.log.")
        raise
    live.ready(gen.torch.cuda.mem_get_info)
    descriptions = {}
    if lay.descriptions.exists():
        descriptions = json.loads(lay.descriptions.read_text(encoding="utf-8"))

    lay.out.mkdir(parents=True, exist_ok=True)
    attempts = read_attempts(lay)
    times = []
    n = 0
    stopped = False
    for n, (item, ref, variant) in enumerate(units, 1):
        if _stop:
            stopped = True
            break
        live.item(n, item, variant, args.variants)
        t = time.time()
        stem = stem_of(item.id, variant)
        attempts[stem] = attempts.get(stem, 0) + 1
        write_attempts(lay, attempts)
        try:
            entry = gen.make(item, ref, lay, descriptions, variant)
        except KeyboardInterrupt:
            attempts[stem] -= 1  # stopped by hand: not the version's fault
            write_attempts(lay, {k: v for k, v in attempts.items() if v > 0})
            print("Stopped.", flush=True)
            stopped = True
            break
        except Exception as e:
            traceback.print_exc()
            entry = {"id": item.id, "key": stem, "variant": variant, "name": item.name,
                     "category": item.category, "status": "failed", "error": f"{type(e).__name__}: {e}"[:500],
                     "secs": round(time.time() - t)}
        if entry["status"] == "ok":
            attempts.pop(stem, None)
            write_attempts(lay, attempts)
        # Hand what this version cached back to the driver: GPU and system share the Spark's memory.
        gen.torch.cuda.empty_cache()
        entry["ts"] = time.strftime("%Y-%m-%d %H:%M:%S")
        with lay.log.open("a", encoding="utf-8") as f:
            f.write(json.dumps(entry) + "\n")
        lay.descriptions.write_text(json.dumps(descriptions, indent=0, sort_keys=True), encoding="utf-8")
        times.append(time.time() - t)
        median = statistics.median(times[-30:])
        eta = median * (len(units) - n) / 3600
        live.finished(entry, median, eta)
        label = f"[{n}/{len(units)}] {item.id} version {variant}"
        if entry["status"] == "ok":
            print(f"{label}: photo {entry['picture_score']}/10, mesh {entry['mesh_score']}/10, fit {entry['fit']:.2f}, "
                  f"{entry['tris'][0]} tris, {entry['secs']:.0f} s · ETA {eta:.1f} h", flush=True)
        else:
            print(f"{label}: FAILED {entry['error']}", flush=True)
        write_gallery(lay)
    # A stop asked for during the last mesh still lets it finish, so that run is complete.
    stopped = stopped or (_stop and n < len(units))
    live.end("stopped" if stopped else "done")
    write_gallery(lay)
    print("Stopped; 'start' picks up where this left off." if stopped else "Done.", flush=True)


# ── refit ───────────────────────────────────────────────────────────────────────────────────────

def cmd_refit(args):
    """Fits finished meshes onto their reference models again with the current meshpost.fit (dropping the
    remesher's inner skins on the way) and redraws their gallery tiles, without generating anything: for when the
    fitting improves, or a reference model changes. --ids takes item ids (every version) or single versions (id@2)."""
    import meshpost
    import numpy as np
    from PIL import Image

    lay = Layout(args.root)
    items = catalog.load_items(lay.resources)
    handmade = catalog.handmade_ids(lay.item_models_cs)
    owner = item_owners(items, handmade)
    wanted = {i.strip().lower() for i in args.ids.split(",") if i.strip()} if args.ids else None
    log = read_log(lay)
    done = 0
    for path in sorted(lay.meshes.glob("*@*.bytes")) if lay.meshes.exists() else []:
        stem = path.stem
        item_id = stem.rpartition("@")[0]
        if wanted is not None and stem.lower() not in wanted and item_id.lower() not in wanted:
            continue
        item = owner.get(item_id.lower())
        ref = reference_path(lay, item.id, handmade) if item else None
        parts = load_reference(ref) if ref else None
        if not parts:
            print(f"{stem}: no reference model to fit onto; left as it is", flush=True)
            continue
        _, smooth, metal, flags, lods = meshpost.read_mesh_file(path)
        lods = [meshpost.drop_inner_shells(lod) for lod in lods]
        entry = dict(log.get(stem) or {"id": item.id, "key": stem, "status": "ok"})
        # Meshes from before up and front were kept have nothing better to go on than how they lie now.
        up, front = entry.get("up") or (0.0, 1.0, 0.0), entry.get("front") or (0.0, 0.0, 1.0)
        fitted = meshpost.fit(lods[0][0], lods[0][1], parts, up=up, front=front)
        lods, base_height, _ = meshpost.place(lods, fitted)
        meshpost.write_mesh_file(path, lods, base_height, smooth, metal, maps=bool(flags & meshpost.FLAG_MAPS))

        entry.update(fit=round(fitted[0], 3), base_height=base_height, refit=time.strftime("%Y-%m-%d %H:%M:%S"),
                     up=[round(float(x), 4) for x in fitted[1] @ np.asarray(up, float)],
                     front=[round(float(x), 4) for x in fitted[1] @ np.asarray(front, float)])
        tex = lay.meshes / f"{stem}_tex0.jpg"
        if tex.exists():
            nrm = lay.meshes / f"{stem}_nrm0.jpg"
            normal_map = Image.open(nrm).convert("RGB") if nrm.exists() else None
            views = meshpost.render_pair(lods[0], Image.open(tex).convert("RGB"), normal_map=normal_map)
            pic = lay.refs / f"{stem}.jpg"
            reference = Image.open(pic) if pic.exists() else Image.new("RGB", (320, 320), meshpost.PREVIEW_BG)
            caption = (f"{item.name} [{stem}]  photo {entry.get('picture_score')}/10  "
                       f"mesh {entry.get('mesh_score')}/10  fit {entry['fit']:.2f}")
            lay.previews.mkdir(parents=True, exist_ok=True)
            meshpost.save_image(meshpost.sheet(reference, views, caption), lay.previews / f"{stem}.jpg", quality=85)
        with lay.log.open("a", encoding="utf-8") as f:
            f.write(json.dumps(entry) + "\n")
        done += 1
        print(f"{stem}: fit {fitted[0]:.2f}", flush=True)
    write_gallery(lay)
    print(f"Refitted {done} mesh(es).", flush=True)
    if not Live.running(lay):
        Live.idle(lay, f"Nothing is running. Refitted {done} mesh(es) at {time.strftime('%H:%M')}.")


# ── status ──────────────────────────────────────────────────────────────────────────────────────

def cmd_status(args):
    lay = Layout(args.root)
    items = catalog.load_items(lay.resources)
    handmade = catalog.handmade_ids(lay.item_models_cs)
    log = read_log(lay)
    done = done_variants(lay)
    unique = item_owners(items, handmade)
    with_ref = [it for it in unique.values() if reference_path(lay, it.id, handmade)]
    have = {it.id: sum(stem_of(it.id, k).lower() in done for k in range(1, args.variants + 1)) for it in with_ref}
    made = sum(have.values())
    total = args.variants * len(with_ref)
    complete = sum(1 for n in have.values() if n == args.variants)
    partly = sum(1 for n in have.values() if 0 < n < args.variants)
    failed = [k for k, e in log.items() if e.get("status") != "ok" and k.lower() not in done]
    print(f"Meshes: {made} of {total} made ({args.variants} versions of each of {len(with_ref)} items: {complete} "
          f"complete, {partly} partly), {len(failed)} failed; {len(unique) - len(with_ref)} item(s) have no model "
          f"to fit onto.")
    gave_up = sorted(k for k, v in read_attempts(lay).items() if v >= GIVE_UP and k.lower() not in done)
    if gave_up:
        print(f"Given up on {len(gave_up)} version(s) after {GIVE_UP} failures in a row (start with --retry-failed to "
              f"try them again): {', '.join(gave_up[:10])}{' ...' if len(gave_up) > 10 else ''}")
    current = [e for e in log.values() if e.get("status") == "ok" and e.get("variant")]
    secs = [e["secs"] for e in current if e.get("secs")]
    if secs:
        med = statistics.median(secs[-100:])
        left = med * (total - made) / 3600
        print(f"Median {med:.0f} s per mesh; the rest would take about {left:.0f} hours, {left / 24:.0f} days running "
              f"around the clock.")
    scores = [e.get("mesh_score") for e in current]
    if scores:
        low = sum(1 for s in scores if (s or 0) < 5)
        print(f"Mesh critic: median {statistics.median(scores)}/10, {low} below 5.")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    sub.add_parser("prepare", help="download the pinned weights and convert DINOv3 (once)")

    g = sub.add_parser("generate", help="mesh the items")
    g.add_argument("--root", default=str(ROOT_DEFAULT))
    g.add_argument("--ollama", default="http://127.0.0.1:11434")
    g.add_argument("--critic-model", default="gemma3:12b")
    g.add_argument("--pilot", action="store_true", help=f"only the {len(PILOT)} pilot items")
    g.add_argument("--ids", help="comma-separated item ids")
    g.add_argument("--category", help="one item category, e.g. Firearm")
    g.add_argument("--limit", type=int, default=0, help="at most this many items")
    g.add_argument("--force", action="store_true", help="redo versions that already have a mesh")
    g.add_argument("--retry-failed", action="store_true",
                   help=f"also try versions that failed {GIVE_UP} times in a row (out/mesh/attempts.json)")
    g.add_argument("--redescribe", action="store_true", help="ask for fresh visual descriptions")
    g.add_argument("--variants", type=int, default=VARIANTS, help="different versions (photo and mesh) per item")
    g.add_argument("--nas", default=os.environ.get("CLONEZ_NAS", "/nas"),
                   help="the NAS share, where the originals are kept (spark.sh mounts it at /nas)")
    g.add_argument("--no-nas", dest="nas", action="store_const", const="", help="don't keep the originals")
    g.add_argument("--style", choices=sorted(STYLES), default="realistic")
    g.add_argument("--res", choices=["512", "1024", "1024_cascade", "1536_cascade"], default="1536_cascade",
                   help="TRELLIS.2 resolution")
    g.add_argument("--max-tokens", type=int, default=49152,
                   help="a cascade's token budget (TRELLIS.2's own default): a shape with more surface than fits steps "
                        "its resolution down by 128 at a time, to 1024 at least. Flat things that fill the frame use "
                        "the most, and the time per step grows with its square")
    g.add_argument("--image-size", type=int, default=1536, help="reference photo size in pixels (square)")
    g.add_argument("--zimage-attention", default="flash", help="Z-Image-Turbo's attention backend (flash or native)")
    g.add_argument("--cond-size", type=int, default=1024,
                   help="long side of the cutout TRELLIS.2 is given (its image encoder works at 1024)")
    g.add_argument("--image-tries", type=int, default=3)
    g.add_argument("--min-picture-score", type=int, default=7)
    g.add_argument("--mesh-tries", type=int, default=2)
    g.add_argument("--min-mesh-score", type=int, default=5)
    g.add_argument("--master-verts", type=int, default=500000, help="decimation target of the master on the NAS")
    g.add_argument("--master-tex", type=int, default=4096, help="texture size of the master on the NAS")
    # The game copy's sizes are scaled to each item's real size (see budget); these cap them.
    g.add_argument("--near-verts", type=int, default=16000,
                   help="near LOD decimation target; it covers both of the remesher's skins, so about half is kept")
    g.add_argument("--far-verts", type=int, default=1500, help="far LOD decimation target (as --near-verts)")
    g.add_argument("--near-tex", type=int, default=1024, help="near LOD base colour size")
    g.add_argument("--normal-tex", type=int, default=1024, help="near LOD normal map size")
    g.add_argument("--mask-tex", type=int, default=512, help="near LOD metallic map size")
    g.add_argument("--far-tex", type=int, default=256, help="far LOD base colour size")
    g.add_argument("--stage-timeout", type=float, default=20,
                   help="minutes a step may run before the run counts as stuck: it logs the version as failed and "
                        "ends, and spark.sh starts it again (0: never; steps waiting on Ollama time out by themselves)")

    s = sub.add_parser("status", help="progress (standard library only)")
    s.add_argument("--root", default=str(ROOT_DEFAULT))
    s.add_argument("--variants", type=int, default=VARIANTS)

    r = sub.add_parser("refit", help="fit finished meshes again (after a fitting change) without generating anything")
    r.add_argument("--root", default=str(ROOT_DEFAULT))
    r.add_argument("--ids", help="comma-separated item ids or versions (id@2); default: every finished mesh")

    args = ap.parse_args()
    {"prepare": cmd_prepare, "generate": cmd_generate, "refit": cmd_refit, "status": cmd_status}[args.cmd](args)


if __name__ == "__main__":
    main()
