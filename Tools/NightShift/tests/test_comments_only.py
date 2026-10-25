"""Tests for documentation tasks, which land the comments they add and nothing else. Run on Linux or Windows:
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
import unittest
from pathlib import Path
from unittest import mock

HERE = Path(__file__).resolve().parent
_spec = importlib.util.spec_from_file_location("nightshift", HERE.parent / "nightshift.py")
ns = importlib.util.module_from_spec(_spec)
sys.modules["nightshift"] = ns
_spec.loader.exec_module(ns)

FOO = """using UnityEngine;

namespace CloneZ
{
    /// <summary>A test script.</summary>
    public class Foo
    {
        /// <summary>Already documented.</summary>
        public int Documented() => 1;

        public int Count { get; private set; }

        public void Tick(float dt)
        {
            // one tick at a time
            Count += 1;
        }

        [SerializeField]
        public void Attributed() { }
    }
}
"""

# What a careless agent makes of FOO: two good summaries, and around them everything a documentation task must not do.
FOO_BY_AGENT = """using UnityEngine;

namespace CloneZ
{
    /// <summary>A test script.</summary>
    public class Foo
    {
        /// <summary>Already documented.</summary>
        /// <summary>Returns one.</summary>
        public int Documented() => 1;

                /// <summary>How many ticks so far.</summary>
public int Count { get; private set; }


        public void Tick(float dt)
        {
            Count += 2;
        }

        /// <summary>Called from the editor.</summary>
        [SerializeField]
        public void Attributed() { }

        /// <summary>A new method.</summary>
        public void Extra() { }
    }
}
// A note after the last line.
"""

# FOO with the two good summaries, indented like the declarations they document, and nothing else changed.
FOO_DOCUMENTED = FOO.replace(
    "        public int Count", "        /// <summary>How many ticks so far.</summary>\n        public int Count").replace(
    "        [SerializeField]", "        /// <summary>Called from the editor.</summary>\n        [SerializeField]")

BAR = "namespace CloneZ\n{\n    public static class Bar\n    {\n    }\n}\n"


class NewCommentsOnlyTest(unittest.TestCase):
    def test_keeps_the_new_summaries_and_puts_back_everything_else(self):
        # Kept: the two summaries. Put back: the stripped indentation, the changed code, the deleted comment and the
        # blank line. Left out: a second summary for Documented, the new method with its summary, the note at the end.
        self.assertEqual(FOO_DOCUMENTED, ns.new_comments_only(FOO, FOO_BY_AGENT))

    def test_comments_only_come_through_unchanged(self):
        self.assertEqual(FOO_DOCUMENTED, ns.new_comments_only(FOO, FOO_DOCUMENTED))
        self.assertEqual(FOO, ns.new_comments_only(FOO, FOO))

    def test_existing_comments_are_never_rewritten(self):
        reworded = FOO.replace("Already documented.", "Returns the number one.")
        self.assertEqual(FOO, ns.new_comments_only(FOO, reworded))
        self.assertEqual(FOO, ns.new_comments_only(FOO, FOO.replace("            // one tick at a time\n", "")))

    def test_a_member_documented_already_gets_no_second_summary(self):
        old = ("    /// <summary>A.</summary>\n    public void A() { }\n"
               "    /// <summary>B.</summary>\n    [Obsolete]\n    public void B() { }\n"
               "    public void C() { }\n")
        new = ("    /// <summary>Another for A, above its own.</summary>\n    /// <summary>A.</summary>\n    public void A() { }\n"
               "    /// <summary>B.</summary>\n    [Obsolete]\n    /// <summary>Another for B, below its attribute.</summary>\n"
               "    public void B() { }\n"
               "    /// <summary>C.</summary>\n    public void C() { }\n")
        self.assertEqual(old.replace("    public void C", "    /// <summary>C.</summary>\n    public void C"),
                         ns.new_comments_only(old, new))

    def test_plain_comments_count_too(self):
        old = "void A()\n{\n    Go();\n}\n"
        self.assertEqual("void A()\n{\n    // Off we go.\n    Go();\n}\n",
                         ns.new_comments_only(old, "void A()\n{\n// Off we go.\n    Go();\n}\n"))

    def test_line_endings_and_byte_order_mark_stay_as_they_were(self):
        old = "\ufeffnamespace CloneZ\r\n{\r\n    public class Foo\r\n    {\r\n        public void A() { }\r\n    }\r\n}\r\n"
        new = "namespace CloneZ\n{\n    public class Foo\n    {\n        /// <summary>Does A.</summary>\n        public void A() { }\n    }\n}\n"
        self.assertEqual(old.replace("        public void A", "        /// <summary>Does A.</summary>\r\n        public void A"),
                         ns.new_comments_only(old, new))

    def test_no_comment_goes_inside_a_string_that_runs_over_several_lines(self):
        old = 'const string Sql = @"\nSELECT * FROM loot\nWHERE kind = 1;\n";\nint x;\n'
        new = ('const string Sql = @"\n// Everything from the loot table.\nSELECT * FROM loot\n// Only crates.\nWHERE kind = 1;\n'
               '";\n// The x.\nint x;\n')
        self.assertEqual(old.replace("int x;", "// The x.\nint x;"), ns.new_comments_only(old, new))
        inside = "var s = $@\"{a}\n{b}\";\n"  # a verbatim interpolated string, holes and all
        self.assertEqual(inside, ns.new_comments_only(inside, inside.replace("{b}", "// b\n{b}")))

    def test_a_comment_at_the_end_of_a_line_of_code_goes_above_it(self):
        old = '    var url = "http://x";\n    Go(url);\n'
        new = '    var url = "http://x"; // the mirror\n    Go(url);   // off it goes\n'
        self.assertEqual('    // the mirror\n    var url = "http://x";\n    // off it goes\n    Go(url);\n',
                         ns.new_comments_only(old, new))
        # Not when the line already ends in a comment (that would be rewording it), nor inside a string.
        commented = "    Go(); // go\n"
        self.assertEqual(commented, ns.new_comments_only(commented, "    Go(); // go, and fast\n"))
        verbatim = 'var s = @"\nend"; Go();\n'
        self.assertEqual(verbatim, ns.new_comments_only(verbatim, 'var s = @"\nend"; Go(); // go\n'))
        # Nor a second summary at the end of a declaration that has one.
        documented = "    /// <summary>A.</summary>\n    public void A() { }\n"
        self.assertEqual(documented, ns.new_comments_only(documented, documented.replace("{ }", "{ } /// <summary>B.</summary>")))


class ScanTest(unittest.TestCase):
    def scan(self, text: str) -> list:
        return ns.scan_cs(text)

    def test_comments_are_found_outside_strings_and_chars_only(self):
        line = 'var s = "a // b \\" c" + @"d "" // e" + \'"\' + \'\\\'\' + $"{f}"; // the end'
        scanned = self.scan(line)[0]
        self.assertEqual(line.index("// the end"), scanned.comment_at)
        self.assertEqual("var s = \"\" + \"\" + ' ' + ' ' + \"\"; ", scanned.code)
        self.assertTrue(scanned.commented)
        self.assertIsNone(self.scan('var u = "http://x";')[0].comment_at)

    def test_braces_in_strings_and_holes_are_not_code(self):
        line = 'var s = $"{(a ? "{" : "}")} {{x}} {b}"; if (c) { d(); }'
        self.assertEqual('var s = ""; if (c) { d(); }', self.scan(line)[0].code)

    def test_strings_and_comments_that_run_over_several_lines(self):
        text = ("var s = @\"one\n\"\"two\"\" // three\nfour\"; // five\n/* six\nseven */ int x;\n"
                "var t = $@\"{a\n}\";\nint y; // eight\n")
        scanned = self.scan(text)
        self.assertEqual([True, False, False, True, False, True, False, True], [s.safe for s in scanned])
        self.assertEqual([None, None, 7, None, None, None, None, 7], [s.comment_at for s in scanned])
        self.assertEqual(['var s = "', "", '"; ', "", " int x;", 'var t = "', '";', "int y; "], [s.code for s in scanned])
        self.assertEqual([False, False, True, True, True, False, False, True], [s.commented for s in scanned])

    def test_preprocessor_lines_are_left_alone(self):
        scanned = self.scan("#region The player's stuff\nint x; // y\n#endregion\n")
        self.assertEqual(["", "int x; ", ""], [s.code for s in scanned])
        self.assertTrue(all(s.safe for s in scanned))
        self.assertEqual(7, scanned[1].comment_at)


class DocsTaskTest(unittest.TestCase):
    """do_task in a scratch checkout, with a fake agent and a compile check that always passes."""

    def setUp(self):
        self.tmp = Path(tempfile.mkdtemp(prefix="nightshift-docs-"))
        self.repo = self.tmp / "repo"
        mock.patch.dict(os.environ, {"GIT_CONFIG_GLOBAL": os.devnull, "GIT_CONFIG_NOSYSTEM": "1", "LC_ALL": "C"}).start()
        self.addCleanup(mock.patch.stopall)
        self.addCleanup(shutil.rmtree, self.tmp, True)
        files = {".gitignore": "/Tools/NightShift/.state/\n", ns.LESSONS_REL: "# Lessons\n",
                 "Assets/Scripts.meta": "meta\n", "Assets/Scripts/Foo.cs": FOO, "Assets/Scripts/Foo.cs.meta": "meta\n",
                 "Assets/Scripts/Bar.cs": BAR, "Assets/Scripts/Bar.cs.meta": "meta\n", "Assets/Notes.txt": "notes\n"}
        for rel, text in files.items():
            self.write(rel, text)
        self.sh("init", "-q")
        self.sh("add", "-A")
        self.sh("commit", "-q", "-m", "base")
        self.base = self.sh("rev-parse", "HEAD").strip()
        mock.patch.object(ns, "write_opencode_config", return_value=self.tmp / "opencode.json").start()
        mock.patch.object(ns, "compile_check", return_value=(True, "COMPILE OK")).start()
        mock.patch.object(ns, "run_opencode", side_effect=self.agent).start()
        self.edit = self.careless_edit  # what the fake agent does to the checkout

    def write(self, rel: str, text: str) -> None:
        path = self.repo / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(text.encode("utf-8"))  # as written, without Windows' newline translation

    def sh(self, *args: str) -> str:
        return subprocess.run(["git", "-C", str(self.repo), "-c", "user.name=t", "-c", "user.email=t@t", *args],
                              check=True, capture_output=True, text=True).stdout

    def agent(self, repo, prompt, *args, **kwargs) -> ns.AgentRun:
        self.edit()
        return ns.AgentRun(session="s1", text="Added summaries to Foo.")

    def careless_edit(self) -> None:
        self.write("Assets/Scripts/Foo.cs", FOO_BY_AGENT)
        (self.repo / "Assets/Scripts/Bar.cs").unlink()
        self.write("Assets/Scripts/New.cs", "namespace CloneZ { public class New { } }\n")
        self.write("Assets/Notes.txt", "notes, reworded\n")
        with (self.repo / ns.LESSONS_REL).open("ab") as f:
            f.write(b"- Foo counts ticks.\n")

    def run_task(self, task: str) -> tuple[ns.Result, str]:
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            res = ns.do_task(self.repo, self.base, task, "model", "http://ollama", 10, 0, 4096, "")
        return res, out.getvalue()

    def test_a_documentation_task_lands_the_comments_it_adds_and_nothing_else(self):
        key = ns.DOC_TASK.format(path="Assets/Scripts/Foo.cs")
        res, log = self.run_task("\n".join((key,) + ns.DOC_DETAILS))
        self.assertEqual("ok", res.status)
        self.assertEqual(["Assets/Scripts/Foo.cs"], res.files)
        self.assertEqual(["Assets/Scripts/Foo.cs"], self.sh("show", "--name-only", "--format=", res.sha).split())
        self.assertEqual(FOO_DOCUMENTED, self.sh("show", f"{res.sha}:Assets/Scripts/Foo.cs"))
        self.assertEqual(["- Foo counts ticks."], res.lessons)  # lessons are still passed on to the coordinator
        self.assertIn("kept only the comments it added", log)
        for path in ("Assets/Scripts/Bar.cs", "Assets/Scripts/New.cs", "Assets/Notes.txt"):
            self.assertIn(path, log)

    def test_an_explain_task_lands_only_its_comments_too(self):
        res, log = self.run_task(ns.EXPLAIN_TASK.format(path="Assets/Scripts/Foo.cs"))
        self.assertEqual("ok", res.status)
        self.assertEqual(["Assets/Scripts/Foo.cs"], res.files)
        self.assertEqual(FOO_DOCUMENTED, self.sh("show", f"{res.sha}:Assets/Scripts/Foo.cs"))
        self.assertIn("kept only the comments it added", log)

    def test_other_tasks_keep_every_change(self):
        res, log = self.run_task("Tidy up Assets/Scripts/Foo.cs")
        self.assertEqual("ok", res.status)
        self.assertEqual(FOO_BY_AGENT, self.sh("show", f"{res.sha}:Assets/Scripts/Foo.cs"))
        self.assertIn("Assets/Scripts/New.cs", res.files)
        self.assertNotIn("kept only the comments", log)

    def test_a_documentation_task_that_only_changed_code_changes_nothing(self):
        self.edit = lambda: self.write("Assets/Scripts/Foo.cs", FOO.replace("+= 1", "+= 2"))
        res, _ = self.run_task(ns.DOC_TASK.format(path="Assets/Scripts/Foo.cs"))
        self.assertEqual("nochange", res.status)
        self.assertEqual(FOO, (self.repo / "Assets/Scripts/Foo.cs").read_bytes().decode("utf-8"))


if __name__ == "__main__":
    unittest.main()
