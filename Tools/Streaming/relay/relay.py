#!/usr/bin/env python3
"""The WTD stream relay: sends the one stream the headless OBS on hts-llm makes on to Twitch and YouTube at once.

OBS streams to nginx's RTMP server in this container (rtmp://127.0.0.1:1936/live, key "wtd"; nginx.conf, which this
script starts). For each destination that's switched on and has a key, an ffmpeg plays the stream from there and sends
it on as it is, with no re-encoding. When one drops, it reconnects on its own while the others carry on.

Everything else goes through OBS's WebSocket, with OBS's own password (read from its config, mounted in by
compose.yaml). The stream control page (../obs-twitch.html) keeps the keys in OBS's persistent data, slot
"wtd-destinations": {"twitch": {"on": true, "key": "live_..."}, "youtube": {"on": true, "key": "..."}}. A destination
is sent the stream only while OBS streams to the relay. Every couple of seconds, and after each command, the relay
reports how each destination is doing to every client as a CustomEvent {"wtdRelay": {...}}; it never reports a key.
Commands come the same way, as CustomEvents {"wtdRelayCommand": ...}:
  "status"   report now
  "reload"   read the keys again (a destination switched on or off mid-stream starts or stops at once)
  "test"     make the next stream, if it starts within a minute, Twitch's private bandwidth test: Twitch alone, with
             ?bandwidthtest=true on the key, and nothing to YouTube, for as long as that stream lasts
  "endtest"  call off a test that hasn't started
"""
import base64
import collections
import hashlib
import itertools
import json
import os
import re
import signal
import subprocess
import threading
import time

import websocket

OBS_URL = os.environ.get("WTD_OBS_URL", "ws://127.0.0.1:4455")
OBS_CONFIG = os.environ.get("WTD_OBS_CONFIG", "/obs-websocket/config.json")
NGINX_CONF = os.environ.get("WTD_NGINX_CONF", "/relay/nginx.conf")
# What OBS streams to (nginx.conf): the server and key the page sets in OBS.
RELAY_SERVER, RELAY_KEY = "rtmp://127.0.0.1:1936/live", "wtd"
SOURCE = RELAY_SERVER + "/" + RELAY_KEY
SLOT = {"realm": "OBS_WEBSOCKET_DATA_REALM_GLOBAL", "slotName": "wtd-destinations"}
TEST_FLAG = "?bandwidthtest=true"
# Each destination's ingest, with the key going on the end: Twitch's global ingest (the one OBS's "Auto" uses) and
# YouTube's RTMPS ingest.
INGEST = {
    "twitch": os.environ.get("WTD_TWITCH_INGEST", "rtmp://ingest.global-contribute.live-video.net/app/"),
    "youtube": os.environ.get("WTD_YOUTUBE_INGEST", "rtmps://a.rtmps.youtube.com:443/live2/"),
}
LABEL = {"twitch": "Twitch", "youtube": "YouTube"}
EVENTS = (1 << 0) | (1 << 6)  # General (CustomEvent, ExitStarted) and Outputs (StreamStateChanged)
REPORT_EVERY = 2.0
CHECK_EVERY = 15.0    # read the keys and OBS's stream state again even without an event
TEST_WINDOW = 60.0
LIVE_WINDOW = 5.0     # a destination is live while its ffmpeg has sent something this recently
STOP = threading.Event()


def log(msg):
    print(time.strftime("%Y-%m-%d %H:%M:%S"), msg, flush=True)


def why(e):
    return str(e) or type(e).__name__


def redact(text, key):
    """ffmpeg names the URL it failed on, key and all: the key never reaches the log or the report."""
    if key:
        text = text.replace(key, "<key>")
    return re.sub(r"(/(?:app|live2)/)[^\s'\"?]+", r"\1<key>", text)


def obs_password():
    try:
        with open(OBS_CONFIG, encoding="utf-8") as f:
            return json.load(f).get("server_password") or ""
    except (OSError, ValueError):
        return ""


class ObsError(Exception):
    """OBS turned a request down; the connection itself is fine."""


class Obs:
    """An obs-websocket v5 connection: the Hello/Identify handshake, requests, and the events that arrive between."""

    def __init__(self, url, password):
        self.ws = websocket.create_connection(url, timeout=10, subprotocols=["obswebsocket.json"])
        self.ids = itertools.count(1)
        self.events = collections.deque()
        hello = json.loads(self.ws.recv())["d"]
        identify = {"rpcVersion": 1, "eventSubscriptions": EVENTS}
        auth = hello.get("authentication")
        if auth:
            if not password:
                self.close()
                raise ConnectionError("OBS wants its WebSocket password, and %s has none" % OBS_CONFIG)
            sha = lambda s: base64.b64encode(hashlib.sha256(s.encode()).digest()).decode()
            identify["authentication"] = sha(sha(password + auth["salt"]) + auth["challenge"])
        self.ws.send(json.dumps({"op": 1, "d": identify}))
        if json.loads(self.ws.recv()).get("op") != 2:
            self.close()
            raise ConnectionError("OBS didn't accept the relay")
        self.ws.settimeout(1)

    def call(self, request_type, **data):
        rid = str(next(self.ids))
        self.ws.send(json.dumps({"op": 6, "d": {"requestType": request_type, "requestId": rid, "requestData": data}}))
        deadline = time.monotonic() + 10
        while time.monotonic() < deadline:
            try:
                msg = json.loads(self.ws.recv())
            except websocket.WebSocketTimeoutException:
                continue
            if msg.get("op") == 5:
                self.events.append(msg["d"])
            elif msg.get("op") == 7 and msg["d"].get("requestId") == rid:
                status = msg["d"]["requestStatus"]
                if not status.get("result"):
                    raise ObsError("%s: %s" % (request_type, status.get("comment") or status.get("code")))
                return msg["d"].get("responseData") or {}
        raise TimeoutError("OBS didn't answer " + request_type)

    def next_event(self, timeout):
        if self.events:
            return self.events.popleft()
        self.ws.settimeout(timeout)
        try:
            msg = json.loads(self.ws.recv())
        except websocket.WebSocketTimeoutException:
            return None
        finally:
            self.ws.settimeout(1)
        return msg["d"] if msg.get("op") == 5 else None

    def close(self):
        try:
            self.ws.close()
        except Exception:
            pass


class Leg:
    """One destination: an ffmpeg sending the stream there, started again with a growing wait whenever it stops."""

    def __init__(self, name):
        self.name = name
        self.label = LABEL[name]
        self.url = None           # the full URL, key and all: never logged or reported
        self.key = ""
        self.proc = None
        self.readers = []
        self.failures = 0
        self.retry_at = 0.0
        self.error = ""
        self.live_since = None
        self.sent_at = 0.0        # when the bytes sent last went up
        self.samples = collections.deque(maxlen=12)  # this run's (time, bytes sent)
        self.tail = collections.deque(maxlen=6)      # this run's last ffmpeg messages, keys taken out

    def reconcile(self, url, key, now):
        if url != self.url:
            if self.proc:
                self.stop()
                log("%s: stopped." % self.label)
            self.url, self.key = url, key
            self.failures, self.retry_at, self.error = 0, 0.0, ""
        if self.url and not self.proc and now >= self.retry_at:
            self.start(now)

    def start(self, now):
        # Each run gets its own buffers, so a finished run's readers can't write into the next one's.
        self.samples = collections.deque(maxlen=12)
        self.tail = collections.deque(maxlen=6)
        self.live_since, self.sent_at = None, 0.0
        cmd = ["ffmpeg", "-hide_banner", "-nostdin", "-nostats", "-loglevel", "error",
               "-progress", "pipe:1", "-stats_period", "1",
               "-rw_timeout", "15000000", "-i", SOURCE,
               "-map", "0", "-c", "copy", "-f", "flv", "-flvflags", "no_duration_filesize",
               "-rw_timeout", "15000000", self.url]
        try:
            proc = subprocess.Popen(cmd, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        except OSError as e:
            self.error, self.retry_at = "couldn't start ffmpeg: %s" % e, now + 30
            log("%s: %s" % (self.label, self.error))
            return
        self.proc = proc
        self.readers = [threading.Thread(target=self._read_progress, args=(proc, self.samples), daemon=True),
                        threading.Thread(target=self._read_errors, args=(proc, self.key, self.tail), daemon=True)]
        for reader in self.readers:
            reader.start()
        log("%s: connecting%s." % (self.label, " (bandwidth test)" if self.url.endswith(TEST_FLAG) else ""))

    def stop(self):
        proc, self.proc = self.proc, None
        if proc and proc.poll() is None:
            proc.terminate()
            try:
                proc.wait(5)
            except subprocess.TimeoutExpired:
                proc.kill()
                proc.wait()
        self.live_since, self.sent_at = None, 0.0

    def poll(self, now):
        proc = self.proc
        if proc is None or proc.poll() is None:
            if proc and self.live_since is None and self.sent_at and now - self.sent_at < LIVE_WINDOW:
                self.live_since = now
                log("%s: live." % self.label)
            return
        self.proc = None
        for reader in self.readers:
            reader.join(1)    # let ffmpeg's last words in
        lasted = now - self.live_since if self.live_since else 0.0
        self.live_since, self.sent_at = None, 0.0
        # A long good run starts the waits over; otherwise each failure in a row waits longer, up to 30 s.
        self.failures = 1 if lasted > 60 else self.failures + 1
        wait = min(30, 2 ** self.failures)
        self.retry_at = now + wait
        self.error = self.describe(proc, lasted)
        log("%s: dropped after %d s (%s); trying again in %d s." % (self.label, lasted, self.error, wait))

    def describe(self, proc, lasted):
        lines = [re.sub(r"^\[[^\]]* @ 0x[0-9a-f]+\]\s*", "", line) for line in self.tail]
        text = lines[-1] if lines else "ffmpeg stopped (exit %s)" % proc.returncode
        # Twitch and YouTube both hang up on a key they don't know as the stream starts, which ffmpeg calls only this.
        if not lasted and text == "Error opening output files: Input/output error":
            text = "it refused the stream as it started, which usually means a wrong or reset key"
        return text[:200]

    def state(self, now):
        if not self.url:
            return "off"
        if not self.proc:
            return "retrying"
        if self.sent_at and now - self.sent_at < LIVE_WINDOW:
            return "live"
        return "stalled" if self.live_since else "connecting"

    def kbps(self, now):
        recent = [s for s in list(self.samples) if now - s[0] <= 5]
        if len(recent) < 2 or recent[-1][0] <= recent[0][0]:
            return 0
        return round((recent[-1][1] - recent[0][1]) * 8 / (recent[-1][0] - recent[0][0]) / 1000)

    def _read_progress(self, proc, samples):
        size = None
        for raw in proc.stdout:
            line = raw.decode("utf-8", "replace").strip()
            if line.startswith("total_size="):
                try:
                    size = int(line.split("=", 1)[1])
                except ValueError:
                    size = None
            elif line.startswith("progress=") and size is not None:
                now = time.monotonic()
                if (not samples or size > samples[-1][1]) and proc is self.proc:
                    self.sent_at = now
                samples.append((now, size))

    def _read_errors(self, proc, key, tail):
        for raw in proc.stderr:
            line = redact(raw.decode("utf-8", "replace").strip(), key)
            if line:
                tail.append(line)
                log("%s: ffmpeg: %s" % (self.label, line))


class Relay:
    def __init__(self):
        self.legs = {name: Leg(name) for name in INGEST}
        self.obs = None
        self.next_connect = 0.0
        self.last_problem = None
        self.settings = {}
        self.feeding = False      # OBS is streaming, to the relay
        self.testing = False      # ...and this stream is Twitch's bandwidth test
        self.test_armed_until = 0.0
        self.next_report = 0.0
        self.next_check = 0.0

    def connect(self):
        try:
            self.obs = Obs(OBS_URL, obs_password())
        except Exception as e:
            problem = "Can't reach OBS's WebSocket at %s (%s); trying again." % (OBS_URL, why(e))
            if problem != self.last_problem:
                log(problem)
            self.last_problem = problem
            self.next_connect = time.monotonic() + 5
            return
        self.last_problem = None
        self.next_check = self.next_report = 0.0
        log("Connected to OBS at %s." % OBS_URL)

    def drop(self, e):
        log("Lost OBS's WebSocket (%s); trying again." % why(e))
        self.obs.close()
        self.obs = None
        self.next_connect = time.monotonic() + 3

    def check(self):
        self.next_check = time.monotonic() + CHECK_EVERY
        self.read_settings()
        self.read_feeding()

    def read_settings(self):
        value = self.obs.call("GetPersistentData", **SLOT).get("slotValue")
        self.settings = value if isinstance(value, dict) else {}

    def destination(self, name):
        cfg = self.settings.get(name)
        cfg = cfg if isinstance(cfg, dict) else {}
        return bool(cfg.get("on", True)), str(cfg.get("key") or "").strip()

    def read_feeding(self):
        status = self.obs.call("GetStreamStatus")
        service = self.obs.call("GetStreamServiceSettings")
        ss = service.get("streamServiceSettings") or {}
        to_relay = (service.get("streamServiceType") == "rtmp_custom" and ss.get("key") == RELAY_KEY
                    and str(ss.get("server") or "").rstrip("/") == RELAY_SERVER)
        feeding = bool(status.get("outputActive")) and to_relay
        if feeding == self.feeding:
            return
        if feeding:
            self.testing = time.monotonic() < self.test_armed_until
            self.test_armed_until = 0.0
            self.read_settings()
            log("OBS is streaming to the relay%s." % (": Twitch's private bandwidth test" if self.testing else ""))
        else:
            self.testing = False
            log("OBS stopped streaming to the relay.")
        self.feeding = feeding

    def wanted(self, name):
        on, key = self.destination(name)
        if not (self.feeding and on and key):
            return None, key
        if self.testing:
            return (INGEST[name] + key + TEST_FLAG, key) if name == "twitch" else (None, key)
        return INGEST[name] + key, key

    def reconcile(self):
        now = time.monotonic()
        for name, leg in self.legs.items():
            leg.poll(now)
            url, key = self.wanted(name)
            leg.reconcile(url, key, now)

    def report(self):
        now = time.monotonic()
        self.next_report = now + REPORT_EVERY
        destinations = {}
        for name, leg in self.legs.items():
            on, key = self.destination(name)
            destinations[name] = {
                "on": on, "key": bool(key), "state": leg.state(now), "kbps": leg.kbps(now),
                "liveFor": int(now - leg.live_since) if leg.live_since else 0,
                "retryIn": max(0, round(leg.retry_at - now)) if leg.url and not leg.proc else 0,
                "error": leg.error,
            }
        self.obs.call("BroadcastCustomEvent", eventData={"wtdRelay": {
            "v": 1, "feeding": self.feeding, "test": self.testing, "testArmed": now < self.test_armed_until,
            "destinations": destinations}})

    def on_event(self, event):
        kind, data = event.get("eventType"), event.get("eventData") or {}
        if kind == "CustomEvent" and "wtdRelayCommand" in data:
            command = data["wtdRelayCommand"]
            if command == "reload":
                self.read_settings()
            elif command == "test":
                self.test_armed_until = time.monotonic() + TEST_WINDOW
                log("Twitch's bandwidth test is set for the next stream.")
            elif command == "endtest":
                self.test_armed_until = 0.0
            self.reconcile()
            self.report()
        elif kind == "StreamStateChanged":
            self.read_feeding()
        elif kind == "ExitStarted":
            raise ConnectionError("OBS is shutting down")

    def run(self, nginx):
        while not STOP.is_set():
            if nginx.poll() is not None:
                log("nginx stopped (exit %s)." % nginx.returncode)
                return 1
            if self.obs is None and time.monotonic() >= self.next_connect:
                self.connect()
            if self.obs is None:
                # Without OBS, destinations already sending carry on as they were.
                self.reconcile()
                STOP.wait(0.5)
                continue
            try:
                if time.monotonic() >= self.next_check:
                    self.check()
                event = self.obs.next_event(0.5)
                if event:
                    self.on_event(event)
                self.reconcile()
                if time.monotonic() >= self.next_report:
                    self.report()
            except ObsError as e:
                log("OBS turned down a request: %s" % e)
                self.next_check = self.next_report = time.monotonic() + 5
            except (OSError, ValueError, KeyError, websocket.WebSocketException) as e:
                self.drop(e)
        return 0

    def shutdown(self):
        for leg in self.legs.values():
            leg.stop()
        if self.obs:
            self.obs.close()


def main():
    signal.signal(signal.SIGTERM, lambda *_: STOP.set())
    signal.signal(signal.SIGINT, lambda *_: STOP.set())
    nginx = subprocess.Popen(["nginx", "-e", "stderr", "-c", NGINX_CONF])
    log("Relay up: OBS streams to %s; it goes on to %s." % (SOURCE, " and ".join(LABEL.values())))
    relay = Relay()
    try:
        return relay.run(nginx)
    finally:
        relay.shutdown()
        if nginx.poll() is None:
            nginx.terminate()
            try:
                nginx.wait(5)
            except subprocess.TimeoutExpired:
                nginx.kill()


if __name__ == "__main__":
    raise SystemExit(main())
