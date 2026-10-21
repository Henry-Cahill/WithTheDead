"""Tests for the night shift's planner (NIGHTSHIFT_AUTOPLAN). Run on Linux (the coordinator needs fcntl):
    python3 -m unittest discover -s Tools/NightShift/tests -p "test_*.py" -v
"""
from __future__ import annotations

import importlib.util
import shutil
import sys
import tempfile
import threading
import time
import types
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
_spec = importlib.util.spec_from_file_location("nightshift", HERE.parent / "nightshift.py")
ns = importlib.util.module_from_spec(_spec)
sys.modules["nightshift"] = ns
_spec.loader.exec_module(ns)

QUEUE = "# Night Shift tasks\n\n## Queue\n\n- [x] An old task about Assets/Scripts/World/Old.cs that landed\n  - 21:00 landed\n"

BUILDER = "\n".join(["using UnityEngine;", "namespace CloneZ", "{", "    public static class Lamp", "    {",
                     "        public static void Build(RaidMesh m, in Surf s)", "        {"]
                    + [f"            m.Box(new Vector3(0f, {i}f, 0f), new Vector3(0.1f, 0.1f, 0.1f), s);" for i in range(14)]
                    + ["        }"] + ["        // padding"] * 12 + ["    }", "}", ""])

DOCS = """namespace CloneZ
{
    public class Foo : Bar
    {
        public Foo(int x) { }
        public int Count;
        public const int Max = 3;
        public static readonly Foo Empty = null;
        public override string DisplayName => "x";
        /// <summary>Documented.</summary>
        public void Documented() { }
        [SerializeField]
        public void NoDocWithAttribute() { }
        /// <summary>Doc above an attribute.</summary>
        [Obsolete]
        public void DocAboveAttribute() { }
        public int Size { get; set; }
        public static List<int> Make<T>(T t) => null;
        public float[] Values() => null;
        public event System.Action Changed;
        public void GetActions(object ctx, object into) { }
""" + "        // padding\n" * 14 + "    }\n}\n"

# A lamp that uses a bulb; the bulb's script, for the related-scripts outline.
LAMP = "\n".join(["using UnityEngine;", "namespace CloneZ", "{", "    public static partial class Lamp", "    {",
                  "        public static void Build(RaidMesh m, in Surf s, Common c)", "        {",
                  "            Bulb.Make(m, s);", "            Bulb.Make(m, s);"]
                 + [f"            m.Box(new Vector3(0f, {i}f, 0f), new Vector3(0.1f, 0.1f, 0.1f), s);" for i in range(14)]
                 + ["        }"] + ["        // padding"] * 12 + ["    }", "}", ""])

BULB = """namespace CloneZ
{
    /// <summary>A bulb.</summary>
    public sealed class Bulb
    {
        public float Watts = 60f;
        private int _secret;
        public static void Make(RaidMesh m, in Surf s, float watts = 40f) { }
        public bool Lit => Watts > 0f;
        internal void Flicker(Common c)
        {
        }
        public int Size { get; set; }
    }
}
"""


class PlannerTest(unittest.TestCase):
    def setUp(self):
        self.tmp = Path(tempfile.mkdtemp(prefix="nightshift-autoplan-"))
        self.repo = self.tmp / "repo"
        (self.repo / "Tools/NightShift/memory").mkdir(parents=True)
        (self.repo / "Tools/NightShift/memory/LESSONS.md").write_text("# Lessons\n- Everything is built from code.\n")
        (self.repo / "Tools/NightShift/check.sh").write_text("#!/bin/sh\n")
        (self.repo / "Tools/NightShift/nightshift.py").write_text("# the coordinator\n")
        state = self.tmp / "state"
        names = ("TASKS", "STATE", "TASKS_LOCK_PATH", "AUTOPLAN_STATE", "STATUS_FILE", "FINISH_FLAG")
        self.saved = {name: getattr(ns, name) for name in names}
        ns.TASKS = self.repo / "Tools/NightShift/tasks.md"
        ns.STATE = state
        ns.TASKS_LOCK_PATH = state / "tasks.lock"
        ns.AUTOPLAN_STATE = state / "autoplan.json"
        ns.STATUS_FILE = state / "status.json"
        ns.FINISH_FLAG = state / "finish-requested"
        ns.TASKS.write_text(QUEUE, encoding="utf-8")
        ns.STOPPING.clear()

    def tearDown(self):
        for name, value in self.saved.items():
            setattr(ns, name, value)
        ns.STOPPING.clear()
        shutil.rmtree(self.tmp, ignore_errors=True)

    def script(self, rel: str, text: str) -> str:
        path = self.repo / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
        return rel

    def unchecked(self) -> list:
        return [task for mark, task in ns.read_tasks() if mark == " "]

    # --- reading scripts ------------------------------------------------------------------------------------

    def test_undocumented_counts_methods_and_properties_only(self):
        # NoDocWithAttribute, Size, Make and Values: not constructors, fields, constants, events, overrides,
        # interface members or documented ones (also when the doc sits above an attribute).
        self.assertEqual(4, ns.undocumented(DOCS))

    def test_candidates_fit_the_focus_and_skip_busy_scripts(self):
        lamp = self.script("Assets/Scripts/World/Lamp.cs", BUILDER)
        foo = self.script("Assets/Scripts/Core/Foo.cs", DOCS)
        self.script("Assets/Scripts/Editor/Tool.cs", BUILDER)
        self.script("Assets/Scripts/World/Huge.cs", BUILDER + "// more\n" * 2000)
        self.script("Assets/Scripts/World/Tiny.cs", "namespace CloneZ { }\n")
        # A UI script full of colours but no 3D shapes isn't object graphics.
        self.script("Assets/Scripts/UI/Panel.cs", "\n".join(f"var c{i} = new Color(0.{i}f, 0.2f, 0.3f);" for i in range(40)))
        mapped = self.script("Assets/Scripts/World/Map/Streets.cs", BUILDER)
        tasks = ns.read_tasks()
        self.assertEqual([lamp, mapped], ns.plan_candidates("graphics", self.repo, tasks))
        self.assertEqual([foo], ns.plan_candidates("docs", self.repo, tasks))
        self.assertEqual([mapped], ns.plan_candidates("map", self.repo, tasks))
        # A script with an unchecked task waiting is left alone; one whose docs task exists isn't documented again.
        ns.TASKS.write_text(QUEUE + f"- [ ] Make the lamp in {lamp} brighter, please, by a lot\n"
                            + "- [!] " + ns.DOC_TASK.format(path=foo) + "\n", encoding="utf-8")
        tasks = ns.read_tasks()
        self.assertEqual([mapped], ns.plan_candidates("graphics", self.repo, tasks))
        self.assertEqual([], ns.plan_candidates("docs", self.repo, tasks))

    def test_pick_target_takes_the_one_shown_least_recently(self):
        seen = {"graphics:a.cs": "2026-10-19T21:00", "graphics:b.cs": "2026-10-19T20:00"}
        self.assertEqual("c.cs", ns.pick_target("graphics", ["a.cs", "b.cs", "c.cs"], seen))
        self.assertEqual("b.cs", ns.pick_target("graphics", ["a.cs", "b.cs"], seen))
        self.assertIsNone(ns.pick_target("graphics", [], seen))

    # --- reading the model's reply --------------------------------------------------------------------------

    def test_parse_plan(self):
        task = ns.parse_plan("Sure!\n- [ ] Do X in Assets/Scripts/World/A.cs.\n  Detail one.\n    Detail two.\n\nThanks")
        self.assertEqual("Do X in Assets/Scripts/World/A.cs.", task.key)
        self.assertEqual("Do X in Assets/Scripts/World/A.cs.\nDetail one.\nDetail two.", task.text)
        self.assertEqual("Real task here", ns.parse_plan("<think>- [ ] not this</think>\n1. [ ] Real task here").key)
        fenced = ns.parse_plan("```markdown\n- [ ] Task in a fence\n  its detail\n```")
        self.assertEqual("Task in a fence\nits detail", fenced.text)
        self.assertEqual("Compact box", ns.parse_plan("- [] Compact box").key)
        self.assertIsNone(ns.parse_plan("NONE"))
        self.assertIsNone(ns.parse_plan(""))

    def test_parse_plan_keeps_code_and_drops_markdown(self):
        reply = ("- [ ] In **Assets/Scripts/World/A.cs**, method **OvenDoor**: add a handle.\n"
                 "  *After* the line `x`, insert:\n"
                 "  ```csharp\n  m.Box(a,   b, chrome);\n\n  m.Box(c, d, chrome);\n  ```\n"
                 "  - Keep the rest.\n\nThat's my suggestion.")
        task = ns.parse_plan(reply)
        self.assertEqual("In Assets/Scripts/World/A.cs, method OvenDoor: add a handle.", task.key)
        self.assertEqual(["*After* the line `x`, insert:", "`m.Box(a, b, chrome);`", "`m.Box(c, d, chrome);`",
                          "Keep the rest."], task.text.split("\n")[1:])
        unindented = ns.parse_plan("- [ ] A task about Assets/Scripts/World/A.cs\nA detail without indent.\n## Why\nno")
        self.assertEqual("A task about Assets/Scripts/World/A.cs\nA detail without indent.", unindented.text)

    def test_check_plan(self):
        lamp = self.script("Assets/Scripts/World/Lamp.cs", BUILDER)
        tasks = ns.read_tasks()

        def check(text: str):
            key = text.split("\n")[0]
            return ns.check_plan(ns.Task(key, text), lamp, self.repo, tasks)

        named, why = check("In **Lamp.cs**, add a brass ring 2 cm tall around the bulb in Lamp.Build.\n- Reuse the surf.\n"
                           "a\nb\nc\nd\ne\nf\ng\nh\ni")
        self.assertEqual("", why)
        self.assertEqual(f"In {lamp}, add a brass ring 2 cm tall around the bulb in Lamp.Build.", named.key)
        # Bullets and bold dropped, eight detail lines at most.
        self.assertEqual(["Reuse the surf.", "a", "b", "c", "d", "e", "f", "g"], named.text.split("\n")[1:])
        self.assertIsNone(check("Add a brass ring 2 cm tall around the bulb of the street lamp builder.")[0])
        self.assertIsNone(check(f"In {lamp}, call the helper in Assets/Scripts/World/Missing.cs to add a ring.")[0])
        self.assertIsNone(check(f"In {lamp}, also edit Tools/NightShift/nightshift.py so the ring is counted.")[0])
        self.assertIsNone(check(f"In {lamp}, copy the lamp from Tools/MapScraper/out/regions/louisville data.")[0])
        self.assertIsNone(check(f"In {lamp}, add a ring and save the scene file Main.unity afterwards.")[0])
        self.assertIsNone(check(f"{lamp} ring")[0])
        _, why = check(f"In {lamp}, add a brass ring around the bulb, then run Tools/NightShift/check.sh to compile.")
        self.assertEqual("", why)
        # Repeats of a task already in tasks.md, word for word or nearly, are dropped.
        ns.TASKS.write_text(QUEUE + f"- [x] In {lamp}, add a brass ring 2 cm tall around the bulb in Lamp.Build.\n",
                            encoding="utf-8")
        tasks = ns.read_tasks()
        self.assertIsNone(check(f"In {lamp}, add a brass ring 2 cm tall around the bulb in Lamp.Build.")[0])
        self.assertIsNone(check(f"In {lamp}, add a brass ring 3 cm tall around the bulb in Lamp.Build.")[0])

    def test_check_plan_locates_by_class_and_catches_made_up_methods(self):
        lamp = self.script("Assets/Scripts/World/Lamp.cs", BUILDER)
        tasks, words = ns.read_tasks(), ns.script_words(self.repo)

        def check(text: str):
            return ns.check_plan(ns.Task(text.split("\n")[0], text), lamp, self.repo, tasks, words)

        located, why = check("Give the street lamp a second, dimmer bulb in Lamp.Build.\n*Add it with `m.Box(a, b, s)`.*")
        self.assertEqual("", why)
        self.assertEqual(f"In {lamp}: Give the street lamp a second, dimmer bulb in Lamp.Build.", located.key)
        self.assertEqual("Add it with `m.Box(a, b, s)`.", located.text.split("\n")[1])  # whole-line italics dropped
        self.assertEqual("*After* the box, add one.", ns.plain("*After* the box, add one."))
        one_new, why = check(f"In {lamp}, add a helper `AddRing(m, s)` and call it from `Build(m, s)` after the boxes.")
        self.assertEqual("", why)
        made_up, why = check(f"In {lamp}, call `m.Hinge(a, b)` and `m.Knob(c)` on the lamp housing in Lamp.Build.")
        self.assertIsNone(made_up)
        self.assertIn("Hinge, Knob", why)

    # --- the queue -----------------------------------------------------------------------------------------

    def test_append_planned_under_one_heading(self):
        heading = "### Auto-planned 2026-10-19: test"
        ns.append_planned(ns.Task("First planned task", "First planned task\nIts detail"), heading)
        ns.append_planned(ns.Task("Second planned task", "Second planned task"), heading)
        ns.append_planned(ns.Task("Third planned task", "Third planned task"), "### Auto-planned 2026-10-20: test")
        text = ns.TASKS.read_text(encoding="utf-8")
        self.assertEqual(1, text.count(heading))
        self.assertTrue(text.startswith(QUEUE.rstrip("\n")))
        self.assertIn(heading + "\n\n- [ ] First planned task\n  Its detail\n- [ ] Second planned task\n\n"
                      "### Auto-planned 2026-10-20: test\n\n- [ ] Third planned task\n", text)
        self.assertEqual(["First planned task\nIts detail", "Second planned task", "Third planned task"],
                         [task.text for task in self.unchecked()])

    def test_a_planned_task_merges_with_a_landing(self):
        # The coordinator keeps queue edits made while a task lands (sync_landed_checkout); a planned task is one.
        base = QUEUE + "- [ ] A queued task about Assets/Scripts/World/A.cs\n"
        landed = ns.with_mark(base, "A queued task about Assets/Scripts/World/A.cs", "x", "21:30 landed by spark")
        ns.TASKS.write_text(base, encoding="utf-8")
        ns.append_planned(ns.Task("A planned task", "A planned task"), "### Auto-planned 2026-10-19: test")
        planned = ns.TASKS.read_text(encoding="utf-8")
        merged = ns.merge_task_edits(base.encode(), landed.encode(), planned.encode()).decode()
        self.assertIn("- [x] A queued task about Assets/Scripts/World/A.cs\n  - 21:30 landed by spark\n", merged)
        self.assertTrue(merged.endswith("### Auto-planned 2026-10-19: test\n\n- [ ] A planned task\n"))

    # --- the planner ---------------------------------------------------------------------------------------

    def planner(self, replies: list, focus: list, **kwargs):
        asked = []

        def ask(base, model, prompt):
            asked.append(prompt)
            return replies.pop(0) if replies else "NONE"

        return ns.Planner("http://ollama", "test-model", focus, repo=self.repo, ask=ask, **kwargs), asked

    def test_planner_queues_a_checked_task(self):
        lamp = self.script("Assets/Scripts/World/Lamp.cs", BUILDER)
        planner, asked = self.planner(["- [ ] In Lamp.cs, add a brass ring around the bulb in Lamp.Build.\n  Reuse s."],
                                      ["graphics"])
        task = planner.plan_one()
        self.assertEqual(f"In {lamp}, add a brass ring around the bulb in Lamp.Build.", task.key)
        self.assertEqual(1, len(asked))
        self.assertIn(f"SCRIPT {lamp}", asked[0])
        self.assertIn("m.Box(new Vector3(0f, 13f, 0f)", asked[0])  # the whole script is shown
        self.assertIn("Everything is built from code.", asked[0])  # and the lessons
        self.assertEqual([task.text], [t.text for t in self.unchecked()])
        self.assertIn("### Auto-planned ", ns.TASKS.read_text(encoding="utf-8"))
        state = ns.load_plan_state()
        self.assertEqual(1, state["turn"])
        self.assertIn(f"graphics:{lamp}", state["seen"])
        self.assertEqual(1, planner.planned)

    def test_planner_moves_on_when_the_model_has_nothing(self):
        self.script("Assets/Scripts/World/Lamp.cs", BUILDER)
        planner, asked = self.planner(["NONE", "I can't think of anything."], ["graphics"])
        self.assertIsNone(planner.plan_one(tries=2))
        self.assertEqual(2, len(asked))
        self.assertEqual([], self.unchecked())
        self.assertEqual(2, ns.load_plan_state()["turn"])

    def test_docs_need_no_model_and_come_once(self):
        foo = self.script("Assets/Scripts/Core/Foo.cs", DOCS)
        planner, asked = self.planner([], ["docs"])
        task = planner.plan_one()
        self.assertEqual(ns.DOC_TASK.format(path=foo), task.key)
        self.assertEqual(list(ns.DOC_DETAILS), task.text.split("\n")[1:])
        self.assertEqual([], asked)
        self.assertIsNone(planner.plan_one())  # already queued for that script
        self.assertEqual(1, len(self.unchecked()))

    def test_dry_run_queues_and_remembers_nothing(self):
        self.script("Assets/Scripts/Core/Foo.cs", DOCS)
        planner, _ = self.planner([], ["docs"], dry_run=True)
        self.assertIsNotNone(planner.plan_one())
        self.assertEqual([], self.unchecked())
        self.assertFalse(ns.AUTOPLAN_STATE.exists())

    def test_nothing_is_queued_once_off_duty(self):
        self.script("Assets/Scripts/World/Lamp.cs", BUILDER)
        duty = {"on": True}
        planner, _ = self.planner([], ["graphics"], on_duty=lambda: duty["on"])

        def ask(base, model, prompt):
            duty["on"] = False  # a stop arrives while the model is thinking
            return "- [ ] In Lamp.cs, add a brass ring around the bulb in Lamp.Build."

        planner.ask = ask
        self.assertIsNone(planner.plan_one())
        self.assertEqual([], self.unchecked())

    # --- big tasks ---------------------------------------------------------------------------------------

    def related_fixture(self) -> tuple:
        """A lamp that uses a bulb, a street that uses the lamp, and two scripts that share only a name every script
        uses (so nothing ties them to the lamp)."""
        lamp = self.script("Assets/Scripts/World/Lamp.cs", LAMP)
        bulb = self.script("Assets/Scripts/World/Bulb.cs", BULB)
        street = self.script("Assets/Scripts/World/Street.cs", "namespace CloneZ\n{\n    public static class Street\n    {\n"
                             "        public static void Draw(Common c) { Lamp.Build(null, default, c); }\n    }\n}\n")
        self.script("Assets/Scripts/Core/Common.cs", "namespace CloneZ\n{\n    public sealed class Common { }\n}\n")
        self.script("Assets/Scripts/World/Unrelated.cs", "namespace CloneZ\n{\n    public sealed class Unrelated { public Common C; }\n}\n")
        return lamp, bulb, street

    def test_focus_names_and_big_variants(self):
        self.assertEqual(("graphics", True), ns.split_focus("graphics+"))
        self.assertEqual(("map", False), ns.split_focus(" map "))
        self.assertEqual(("docs", False), ns.split_focus("docs+"))  # documentation is fixed text: no big variant
        self.assertIn("fixes+", ns.focus_names())
        self.assertNotIn("docs+", ns.focus_names())
        self.assertEqual("bug-fix (big)", ns.focus_label("fixes+"))
        planner, _ = self.planner([], ["graphics+", "bogus", "docs+", "fixes"])
        self.assertEqual(["graphics+", "docs", "fixes"], planner.focus)

    def test_related_scripts_rank_siblings_then_types_used_then_users(self):
        lamp, bulb, street = self.related_fixture()
        self.assertEqual([bulb, street], ns.related_scripts(lamp, self.repo))
        parts = self.script("Assets/Scripts/World/LampParts.cs", "namespace CloneZ\n{\n    public static partial class Lamp\n"
                            "    {\n        private static void Collar(Common c) { }\n    }\n}\n")
        self.assertEqual([parts, bulb, street], ns.related_scripts(lamp, self.repo))
        self.assertEqual([parts, street], ns.related_scripts(lamp, self.repo, busy={bulb}))
        self.assertEqual([parts], ns.related_scripts(lamp, self.repo, limit=1))
        self.assertEqual([], ns.related_scripts("Assets/Scripts/World/Missing.cs", self.repo))

    def test_outline_keeps_types_and_public_members_without_bodies(self):
        self.assertEqual(["public sealed class Bulb", "public float Watts",
                          "public static void Make(RaidMesh m, in Surf s, float watts = 40f)", "public bool Lit",
                          "internal void Flicker(Common c)", "public int Size"], ns.outline(BULB))
        self.assertEqual(["public sealed class Bulb", "public float Watts", "..."], ns.outline(BULB, max_lines=2))

    def test_planner_queues_a_big_task_with_the_related_scripts_shown(self):
        lamp, bulb, street = self.related_fixture()
        reply = ("- [ ] Give the street lamp in Lamp.cs a full detailing pass.\n"
                 "  Step 1: add a brass collar under the bulb in Lamp.Build with `m.Box(a, b, s)`.\n"
                 "  Step 2: add a drip cap over the bulb.\n  Step 3: tint the collar a little darker than the cap.\n"
                 "  Done when: the lamp reads as one fitting and its collider is unchanged.")
        planner, asked = self.planner([reply], ["graphics+"])
        task = planner.plan_one()
        self.assertEqual(f"Give the street lamp in {lamp} a full detailing pass.", task.key)
        self.assertEqual(4, len(task.text.split("\n")) - 1)
        for text in ("RELATED SCRIPTS", f"{bulb} (", "public sealed class Bulb", f"{street} (", "Step 1:", "Done when:",
                     ns.BIG_HELP["graphics"][:40], f"SCRIPT {lamp}", "m.Box(new Vector3(0f, 13f, 0f)"):
            self.assertIn(text, asked[0])
        self.assertIn(f"graphics+:{lamp}", ns.load_plan_state()["seen"])
        self.assertNotIn("RELATED SCRIPTS", ns.plan_prompt("graphics", lamp, "// code", [], ""))

    def test_check_plan_gives_big_tasks_room_but_keeps_them_grounded(self):
        lamp, bulb, street = self.related_fixture()
        tasks, words = ns.read_tasks(), ns.script_words(self.repo)

        def check(lines: list, big: bool = True):
            text = "\n".join(lines)
            return ns.check_plan(ns.Task(lines[0], text), lamp, self.repo, tasks, words, big=big)

        head = f"Give the street lamp in {lamp} a full detailing pass, part by part."
        steps = [f"Step {i}: add part {i} beside the bulb with `m.Box(a, b, s)`." for i in range(1, 18)]
        task, why = check([head] + steps)
        self.assertEqual("", why)
        self.assertEqual(16, len(task.text.split("\n")) - 1)  # sixteen detail lines kept
        self.assertEqual(8, len(check([head] + steps, big=False)[0].text.split("\n")) - 1)  # a small task keeps eight
        self.assertIn("steps", check([head, "Step 1: just this."])[1])  # a big task spells out its steps
        # One new script, in a folder that exists, for a big task only.
        new = "Assets/Scripts/World/LampTrim.cs"
        three = [f"Step 1: create {new} with a static class LampTrim.", "Step 2: call `LampTrim.Add(m, s)` from Lamp.Build.",
                 f"Done when: {new} draws the trim and the lamp looks the same otherwise."]
        self.assertEqual("", check([head] + three)[1])
        self.assertIsNone(check([head] + three + ["Step 4: and create Assets/Scripts/World/LampCap.cs too."])[0])
        self.assertIsNone(check([head, "Step 1: create Assets/Scripts/Nowhere/LampTrim.cs.", "Step 2: use it.",
                                 "Done when: it is used."])[0])
        self.assertIsNone(check([head] + three, big=False)[0])
        # A few new helpers are fine; more means it's making things up.
        made_up = [f"Step {i}: call `Helper{i}(m)` after the boxes." for i in range(1, 5)] + ["Done when: all four run."]
        self.assertIn("Helper1", check([head] + made_up)[1])
        self.assertEqual("", check([head] + made_up[1:])[1])

    def test_check_plan_leaves_scripts_a_queued_task_is_changing_alone(self):
        lamp, bulb, street = self.related_fixture()
        queued = f"Make the bulb in {bulb} burn warmer, please, a lot warmer"
        key = f"In {lamp}, match the colour the bulb in {bulb} burns with, everywhere it shows."
        task = ns.Task(key, key)
        ns.TASKS.write_text(QUEUE + f"- [ ] {queued}\n", encoding="utf-8")
        tasks = ns.read_tasks()
        checked, why = ns.check_plan(task, lamp, self.repo, tasks)
        self.assertIsNone(checked)
        self.assertIn(bulb, why)
        self.assertNotIn(bulb, ns.related_scripts(lamp, self.repo, ns.busy_scripts(tasks)))
        ns.TASKS.write_text(QUEUE + f"- [x] {queued}\n", encoding="utf-8")  # once it has landed the script is free
        self.assertEqual("", ns.check_plan(task, lamp, self.repo, ns.read_tasks())[1])

    def test_planner_reads_all_the_lessons(self):
        self.script("Assets/Scripts/World/Lamp.cs", BUILDER)
        (self.repo / "Tools/NightShift/memory/LESSONS.md").write_text("# Lessons\n" + "- filler\n" * 1200 + "- the last one\n")
        planner, asked = self.planner(["NONE"], ["graphics"])
        planner.plan_one(tries=1)
        self.assertIn("- the last one", asked[0])

    # --- big tasks for some workers, small ones for the rest -----------------------------------------------

    def test_is_big_when_written_in_steps_or_long(self):
        self.assertTrue(ns.is_big(ns.Task("k", "k\nStep 1: a\nstep 2. b")))
        self.assertFalse(ns.is_big(ns.Task("k", "k\nStep 1: just one")))
        self.assertFalse(ns.is_big(ns.Task("k", "k\n" + "\n".join(["detail"] * 8))))
        self.assertTrue(ns.is_big(ns.Task("k", "k\n" + "\n".join(["detail"] * 9))))
        self.assertTrue(ns.is_big(ns.Task("k", "k\n" + "x" * 3000)))

    def test_check_plan_keeps_small_tasks_small_and_big_ones_in_steps(self):
        lamp = self.script("Assets/Scripts/World/Lamp.cs", BUILDER)
        tasks = ns.read_tasks()
        text = f"In {lamp}, add a brass ring around the bulb in Lamp.Build.\nStep 1: add the ring.\nStep 2: tint it."
        small, why = ns.check_plan(ns.Task(text.split("\n")[0], text), lamp, self.repo, tasks)
        self.assertEqual("", why)
        self.assertEqual(["add the ring.", "tint it."], small.text.split("\n")[1:])
        self.assertFalse(ns.is_big(small))
        big, why = ns.check_plan(ns.Task(text.split("\n")[0], text), lamp, self.repo, tasks, big=True)
        self.assertEqual("", why)
        self.assertTrue(ns.is_big(big))
        unnumbered = text.replace("Step 1: ", "").replace("Step 2: ", "")
        self.assertIn("steps", ns.check_plan(ns.Task(text.split("\n")[0], unnumbered), lamp, self.repo, tasks,
                                             big=True)[1])

    def test_planner_writes_a_big_focus_small_when_asked(self):
        lamp, _, _ = self.related_fixture()
        reply = "- [ ] In Lamp.cs, add a brass ring around the bulb in Lamp.Build.\n  Step 1: add it.\n  Step 2: tint it."
        planner, asked = self.planner([reply], ["graphics+"])
        task = planner.plan_one(small=True)
        self.assertFalse(ns.is_big(task))
        self.assertNotIn("RELATED SCRIPTS", asked[0])
        self.assertIn("one concrete, precisely located change", asked[0])  # the small task's prompt
        state = ns.load_plan_state()
        self.assertEqual(1, state["turn"])  # the rotation moves on as usual
        self.assertIn(f"graphics:{lamp}", state["seen"])

    def routed(self):
        """A coordinator with a helper that takes big tasks and the Spark, which takes only small ones."""
        coord = ns.Coordinator(dict(ns.DEFAULTS, NIGHTSHIFT_SMALL_TASK_MODELS="qwen3.8"))
        coord.on_branch = lambda: True
        coord.planner = types.SimpleNamespace(planned=0)  # planning is on
        helper = types.SimpleNamespace(name="helper", big=True, retired=False)
        spark = types.SimpleNamespace(name="spark", big=False, retired=False)
        coord.workers = [helper, spark]
        return coord, helper, spark

    @staticmethod
    def big_task(name: str) -> ns.Task:
        key = f"A big task about Assets/Scripts/World/{name}.cs"
        return ns.Task(key, key + "\nStep 1: one thing.\nStep 2: another.\nDone when: both show.")

    def test_coordinator_reads_the_small_task_models(self):
        coord = ns.Coordinator(dict(ns.DEFAULTS, NIGHTSHIFT_SMALL_TASK_MODELS=" qwen3.8:latest, other "))
        self.assertEqual({"qwen3.8", "other"}, coord.small_models)
        self.assertEqual(set(), ns.Coordinator(dict(ns.DEFAULTS)).small_models)

    def test_claim_routes_big_tasks_to_the_workers_that_take_them(self):
        coord, helper, spark = self.routed()
        heading = "### Auto-planned test"
        ns.append_planned(ns.Task("Small planned task about Assets/Scripts/World/S1.cs",
                                  "Small planned task about Assets/Scripts/World/S1.cs"), heading)
        ns.append_planned(self.big_task("B1"), heading)
        ns.append_planned(ns.Task("Small planned task about Assets/Scripts/World/S2.cs",
                                  "Small planned task about Assets/Scripts/World/S2.cs"), heading)
        with ns.TASKS.open("a", encoding="utf-8") as f:
            f.write("\n## Mine\n\n- [ ] A task I wrote about Assets/Scripts/World/Mine.cs\n")
        self.assertEqual("A task I wrote about Assets/Scripts/World/Mine.cs", coord.claim(helper).key)  # mine first
        self.assertEqual(self.big_task("B1").key, coord.claim(helper).key)  # then the planner's big ones
        self.assertEqual("Small planned task about Assets/Scripts/World/S1.cs", coord.claim(spark).key)
        self.assertEqual("Small planned task about Assets/Scripts/World/S2.cs", coord.claim(helper).key)
        ns.append_planned(self.big_task("B2"), heading)
        self.assertIsNone(coord.claim(spark))  # the Spark never takes a big one...
        helper.retired = True
        self.assertEqual(self.big_task("B2").key, coord.claim(spark).key)  # ...unless nobody else is left to

    def test_wanted_keeps_a_small_task_waiting_for_each_small_only_worker(self):
        coord, helper, spark = self.routed()  # two workers, so four should wait
        small = ns.Task("small", "small")
        big = self.big_task("B")
        self.assertEqual("small", coord.wanted([]))
        self.assertEqual("any", coord.wanted([small]))
        self.assertIsNone(coord.wanted([small, big, big, big]))
        self.assertEqual("small", coord.wanted([big] * 5))  # plenty waiting, but none the Spark may take
        helper.retired = True
        self.assertEqual("small", coord.wanted([small]))  # nobody left for big tasks: write small ones only
        spark.big = True  # nobody takes only small tasks: whatever comes next in turn
        helper.retired = False
        self.assertEqual("any", coord.wanted([]))

    def test_plan_loop_writes_a_small_task_first_for_the_small_only_worker(self):
        coord, helper, spark = self.routed()
        asked = []

        class FakePlanner:
            planned = 0

            def plan_one(self, small=False):
                asked.append(small)
                self.planned += 1
                key = f"Planned task {self.planned} for Assets/Scripts/World/A{self.planned}.cs"
                ns.append_planned(ns.Task(key, key), "### Auto-planned test")
                if self.planned > 10:
                    coord.planning.clear()  # runaway: fail below rather than hang
                return ns.Task(key, key)

        coord.planner = FakePlanner()
        coord.planning.set()
        loop = threading.Thread(target=coord.plan_loop, daemon=True)
        loop.start()
        deadline = time.time() + 10
        while time.time() < deadline and len(self.unchecked()) < coord.low_water():
            time.sleep(0.05)
        time.sleep(0.3)
        coord.planning.clear()
        ns.STOPPING.set()
        loop.join(5)
        self.assertFalse(loop.is_alive())
        self.assertEqual([True, False, False, False], asked)

    # --- the coordinator -----------------------------------------------------------------------------------

    def test_claim_takes_tasks_people_wrote_first(self):
        coord = ns.Coordinator(dict(ns.DEFAULTS))
        coord.on_branch = lambda: True
        coord.planner = types.SimpleNamespace(planned=0)  # planning is on
        ns.append_planned(ns.Task("A planned task about Assets/Scripts/World/A.cs", "A planned task about "
                                  "Assets/Scripts/World/A.cs"), "### Auto-planned test")
        with ns.TASKS.open("a", encoding="utf-8") as f:
            f.write("\n## Mine\n\n- [ ] A task I wrote about Assets/Scripts/World/B.cs\n")
        worker = types.SimpleNamespace(name="one")
        self.assertEqual("A task I wrote about Assets/Scripts/World/B.cs", coord.claim(worker).key)
        self.assertEqual("A planned task about Assets/Scripts/World/A.cs", coord.claim(worker).key)
        self.assertIsNone(coord.claim(worker))

    def test_plan_loop_keeps_a_couple_more_tasks_waiting_than_workers(self):
        coord = ns.Coordinator(dict(ns.DEFAULTS))
        coord.workers = [types.SimpleNamespace(name="one"), types.SimpleNamespace(name="two")]  # so four should wait
        coord.on_branch = lambda: True

        class FakePlanner:
            planned = 0

            def plan_one(self, small=False):
                self.planned += 1
                key = f"Planned task {self.planned} for Assets/Scripts/World/A{self.planned}.cs"
                ns.append_planned(ns.Task(key, key), "### Auto-planned test")
                if self.planned > 10:
                    coord.planning.clear()  # runaway: fail below rather than hang
                return ns.Task(key, key)

        coord.planner = FakePlanner()
        coord.planning.set()
        loop = threading.Thread(target=coord.plan_loop, daemon=True)
        loop.start()
        deadline = time.time() + 10
        while time.time() < deadline and len(self.unchecked()) < coord.low_water():
            time.sleep(0.05)
        time.sleep(0.3)  # long enough for a runaway loop to show
        coord.planning.clear()
        ns.STOPPING.set()
        loop.join(5)
        self.assertFalse(loop.is_alive())
        self.assertEqual(4, len(self.unchecked()))
        self.assertEqual(4, coord.planner.planned)


if __name__ == "__main__":
    unittest.main()
