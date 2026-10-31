# WithTheDead LLM asset pipeline

Generates ground models for the item catalogue with a local LLM on the DGX Spark (`spark-f146`,
Ollama, `gpt-oss:120b`). The output is WithTheDead's own model format: a list of coloured cubes,
spheres and cylinders. That way generated models bake and draw instanced exactly like the
hand-built ones in `ItemModels.cs`, and nothing has to be imported.

```
item catalogue ─► gpt-oss:120b ─► sanitise + geometry checks ─► render ─► gemma3:12b critic ─► JSON
(Resources/*.txt)  (structured JSON)   (scale, floating parts)    (preview)  (score, fix-up loop)   │
                                                                                                    ▼
                                  Assets/Resources/GeneratedItemModels/<id>.json ─► GeneratedItemModels.cs
```

In game, `ItemMeshBaker.Get` resolves a model as **generated textured mesh (`mesh/`) → hand-built C# →
generated JSON → tinted fallback primitive**.

## One-click app

Double-click **`AssetPipeline.bat`** for a menu, or run `.\AssetPipeline.ps1 <action>`:

| Action    | What it does |
|-----------|--------------|
| `start`   | Pushes the pipeline and catalogue to the Spark, then starts or resumes generation in tmux session `clonez`. Add `-Critic` for the vision critic, `-Workers N`, or `-Extra '--category','Clothing'`. |
| `status`  | Shows whether it is running, the latest progress, and counts per category. |
| `watch`   | Shows the live log. Ctrl+C stops watching only; the run keeps going. |
| `pull`    | Copies finished models into `Assets/Resources/GeneratedItemModels` and previews into `out/`. Local files that are newer than the Spark's copies (hand fixes) are kept. |
| `gallery` | Opens `out/index.html`, a filterable contact sheet of every preview. |
| `stop`    | Stops gracefully: it finishes the items in flight, then exits. Starting again resumes. |
| `mesh-*`  | The mesh stage: `mesh-build`, `mesh-prepare`, `mesh-pilot`, `mesh-start`, `mesh-status`, `mesh-watch`, `mesh-pull`, `mesh-gallery`, `mesh-stop` (menu: **M**). See [mesh/README.md](mesh/README.md). |

The run lives on the Spark, so this PC can sleep or reboot. Items that already have a JSON file are
skipped, so `start` always resumes where it left off.

To attach to the session directly: `ssh henrylawrencecahill@spark-f146 -t tmux attach -t clonez`
(detach with Ctrl+B then D).

## Running the CLI directly

```
pip install numpy pillow
python pipeline.py status
python pipeline.py generate --workers 4 --critic --category Clothing --limit 50
python pipeline.py generate --ids Hammer Crowbar --force     # redo specific items
python pipeline.py generate --match "Dress_*" "Skirt_*"
python pipeline.py preview                                   # re-render previews + gallery
```

`--host` defaults to `http://192.168.1.252:11434`. You can override it with the
`OLLAMA_HOST_URL` environment variable.

## Spark / Ollama setup

`/etc/systemd/system/ollama.service.d/override.conf` on the Spark:

```
Environment="OLLAMA_NUM_PARALLEL=4"      # 4 concurrent generations ≈ 2x throughput
Environment="OLLAMA_KEEP_ALIVE=2h"       # keep the 64 GB model resident between items
Environment="OLLAMA_FLASH_ATTENTION=1"
Environment="OLLAMA_MAX_LOADED_MODELS=2" # gpt-oss + gemma3 critic side by side
```

Keep `--workers` equal to `OLLAMA_NUM_PARALLEL`. Keep `--num-ctx` constant between runs, because a
different context size makes Ollama reload the model.

Measured throughput: about 75 s per item with one worker, and about 35–45 s per item with 4 workers
(41 s with the critic on). The full catalogue of about 3,600 items takes roughly 40 hours.

## Tuning quality

- `prompts/system.md` holds the modelling rules and four worked examples (an upright can, a tool
  lying on its side, a flat garment, and a gun lying on its left side). Better examples are the
  biggest quality lever.
- `CATEGORY_HINTS` in `pipeline.py` adds per-category pose guidance.
- `geometry.check` rejects models that are wildly out of scale or have floating parts, and sends
  the problems back to the LLM. A separate piece resting on the floor that is at least 40% the size
  of the main body counts as an object of its own (the other shoe of a pair, loose rounds beside
  their box, the next book of a set), not as a floating part.
- With `--critic`, the vision model scores each render from 1 to 10. Anything below `--min-score`
  (default 6) goes back to the LLM with the reviewer's notes. The score is saved in each JSON file,
  so weak models are easy to find and redo with `--ids ... --force`. If the reviewer itself fails,
  the model is kept unscored instead of being revised against an empty review.
- An item that still has problems after `--max-attempts` is logged as `failed` in `out/log.jsonl`
  together with its last problems, so failures can be diagnosed. Items cut short by `stop` are
  logged as `stopped` and simply stay on the to-do list.

## File format

```json
{"version": 1, "id": "Hammer", "baseHeight": 0.08, "name": "...", "model": "...", "notes": "...", "score": 7,
 "parts": [{"name": "Handle", "shape": "cylinder", "pos": [x,y,z], "rot": [x,y,z], "size": [x,y,z],
            "color": [r,g,b], "smooth": 0.15, "metal": 0}]}
```

- `size` is in real metres for every shape; a cylinder's size is `[diameter, length, diameter]`.
- `rot` is Unity Euler degrees.
- The model is centred on its origin, and `baseHeight` is the hover height of that centre.
- Ids that differ only by case (curated `gasmaskfilter`, PZ `GasmaskFilter`) share one file, because
  Windows cannot hold both and Unity's `Resources.Load` does not tell them apart.

### Pose conventions

`HumanoidRig.SetHeldItem` puts an item's ground model straight into a character's hand, so long
items must keep one orientation:

- Tools and melee weapons lie along X with the handle at -X and the business end at +X.
- Guns lie on their left side: muzzle at +X, top of the gun (sights) towards +Z, grip and magazine
  towards -Z, thickness along Y. This matches the hand-built pistol in `ItemModels.cs`.

## Next stage: real meshes

The mesh stage in [mesh/](mesh/README.md) turns every item into a textured mesh on the Spark's GPU:
Z-Image-Turbo paints a reference picture, TRELLIS.2 turns it into a mesh, and the mesh is fitted onto the
item's primitive model from this stage, so it keeps the size and pose conventions above.
