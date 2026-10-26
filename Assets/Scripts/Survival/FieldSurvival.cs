using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// A timed action the survivor performs in the field (foraging, fishing, reading, cranking a charger): a progress
    /// bar on the HUD, a matching activity on the body, and an interruption if they walk off or get hurt.
    /// </summary>
    public sealed class PlayerChannel : MonoBehaviour
    {
        /// <summary>Singleton instance of the local player's channel for field actions.</summary>
        public static PlayerChannel Local { get; private set; }
        /// <summary>True if a field action is currently active.</summary>
        public bool Busy => _onDone != null;

        private Action _onDone;
        private string _label;
        private float _t, _len, _health;
        private HumanoidActivity _activity;
        private PlayerStats _stats;
        private PlayerAvatar _avatar;
        private PlayerController _controller;
        private Vector3 _start;
        private Image _bar;
        private Text _text;
        private string _labelText;
        private HumanoidAnimator _animator;
        private RectTransform _slot;
        private const float _maxDistSq = 0.8f * 0.8f;

        private void Awake()
        {
            Local = this;
            _stats = GetComponent<PlayerStats>();
            _avatar = GetComponent<PlayerAvatar>();
            _controller = GetComponent<PlayerController>();
            // Cache the animator reference to avoid per‑frame lookups
            _animator = _avatar != null ? _avatar.Animator : null;
        }

        private void OnDestroy() { if (Local == this) Local = null; if (_slot != null) Destroy(_slot.gameObject); }

        /// <summary>Starts a timed field action with a label and duration, unless the player is busy or dead.</summary>
        public bool Begin(string label, float seconds, Action onDone, HumanoidActivity activity = HumanoidActivity.Use)
        {
            if (Busy || _stats == null || _stats.IsDead || _stats.Incapacitated) return false;
            _label = label;
            _labelText = _label + "..."; 
            _len = Mathf.Max(0.1f, seconds);
            _t = 0f;
            _onDone = onDone;
            _activity = activity;
            _start = transform.position;
            _health = _stats.Health;
            EnsureHud();
            _text.text = _labelText;
            _slot.gameObject.SetActive(true);
            return true;
        }

        /// <summary>Cancels the current field action, optionally showing a toast message.</summary>
        public void Cancel(string why = null)
        {
            if (!Busy) return;
            _onDone = null;
            if (_slot != null) _slot.gameObject.SetActive(false);
            if (_animator != null) _animator.Activity = HumanoidActivity.None;
            if (why != null) GameEvents.RaiseToast(why);
        }

        private void Update()
        {
            if (!Busy) return;
            if (_stats.IsDead || _stats.Incapacitated) { Cancel(); return; }
            if (_stats.Health < _health - 0.5f) { Cancel("Interrupted."); return; }
            Vector3 diff = transform.position - _start;
                diff.y = 0f;
                if (diff.sqrMagnitude > _maxDistSq) { Cancel("You stopped " + _label.ToLowerInvariant() + "."); return; }
            if (_animator != null) _animator.Activity = _activity;
            _t += Time.deltaTime;
            UIKit.SetBar(_bar, _t / _len);

            if (_t < _len) return;
            Action done = _onDone;
            _onDone = null;
            _slot.gameObject.SetActive(false);
            if (_animator != null) _animator.Activity = HumanoidActivity.None;
            try { done(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>Ensures the HUD elements for the channel exist.</summary>
        private void EnsureHud()
        {
            if (_slot != null) return;
            _slot = HudLayer.Anchor("Channel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(340f, 46f));
            Image bg = UIKit.Panel(_slot, "Bg", new Color(0f, 0f, 0f, 0.6f));
            UIKit.Stretch(bg.rectTransform);
            _text = UIKit.Label(_slot, "", 16, UIKit.TextColor, TextAnchor.UpperCenter);
            UIKit.Place(_text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(330f, 24f));
            _bar = UIKit.Bar(_slot, UIKit.Accent, 0f, 8f);
            UIKit.Place((RectTransform)_bar.transform.parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(310f, 8f));
        }
    }

    /// <summary>
    /// Living off the land, Zomboid-style: forage the woods and verges (berries, mushrooms, herbs, worms for bait),
    /// fish at the river with a rod and bait, set snares and come back for what they caught, light a fire to cook and
    /// keep warm, and take apart what you can't use. Skills make every one of these better.
    /// </summary>
    public static class FieldSurvival
    {
        /// <summary>Returns true if a point is near the river and a raid is in extraction mode.</summary>
        public static bool NearRiver(Vector3 p) => p.x < -64f && GameSession.IsExtraction;

        /// <summary>Initiates a foraging action, adding items based on environment and skills.</summary>
        public static void Forage(PlayerInventory inv)
        {
            if (inv == null || PlayerChannel.Local == null) return;
            Vector3 p = inv.transform.position;
            if (Shelter.Count > 0 && Shelter.IsSheltered(p + Vector3.up)) { GameEvents.RaiseToast("Nothing grows indoors. Go outside."); return; }
            var skills = inv.GetComponent<PlayerSkills>();
            PlayerChannel.Local.Begin("Foraging", 14f / (skills != null ? Mathf.Sqrt(skills.ForagingMult) : 1f), () =>
            {
                float trees = ZoneForest.Density(p);
                bool winter = Climate.SnowFraction > 0.5f;
                float luck = (skills != null ? skills.ForagingMult : 1f) * PlayerModifiers.Current.LootLuck * (winter ? 0.6f : 1f) * (0.6f + trees);
                var found = new List<string>();
                Roll(inv, "berries", 0.35f * luck, 1, 3, found, !winter);
                Roll(inv, "mushrooms", 0.3f * luck * (0.4f + trees), 1, 2, found, true);
                Roll(inv, "herbs", 0.22f * luck, 1, 2, found, true);
                Roll(inv, "bait", 0.4f * luck, 1, 4, found, !winter);
                Roll(inv, "plank", 0.15f * luck * (0.5f + trees), 1, 1, found, true);
                if (skills != null) skills.AddXp(PlayerSkills.Skill.Foraging, 6f + found.Count * 4f);
                GameEvents.RaiseToast(found.Count == 0 ? "You find nothing worth taking." : "Foraged: " + string.Join(", ", found));
                NoiseSystem.Emit(p, 4f);
            }, HumanoidActivity.Search);
        }

        /// <summary>Attempts to add an item to the inventory based on chance and context.</summary>
        private static void Roll(PlayerInventory inv, string id, float chance, int min, int max, List<string> found, bool allowed)
        {
            if (!allowed || UnityEngine.Random.value > chance) return;
            ItemDef d = ItemDatabase.Get(id);
            if (d == null) return;
            int n = UnityEngine.Random.Range(min, max + 1);
            if (inv.Inventory.Add(d, n, false, extra: new ItemExtra { FoundInRaid = true }) > 0) found.Add(n + "x " + d.DisplayName);
        }

        /// <summary>Starts a fishing action if near water and the player has a rod and bait.</summary>
        public static void Fish(PlayerInventory inv)
        {
            if (inv == null || PlayerChannel.Local == null) return;
            if (!NearRiver(inv.transform.position)) { GameEvents.RaiseToast("You need open water — the river is on the west edge."); return; }
            if (!inv.Has("fishingrod")) { GameEvents.RaiseToast("You need a fishing rod."); return; }
            if (!inv.Has("bait")) { GameEvents.RaiseToast("You need bait (forage for worms)."); return; }
            var skills = inv.GetComponent<PlayerSkills>();
            PlayerChannel.Local.Begin("Fishing", 22f, () =>
            {
                inv.Consume("bait", 1);
                float chance = 0.45f * (skills != null ? skills.FishingMult : 1f);
                Sfx.Play(SfxIds.Splash, inv.transform.position + inv.transform.forward * 3f, 0.6f, 1f, 20f);
                if (UnityEngine.Random.value < chance)
                {
                    ItemDef fish = ItemDatabase.Get("rawfish");
                    if (fish != null) inv.Inventory.Add(fish, 1, false, extra: new ItemExtra { FoundInRaid = true });
                    GameEvents.RaiseToast("A fish! Cook it before you eat it.");
                    if (skills != null) skills.AddXp(PlayerSkills.Skill.Fishing, 14f);
                }
                else
                {
                    GameEvents.RaiseToast("Nothing bites. The bait's gone.");
                    if (skills != null) skills.AddXp(PlayerSkills.Skill.Fishing, 4f);
                }
            }, HumanoidActivity.Use);
        }

        /// <summary>Places a snare trap after checking inventory and location.</summary>
        public static void SetSnare(PlayerInventory inv)
        {
            if (inv == null || !inv.Has("snare")) { GameEvents.RaiseToast("You need a snare."); return; }
            Vector3 p = inv.transform.position;
            if (Shelter.Count > 0 && Shelter.IsSheltered(p + Vector3.up)) { GameEvents.RaiseToast("Set snares outside, where animals run."); return; }
            PlayerChannel.Local.Begin("Setting a snare", 6f, () =>
            {
                inv.Consume("snare", 1);
                SnareTrap.Place(p + inv.transform.forward * 0.8f, inv.GetComponent<PlayerSkills>());
                GameEvents.RaiseToast("Snare set. Come back in a while.");
            }, HumanoidActivity.Dig);
        }

        /// <summary>Attempts to light a fire if the player has fuel and a lighter or matches.</summary>
        public static bool LightFire(PlayerInventory inv)
        {
            if (inv == null) return false;
            if (PlayerChannel.Local == null) return false;
            if (!inv.Has("lighter") && !inv.Has("matches")) { GameEvents.RaiseToast("You need a lighter or matches."); return false; }
            string fuel = inv.Has("plank") ? "plank" : inv.Has("rag") ? "rag" : inv.Has("paper") ? "paper" : null;
            if (fuel == null) { GameEvents.RaiseToast("You need something to burn: a plank, rags or paper."); return false; }
            Vector3 p = inv.transform.position + inv.transform.forward * 1.2f;
            bool started = PlayerChannel.Local.Begin("Building a fire", 5f, () =>
            {
                inv.Consume(fuel, 1);
                if (!inv.Has("lighter")) inv.Consume("matches", 1);
                Sfx.Play(SfxIds.Lighter, p, 0.6f, 1f, 11f);
                Campfire.Light(p, fuel == "plank" ? 240f : 90f);
            }, HumanoidActivity.Build);
            return started;
        }

        /// <summary>Starts a cranking action to charge a battery if the player has a crank.</summary>
        public static void Crank(PlayerInventory inv)
        {
            var gear = inv != null ? inv.GetComponent<PlayerGear>() : null;
            if (gear == null) return;
            PlayerChannel.Local.Begin("Cranking", 8f, () =>
            {
                Sfx.Play(SfxIds.Crank, inv.transform.position + Vector3.up, 0.7f, 1f, 22f);
                NoiseSystem.Emit(inv.transform.position, 12f);
                GameEvents.RaiseToast(gear.ChargeFlattest(0.3f, out string name) ? "Some life back in the " + name + "." : "Nothing needs charging.");
            }, HumanoidActivity.Use);
        }

        /// <summary>Starts a cranking action to charge a battery if the player has a crank.</summary>
        public static bool TryCrank(PlayerInventory inv)
        {
            // Crank itself schedules the action; we only need to report whether it could be scheduled.
if (PlayerChannel.Local == null || PlayerChannel.Local.Busy) return false;
            Crank(inv);
            return PlayerChannel.Local.Busy;
        }

        #region Disassembly
/// <summary>Returns the parts an item can be salvaged into, or null if it cannot be taken apart.</summary>
        public static (string id, int n)[] SalvageOf(ItemDef d)
        {
            if (d == null) return null;
            if (d.IsFirearm) return new[] { ("scrap", 3), ("wires", 1) };
            if (d.IsWeapon) return new[] { ("scrap", 2) };
            if (d.IsClothing) return new[] { ("rag", Mathf.Clamp(Mathf.RoundToInt(d.Weight * 3f), 1, 4)), ("thread", 1) };
            switch (d.Gear)
            {
                case GearKind.Flashlight: case GearKind.NightVision: case GearKind.Battery: return new[] { ("wires", 1), ("scrap", 1) };
            }
            string id = d.Id;
            if (id.StartsWith("detector_") || id == "gps" || id == "radio_walkie" || id == "tablet" || id.StartsWith("drone_")) return new[] { ("circuitboard", 1), ("wires", 2), ("scrap", 1) };
            if (id == "tincan" || id == "carwheel" || id == "radiator" || id == "sparkplug") return new[] { ("scrap", id == "tincan" ? 1 : 3) };
            if (id == "carbattery") return new[] { ("scrap", 2), ("chemicals", 2) };
            if (id.Contains("pills") || id == "disinfectant" || id == "antirad" || id == "charcoal") return new[] { ("chemicals", 1) };
            if (d.Category == ItemCategory.Medical && id.Contains("bandage")) return new[] { ("rag", 1) };
            return null;
        }

        /// <summary>Disassembles an item stack into its component parts, applying skill bonuses.</summary>
        public static bool Disassemble(PlayerInventory inv, ItemStack s)
        {
            (string id, int n)[] parts = SalvageOf(s?.Def);
            if (parts == null || inv == null) return false;
            var skills = inv.GetComponent<PlayerSkills>();
            float mech = skills != null ? skills.MechanicsMult : 1f;
            inv.Inventory.Remove(s, 1);
            var got = new List<string>();
            foreach ((string id, int n) in parts)
            {
                ItemDef d = ItemDatabase.Get(id);
                if (d == null) continue;
                int count = Mathf.Max(1, Mathf.RoundToInt(n * Mathf.Lerp(0.6f, 1.2f, Mathf.InverseLerp(1f, 1.6f, mech)) * UnityEngine.Random.Range(0.8f, 1.2f)));
                inv.Inventory.Add(d, count, false);
                got.Add(count + "x " + d.DisplayName);
            }
            if (skills != null) skills.AddXp(PlayerSkills.Skill.Mechanics, 5f);
            Sfx.PlayUI(SfxIds.GearRustle, 0.6f, 0.8f);
            GameEvents.RaiseToast("Took apart: " + string.Join(", ", got));
            return true;
        }
        #endregion
    }

    /// <summary>A set snare: after a while (more with Trapping) it may hold a rabbit. Check it by walking up to it.</summary>
    public sealed class SnareTrap : Interactable
    {
        private float _readyAt;
        private bool _caught;

        public override string DisplayName => _caught && Time.time >= _readyAt ? "Snare (something's in it!)" : "Snare";
        public override float Reach => 1.8f;

        /// <summary>Creates a snare trap at the given position using the player's trapping skill.</summary>
        public static void Place(Vector3 at, PlayerSkills skills)
        {
            var go = new GameObject("Snare");
            go.transform.position = at;
            var m = new RaidMesh();
            m.SetFrame(at, 0f);
            m.Rod(new Vector3(-0.2f, 0f, 0f), new Vector3(-0.2f, 0.35f, 0f), 0.02f, ZoneKit.M(ZoneKit.WetWood), 5);
            m.Rod(new Vector3(0.2f, 0f, 0f), new Vector3(0.2f, 0.35f, 0f), 0.02f, ZoneKit.M(ZoneKit.WetWood), 5);
            m.Rod(new Vector3(-0.2f, 0.3f, 0f), new Vector3(0.2f, 0.3f, 0f), 0.006f, ZoneKit.M(new Color(0.7f, 0.66f, 0.55f)), 4);
            ZoneKit.Emit(m, "Mesh", go.transform, at, UnityEngine.Rendering.ShadowCastingMode.Off);
            var s = go.AddComponent<SnareTrap>();
            float mult = skills != null ? skills.TrappingMult : 1f;
            s._readyAt = Time.time + UnityEngine.Random.Range(120f, 300f) / mult;
            s._caught = UnityEngine.Random.value < Mathf.Clamp01(0.55f * mult * (0.6f + ZoneForest.Density(at)));
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool ready = Time.time >= _readyAt;
            into.Add(new InteractAction(ready ? (_caught ? "Take the rabbit" : "Collect the empty snare") : "Leave it (not yet)", c =>
            {
                if (!ready) return;
                if (_caught)
                {
                    ItemDef r = ItemDatabase.Get("rawrabbit");
                    if (r != null) c.Inventory.Inventory.Add(r, 1, false, extra: new ItemExtra { FoundInRaid = true });
                    var sk = c.Skills;
                    if (sk != null) sk.AddXp(PlayerSkills.Skill.Trapping, 15f);
                }
                ItemDef snare = ItemDatabase.Get("snare");
                if (snare != null) c.Inventory.Inventory.Add(snare, 1, false);
                Destroy(gameObject);
            }).DisabledIf(!ready, "Give it time"));
            into.Add(new InteractAction("Inspect", c => {
                float secs = Mathf.Max(0f, _readyAt - Time.time);
                GameEvents.RaiseToast($"Snare will be ready in {Mathf.CeilToInt(secs)} s.");
            }));
        }
    }

    /// <summary>A small fire: cook on it, warm yourself by it. It burns down and goes out (and everything sees it at night).</summary>
    public sealed class Campfire : Interactable, IWarmthSource
    {
        private float _until;
        private Light _light;

        /// <summary>Provides the world position of the campfire's warmth source.</summary>
        public override string DisplayName => "Campfire";
        public Vector3 WarmthPosition => transform.position;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool hasPlank = ctx.Inventory.Has("plank");
            bool hasRag = ctx.Inventory.Has("rag");
            bool hasPaper = ctx.Inventory.Has("paper");
            bool anyFuel = hasPlank || hasRag || hasPaper;

            into.Add(new InteractAction("Add fuel", c =>
            {
                string fuel = null;
                float add = 0f;
                if (c.Inventory.Has("plank")) { fuel = "plank"; add = 240f; }
                else if (c.Inventory.Has("rag")) { fuel = "rag"; add = 90f; }
                else if (c.Inventory.Has("paper")) { fuel = "paper"; add = 90f; }

                if (fuel != null)
                {
                    c.Inventory.Consume(fuel, 1);
                    _until = Mathf.Max(_until, Time.time) + add;
                    Sfx.Play(SfxIds.Lighter, transform.position, 0.6f, 1f, 11f);
                    GameEvents.RaiseToast($"Added {fuel}, the fire will burn longer.");
                }
            }).DisabledIf(!anyFuel, "No fuel"));
            // Inspect action to show remaining burn time
into.Add(new InteractAction("Inspect", c =>
                    {
                        float secs = Mathf.Max(0f, _until - Time.time);
                        GameEvents.RaiseToast($"Campfire will burn for {Mathf.CeilToInt(secs)} s.");
                    }));
                // Extinguish action to douse the fire early
                into.Add(new InteractAction("Extinguish", c =>
                    {
                        Extinguish();
                    }).DisabledIf(Time.time >= _until, "The fire is already out"));
        }
        /// <summary>Radius in metres within which the campfire provides warmth.</summary>
        public float WarmthRadius => 4f;
        /// <summary>Current warmth output, zero when the fire has burned out.</summary>
        public float Warmth => Time.time < _until ? CloneZConfig.ComfortWarmthMax : 0f;

        /// <summary>Immediately puts out the fire, unregistering its warmth and destroying the object.</summary>
        private void Extinguish()
        {
            HomeComfort.Unregister(this);
            // Water thrown over the embers.
            Sfx.Play(SfxIds.Splash, transform.position, 0.4f, 1.25f, 10f);
            GameEvents.RaiseToast("You douse the fire.");
            Destroy(gameObject);
        }

        /// <summary>Creates a campfire at the given location that emits warmth for the specified duration.</summary>
        public static Campfire Light(Vector3 at, float seconds)
        {
            var go = new GameObject("Campfire");
            go.transform.position = at;
            var m = new RaidMesh();
            m.SetFrame(at, 0f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                m.Rod(Quaternion.Euler(0f, a, 0f) * new Vector3(0.35f, 0.02f, 0f), Quaternion.Euler(0f, a + 15f, 0f) * new Vector3(0.05f, 0.25f, 0f), 0.04f, ZoneKit.M(ZoneKit.Bark), 5);
            }
            for (int i = 0; i < 8; i++) m.Ellipsoid(Quaternion.Euler(0f, i * 45f, 0f) * new Vector3(0.45f, 0.05f, 0f), new Vector3(0.18f, 0.12f, 0.16f), ZoneKit.M(new Color(0.35f, 0.34f, 0.32f)), 6, 3);
            ZoneKit.Emit(m, "Fire", go.transform, at);
            ZoneKit.Fire(go.transform, at + Vector3.up * 0.1f, 0.8f);
            go.AddComponent<CookingStation>().DisplayName = "Campfire";
            var c = go.AddComponent<Campfire>();
            c._until = Time.time + seconds;
            HomeComfort.Register(c);
            NoiseSystem.Emit(at, 6f);
            return c;
        }

        private void Update()
        {
if (Time.time > _until)
                {
                    // If the fire was already extinguished via Extinguish(), _until may still be in the future; guard against double‑unregister.
                    if (HomeComfort.IsRegistered(this)) HomeComfort.Unregister(this);
                    Destroy(gameObject);
                }
        }

        private void OnDestroy() => HomeComfort.Unregister(this);
    }

    /// <summary>The field menu (C): field crafts from the pack, taking things apart, foraging, fishing, snares, a fire.</summary>
    public sealed class FieldCraftScreen : UIScreen
    {
        protected override string Title => "Field — craft, salvage, live off the land";
        protected override Vector2 WindowSize => new Vector2(1300f, 820f);

        private PlayerInventory Inv => Actor.LocalPlayer != null ? Actor.LocalPlayer.GetComponent<PlayerInventory>() : null;

        protected override void Build()
        {
            PlayerInventory inv = Inv;
            if (inv == null) return;
            RectTransform[] cols = HubUI.Columns(Body, 16f, 1f, 1f, 1f);

            // Crafts (Metro: two resources, anywhere).
            HubUI.Card(cols[0]);
            RectTransform craft = HubUI.Scroll(cols[0], 40f, 0f, 4f);
            HubUI.AreaHeading(cols[0], "Field crafts", 20);
            foreach (Recipe r in Hideout.Recipes)
            {
                if (!r.Field) continue;
                var sb = new System.Text.StringBuilder();
                foreach ((string id, int n) in r.Inputs) sb.Append(n).Append("x ").Append(Trading.NameOf(id)).Append("  ");
                RectTransform row = HubUI.Row(craft, 52f, new Color(1f, 1f, 1f, 0.04f));
                HubUI.Cell(row, Hideout.Describe(r) + "\n<size=13><color=#9aa0a8>" + sb + "</color></size>", 260f, 15);
                Recipe pick = r;
                HubUI.Btn(row, "Make", () =>
                {
                    bool ok = Hideout.FieldCraft(pick, inv, out string msg);
                    SetStatus(msg, !ok);
                    Sfx.PlayUI(ok ? SfxIds.UiBuy : SfxIds.UiError);
                    Rebuild();
                }, 80f);
            }

            // Salvage.
            HubUI.Card(cols[1]);
            RectTransform salvage = HubUI.Scroll(cols[1], 40f, 0f, 4f);
            HubUI.AreaHeading(cols[1], "Take apart", 20);
            var seen = new HashSet<ItemDef>();
            foreach (ItemStack s in new List<ItemStack>(inv.Inventory.Stacks))
            {
                (string id, int n)[] parts = FieldSurvival.SalvageOf(s.Def);
                if (parts == null || !seen.Add(s.Def) || s.Def.Id == "scrap" || (s.Extra != null && s.Extra.OnBelt)) continue;
                var sb = new System.Text.StringBuilder();
                foreach ((string id, int n) in parts) sb.Append(Trading.NameOf(id)).Append(' ');
                RectTransform row = HubUI.Row(salvage, 46f, new Color(1f, 1f, 1f, 0.04f));
                HubUI.Cell(row, s.Def.DisplayName + "\n<size=13><color=#9aa0a8>→ " + sb + "</color></size>", 260f, 15);
                ItemStack pick = s;
                HubUI.Btn(row, "Salvage", () => { FieldSurvival.Disassemble(inv, pick); Rebuild(); }, 90f);
            }

            // Living off the land.
            HubUI.Card(cols[2]);
            RectTransform land = HubUI.Stack(cols[2], 8f, 14);
            HubUI.Heading(land, "The land", 20);
            Action(land, "Forage here", "Berries, mushrooms, herbs and worms. Better in the woods; poor in winter.", () => FieldSurvival.Forage(inv));
            Action(land, "Fish", "Rod and bait, at the river on the west edge.", () => FieldSurvival.Fish(inv), inv.Has("fishingrod"));
            Action(land, "Set a snare", "Come back later for a rabbit.", () => FieldSurvival.SetSnare(inv), inv.Has("snare"));
            Action(land, "Light a fire", "Cook and get warm. Seen from far off at night.", () => FieldSurvival.LightFire(inv), inv.Has("lighter") || inv.Has("matches"));
            Action(land, "Crank a charger", "Bring a flat battery back. Noisy.", () => FieldSurvival.TryCrank(inv), inv.Has("handcrank"));
        }

        private void Action(Transform list, string label, string help, System.Action run, bool enabled = true)
        {
            RectTransform row = HubUI.Row(list, 56f, new Color(1f, 1f, 1f, 0.04f));
            HubUI.Cell(row, label + "\n<size=13><color=#9aa0a8>" + help + "</color></size>", 250f, 16);
            HubUI.Btn(row, "Do it", () => { Close(); run(); }, 80f, 32f, null, enabled);
        }
    }

    /// <summary>Opens the field menu (C) and registers the gadget and book verbs.</summary>
    public sealed class FieldSurvivalModule : IGameModule
    {
        /// <summary>Module load order; ensures field features initialise after core systems.</summary>
        public int Order => 60;

        /// <summary>Registers field actions, UI and hotkeys with the game on module initialisation.</summary>
        public void Init()
        {
            ItemActions.RegisterUse("handcrank", (inv, s) => { return FieldSurvival.TryCrank(inv); });
            ItemActions.RegisterUse("lighter", (inv, s) => { return FieldSurvival.LightFire(inv); });
            ItemActions.RegisterUse("fishingrod", (inv, s) => { FieldSurvival.Fish(inv); return true; });
            ItemActions.RegisterUse("snare", (inv, s) => { FieldSurvival.SetSnare(inv); return true; });
            ItemActions.RegisterUse("purifytabs", PurifyWater);
            ItemActions.RegisterMenu((inv, stack, into) =>
            {
                if (FieldSurvival.SalvageOf(stack.Def) != null && !(stack.Extra != null && stack.Extra.OnBelt))
                    into.Add(new ItemMenuAction("Take apart", () => FieldSurvival.Disassemble(inv, stack)));
            });
            SkillBooks.Register();
            WorldHooks.PlayerBuilt += p =>
            {
                if (p == null) return;
                if (p.GetComponent<PlayerChannel>() == null) p.gameObject.AddComponent<PlayerChannel>();
                if (p.GetComponent<FieldHotkeys>() == null) p.gameObject.AddComponent<FieldHotkeys>();
                if (UnityEngine.Object.FindAnyObjectByType<HudCompass>() == null) HudCompass.Create(p.GetComponent<PlayerInventory>());
            };
        }

        private static bool PurifyWater(PlayerInventory inv, ItemStack tabs)
        {
            foreach (ItemStack s in inv.Inventory.Stacks)
            {
                ItemDef d = s.Def;
                if (d == null || !d.IsWaterContainer || !d.HoldsWater || !d.WaterTainted || string.IsNullOrEmpty(d.PurifiedId)) continue;
                ItemDef clean = ItemDatabase.Get(d.PurifiedId);
                if (clean == null) continue;
                inv.Inventory.Remove(s, 1);
                inv.Inventory.Add(clean, 1, false);
                inv.Inventory.Remove(tabs, 1);
                GameEvents.RaiseToast("A tablet fizzes. The water's safe to drink now.");
                return true;
            }
            GameEvents.RaiseToast("You have no tainted water to treat.");
            return true;
        }
    }

    /// <summary>The field key (C) and the map key (P).</summary>
    public sealed class FieldHotkeys : MonoBehaviour
    {
        private void Update()
        {
            if (GameInput.Pressed(GameAction.FieldCraft) && UIScreen.Current == null) UIScreen.Show<FieldCraftScreen>();
            if (GameInput.Pressed(GameAction.Map) && UIScreen.Current == null) UIScreen.Show<ZoneMapScreen>();
            if (GameInput.Pressed(GameAction.Journal) && UIScreen.Current == null) UIScreen.Show<JournalScreen>();
        }
    }

    /// <summary>
    /// Skill books (Zomboid): reading one multiplies the experience you earn in its skill. Reading takes a while and
    /// can be done in sittings; the multiplier lasts until the book is finished, then a little longer.
    /// </summary>
    public static class SkillBooks
    {
        private static readonly Dictionary<string, PlayerSkills.Skill> s_Skills = new Dictionary<string, PlayerSkills.Skill>
        {
            ["book_aiming"] = PlayerSkills.Skill.Aiming, ["book_firstaid"] = PlayerSkills.Skill.FirstAid,
            ["book_mechanics"] = PlayerSkills.Skill.Mechanics, ["book_carpentry"] = PlayerSkills.Skill.Carpentry,
            ["book_foraging"] = PlayerSkills.Skill.Foraging, ["book_fishing"] = PlayerSkills.Skill.Fishing,
            ["book_cooking"] = PlayerSkills.Skill.Cooking, ["book_metabolism"] = PlayerSkills.Skill.Metabolism,
        };

        internal static void Register()
        {
            ItemActions.RegisterUse(d => d != null && s_Skills.ContainsKey(d.Id), Read);
            PlayerSkills.XpMultiplier = (skill, level) =>
            {
                CharacterProfile c = ProfileStore.ActiveCharacter;
                float m = PlayerModifiers.Current.XpMult;
                if (c == null) return m;
                foreach (SkillBookState b in c.Books)
                    if (b.Read > 0f && s_Skills.TryGetValue(b.BookId, out PlayerSkills.Skill s) && s == skill) m *= b.Finished ? 1.5f : 3f;
                return m;
            };
        }

        private static bool Read(PlayerInventory inv, ItemStack book)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || PlayerChannel.Local == null) return false;
            SkillBookState st = c.Books.Find(b => b.BookId == book.Def.Id);
            if (st == null) { st = new SkillBookState { BookId = book.Def.Id }; c.Books.Add(st); }
            if (st.Finished) { GameEvents.RaiseToast("You've read this one cover to cover."); return true; }
            PlayerChannel.Local.Begin("Reading", 20f, () =>
            {
                st.Read = Mathf.Min(1f, st.Read + 0.25f);
                Sfx.PlayUI(SfxIds.PageTurn, 0.6f);
                if (st.Read >= 1f)
                {
                    st.Finished = true;
                    GameEvents.RaiseToast("Finished " + book.Def.DisplayName + ". The lessons stick.");
                }
                else GameEvents.RaiseToast(book.Def.DisplayName + ": " + Mathf.RoundToInt(st.Read * 100f) + "% read — XP x3 in " + s_Skills[book.Def.Id]);
            }, HumanoidActivity.Read);
            return true;
        }
    }
}
