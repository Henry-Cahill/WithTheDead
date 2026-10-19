#!/usr/bin/env bash
# Night Shift control, run inside the AI Workbench project container (the "night-shift" app uses it).
#   start   work through tasks.md in the background
#   finish  stop after the tasks in progress
#   stop    stop now; unfinished tasks are rolled back and stay queued
#   health  exit 0 while running (Workbench health check)
#   status  progress, current tasks and recent commits
#   log     follow the live log
#   check   compile check of the project as it is now
#   chat    interactive OpenCode with the same local model and settings
set -uo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STATE="$HERE/.state"
PIDFILE="$STATE/nightshift.pid"
LOG="$STATE/nightshift.log"
PY="$(command -v python3)"
mkdir -p "$STATE"

running() { [ -f "$PIDFILE" ] && kill -0 "$(cat "$PIDFILE")" 2>/dev/null; }

case "${1:-status}" in
  start)
    if running; then echo "The night shift is already running (pid $(cat "$PIDFILE"))."; exit 0; fi
    echo "===== $(date '+%Y-%m-%d %H:%M') start =====" >>"$LOG"
    nohup setsid "$PY" -u "$HERE/nightshift.py" run >>"$LOG" 2>&1 </dev/null &
    echo $! >"$PIDFILE"
    sleep 2
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
    if running; then kill -KILL -- "-$pid" 2>/dev/null || kill -KILL "$pid"; pkill -f "opencode run" || true; fi
    echo "The night shift has stopped."
    ;;
  health) running ;;
  status) exec "$PY" "$HERE/nightshift.py" status ;;
  log) exec tail -n 40 -f "$LOG" ;;
  check) exec bash "$HERE/check.sh" ;;
  chat) shift; exec "$PY" "$HERE/nightshift.py" chat "$@" ;;
  *) echo "usage: $0 start|finish|stop|health|status|log|check|chat"; exit 2 ;;
esac
