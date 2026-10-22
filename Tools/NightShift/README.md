# Night Shift

Local AI coding agents that work through [tasks.md](tasks.md) overnight (and into the day, until you stop them),
so CLONE-Z gets built while you sleep without spending cloud AI tokens. The models run in Ollama on the DGX Spark
(`qwen3.8` and `gpt-oss:120b`),
[OpenCode](https://opencode.ai) is the agent, and everything runs on your own machines:

- **The Spark** runs the model and the coordinator, inside the AI Workbench project container. In AI
  Workbench it is the **night-shift** toggle under *Applications*.
- **Helper machines** (optional) run extra agents in parallel. They use the Spark's model over the LAN and
  do their own compile checks, so more tasks get done per night. They can be Linux or Windows (see
  [Windows helpers](#windows-helpers)); a helper can also be set to work through the documentation tasks only (see
  [Documentation workers](#documentation-workers)).
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
   low CPU priority, so game servers on the same machines aren't affected. A Windows PC needs one step on it
   first: see [Windows helpers](#windows-helpers). Without `-Helpers`, it sets up every helper the night shift uses
   (the list at the top of [NightShift.ps1](NightShift.ps1)); the Spark uses those that were set up.
4. **Automatic pull request (optional).** Choose **A** in the menu, or run `.\NightShift.ps1 autopr`. Every
   15 minutes this PC checks the Spark, and once the agents have nothing left to do it pushes their branch to
   GitHub and opens a pull request (or updates the open one) with the reports as its description. Once that
   pull request is merged, a new one opens only when more tasks finish on the branch. It uses
   this PC's `gh` sign-in, so no GitHub token goes near the agents. It runs only while this PC is on and
   you're logged in; `.\NightShift.ps1 autopr -Off` turns it off, and `.state\pr.log` records what it did.

## Every night

1. **Queue tasks** in [tasks.md](tasks.md), one `- [ ]` line each. Edit it in JupyterLab (*Open
   JupyterLab* in AI Workbench, then `Tools/NightShift/tasks.md`), or choose **1** in the menu. Small,
   concrete tasks work best: name the place and say what you want to see in game. For a bigger job, write
   it as steps under the task (`Step 1: ...`, `Step 2: ...`); it then goes to the helpers' stronger model
   (see [Big tasks for the stronger model](#big-tasks-for-the-stronger-model)).
2. **Start it.** Switch on **night-shift** under *Applications* in AI Workbench, or choose **2**. It keeps
   going into the day until you stop it, picking up tasks you add in the meantime. (Set
   `NIGHTSHIFT_STOP_AT` in [nightshift.env](nightshift.env) for a fixed time to stop taking new tasks.)
   With auto-planning on (the default, see below) the queue never runs dry. With it off, the log says
   `idle: nothing left to start` when the queue is empty, and the agents wait for new tasks.
3. **Watch it work** with **4** in the menu: a live, color-coded log of every agent's steps and messages
   (one color per machine, landings in green, failures in red). The same view is
   `bash /project/Tools/NightShift/nightshift.sh log` in a JupyterLab terminal. **3** shows a status summary.

## A queue that never runs dry

With `NIGHTSHIFT_AUTOPLAN=1` in [nightshift.env](nightshift.env), the night shift writes its own tasks whenever
fewer are waiting than there are workers plus two. It takes the focus areas in `NIGHTSHIFT_AUTOPLAN_FOCUS` in turn:

| Focus | What the planner looks for |
|---|---|
| `map` | the real-world raid map's code (`Assets/Scripts/World/Map/`): buildings, streets and props that look and play like the hand-built town |
| `graphics` | small visual detail or variation on an object a script builds from 3D shapes (boxes, cylinders, quads) |
| `fixes` | a bug it can prove from the code |
| `perf` | work repeated every frame that could be cached |
| `features` | a small addition next to an existing action or mechanic |
| `flavour` | new lines for an existing list of written text |
| `docs` | missing `/// <summary>` comments (a fixed task; no model needed; see [Documentation workers](#documentation-workers)) |

For each task it picks the script that fits the focus and was shown least recently, shows it whole to the local model
(`NIGHTSHIFT_AUTOPLAN_MODEL`, by default the helpers' model, which runs beside the agents) with
[LESSONS.md](memory/LESSONS.md), and queues the task it writes under `### Auto-planned <date>` in tasks.md, but only
if the task names that script, mentions no file that doesn't exist or that agents may not touch, calls no method
that exists nowhere in the scripts, and doesn't repeat a task already in tasks.md. It skips scripts that already
have a task waiting, so no two tasks collide. Tasks you write always start before the planner's, wherever you add
them. Delete any planned task you don't want before it starts.

`nightshift.sh plan --dry-run` (or `plan --focus graphics --target Assets/Scripts/World/DecorProps.cs`) shows what
the planner would write, without queueing it. Since the queue never empties, the automatic pull request publishes
every `NIGHTSHIFT_PUBLISH_EVERY` minutes (120 by default) instead of waiting for the agents to run out of work.

### Big tasks for the stronger model

The helpers' `gpt-oss:120b` lands nearly every task in a few minutes of its two hours, while the Spark's `qwen3.8`
lands about three in four and takes half an hour over each. So the planner writes big tasks for the first and small
ones for the second:

- **A focus with a trailing `+`** (`map+`, `graphics+`, `fixes+`, `perf+`, `features+`, `flavour+`) asks for one
  big task: three to six located steps (`Step 1:`, `Step 2:`, ... then `Done when:`) that together give the player one
  visible result. The planner also shows the model the scripts most tied to the target, in outline, and the task may
  change up to two of them and add one new script. It must still name only methods that exist (a few new helpers
  are fine) and leave alone every script a queued task names. `docs` has no big variant.
- **Workers on a model in `NIGHTSHIFT_SMALL_TASK_MODELS`** (`qwen3.8` by default) take only small tasks. Any task
  written in steps (two `Step N:` lines or more), or longer than eight detail lines or 3,000 characters, is big and
  goes to the other workers, which take the planner's big tasks before its small ones. Tasks you write keep their
  order either way.
- **The planner keeps a small task waiting for each of those workers**: when none is left, it writes the next focus
  in turn at its small size (`graphics+` as `graphics`, say). If no worker that takes big tasks is left on duty,
  every worker takes any task and the planner writes only small ones.

`plan --focus graphics+ --dry-run` shows a big task the planner would write.

### Documentation workers

`NIGHTSHIFT_DOCS_WORKERS` in [nightshift.env](nightshift.env) names workers (as the log names them, or by host) that
work through the documentation queue and nothing else: the `docs` focus's tasks, which add missing `/// <summary>`
comments. `laserengraver` is one. Tasks you write with the planner's wording ("Add a one-sentence `/// <summary>` to
each public method and property in ...") count as documentation tasks too. While a documentation worker is on duty:

- it takes only documentation tasks, and the other workers leave those to it;
- the planner keeps a documentation task waiting for each documentation worker, and its focus rotation skips `docs`,
  so the rest of the queue goes to the other kinds of work;
- a script whose earlier documentation passes all landed gets another pass (three at most) once later work has left
  three or more of its public members without a summary. A pass that found nothing to do or failed isn't repeated.
  When no script needs one, the planner looks again later, as the night's other work lands.

When no documentation worker is left on duty, the others take documentation tasks again. A documentation worker uses
`NIGHTSHIFT_HELPER_MODEL`, like the other helpers.

## Windows helpers

A Windows PC can be a helper too: `laserengraver` (account `LASERENGRAVER\HTSADMIN`) is one. The Spark drives it over
SSH like the others. Its OpenSSH server runs the commands in Git Bash, its worker runs in Windows' own Python, and the
agent's shell commands and [check.sh](check.sh) run in Git Bash too. To set one up:

1. On that PC, signed in as the account the night shift should use (Remote Desktop is fine), copy
   [WindowsHelper.ps1](WindowsHelper.ps1) over and run it in PowerShell **as administrator**, with this PC's public
   key (the line in `%USERPROFILE%\.ssh\id_ed25519.pub` here):
   `powershell -ExecutionPolicy Bypass -File .\WindowsHelper.ps1 -AuthorizedKey 'ssh-ed25519 AAAA... you@this-pc'`
   It installs and starts Windows' OpenSSH server (reachable from the local network only), installs Git and Python 3
   with winget when they're missing, makes Git Bash the shell OpenSSH runs commands in (for every SSH login on that
   PC; it prints the shell it replaces), and authorizes the key. Running it again is safe.
2. On this PC, connect once so it remembers the PC's host key: `ssh htsadmin@laserengraver exit`.
3. Choose **9** in the menu, or run `.\NightShift.ps1 helpers`. It finishes the PC like a Linux helper: OpenCode and
   .NET in the account's home, the Spark's key (accepted only from the Spark's address; an administrator's keys live
   in `C:\ProgramData\ssh\administrators_authorized_keys`) and the Unity references. The helper is named after the
   computer, in lower case. The Spark only gets the PC once its night shift can drive Windows helpers: one from
   before them would run the worker as on Linux, which fails at once. Until then `helpers` says so and leaves the PC
   out; with the night shift stopped, switch the Workbench project to a branch that has this version (`main`, once
   it's merged there) and pull, then run `helpers` again.

Keep the PC awake through the night (WindowsHelper.ps1 warns if it sleeps on mains power). Its worker runs at
below-normal CPU priority, and everything it starts (the agent, Git Bash, dotnet) ends with it.

## Start and stop on command

| | Start | Stop after the tasks in progress | Stop now |
|---|---|---|---|
| AI Workbench | **night-shift** toggle on | | **night-shift** toggle off |
| This PC (menu: double-click [NightShift.bat](NightShift.bat)) | **2**, or `NightShift.bat start` | **5**, or `NightShift.bat finish` | **6**, or `NightShift.bat stop` |
| JupyterLab terminal | `bash /project/Tools/NightShift/nightshift.sh start` | `... nightshift.sh finish` | `... nightshift.sh stop` |

Starting from this PC goes through AI Workbench's CLI, so it also starts the project container when it's down (and
AI Workbench itself on the Spark, if nobody has opened the Spark in the AI Workbench app since it last stopped),
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
  GitHub-side edits on the same branch are preserved: publishing merges them with the Spark's results in
  a temporary checkout, without switching this PC's checkout or changing the running Spark. It never force-pushes.
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
limit per task, the number of fix rounds, auto-planning, which models take only small tasks and which workers take
only documentation tasks. It's read at every start. The Spark's own agent uses `qwen3.8`, and the helpers use
`gpt-oss:120b`: Ollama serves `qwen3.8` one request at a time, so helpers on it would only wait their turn,
while `gpt-oss:120b` runs several at once (and is about twice as fast).

Inside the container, `bash Tools/NightShift/nightshift.sh` also has `check` (compile the project as it is),
`state` (one line of JSON that `NightShift.ps1 pr` reads), `plan` (write one task with the planner now) and `chat`
(OpenCode in a terminal with the same local model, for interactive work in JupyterLab's terminal).

## Troubleshooting

- **The log has gone quiet**: if its last line starts with `idle:`, every task is done and the agents are
  waiting for new ones; add tasks and they start within a minute. **3** (status) shows the same. With
  auto-planning on, `idle: waiting for the planner's next task` means the planner is writing one; lines starting
  `planner:` say what it queued or why it skipped a script.
- **"Unity's reference assemblies aren't on the Spark"**: run `.\NightShift.ps1 refs` on this PC.
- **"the project doesn't compile before any changes"**: the branch the Workbench project is on doesn't
  compile; fix it (or switch branch) first. Errors that only appear on the Spark usually mean the
  references are stale; rerun `refs`.
- **A helper is "not used"**: the log says why. Rerun `.\NightShift.ps1 helpers` for that machine.
- **`helpers` skips a machine with "exit 255"**: ssh couldn't connect to it or log in (its own message, such as
  `Connection timed out`, is just above). A Windows PC answers like that until [WindowsHelper.ps1](WindowsHelper.ps1)
  has run on it; then connect once with `ssh user@host exit` and run `helpers` again (see
  [Windows helpers](#windows-helpers)).
- **A Windows helper fails at once with `ModuleNotFoundError: No module named 'fcntl'`** (then "two errors in a row;
  this worker is done for the night"): the Spark's night shift is from before Windows helpers, so it runs the worker
  there as on Linux (and ignores `NIGHTSHIFT_DOCS_WORKERS`). Bring the Workbench project on the Spark up to this
  version (see [Windows helpers](#windows-helpers)); until then, running `helpers` again takes the PC off the
  Spark's list.
- **"couldn't bring the project folder up to date with the landing"**: a file that landing changed was edited in
  the project folder while it ran, so the landing was undone and the task stays queued. Adding tasks to
  `tasks.md` is fine at any time; leave the other files alone while it runs.
- **"Unable to create '/project/.git/index.lock': File exists"**: a git command in the project folder was killed
  partway (by a container stop, say) and left its lock behind. The night shift waits a few seconds for a lock, as
  AI Workbench holds one briefly, but deletes one nothing has touched for 5 minutes and logs
  `removed …: a git command that was killed left it behind`. If it stopped on this error, start it again after
  those 5 minutes.
- **No pull request appeared**: `.state\pr.log` on this PC says why (for example a failed push). The
  automatic one waits until nothing is left to do (or, with auto-planning on, for the next `NIGHTSHIFT_PUBLISH_EVERY`
  minutes); **P** makes it right away.
- **Publishing reports a merge conflict**: nothing was pushed or changed on the Spark. Stop the night shift,
  fetch the GitHub and Spark versions of the branch, and reconcile them manually before retrying **P**.
  If GitHub changed during publishing, retry **P** to fetch and reconcile the latest commits.
- **Logs**: `Tools/NightShift/.state/nightshift.log` (coordinator), and each worker's
  `.state/runs/*.jsonl` (full agent transcripts).

## Publication regression tests

Run `pwsh -NoProfile -File Tools/NightShift/tests/Publish.Tests.ps1` from the repository root (or use
`powershell -NoProfile -File ...` on Windows PowerShell 5). Tests use temporary local Git history only;
they do not connect to the Spark or GitHub. `Tools/NightShift/tests/Helpers.Tests.ps1`, run the same way, checks that
`helpers` can tell whether the Spark's night shift drives Windows helpers.

The coordinator's tests (the planner, task routing and the helpers' commands) run on Linux or Windows:
`python3 -m unittest discover -s Tools/NightShift/tests -p "test_*.py"` (`python` on Windows). They use a fake model,
fake ssh and temporary folders. On Windows, which has no `fcntl`, tasks.md is locked within the process only.
