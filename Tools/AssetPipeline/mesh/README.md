# Item meshes (TRELLIS.2 on the DGX Spark)

Gives every item three different textured 3D versions, generated entirely on the Spark's NVIDIA GB10. There is no
cloud service, no account and no token. The only time anything comes from outside is `prepare`, which makes a one-time
anonymous download of the pinned open model weights.

```
item catalogue ─► gemma3:12b ─► Z-Image-Turbo ─► gemma3:12b ─► BiRefNet ─► TRELLIS.2 ─► fit onto the item's ─► game copy
(Resources/*.txt)  (3 different   (1536 px photo   (rejects bad   (cutout at   (1536,       primitive model;     + originals
                    versions)      per version)     photos)        full size)   textured)    normal map baked     on the NAS
                                                                                             from the full mesh
   Assets/Resources/GeneratedItemMeshes/<id>@<n>.bytes + _tex0/_tex1/_nrm0/_msk0 ─► GeneratedItemMeshes.cs
   \\192.168.1.170\CLONE-Z\assets\item-meshes\<id>\v<n>\ (photos, cutout, full-detail GLB, meta.json)
```

In game, `ItemMeshBaker.Get` uses an item's generated meshes in place of any primitive model, hand-built ones
included. Items without them keep their primitive model. A dropped item shows one of its versions, picked from where
it lies (so every client and every reload agree); a character holding one keeps one version. The near level of detail
has a normal map baked from TRELLIS.2's full-detail mesh (~2 million triangles) and URP's metallic map, so the ~10,000
triangles the game draws carry the detail of the original. The fit keeps the old model's size, resting pose and axes,
so ground items, the bob and held items work as before.

## Models (all local)

| Step | Model | Licence |
|------|-------|---------|
| The three versions' descriptions, photo and mesh critic | `gemma3:12b` in the Spark's Ollama | Gemma terms |
| Reference photos | `Tongyi-MAI/Z-Image-Turbo` (flash attention) | Apache-2.0 |
| Background removal | `ZhengPeng7/BiRefNet` | MIT |
| Photo → textured mesh | `microsoft/TRELLIS.2-4B` (+ one decoder from `TRELLIS-image-large`) | MIT |
| TRELLIS.2's image encoder | DINOv3 ViT-L/16 from `timm/vit_large_patch16_dinov3.lvd1689m` | Meta DINOv3 licence |

TRELLIS.2's defaults are two gated models: `facebook/dinov3-*`, which needs an account, and `briaai/RMBG-2.0`, which
is also non-commercial. `prepare` swaps them out:
- It converts timm's ungated copy of the same DINOv3 weights to the `transformers` format TRELLIS.2 loads, and
  checks that both give the same features (they match to 1e-6). It refuses to continue if they differ.
- It uses BiRefNet in place of RMBG-2.0.

These choices go in `pipeline.clonez.json` beside the downloaded TRELLIS.2 files, which stay untouched. Every
download is pinned to a revision in `meshgen.py` (`PINS`).

PyTorch's own fast attention kernels don't support the GB10 (sm_121), so by default Z-Image-Turbo falls back to the
slow reference path. The image's flash-attn build does run: a 1536 px photo takes about 31 s instead of 203 s.

## What each version costs

Measured at the defaults (photos at 1536 px, TRELLIS.2 at `1536_cascade`):

| Item | Time per version | Why |
|------|------------------|-----|
| A rifle, glasses (thin, intricate) | ~2.5 min | Few surface voxels: TRELLIS.2 samples in ~30 s |
| A cap, a badge (bulky or broad) | ~7-11 min | Many surface voxels: sampling takes 4-6 min (time per step grows with the square of the voxels) |
| A T-shirt, boots (garments) | ~9-16 min | Two meshes: laid out and as worn |

Each photo takes ~35 s with its checks; the full-detail master (500k triangles, 4096 px textures) 40-80 s; the game
copy, fit, previews and mesh critic ~20 s. Across the catalogue (3,639 items × 3 versions, plus 1,053 worn garment
meshes) that is roughly 1,000 hours of GPU time: **about 6 weeks** running around the clock.

`--res 1024_cascade` roughly halves that (**about 3 weeks**): on the cap, sampling took 88 s instead of 244 s, and
peak memory 22 GB instead of 30 GB. The cost is detail on intricate hard surfaces: on the rifle, 1536 kept the holes
in the handguard and the shape of the muzzle device, which 1024 smoothed over; on the cap the difference was slight.
`--max-tokens 32768` is a middle way that only lowers the resolution of the bulkiest items. Whatever is chosen can be
changed at any time (the next start uses it), and the NAS keeps every original to remake a version from later.

Bigger photos don't change the mesh: TRELLIS.2's image encoder works at 1024 px, and the cutout gives it the item at
full resolution within that. The 1536 px photos are kept on the NAS as the originals.

## The photos

Each version's photo is painted from the item's name, one of its three descriptions, the style line and a shot
(`Generator.make`):
- The descriptions describe one of the item alone: the first test's aviator glasses came with a leather case and its
  sugar packet was "stacked four high", both of which went into the photo and would have gone into the mesh. An item
  sold packaged is described as its package.
- The shot is a three-quarter product photo, with these exceptions. Flat things with a broad face (773 items: badges,
  plates, packets, leathers) are shot lying flat from a low angle that shows their edge: face on, TRELLIS.2 can't
  tell how thick they are and made the test badge a cube. Tops and legwear are laid out flat, as their models lie.
  Rigid worn things are shown worn (below).
- The critic scores each photo 1-10, and plain yes-or-no questions catch what that score lets through: whether it
  shows several loose copies of the item (a stack of sugar packets scored 9; a tin of nails is asked a second
  question, whether the repeated things are inside a container, and passes), and for worn photos whether any part of
  a person shows. A photo that fails either scores 2. Below 7, another photo is painted (three at most, then the best
  is used, and its score is in the log).

## Worn items

Items worn on the body get photos that show them worn, with nothing of the wearer (`WORN` in `meshgen.py`):
- **Rigid things** (hats, helmets, glasses, masks, goggles, backpacks, chest rigs, thigh bags, body armour): each
  version's one photo shows it worn, seen from where `WORN` says (in front, behind or to the right of the wearer).
  The same mesh lies on the floor and goes on the body; `meta.json` records the camera side, which tells the fitting
  step which side of the mesh faces where.
- **Garments** (tops, legwear, footwear, gloves): the ground version is photographed laid out (as the primitive
  models lie), and a second photo and mesh show the garment worn, "ghost mannequin" style: hollow, holding the shape
  it has when worn, sleeves and gloves pointing down as in the game's bind pose. That mesh goes to the NAS only
  (`worn/` in the version's folder), for the step that fits garments to the body.

The worn wording never mentions a person. Z-Image-Turbo has no negative prompt, so "worn by an invisible person, no
people" painted people: in a test of 9 garments, a man in the T-shirt, a woman in the dress, legs in the socks and
hands in the gloves (11 photos of 18), and none with the current wording. The yes-or-no person question (above) backs
it up. A garment whose worn photos all show someone gets no worn mesh (`"skipped"` in its log entry) rather than one
with a body inside.

Fitting worn things onto the body (placing rigid ones on their bones, and shaping and skinning garments to the body)
is a separate step that reads the NAS; it can be redone and improved without generating anything again. The run
does plain items first, then rigid worn ones, then garments.

## Running it

Everything is in **AssetPipeline.bat → M** (or `.\AssetPipeline.ps1 mesh-<action>`):

| Action | What it does |
|--------|--------------|
| `mesh-build` | Builds the `clonez-mesh:1` Docker image on the Spark (once; about an hour, mostly compiling flash-attn). |
| `mesh-prepare` | Downloads the weights (~50 GB) into `~/clonez-models` and converts DINOv3 (once). |
| `mesh-pilot` | Makes the 24 pilot items (`PILOT` in `meshgen.py`), a spread of categories, to judge the look. |
| `mesh-start` | Starts or resumes every item. Add `-Extra '--category','Firearm'`, `'--ids','Axe,Apple'`, `'--force'`... |
| `mesh-view` | The live view in the browser: the item and version being made, its photo and the critic's score, the step it is on, the GPU, and the latest meshes. |
| `mesh-status` | Running or not, the latest log lines, counts, time left and critic scores. |
| `mesh-watch` | The raw log (Ctrl+C stops watching only). |
| `mesh-pull` | Copies finished meshes into `Assets/Resources/GeneratedItemMeshes` and the gallery into `out/mesh/`. |
| `mesh-gallery` | Opens `out/mesh/index.html`. Each tile shows the reference photo and the mesh as the game will draw it, normal map included. |
| `mesh-stop` | Finishes the version in progress, then stops. It stays stopped (the schedule doesn't restart it) until `mesh-start`, which resumes. |
| `mesh-refit` | Fits the finished meshes onto their primitive models again and redraws their gallery tiles, without generating anything (for after a change to the fitting; `-Extra '--ids','Axe,Apple'` for some only). Beside a run it only takes named ids. |

The live view and gallery are served from the Spark on port 8765 (`http://192.168.1.252:8765/live.html`). The run
lives in tmux session `clonez-mesh` on the Spark, so this PC can sleep. On the Spark itself the same commands are
`bash ~/clonez-pipeline/Tools/AssetPipeline/mesh/spark.sh build|prepare|start|stop|status|refit|web|hold|release|schedule`.

### Around the clock

The run has the Spark to itself, day and night: its models and the night shift's don't fit in memory together, and
at its peak (a broad, flat item at 1536) it uses about 116 GB of the Spark's 124 GB. Stop it before loading anything
big beside it, another model in Ollama say.
- `spark.sh schedule` puts **keep-running** in the crontab, every 10 minutes: if the run isn't going, it starts it
  with the arguments of the last `start`, so a reboot, or a crash the run gave up restarting from, costs 10 minutes
  at most. It leaves the run alone after a `stop` (until the next `start`), while it is held, and once a start has
  found nothing left to make. `spark.sh unschedule` removes it; `schedule.log` in `out/mesh` says what it did, and
  `status` says which of these applies.
- `start` refuses while the night shift is running, or while another mesh-stage job (a refit, a test) has the GPU;
  with the schedule in place, the run then starts by itself once that has finished.
- While the run is going it touches `Tools/NightShift/.state/mesh-run.lock` in the AI Workbench project every
  minute. `nightshift.sh start` and `NightShift.ps1 start` won't start the night shift while that lock is fresher
  than ten minutes, so a crashed run can't block it for long. To give the night shift the Spark, stop the meshes
  first (`mesh-stop`), and start them again when it is done.
- `spark.sh hold "reason"` stops `start` and `refit` (the schedule included) until `spark.sh release`, for when the
  pipeline is being changed.
- A run that crashes (exits with an error, the kernel's out-of-memory killer, say) starts again after a minute, up to
  five times in a row, then keep-running takes over; a `stop` or a hold never restarts. Each version's tries are
  counted in `attempts.json` before it starts, so the one that took the run down is given up on after its second
  crash instead of stopping every run.
- A step that runs past `--stage-timeout` (20 minutes) counts as stuck: native code that never returns, which no
  signal reaches (xatlas's UV chart search once spun on one mesh's master bake for good, GPU idle). The run writes
  where every thread was to `run.log`, logs the version as failed, and ends with exit code 4, which restarts it like a
  crash; a version stuck twice is given up on. Steps that wait on Ollama (describing, judging) have no limit, as
  Ollama's own timeouts end them. A hang that freezes Python as well is ended five minutes later by faulthandler.

### Hand-built items

The 94 items with a C# model in `ItemModels.cs` have no JSON for the mesh to be fitted onto. Run **CloneZ → Asset
Pipeline → Export Hand-Built Item Models** in Unity once (and again after changing one). It writes them to
`out/handbuilt/`, and the next `mesh-start` uploads them. Until then those items are skipped, and the log says so.

## The originals on the NAS

`\\192.168.1.170\CLONE-Z\assets\item-meshes\<id>\v<n>\` (`/mnt/clonez/...` on the Spark), about 30 MB per version:

| File | What |
|------|------|
| `photo.png` | The reference photo the mesh was made from, at full size (1536 px) |
| `candidates/<n>.png` | Every photo painted for this version, the rejected ones included |
| `cutout.png` | What TRELLIS.2 was given: the item cut out of the photo and cropped |
| `master.glb` | TRELLIS.2's mesh at full detail (~500k triangles) with 4096 px PBR textures, in TRELLIS.2's own frame (glTF, Y up) |
| `worn/` | For a garment, the same four for its photo and mesh as worn |
| `meta.json` | The description, prompts, seeds, scores, the placement of the game copy, the worn camera side, and the models used |

Each version's folder is written under a temporary name and renamed when complete, as the share's README asks.

## How the fit works

TRELLIS.2 returns a mesh in a unit box. It comes out upright, but may lie at any angle about the vertical: a bat
photographed at an angle can come out diagonal. `meshpost.fit` lays it onto the item's primitive model, which already
holds everything the game expects:
- the item's real size in metres
- how it rests on the floor
- for held items, which end is the handle (−X) and which way a gun's top faces (+Z); see "Pose conventions" in
  `../README.md`

The mesh is first squared up. Besides the mesh as generated, the fit tries:
- its footprint turned about the vertical so its long side runs along X, by its principal axis
- the same, by its smallest bounding rectangle
- its smallest 3D box squared to the axes

In each of those frames, of the 24 ways of turning the mesh square onto the model's axes, those whose proportions
match the model's (long side long, thin side thin) are scaled to its extents, and the one whose voxelised volume
overlaps the model's best wins. Turns within 0.03 of the best count as equal: then the one that keeps the mesh's up
direction up wins, then the one that turns its front up (a badge or a towel face up). A squared-up frame has to beat
the mesh as generated by a small margin, so a mesh that was already square isn't nudged a degree or two by its own
asymmetry. The overlap is logged as `fit` (1.0 = same shape); a low value means the photo and the old model disagree
about the item's shape, so look at that tile in the gallery.

The remesher in TRELLIS.2's export wraps every solid part in two skins, an outer one and an inner one facing in.
`drop_inner_shells` removes the inner ones (half the triangles, never seen), and the previews cull back faces as the
game does.

## Tuning

| Flag (`-Extra`) | Default | Effect |
|-----------------|---------|--------|
| `--variants` | `3` | Versions per item (at most 8, as `GeneratedItemMeshes.MaxVariants`). |
| `--res` | `1536_cascade` | TRELLIS.2 resolution: `512`, `1024`, `1024_cascade` or `1536_cascade`. |
| `--max-tokens` | `49152` | The cascade's token budget (TRELLIS.2's default). A shape with more surface than fits is made at a lower resolution, 128 at a time, down to 1024. Time per step grows with the square of the tokens, so bulky and flat things (boxes, caps, a badge filling the frame) cost several times a rifle. |
| `--image-size` | `1536` | Reference photo size. |
| `--style` | `realistic` | `realistic` (used, worn, muted), `stylized` or `lowpoly`; it steers the photo, which TRELLIS.2 copies. |
| `--near-verts` / `--far-verts` | 16000 / 1500 | Caps on the two levels' decimation targets (about half is kept once the inner skins go). |
| `--near-tex` / `--normal-tex` / `--mask-tex` / `--far-tex` | 1024 / 1024 / 512 / 256 | Caps on the game copy's map sizes. |
| `--master-verts` / `--master-tex` | 500000 / 4096 | The full-detail master on the NAS. |
| `--image-tries`, `--min-picture-score` | 3, 7 | Photos per attempt, and the critic score that ends the search early. |
| `--mesh-tries`, `--min-mesh-score` | 2, 5 | Whole attempts (new photo, new mesh) while the mesh critic scores below the minimum. |
| `--no-nas` | off | Don't keep the originals (and skip garments' worn meshes). |
| `--retry-failed` | off | Also try the versions given up on: one that fails, or takes the whole run down, twice in a row is skipped after that (`out/mesh/attempts.json`; `status` lists them). |
| `--stage-timeout` | `20` | Minutes one step may run before the run counts as stuck and restarts (see "Around the clock"); `0` turns the watchdog off. |
| `--redescribe` | off | Ask for fresh descriptions. They are cached in `out/mesh/descriptions.json` (three per item), where you can also edit one by hand. |

The prompt is built in `Generator.make`: the item's name, the version's description, the style line, and the shot.

The game copy's sizes follow the item's real size (`budget` in `meshgen.py`, about 1,000 texels per metre): items over
0.5 m get 1024 px maps and a 16,000-vertex target, items over 0.25 m 512 px and 8,000, smaller ones 256 px and 4,000;
the metallic map is a quarter of the colour map's size. Vertices are stored compactly (18 bytes: positions to within
0.01 mm, normals to 0.005°). Measured versions came to ~1.4 MB at 1024 px, ~0.43 MB at 512 and ~0.15 MB at 256,
which puts all three versions of the catalogue at about 7 GB in Git LFS, most of it normal maps, which don't compress
well without visibly shifting the normals. GitHub Pro includes 10 GiB of LFS
storage and 10 GiB of downloads a month; every pushed change to a file counts again, so refit before committing, not
after. The masters on the NAS keep the full detail, so bigger copies can be baked from them later.

## Files

- `Dockerfile`: the image. The CUDA, PyTorch and Triton pins (and why) follow
  [mmanco/spark-trellis2-docker](https://github.com/mmanco/spark-trellis2-docker). Kernels are compiled for sm_120,
  which runs on the GB10's sm_121.
- `spark.sh`: build, prepare, start, stop, refit, status, the live view, hold, and keeping the run going around the
  clock (schedule, keep-running) on the Spark.
- `meshgen.py`: `prepare`, `generate`, `refit` and `status` (`status` needs only the standard library).
- `meshpost.py`: the conversion to Unity's frame, the fit, the normal and metallic maps, the `.bytes` writer and reader
  (the format is documented at the top), the NAS master export, and the nvdiffrast previews.
- `live.html`: the live view.
- Outputs on the Spark under `~/clonez-pipeline`:
  - `Assets/Resources/GeneratedItemMeshes/`
  - `Tools/AssetPipeline/out/mesh/`: `log.jsonl`, `run.log`, `schedule.log`, `previews/`, `refs/` (the photos at
    1024 px, not pulled), `descriptions.json`, `status.json`, `index.html` and `live.html`

In Unity, `Assets/Editor/GeneratedItemMeshImport.cs` sets the maps' import settings and makes
`Resources/ItemMeshes/Template.mat`, the material the near levels are cloned from, which keeps URP's normal-map and
metallic-map shader variants in builds. The `.bytes` files are tracked by Git LFS (see `.gitattributes`), as are the
`.jpg` and `.png` maps.
