using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// A window of the raid town: the starter home's double-hung window (<see cref="HomeWindow"/>) — a lower sash that slides
    /// up, a sash lock, and whatever a building of its kind hangs at its windows (a house's curtains, an office's or a
    /// clinic's blind, an army post's blackout cloth, nothing at all in a shop front) — fitted in a raid building's wall by
    /// <c>RaidBuilding.Windows.cs</c> as the town was left: glazed, smashed, or boarded over outside.
    /// <list type="bullet">
    /// <item>Glazed, it works as the home's does, and the glass can be smashed from either side — loud enough to carry down
    /// the street. From outside a latched sash only rattles.</item>
    /// <item>Smashed, the sash is past opening but the frame is a way through: over the shards (a cut, as often as not), or
    /// safely once the last of the glass has been knocked out.</item>
    /// <item>Boarded, there's no way through until the boards are pried off — slow and noisy, quicker with a crowbar or a claw
    /// hammer — leaving whatever glass was behind them.</item>
    /// </list>
    /// From outside the survivor can peer in to count the dead (not past drawn curtains). Only the parts that move or change
    /// — the lower sash, the glazing of each sash, the lock's lever, the dressing, the boards — are objects of their own under
    /// the window's wall segment; the frame, the casings and the sills are drawn into the segment itself.
    /// </summary>
    public sealed class RaidWindow : HomeWindow
    {
        /// <summary>What's left of the glass.</summary>
        internal enum Glazing { Intact, Smashed, Cleared }

        /// <summary>What hangs at the window on the room side.</summary>
        internal enum Dressing { None, Curtains, Blind, Cloth }

        /// <summary>What a raid adds to a window's <see cref="HomeWindow.Setup"/>.</summary>
        internal struct Raid
        {
            public Glazing Glass;
            /// <summary>Planks nailed across the outside.</summary>
            public bool Boarded;
            public Dressing Dressing;
            /// <summary>The glazing of the fixed upper sash and of the sliding lower one (riding on it): glass, or shards.</summary>
            public MeshFilter Upper, Lower;
            /// <summary>The planks (shown while boarded).</summary>
            public Renderer Boards;
            /// <summary>The window's measures, for drawing its shards when it's smashed: the wall line, the sashes' middle along
            /// it and their width, and each sash's heights and track (its distance across the wall line).</summary>
            public OpeningLine Line;
            public float Mid, SashWidth, UpperY0, UpperY1, UpperAcross, LowerY0, LowerY1, LowerAcross;
            public int Seed;
        }

        private Glazing _glass;
        private bool _boarded;
        private Dressing _dressing;
        private MeshFilter _upper, _lower;
        private MeshRenderer _upperRenderer, _lowerRenderer;
        private Renderer _boards;
        private OpeningLine _line;
        private float _mid, _sashWidth, _upperY0, _upperY1, _upperAcross, _lowerY0, _lowerY1, _lowerAcross;
        private int _seed;
        private float _pryClock, _pryNext;

        private Action<InteractContext> _tryAct, _smashAct, _clearAct, _pryAct, _pryBegin, _lookInAct;
        private Action<InteractContext, float, float> _pryTick;

        /// <summary>True while the glass is whole.</summary>
        public bool IsGlazed => _glass == Glazing.Intact;
        /// <summary>True while broken glass still bristles in the frame.</summary>
        public bool HasShards => _glass == Glazing.Smashed;
        /// <summary>True while planks are nailed across the outside.</summary>
        public bool IsBoarded => _boarded;

        protected override bool CanClimb => !_boarded && (IsOpen || _glass != Glazing.Intact);
        protected override string ClimbBlocked => _boarded ? "It's boarded up" : "The window is shut";

        protected override void Awake()
        {
            base.Awake();
            _tryAct = Rattle;
            _smashAct = Smash;
            _clearAct = ClearGlass;
            _pryAct = PryBoards;
            _pryBegin = c =>
            {
                _pryClock = 0f;
                _pryNext = CloneZConfig.RaidWindowPryInterval * 0.5f;
            };
            _pryTick = PryTick;
            _lookInAct = LookIn;
        }

        /// <summary>Makes <paramref name="root"/> (holding the sash, the glazing, the dressing and any boards, under the window's
        /// wall segment) a working raid window.</summary>
        internal static RaidWindow Attach(GameObject root, in Setup s, in Raid r)
        {
            RaidWindow w = Attach<RaidWindow>(root, null, s);
            w._glass = r.Glass;
            w._boarded = r.Boarded;
            w._dressing = r.Dressing;
            w._upper = r.Upper;
            w._lower = r.Lower;
            w._upperRenderer = r.Upper != null ? r.Upper.GetComponent<MeshRenderer>() : null;
            w._lowerRenderer = r.Lower != null ? r.Lower.GetComponent<MeshRenderer>() : null;
            w._boards = r.Boards;
            w._line = r.Line;
            w._mid = r.Mid;
            w._sashWidth = r.SashWidth;
            w._upperY0 = r.UpperY0;
            w._upperY1 = r.UpperY1;
            w._upperAcross = r.UpperAcross;
            w._lowerY0 = r.LowerY0;
            w._lowerY1 = r.LowerY1;
            w._lowerAcross = r.LowerAcross;
            w._seed = r.Seed;
            w.ShowGlass();
            w.ShowBoards();
            return w;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            int side = SideOf(ctx.Position, InteractPoint, Normal);
            bool inside = OutSide == 0 || side != OutSide;
            bool reach = inside || !_boarded;   // boards nailed on outside keep the glass and the sash out of reach
            bool glazed = _glass == Glazing.Intact;
            // Outside, or at an empty frame, the way through is the point; inside a whole window the sash comes first.
            bool climbFirst = CanClimb && (!inside || !glazed);

            if (_boarded) into.Add(PryAction(ctx, inside));
            else if (climbFirst) AddClimb(ctx, side, inside, into);

            // The sash, while it has its glass. From outside a latched one only rattles.
            if (glazed && reach)
            {
                if (IsOpen) into.Add(new InteractAction("Close the window", CloseAction));
                else if (!inside && IsLatched) into.Add(new InteractAction("Try the window", _tryAct));
                else into.Add(new InteractAction("Open the window", OpenAction).DisabledIf(IsLatched, "The window is latched"));
                if (inside)
                {
                    if (IsLatched) into.Add(new InteractAction("Unlatch the window", UnlatchAction));
                    else into.Add(new InteractAction("Latch the window", LatchAction).DisabledIf(IsOpen, "Close the window first"));
                }
            }

            if (_glass == Glazing.Smashed && reach)
                into.Add(new InteractAction("Clear away the broken glass", _clearAct)
                    .Timed(CloneZConfig.RaidWindowClearSeconds, "Knocking out the shards", HumanoidActivity.Use));

            if (inside)
            {
                if (_dressing != Dressing.None) into.Add(new InteractAction(DressingLabel, CurtainAction));
                into.Add(new InteractAction(OutSide == 0 ? "Look through the window" : "Look outside", LookAction)
                    .Timed(CloneZConfig.WindowLookSeconds, "Looking outside", HumanoidActivity.Use));
            }
            else into.Add(new InteractAction("Look inside", _lookInAct)
                .Timed(CloneZConfig.WindowLookSeconds, "Peering inside", HumanoidActivity.Use)
                .DisabledIf(CurtainsDrawn, DrawnReason));

            // No way through yet (say why), or the climb out of an open window, further down the list.
            if (!climbFirst) AddClimb(ctx, side, inside, into);

            // Last, and never the default: put the glass in.
            if (glazed && reach && !IsOpen)
                into.Add(new InteractAction("Smash the window", _smashAct)
                    .Timed(CloneZConfig.RaidWindowSmashSeconds, "Smashing the glass", HumanoidActivity.Build));
        }

        private string DressingLabel
        {
            get
            {
                switch (_dressing)
                {
                    case Dressing.Blind: return CurtainsDrawn ? "Raise the blind" : "Lower the blind";
                    case Dressing.Cloth: return CurtainsDrawn ? "Roll up the blackout cloth" : "Let down the blackout cloth";
                    default: return CurtainsDrawn ? "Open the curtains" : "Close the curtains";
                }
            }
        }

        private string DrawnReason
        {
            get
            {
                switch (_dressing)
                {
                    case Dressing.Blind: return "The blind is down";
                    case Dressing.Cloth: return "There's a cloth across it";
                    default: return "The curtains are drawn";
                }
            }
        }

        protected override string LookPrefix
        {
            get
            {
                if (CurtainsDrawn)
                {
                    switch (_dressing)
                    {
                        case Dressing.Blind: return "You lift the edge of the blind and peer out. ";
                        case Dressing.Cloth: return "You lift a corner of the cloth and peer out. ";
                        default: return "You ease the curtain aside and peer out. ";
                    }
                }
                return _boarded ? "You put an eye to a gap between the boards. " : null;
            }
        }

        protected override string ClimbLabel(bool inside)
        {
            if (!HasShards || IsOpen) return base.ClimbLabel(inside);
            return OutSide == 0 ? "Climb through over the broken glass" : inside ? "Climb out over the broken glass" : "Climb in over the broken glass";
        }

        // Over the shards: as often as not they find an arm or a leg on the way.
        protected override void OnClimbed(InteractContext ctx)
        {
            if (!HasShards || IsOpen) return;
            if (UnityEngine.Random.value >= CloneZConfig.RaidWindowCutChance)
            {
                ctx.Notify("Glass grates under you, but you get through without a scratch.");
                return;
            }
            bool deep = UnityEngine.Random.value < CloneZConfig.RaidWindowCutDeepChance;
            if (ctx.Wounds != null) ctx.Wounds.Inflict(deep ? WoundType.Laceration : WoundType.Scratch);
            if (ctx.Stats != null) ctx.Stats.EnvironmentDrain(CloneZConfig.RaidWindowCutDamage * (deep ? 1f : 0.4f), "bled out from a cut on broken glass");
            ctx.Notify(deep ? "A jagged shard opens a deep gash as you drag yourself through!" : "A shard nicks you on the way through \u2014 just a scratch.");
        }

        #region Glass
        private void Rattle(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.WindowNoise * 0.6f);
            ctx.Notify("The sash rattles but won't lift \u2014 it's latched from the inside.");
        }

        // An elbow (or a boot) through the glass: both sashes' panes go, leaving shards bristling in the frames.
        private void Smash(InteractContext ctx)
        {
            if (_glass != Glazing.Intact || IsOpen) return;
            _glass = Glazing.Smashed;
            DrawShards();
            ShowGlass();
            MakeNoise(CloneZConfig.RaidWindowSmashNoise);
            ctx.Notify("The glass bursts with a crash that carries down the street.");
        }

        private void ClearGlass(InteractContext ctx)
        {
            if (_glass != Glazing.Smashed) return;
            _glass = Glazing.Cleared;
            ShowGlass();
            MakeNoise(CloneZConfig.RaidWindowClearNoise);
            ctx.Notify("You knock the last of the glass out of the frame. It's safe to climb through now.");
        }

        // The glazing shows while there's glass of any kind in the frames; the cutaway gathers the wall's parts afresh.
        private void ShowGlass()
        {
            bool any = _glass != Glazing.Cleared;
            if (_upperRenderer != null) _upperRenderer.enabled = any;
            if (_lowerRenderer != null) _lowerRenderer.enabled = any;
            PartsChanged();
        }

        // Swaps both sashes' glass for shards. The lower sash is shut whenever this happens, so its glazing is drawn where it
        // stands.
        private void DrawShards()
        {
            var rng = new System.Random(_seed);
            Shards(_upper, rng, _upperY0, _upperY1, _upperAcross, false);
            Shards(_lower, rng, _lowerY0, _lowerY1, _lowerAcross, true);
            DecorPalette.Apply();
        }

        private void Shards(MeshFilter glazing, System.Random rng, float y0, float y1, float across, bool lower)
        {
            if (glazing == null) return;
            var m = new RaidMesh();
            FurnitureBuilder.OpeningShards(m, rng, _line, _mid, _sashWidth, y0, y1, across, lower);
            m.ApplyTo(glazing.gameObject, glazing.transform.position, ShadowCastingMode.Off);
        }
        #endregion

        #region Boards
        private InteractAction PryAction(InteractContext ctx, bool inside)
        {
            string tool = Tool(ctx);
            float seconds = tool == "crowbar" ? CloneZConfig.RaidWindowPryCrowbarSeconds
                : tool == "hammer" ? CloneZConfig.RaidWindowPryHammerSeconds : CloneZConfig.RaidWindowPrySeconds;
            string busy = tool == "crowbar" ? "Levering the boards off" : tool == "hammer" ? "Clawing the nails out" : "Tearing at the boards";
            var a = new InteractAction("Pry the boards off", _pryAct).Timed(seconds, busy, HumanoidActivity.Build);
            if (inside && _glass == Glazing.Intact && !IsOpen) a.DisabledIf(true, "The glass is in the way");
            a.Begin = _pryBegin;
            a.Tick = _pryTick;
            return a;
        }

        // The best thing in the pack for the job: a crowbar, else a claw hammer, else bare hands (null).
        private static string Tool(InteractContext ctx)
        {
            PlayerInventory inv = ctx.Inventory;
            if (inv == null) return null;
            if (inv.Has("crowbar")) return "crowbar";
            return inv.Has("hammer") ? "hammer" : null;
        }

        // A nail shrieks out every so often while the job goes on.
        private void PryTick(InteractContext ctx, float dt, float progress)
        {
            _pryClock += dt;
            if (_pryClock < _pryNext) return;
            _pryNext = _pryClock + CloneZConfig.RaidWindowPryInterval;
            MakeNoise(CloneZConfig.RaidWindowPryNoise);
        }

        private void PryBoards(InteractContext ctx)
        {
            if (!_boarded) return;
            _boarded = false;
            ShowBoards();
            MakeNoise(CloneZConfig.RaidWindowPryNoise);
            if (ctx.Stats != null) ctx.Stats.TryConsumeEndurance(CloneZConfig.RaidWindowPryEndurance);
            string tool = Tool(ctx);
            if (tool != null) ctx.Inventory.WearTool(tool);
            ctx.Notify(_glass == Glazing.Intact ? "The boards come away. The glass behind them is still whole."
                : _glass == Glazing.Smashed ? "The boards come away, nails shrieking. Broken glass still bristles in the frame."
                : "The boards come away, nails shrieking. The frame behind them is empty.");
        }

        private void ShowBoards()
        {
            if (_boards != null) _boards.enabled = _boarded;
            PartsChanged();
        }
        #endregion

        // Peering in from outside: the dead in there, as far as the light allows and where nothing hides them.
        private void LookIn(InteractContext ctx)
        {
            if (CurtainsDrawn)
            {
                ctx.Notify("You can't see in \u2014 " + DrawnReason.ToLowerInvariant() + ".");
                return;
            }
            Vector3 inward = Normal * -SideOf(ctx.Position, InteractPoint, Normal);
            float range = Lookout.Range(CloneZConfig.WindowLookRange, out bool dark);
            int n = Lookout.Count(InteractPoint + inward * 0.16f, InteractPoint, inward, range, out float nearest);
            string seen;
            if (n == 0) seen = dark ? "It's black as pitch in there \u2014 nothing you can make out." : "Nothing stirs inside.";
            else if (nearest <= CloneZConfig.WindowUnderRange)
                seen = n == 1 ? "One of the dead is right by the window!"
                    : Lookout.Capitalised(Lookout.Number(n)) + " of the dead in there \u2014 one right by the window!";
            else seen = n == 1 ? "One of the dead stands inside, swaying." : "You make out " + Lookout.Number(n) + " of the dead inside.";
            if (dark && n > 0) seen += " There may be more in the shadows.";
            if (_boarded) seen = "You peer in between the boards. " + seen;
            ctx.Notify(seen);
        }

        public override void CaptureState(FixtureSave save)
        {
            base.CaptureState(save);
            save.State |= ((int)_glass << 3) | (_boarded ? 32 : 0);
        }

        public override void RestoreState(FixtureSave save)
        {
            base.RestoreState(save);
            var glass = (Glazing)((save.State >> 3) & 3);
            if (glass != _glass && glass != Glazing.Intact)
            {
                if (_glass == Glazing.Intact) DrawShards();
                _glass = glass;
                ShowGlass();
            }
            if (_boarded && (save.State & 32) == 0)
            {
                _boarded = false;
                ShowBoards();
            }
        }
    }
}
