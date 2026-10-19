# Night Shift

Local AI coding agents that work through [tasks.md](tasks.md) overnight (and into the day, until you stop them),
so CLONE-Z gets built while you sleep without spending cloud AI tokens. The models run in Ollama on the DGX Spark
(`qwen3.8` and `gpt-oss:120b`),
[OpenCode](https://opencode.ai) is the agent, and everything runs on your own machines:

- **The Spark** runs the model and the coordinator, inside the AI Workbench project container. In AI
  Workbench it is the **night-shift** toggle under *Applications*.
- **Helper machines** (optional) run extra agents in parallel. They use the Spark's model over the LAN and
  do their own compile checks, so more tasks get done per night.
- **This PC** sets things up, opens the pull request when the work is done, and fetches the results. The
  agents keep working while it sleeps.

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
4. **Automatic pull request (optional).** Choose **A** in the menu, or run `.\NightShift.ps1 autopr`. Every
   15 minutes this PC checks the Spark, and once the agents have nothing left to do it pushes their branch to
   GitHub and opens a pull request (or updates the open one) with the reports as its description. It uses
   this PC's `gh` sign-in, so no GitHub token goes near the agents. It runs only while this PC is on and
   you're logged in; `.\NightShift.ps1 autopr -Off` turns it off, and `.state\pr.log` records what it did.

## Every night

1. **Queue tasks** in [tasks.md](tasks.md), one `- [ ]` line each. Edit it in JupyterLab (*Open
   JupyterLab* in AI Workbench, then `Tools/NightShift/tasks.md`), or choose **1** in the menu. Small,
   concrete tasks work best: name the place and say what you want to see in game.
2. **Start it.** Switch on **night-shift** under *Applications* in AI Workbench, or choose **2**. It keeps
   going into the day until you stop it, picking up tasks you add in the meantime. (Set
   `NIGHTSHIFT_STOP_AT` in [nightshift.env](nightshift.env) for a fixed time to stop taking new tasks.)
   When the queue runs dry, the log says `idle: nothing left to start` and the agents wait for new tasks.
3. **Watch it work** with **4** in the menu: a live, color-coded log of every agent's steps and messages
   (one color per machine, landings in green, failures in red). The same view is
   `bash /project/Tools/NightShift/nightshift.sh log` in a JupyterLab terminal. **3** shows a status summary.

## Start and stop on command

| | Start | Stop after the tasks in progress | Stop now |
|---|---|---|---|
| AI Workbench | **night-shift** toggle on | | **night-shift** toggle off |
| This PC (menu: double-click [NightShift.bat](NightShift.bat)) | **2**, or `NightShift.bat start` | **5**, or `NightShift.bat finish` | **6**, or `NightShift.bat stop` |
| JupyterLab terminal | `bash /project/Tools/NightShift/nightshift.sh start` | `... nightshift.sh finish` | `... nightshift.sh stop` |

Starting from this PC goes through AI Workbench's CLI, so it also starts the project container when it's down,
and the toggle in Workbench shows the right state either way. Stopping now rolls back the unfinished tasks; they
stay queued and start over next time. Starting again carries on with the same `nightshift/` branch (and pull
request) as long as the Workbench project is still on it. To begin a fresh branch, switch the Workbench project
back to `main` first.

**While it runs, leave the Workbench project's git alone** (no branch switches, commits, pulls or pushes in
Workbench). Switching to an older branch can hide the ignore rules for `.state/` and `.unityrefs/` (the Spark's SSH
key and Unity's DLLs), and Workbench will then offer to commit them. If the project is switched off the night
shift's branch, the night shift stops so nothing lands on the other branch.

## Reviewing the work

- `tasks.md` on the branch is ticked off: `[x]` landed, `[!]` failed (the attempt is saved as a
  patch), `[~]` nothing needed changing. `reports/<date>.md` has a summary of each task.
- With the automatic pull request on, the pull request is waiting on GitHub. Choose **P** (or
  `.\NightShift.ps1 pr`) to open or update it right away, even while the agents are still working.
- Choose **7** (or `.\NightShift.ps1 fetch`) to fetch the branch into this repo, then
  `git switch -c nightshift/<date> spark/nightshift/<date>` and try it in Unity. The night shift can
  compile but not play the game, so play-test before merging.
- After merging, switch the Workbench project back to your normal branch so the next run starts fresh.

## How it works

`nightshift.py run` (started by `nightshift.sh start`) is the coordinator:

1. It carries on with the `nightshift/` branch the Workbench project is on, or else creates
   `nightshift/<date>` and switches to it. It commits the queue and refreshes
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
container on the Spark and as the helper user on the helpers. The pull request step refuses to push anything
under `.state/` or `.unityrefs/`, or anything that looks like a private key.

## Settings

[nightshift.env](nightshift.env) sets the models, the stop time (empty: keep going until stopped), the time
limit per task and the number of fix rounds. It's read at every start. The Spark's own agent uses `qwen3.8`, and the helpers use
`gpt-oss:120b`: Ollama serves `qwen3.8` one request at a time, so helpers on it would only wait their turn,
while `gpt-oss:120b` runs several at once (and is about twice as fast).

Inside the container, `bash Tools/NightShift/nightshift.sh` also has `check` (compile the project as it is),
`state` (one line of JSON that `NightShift.ps1 pr` reads) and `chat` (OpenCode in a terminal with the same
local model, for interactive work in JupyterLab's terminal).

## Troubleshooting

- **The log has gone quiet**: if its last line starts with `idle:`, every task is done and the agents are
  waiting for new ones; add tasks and they start within a minute. **3** (status) shows the same.
- **"Unity's reference assemblies aren't on the Spark"**: run `.\NightShift.ps1 refs` on this PC.
- **"the project doesn't compile before any changes"**: the branch the Workbench project is on doesn't
  compile; fix it (or switch branch) first. Errors that only appear on the Spark usually mean the
  references are stale; rerun `refs`.
- **A helper is "not used"**: the log says why. Rerun `.\NightShift.ps1 helpers` for that machine.
- **"couldn't bring the project folder up to date with the landing"**: a file that landing changed was edited in
  the project folder while it ran, so the landing was undone and the task stays queued. Adding tasks to
  `tasks.md` is fine at any time; leave the other files alone while it runs.
- **No pull request appeared**: `.state\pr.log` on this PC says why (for example a failed push). The
  automatic one waits until nothing is left to do; **P** makes it right away.
- **Logs**: `Tools/NightShift/.state/nightshift.log` (coordinator), and each worker's
  `.state/runs/*.jsonl` (full agent transcripts).
