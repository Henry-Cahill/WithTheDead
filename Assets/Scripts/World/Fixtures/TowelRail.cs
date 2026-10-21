using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The chrome towel rail on the bathroom wall over the toilet, a towel folded over it. "Dry yourself off" rubs the wet
    /// out of the survivor — after a shower, or a soaking out in the rain — and afterwards the towel hangs damp (a shade
    /// darker, slung back on the rail a little crooked) until it dries out over a few in-game hours; a damp towel only
    /// takes the worst of the wet off. It can be straightened. Usable only from the bathroom side of the wall. Saves when
    /// it'll be dry again and how crooked it hangs.
    /// </summary>
    [DisallowMultipleComponent]
    public class TowelRail : Interactable
    {
        private const float CrookedAbove = 1f;   // degrees of tilt the eye notices
        private const float SwingSpeed = 40f;    // degrees a second the towel settles to its new tilt

        private string _key;
        private Vector3 _point, _face, _normal;
        private Transform _towel;
        private Renderer _dry, _damp;
        private float _dryAt = -1f;               // the in-game hour (TimeOfDay.Now) it's dry again; below 0 it is
        private float _roll, _shownRoll;
        private float _nextCheck;
        private float _nextAvailCheck;
        private bool _cachedAvailable = true;

        public override string DisplayName => "Towel rail";
        public override Vector3 InteractPoint => _point;
        public override bool IsAvailable => base.IsAvailable && _cachedAvailable;
        public override string SaveKey => _key;

        /// <summary>True while the towel is still damp from the last use.</summary>
        public bool IsDamp => _dryAt >= 0f && TimeOfDay.Now < _dryAt;

        /// <summary>True while the towel hangs noticeably crooked.</summary>
        public bool IsCrooked => Mathf.Abs(_roll) > CrookedAbove;

        /// <summary>A towel rail under <paramref name="wall"/> (the wall piece). <paramref name="point"/> is where the survivor
        /// reaches (the towel's front), <paramref name="face"/> the rail's spot on the wall face and <paramref name="normal"/>
        /// the way it faces.</summary>
        internal static TowelRail Create(Transform wall, Vector3 point, Vector3 face, Vector3 normal)
        {
            var go = new GameObject("TowelRail");
            go.transform.SetParent(wall, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var t = go.AddComponent<TowelRail>();
            t._point = point;
            t._face = face;
            t._normal = normal;
            t._key = KeyFor("towelrail", point);
            return t;
        }

        /// <summary>Hands over the towel: a pivot on the rail's bar (the towel tilts about the wall's normal through it)
        /// carrying its dry look and its damp one.</summary>
        internal void SetTowel(Transform towel, Renderer dry, Renderer damp)
        {
            _towel = towel;
            _dry = dry;
            _damp = damp;
            ShowLook();
            ApplyRoll();
        }

private void Update()
{
    if (_shownRoll != _roll)
    {
        _shownRoll = Mathf.MoveTowards(_shownRoll, _roll, SwingSpeed * Time.deltaTime);
        ApplyRoll();
    }
    // Dryness check (original behavior)
    if (Time.time >= _nextCheck)
    {
        _nextCheck = Time.time + 1f;
        if (_dryAt >= 0f && !IsDamp)
        {
            _dryAt = -1f;
            ShowLook();
        }
    }
    // Cache bathroom-side availability check
    if (Time.time >= _nextAvailCheck)
    {
        _nextAvailCheck = Time.time + 1f;
        _cachedAvailable = BathroomFixtures.SurvivorInFront(_face, _normal);
    }
}

        private void ApplyRoll()
        {
            if (_towel != null) _towel.rotation = Quaternion.AngleAxis(_shownRoll, _normal);
        }

        private void ShowLook()
        {
            bool damp = IsDamp;
            if (_dry != null && _dry.enabled == damp) _dry.enabled = !damp;
            if (_damp != null && _damp.enabled != damp) _damp.enabled = damp;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Dry yourself off", DryOff).Timed(CloneZConfig.TowelDrySeconds, "Drying off", HumanoidActivity.Use));
            into.Add(new InteractAction("Straighten the towel", Straighten));
            into.Add(new InteractAction("Inspect towel", Inspect));
            into.Add(new InteractAction("Check towel dryness", CheckDryness));
            into.Add(new InteractAction("Let towel dry", LetDry));
        }

        private void CheckDryness(InteractContext ctx)
        {
            if (IsDamp)
            {
                float remaining = _dryAt - TimeOfDay.Now;
                if (remaining < 0f) remaining = 0f;
                ctx.Notify($"The towel will be dry in {remaining:0.0} hours.");
            }
            else
            {
                ctx.Notify("The towel is already dry.");
            }
        }

        private void DryOff(InteractContext ctx)
        {
            PlayerStats s = ctx.Stats;
            float wet = s != null ? s.Wetness : 0f;
            if (wet <= 0.05f)
            {
                ctx.Notify("You're dry already. You give your hands a wipe anyway, for the feel of a clean towel.");
                return;
            }
            bool damp = IsDamp;
            s.Wetness = damp ? Mathf.Min(s.Wetness, CloneZConfig.TowelDampLeaves) : 0f;
            _dryAt = TimeOfDay.Now + CloneZConfig.TowelDampHours;
            ShowLook();
            _roll = Random.Range(2.5f, CloneZConfig.TowelCrookedRoll) * (Random.value < 0.5f ? -1f : 1f);
            ctx.Notify(damp ? "The towel's still damp from last time. It takes the worst of the wet off, and that's all."
                : wet >= 0.55f ? "You rub yourself down from head to toe, hair and all. Dry at last."
                : "A quick rub with the towel and you're dry.");
        }

                private void Straighten(InteractContext ctx)
        {
            if (!IsCrooked)
            {
                ctx.Notify("It's hanging straight already. You give it a tug anyway.");
                return;
            }
            _roll = 0f;
            ctx.Notify("You square the towel up on the rail and smooth it flat. There.");
        }

        private void LetDry(InteractContext ctx)
        {
            if (!IsDamp)
            {
                ctx.Notify("The towel is already dry.");
                return;
            }
            // Speed up drying by setting the dry time to now.
            _dryAt = TimeOfDay.Now;
            ShowLook();
            ctx.Notify("You give the towel some extra air, and it dries instantly.");
        }

        // New inspection action
        private void Inspect(InteractContext ctx)
        {
            // Report towel condition: dampness and crookedness.
            string damp = IsDamp ? "damp" : "dry";
            string crooked = IsCrooked ? "crooked" : "straight";
            ctx.Notify($"The towel is {damp} and {crooked}.");
        }

        public override void CaptureState(FixtureSave save)
        {
            save.State = 1;
            save.A = IsDamp ? _dryAt : -1f;
            save.B = _roll;
        }

        public override void RestoreState(FixtureSave save)
        {
            if ((save.State & 1) == 0) return;
            _dryAt = save.A;
            _roll = _shownRoll = Mathf.Clamp(save.B, -CloneZConfig.TowelCrookedRoll, CloneZConfig.TowelCrookedRoll);
            ShowLook();
            ApplyRoll();
        }
    }
}
