using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The food mart's floor safe in the stock room, locked. "Work the dial" is a long job — an ear to the door, the dial
    /// turned a notch at a time, the handle tried and tried again, with a clunk and a thump every so often that carries
    /// further than you'd like — and at the end of it the bolts slide back and the door swings open. From then on the
    /// safe is ordinary storage (its <see cref="StorageFixture"/>, kept switched off until now so nothing inside can be
    /// reached, nor listed among the containers nearby, before it's open). Nothing is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidStoreSafe : Interactable
    {
        private static readonly string[] Listening =
        {
            "You press your ear to the cold steel and turn the dial slowly. Click... click... a faint heavier clunk.",
            "The handle won't budge. Somebody has scratched three numbers into the paint beside it, then scratched them out.",
        };

        private Transform _body;
        private Vector3 _point;
        private StorageFixture _storage;
        private float _noiseAt;
        private int _listened;

        public override string DisplayName => "Safe";
        public override Vector3 InteractPoint => _point;
        public override Transform OcclusionRoot => _body != null ? _body : transform;

        /// <summary>The locked safe's fixture on its body <paramref name="body"/>: <paramref name="point"/> is the dial; once
        /// it's open the survivor uses <paramref name="storage"/> (switched off here until then).</summary>
        internal static RaidStoreSafe Create(Transform body, Vector3 point, StorageFixture storage)
        {
            var go = new GameObject("Fixture_SafeDial");
            go.transform.SetParent(body, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var s = go.AddComponent<RaidStoreSafe>();
            s._body = body;
            s._point = point;
            s._storage = storage;
            if (storage != null) storage.gameObject.SetActive(false);
            return s;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            var dial = new InteractAction("Work the dial").Timed(CloneZConfig.RaidSafeDialSeconds, "Working the dial", HumanoidActivity.Use);
            dial.Begin = c => _noiseAt = Time.time + CloneZConfig.RaidSafeDialNoiseInterval * 0.5f;
            dial.Tick = (c, dt, progress) =>
            {
                if (Time.time < _noiseAt) return;
                _noiseAt = Time.time + CloneZConfig.RaidSafeDialNoiseInterval;
                MakeNoise(CloneZConfig.RaidSafeDialNoise);
            };
            dial.Run = Crack;
            into.Add(dial);
            var force = new InteractAction("Force the dial").Timed(CloneZConfig.RaidSafeDialSeconds * 2, "Working the dial", HumanoidActivity.Use);
            force.Begin = c => _noiseAt = Time.time + CloneZConfig.RaidSafeDialNoiseInterval * 0.5f;
            force.Tick = (c, dt, progress) =>
            {
                if (Time.time < _noiseAt) return;
                _noiseAt = Time.time + CloneZConfig.RaidSafeDialNoiseInterval;
                MakeNoise(CloneZConfig.RaidSafeDialNoise);
            };
            force.Run = Crack;
            into.Add(force);
            into.Add(new InteractAction("Try the handle", c => c.Notify("Locked solid. The wheel turns a quarter and stops dead.")));
            into.Add(new InteractAction("Inspect the dial", c => c.Notify("The dial is stiff and scratched; it looks like someone tried to force it.")));
            into.Add(new InteractAction("Listen at the door", c => c.Notify(Listening[_listened++ % Listening.Length])));
            into.Add(new InteractAction("Knock on the safe", c => { MakeNoise(CloneZConfig.RaidSafeDialNoise); c.Notify("Your knuckles rattle the steel. Nothing else happens."); }));
        }

        // The last number falls into place: the bolts draw back, the door swings open and the safe becomes plain storage.
        private void Crack(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.RaidSafeDialNoise);
            if (_storage != null)
            {
                _storage.gameObject.SetActive(true);
                _storage.SetOpen(true, false);
            }
            ctx.Notify("A last soft click, and the bolts slide back with a clunk you feel in your teeth. The door swings open.");
            if (_storage != null && _storage.Container != null && ctx.Interactor != null) ctx.Interactor.SearchOrOpen(_storage.Container);
            gameObject.SetActive(false);
        }
    }
}
