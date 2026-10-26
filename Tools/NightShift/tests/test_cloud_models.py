"""Tests for the night shift on one of OpenCode's own models (opencode/<id>, such as Big Pickle) and for the switch to
NIGHTSHIFT_FALLBACK_MODEL once it stops answering. OpenCode, ssh, git and Ollama are faked; nothing reaches the cloud.
    python3 -m unittest discover -s Tools/NightShift/tests -p "test_*.py" -v
"""
from __future__ import annotations

import base64
import importlib.util
import io
import json
import re
import shutil
import subprocess
import sys
import tempfile
import threading
import types
import unittest
from pathlib import Path
from unittest import mock

HERE = Path(__file__).resolve().parent
_spec = importlib.util.spec_from_file_location("nightshift", HERE.parent / "nightshift.py")
ns = importlib.util.module_from_spec(_spec)
sys.modules["nightshift"] = ns
_spec.loader.exec_module(ns)

BIG_PICKLE = "opencode/big-pickle"
FALLBACK = "gpt-oss:120b"
# What OpenCode 1.19.3 logged (with LOG_ERRORS) when Big Pickle's free usage limit was reached. It then retried without
# another word, for as long as it was left running.
LIMIT_LINE = ('timestamp=2026-10-26T02:40:19.009Z level=ERROR run=0040c30c message="stream error" providerID=opencode '
              'modelID=big-pickle session.id=ses_e89eb7c36ffeDGkU4B2SW551Np small=false agent=build mode=primary '
              'error.error="AI_APICallError: Free usage limit reached. Free models work in OpenCode (https://opencode.ai); '
              'subscribe to OpenCode Go to keep going: https://opencode.ai/go"\n')


def event(kind: str, message: str = "m1", **part) -> str:
    """One line of `opencode run --format json`."""
    return json.dumps({"type": kind, "sessionID": "s1", "part": dict(part, messageID=message)}) + "\n"


class FakeOpenCode:
    """Stands in for subprocess.Popen running `opencode run`: records how it was started, its settings and what was piped
    to it, then prints `lines`. Like OpenCode, it reads a piped prompt before printing anything. It has always finished
    already, so terminate() never signals a real process."""

    def __init__(self, lines: list[str]):
        self.lines = lines
        self.started: list[tuple[list, dict, dict | None]] = []  # (command line, Popen's arguments, its opencode.json)
        self.sent: list[str] = []
        self.read = 0  # lines read so far

    def __call__(self, cmd, **kwargs):
        fake, piped = self, threading.Event()
        cfg_path = (kwargs.get("env") or {}).get("OPENCODE_CONFIG")
        config = json.loads(Path(cfg_path).read_text(encoding="utf-8")) if cfg_path and Path(cfg_path).exists() else None
        self.started.append((cmd, kwargs, config))
        if kwargs.get("stdin") != subprocess.PIPE:
            piped.set()

        class Stdin:
            def write(self, text):
                fake.sent.append(text)

            def close(self):
                piped.set()

        def stdout():
            piped.wait(5)
            for line in fake.lines:
                fake.read += 1
                yield line

        return types.SimpleNamespace(pid=4242, returncode=0, stdin=Stdin(), stdout=stdout(), poll=lambda: 0,
                                     wait=lambda timeout=None: 0)


class AskingTest(unittest.TestCase):
    def test_model_names(self):
        self.assertTrue(ns.is_cloud(BIG_PICKLE))
        self.assertFalse(ns.is_cloud(FALLBACK))
        self.assertEqual(BIG_PICKLE, ns.opencode_model(BIG_PICKLE))
        self.assertEqual("ollama/gpt-oss:120b", ns.opencode_model(FALLBACK))
        self.assertEqual("Free usage limit reached", ns.why_out(LIMIT_LINE))

    def test_the_agents_settings_name_a_cloud_model_as_opencode_does(self):
        cfg = ns.opencode_config("http://ollama:11434", BIG_PICKLE, 131072, Path("repo"))
        self.assertEqual((BIG_PICKLE, BIG_PICKLE), (cfg["model"], cfg["small_model"]))
        models = cfg["provider"]["ollama"]["models"]
        self.assertNotIn(BIG_PICKLE, models)  # OpenCode knows its own model's limits
        self.assertIn(FALLBACK, models)  # the fallback stays at hand
        local = ns.opencode_config("http://ollama:11434", FALLBACK, 131072, Path("repo"))
        self.assertEqual("ollama/gpt-oss:120b", local["model"])
        self.assertEqual({"context": 131072, "output": 32768}, local["provider"]["ollama"]["models"][FALLBACK]["limit"])

    def test_a_cloud_model_is_asked_through_opencode_with_the_prompt_piped_in(self):
        fake = FakeOpenCode([event("step_start"), event("text", "m1", text="Let me think."),
                             event("text", "m2", text="<think>hmm</think>- [ ] A task"), event("text", "m2", text="  Step 1")])
        prompt = "Plan a task.\n" + "x" * 200_000  # longer than one command-line argument may be on Linux
        with mock.patch.object(ns.subprocess, "Popen", fake), mock.patch.object(ns, "opencode_bin", return_value="opencode"):
            reply = ns.ask_model("http://ollama", BIG_PICKLE, prompt)
        self.assertEqual("- [ ] A task\n  Step 1", reply)  # the last message, without its reasoning
        cmd, kwargs, config = fake.started[0]
        self.assertEqual(["opencode", "run", "--format", "json", "--model", BIG_PICKLE], cmd[:6])
        self.assertEqual(ns.ASK_MESSAGE, cmd[-1])
        for flag in ns.LOG_ERRORS:
            self.assertIn(flag, cmd)
        self.assertNotIn(prompt, cmd)
        self.assertEqual([prompt], fake.sent)
        # Read-only tools (Big Pickle's free tier turns away a request with none), nothing outside the empty folder.
        permission = config["permission"]
        self.assertEqual(("deny", "deny"), (permission["*"], permission["external_directory"]))
        self.assertEqual("allow", permission["read"])
        self.assertFalse({"edit", "write", "bash", "webfetch", "task"} & set(permission))
        self.assertEqual(ns.ASK_STEPS, config["agent"]["build"]["steps"])
        self.assertEqual(BIG_PICKLE, config["model"])
        self.assertEqual("work", Path(kwargs["cwd"]).name)
        self.assertTrue(kwargs["start_new_session"])
        self.assertIsNone(kwargs.get("preexec_fn"))  # the coordinator runs other threads

    def test_an_ollama_model_is_still_asked_directly(self):
        asked = []

        def urlopen(req, timeout=None):
            asked.append((req.full_url, json.loads(req.data)))
            return io.BytesIO(json.dumps({"choices": [{"message": {"content": "<think>x</think> Fine "}}]}).encode())

        with mock.patch.object(ns.urllib.request, "urlopen", urlopen), \
                mock.patch.object(ns.subprocess, "Popen", side_effect=AssertionError("OpenCode isn't needed")):
            self.assertEqual("Fine", ns.ask_model("http://ollama", FALLBACK, "Plan."))
        self.assertEqual(("http://ollama/v1/chat/completions", FALLBACK), (asked[0][0], asked[0][1]["model"]))

    def test_asking_stops_at_once_when_the_free_usage_limit_is_reached(self):
        fake = FakeOpenCode([LIMIT_LINE, event("text", text="never read")])
        with mock.patch.object(ns.subprocess, "Popen", fake), mock.patch.object(ns, "opencode_bin", return_value="opencode"):
            with self.assertRaises(ns.ModelOut) as caught:
                ns.ask_model("http://ollama", BIG_PICKLE, "Plan.")
        self.assertEqual((BIG_PICKLE, "Free usage limit reached"), (caught.exception.model, caught.exception.why))
        self.assertEqual(1, fake.read)  # it didn't wait out OpenCode's silent retries

    def test_an_error_event_from_opencode_raises(self):
        fake = FakeOpenCode([event("error", error={"name": "APIError", "message": "bad request"})])
        with mock.patch.object(ns.subprocess, "Popen", fake), mock.patch.object(ns, "opencode_bin", return_value="opencode"):
            with self.assertRaises(RuntimeError) as caught:
                ns.ask_model("http://ollama", BIG_PICKLE, "Plan.")
        self.assertNotIsInstance(caught.exception, ns.ModelOut)

    def test_warming_a_cloud_model_asks_it_for_a_word(self):
        with mock.patch.object(ns, "ask_opencode", return_value="OK") as ask, \
                mock.patch.object(ns.urllib.request, "urlopen", side_effect=AssertionError("Ollama isn't needed")):
            ns.warm_model("http://ollama", BIG_PICKLE)
        self.assertEqual(BIG_PICKLE, ask.call_args[0][0])

    def test_has_model_asks_opencode_about_its_own(self):
        listed = subprocess.CompletedProcess([], 0, "opencode/beam-free\n\x1b[0mopencode/big-pickle\x1b[0m\n", "")
        with mock.patch.object(ns, "opencode_bin", return_value="opencode"), \
                mock.patch.object(ns.subprocess, "run", return_value=listed) as run:
            self.assertTrue(ns.has_model("http://ollama", BIG_PICKLE))
            self.assertFalse(ns.has_model("http://ollama", "opencode/withdrawn"))
        self.assertEqual(["opencode", "models", "opencode"], run.call_args[0][0])

    def test_ollama_isnt_needed_when_every_model_is_opencodes(self):
        cfg = dict(ns.DEFAULTS, NIGHTSHIFT_MODEL=BIG_PICKLE, NIGHTSHIFT_HELPER_MODEL="",
                   NIGHTSHIFT_HELPER_OLLAMA_URL="http://spark:11434")
        with mock.patch.object(ns.urllib.request, "urlopen", side_effect=OSError("down")), \
                mock.patch.object(ns, "default_gateway", return_value=None):
            self.assertEqual("http://spark:11434", ns.find_ollama(cfg))
            with self.assertRaises(RuntimeError):  # the fallback runs in Ollama
                ns.find_ollama(dict(cfg, NIGHTSHIFT_FALLBACK_MODEL=FALLBACK))


class AgentTest(unittest.TestCase):
    def setUp(self):
        self.tmp = Path(tempfile.mkdtemp(prefix="nightshift-cloud-"))
        mock.patch.object(ns, "opencode_bin", return_value="opencode").start()
        mock.patch.object(ns, "bash_exe", return_value="bash").start()
        mock.patch.object(ns, "log").start()
        self.addCleanup(mock.patch.stopall)
        self.addCleanup(shutil.rmtree, self.tmp, True)

    def agent(self, model: str, lines: list[str]) -> tuple[ns.AgentRun, list]:
        fake = FakeOpenCode(lines)
        with mock.patch.object(ns.subprocess, "Popen", fake):
            run = ns.run_opencode(self.tmp, "Do the task.", self.tmp / "opencode.json", model, 600, self.tmp / "run.jsonl", {})
        return run, fake.started[0][0]

    def test_an_agent_is_stopped_when_the_free_usage_limit_is_reached(self):
        run, cmd = self.agent(BIG_PICKLE, [event("step_start"), LIMIT_LINE])
        self.assertEqual((True, "Free usage limit reached"), (run.model_out, run.error))
        self.assertEqual(BIG_PICKLE, cmd[cmd.index("--model") + 1])
        for flag in ns.LOG_ERRORS:
            self.assertIn(flag, cmd)

    def test_an_agent_on_an_ollama_model_runs_as_before(self):
        run, cmd = self.agent(FALLBACK, [event("text", text="Done.")])
        self.assertEqual("ollama/gpt-oss:120b", cmd[cmd.index("--model") + 1])
        self.assertNotIn("--print-logs", cmd)
        self.assertEqual((False, "Done."), (run.model_out, run.text))

    def test_a_task_whose_model_stops_answering_goes_back_as_it_was(self):
        repo = self.tmp / "repo"
        repo.mkdir()
        out = ns.AgentRun(model_out=True, error="Free usage limit reached")
        with mock.patch.object(ns, "git", return_value=""), \
                mock.patch.object(ns, "write_opencode_config", return_value=self.tmp / "opencode.json"), \
                mock.patch.object(ns, "run_opencode", return_value=out), \
                mock.patch.object(ns, "compile_check", side_effect=AssertionError("nothing to check")):
            res = ns.do_task(repo, "base", "A task", BIG_PICKLE, "http://ollama", 10, 2, 4096, "")
        self.assertEqual(("error", True, "Free usage limit reached", BIG_PICKLE),
                         (res.status, res.model_out, res.errors, res.model))


class FallbackTest(unittest.TestCase):
    """The coordinator once a cloud model stops answering, with NIGHTSHIFT_FALLBACK_MODEL to switch to."""

    def setUp(self):
        self.tmp = Path(tempfile.mkdtemp(prefix="nightshift-fallback-"))
        state = self.tmp / "state"
        state.mkdir()
        names = ("TASKS", "STATE", "TASKS_LOCK_PATH", "STATUS_FILE", "FINISH_FLAG", "REPORTS", "SSH_KEY")
        self.saved = {name: getattr(ns, name) for name in names}
        ns.TASKS = self.tmp / "tasks.md"
        ns.STATE, ns.TASKS_LOCK_PATH, ns.STATUS_FILE = state, state / "tasks.lock", state / "status.json"
        ns.FINISH_FLAG, ns.REPORTS, ns.SSH_KEY = state / "finish-requested", self.tmp / "reports", self.tmp / "id_ed25519"
        ns.SSH_KEY.write_text("key")
        ns.TASKS.write_text("# Night Shift tasks\n\n## Queue\n\n- [ ] Add a lamp to Assets/Scripts/World/Lamp.cs\n",
                            encoding="utf-8")
        ns.STOPPING.clear()
        self.warm = mock.patch.object(ns, "warm_model").start()
        mock.patch.object(ns, "log").start()
        self.addCleanup(mock.patch.stopall)

    def tearDown(self):
        for name, value in self.saved.items():
            setattr(ns, name, value)
        ns.STOPPING.clear()
        shutil.rmtree(self.tmp, ignore_errors=True)

    def coordinator(self, **cfg) -> ns.Coordinator:
        settings = dict(NIGHTSHIFT_MODEL=BIG_PICKLE, NIGHTSHIFT_HELPER_MODEL="", NIGHTSHIFT_FALLBACK_MODEL=FALLBACK,
                        NIGHTSHIFT_SMALL_TASK_MODELS=FALLBACK)
        coord = ns.Coordinator(dict(ns.DEFAULTS, **dict(settings, **cfg)))
        coord.on_branch = lambda: True
        coord.ollama = "http://ollama"
        coord.night = ns.Night("2026-10-26", "nightshift/2026-10-26", BIG_PICKLE)
        return coord

    @staticmethod
    def worker(name: str, model: str = BIG_PICKLE, run=None):
        return types.SimpleNamespace(name=name, model=model, big=True, docs=False, retired=False, errors=0, run=run)

    def test_every_worker_on_the_model_and_the_planner_switch_to_the_fallback(self):
        coord = self.coordinator()
        coord.workers = [self.worker("spark"), self.worker("helper"), self.worker("other", "qwen3.8")]
        coord.planner = types.SimpleNamespace(model=BIG_PICKLE, planned=0)
        self.assertTrue(coord.fall_back(BIG_PICKLE, "Free usage limit reached"))
        self.assertTrue(coord.fall_back(BIG_PICKLE, "Free usage limit reached"))  # the next worker to find out
        self.assertEqual([FALLBACK, FALLBACK, "qwen3.8"], [w.model for w in coord.workers])
        self.assertEqual([False, False, True], [w.big for w in coord.workers])  # FALLBACK takes only small tasks here
        self.assertEqual((FALLBACK, FALLBACK), (coord.planner.model, coord.model))
        self.warm.assert_called_once_with("http://ollama", FALLBACK)
        self.assertEqual(1, len(coord.night.switches))
        self.assertIn(f"to `{FALLBACK}`: `{BIG_PICKLE}` stopped answering (Free usage limit reached)",
                      coord.night.switches[0])
        self.assertEqual(f"{FALLBACK}, {BIG_PICKLE}", coord.night.model)

    def test_without_a_fallback_nothing_switches(self):
        coord = self.coordinator(NIGHTSHIFT_FALLBACK_MODEL="")
        coord.workers = [self.worker("spark")]
        self.assertFalse(coord.fall_back(BIG_PICKLE, "Free usage limit reached"))
        self.assertEqual(BIG_PICKLE, coord.workers[0].model)
        self.warm.assert_not_called()

    def test_a_task_the_model_stopped_on_starts_again_on_the_fallback_without_counting_as_a_try(self):
        coord = self.coordinator(NIGHTSHIFT_WAIT_FOR_TASKS="0")
        ran, landed = [], []
        results = [ns.Result(status="error", errors="Free usage limit reached", model=BIG_PICKLE, model_out=True),
                   ns.Result(status="nochange", summary="Nothing to add.", model=FALLBACK)]

        def run(task, base, minutes, model, fix_rounds, context):
            ran.append(model)
            return results[len(ran) - 1]

        def land(worker, task, res):
            landed.append(res.status)
            if res.status != "error":
                coord.completed.add(task.key)

        spark = self.worker("spark", run=run)
        coord.workers = [spark]
        coord.land = land
        with mock.patch.object(ns, "git", return_value="abc123"):
            coord.take_tasks(spark)
        self.assertEqual([BIG_PICKLE, FALLBACK], ran)
        self.assertEqual(["error", "nochange"], landed)
        self.assertEqual(1, coord.dispatches["Add a lamp to Assets/Scripts/World/Lamp.cs"])  # the stopped try didn't count
        self.assertEqual(0, spark.errors)

    def test_without_a_fallback_the_worker_stops_and_the_task_stays_queued(self):
        coord = self.coordinator(NIGHTSHIFT_FALLBACK_MODEL="")
        ran = []

        def run(task, base, minutes, model, fix_rounds, context):
            ran.append(model)
            return ns.Result(status="error", errors="Free usage limit reached", model=BIG_PICKLE, model_out=True)

        spark = self.worker("spark", run=run)
        coord.workers = [spark]
        coord.land = lambda worker, task, res: None
        with mock.patch.object(ns, "git", return_value="abc123"):
            coord.take_tasks(spark)
        self.assertEqual([BIG_PICKLE], ran)
        self.assertEqual(0, coord.dispatches["Add a lamp to Assets/Scripts/World/Lamp.cs"])

    def test_the_planner_moves_to_the_fallback_when_its_model_stops_answering(self):
        coord = self.coordinator()
        coord.workers = [self.worker("spark")]

        class FakePlanner:
            model, planned, asked = BIG_PICKLE, 0, []

            def plan_one(self, small=False):
                self.asked.append(self.model)
                if self.model == BIG_PICKLE:
                    raise ns.ModelOut(BIG_PICKLE, "Free usage limit reached")
                coord.planning.clear()  # one task from the fallback is enough
                self.planned += 1
                return ns.Task("A planned task", "A planned task")

        coord.planner = FakePlanner()
        coord.planning.set()
        coord.plan_loop()
        self.assertEqual([BIG_PICKLE, FALLBACK], coord.planner.asked)
        self.assertEqual(FALLBACK, coord.workers[0].model)

    def test_the_night_starts_on_the_cloud_model_when_it_answers(self):
        coord = self.coordinator()
        with mock.patch.object(ns, "has_model", return_value=True):
            self.assertEqual(BIG_PICKLE, coord.start_model(BIG_PICKLE))
        self.warm.assert_called_once_with("http://ollama", BIG_PICKLE)  # asked for a word (see warm_model)
        self.assertEqual(set(), coord.models_out)

    def test_the_night_starts_on_the_fallback_when_the_cloud_model_is_already_out(self):
        def warm(base, model):
            if ns.is_cloud(model):
                raise ns.ModelOut(model, "Free usage limit reached")

        self.warm.side_effect = warm
        coord = self.coordinator()
        with mock.patch.object(ns, "has_model", return_value=True):
            self.assertEqual(FALLBACK, coord.start_model(BIG_PICKLE))
            alone = self.coordinator(NIGHTSHIFT_FALLBACK_MODEL="")
            with self.assertRaises(RuntimeError) as caught:
                alone.start_model(BIG_PICKLE)
        self.assertEqual({BIG_PICKLE}, coord.models_out)
        self.assertIn("Free usage limit reached", coord.night.switches[0])
        self.assertIn("Free usage limit reached", str(caught.exception))

    def test_the_night_starts_on_the_fallback_when_opencode_no_longer_offers_the_model(self):
        coord = self.coordinator()
        with mock.patch.object(ns, "has_model", return_value=False):
            self.assertEqual(FALLBACK, coord.start_model(BIG_PICKLE))
        self.assertIn("OpenCode doesn't offer it here", coord.night.switches[0])

    def test_the_report_says_where_the_models_ran(self):
        local = ns.Night("2026-10-24", "nightshift/2026-10-24", "gpt-oss:120b, qwen3.8")
        local.write()
        text = local.file.read_text(encoding="utf-8")
        self.assertIn("model `gpt-oss:120b, qwen3.8` in Ollama on the DGX Spark (no cloud tokens) · ", text)
        self.assertIn("tokens processed locally (", text)
        cloud = ns.Night("2026-10-25", "nightshift/2026-10-25", f"{FALLBACK}, {BIG_PICKLE}")
        cloud.switches.append(f"23:41 to `{FALLBACK}`: `{BIG_PICKLE}` stopped answering (Free usage limit reached)")
        cloud.write()
        text = cloud.file.read_text(encoding="utf-8")
        self.assertIn(f"model `{BIG_PICKLE}` in OpenCode's cloud and `{FALLBACK}` in Ollama on the DGX Spark · ", text)
        self.assertIn("tokens processed (", text)
        self.assertIn(f"Switched at 23:41 to `{FALLBACK}`: `{BIG_PICKLE}` stopped answering", text)

    def test_a_helper_checks_it_can_reach_what_its_models_need(self):
        def prepare(**cfg) -> str:
            sent = []

            def run(cmd, **kwargs):
                sent.append(cmd)
                return subprocess.CompletedProcess(cmd, 0, "READY\n", "")

            w = ns.Worker("hts", "user@hts", dict(ns.DEFAULTS, **cfg), "http://ollama")
            with mock.patch.object(ns.subprocess, "run", run):
                self.assertIsNone(w.prepare())
            return base64.b64decode(re.fullmatch(r"echo (\S+) \| base64 -d \| bash", sent[0][-1]).group(1)).decode()

        both = prepare(NIGHTSHIFT_MODEL=BIG_PICKLE, NIGHTSHIFT_HELPER_MODEL="", NIGHTSHIFT_FALLBACK_MODEL=FALLBACK)
        self.assertIn("https://opencode.ai", both)
        self.assertIn("/api/version", both)  # Ollama, for the fallback
        cloud = prepare(NIGHTSHIFT_MODEL=BIG_PICKLE, NIGHTSHIFT_HELPER_MODEL="")
        self.assertIn("https://opencode.ai", cloud)
        self.assertNotIn("/api/version", cloud)
        local = prepare(NIGHTSHIFT_MODEL=FALLBACK, NIGHTSHIFT_HELPER_MODEL="")
        self.assertNotIn("https://opencode.ai", local)
        self.assertIn("/api/version", local)


if __name__ == "__main__":
    unittest.main()
