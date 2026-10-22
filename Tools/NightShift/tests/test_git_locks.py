"""Tests for how the night shift's git calls treat locks (index.lock, HEAD.lock): one in use is waited for, one left
behind by a git that was killed is deleted. Run on Linux (the coordinator needs fcntl):
    python3 -m unittest discover -s Tools/NightShift/tests -p "test_*.py" -v
"""
from __future__ import annotations

import contextlib
import importlib.util
import io
import os
import shutil
import subprocess
import sys
import tempfile
import time
import unittest
from pathlib import Path
from unittest import mock

HERE = Path(__file__).resolve().parent
_spec = importlib.util.spec_from_file_location("nightshift", HERE.parent / "nightshift.py")
ns = importlib.util.module_from_spec(_spec)
sys.modules["nightshift"] = ns
_spec.loader.exec_module(ns)

BRANCH = "nightshift/2026-01-01"


class GitLockTest(unittest.TestCase):
    def setUp(self):
        self.tmp = Path(tempfile.mkdtemp(prefix="nightshift-locks-"))
        self.repo = self.tmp / "repo"
        self.repo.mkdir()
        mock.patch.dict(os.environ, {"GIT_CONFIG_GLOBAL": os.devnull, "GIT_CONFIG_NOSYSTEM": "1", "LC_ALL": "C"}).start()
        self.sleep = mock.patch.object(ns.time, "sleep").start()  # the waits are what's tested, not slept through
        self.addCleanup(mock.patch.stopall)
        self.addCleanup(shutil.rmtree, self.tmp, True)
        self.sh("init", "-q", "-b", BRANCH)
        (self.repo / "a.txt").write_text("a\n")
        self.index_lock = self.repo / ".git" / "index.lock"
        self.head_lock = self.repo / ".git" / "HEAD.lock"
        self.out = io.StringIO()

    def sh(self, *args: str) -> str:
        return subprocess.run(["git", "-C", str(self.repo), "-c", "user.name=t", "-c", "user.email=t@t", *args],
                              check=True, capture_output=True, text=True).stdout

    @staticmethod
    def left_behind(lock: Path, minutes: float = 420) -> None:
        """A partly written lock that nothing has touched for `minutes`, as a git killed mid-write leaves it."""
        lock.write_bytes(b"DIRC" + b"\0" * 4096)
        then = time.time() - minutes * 60
        os.utime(lock, (then, then))

    def refresh(self) -> None:
        coordinator = object.__new__(ns.Coordinator)  # sync_landed_checkout needs none of the coordinator's state
        with mock.patch.multiple(ns, ROOT=self.repo, TASKS=self.tmp / "no-tasks.md", STATE=self.tmp / "state",
                                 TASKS_LOCK_PATH=self.tmp / "state" / "tasks.lock"), \
                contextlib.redirect_stdout(self.out):
            coordinator.sync_landed_checkout(BRANCH)

    def test_a_lock_left_behind_is_deleted_and_the_command_goes_on(self):
        self.left_behind(self.index_lock)
        with contextlib.redirect_stdout(self.out):
            ns.git(self.repo, "add", "-A")
        self.assertFalse(self.index_lock.exists())
        self.assertEqual("a.txt\n", self.sh("diff", "--cached", "--name-only"))
        self.sleep.assert_not_called()
        self.assertIn("index.lock: a git command that was killed left it behind", self.out.getvalue())

    def test_a_ref_lock_left_behind_is_deleted_too(self):
        self.sh("add", "-A")
        self.left_behind(self.head_lock)
        with contextlib.redirect_stdout(self.out):
            sha = ns.git_commit(self.repo, "Night shift: queue and project map")
        self.assertEqual(sha, self.sh("rev-parse", "HEAD").strip())
        self.assertFalse(self.head_lock.exists())

    def test_a_lock_in_use_is_waited_for_and_kept(self):
        self.index_lock.write_bytes(b"")  # a git that's working right now
        with contextlib.redirect_stdout(self.out):
            r = ns.git_run(self.repo, "add", "-A")
        self.assertNotEqual(0, r.returncode)
        self.assertIn("File exists", r.stderr)
        self.assertTrue(self.index_lock.exists())
        self.assertEqual(7, self.sleep.call_count)
        self.assertEqual("", self.out.getvalue())

    def test_a_lock_is_only_stale_after_the_threshold(self):
        self.left_behind(self.index_lock, minutes=ns.STALE_LOCK_SECONDS / 60 - 1)
        self.assertFalse(ns.clear_stale_lock(self.index_lock))
        self.assertTrue(self.index_lock.exists())
        self.assertTrue(ns.clear_stale_lock(self.tmp / "released-meanwhile.lock"))

    def test_the_landing_refresh_deletes_a_head_lock_left_behind(self):
        self.sh("add", "-A")
        self.sh("commit", "-q", "-m", "start")
        self.left_behind(self.head_lock)
        self.refresh()
        self.assertFalse(self.head_lock.exists())
        self.sleep.assert_not_called()
        self.assertIn("HEAD.lock: a git command that was killed left it behind", self.out.getvalue())

    def test_the_landing_refresh_waits_for_a_head_lock_in_use(self):
        self.sh("add", "-A")
        self.sh("commit", "-q", "-m", "start")
        self.head_lock.write_bytes(b"")
        with self.assertRaisesRegex(RuntimeError, "stayed busy"):
            self.refresh()
        self.assertTrue(self.head_lock.exists())
        self.assertEqual(30, self.sleep.call_count)


if __name__ == "__main__":
    unittest.main()
