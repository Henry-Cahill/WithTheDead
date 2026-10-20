using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A framed print on the wall. "Look at the picture" describes it and, now and then, steadies the nerves a
    /// little. A picture hanging crooked — as the house came, or knocked askew when the dead batter its wall — can
    /// be straightened. The frame and print turn about the wall's normal as one object; the tilt is saved. The same
    /// fixture serves any framed thing on a wall — the raid town's posters, calendars and notices — named for what it is.
    /// </summary>
    [DisallowMultipleComponent]
    public class PictureFixture : Interactable
    {
        private const float CrookedAbove = 0.75f;   // degrees of tilt the eye notices
        private const float SwingSpeed = 45f;       // degrees a second the frame swings to its new tilt

        private string _key;
        private string _description;
        private string _name = "Picture", _lookLabel = "Look at the picture", _straightenLabel = "Straighten the picture";
        private bool _calming = true;
        private Vector3 _point, _face, _normal;
        private Transform _frame;
        private BuildPiece _wall;
        private float _roll, _shownRoll;
        private float _wallHealth;
        private float _calmAgainAt;
        private float _nextCheck;

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => _point;
        public override bool IsAvailable => base.IsAvailable && LightingFixtures.SurvivorInFront(_face, _normal);
        public override string SaveKey => _key;

        /// <summary>True while the picture hangs noticeably crooked.</summary>
        public bool IsCrooked => Mathf.Abs(_roll) > CrookedAbove;

        /// <summary>A picture under <paramref name="wall"/>'s piece, centred on <paramref name="face"/> (on the wall face)
        /// and looking along <paramref name="normal"/>, hanging <paramref name="roll"/> degrees off level. A blow to the
        /// wall can knock it askew.</summary>
        internal static PictureFixture Create(BuildPiece wall, Vector3 point, Vector3 face, Vector3 normal, float roll, string description)
        {
            PictureFixture p = Create(wall.transform, point, face, normal, roll, description);
            p._wall = wall;
            p._wallHealth = wall.Health;
            return p;
        }

        /// <summary>A picture under <paramref name="host"/> (the wall segment it hangs on), centred on <paramref name="face"/>
        /// (on the wall face) and looking along <paramref name="normal"/>, hanging <paramref name="roll"/> degrees off level.
        /// <paramref name="noun"/> names it in its prompts ("poster": "Look at the poster"); a <paramref name="calming"/>
        /// one steadies the nerves now and then when looked at.</summary>
        internal static PictureFixture Create(Transform host, Vector3 point, Vector3 face, Vector3 normal, float roll, string description,
            string noun = "picture", bool calming = true)
        {
            var go = new GameObject("PictureFixture");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(face, Quaternion.identity);
            var p = go.AddComponent<PictureFixture>();
            p._point = point;
            p._face = face;
            p._normal = normal;
            p._roll = p._shownRoll = Mathf.Clamp(roll, -CloneZConfig.PictureMaxRoll, CloneZConfig.PictureMaxRoll);
            p._description = description;
            if (!string.IsNullOrEmpty(noun) && noun != "picture")
            {
                p._name = char.ToUpperInvariant(noun[0]) + noun.Substring(1);
                p._lookLabel = "Look at the " + noun;
                p._straightenLabel = "Straighten the " + noun;
            }
            p._calming = calming;
            p._key = KeyFor("picture", point);
            return p;
        }

        /// <summary>Hands over the frame and print: drawn level, its object's origin on the picture's centre.</summary>
        internal void SetFrame(Transform frame)
        {
            _frame = frame;
            Apply();
        }

        private void Update()
        {
            if (_shownRoll != _roll)
            {
                _shownRoll = Mathf.MoveTowards(_shownRoll, _roll, SwingSpeed * Time.deltaTime);
                Apply();
            }
            if (Time.time < _nextCheck || _wall == null) return;
            _nextCheck = Time.time + CloneZConfig.SwitchSyncInterval;
            // A blow to the wall can knock the picture askew.
            float hp = _wall.Health;
            if (hp < _wallHealth - 0.01f && Random.value < CloneZConfig.PictureKnockChance)
            {
                float knock = Random.Range(3f, 7f) * (Random.value < 0.5f ? -1f : 1f);
                _roll = Mathf.Clamp(_roll + knock, -CloneZConfig.PictureMaxRoll, CloneZConfig.PictureMaxRoll);
            }
            _wallHealth = hp;
        }

        private void Apply()
        {
            if (_frame != null) _frame.rotation = Quaternion.AngleAxis(_shownRoll, _normal);
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction(_lookLabel, Look));
            into.Add(new InteractAction("Inspect the picture", Inspect));
            if (IsCrooked) into.Add(new InteractAction(_straightenLabel, Straighten));
        }

        private void Inspect(InteractContext ctx)
        {
            Look(ctx);
            ctx.Notify("A brief memory of home flits through your mind.");
        }
        private void Look(InteractContext ctx)
        {
            ctx.Notify(_description);
            if (IsCrooked) ctx.Notify("It's hanging a little crooked.");
            if (!_calming || Time.time < _calmAgainAt) return;
            LightingFixtures.Calm(ctx, CloneZConfig.PictureCalm);
            _calmAgainAt = Time.time + CloneZConfig.PictureCalmCooldown;
        }

        private void Straighten(InteractContext ctx)
        {
            _roll = 0f;
            ctx.Notify("You nudge the frame level. Better.");
            if (_calming && Time.time >= _calmAgainAt) {
                LightingFixtures.Calm(ctx, CloneZConfig.PictureCalm);
                _calmAgainAt = Time.time + CloneZConfig.PictureCalmCooldown;
            }
        }

        public override void CaptureState(FixtureSave save)
        {
            save.State = 1;
            save.A = _roll;
        }

        public override void RestoreState(FixtureSave save)
        {
            if ((save.State & 1) == 0) return;
            _roll = _shownRoll = Mathf.Clamp(save.A, -CloneZConfig.PictureMaxRoll, CloneZConfig.PictureMaxRoll);
            Apply();
        }
    }
}
