# Night Shift

Local AI coding agents that work through [tasks.md](tasks.md) overnight, so CLONE-Z gets built while you
sleep without spending cloud AI tokens. The model runs in Ollama on the DGX Spark (`qwen3.8` by default),
[OpenCode](https://opencode.ai) is the agent, and everything runs on your own machines:

- **The Spark** runs the model and the coordinator, inside the AI Workbench project container. In AI
  Workbench it is the **night-shift** toggle under *Applications*.
- **Helper machines** (optional) run extra agents in parallel. They use the Spark's model over the LAN and
  do their own compile checks, so more tasks get done per night.
- **This PC** only sets things up and fetches the results. It can sleep while the night shift works.

Every change is compiled against Unity's own assemblies before it lands, and lands on its own branch
(`nightshift/<date>`), never on `main`.

## One-time setup

1. **Unity references.** The Spark has no Unity editor, so the night shift compiles against the DLLs from
   this PC's Unity install. Double-click [NightShift.bat](NightShift.bat) and choose **8**, or run
   `.\NightShift.ps1 refs`. Repeat this after upgrading Unity or changing packages.
2. **Container.** In AI Workbench, open *Henry-Cahill-CLONE-Z* on *spark-f146* and build the environment
   when it asks. The build installs OpenCode, the .NET SDK and ripgrep (see `postBuild.bash` and `apt.txt`).
3. **Helpers (optional).** Choose **9** in the menu, or run
   `.\NightShift.ps1 helpers -Helpers htsadmin@192.168.1.200, htsadmin@192.168.1.201`. This installs
   OpenCode and .NET in the helper user's home (no sudo), lets the Spark's coordinator in with its own SSH
   key (accepted only from the Spark's address) and copies the Unity references. Helper agents run at
   low CPU priority, so game servers on the same machines aren't affected.

## Every night

1. **Queue tasks** in [tasks.md](tasks.md), one `- [ ]` line each. Edit it in JupyterLab (*Open
   JupyterLab* in AI Workbench, then `Tools/NightShift/tasks.md`), or choose **1** in the menu. Small,
   concrete tasks work best: name the place and say what you want to see in game.
2. **Start it.** Switch on **night-shift** under *Applications* in AI Workbench, or choose **2**. It stops
   taking new tasks at 07:00 and stops on its own when the queue is empty.
3. **Check on it** any time with **3** (status) or **4** (live log). New tasks added during the night
   are picked up.

## In the morning

- `tasks.md` on the night's branch is ticked off: `[x]` landed, `[!]` failed (the attempt is saved as a
  patch), `[~]` nothing needed changing. `reports/<date>.md` has a summary of each task.
- Choose **7** (or `.\NightShift.ps1 fetch`) to fetch the branch into this repo, then
  `git switch -c nightshift/<date> spark/nightshift/<date>` and try it in Unity. The night shift can
  compile but not play the game, so play-test before merging.
- To review it as a pull request, press **Push** in AI Workbench (the night's branch is checked out
  there) or push it from this PC. Switch the Workbench project back to your normal branch afterwards.

## How it works

`nightshift.py run` (started by `nightshift.sh start`) is the coordinator:

1. It switches the Workbench project to `nightshift/<date>`, commits the queue and refreshes
   `memory/PROJECT_MAP.md`, and checks that the project compiles before anything changes.
2. Each worker (the Spark itself in `/tmp`, and every helper in `~/clonez-nightshift`) takes the next
   unchecked task and gets the latest commit. It runs `opencode run` with the local model, then runs
   [check.sh](check.sh) and gives the agent up to two rounds to fix compile errors before committing.
3. The coordinator lands each result on the night's branch one at a time, compiles everything again and
   ticks the task off. If two tasks collide, the later one goes back in the queue once to be redone on
   top of the other.

The agent's **memory** is in [memory/](memory). `LESSONS.md` holds hand-written notes on how CLONE-Z is
built, plus one-line lessons the agents add after each task. `PROJECT_MAP.md` lists every script and what
it is for, so the agent finds code without reading the whole project, which saves time per task.

**Guard rails.** The agent can't commit, push or switch branches. Changes to `ProjectSettings/`,
`Packages/`, scenes, `.meta` files, generated item models and the night shift's own files are rolled
back. `.meta` files for new scripts are generated so the branch imports cleanly. It runs in the Workbench
container on the Spark and as the helper user on the helpers.

## Settings

[nightshift.env](nightshift.env) sets the model (`gpt-oss:120b` is about twice as fast as `qwen3.8`),
the stop time, the time limit per task and the number of fix rounds. It's read at every start.

Inside the container, `bash Tools/NightShift/nightshift.sh` also has `finish` (stop after the current
tasks), `stop`, `check` (compile the project as it is) and `chat` (OpenCode in a terminal with the same
local model, for interactive work in JupyterLab's terminal).

## Troubleshooting

- **"Unity's reference assemblies aren't on the Spark"**: run `.\NightShift.ps1 refs` on this PC.
- **"the project doesn't compile before any changes"**: the branch the Workbench project is on doesn't
  compile; fix it (or switch branch) first. Errors that only appear on the Spark usually mean the
  references are stale; rerun `refs`.
- **A helper is "not used"**: the log says why. Rerun `.\NightShift.ps1 helpers` for that machine.
- **Logs**: `Tools/NightShift/.state/nightshift.log` (coordinator), and each worker's
  `.state/runs/*.jsonl` (full agent transcripts).
