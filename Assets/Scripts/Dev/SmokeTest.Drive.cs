using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace CloneZ
{
    // The wave run's hands. Keys and the mouse go through the Input System on the game's own Keyboard.current and
    // Mouse.current, so every press takes the code path a player's does; UI controls get the pointer events the
    // EventSystem sends on a click; PressEverything works through every control on a panel; StandAt and OpenGround
    // put the survivor where a test needs them.
    public sealed partial class SmokeTest
    {
        private static Keyboard s_Kb;
        private static Mouse s_Mouse;
        private static InputSettings s_ProjectInput;
        private static readonly List<InputDevice> s_Silenced = new List<InputDevice>();
        private static readonly HashSet<Key> s_Held = new HashSet<Key>();
        private static MouseState s_MouseState;
        // Seconds between presses, to watch a run by (the editor's Smoke Test menu, or -clonez-smoke-pace); 0 at full speed.
        private static float s_Pace;

        private static readonly Regex RichText = new Regex("<[^>]*>");

        #region Keyboard and mouse
        // A keyboard and a mouse of the run's own, made current before any component caches Keyboard.current, with any
        // real ones switched off until the end (so a hand on the desk can't steer the run, or a real device become current
        // again). Input has to reach the game with nothing focused, too: a batch editor has no Game view to focus. The
        // project's own settings and devices are put back in RemoveInput.
        private static void InstallInput()
        {
            s_ProjectInput = InputSystem.settings;
            InputSettings settings = UnityEngine.Object.Instantiate(s_ProjectInput);
            settings.name = "Smoke input settings";
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings;

            s_Silenced.Clear();
            foreach (InputDevice d in InputSystem.devices)
                if ((d is Keyboard || d is Mouse) && d.enabled) s_Silenced.Add(d);
            foreach (InputDevice d in s_Silenced) InputSystem.DisableDevice(d);
            s_Held.Clear();
            s_MouseState = default;
            s_Kb = InputSystem.AddDevice<Keyboard>("SmokeKeyboard");
            s_Mouse = InputSystem.AddDevice<Mouse>("SmokeMouse");
            s_Kb.MakeCurrent();
            s_Mouse.MakeCurrent();
            Debug.Log("[Smoke] input: " + s_Kb.name + ", " + s_Mouse.name + (s_Silenced.Count > 0 ? "; switched off for the run: " + string.Join(", ", s_Silenced.ConvertAll(d => d.name)) : ""));
        }

        private static void RemoveInput()
        {
            if (s_Kb == null) return;
            InputSystem.RemoveDevice(s_Kb);
            InputSystem.RemoveDevice(s_Mouse);
            foreach (InputDevice d in s_Silenced)
                if (d != null && d.added) InputSystem.EnableDevice(d);
            s_Silenced.Clear();
            if (s_ProjectInput != null) InputSystem.settings = s_ProjectInput;
            s_Kb = null;
            s_Mouse = null;
        }

        private static void SendKeys()
        {
            var state = new KeyboardState();
            foreach (Key k in s_Held) state.Set(k, true);
            InputSystem.QueueStateEvent(s_Kb, state);
        }

        // Delta and scroll add up within a frame and fall back to zero after it, as a real mouse's do.
        private static void SendMouse(Vector2 delta, Vector2 scroll)
        {
            MouseState state = s_MouseState;
            state.delta = delta;
            state.scroll = scroll;
            InputSystem.QueueStateEvent(s_Mouse, state);
        }

        private static void KeyDown(params Key[] keys)
        {
            foreach (Key k in keys) s_Held.Add(k);
            SendKeys();
        }

        private static void KeyUp(params Key[] keys)
        {
            foreach (Key k in keys) s_Held.Remove(k);
            SendKeys();
        }

        private static void ReleaseAll()
        {
            if (s_Kb == null || !Application.isPlaying) return;
            s_Held.Clear();
            SendKeys();
            s_MouseState.buttons = 0;
            SendMouse(Vector2.zero, Vector2.zero);
        }

        // The pause between presses when the run is being watched.
        private static IEnumerator Pace()
        {
            if (s_Pace > 0f) yield return Wait(s_Pace);
        }

        // One press as a player makes it: down for one frame of the game's Updates, then up (a queued event lands at
        // the start of the next frame, so wasPressedThisFrame holds for exactly that frame).
        private static IEnumerator Tap(Key key)
        {
            KeyDown(key);
            yield return null;
            KeyUp(key);
            yield return null;
            yield return Pace();
        }

        // Keys held together for a while (real time, so it works while the game is paused); `during` sees every frame.
        private static IEnumerator Hold(float seconds, Action during, params Key[] keys)
        {
            yield return Held(seconds, during, keys);
            yield return Pace();
        }

        private static IEnumerator Held(float seconds, Action during, Key[] keys)
        {
            KeyDown(keys);
            yield return null;
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                during?.Invoke();
                yield return null;
            }
            KeyUp(keys);
            yield return null;
        }

        private static void PointAtScreen(Vector2 screen)
        {
            s_MouseState.position = screen;
            SendMouse(Vector2.zero, Vector2.zero);
        }

        private static void PointAtMiddle() => PointAtScreen(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));

        // The cursor over a point of the world, as the game's camera sees it. False when it's behind the camera.
        private static bool PointAt(Vector3 world)
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return false;
            PointAtScreen(new Vector2(sp.x, sp.y));
            return true;
        }

        private static IEnumerator Click(MouseButton button)
        {
            s_MouseState = s_MouseState.WithButton(button, true);
            SendMouse(Vector2.zero, Vector2.zero);
            yield return null;
            s_MouseState = s_MouseState.WithButton(button, false);
            SendMouse(Vector2.zero, Vector2.zero);
            yield return null;
            yield return Pace();
        }

        private static IEnumerator HoldButton(MouseButton button, float seconds, Action during, params Key[] keys)
        {
            s_MouseState = s_MouseState.WithButton(button, true);
            SendMouse(Vector2.zero, Vector2.zero);
            yield return Held(seconds, during, keys);
            s_MouseState = s_MouseState.WithButton(button, false);
            SendMouse(Vector2.zero, Vector2.zero);
            yield return null;
            yield return Pace();
        }

        // One notch of the wheel per unit (a notch reads 120 raw, or 1 where the Input System normalises it).
        private static IEnumerator Wheel(float notches)
        {
            SendMouse(Vector2.zero, new Vector2(0f, 120f * notches));
            yield return null;
            yield return null;
            yield return Pace();
        }

        private static IEnumerator Look(Vector2 perFrame, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                SendMouse(perFrame, Vector2.zero);
                yield return null;
            }
            yield return null;
            yield return Pace();
        }

        private static IEnumerator Wait(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        // Game time, as the game's own timers count it: in a batch run frames can be long, and then Time.deltaTime (capped)
        // falls behind the clock. `realCap` keeps a frozen world (the pause menu) from hanging the run.
        private static IEnumerator WaitGame(float seconds, float realCap = 20f)
        {
            float end = Time.time + seconds, cap = Time.realtimeSinceStartup + realCap;
            while (Time.time < end && Time.realtimeSinceStartup < cap) yield return null;
        }

        private static IEnumerator UntilGame(Func<bool> cond, float seconds, float realCap = 30f)
        {
            float end = Time.time + seconds, cap = Time.realtimeSinceStartup + realCap;
            while (!cond() && Time.time < end && Time.realtimeSinceStartup < cap) yield return null;
        }

        private static Key Digit(int n)
        {
            switch (n)
            {
                case 1: return Key.Digit1;
                case 2: return Key.Digit2;
                case 3: return Key.Digit3;
                case 4: return Key.Digit4;
                case 5: return Key.Digit5;
                case 6: return Key.Digit6;
                case 7: return Key.Digit7;
                case 8: return Key.Digit8;
                default: return Key.Digit9;
            }
        }
        #endregion

        #region The UI
        // A left click on a control as the EventSystem delivers one: the pointer enters it, clicks it and leaves.
        private static void Press(GameObject go)
        {
            if (go == null) return;
            var data = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                clickCount = 1,
                position = ScreenPointOf(go),
                pointerEnter = go,
                pointerPress = go,
                rawPointerPress = go,
                eligibleForClick = true,
            };
            ExecuteEvents.Execute(go, data, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(go, data, ExecuteEvents.pointerClickHandler);
            if (go != null) ExecuteEvents.Execute(go, data, ExecuteEvents.pointerExitHandler);
        }

        private static Vector2 ScreenPointOf(GameObject go)
        {
            var rt = go.transform as RectTransform;
            if (rt == null) return Vector2.zero;
            Canvas canvas = go.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
        }

        // What a player would call the control: its own text, else its row's (a toggle's or a slider's label) and its
        // name; a pause-menu row is named for its label.
        private static string LabelOf(Component c)
        {
            string name = c.gameObject.name;
            if (name.StartsWith("Item ", StringComparison.Ordinal)) return name.Substring(5);
            Text own = c.GetComponentInChildren<Text>();
            string label;
            if (own != null && !string.IsNullOrWhiteSpace(own.text)) label = own.text;
            else
            {
                Text row = c.transform.parent != null ? c.transform.parent.GetComponentInChildren<Text>() : null;
                label = (row != null && !string.IsNullOrWhiteSpace(row.text) ? row.text + " " : "") + "[" + name + "]";
            }
            label = RichText.Replace(label, "").Replace('\n', ' ').Trim();
            return label.Length > 60 ? label.Substring(0, 60) + "..." : label;
        }

        // The controls a click can reach under a panel: buttons, toggles, sliders and the pause menu's rows.
        private static List<Component> Controls(Transform root)
        {
            var list = new List<Component>();
            foreach (Selectable s in root.GetComponentsInChildren<Selectable>(false))
                if (s is Button || s is Toggle || s is Slider) list.Add(s);
            foreach (PausePointer p in root.GetComponentsInChildren<PausePointer>(false)) list.Add(p);
            return list;
        }

        // Whether a real pointer could use the control now: active, interactable, and under no canvas group that
        // ignores the pointer.
        private static bool Live(Component c)
        {
            if (c == null || !c.gameObject.activeInHierarchy) return false;
            if (c is Behaviour b && !b.isActiveAndEnabled) return false;
            if (c is Selectable s && !s.IsInteractable()) return false;
            for (Transform t = c.transform; t != null; t = t.parent)
            {
                CanvasGroup g = t.GetComponent<CanvasGroup>();
                if (g == null || !g.enabled) continue;
                if (!g.blocksRaycasts || !g.interactable) return false;
                if (g.ignoreParentGroups) break;
            }
            return true;
        }

        private static string PathKey(Transform t, Transform root)
        {
            var sb = new StringBuilder();
            for (; t != null && t != root; t = t.parent) sb.Insert(0, "/" + t.name + "#" + t.GetSiblingIndex());
            return sb.ToString();
        }

        /// <summary>
        /// Presses every control under a panel once and records each press as a check (clean when nothing logged an
        /// error): buttons, toggles and the pause menu's rows are clicked, sliders moved and put back. The panel is
        /// looked for again after every press, since presses rebuild panels: <paramref name="root"/> finds it (null while
        /// it's closed), <paramref name="reopen"/> brings it back when a press closed it, <paramref name="skip"/> says why
        /// a control (and its label) is left alone (null to press it) and <paramref name="last"/> marks the labels pressed
        /// only once all the others have been (closing ones). A control a pointer can't use yet gets a moment to become
        /// usable (a fade, a list being laid out, a popup going away) before it's reported as disabled. A screen a press
        /// puts over the panel (a book read, a recipe) is noted and shut.
        /// </summary>
        private IEnumerator PressEverything(string panel, Func<Transform> root, Func<IEnumerator> reopen,
            Func<Component, string, string> skip, Func<string, bool> last, int max)
        {
            const float Grace = 1.5f;
            var done = new HashSet<string>();
            var skipped = new List<string>();
            var blocked = new Dictionary<string, string>();
            int pressed = 0, reopened = 0;
            float waitingSince = -1f;
            while (pressed < max)
            {
                Transform r = root();
                if (r == null)
                {
                    if (reopen == null || reopened >= max) break;
                    reopened++;
                    yield return reopen();
                    r = root();
                    if (r == null) { Check(panel + ": opens again", false); break; }
                }

                Component next = null;
                string key = null, label = null;
                bool pending = false;
                bool graceLeft = waitingSince < 0f || Time.realtimeSinceStartup - waitingSince < Grace;
                blocked.Clear();
                for (int pass = 0; pass < 2 && next == null; pass++)
                {
                    if (pass == 1 && pending && graceLeft) break;   // the closing ones wait for the rest
                    foreach (Component c in Controls(r))
                    {
                        string l = LabelOf(c);
                        string k = PathKey(c.transform, r) + "|" + l;
                        if (done.Contains(k)) continue;
                        bool closing = last != null && last(l);
                        if (closing != (pass == 1)) continue;
                        string why = skip != null ? skip(c, l) : null;
                        if (why != null) { done.Add(k); skipped.Add(l + " (" + why + ")"); continue; }
                        if (!Live(c))
                        {
                            blocked[k] = l;
                            if (!closing) pending = true;
                            continue;
                        }
                        next = c;
                        key = k;
                        label = l;
                        break;
                    }
                }
                if (next == null)
                {
                    if (blocked.Count > 0 && graceLeft)
                    {
                        if (waitingSince < 0f) waitingSince = Time.realtimeSinceStartup;
                        yield return null;
                        continue;
                    }
                    break;
                }
                waitingSince = -1f;
                done.Add(key);

                int before = s_Errors;
                UIScreen screen = UIScreen.Current;
                if (next is Slider slider)
                {
                    float was = slider.value;
                    slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, Mathf.Approximately(was, slider.maxValue) ? 0.25f : 0.75f);
                    yield return null;
                    if (slider != null) slider.value = was;
                }
                else Press(next.gameObject);
                yield return null;
                yield return null;
                pressed++;

                string note = "";
                if (UIScreen.Current != null && UIScreen.Current != screen && !UIScreen.Current.transform.IsChildOf(r))
                {
                    note = "opened " + UIScreen.Current.GetType().Name;
                    UIScreen.Current.Close();
                    yield return null;
                }
                Check(panel + ": '" + label + "'", s_Errors == before, Join(note, ErrorsSince(before)));
                yield return Pace();
            }
            Debug.Log("[Smoke] " + panel + ": " + pressed + " control(s) pressed"
                + (blocked.Count > 0 ? "; not usable (disabled, or in a view not showing): " + string.Join(", ", blocked.Values) : "")
                + (skipped.Count > 0 ? "; left alone: " + string.Join(", ", skipped) : ""));
        }

        private IEnumerator Reopen(Key key, Func<bool> isOpen)
        {
            yield return Tap(key);
            yield return Until(isOpen, 2f);
        }
        #endregion

        #region Placing the survivor
        private static readonly int SolidMask = ~(1 << CloneZConfig.IgnoreRaycastLayer);

        // Puts the survivor on clear floor within `reach` of `point`, with nothing but `own` between their chest and it,
        // facing it, and the camera with them. False when there's no such spot around it.
        private bool StandAt(Vector3 point, float reach, Transform own)
        {
            var cc = _me.GetComponent<CharacterController>();
            bool had = cc != null && cc.enabled;
            if (had) cc.enabled = false;   // not a spot of their own body
            Vector3 feet = Vector3.zero;
            float face = 0f;
            bool found = false;
            try
            {
                float[] radii = { 0.8f, 1.05f, 0.6f, 1.35f, 1.7f };
                for (int ri = 0; ri < radii.Length && !found; ri++)
                {
                    float radius = Mathf.Min(radii[ri], reach * 0.85f);
                    for (int i = 0; i < 16 && !found; i++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward;
                        Vector3 probe = new Vector3(point.x, 0f, point.z) + dir * radius;
                        if (!FloorAt(probe, out feet) || Blocked(feet)) continue;
                        if (!ClearLine(feet + Vector3.up * 1.2f, point, own)) continue;
                        face = Quaternion.LookRotation(-dir).eulerAngles.y;
                        found = true;
                    }
                }
            }
            finally
            {
                if (had) cc.enabled = true;
            }
            if (!found) return false;
            Put(feet, face);
            return true;
        }

        // Clear open ground near the survivor (or the arena's middle): nothing within `clear` metres of it.
        private bool OpenGround(float clear, out Vector3 spot)
        {
            var cc = _me.GetComponent<CharacterController>();
            bool had = cc != null && cc.enabled;
            if (had) cc.enabled = false;
            try
            {
                foreach (Vector3 around in new[] { _me.transform.position, Vector3.zero })
                    for (int ring = 0; ring < 10; ring++)
                        for (int i = 0; i < 12; i++)
                        {
                            Vector3 p = around + Quaternion.Euler(0f, i * 30f + ring * 13f, 0f) * Vector3.forward * (ring * 2.5f);
                            float edge = CloneZConfig.ArenaHalfSize - clear - 1f;
                            p.x = Mathf.Clamp(p.x, -edge, edge);
                            p.z = Mathf.Clamp(p.z, -edge, edge);
                            if (!Physics.Raycast(new Vector3(p.x, 4f, p.z), Vector3.down, out RaycastHit hit, 8f, SolidMask, QueryTriggerInteraction.Ignore)) continue;
                            if (Mathf.Abs(hit.point.y) > 0.3f) continue;   // a roof, a table, a car
                            if (Physics.CheckBox(hit.point + Vector3.up * 1.3f, new Vector3(clear, 0.9f, clear), Quaternion.identity, SolidMask, QueryTriggerInteraction.Ignore)) continue;
                            spot = hit.point;
                            return true;
                        }
            }
            finally
            {
                if (had) cc.enabled = true;
            }
            spot = _me.transform.position;
            return false;
        }

        private bool GoToOpenGround(float clear)
        {
            bool ok = OpenGround(clear, out Vector3 spot);
            Put(spot, _me.transform.eulerAngles.y);
            return ok;
        }

        // The game's own teleport (it moves the body's collider and snaps the animation), then the camera.
        private void Put(Vector3 feet, float yaw)
        {
            _pi.Context.Teleport(feet + Vector3.up * 0.02f, yaw);
            Physics.SyncTransforms();
            if (_cam != null) _cam.Snap();
        }

        // The ground floor under a point (never a table top, a bed or a roof).
        private static bool FloorAt(Vector3 probe, out Vector3 feet)
        {
            feet = probe;
            if (!Physics.Raycast(new Vector3(probe.x, 2.1f, probe.z), Vector3.down, out RaycastHit hit, 4f, SolidMask, QueryTriggerInteraction.Ignore)) return false;
            if (hit.point.y < -0.5f || hit.point.y > 0.45f || hit.normal.y < 0.8f) return false;
            feet = hit.point;
            return true;
        }

        private static bool Blocked(Vector3 feet)
            => Physics.CheckCapsule(feet + Vector3.up * 0.45f, feet + Vector3.up * 1.6f, 0.38f, SolidMask, QueryTriggerInteraction.Ignore);

        // As the interactor's sight test: nothing solid between, short of the point, but parts of `own`.
        private static bool ClearLine(Vector3 from, Vector3 to, Transform own)
        {
            Vector3 end = to - (to - from).normalized * 0.06f;
            if (!Physics.Linecast(from, end, out RaycastHit hit, SolidMask, QueryTriggerInteraction.Ignore)) return true;
            return own != null && hit.collider.transform.IsChildOf(own);
        }

        // The longest clear way from the survivor, and the cursor `distance` along it on the ground (the isometric aim).
        private Vector3 AimClear(float distance)
        {
            Vector3 at = _me.transform.position;
            Vector3 dir = Quaternion.Euler(0f, ClearestHeading(at + Vector3.up * 0.8f), 0f) * Vector3.forward;
            PointAt(at + dir * distance);
            return dir;
        }
        #endregion

        #region Small helpers
        private static object Field(object o, string name)
            => o?.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(o);

        private static string ErrorsSince(int before)
            => s_Errors == before ? "" : (s_Errors - before) + " error(s), last: " + s_LastError;

        private static string Join(params string[] parts)
        {
            var kept = new List<string>();
            foreach (string p in parts) if (!string.IsNullOrEmpty(p)) kept.Add(p);
            return string.Join("; ", kept);
        }

        // An action that passes when `ok` holds and nothing logged an error since `before`.
        private void Did(string name, int before, bool ok, string detail = "")
            => Check(name, ok && s_Errors == before, Join(detail, ErrorsSince(before)));

        // A check this run couldn't make (the environment, not the game, stood in the way): logged, not failed.
        private void Skip(string name, string why)
        {
            _checks.Add("SKIP " + name + " (" + why + ")");
            Debug.Log("[Smoke] SKIP " + name + " — " + why);
        }
        #endregion
    }
}
