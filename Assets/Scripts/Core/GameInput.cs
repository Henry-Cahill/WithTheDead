using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>Gameplay actions bound through <see cref="GameInput"/> (the original controls read the keyboard directly).</summary>
    public enum GameAction
    {
        // Every view
        ThrowBolt, Detector, FieldCraft, Map, Journal, Orders, Radio, PushToTalk, Grenade,
        // Isometric
        AimLine, RotateView,
        // Over the shoulder
        SwapShoulder,
        // First person and bodycam
        HoldBreath,
        // Every view (late additions)
        WipeVisor,
    }

    /// <summary>
    /// The new controls, as Input System action maps: one map shared by every view and one per camera (isometric,
    /// over the shoulder, first person/bodycam), enabled as the view changes — so the same key can mean "rotate
    /// the view" from above, "swap shoulder" behind the survivor and "hold breath" through the eyes. Everything is
    /// suppressed while a menu, the action menu or build mode owns the input.
    /// </summary>
    public static class GameInput
    {
        private static InputActionMap s_Common, s_Iso, s_Shoulder, s_First;
        private static readonly Dictionary<GameAction, InputAction> s_Actions = new Dictionary<GameAction, InputAction>();
        private static CameraView s_Applied = (CameraView)(-1);

        /// <summary>True while gameplay input should be ignored (a screen, the action menu, typing).</summary>
        public static bool Blocked => UIState.GameplayBlocked || UIState.ActionMenuOpen || UIScreen.Current != null;

        private static void Ensure()
        {
            if (s_Common != null)
            {
                if (s_Applied != ViewState.Current) ApplyView(ViewState.Current);
                return;
            }
            s_Common = new InputActionMap("Common");
            s_Iso = new InputActionMap("Isometric");
            s_Shoulder = new InputActionMap("Shoulder");
            s_First = new InputActionMap("FirstPerson");

            Bind(s_Common, GameAction.ThrowBolt, "<Keyboard>/x");
            Bind(s_Common, GameAction.Detector, "<Keyboard>/o");
            Bind(s_Common, GameAction.FieldCraft, "<Keyboard>/c");
            Bind(s_Common, GameAction.Map, "<Keyboard>/p");
            Bind(s_Common, GameAction.Journal, "<Keyboard>/j");
            Bind(s_Common, GameAction.Orders, "<Keyboard>/u");
            Bind(s_Common, GameAction.Radio, "<Keyboard>/y");
            Bind(s_Common, GameAction.PushToTalk, "<Keyboard>/capsLock");
            Bind(s_Common, GameAction.Grenade, "<Keyboard>/backquote");
            Bind(s_Common, GameAction.WipeVisor, "<Keyboard>/f2");
            s_Actions[GameAction.WipeVisor].AddBinding("<Mouse>/forwardButton");

            Bind(s_Iso, GameAction.AimLine, "<Keyboard>/leftAlt");
            Bind(s_Iso, GameAction.RotateView, "<Keyboard>/z");
            Bind(s_Shoulder, GameAction.SwapShoulder, "<Keyboard>/z");
            Bind(s_First, GameAction.HoldBreath, "<Keyboard>/z");

            s_Common.Enable();
            ApplyView(ViewState.Current);
        }

        private static void Bind(InputActionMap map, GameAction a, string path)
        {
            InputAction act = map.AddAction(a.ToString(), InputActionType.Button, path);
            s_Actions[a] = act;
        }

        /// <summary>Enables the action map of <paramref name="view"/> (and disables the others).</summary>
        public static void ApplyView(CameraView view)
        {
            if (s_Common == null) { Ensure(); return; }
            s_Applied = view;
            Set(s_Iso, view == CameraView.Isometric);
            Set(s_Shoulder, view == CameraView.ThirdPerson);
            Set(s_First, view == CameraView.FirstPerson || view == CameraView.Bodycam);
        }

        private static void Set(InputActionMap m, bool on)
        {
            if (on && !m.enabled) m.Enable();
            else if (!on && m.enabled) m.Disable();
        }

        /// <summary>True on the frame the action's key went down (never while <see cref="Blocked"/>).</summary>
        public static bool Pressed(GameAction a)
        {
            Ensure();
            return !Blocked && s_Actions.TryGetValue(a, out InputAction act) && act.enabled && act.WasPressedThisFrame();
        }

        /// <summary>True while the action's key is held (never while <see cref="Blocked"/>).</summary>
        public static bool Held(GameAction a)
        {
            Ensure();
            return !Blocked && s_Actions.TryGetValue(a, out InputAction act) && act.enabled && act.IsPressed();
        }

        /// <summary>True while held, ignoring menus (push-to-talk keeps working with the inventory open).</summary>
        public static bool HeldRaw(GameAction a)
        {
            Ensure();
            return s_Actions.TryGetValue(a, out InputAction act) && act.enabled && act.IsPressed();
        }

        /// <summary>The key shown in prompts, e.g. "X".</summary>
        public static string KeyName(GameAction a)
        {
            Ensure();
            if (!s_Actions.TryGetValue(a, out InputAction act) || act.bindings.Count == 0) return "?";
            return act.GetBindingDisplayString(0);
        }
    }
}
