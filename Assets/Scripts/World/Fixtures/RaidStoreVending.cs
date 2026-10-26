using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The break room's snack and soda machine. Nobody carries change any more, so the buttons are no use; but a good thump
    /// on its side (loud) may shake something off its coil into the delivery tray, and putting the front glass in (very
    /// loud — the whole block hears it) opens up whatever is still inside to be searched. The tray and the stock are
    /// fixture containers of its own (<see cref="LootContainer.SpawnFixture"/>); the stock stays out of reach — and out
    /// of the inventory's list of nearby containers — until the glass is broken. Nothing is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidStoreVending : Interactable
    {
        // What it was stocked with: crisps, soda, crackers, juice boxes and bottled water.
        /// <summary>Possible scrap items that can be salvaged from a broken vending machine.</summary>
        private static readonly string[] Scraps = { "metal_scrap", "wire", "duct_tape" };

        private static readonly string[] Snacks = { "chips", "soda", "crackers", "juicebox", "waterbottle", "chips", "soda" };
        private static readonly string[] Misses =
        {
            "The machine booms like a drum. Something inside shifts, then settles. Nothing drops.",
            "You thump it again. A packet sways on its coil, thinks about it, and stays put.",
            "The glass rattles in its frame. Not a crumb comes loose.",
            "Your thump reverberates, but nothing shifts; the machine stays stubbornly still.",
            "The coils grin at your effort, yet they refuse to give up any treasure.",
            "A hollow clang echoes, but no snack falls from the depths.",
            "A faint promise of loot hangs in the air, though the machine yields nothing this time.",
            "You thump echoes futilely, the coils remain unmoved.",
            "A hollow thud reverberates, but the machine holds its secrets.",
            "The metal shudders, yet no snack is dislodged.",
            "A distant clang rolls, the vending's belly stays sealed.",
            "Even your forceful hit finds only stubborn silence.",
            "The frosted metal shivers under the blow, but the coils keep their secret cargo.",
            "A thin wind whistles through the cracked glass, carrying no crumbs of snack.",
            "Your thump rattles the rusted frame; only a cold echo returns.",
            "The vending box sighs with a metallic wheeze, yet nothing falls into the tray.",
            "A faint smell of stale soda hangs in the air, but no bottle is freed.",
            "The machine’s heart beats a silent rhythm, refusing to spill its winter‑stewed loot.",
            "A hollow clang reverberates; the coils remain stubbornly mute.",
            "The frosted metal shivers under your blow, yet no snack rattles free.",
            "Your thump sends a thin gust of stale air through the machine, but no crumbs fall.",
            "A distant echo rattles the panel, but the interior stays sealed tight.",
            "The impact sparks a brief whine, then settles into deafening silence.",
            "A faint scent of yesterday’s soda lingers, offering only memory, not loot.",
        };

        private Transform _body;
        private Vector3 _panel, _trayPoint;
        private Renderer _glass, _smashed;
        private LootContainer _stock, _tray;
        private bool _lit, _broken;
        private const int BitLit = 1 << 0; // already used by existing save logic
        private const int BitBroken = 1 << 1; // persists whether the glass is smashed
        private const int BitScavenged = 1 << 2; // persists whether the wreckage has been scavenged
        private const float LitThumpNoiseMultiplier = 1.5f;
        private int _missed;
        private bool _scavenged;

        public override string DisplayName => "Vending machine";
        public override Vector3 InteractPoint => _panel;
        public override Transform OcclusionRoot => _body != null ? _body : transform;

        /// <summary>The machine's fixture on its body <paramref name="body"/>: <paramref name="panel"/> is the selection
        /// panel, <paramref name="tray"/> the delivery tray; <paramref name="glass"/> and <paramref name="smashed"/> are the
        /// window whole and broken in. <paramref name="lit"/>: it's plugged in and glowing.</summary>
        internal static RaidStoreVending Create(Transform body, Vector3 panel, Vector3 tray, Renderer glass, Renderer smashed, bool lit, int seed)
        {
            // Instantiate a container GameObject for the vending fixture
            var go = new GameObject("Fixture_Vending");
            go.transform.SetParent(body, false);
            go.transform.SetPositionAndRotation(panel, Quaternion.identity);
            var v = go.AddComponent<RaidStoreVending>();
            v._body = body;
            v._panel = panel;
            v._trayPoint = tray;
            v._glass = glass;
            v._smashed = smashed;
            v._lit = lit;
            string key = KeyFor("vending", body.position);
            var rng = new System.Random(seed);

            v._stock = LootContainer.SpawnFixture(go.transform, panel, ContainerType.Pantry, "Vending machine", key + "/stock", seed,
                startEmpty: true, searchTime: CloneZConfig.RaidVendingSearchSeconds);
            int stacks = CloneZConfig.RaidVendingStockMin + rng.Next(CloneZConfig.RaidVendingStockMax - CloneZConfig.RaidVendingStockMin + 1);
            // Populate the stock with a random number of snack items
            for (int i = 0; i < stacks; i++)
            {
                ItemDef def = ItemDatabase.Get(Snacks[rng.Next(Snacks.Length)]);
                if (def != null) v._stock.Contents.Add(def, 1 + rng.Next(2), enforceWeight: false);
            }
            // Ensure there is at least some loot if the random selection left the container empty
            if (v._stock.IsEmpty) ItemDatabase.RollLoot(ContainerType.Pantry, rng, v._stock.Contents);
            v._stock.Searched = false;
            v._stock.Owner = v;
            // Hide the stock container until the glass is broken, so it can't be accessed directly
            v._stock.gameObject.SetActive(false);

            // Initialise the delivery tray container and hide it behind intact glass
            v._tray = LootContainer.SpawnFixture(go.transform, tray, ContainerType.Pantry, "Delivery tray", key + "/tray", seed + 1,
                startEmpty: true, searchTime: 0f);
            // Record this object as the tray's owner for later interactions
            v._tray.Owner = v;
            v.ShowBroken(false);
            return v;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            // Determine if the delivery tray contains any items to reach into
            bool tray = _tray != null && !_tray.IsEmpty;
            if (tray) into.Add(new InteractAction("Reach into the delivery tray", c => Open(c, _tray)));
            // If the machine hasn't been smashed, expose actions for an intact machine
            if (!_broken)
            {
                into.Add(new InteractAction("Thump the machine", Thump)
                    .Timed(CloneZConfig.RaidVendingThumpSeconds, "Thumping the machine", HumanoidActivity.Use));
                // Let the player smash the glass, exposing the interior for scavenging
                into.Add(new InteractAction("Smash the glass", Smash)
                    .Timed(CloneZConfig.RaidVendingSmashSeconds, "Smashing the glass", HumanoidActivity.Use));
                into.Add(new InteractAction("Press a button", PressButton));
                // Toggle power on or off, changing the machine’s lit state
                into.Add(new InteractAction("Toggle power", TogglePower));
                // Show detailed info about the machine’s current state
                into.Add(new InteractAction("Inspect the machine", Inspect));
                // Let the player listen for any faint whirring noises
                into.Add(new InteractAction("Listen to the machine", Listen));
                // Check whether the stock still contains any items
                into.Add(new InteractAction("Check the machine", CheckStock));
            }
            // When broken, expose actions for searching the interior or scavenging wreckage
            else
            {
                into.Add(new InteractAction(_stock != null && _stock.Searched ? "Look inside the machine" : "Search the machine", c => Open(c, _stock)));
                if (!_scavenged)
                    into.Add(new InteractAction("Scavenge the wreckage", Scavenge)
                        .Timed(2f, "Scavenging the wreckage", HumanoidActivity.Use));
            }
        }

        private void Open(InteractContext ctx, LootContainer c)
        {
            if (c != null && ctx.Interactor != null) ctx.Interactor.SearchOrOpen(c);
        }

        /// <summary>Handles the player thumping the machine, possibly dislodging a snack onto the tray.</summary>
        private void Thump(InteractContext ctx)
        {
            // Scale the noise radius: louder if the machine is powered on
            float noiseRadius = CloneZConfig.RaidVendingThumpNoise * (_lit ? LitThumpNoiseMultiplier : 1f);
            // Emit the thump noise so nearby zombies can hear it
            MakeNoise(noiseRadius);
            // Choose a random snack from stock with a chance to drop; otherwise no item is selected
            ItemStack loose = _stock != null && !_stock.IsEmpty && Random.value < CloneZConfig.RaidVendingThumpChance
                ? _stock.Contents.Stacks[Random.Range(0, _stock.Contents.Stacks.Count)] : null;
            // If nothing drops, show a miss message (cycling through flavored "nothing" lines)
            if (loose == null)
            {
                ctx.Notify(_stock == null || _stock.IsEmpty
                    ? "You thump it. Nothing left on the coils to shake loose."
                    : Misses[_missed++ % Misses.Length]);
                return;
            }
            // Resolve a readable name for the dropped item (fallback to "something")
            string name = loose.Def != null && !string.IsNullOrEmpty(loose.Def.DisplayName) ? loose.Def.DisplayName : "something";
            // Transfer the item from stock to the tray and inform the player
            if (_stock.Contents.TransferTo(_tray.Contents, loose, 1, enforceWeight: false) > 0)
                ctx.Notify("Something gives with a clunk and drops into the tray: " + name + ".");
        }

        /// <summary>Breaks the glass, exposing the machine's interior for scavenging.</summary>
        private void Smash(InteractContext ctx)
        {
            _broken = true;
            _scavenged = false;
            ShowBroken(true);
            MakeNoise(CloneZConfig.RaidVendingSmashNoise);
            if (_stock != null) _stock.gameObject.SetActive(true);
            ctx.Notify("The front glass caves in with a crash that must carry halfway down the street. The coils are yours.");
        }

        private void PressButton(InteractContext ctx)
        {
            ctx.Notify(_lit
                ? "You punch B4. The display flickers: EXACT CHANGE ONLY. Nobody carries change any more."
                : "The buttons click under your thumb. Nothing lights up \u2014 somebody's pulled its plug.");
        }

        private void Listen(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.RaidVendingThumpNoise);
            ctx.Notify("You press your ear to the vending machine; faint whirring can be heard, but it offers no help.");
        }

                private void CheckStock(InteractContext ctx)
        {
            if (_stock != null && !_stock.IsEmpty) ctx.Notify("The vending machine seems to still hold something.");
            else ctx.Notify("The vending machine looks empty.");
        }

        /// <summary>Gathers a random scrap part from the broken vending machine.</summary>
        private void Scavenge(InteractContext ctx)
        {
            // pick a random scrap definition
            var rng = new System.Random();
            string id = Scraps[rng.Next(Scraps.Length)];
            var def = ItemDatabase.Get(id);
            if (def != null && _stock != null)
            {
                _stock.Contents.Add(def, 1, enforceWeight: false);
                ctx.Notify($"You pry out a {def.DisplayName ?? id} from the smashed machine.");
            }
            else
            {
                ctx.Notify("You find nothing useful among the wreckage.");
            }
            _scavenged = true;
        }
        private void TogglePower(InteractContext ctx)
        {
            _lit = !_lit;
            ctx.Notify(_lit ? "You wrestle the cord back into the outlet; a faint glow returns to the panel." : "You yank the cord out; the machine goes dark.");
        }
        /// <summary>Provides a brief description of the machine's current state.</summary>
        private void Inspect(InteractContext ctx)
        {
            if (_broken)
                ctx.Notify("The shattered glass reveals the grim interior of the vending machine.");
            else if (_lit)
                ctx.Notify("The vending machine hums faintly, its display flickering with a broken \"EXACT CHANGE ONLY\" message.");
            else
                ctx.Notify("The vending machine sits silent, its glass whole and its lights off.");
        }

        public override void RestoreState(FixtureSave save)
        {
            base.RestoreState(save);
            _lit = (save.State & BitLit) != 0;
            _broken = (save.State & BitBroken) != 0;
            _scavenged = (save.State & BitScavenged) != 0;
            // Reflect the broken flag visually and make the stock container accessible if needed
            ShowBroken(_broken);
            if (_broken && _stock != null)
                _stock.gameObject.SetActive(true);
        }
        public override void CaptureState(FixtureSave save)
        {
            base.CaptureState(save);
            const int mask = BitLit | BitBroken | BitScavenged;
            int bits = (_lit ? BitLit : 0) |
                       (_broken ? BitBroken : 0) |
                       (_scavenged ? BitScavenged : 0);
            save.State = (save.State & ~mask) | bits;
        }
        private void ShowBroken(bool broken)
        {
            if (_glass != null) _glass.enabled = !broken;
            if (_smashed != null) _smashed.enabled = broken;
        }
    }
}
