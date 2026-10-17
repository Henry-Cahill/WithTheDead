using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A framed print on the wall. "Look at the picture" describes it and, now and then, steadies the nerves a
    /// little. A picture hanging crooked — as the house came, or knocked askew when the dead batter its wall — can
    /// be straightened. The frame and print turn about the wall's normal as one object; the tilt is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class PictureFixture : Interactable
    {
        private const float CrookedAbove = 0.75f;   // degrees of tilt the eye notices
        private const float SwingSpeed = 45f;       // degrees a second the frame swings to its new tilt

        private string _key;
        private string _description;
        private Vector3 _point, _face, _normal;
        private Transform _frame;
        private BuildPiece _wall;
        private float _roll, _shownRoll;
        private float _wallHealth;
        private float _calmAgainAt;
        private float _nextCheck;

        public override string DisplayName => "Picture";
        public override Vector3 InteractPoint => _point;
        public override bool IsAvailable => base.IsAvailable && LightingFixtures.SurvivorInFront(_face, _normal);
        public override string SaveKey => _key;

        /// <summary>True while the picture hangs noticeably crooked.</summary>
        public bool IsCrooked => Mathf.Abs(_roll) > CrookedAbove;

        /// <summary>A picture under <paramref name="wall"/>'s piece, centred on <paramref name="face"/> (on the wall face)
        /// and looking along <paramref name="normal"/>, hanging <paramref name="roll"/> degrees off level.</summary>
        internal static PictureFixture Create(BuildPiece wall, Vector3 point, Vector3 face, Vector3 normal, float roll, string description)
        {
            var go = new GameObject("PictureFixture");
            go.transform.SetParent(wall.transform, false);
            go.transform.SetPositionAndRotation(face, Quaternion.identity);
            var p = go.AddComponent<PictureFixture>();
            p._wall = wall;
            p._wallHealth = wall.Health;
            p._point = point;
            p._face = face;
            p._normal = normal;
            p._roll = p._shownRoll = Mathf.Clamp(roll, -CloneZConfig.PictureMaxRoll, CloneZConfig.PictureMaxRoll);
            p._description = description;
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
            into.Add(new InteractAction("Look at the picture", Look));
            if (IsCrooked) into.Add(new InteractAction("Straighten the picture", Straighten));
        }

        private void Look(InteractContext ctx)
        {
            ctx.Notify(_description);
            if (IsCrooked) ctx.Notify("It's hanging a little crooked.");
            if (Time.time < _calmAgainAt) return;
            LightingFixtures.Calm(ctx, CloneZConfig.PictureCalm);
            _calmAgainAt = Time.time + CloneZConfig.PictureCalmCooldown;
        }

        private void Straighten(InteractContext ctx)
        {
            _roll = 0f;
            ctx.Notify("You nudge the frame level. Better.");
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
