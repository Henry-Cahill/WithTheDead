#!/usr/bin/env bash
# Night Shift control, run inside the AI Workbench project container (the "night-shift" app uses it).
#   start   work through tasks.md in the background
#   finish  stop after the tasks in progress
#   stop    stop now; unfinished tasks are rolled back and stay queued
#   health  exit 0 while running (Workbench health check)
#   status  progress, current tasks and recent commits
#   state   one line of JSON (running, busy or idle, branch) for NightShift.ps1
#   log     follow the live log
#   check   compile check of the project as it is now
#   plan    write one task with the local model now (plan --dry-run only shows it)
#   chat    interactive OpenCode with the same local model and settings
set -uo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STATE="$HERE/.state"
PIDFILE="$STATE/nightshift.pid"   # written by the coordinator itself
LOG="$STATE/nightshift.log"
LOCAL_WORK_PID=/tmp/nightshift-work/spark/work.pid
PY="$(command -v python3)"
TZ_SETTING="$(sed -n 's/^NIGHTSHIFT_TZ=//p' "$HERE/nightshift.env" 2>/dev/null)"
export TZ="${TZ_SETTING:-UTC}"
mkdir -p "$STATE"

running() { [ -f "$PIDFILE" ] && kill -0 "$(cat "$PIDFILE")" 2>/dev/null; }

case "${1:-status}" in
  start)
    exec 9>"$STATE/start.lock"
    flock -n 9 || { echo "The night shift is already starting."; exit 0; }
    if running; then echo "The night shift is already running (pid $(cat "$PIDFILE"))."; exit 0; fi
    # The asset pipeline's mesh run (Tools/AssetPipeline/mesh/spark.sh) has the Spark to itself, since its models
    # and gpt-oss:120b don't fit in memory together. It touches this file every minute; a stale one (a crashed run)
    # stops counting after ten.
    if [ -n "$(find "$STATE/mesh-run.lock" -mmin -10 2>/dev/null)" ]; then
      msg="The Spark is generating item meshes and has no room for the night shift's model. Stop that run (AssetPipeline.bat, mesh stop) or let it finish, then start the night shift again."
      echo "$msg"; echo "$(date '+%H:%M:%S') not starting: $msg" >>"$LOG"; exit 1
    fi
    echo "===== $(date '+%Y-%m-%d %H:%M') start =====" >>"$LOG"
    nohup setsid "$PY" -u "$HERE/nightshift.py" run >>"$LOG" 2>&1 </dev/null 9>&- &
    for _ in 1 2 3 4 5; do sleep 1; running && break; done
    if running; then
      echo "The night shift has started. Follow it with: bash $HERE/nightshift.sh log"
    else
      echo "The night shift stopped straight away:"; tail -n 15 "$LOG"; exit 1
    fi
    ;;
  finish)
    if ! running; then echo "The night shift is not running."; exit 0; fi
    touch "$STATE/finish-requested"
    echo "The night shift will stop after the tasks in progress."
    ;;
  stop)
    if ! running; then echo "The night shift is not running."; exit 0; fi
    pid="$(cat "$PIDFILE")"
    kill -TERM "$pid"
    for _ in $(seq 1 90); do running || break; sleep 1; done
    if running; then
      kill -KILL -- "-$pid" 2>/dev/null || kill -KILL "$pid"
      [ -f "$LOCAL_WORK_PID" ] && kill -KILL -- "-$(cat "$LOCAL_WORK_PID")" 2>/dev/null
    fi
    echo "The night shift has stopped."
    ;;
  health) running ;;
  status) exec "$PY" "$HERE/nightshift.py" status ;;
  state) exec "$PY" "$HERE/nightshift.py" state ;;
  log)
    echo "Watching the night shift (Ctrl+C stops watching; the work keeps going)"
    # One color per worker; landings bright green, failures red, run headers white.
    tail -n 60 -F "$LOG" 2>/dev/null | awk 'BEGIN { c[0] = "36"; c[1] = "33"; c[2] = "35"; c[3] = "32"; c[4] = "34"; n = 0 }
    {
      col = ""
      if (match($0, /\[[^]]+\]/)) { w = substr($0, RSTART, RLENGTH); if (!(w in m)) { m[w] = c[n % 5]; n++ }; col = m[w] }
      if ($0 ~ / idle: /) col = "1;97"
      else if ($0 ~ /landed /) col = "1;92"
      else if ($0 ~ /\] (failed|gave up)|\(failed\)|not using|night shift stopped|agent error|exited with code|time limit|was switched from/) col = "1;91"
      else if ($0 ~ /^=====|starting ·|workers:|done:/) col = "1;97"
      if (col != "") printf "\033[%sm%s\033[0m\n", col, $0; else print $0
      fflush()
    }'
    ;;
  check) exec bash "$HERE/check.sh" ;;
  plan) shift; exec "$PY" "$HERE/nightshift.py" plan "$@" ;;
  chat) shift; exec "$PY" "$HERE/nightshift.py" chat "$@" ;;
  *) echo "usage: $0 start|finish|stop|health|status|state|log|check|plan|chat"; exit 2 ;;
esac
