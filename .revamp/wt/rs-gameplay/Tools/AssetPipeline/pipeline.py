#!/usr/bin/env python3
"""CLONE-Z LLM asset pipeline: turns catalogue items into primitive ground models with a local LLM.

  python pipeline.py status
  python pipeline.py generate --workers 4 --category Clothing --limit 50
  python pipeline.py generate --ids Hammer Crowbar --force --critic
  python pipeline.py preview          # re-render previews + gallery for everything generated

Output: Assets/Resources/GeneratedItemModels/<id>.json, loaded in game by GeneratedItemModels.cs.
"""
import argparse
import fnmatch
import json
import os
import signal
import sys
import threading
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

import catalog
import geometry
import llm

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parent.parent
RESOURCES = PROJECT / "Assets" / "Resources"
OUT_DIR = RESOURCES / "GeneratedItemModels"
WORK = HERE / "out"
PREVIEWS = WORK / "previews"
LOG = WORK / "log.jsonl"

FORMAT_VERSION = 1

CATEGORY_HINTS = {
    "Clothing": "Lay the garment out flat and spread, as in a product photo, with its true outline.",
    "Weapon": "It lies on its side along X: handle or grip at -X, business end (head, blade, spike) at +X; "
              "show the business end clearly.",
    "Firearm": "It lies flat on its left side: muzzle at +X, stock or grip end at -X, top of the gun (sights) "
               "towards +Z, grip and magazine towards -Z, right side facing up; show stock, grip, trigger guard, "
               "magazine, sights.",
    "Food": "Show it as it would be found: its packaging, tin or container, or the food itself if loose.",
    "Drink": "Stand the bottle, can or carton upright.",
    "Ammo": "Show the box or a small pile of rounds / shells.",
    "Medical": "Show the packaging (box, bottle, blister pack, roll) as found in a cabinet.",
    "Material": "Show the material or part as it would be found lying around.",
}

_stop = threading.Event()
_log_lock = threading.Lock()
_out_lock = threading.Lock()
_out_files = None  # lower-cased id -> model file


def load_system_prompt():
    return (HERE / "prompts" / "system.md").read_text(encoding="utf-8")


def output_path(item_id):
    """The model file for an item. Ids that differ only by case (curated "gasmaskfilter", PZ "GasmaskFilter")
    share one file: the project lives on Windows, which cannot hold both, and Unity's Resources.Load
    does not tell them apart either."""
    global _out_files
    with _out_lock:
        if _out_files is None:
            _out_files = {p.stem.lower(): p for p in OUT_DIR.glob("*.json")} if OUT_DIR.exists() else {}
        return _out_files.setdefault(item_id.lower(), OUT_DIR / f"{item_id}.json")


def write_model(item, parts, base_height, meta):
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    body = {"version": FORMAT_VERSION, "id": item.id, "baseHeight": base_height, **meta, "parts": parts}
    # One part per line keeps the files diffable without bloating them.
    head = json.dumps({k: v for k, v in body.items() if k != "parts"}, ensure_ascii=False)[:-1]
    lines = ",\n".join("  " + json.dumps(p, ensure_ascii=False, separators=(",", ":")) for p in parts)
    tmp = output_path(item.id).with_suffix(".tmp")
    # LF on every OS, so files written on Windows match the ones generated on the Spark.
    with open(tmp, "w", encoding="utf-8", newline="\n") as f:
        f.write(f"{head}, \"parts\": [\n{lines}\n]}}\n")
    os.replace(tmp, output_path(item.id))


def log(entry):
    WORK.mkdir(parents=True, exist_ok=True)
    with _log_lock, open(LOG, "a", encoding="utf-8") as f:
        f.write(json.dumps(entry, ensure_ascii=False) + "\n")


def user_prompt(item):
    hint = CATEGORY_HINTS.get(item.category, "")
    return f"Model this item: {item.prompt_line()}\n{hint}".strip()


def generate_one(item, client, system_prompt, args):
    """LLM -> validate -> (optional) vision critique, with feedback loops. Returns a log entry."""
    t0 = time.time()
    messages = [{"role": "system", "content": system_prompt}, {"role": "user", "content": user_prompt(item)}]
    best = None  # (score, parts, notes)
    last_issues = []
    tokens = 0
    attempts = 0
    critic_rounds = 0

    while attempts < args.max_attempts and not _stop.is_set():
        attempts += 1
        try:
            out, raw, stats = client.chat_json(messages, llm.MODEL_SCHEMA, temperature=args.temperature)
            if not isinstance(out, dict):
                raise ValueError("the answer is not a JSON object")
        except Exception as e:  # malformed JSON or transport failure: just try again
            last_issues = [f"{type(e).__name__}: {str(e)[:160]}"]
            retry = f"Your last answer could not be used ({type(e).__name__}). Answer again with valid JSON."
            messages = messages[:2] + [{"role": "user", "content": retry}]
            continue
        tokens += stats["eval"]
        parts = geometry.sanitize(out.get("parts"))
        issues = geometry.check(parts)
        messages = messages[:2] + [{"role": "assistant", "content": raw}]

        if issues:
            last_issues = issues
            messages.append({"role": "user", "content": "Fix these problems and return the full corrected model:\n- "
                             + "\n- ".join(issues)})
            if best is None:
                best = (-1, parts, str(out.get("notes", "")))
            continue

        notes = str(out.get("notes", ""))
        if not args.critic:
            best = (0, parts, notes)
            break

        norm, _ = geometry.normalize(parts)
        try:
            score, problems = llm.critique(client, args.critic_model, geometry.render_sheet(norm), item.prompt_line())
        except Exception:
            # No review to act on, so no "revision" against an empty one: keep this model unless an
            # earlier one was actually scored.
            if best is None or best[0] <= 0:
                best = (0, parts, notes)
            break
        if best is None or score > best[0]:
            best = (score, parts, notes)
        if score < args.min_score and critic_rounds < args.critic_rounds and attempts < args.max_attempts:
            critic_rounds += 1
            messages.append({"role": "user", "content":
                             f"A reviewer looked at a render of your model and scored it {score}/10. "
                             f"Problems: {problems}\nImprove the model and return the full corrected model."})
            continue
        break

    entry = {"id": item.id, "category": item.category, "attempts": attempts, "tokens": tokens,
             "secs": round(time.time() - t0, 1)}
    if best is None or best[0] < 0:
        # Cut short by a stop request is not a failure: the item is simply still to do.
        entry["status"] = "stopped" if _stop.is_set() and attempts < args.max_attempts else "failed"
        if last_issues:
            entry["issues"] = last_issues[:3]
        return entry

    score, parts, notes = best
    norm, base_height = geometry.normalize(parts)
    meta = {"name": item.name, "model": client.model, "notes": notes[:400]}
    if args.critic and score > 0:
        meta["score"] = score
    write_model(item, norm, base_height, meta)
    if not args.no_preview:
        PREVIEWS.mkdir(parents=True, exist_ok=True)
        geometry.render_sheet(norm, f"{item.name}  [{item.id}]").save(PREVIEWS / f"{item.id}.png")
    entry.update(status="ok", parts=len(norm))
    if "score" in meta:
        entry["score"] = score
    return entry


def select_items(args):
    items = catalog.load_items(RESOURCES)
    skip = catalog.handmade_ids(PROJECT / "Assets" / "Scripts" / "World" / "ItemModels.cs")
    chosen = []
    seen = set()
    ids = set(args.ids or [])
    for it in items:
        if it.id in skip and not args.include_handmade:
            continue
        if ids and it.id not in ids:
            continue
        if args.category and it.category.lower() not in {c.lower() for c in args.category}:
            continue
        if args.match and not any(fnmatch.fnmatch(it.id.lower(), m.lower()) for m in args.match):
            continue
        if not args.force and output_path(it.id).exists():
            continue
        if it.id.lower() in seen:  # case-only twins share one file; generate it once
            continue
        seen.add(it.id.lower())
        chosen.append(it)
    if args.limit:
        chosen = chosen[: args.limit]
    return chosen, items, skip


def cmd_generate(args):
    client = llm.Ollama(args.host, args.model, think=args.think, num_ctx=args.num_ctx)
    try:
        available = client.ping()
    except Exception as e:
        sys.exit(f"Cannot reach Ollama at {args.host}: {e}")
    for m in [args.model] + ([args.critic_model] if args.critic else []):
        if m not in available:
            sys.exit(f"Model '{m}' is not pulled on {args.host}. Available: {', '.join(available)}")

    todo, _, _ = select_items(args)
    if not todo:
        print("Nothing to do (everything selected already has a model; use --force to redo).")
        return
    print(f"Generating {len(todo)} models with {args.model} on {args.host} using {args.workers} workers"
          f"{' + critic ' + args.critic_model if args.critic else ''}.", flush=True)

    system_prompt = load_system_prompt()
    signal.signal(signal.SIGINT, lambda *_: (_stop.set(), print("\nStopping after in-flight items...", flush=True)))

    done = ok = 0
    cancelled = False
    t0 = time.time()
    with ThreadPoolExecutor(args.workers) as pool:
        futures = {pool.submit(generate_one, it, client, system_prompt, args): it for it in todo}
        for fut in as_completed(futures):
            it = futures[fut]
            if fut.cancelled():
                continue
            try:
                entry = fut.result()
            except Exception as e:
                entry = {"id": it.id, "category": it.category, "status": "error", "error": str(e)[:300]}
            entry["ts"] = time.strftime("%Y-%m-%dT%H:%M:%S")
            log(entry)
            done += 1
            ok += entry.get("status") == "ok"
            rate = (time.time() - t0) / done
            eta = rate * (len(todo) - done)
            extra = f" score {entry['score']}" if entry.get("score") is not None else ""
            print(f"[{done}/{len(todo)}] {entry.get('status'):6} {it.id:<40} {entry.get('parts', '-')} parts,"
                  f" {entry.get('secs', '-')}s{extra} | {rate:.1f}s/item, ETA {eta / 3600:.1f}h", flush=True)
            if _stop.is_set() and not cancelled:
                cancelled = True
                for f in futures:
                    f.cancel()
    print(f"Done: {ok}/{done} succeeded in {(time.time() - t0) / 60:.1f} min. Log: {LOG}")
    if not args.no_preview:
        build_gallery()


def build_gallery():
    """A static HTML contact sheet of every preview, for eyeballing hundreds of models quickly."""
    if not PREVIEWS.exists():
        return
    cards = []
    for png in sorted(PREVIEWS.glob("*.png")):
        cards.append(f'<figure><img loading="lazy" src="previews/{png.name}"><figcaption>{png.stem}</figcaption></figure>')
    html = ("<!doctype html><meta charset=utf-8><title>CLONE-Z generated models</title><style>"
            "body{background:#1e1f24;color:#ddd;font:13px sans-serif;margin:12px}"
            "main{display:grid;grid-template-columns:repeat(auto-fill,minmax(420px,1fr));gap:10px}"
            "figure{margin:0}img{width:100%}input{width:320px;padding:4px;margin-bottom:10px}</style>"
            f"<h3>{len(cards)} generated models</h3>"
            "<input placeholder='filter by id' oninput=\"for(const f of document.querySelectorAll('figure'))"
            "f.style.display=f.textContent.toLowerCase().includes(this.value.toLowerCase())?'':'none'\">"
            "<main>" + "".join(cards) + "</main>")
    (WORK / "index.html").write_text(html, encoding="utf-8")
    print(f"Gallery: {WORK / 'index.html'}")


def cmd_preview(args):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    files = sorted(OUT_DIR.glob("*.json")) if OUT_DIR.exists() else []
    for i, f in enumerate(files, 1):
        d = json.loads(f.read_text(encoding="utf-8"))
        geometry.render_sheet(d["parts"], f"{d.get('name', d['id'])}  [{d['id']}]").save(PREVIEWS / f"{d['id']}.png")
        if i % 100 == 0:
            print(f"{i}/{len(files)}", flush=True)
    build_gallery()


def cmd_status(args):
    items = catalog.load_items(RESOURCES)
    skip = catalog.handmade_ids(PROJECT / "Assets" / "Scripts" / "World" / "ItemModels.cs")
    rows = {}
    for it in items:
        r = rows.setdefault(it.category, [0, 0, 0])
        r[0] += 1
        if it.id in skip:
            r[1] += 1
        elif output_path(it.id).exists():
            r[2] += 1
    print(f"{'category':<10} {'items':>6} {'handmade':>9} {'generated':>10} {'remaining':>10}")
    tot = [0, 0, 0]
    for cat, (n, h, g) in sorted(rows.items()):
        print(f"{cat:<10} {n:>6} {h:>9} {g:>10} {n - h - g:>10}")
        tot = [tot[0] + n, tot[1] + h, tot[2] + g]
    print(f"{'TOTAL':<10} {tot[0]:>6} {tot[1]:>9} {tot[2]:>10} {tot[0] - tot[1] - tot[2]:>10}")
    if LOG.exists():
        failed = {}
        for line in LOG.read_text(encoding="utf-8").splitlines():
            try:
                e = json.loads(line)
            except ValueError:
                continue  # a line cut short by a crash
            if e.get("status") == "ok":
                failed.pop(e["id"], None)
            elif e.get("status") != "stopped":
                failed[e["id"]] = e.get("status")
        failed = {k: v for k, v in failed.items() if not output_path(k).exists()}
        if failed:
            print(f"\n{len(failed)} items failed last time: {', '.join(sorted(failed)[:20])}"
                  f"{' ...' if len(failed) > 20 else ''}")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)

    g = sub.add_parser("generate", help="generate models with the LLM")
    g.add_argument("--host", default=os.environ.get("OLLAMA_HOST_URL", "http://192.168.1.252:11434"))
    g.add_argument("--model", default="gpt-oss:120b")
    g.add_argument("--think", default="medium", choices=["low", "medium", "high"])
    g.add_argument("--num-ctx", type=int, default=16384, help="keep constant between runs to avoid model reloads")
    g.add_argument("--temperature", type=float, default=0.5)
    g.add_argument("--workers", type=int, default=4, help="match OLLAMA_NUM_PARALLEL on the server")
    g.add_argument("--ids", nargs="+", help="only these item ids")
    g.add_argument("--category", nargs="+", help="only these categories (Clothing, Weapon, Food, ...)")
    g.add_argument("--match", nargs="+", help="id glob(s), e.g. 'Dress_*' 'Skirt_*'")
    g.add_argument("--limit", type=int, default=0)
    g.add_argument("--force", action="store_true", help="regenerate items that already have a model")
    g.add_argument("--include-handmade", action="store_true", help="also generate items with a C# model")
    g.add_argument("--max-attempts", type=int, default=4, help="LLM calls per item, incl. fix-ups")
    g.add_argument("--critic", action="store_true", help="score renders with a vision model and refine")
    g.add_argument("--critic-model", default="gemma3:12b")
    g.add_argument("--critic-rounds", type=int, default=2)
    g.add_argument("--min-score", type=int, default=6)
    g.add_argument("--no-preview", action="store_true")
    g.set_defaults(func=cmd_generate)

    p = sub.add_parser("preview", help="re-render previews and the HTML gallery")
    p.set_defaults(func=cmd_preview)
    s = sub.add_parser("status", help="progress by category")
    s.set_defaults(func=cmd_status)

    args = ap.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
