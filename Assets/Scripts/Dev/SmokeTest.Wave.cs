using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace CloneZ
{
    // -clonez-smoke wave: Wave Survival played end to end through its own controls. From the title's New run: every key
    // and mouse action of the survivor (on a keyboard and a mouse driven through the Input System), every action of one
    // fixture of each kind in the home and the yard (through the action menu, by number key and by click), every control
    // of every panel and screen the run opens, the pause menu, two waves cut down, F5, Save & quit to the title, Continue,
    // and a death. It plays on its own save file (SaveSystem.PathOverride) and puts back the settings its presses change.
    public sealed partial class SmokeTest
    {
        private const string WaveSaveFile = "smoke_wave_save.db";
        // The PlayerPrefs the run's presses write (the settings screen, the pause menu's HUD style, the inventory's view
        // and sort), named as GameSettings and InventoryUIController name them; put back in Finish.
        private static readonly string[] StringPrefs = { "clonez.settings", "clonez.inventoryView" };
        private static readonly string[] IntPrefs = { "clonez.inventorySort", "clonez.inventoryKeepWeapon" };
        private static readonly Dictionary<string, string> s_StringPrefs = new Dictionary<string, string>();
        private static readonly Dictionary<string, int?> s_IntPrefs = new Dictionary<string, int?>();
        private static int s_Saves;

        // The live run's pieces, found again after every world build.
        private GameManager _gm;
        private Actor _me;
        private PlayerStats _stats;
        private PlayerController _pc;
        private PlayerCombat _combat;
        private PlayerInventory _inv;
        private PlayerInteractor _pi;
        private PlayerBuilder _builder;
        private PlayerFarming _farming;
        private PlayerHydration _hydration;
        private PlayerSleep _sleep;
        private PlayerGear _gear;
        private PlayerSeat _seat;
        private PlayerClothing _clothing;
        private InventoryUIController _invUi;
        private HealthPanelController _health;
        private SkillsPanelController _skills;
        private PauseMenuController _pause;
        private HUDController _hud;
        private IsoCameraFollow _cam;
        private int _menuChoice;
        private bool _afterOk;

        private static void PrepareWave()
        {
            SaveSystem.PathOverride = WaveSaveFile;
            SaveSystem.Delete();
            foreach (string k in StringPrefs) s_StringPrefs[k] = PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k) : null;
            foreach (string k in IntPrefs) s_IntPrefs[k] = PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k) : (int?)null;
            InstallInput();
        }

        // The override stays set: a save on quit lands on the test's file, never on the player's.
        private static void EndWave()
        {
            ReleaseAll();
            RemoveInput();
            foreach (KeyValuePair<string, string> p in s_StringPrefs)
                if (p.Value == null) PlayerPrefs.DeleteKey(p.Key); else PlayerPrefs.SetString(p.Key, p.Value);
            foreach (KeyValuePair<string, int?> p in s_IntPrefs)
                if (p.Value == null) PlayerPrefs.DeleteKey(p.Key); else PlayerPrefs.SetInt(p.Key, p.Value.Value);
            PlayerPrefs.Save();
            SaveSystem.Delete();
        }

        private IEnumerator WaveRun()
        {
            Debug.Log("[Smoke] screen " + Screen.width + "x" + Screen.height + ", HUD style " + GameSettings.Current.Hud);
            yield return NewRun();
            if (!WorldUp) yield break;
            BindRun();
            _me.Untargetable = true;   // the dead roam the arena meanwhile: this run is about the controls, not the bites
            yield return Until(() => _gm.Wave >= 1, 15f);
            Check("wave 1 begins", _gm.Wave >= 1, "wave " + _gm.Wave);

            yield return MovementPass();
            yield return CombatPass();
            yield return CameraPass();
            yield return HudKeysPass();
            yield return GearPass();
            yield return BuildPass();
            yield return HomePass();
            yield return FixturePass();
            yield return PanelPass();
            yield return ScreenPass();
            yield return PausePass();
            yield return GrenadePass();
            if (_stats.IsDead)
            {
                yield return GameOver(s_Errors);
                yield break;
            }
            yield return WavesPass();
            yield return SaveQuitContinue();
            if (WorldUp) yield return Death();
        }

        #region The run
        private static bool WorldUp => GameSession.IsWaveSurvival && GameManager.Instance != null && Actor.LocalPlayer != null
            && FindAnyObjectByType<PauseMenuController>() != null && FindAnyObjectByType<HUDController>() != null;

        private void BindRun()
        {
            _gm = GameManager.Instance;
            _me = Actor.LocalPlayer;
            GameObject p = _me.gameObject;
            _stats = p.GetComponent<PlayerStats>();
            _pc = p.GetComponent<PlayerController>();
            _combat = p.GetComponent<PlayerCombat>();
            _inv = p.GetComponent<PlayerInventory>();
            _pi = p.GetComponent<PlayerInteractor>();
            _builder = p.GetComponent<PlayerBuilder>();
            _farming = p.GetComponent<PlayerFarming>();
            _hydration = p.GetComponent<PlayerHydration>();
            _sleep = p.GetComponent<PlayerSleep>();
            _gear = p.GetComponent<PlayerGear>();
            _seat = p.GetComponent<PlayerSeat>();
            _clothing = p.GetComponent<PlayerClothing>();
            _invUi = FindAnyObjectByType<InventoryUIController>();
            _health = FindAnyObjectByType<HealthPanelController>();
            _skills = FindAnyObjectByType<SkillsPanelController>();
            _pause = FindAnyObjectByType<PauseMenuController>();
            _hud = FindAnyObjectByType<HUDController>();
            _cam = IsoCameraFollow.Instance;
            var missing = new List<string>();
            if (_stats == null) missing.Add("stats");
            if (_pc == null) missing.Add("controller");
            if (_combat == null) missing.Add("combat");
            if (_inv == null) missing.Add("inventory");
            if (_pi == null) missing.Add("interactor");
            if (_builder == null) missing.Add("builder");
            if (_farming == null) missing.Add("farming");
            if (_hydration == null) missing.Add("hydration");
            if (_sleep == null) missing.Add("sleep");
            if (_gear == null) missing.Add("gear");
            if (_seat == null) missing.Add("seat");
            if (_clothing == null) missing.Add("clothing");
            if (_invUi == null) missing.Add("inventory panel");
            if (_health == null) missing.Add("health panel");
            if (_skills == null) missing.Add("skills panel");
            if (_cam == null) missing.Add("camera");
            Check("the survivor and the run's panels", missing.Count == 0, missing.Count == 0 ? "" : "missing: " + string.Join(", ", missing));
        }

        // Back to plain play between tests: no screen, panel, menu, job or build mode; standing, awake, isometric,
        // every key up, the cursor mid-screen.
        private IEnumerator Calm()
        {
            ReleaseAll();
            PointAtMiddle();
            if (UIScreen.Current != null) UIScreen.Current.Close();
            if (_pause != null && _pause.IsOpen) yield return Tap(Key.Escape);
            if (_invUi != null && _invUi.IsOpen) _invUi.Close();
            if (_health != null && _health.IsOpen) yield return Tap(Key.H);
            if (_skills != null && _skills.IsOpen) yield return Tap(Key.K);
            if (_builder != null && _builder.BuildMode) yield return Tap(Key.B);
            if (_pi != null)
            {
                if (_pi.IsSearching) _pi.CancelSearch();
                _pi.StopJob();
                _pi.CloseMenu();
            }
            if (_sleep != null && _sleep.IsSleeping)
            {
                yield return WaitGame(0.6f);
                yield return Tap(Key.T);
            }
            if (_seat != null && (_seat.IsSeated || _seat.IsLyingDown)) yield return Tap(Key.Space);
            if (_cam != null && _cam.View != CameraView.Isometric) _cam.SetView(CameraView.Isometric);
            if (_me != null)
            {
                _me.Untargetable = true;
                _me.Surrendered = false;
            }
            yield return null;
            yield return null;
        }

        private static Button TitleButton(MainMenuController menu, string label)
        {
            foreach (Button b in menu.GetComponentsInChildren<Button>(true))
                if (LabelOf(b) == label) return b;
            return null;
        }

        private bool Give(string id, int count)
        {
            ItemDef d = ItemDatabase.Get(id);
            if (d == null)
            {
                Check("item '" + id + "' in the content database", false);
                return false;
            }
            _inv.Inventory.Add(d, count, enforceWeight: false);
            return true;
        }

        private static int Zombies()
        {
            int n = 0;
            foreach (ZombieHealth z in FindObjectsByType<ZombieHealth>()) if (z != null && !z.IsDead) n++;
            return n;
        }

        // Cuts down the dead (within `radius` of `near`; everywhere when it's 0) as the survivor's kills.
        private int KillTheDead(Vector3 near, float radius)
        {
            int n = 0;
            foreach (ZombieHealth z in FindObjectsByType<ZombieHealth>())
            {
                if (z == null || z.IsDead) continue;
                if (radius > 0f && FlatDistance(z.transform.position, near) > radius) continue;
                var info = new DamageInfo(100000f, DamageKind.Melee, z.transform.position + Vector3.up, Vector3.forward, _me != null ? _me.gameObject : null);
                if (CombatResolver.Apply(z, info, _me)) n++;
            }
            return n;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private GameObject PauseRow(string label)
        {
            if (_pause == null) return null;
            foreach (PausePointer p in _pause.GetComponentsInChildren<PausePointer>(true))
                if (p.name == "Item " + label) return p.gameObject;
            return null;
        }

        private int PauseSelection => Field(_pause, "_selected") is int i ? i : -1;
        private bool HintsVisible => Field(Field(_hud, "_hints"), "_visible") is bool v && v;
        private bool RunPanelOpen => Field(_hud, "_status") is HudStatusRow s && s.InfoOpen;

        // A row of the HUD's action menu, as the mouse clicks it.
        private static GameObject MenuRow(int index)
        {
            string name = "Action" + index;
            foreach (Button b in FindObjectsByType<Button>())
                if (b != null && b.name == name && b.IsActive() && b.IsInteractable()) return b.gameObject;
            return null;
        }
        #endregion

        #region The title
        private IEnumerator NewRun()
        {
            MainMenuController menu = null;
            yield return Until(() => (menu = FindAnyObjectByType<MainMenuController>()) != null, 90f);
            Check("title screen up", menu != null);
            if (menu == null) yield break;
            yield return null;
            Button newRun = TitleButton(menu, "New run"), cont = TitleButton(menu, "Continue");
            Check("title: Wave survival offers New run", newRun != null && newRun.IsInteractable());
            Check("title: no Continue without a save", cont != null && !cont.IsInteractable(), cont == null ? "no Continue button" : "");
            if (newRun == null) yield break;
            int before = s_Errors;
            float t0 = Time.realtimeSinceStartup;
            Press(newRun.gameObject);
            yield return Until(() => WorldUp, 120f);
            Did("title: New run builds the wave world", before, WorldUp, (Time.realtimeSinceStartup - t0).ToString("0.0") + " s");
        }
        #endregion

        #region Moving, fighting, looking
        private IEnumerator MovementPass()
        {
            yield return Calm();
            if (!OpenGround(4f, out Vector3 spot)) Skip("movement on open ground", "no open ground found; walking where the survivor stands");
            float walk = 0f;
            foreach (var (key, name) in new[] { (Key.W, "W"), (Key.A, "A"), (Key.S, "S"), (Key.D, "D"),
                         (Key.UpArrow, "Up"), (Key.DownArrow, "Down"), (Key.LeftArrow, "Left"), (Key.RightArrow, "Right") })
            {
                Put(spot, 0f);
                yield return null;
                Vector3 from = _me.transform.position;
                float top = 0f;
                int b = s_Errors;
                yield return Hold(0.5f, () => top = Mathf.Max(top, _pc.CurrentSpeed), key);
                float moved = FlatDistance(_me.transform.position, from);
                walk = Mathf.Max(walk, top);
                Did("key " + name + " walks", b, top > 0.1f && moved > 0.2f, top.ToString("0.0") + " m/s, " + moved.ToString("0.00") + " m");
            }

            Put(spot, 0f);
            yield return null;
            float sprint = 0f;
            bool sprinting = false;
            int before = s_Errors;
            yield return Hold(0.6f, () =>
            {
                sprint = Mathf.Max(sprint, _pc.CurrentSpeed);
                sprinting |= _stats.Exertion == Exertion.Sprinting;
            }, Key.W, Key.LeftShift);
            Did("Shift + W sprints", before, sprinting && sprint > walk, sprint.ToString("0.0") + " m/s against " + walk.ToString("0.0") + " walking");

            Put(spot, 0f);
            yield return null;
            float sneak = 0f;
            bool sneaking = false;
            before = s_Errors;
            yield return Hold(0.6f, () =>
            {
                sneak = Mathf.Max(sneak, _pc.CurrentSpeed);
                sneaking |= _pc.IsSneaking;
            }, Key.W, Key.LeftCtrl);
            Did("Ctrl + W sneaks", before, sneaking && sneak > 0f && sneak < walk, sneak.ToString("0.0") + " m/s");
            Put(spot, 0f);
        }

        private IEnumerator CombatPass()
        {
            yield return Calm();
            GoToOpenGround(4f);
            AimClear(5f);
            int fired = 0, dry = 0, shoves = 0, reloads = 0, swings = 0;
            Action onFire = () => fired++, onDry = () => dry++, onShove = () => shoves++, onReload = () => reloads++, onSwing = () => swings++;
            _combat.OnFire += onFire;
            _combat.OnDryFire += onDry;
            _combat.OnShove += onShove;
            _combat.OnReload += onReload;
            _combat.OnSwing += onSwing;

            ItemDef pistol = ItemDatabase.Get("pistol");
            Check("the survivor starts with the pistol in hand", _combat.EquippedFirearm != null,
                _inv.EquippedWeapon != null ? _inv.EquippedWeapon.Id : "nothing in hand");
            if (_combat.EquippedFirearm == null && pistol != null && _inv.Has("pistol"))
            {
                _inv.Equip(pistol);
                yield return Wait(0.2f);
            }

            // LMB fires (or clicks on an empty magazine), R reloads, LMB fires from the fresh magazine. The weapon's own
            // timers run on the game's clock, so the waits between presses do too.
            yield return UntilGame(() => !_combat.IsReloading, 5f);
            int b = s_Errors, f0 = fired, d0 = dry;
            yield return Click(MouseButton.Left);
            yield return WaitGame(0.15f);
            Did("LMB fires the pistol", b, fired > f0 || dry > d0, fired > f0 ? _combat.LoadedAmmo + " rounds left" : "a dry click: the magazine was empty");
            yield return WaitGame(0.6f);
            b = s_Errors;
            int r0 = reloads, a0 = _combat.LoadedAmmo;
            yield return Tap(Key.R);
            yield return UntilGame(() => reloads > r0 && !_combat.IsReloading, 8f);
            Did("key R reloads the pistol", b, reloads > r0 || _combat.LoadedAmmo > a0, a0 + " -> " + _combat.LoadedAmmo + " rounds");
            yield return WaitGame(0.3f);
            b = s_Errors;
            f0 = fired;
            yield return Click(MouseButton.Left);
            yield return WaitGame(0.15f);
            Did("LMB fires a round from the fresh magazine", b, fired > f0, _combat.LoadedAmmo + " rounds left");

            yield return WaitGame(1f);
            b = s_Errors;
            int s0 = shoves;
            yield return Click(MouseButton.Right);
            yield return WaitGame(0.15f);
            Did("RMB shoves", b, shoves > s0);

            // Melee: the kitchen knife, then bare hands.
            ItemDef knife = ItemDatabase.Get("kitchenknife");
            if (knife != null && _inv.Has("kitchenknife"))
            {
                _inv.Equip(knife);
                yield return WaitGame(0.2f);
                b = s_Errors;
                int w0 = swings;
                yield return Click(MouseButton.Left);
                yield return WaitGame(0.15f);
                Did("LMB swings the kitchen knife", b, swings > w0);
                yield return WaitGame(1.5f);
            }
            else Skip("LMB swings the kitchen knife", "no kitchen knife in the starting kit");
            _inv.EquipBareHands();
            yield return WaitGame(0.2f);
            b = s_Errors;
            int w1 = swings;
            yield return Click(MouseButton.Left);
            yield return WaitGame(0.15f);
            Did("LMB punches bare-handed", b, swings > w1);
            if (pistol != null && _inv.Has("pistol")) _inv.Equip(pistol);
            yield return WaitGame(0.2f);

            _combat.OnFire -= onFire;
            _combat.OnDryFire -= onDry;
            _combat.OnShove -= onShove;
            _combat.OnReload -= onReload;
            _combat.OnSwing -= onSwing;
        }

        private IEnumerator CameraPass()
        {
            yield return Calm();
            GoToOpenGround(3f);
            PointAtMiddle();
            Check("the run starts in the isometric view", _cam.View == CameraView.Isometric, _cam.View.ToString());

            // Isometric: Z turns the view a quarter (four turns come round), the wheel zooms, Alt holds up the aim line.
            float yaw0 = _cam.IsoYaw;
            int b = s_Errors;
            yield return Tap(Key.Z);
            Did("key Z turns the isometric view", b, Mathf.Abs(Mathf.DeltaAngle(yaw0, _cam.IsoYaw)) > 80f, yaw0.ToString("0") + " -> " + _cam.IsoYaw.ToString("0") + " degrees");
            for (int i = 0; i < 3; i++) yield return Tap(Key.Z);
            Check("four turns come round", Mathf.Abs(Mathf.DeltaAngle(yaw0, _cam.IsoYaw)) < 1f, _cam.IsoYaw.ToString("0") + " degrees");

            int zoom0 = _cam.IsoZoomStep;
            float notch = zoom0 > 0 ? 1f : -1f;
            b = s_Errors;
            yield return Wheel(notch);
            Did("the wheel zooms the isometric view", b, _cam.IsoZoomStep != zoom0, "step " + zoom0 + " -> " + _cam.IsoZoomStep);
            yield return Wait(0.1f);
            yield return Wheel(-notch);
            Check("the wheel zooms back", _cam.IsoZoomStep == zoom0, "step " + _cam.IsoZoomStep);

            b = s_Errors;
            yield return Hold(0.4f, null, Key.LeftAlt);
            Did("Alt holds up the aim line", b, true);

            // V: over the shoulder, where Z swaps the shoulder, the mouse looks, RMB aims and MMB shoves.
            b = s_Errors;
            yield return Tap(Key.V);
            Did("key V: over the shoulder", b, _cam.View == CameraView.ThirdPerson, _cam.View.ToString());
            if (_cam.View == CameraView.ThirdPerson)
            {
                float side = _cam.ShoulderOffset.x;
                b = s_Errors;
                yield return Tap(Key.Z);
                Did("key Z swaps the shoulder", b, Mathf.Sign(_cam.ShoulderOffset.x) != Mathf.Sign(side));
                yield return Tap(Key.Z);
                yield return MouseLook();
                yield return Sights("over the shoulder");
            }

            // V: first person, where Z holds the breath on the sights.
            b = s_Errors;
            yield return Tap(Key.V);
            Did("key V: first person", b, _cam.View == CameraView.FirstPerson, _cam.View.ToString());
            if (_cam.View == CameraView.FirstPerson && _combat.EquippedFirearm != null)
            {
                bool ads = false;
                b = s_Errors;
                yield return HoldButton(MouseButton.Right, 0.5f, () => ads |= _combat.AimingDownSights, Key.Z);
                Did("RMB + Z: on the sights, holding the breath", b, ads);
            }

            b = s_Errors;
            yield return Tap(Key.V);
            Did("key V: bodycam", b, _cam.View == CameraView.Bodycam, _cam.View.ToString());
            b = s_Errors;
            yield return Tap(Key.V);
            Did("key V: back to isometric", b, _cam.View == CameraView.Isometric, _cam.View.ToString());
        }

        private IEnumerator MouseLook()
        {
            yield return Wait(0.2f);
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Skip("the mouse turns the view", "this player can't lock the cursor (no window)");
                yield break;
            }
            Vector3 before = Flat(Camera.main.transform.forward);
            int b = s_Errors;
            yield return Look(new Vector2(25f, 0f), 12);
            float turned = Vector3.Angle(before, Flat(Camera.main.transform.forward));
            Did("the mouse turns the view", b, turned > 2f, turned.ToString("0") + " degrees");
        }

        private IEnumerator Sights(string view)
        {
            if (_combat.EquippedFirearm == null)
            {
                Skip("RMB aims down the sights (" + view + ")", "no gun in hand");
                yield break;
            }
            bool ads = false;
            int b = s_Errors;
            yield return HoldButton(MouseButton.Right, 0.4f, () => ads |= _combat.AimingDownSights);
            Did("RMB aims down the sights (" + view + ")", b, ads);
            yield return WaitGame(1f);
            int shoves = 0;
            Action onShove = () => shoves++;
            _combat.OnShove += onShove;
            b = s_Errors;
            yield return Click(MouseButton.Middle);
            yield return WaitGame(0.15f);
            _combat.OnShove -= onShove;
            Did("MMB shoves with a gun in hand (" + view + ")", b, shoves > 0);
        }
        #endregion

        #region The HUD's keys and the gadgets
        private IEnumerator HudKeysPass()
        {
            yield return Calm();
            bool hints = HintsVisible;
            int b = s_Errors;
            yield return Tap(Key.F1);
            Did("key F1 toggles the key hints", b, HintsVisible != hints, (hints ? "shown" : "hidden") + " -> " + (HintsVisible ? "shown" : "hidden"));
            yield return Tap(Key.F1);

            bool info = RunPanelOpen;
            b = s_Errors;
            yield return Tap(Key.M);
            Did("key M toggles the run panel", b, RunPanelOpen != info);
            yield return Tap(Key.M);

            // The HUD's own buttons (the fixture pass clicks the action menu's rows), with the run panel open: its box then
            // shows whatever the HUD style.
            if (!RunPanelOpen) yield return Tap(Key.M);
            yield return PressEverything("HUD", () => _hud != null ? _hud.transform : null, null,
                (c, l) => c.name.StartsWith("Action", StringComparison.Ordinal) ? "an action-menu row" : null, null, 30);
            if (RunPanelOpen != info) yield return Tap(Key.M);

            b = s_Errors;
            yield return Tap(Key.F3);
            Did("key F3 puts the hands up", b, _me.Surrendered);
            b = s_Errors;
            yield return Tap(Key.F3);
            Did("key F3 puts them down", b, !_me.Surrendered);

            // O: the detector (none, then one in the pack).
            var detector = _me.GetComponent<DetectorDevice>();
            b = s_Errors;
            yield return Tap(Key.O);
            Did("key O with no detector", b, detector == null || !detector.On);
            if (detector != null && Give("detector_echo", 1))
            {
                yield return null;
                b = s_Errors;
                yield return Tap(Key.O);
                Did("key O switches the detector on", b, detector.On);
                b = s_Errors;
                yield return Tap(Key.O);
                Did("key O switches it off", b, !detector.On);
            }

            // X: a bolt where the cursor points (none, then three in the pack).
            AimClear(6f);
            b = s_Errors;
            int bolts = _inv.Count(BoltThrower.BoltId);
            yield return Tap(Key.X);
            Did("key X with no bolts", b, bolts > 0 || _inv.Count(BoltThrower.BoltId) == 0);
            if (Give(BoltThrower.BoltId, 3))
            {
                yield return WaitGame(0.7f);   // the thrower's own pause between bolts
                bolts = _inv.Count(BoltThrower.BoltId);
                b = s_Errors;
                yield return Tap(Key.X);
                Did("key X throws a bolt", b, _inv.Count(BoltThrower.BoltId) == bolts - 1, bolts + " -> " + _inv.Count(BoltThrower.BoltId));
            }

            // The keys with nothing to do in a lone wave run must still be harmless.
            foreach (var (key, name) in new[] { (Key.F2, "F2 (wipe the visor)"), (Key.F4, "F4 (the squad, online only)"), (Key.U, "U (orders, with companions)") })
            {
                b = s_Errors;
                yield return Tap(key);
                yield return null;
                string opened = UIScreen.Current != null ? "opened " + UIScreen.Current.GetType().Name : "";
                if (UIScreen.Current != null) UIScreen.Current.Close();
                Did("key " + name, b, true, opened);
            }
            foreach (var (key, name) in new[] { (Key.Y, "Y held (the radio)"), (Key.CapsLock, "Caps Lock held (push to talk)") })
            {
                b = s_Errors;
                yield return Hold(0.4f, null, key);
                Did("key " + name, b, true);
            }
        }

        private IEnumerator GearPass()
        {
            yield return Calm();
            int b = s_Errors;
            yield return Tap(Key.L);
            yield return Tap(Key.N);
            Did("keys L and N with no flashlight or goggles", b, !_gear.FlashlightOn && !_gear.NightVisionOn);

            if (Give("flashlight", 1))
            {
                yield return Until(() => _gear.HasFlashlight, 1f);
                b = s_Errors;
                yield return Tap(Key.L);
                yield return null;
                Did("key L switches the flashlight on", b, _gear.FlashlightOn, "charge " + _gear.FlashlightCharge01.ToString("0.00"));
                b = s_Errors;
                yield return Tap(Key.L);
                yield return null;
                Did("key L switches it off", b, !_gear.FlashlightOn);
            }

            ItemDef nvg = ItemDatabase.Get("nvg");
            Check("item 'nvg' in the content database", nvg != null);
            if (nvg != null)
            {
                _clothing.Equip(nvg);
                yield return Until(() => _gear.HasNightVision, 1f);
                b = s_Errors;
                yield return Tap(Key.N);
                yield return null;
                Did("key N switches the night vision on", b, _gear.NightVisionOn, "charge " + _gear.NightVisionCharge01.ToString("0.00"));
                b = s_Errors;
                yield return Tap(Key.N);
                yield return null;
                Did("key N switches it off", b, !_gear.NightVisionOn);
                _clothing.Remove(nvg.ClothingSlot);
            }
        }

        private IEnumerator GrenadePass()
        {
            yield return Calm();
            GoToOpenGround(6f);
            AimClear(12f);
            int b = s_Errors;
            int had = _inv.Count("grenade");
            yield return Tap(Key.Backquote);
            Did("key ` with no grenade", b, had > 0 || _inv.Count("grenade") == 0);
            if (!Give("grenade", 1)) yield break;
            yield return WaitGame(1.2f);   // the key's own second between throws
            AimClear(12f);
            had = _inv.Count("grenade");
            b = s_Errors;
            yield return Tap(Key.Backquote);
            yield return null;
            Did("key ` throws a grenade", b, _inv.Count("grenade") == had - 1);
            yield return WaitGame(5f);
            Check("the survivor lived through their own grenade", !_stats.IsDead, "health " + _stats.Health.ToString("0"));
        }
        #endregion

        #region Build mode
        private IEnumerator BuildPass()
        {
            yield return Calm();
            GoToOpenGround(4f);
            Vector3 dir = AimClear(3f);
            int b = s_Errors;
            yield return Tap(Key.B);
            Did("key B enters build mode", b, _builder.BuildMode && UIState.BuildMode);
            if (!_builder.BuildMode) yield break;

            PlayerBuilder.BuildSelection[] pieces =
            {
                PlayerBuilder.BuildSelection.Barricade, PlayerBuilder.BuildSelection.Floor, PlayerBuilder.BuildSelection.Wall,
                PlayerBuilder.BuildSelection.Doorway, PlayerBuilder.BuildSelection.Roof, PlayerBuilder.BuildSelection.Window,
                PlayerBuilder.BuildSelection.Demolish,
            };
            for (int i = 0; i < pieces.Length; i++)
            {
                b = s_Errors;
                yield return Tap(Digit(i + 1));
                Did("build mode: key " + (i + 1) + " picks " + pieces[i], b, _builder.Selection == pieces[i], _builder.Selection.ToString());
            }
            PlayerBuilder.BuildSelection was = _builder.Selection;
            b = s_Errors;
            yield return Wheel(1f);
            Did("build mode: the wheel cycles the pieces", b, _builder.Selection != was, was + " -> " + _builder.Selection);
            yield return Wheel(-1f);
            Check("build mode: and back", _builder.Selection == was, _builder.Selection.ToString());

            // 1 and LMB put up a barricade where the survivor faces; 7 and LMB take it down again.
            yield return Tap(Key.Digit1);
            PointAt(_me.transform.position + dir * 3f);
            yield return Wait(0.2f);
            int bars = Barricade.All.Count;
            string prompt = _builder.Prompt;
            b = s_Errors;
            yield return Click(MouseButton.Left);
            yield return Until(() => _builder.IsBuilding, 0.5f);
            bool started = _builder.IsBuilding;
            yield return Until(() => !_builder.IsBuilding, 20f);
            Did("build mode: LMB builds a barricade", b, started && Barricade.All.Count > bars, Join(prompt, (Barricade.All.Count - bars) + " built"));
            Barricade built = Barricade.All.Count > bars ? Barricade.All[Barricade.All.Count - 1] : null;
            if (built != null)
            {
                yield return Tap(Key.Digit7);
                PointAt(built.Position + Vector3.up * 0.6f);
                yield return Wait(0.2f);
                prompt = _builder.Prompt;
                b = s_Errors;
                yield return Click(MouseButton.Left);
                yield return Until(() => _builder.IsBuilding, 0.5f);
                yield return Until(() => !_builder.IsBuilding, 20f);
                Did("build mode: 7 and LMB demolish it", b, built == null || !Barricade.All.Contains(built), prompt);
            }

            b = s_Errors;
            yield return Tap(Key.Escape);
            yield return null;
            Did("build mode: Esc leaves it", b, !_builder.BuildMode && !UIState.BuildMode);
            Check("build mode: that Esc doesn't also pause the game", !_pause.IsOpen);
            if (_pause.IsOpen) yield return Tap(Key.Escape);

            yield return Tap(Key.B);
            b = s_Errors;
            yield return Tap(Key.B);
            Did("key B leaves build mode", b, !_builder.BuildMode && !UIState.BuildMode);
        }
        #endregion

        #region The home: farm, water, bed, door, crate, the floor
        private IEnumerator HomePass()
        {
            yield return Calm();
            yield return HomeWindows();
            yield return Calm();
            yield return Farm();
            yield return Calm();
            yield return Water();
            yield return Calm();
            yield return Bed();
            yield return Calm();
            yield return Door();
            yield return Calm();
            yield return Crate();
            yield return Calm();
            yield return Pickup();
        }

        // The home's windows are seen through: an outside one, shut with its curtains open, has see-through glass (no window of
        // the home has the old opaque glass) and nothing solid across it, so the eye (and the dead's) goes straight through.
        // With -clonez-smoke-shots, pictures of it from the yard and from the room.
        private IEnumerator HomeWindows()
        {
            HomeWindow win = null;
            int clear = 0, opaque = 0;
            foreach (HomeWindow w in FindObjectsByType<HomeWindow>())
            {
                if (w == null || w is RaidWindow || w.transform.parent == null) continue;
                foreach (Renderer r in w.transform.parent.GetComponentsInChildren<Renderer>(true))
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m == null) continue;
                        if (m.name.StartsWith("RaidPaletteWindowGlass", StringComparison.Ordinal) && m.renderQueue >= 3000) clear++;
                        else if (m.name.StartsWith("RaidPaletteGlass", StringComparison.Ordinal)) opaque++;
                    }
                if (win == null && w.Outward.sqrMagnitude > 0.5f && !w.CurtainsDrawn && !w.IsOpen) win = w;
            }
            Check("windows: the home's glass is see-through", clear > 0 && opaque == 0, clear + " see-through, " + opaque + " opaque");
            if (win == null)
            {
                Check("windows: an outside window of the home, shut, its curtains open", false);
                yield break;
            }
            Vector3 at = win.InteractPoint, outward = win.Outward.normalized;
            Vector3 outside = at + outward * 1.5f, inside = at - outward * 1.5f;
            bool blocked = Physics.Linecast(outside, inside, out RaycastHit hit, ~(1 << CloneZConfig.IgnoreRaycastLayer), QueryTriggerInteraction.Ignore);
            Check("windows: nothing solid across " + win.DisplayName.ToLowerInvariant() + ", shut, its curtains open", !blocked,
                blocked ? hit.collider.name + " at " + hit.distance.ToString("0.00") + " m" : "the eye goes through");
            float share = OpenShare(win, outward);
            Check("windows: " + win.DisplayName.ToLowerInvariant() + "'s glass isn't covered", share >= 0.6f,
                (share * 100f).ToString("0") + "% of it clear of anything opaque drawn across it");
            ShotFrom("home-window-out", at + outward * 3.2f + Vector3.up * 0.4f, at, 55f);
            ShotFrom("home-window-in", at - outward * 2.6f + Vector3.up * 0.2f, at + outward * 4f, 65f);
            yield return null;
        }

        // How much of a window's opening (its curtains' screen), looked at square on from outside, nothing opaque is drawn
        // across (its sashes' frames and bars take a little): rays over a grid against a stand-in collider made of the opaque
        // triangles of every part of the window's piece that shows.
        private static float OpenShare(HomeWindow win, Vector3 outward)
        {
            BoxCollider screen = null;
            foreach (BoxCollider b in win.GetComponentsInChildren<BoxCollider>(true))
                if (b.name == "Screen") screen = b;
            if (screen == null || win.transform.parent == null) return 0f;
            var made = new List<UnityEngine.Object>();
            var stand = new List<MeshCollider>();
            try
            {
                foreach (MeshRenderer r in win.transform.parent.GetComponentsInChildren<MeshRenderer>(false))
                {
                    var mf = r.GetComponent<MeshFilter>();
                    Mesh mesh = mf != null ? mf.sharedMesh : null;
                    if (!r.enabled || mesh == null || !mesh.isReadable) continue;
                    var tris = new List<int>();
                    Material[] mats = r.sharedMaterials;
                    for (int s = 0; s < mesh.subMeshCount; s++)
                        if (s < mats.Length && mats[s] != null && mats[s].renderQueue < 3000) tris.AddRange(mesh.GetTriangles(s));
                    if (tris.Count == 0) continue;
                    var solid = new Mesh { indexFormat = mesh.indexFormat };
                    solid.SetVertices(mesh.vertices);
                    solid.SetTriangles(tris, 0);
                    var go = new GameObject("[Smoke] opaque");
                    go.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
                    go.transform.localScale = r.transform.lossyScale;
                    MeshCollider mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = solid;
                    stand.Add(mc);
                    made.Add(solid);
                    made.Add(go);
                }
                Physics.SyncTransforms();
                Vector3 size = screen.size, across = screen.transform.right, up = screen.transform.up;
                if (Mathf.Abs(Vector3.Dot(across, outward)) > 0.7f)
                {
                    across = screen.transform.forward;
                    size = new Vector3(size.z, size.y, size.x);
                }
                Vector3 middle = screen.transform.TransformPoint(screen.center);
                int clear = 0, all = 0;
                for (int i = 0; i < 8; i++)
                    for (int k = 0; k < 6; k++)
                    {
                        Vector3 p = middle + across * (size.x * 0.8f * ((i + 0.5f) / 8f - 0.5f)) + up * (size.y * 0.8f * ((k + 0.5f) / 6f - 0.5f));
                        var ray = new Ray(p + outward * 0.6f, -outward);
                        bool covered = false;
                        foreach (MeshCollider mc in stand)
                            if (mc.Raycast(ray, out _, 1.2f)) { covered = true; break; }
                        all++;
                        if (!covered) clear++;
                    }
                return all > 0 ? clear / (float)all : 0f;
            }
            finally
            {
                foreach (UnityEngine.Object o in made) if (o != null) Destroy(o);
            }
        }

        private IEnumerator Farm()
        {
            FarmPlot plot = FarmPlot.All.FirstOrDefault(p => p != null && p.State == FarmPlot.PlotState.Empty) ?? FarmPlot.All.FirstOrDefault(p => p != null);
            if (plot == null)
            {
                Check("key F: a farm plot in the yard", false, "none");
                yield break;
            }
            Vector3 at = plot.transform.position + Vector3.up * 0.3f;
            if (!StandAt(at, CloneZConfig.FarmActionRange - 0.3f, plot.transform))
            {
                Check("key F: a spot by the farm plot", false);
                yield break;
            }
            PointAt(at);
            yield return Wait(0.2f);
            FarmPlot.PlotState was = plot.State;
            string prompt = _farming.Prompt;
            int b = s_Errors;
            yield return Tap(Key.F);
            yield return Until(() => _farming.IsBusy, 0.5f);
            bool started = _farming.IsBusy;
            yield return Until(() => !_farming.IsBusy, 20f);
            Did("key F tends the farm plot", b, started && !_farming.IsBusy && (was != FarmPlot.PlotState.Empty || plot.State != was),
                Join(prompt, was + " -> " + plot.State));
        }

        private IEnumerator Water()
        {
            bool placed = false;
            foreach (WaterSource w in WaterSource.All)
            {
                if (w == null || !w.HasWater) continue;
                if (StandAt(w.transform.position + Vector3.up * 0.5f, CloneZConfig.WaterSourceRange - 0.4f, w.transform))
                {
                    placed = true;
                    break;
                }
            }
            if (!placed)
            {
                Check("key G: a spot by a water source", false, WaterSource.All.Count + " source(s)");
                yield break;
            }
            yield return Wait(0.2f);
            // With empty bottles G fills one; once they're all full it drinks.
            _stats.Thirst = Mathf.Max(_stats.Thirst, 0.5f);
            for (int i = 0; i < 5; i++)
            {
                int empty = _inv.Count("emptybottle");
                float thirst = _stats.Thirst;
                string prompt = _hydration.Prompt;
                int b = s_Errors;
                yield return Tap(Key.G);
                yield return null;
                if (empty > 0) Did("key G fills a bottle", b, _inv.Count("emptybottle") < empty, prompt);
                else
                {
                    Did("key G drinks", b, _stats.Thirst < thirst, Join(prompt, "thirst " + thirst.ToString("0.00") + " -> " + _stats.Thirst.ToString("0.00")));
                    break;
                }
            }
        }

        private IEnumerator Bed()
        {
            SleepSpot bed = SleepSpot.All.FirstOrDefault(s => s != null);
            if (bed == null)
            {
                Check("key T: a bed", false);
                yield break;
            }
            if (!StandAt(bed.transform.position + Vector3.up * 0.5f, CloneZConfig.SleepRange - 0.4f, bed.transform))
            {
                Check("key T: a spot by the bed", false);
                yield break;
            }
            int cleared = KillTheDead(bed.transform.position, CloneZConfig.SleepDangerRadius + 4f);
            yield return Wait(0.3f);
            Action undo = HoldSleep();
            try
            {
                bool can = _sleep.CanSleep(out string why);
                int b = s_Errors;
                yield return Tap(Key.T);
                yield return null;
                Did("key T goes to sleep in the bed", b, _sleep.IsSleeping,
                    Join(can ? "" : "can't: " + why, cleared > 0 ? cleared + " of the dead cleared from round the bed first" : ""));
                if (!_sleep.IsSleeping) yield break;
                yield return WaitGame(0.6f);   // sleep ignores the keys for its first moments (game time)
                b = s_Errors;
                yield return WakeBy(Key.T);
                yield return null;
                Did("key T wakes", b, !_sleep.IsSleeping && !UIState.GameplayBlocked, _sleep.IsSleeping ? _wakeSeen : "");
                if (_sleep.IsSleeping) yield break;

                yield return WaitGame(0.3f);
                _stats.Fatigue = Mathf.Max(_stats.Fatigue, 0.9f);
                b = s_Errors;
                yield return Tap(Key.T);
                yield return null;
                bool again = _sleep.CanSleep(out why) || _sleep.IsSleeping;
                Did("key T goes back to sleep", b, _sleep.IsSleeping, again ? "" : "can't: " + why);
                if (!_sleep.IsSleeping) yield break;
                yield return WaitGame(0.6f);
                b = s_Errors;
                yield return WakeBy(Key.Space);
                yield return null;
                Did("Space wakes too", b, !_sleep.IsSleeping && !UIState.GameplayBlocked, _sleep.IsSleeping ? _wakeSeen : "");
            }
            finally
            {
                undo();
            }
        }

        // Makes a sleep last until a key wakes it: a rested survivor, or one lying down between 7 and 11 in the morning
        // (the dawn wake), is up again the moment the sleep settles. Tires the survivor and sets the clock to 10 pm;
        // returns what puts both back.
        private Action HoldSleep()
        {
            float fatigue = _stats.Fatigue;
            TimeOfDay clock = TimeOfDay.Instance;
            int day = clock != null ? clock.Day : 1;
            float hour = clock != null ? clock.Hour : 0f;
            _stats.Fatigue = Mathf.Max(fatigue, 0.9f);
            if (clock != null) clock.RestoreClock(day, 22f);
            return () =>
            {
                _stats.Fatigue = fatigue;
                if (clock != null) clock.RestoreClock(day, hour);
            };
        }

        // A key pressed to wake the sleeper; _wakeSeen says what the sleep had counted and the key's state on the frame
        // it went down (for when it doesn't wake).
        private string _wakeSeen;

        private IEnumerator WakeBy(Key key)
        {
            object before = Field(_sleep, "_sleepElapsed");
            KeyDown(key);
            yield return null;
            _wakeSeen = "asleep " + Seconds(before) + " -> " + Seconds(Field(_sleep, "_sleepElapsed")) + ", " + key + " " + (s_Kb[key].isPressed ? "down" : "up")
                + (Keyboard.current == s_Kb ? "" : ", another keyboard is current") + ", frame " + Time.deltaTime.ToString("0.000") + " s, time scale " + Time.timeScale;
            KeyUp(key);
            yield return null;
            yield return Pace();
        }

        private static string Seconds(object o) => o is float f ? f.ToString("0.00") + " s" : "?";

        private IEnumerator Door()
        {
            foreach (HomeDoor door in FindObjectsByType<HomeDoor>())
            {
                if (door == null || !door.IsAvailable || !StandAt(door.InteractPoint, door.Reach, door.OcclusionRoot)) continue;
                PointAt(door.InteractPoint);
                yield return Until(() => _pi.FixtureTarget == door, 0.5f);
                if (_pi.FixtureTarget != door) continue;
                bool open = door.IsOpen;
                int b = s_Errors;
                yield return Tap(Key.E);
                yield return Wait(0.3f);
                Did("key E " + (open ? "closes" : "opens") + " the " + door.DisplayName, b, door.IsOpen != open);
                b = s_Errors;
                yield return Tap(Key.E);
                yield return Wait(0.3f);
                Did("key E " + (open ? "opens" : "closes") + " it again", b, door.IsOpen == open);
                yield break;
            }
            Check("key E uses a door", false, "no door of the home could be put under the cursor");
        }

        private IEnumerator Crate()
        {
            LootContainer crate = LootContainer.All.FirstOrDefault(c => c != null && !c.IsFixture && c.gameObject.activeInHierarchy);
            if (crate == null)
            {
                Skip("key E opens a container", "no free-standing container in this run");
                yield break;
            }
            Vector3 at = crate.transform.position + Vector3.up * 0.4f;
            if (!StandAt(at, 1.4f, crate.transform))
            {
                Check("key E: a spot by the " + crate.DisplayName, false);
                yield break;
            }
            int b = s_Errors;
            yield return OpenCrate(crate);
            Did("key E searches and opens the " + crate.DisplayName, b, _invUi.IsOpen && _invUi.CurrentContainer == crate,
                _pi.Target == crate || _invUi.IsOpen ? "" : "the cursor picked '" + _pi.FocusName + "'");
            if (!_invUi.IsOpen) yield break;

            // Every button of the inventory with the crate open (take, store, take all, transfer, sort; the picker can
            // switch it to the floor and back), in each of its views. Drop All waits for the panel pass.
            Func<Transform> inventory = () => _invUi.IsOpen ? _invUi.transform : null;
            foreach (bool grid in new[] { true, false })
            {
                if (!_invUi.IsOpen) yield return OpenCrate(crate, true);
                yield return InventoryView(grid);
                string other = grid ? "List view" : "Grid view";
                yield return PressEverything("inventory with the " + crate.DisplayName + (grid ? " (grid view)" : " (list view)"), inventory,
                    () => OpenCrate(crate, true), (c, l) => l == other ? "switches the view; pressed on its own" : l == "Drop All" ? "pressed in the panel pass" : null,
                    l => l == "X" || l.StartsWith("Close", StringComparison.Ordinal), 200);
            }

            if (!_invUi.IsOpen) yield return OpenCrate(crate, true);
            // Esc lets go of whatever is open inside the inventory first (a menu, a held item), then closes it.
            b = s_Errors;
            int presses = 0;
            while (_invUi.IsOpen && presses < 3)
            {
                presses++;
                yield return Tap(Key.Escape);
                yield return null;
            }
            Did("Esc closes the inventory", b, !_invUi.IsOpen, presses > 1 ? "the first " + (presses - 1) + " closed what was open inside it" : "");
            Check("that Esc doesn't also pause the game", !_pause.IsOpen);
            if (_pause.IsOpen) yield return Tap(Key.Escape);

            // R rummages the nearest container (when another thing has the focus).
            PointAt(_me.transform.position);
            yield return null;
            b = s_Errors;
            yield return Tap(Key.R);
            yield return null;
            Did("key R by the " + crate.DisplayName, b, true, _pi.IsSearching ? "rummaging" : _invUi.IsOpen ? "opened it" : "nothing to rummage: '" + _pi.FocusName + "' had the focus");
            _pi.StopJob();
            if (_invUi.IsOpen) _invUi.Close();
        }

        // Opens the crate with E. `orElse`: the crate only has to be open again to go on pressing the inventory's buttons;
        // when things dropped at the survivor's feet take the cursor's pick from it, it's opened directly (E on it has its
        // own check).
        private IEnumerator OpenCrate(LootContainer crate, bool orElse = false)
        {
            PointAt(crate.transform.position + Vector3.up * 0.4f);
            yield return Until(() => _pi.Target == crate, 0.5f);
            if (_pi.Target == crate || !orElse)
            {
                yield return Tap(Key.E);
                yield return Until(() => _invUi.IsOpen, crate.SearchTime + 4f);
            }
            if (!_invUi.IsOpen && orElse) _invUi.OpenContainer(crate);
        }

        private IEnumerator Pickup()
        {
            GoToOpenGround(2f);
            if (!_inv.Has("apple") && !Give("apple", 1)) yield break;
            ItemStack apple = _inv.Inventory.Stacks.FirstOrDefault(s => s != null && s.Def != null && s.Def.Id == "apple");
            int had = _inv.Count("apple");
            _inv.Drop(apple, 1);
            yield return null;
            Check("an apple dropped at the survivor's feet", _inv.Count("apple") == had - 1);
            PointAt(_me.transform.position);
            yield return Until(() => _pi.PickupTarget != null, 0.5f);

            int notes = 0;
            Action<string> onNotice = m => { if (m != null && m.StartsWith("Item:", StringComparison.Ordinal)) notes++; };
            _pi.Notice += onNotice;
            int b = s_Errors;
            yield return Tap(Key.F);
            yield return null;
            _pi.Notice -= onNotice;
            Did("key F looks at the thing on the ground", b, notes > 0, _pi.PickupTarget != null ? _pi.PickupTarget.PickupLabel : "nothing in focus");

            b = s_Errors;
            yield return Tap(Key.E);
            yield return Until(() => _inv.Count("apple") == had, 1f);
            Did("key E picks it up", b, _inv.Count("apple") == had, _inv.Count("apple") + " of " + had);
        }
        #endregion

        #region Every action of every kind of fixture
        private IEnumerator FixturePass()
        {
            yield return Calm();
            var picks = new List<Interactable>();
            var kinds = new HashSet<string>();
            foreach (Interactable f in Interactable.All.ToArray())
                if (f != null && f.IsAvailable && kinds.Add(f.GetType().Name + ":" + f.DisplayName)) picks.Add(f);
            Debug.Log("[Smoke] fixtures: " + Interactable.All.Count + " in the run, " + picks.Count + " kinds: "
                + string.Join(", ", picks.Select(f => f.DisplayName)));
            Check("fixtures in the home and the yard", picks.Count > 0, picks.Count + " kinds");
            foreach (Interactable f in picks)
            {
                yield return Calm();
                if (f != null) yield return EveryAction(f);
            }
        }

        // Stands at the fixture, points at it, opens its menu (Q) and chooses each of its actions once, by its number key
        // or by clicking its row in turn, then undoes whatever the action left (a job, a seat, sleep, a screen).
        private IEnumerator EveryAction(Interactable f)
        {
            string name = "fixture '" + f.DisplayName + "'";
            var ran = new HashSet<string>();
            var greyed = new List<string>();
            for (int round = 0; round < 12; round++)
            {
                if (f == null || !f.IsAvailable) break;
                if (round == 0 || FlatDistance(f.InteractPoint, _me.transform.position) > f.Reach * 0.9f)
                    if (!StandAt(f.InteractPoint, f.Reach, f.OcclusionRoot))
                    {
                        Skip(name, "no clear spot within its reach");
                        yield break;
                    }
                PointAt(f.InteractPoint);
                yield return Until(() => _pi.FixtureTarget == f, 0.4f);
                bool focused = _pi.FixtureTarget == f;
                int b = s_Errors;
                if (focused) yield return Tap(Key.Q);
                else _pi.OpenMenu(f);
                yield return null;
                yield return null;
                if (!_pi.MenuOpen || _pi.MenuTarget != f)
                {
                    // An action can leave a fixture with nothing more to do; otherwise the menu should have opened.
                    var offered = new List<InteractAction>();
                    f.GetActions(_pi.Context, offered);
                    if (round > 0 && offered.Count == 0) break;
                    Check(name + ": its action menu opens", false, Join(focused ? "Q did nothing" : "it wouldn't open",
                        offered.Count + " action(s) on offer", _pi.IsBusy ? "the survivor is busy" : "", "focus: '" + _pi.FocusName + "'"));
                    yield break;
                }
                if (round == 0)
                    Did(name + ": " + (focused ? "Q opens its action menu" : "its action menu opens (the cursor picks '" + _pi.FocusName + "' from here)"), b, true);

                IReadOnlyList<InteractAction> acts = _pi.MenuActions;
                int index = -1;
                for (int i = 0; i < acts.Count; i++)
                {
                    InteractAction a = acts[i];
                    if (a == null || ran.Contains(a.Label)) continue;
                    if (!a.Enabled)
                    {
                        ran.Add(a.Label);
                        greyed.Add(a.Label + (string.IsNullOrEmpty(a.DisabledReason) ? "" : " (" + a.DisabledReason + ")"));
                        continue;
                    }
                    index = i;
                    break;
                }
                if (index < 0)
                {
                    b = s_Errors;
                    if (focused)
                    {
                        yield return Tap(Key.Q);
                        Did(name + ": Q closes its menu", b, !_pi.MenuOpen);
                    }
                    else _pi.CloseMenu();
                    break;
                }

                InteractAction act = acts[index];
                ran.Add(act.Label);
                GameObject row = index >= 9 || _menuChoice++ % 2 == 1 ? MenuRow(index) : null;
                string how = row != null ? " (clicked)" : index < 9 ? " (key " + (index + 1) + ")" : "";
                b = s_Errors;
                if (row != null) Press(row);
                else if (index < 9) yield return Tap(Digit(index + 1));
                else _pi.ChooseMenu(index);
                var notes = new List<string>();
                yield return AfterAction(act, notes);
                Did(name + ": '" + act.Label + "'" + how, b, _afterOk, string.Join(", ", notes));
            }
            if (greyed.Count > 0) Debug.Log("[Smoke] " + name + ": greyed out: " + string.Join(", ", greyed));
        }

        // What an action can leave behind, undone the way a player would: a timed job (let it finish, or stop it), a
        // container being searched (let it, or stop it), the survivor seated, lying down or asleep, a screen or the
        // inventory open. Jobs and sleep are timed on the game's clock. _afterOk is false when a control failed.
        private IEnumerator AfterAction(InteractAction act, List<string> notes)
        {
            _afterOk = true;
            yield return null;
            yield return null;
            Action undoSleep = _sleep.IsSleeping ? HoldSleep() : null;   // before any pause: a sleep settles in 0.4 s
            yield return Pace();
            if (_pi.IsWorking)
            {
                if (_pi.JobOpenEnded)
                {
                    yield return WaitGame(1f);
                    yield return Tap(Key.E);
                    notes.Add("open-ended; E stopped it");
                }
                else
                {
                    float limit = Mathf.Min(act.Duration + 1f, 15f);
                    yield return UntilGame(() => !_pi.IsWorking, limit, limit * 3f + 5f);
                    if (_pi.IsWorking)
                    {
                        yield return Tap(Key.Escape);
                        notes.Add("Esc stopped it after " + limit.ToString("0") + " s");
                    }
                    else notes.Add("done in " + act.Duration.ToString("0.#") + " s");
                }
                yield return null;
                if (_pi.IsWorking)
                {
                    _afterOk = false;
                    notes.Add("the job wouldn't stop");
                    _pi.StopJob();
                }
            }
            if (_pi.IsSearching)
            {
                LootContainer searched = _pi.SearchTarget;
                float limit = (searched != null ? searched.SearchTime : 3f) + 1f;
                yield return UntilGame(() => !_pi.IsSearching, limit, limit * 3f + 5f);
                if (_pi.IsSearching)
                {
                    yield return Tap(Key.E);
                    notes.Add("E stopped the search");
                }
                else notes.Add("searched it");
                yield return null;
                if (_pi.IsSearching)
                {
                    _afterOk = false;
                    notes.Add("the search wouldn't stop");
                    _pi.CancelSearch();
                }
            }
            if (_sleep.IsSleeping)
            {
                if (undoSleep == null) undoSleep = HoldSleep();
                yield return WaitGame(0.6f);   // sleep ignores the keys for its first moments (game time)
                yield return WakeBy(Key.T);
                yield return null;
                if (_sleep.IsSleeping)
                {
                    _afterOk = false;
                    notes.Add("asleep, and T didn't wake (" + _wakeSeen + ")");
                }
                else notes.Add("slept; T woke");
            }
            undoSleep?.Invoke();
            if (_seat.IsSeated || _seat.IsLyingDown)
            {
                string pose = _seat.IsSeated ? "seated" : "lying down";
                yield return Tap(Key.Space);
                yield return Wait(0.2f);
                if (_seat.IsSeated || _seat.IsLyingDown)
                {
                    _afterOk = false;
                    notes.Add(pose + ", and Space didn't get up");
                }
                else notes.Add(pose + "; Space got up");
            }
            if (UIScreen.Current != null)
            {
                notes.Add("opened " + UIScreen.Current.GetType().Name);
                UIScreen.Current.Close();
            }
            if (_invUi.IsOpen)
            {
                notes.Add("opened the inventory");
                _invUi.Close();
            }
            if (_pi.MenuOpen) _pi.CloseMenu();
            if (_pause.IsOpen)
            {
                _afterOk = false;
                notes.Add("the pause menu opened");
                yield return Tap(Key.Escape);
            }
        }
        #endregion

        #region Panels and screens
        private IEnumerator PanelPass()
        {
            yield return Calm();
            yield return KeyPanel(Key.H, "health panel", () => _health.IsOpen, _health.transform);
            yield return Calm();
            yield return KeyPanel(Key.K, "skills panel", () => _skills.IsOpen, _skills.transform);
            yield return Calm();

            int b = s_Errors;
            yield return Tap(Key.I);
            Did("key I opens the inventory", b, _invUi.IsOpen);
            b = s_Errors;
            yield return Tap(Key.I);
            Did("key I closes it", b, !_invUi.IsOpen);
            b = s_Errors;
            yield return Tap(Key.Tab);
            Did("Tab opens the inventory", b, _invUi.IsOpen);

            // Each of its two views (the grid and the list) in turn, switched by its own buttons.
            Func<Transform> inventory = () => _invUi.IsOpen ? _invUi.transform : null;
            yield return InventoryView(true);
            yield return PressEverything("inventory (grid view)", inventory, () => Reopen(Key.I, () => _invUi.IsOpen),
                (c, l) => l == "List view" ? "switches the view; pressed on its own" : l == "Drop All" ? "pressed in the list view" : null,
                l => l == "X" || l.StartsWith("Close", StringComparison.Ordinal), 200);
            if (!_invUi.IsOpen) yield return Reopen(Key.I, () => _invUi.IsOpen);
            yield return InventoryView(false);
            yield return PressEverything("inventory (list view)", inventory, () => Reopen(Key.I, () => _invUi.IsOpen),
                (c, l) => l == "Grid view" ? "switches the view; pressed on its own" : null,
                l => l == "X" || l == "Drop All" || l.StartsWith("Close", StringComparison.Ordinal), 200);
            if (!_invUi.IsOpen) yield return Reopen(Key.I, () => _invUi.IsOpen);
            yield return InventoryView(true);

            b = s_Errors;
            yield return Tap(Key.Tab);
            Did("Tab closes it", b, !_invUi.IsOpen);
            yield return Reopen(Key.I, () => _invUi.IsOpen);
            b = s_Errors;
            yield return Tap(Key.E);
            Did("key E closes the inventory", b, !_invUi.IsOpen);
        }

        private bool InventoryGrid => Field(_invUi, "_gridMode") is bool g && g;

        // The inventory's grid or list view, by its own button.
        private IEnumerator InventoryView(bool grid)
        {
            if (!_invUi.IsOpen || InventoryGrid == grid) yield break;
            string label = grid ? "Grid view" : "List view";
            Component toggle = Controls(_invUi.transform).FirstOrDefault(c => Live(c) && LabelOf(c) == label);
            int b = s_Errors;
            if (toggle != null) Press(toggle.gameObject);
            yield return null;
            yield return null;
            Did("inventory: '" + label + "' switches the view", b, InventoryGrid == grid, toggle == null ? "no such button" : "");
        }

        // A panel on a key: the key opens it, every control on it, the key closes it, Esc closes it (and doesn't pause).
        private IEnumerator KeyPanel(Key key, string panel, Func<bool> isOpen, Transform root)
        {
            int b = s_Errors;
            yield return Tap(key);
            Did("key " + key + " opens the " + panel, b, isOpen());
            if (!isOpen()) yield break;
            yield return PressEverything(panel, () => isOpen() ? root : null, () => Reopen(key, isOpen), null, null, 60);
            if (!isOpen()) yield return Reopen(key, isOpen);
            b = s_Errors;
            yield return Tap(key);
            Did("key " + key + " closes it", b, !isOpen());
            yield return Reopen(key, isOpen);
            b = s_Errors;
            yield return Tap(Key.Escape);
            yield return null;
            Did("Esc closes the " + panel, b, !isOpen());
            Check("that Esc doesn't also pause the game", !_pause.IsOpen);
            if (_pause.IsOpen) yield return Tap(Key.Escape);
        }

        // C, P and J: the field menu, the map and the journal, every control on each, and Esc.
        private IEnumerator ScreenPass()
        {
            foreach (var (key, name) in new[] { (Key.C, "the field menu"), (Key.P, "the map"), (Key.J, "the journal") })
            {
                yield return Calm();
                int b = s_Errors;
                yield return Tap(key);
                yield return null;
                UIScreen screen = UIScreen.Current;
                Did("key " + key + " opens " + name, b, screen != null, screen != null ? screen.GetType().Name : "nothing opened");
                if (screen == null) continue;
                Type type = screen.GetType();
                Func<bool> open = () => UIScreen.Current != null && UIScreen.Current.GetType() == type;
                yield return PressEverything(name + " (" + key + ")", () => open() ? UIScreen.Current.transform : null, () => Reopen(key, open), null,
                    l => l.StartsWith("Close", StringComparison.Ordinal), 150);
                if (!open()) yield return Reopen(key, open);
                b = s_Errors;
                yield return Tap(Key.Escape);
                yield return null;
                Did("Esc closes " + name, b, UIScreen.Current == null);
                Check("that Esc doesn't also pause the game", !_pause.IsOpen);
            }
        }

        private IEnumerator PausePass()
        {
            yield return Calm();
            HudMode hud = GameSettings.Current.Hud;
            int b = s_Errors;
            yield return Tap(Key.Escape);
            yield return null;
            Did("Esc pauses the game", b, _pause.IsOpen && Time.timeScale == 0f && UIState.GameplayBlocked);
            if (!_pause.IsOpen) yield break;

            // The keyboard: Down onto SAVE GAME and Enter saves; S onto VIEW, Right and Left change it; W and Up back up.
            int saves = s_Saves;
            b = s_Errors;
            yield return Tap(Key.DownArrow);
            yield return Tap(Key.Enter);
            yield return null;
            Did("pause menu: Down and Enter save the game", b, s_Saves > saves && _pause.IsOpen, "on row " + PauseSelection);
            b = s_Errors;
            yield return Tap(Key.S);
            Did("pause menu: S moves down a row", b, PauseSelection == 2, "row " + PauseSelection);
            CameraView view = _cam.View;
            b = s_Errors;
            yield return Tap(Key.RightArrow);
            Did("pause menu: Right changes the view", b, _cam.View != view, view + " -> " + _cam.View);
            yield return Tap(Key.LeftArrow);
            Check("pause menu: Left changes it back", _cam.View == view, _cam.View.ToString());
            b = s_Errors;
            yield return Tap(Key.W);
            yield return Tap(Key.UpArrow);
            Did("pause menu: W and Up move back up", b, PauseSelection == 0, "row " + PauseSelection);

            // Every row and arrow by the pointer; the screens two of them open are gone through below.
            yield return PressEverything("pause menu", () => _pause.IsOpen && UIScreen.Current == null ? _pause.transform : null,
                () => Reopen(Key.Escape, () => _pause.IsOpen),
                (c, l) => l == "SAVE & QUIT TO MENU" ? "leaves the run; pressed at the end"
                    : l == "SETTINGS" || l == "FIELD MANUAL" ? "its screen is gone through next" : null,
                l => l == "RESUME", 40);
            foreach (string row in new[] { "SETTINGS", "FIELD MANUAL" })
            {
                if (!_pause.IsOpen) yield return Reopen(Key.Escape, () => _pause.IsOpen);
                b = s_Errors;
                Press(PauseRow(row));
                yield return null;
                UIScreen screen = UIScreen.Current;
                Did("pause menu: '" + row + "' opens its screen", b, screen != null, screen != null ? screen.GetType().Name : "");
                if (screen == null) continue;
                Type type = screen.GetType();
                Func<bool> open = () => UIScreen.Current != null && UIScreen.Current.GetType() == type;
                yield return PressEverything(type.Name, () => open() ? UIScreen.Current.transform : null, () => ScreenFromPause(row, open), null,
                    l => l.StartsWith("Close", StringComparison.Ordinal), 120);
                if (!open()) yield return ScreenFromPause(row, open);
                b = s_Errors;
                yield return Tap(Key.Escape);
                yield return null;
                Did(type.Name + ": Esc closes it, back to the pause menu", b, UIScreen.Current == null && _pause.IsOpen);
            }

            if (!_pause.IsOpen) yield return Reopen(Key.Escape, () => _pause.IsOpen);
            b = s_Errors;
            yield return Tap(Key.Escape);
            yield return null;
            Did("Esc resumes", b, !_pause.IsOpen && Time.timeScale == 1f && !UIState.GameplayBlocked);
            GameSettings.Current.Hud = hud;
        }

        private IEnumerator ScreenFromPause(string row, Func<bool> open)
        {
            if (!_pause.IsOpen) yield return Reopen(Key.Escape, () => _pause.IsOpen);
            Press(PauseRow(row));
            yield return Until(open, 2f);
        }
        #endregion

        #region Waves, saving, dying
        private IEnumerator WavesPass()
        {
            yield return Calm();
            for (int i = 0; i < 2; i++)
            {
                // A wave under way (out of a break if need be), all of it out (a head every quarter second), then cut down.
                yield return Until(() => Zombies() > 0, CloneZConfig.WaveBreakSeconds + 10f);
                int wave = _gm.Wave;
                int expected = CloneZConfig.WaveBaseZombies + (wave - 1) * CloneZConfig.WavePerWaveIncrease;
                int last = -1;
                float quiet = Time.realtimeSinceStartup;
                yield return Until(() =>
                {
                    int n = Zombies();
                    if (n != last)
                    {
                        last = n;
                        quiet = Time.realtimeSinceStartup;
                    }
                    return n >= expected || Time.realtimeSinceStartup - quiet > 3f;
                }, expected * 0.25f + 15f);
                int kills = _gm.ZombieKills;
                int killed = KillTheDead(Vector3.zero, 0f);
                yield return Until(() => Zombies() == 0, 5f);
                Check("wave " + wave + ": all " + killed + " of the dead cut down", killed > 0 && Zombies() == 0 && _gm.ZombieKills == kills + killed,
                    "kills " + kills + " -> " + _gm.ZombieKills);
                yield return Until(() => _gm.Wave > wave, CloneZConfig.WaveBreakSeconds + 15f);
                Check("wave " + (wave + 1) + " begins after the break", _gm.Wave == wave + 1, "wave " + _gm.Wave);
            }
        }

        private IEnumerator SaveQuitContinue()
        {
            yield return Calm();
            int saves = s_Saves;
            int b = s_Errors;
            yield return Tap(Key.F5);
            yield return null;
            Did("key F5 quick-saves the run", b, s_Saves > saves && SaveSystem.HasSave, SaveSystem.FilePath);

            int wave = _gm.Wave, kills = _gm.ZombieKills;
            string survivor = GameSession.SurvivorName;
            yield return Tap(Key.Escape);
            yield return null;
            GameObject quit = _pause.IsOpen ? PauseRow("SAVE & QUIT TO MENU") : null;
            Check("pause menu: SAVE & QUIT TO MENU", quit != null);
            if (quit == null) yield break;
            saves = s_Saves;
            b = s_Errors;
            Press(quit);
            MainMenuController menu = null;
            yield return Until(() => GameManager.Instance == null && (menu = FindAnyObjectByType<MainMenuController>()) != null, 60f);
            Did("pause menu: SAVE & QUIT TO MENU saves and goes back to the title", b, s_Saves > saves && menu != null, "wave " + wave + ", " + kills + " kills");
            if (menu == null) yield break;
            yield return null;

            Button cont = TitleButton(menu, "Continue");
            Check("title: Continue offered with a save", cont != null && cont.IsInteractable());
            if (cont == null || !cont.IsInteractable()) yield break;
            b = s_Errors;
            Press(cont.gameObject);
            yield return Until(() => WorldUp, 120f);
            if (!WorldUp)
            {
                Did("title: Continue rebuilds the run", b, false);
                yield break;
            }
            BindRun();
            _me.Untargetable = true;
            Did("title: Continue picks the run up where it was saved", b,
                (_gm.Wave == wave || _gm.Wave == wave + 1) && _gm.ZombieKills == kills && GameSession.SurvivorName == survivor,
                "wave " + _gm.Wave + " (saved " + wave + "), " + _gm.ZombieKills + " kills (saved " + kills + "), " + GameSession.SurvivorName);
        }

        private IEnumerator Death()
        {
            yield return Calm();
            int b = s_Errors;
            _stats.Kill("the smoke test");
            yield return null;
            yield return null;
            yield return GameOver(b);
        }

        private IEnumerator GameOver(int before)
        {
            Did("death ends the run", before, _gm.State == GameState.GameOver, _gm.CauseOfDeath);
            Check("the game-over screen shows", GameOverShown());
            Check("death deletes the run's save", !SaveSystem.HasSave);
            yield return Wait(0.5f);
            int b = s_Errors;
            yield return Tap(Key.R);
            MainMenuController menu = null;
            yield return Until(() => GameManager.Instance == null && (menu = FindAnyObjectByType<MainMenuController>()) != null, 60f);
            Did("key R after a death goes back to the title", b, menu != null);
            if (menu == null) yield break;
            yield return null;
            Button cont = TitleButton(menu, "Continue");
            Check("title: no Continue after a death", cont != null && !cont.IsInteractable());
        }

        private bool GameOverShown()
        {
            if (_hud != null)
                foreach (Transform t in _hud.GetComponentsInChildren<Transform>(true))
                    if (t.name == "GameOver") return t.gameObject.activeInHierarchy;
            return GameObject.Find("GameOver") != null;
        }
        #endregion
    }
}
