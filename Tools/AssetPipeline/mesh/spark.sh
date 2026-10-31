#!/usr/bin/env bash
# Mesh stage control, run on the DGX Spark itself. AssetPipeline.ps1 calls it over SSH after uploading the pipeline;
# it also works by hand from ~/clonez-pipeline/Tools/AssetPipeline/mesh:
#   build           build (or rebuild) the container image, in tmux session clonez-mesh-build
#   prepare         download the model weights once (~40 GB) and convert DINOv3; the only step that needs internet
#   start [args]    generate meshes in tmux session clonez-mesh; args go to meshgen.py generate (e.g. --pilot)
#   stop            finish the version in progress, then stop (and stay stopped until the next start)
#   refit [args]    fit finished meshes again after a fitting change, without generating; args: --ids A,B
#   status          whether it is running, the latest log lines and progress
#   web             serve the live view (live.html: the item being made, its picture, the step, the latest meshes)
#                   and the gallery on port 8765, in tmux session clonez-mesh-web; start does this too
#   web-stop        stop serving them
#   hold [reason]   make start and refit refuse (with the reason) until release, e.g. while the pipeline is changed
#   release         undo hold
#   schedule        keep the run going around the clock: cron runs keep-running every 10 minutes, which starts it
#                   again with the last start's arguments after a reboot or a crash; unschedule undoes it
#
# The mesh run has the Spark to itself: its models and gpt-oss:120b don't fit in memory together. start refuses
# while the night shift is running, unloads every Ollama model but the critic, and while it runs it touches
# Tools/NightShift/.state/mesh-run.lock in the AI Workbench project, which stops nightshift.sh from starting.
set -uo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../../.." && pwd)"
MODELS="${CLONEZ_MODELS:-$HOME/clonez-models}"
IMAGE="${CLONEZ_MESH_IMAGE:-clonez-mesh:1}"
SESSION=clonez-mesh   # tmux takes a bare session name as a prefix (clonez-mesh would match clonez-mesh-web), so checks use =name
OUT="$ROOT/Tools/AssetPipeline/out/mesh"
OLLAMA=http://127.0.0.1:11434
CRITIC_MODEL="${CLONEZ_CRITIC_MODEL:-gemma3:12b}"
NS_CONTAINER=project-henry-cahill-clone-z
NS_STATE="$HOME/nvidia-workbench/Henry-Cahill-CLONE-Z/Tools/NightShift/.state"
LOCK="$NS_STATE/mesh-run.lock"
PORT=8765   # AssetPipeline.ps1 opens the live view on this port too ($MeshPort)
NAS="${CLONEZ_NAS:-/mnt/clonez}"   # the NAS share (\\192.168.1.170\CLONE-Z), where the originals are kept

night_shift_running() {
  docker ps --format '{{.Names}}' | grep -qx "$NS_CONTAINER" &&
    docker exec "$NS_CONTAINER" bash /project/Tools/NightShift/nightshift.sh health >/dev/null 2>&1
}

# Frees the memory Ollama holds (gpt-oss:120b alone is 65 GB); the critic stays, the run uses it for every item.
unload_ollama() {
  local name
  for name in $(curl -fs --max-time 5 "$OLLAMA/api/ps" | python3 -c 'import json, sys; print("\n".join(m["name"] for m in json.load(sys.stdin).get("models") or []))' 2>/dev/null); do
    [ "$name" = "$CRITIC_MODEL" ] && continue
    echo "Unloading $name from Ollama..."
    curl -fs --max-time 60 "$OLLAMA/api/generate" -d "{\"model\": \"$name\", \"keep_alive\": 0}" >/dev/null || true
  done
}

# Runs a command in the image as this user, with the pipeline at /work, the weights at /models and the NAS share at /nas.
in_container() {
  local name="$1"; shift
  mkdir -p "$MODELS/home" "$MODELS/hf"
  ls "$NAS/README.md" >/dev/null 2>&1   # wakes the share's automount, so the bind below sees the share itself
  docker run --rm --name "$name" --gpus all --network host --shm-size 8g \
    --user "$(id -u):$(id -g)" -e HOME=/models/home -e HF_HOME=/models/hf -e PYTHONIOENCODING=utf-8 \
    -v "$ROOT":/work -v "$MODELS":/models --mount "type=bind,source=$NAS,target=/nas,bind-propagation=rslave" \
    -w /work/Tools/AssetPipeline/mesh "$IMAGE" "$@"
}

need_image() {
  docker image inspect "$IMAGE" >/dev/null 2>&1 && return 0
  # Just after a reboot, keep-running can get here before Docker is up.
  if ! docker info >/dev/null 2>&1; then echo "Docker isn't answering (still starting up?)."; return 1; fi
  echo "The mesh image isn't built yet. Run 'build' first (about an hour, once)."
  return 1
}

# Whether keep-running is in this user's crontab (schedule), and so whether a start refused for now happens later.
scheduled() {
  crontab -l 2>/dev/null | grep -q 'clonez-mesh-schedule'
}

when_free() {
  if scheduled; then echo "the run starts by itself once that has finished (keep-running checks every 10 minutes)"
  else echo "start again once that has finished"; fi
}

# The Spark's address on the network (the source address of its default route).
lan_ip() {
  ip -4 route get 1.1.1.1 2>/dev/null | sed -n 's/.* src \([0-9.]*\).*/\1/p'
}

# Serves out/mesh read-only to the network: the live view, the gallery, the previews and the logs. Idempotent.
serve() {
  mkdir -p "$OUT"
  if [ ! -f "$HERE/live.html" ]; then echo "live.html is missing beside spark.sh; upload the pipeline again."; return 1; fi
  cp -f "$HERE/live.html" "$OUT/live.html"
  if ! tmux has-session -t =clonez-mesh-web 2>/dev/null; then
    tmux new-session -d -s clonez-mesh-web "python3 -m http.server $PORT --bind 0.0.0.0 --directory '$OUT'"
    sleep 1
    if ! tmux has-session -t =clonez-mesh-web 2>/dev/null; then echo "The live view didn't start (is port $PORT taken?)."; return 1; fi
  fi
  local ip
  ip="$(lan_ip)"
  echo "Watch it live: http://${ip:-<the Spark>}:$PORT/live.html  (every finished mesh: http://${ip:-<the Spark>}:$PORT/)"
}

case "${1:-status}" in
  build)
    if tmux has-session -t =clonez-mesh-build 2>/dev/null; then echo 'A build is already running (tmux session clonez-mesh-build).'; exit 0; fi
    tmux new-session -d -s clonez-mesh-build \
      "cd '$HERE' && { date; docker build --progress=plain -t '$IMAGE' . ; echo BUILD_EXIT=\$?; date; } 2>&1 | tee build.log"
    echo "Building $IMAGE in tmux session clonez-mesh-build; log: $HERE/build.log"
    ;;

  prepare)
    need_image || exit 1
    if tmux has-session -t =clonez-mesh-prepare 2>/dev/null; then echo 'Already preparing (tmux session clonez-mesh-prepare).'; exit 0; fi
    mkdir -p "$OUT"
    tmux new-session -d -s clonez-mesh-prepare \
      "bash '$HERE/spark.sh' _prepare 2>&1 | tee -a '$OUT/prepare.log'"
    echo "Downloading weights in tmux session clonez-mesh-prepare; log: $OUT/prepare.log"
    ;;

  _prepare)
    in_container clonez-mesh-prepare python -u meshgen.py prepare
    echo "PREPARE_EXIT=$?"
    ;;

  start)
    shift
    need_image || exit 1
    if [ ! -f "$MODELS/prepared.json" ]; then echo "The weights aren't downloaded yet. Run 'prepare' first."; exit 1; fi
    if [ -f "$OUT/hold" ]; then echo "Not starting: $(cat "$OUT/hold")"; exit 6; fi
    if tmux has-session -t "=$SESSION" 2>/dev/null; then echo "A mesh run is already going. Use 'status'."; exit 0; fi
    # From here on this is the run wanted: keep-running starts it with these arguments whenever it isn't going (once
    # the GPU or the night shift below has finished, after a crash or a reboot) until a stop, or until a start finds
    # nothing left to make.
    mkdir -p "$OUT"
    args=""
    [ $# -gt 0 ] && args=$(printf ' %q' "$@")
    printf '%s\n' "$args" > "$OUT/run.args"
    rm -f "$OUT/stop-requested" "$OUT/complete"
    # Every mesh-stage job (a refit, a benchmark, the weights' conversion) loads models onto the same GPU.
    busy="$(docker ps --filter "ancestor=$IMAGE" --format '{{.Names}}' | head -n 1)"
    if [ -n "$busy" ]; then
      echo "The GPU is busy with another mesh-stage job ($busy); $(when_free)."
      exit 5
    fi
    if night_shift_running; then
      echo "The night shift is running, and the mesh run needs the Spark to itself. Stop the night shift (NightShift.bat, 6); $(when_free)."
      exit 4
    fi
    [ -d "$NS_STATE" ] || mkdir -p "$NS_STATE" 2>/dev/null ||
      echo "Warning: can't reach $NS_STATE, so the night shift won't know to stay off."
    unload_ollama
    tmux new-session -d -s "$SESSION" "bash '$HERE/spark.sh' _run$args"
    sleep 2
    echo "Mesh generation started in tmux session $SESSION; log: $OUT/run.log"
    serve || true
    ;;

  _run)
    shift
    # Keeps the night shift off while this run lives, and should this shell be killed first, for as long as its
    # container still runs (an orphaned container ignores the hang-up). Then the lock goes.
    (
      while :; do
        touch "$LOCK" 2>/dev/null
        sleep 60
        kill -0 $$ 2>/dev/null && continue
        [ "$(docker inspect -f '{{.State.Running}}' "$SESSION" 2>/dev/null)" = true ] || break
      done
      rm -f "$LOCK"
    ) &
    heartbeat=$!
    trap 'kill $heartbeat 2>/dev/null; rm -f "$LOCK"' EXIT
    rm -f "$OUT/stop-requested"
    crashes=0
    while :; do
      in_container "$SESSION" python -u meshgen.py generate --ollama "$OLLAMA" --critic-model "$CRITIC_MODEL" "$@" 2>&1 |
        tee -a "$OUT/run.log"
      code=$?
      # 3: nothing left to make (every version asked for has its mesh or was given up on); keep-running leaves it at
      # that until the next start.
      if [ "$code" -eq 3 ]; then touch "$OUT/complete"; break; fi
      # 0: it made what it set out to, or was stopped. Anything else is a crash (the kernel's out-of-memory killer,
      # say): start again. meshgen counts each try in attempts.json before it starts, so the version that took the
      # run down is skipped after its second crash.
      if [ "$code" -eq 0 ] || [ -f "$OUT/stop-requested" ] || [ -f "$OUT/hold" ]; then break; fi
      crashes=$((crashes + 1))
      if [ "$crashes" -ge 5 ]; then
        echo "$(date '+%F %T') the run crashed $crashes times in a row; giving up for now (with the schedule in place, keep-running tries again within 10 minutes)." |
          tee -a "$OUT/run.log"
        break
      fi
      echo "$(date '+%F %T') the run ended with exit code $code; starting again in a minute ($crashes of 5)." |
        tee -a "$OUT/run.log"
      sleep 60
      [ -f "$OUT/stop-requested" ] && break
    done
    ;;

  stop)
    # Also what tells keep-running to leave the run alone, until the next start.
    mkdir -p "$OUT"
    touch "$OUT/stop-requested"
    if docker kill --signal INT "$SESSION" >/dev/null 2>&1; then
      echo 'Stopping after the version in progress (up to a few minutes). It stays stopped until the next start.'
    else
      echo 'Not running. It stays stopped until the next start.'
    fi
    ;;

  refit)
    shift
    need_image || exit 1
    if [ -f "$OUT/hold" ]; then echo "Not refitting: $(cat "$OUT/hold")"; exit 6; fi
    # A run never touches finished meshes, so refitting named ones beside it is safe; refitting them all is not, as
    # the run may be writing the one it has just finished.
    if tmux has-session -t "=$SESSION" 2>/dev/null && [[ " $* " != *" --ids "* ]]; then
      echo "A mesh run is going. Refit named meshes only (--ids A,B), or wait until it has finished or been stopped."
      exit 1
    fi
    in_container clonez-mesh-refit python -u meshgen.py refit "$@"
    ;;

  status)
    if tmux has-session -t "=$SESSION" 2>/dev/null; then echo '== RUNNING =='; else echo '== not running =='; fi
    if scheduled; then
      if [ -f "$OUT/hold" ]; then echo "Held: $(cat "$OUT/hold")"
      elif [ -f "$OUT/stop-requested" ]; then echo 'Stopped by hand: it stays stopped until the next start.'
      elif [ -f "$OUT/complete" ]; then echo 'Complete: the last start found nothing left to make.'
      else echo 'Kept going around the clock: if it stops, keep-running starts it again within 10 minutes.'; fi
    fi
    if tmux has-session -t =clonez-mesh-build 2>/dev/null; then echo '(the image is still building: tail build.log)'; fi
    if tmux has-session -t =clonez-mesh-prepare 2>/dev/null; then echo '(weights are still downloading: tail out/mesh/prepare.log)'; fi
    if tmux has-session -t =clonez-mesh-web 2>/dev/null; then echo "Live view: http://$(lan_ip):$PORT/live.html"; fi
    echo; echo 'Latest progress:'; tail -n 6 "$OUT/run.log" 2>/dev/null
    echo; python3 "$HERE/meshgen.py" status --root "$ROOT"
    ;;

  web)
    serve
    ;;

  web-stop)
    if tmux kill-session -t =clonez-mesh-web 2>/dev/null; then echo 'The live view has stopped.'; else echo 'The live view was not running.'; fi
    ;;

  hold)
    shift
    mkdir -p "$OUT"
    echo "${*:-the mesh stage is on hold}" > "$OUT/hold"
    echo "Held: start refuses until 'release'."
    ;;

  release)
    rm -f "$OUT/hold" && echo 'Released: start works again.'
    ;;

  keep-running)
    # cron, every 10 minutes (schedule): starts the run with the last start's arguments whenever it isn't going (after
    # a reboot, or a crash it gave up restarting from), unless it was stopped by hand, is held, or found nothing left
    # to make. A start refused for now (the night shift, another job on the GPU) is simply tried again next time.
    tmux has-session -t "=$SESSION" 2>/dev/null && exit 0
    if [ -f "$OUT/stop-requested" ] || [ -f "$OUT/hold" ] || [ -f "$OUT/complete" ]; then exit 0; fi
    mkdir -p "$OUT"
    exec >>"$OUT/schedule.log" 2>&1
    echo "$(date '+%F %T') keep-running: the run isn't going; starting it"
    saved="$(cat "$OUT/run.args" 2>/dev/null)"
    eval "set -- $saved"
    bash "$0" start "$@"
    echo "  start exit $?"
    ;;

  schedule)
    # Puts keep-running in this user's crontab, replacing an earlier schedule (the old daily window's included);
    # unschedule removes it.
    { crontab -l 2>/dev/null | grep -v 'clonez-mesh-schedule'
      echo "*/10 * * * * bash '$HERE/spark.sh' keep-running  # clonez-mesh-schedule"
    } | crontab -
    echo "The mesh run is kept going around the clock: every 10 minutes, cron starts it again if it has stopped (a reboot, a crash), unless it was stopped by hand, is held, or had nothing left to make. Log: $OUT/schedule.log"
    ;;

  unschedule)
    crontab -l 2>/dev/null | grep -v 'clonez-mesh-schedule' | crontab -
    echo 'The mesh run is no longer started by itself; start and stop it by hand.'
    ;;

  *)
    sed -n '2,16p' "$0"; exit 2
    ;;
esac
