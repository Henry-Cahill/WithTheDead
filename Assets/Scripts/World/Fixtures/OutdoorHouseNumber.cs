using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>The enamelled house-number plate beside the front door: "Read the house number".</summary>
    [DisallowMultipleComponent]
    public class OutdoorHouseNumber : Interactable
    {
        private Vector3 _point, _face, _normal;
        private string _number, _remark;
        private bool _scraped = false;

        public override string DisplayName => "House number";
        public override Vector3 InteractPoint => _point;
        public override bool IsAvailable => base.IsAvailable && LightingFixtures.SurvivorInFront(_face, _normal);

        /// <summary>The plate's fixture under <paramref name="host"/> (the doorway piece). A <paramref name="remark"/> replaces
        /// the starter home's line about it (a raid house is somebody else's home).</summary>
        internal static OutdoorHouseNumber Create(Transform host, string number, Vector3 point, Vector3 face, Vector3 normal, string remark = null)
        {
            var go = new GameObject("HouseNumber");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var h = go.AddComponent<OutdoorHouseNumber>();
            h._number = number;
            h._remark = remark;
            h._point = point;
            h._face = face;
            h._normal = normal;
            return h;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (_scraped)
            {
                into.Add(new InteractAction("Read the scratched plate", c =>
                    c.Notify(_remark ?? "The enamel is worn away, faint graffiti reads “—‑—”. The house number is no longer visible.")));
            }
            else
            {
                into.Add(new InteractAction("Read the house number", c =>
                    c.Notify(_remark ?? "Number " + _number + ", says the little enamel plate by the door. Home — for now.")));
                into.Add(new InteractAction("Scrape off the number", Scrape));
            }
            into.Add(new InteractAction("Knock on the door", Knock));
            into.Add(new InteractAction("Yell at the house number", Yell));
        }

        private void Scrape(InteractContext ctx)
        {
            // Loud scraping sound
            MakeNoise(CloneZConfig.FixtureNoiseDoor);
            _scraped = true;
            ctx.Notify("You scrape away the enamel, revealing faint graffiti. The house number is gone.");
        }

        private void Yell(InteractContext ctx)
        {
            // Louder thump, like shouting against a metal plate
            MakeNoise(CloneZConfig.FixtureNoiseDoor);
            ctx.Notify("Your voice bounces off the enamel plate, echoing loudly.");
        }

        private void Knock(InteractContext ctx)
        {
            // Small thud sound, like knocking on a metal plate
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            ctx.Notify("You knock on the house‑number plate.");
        }

        public override void CaptureState(FixtureSave save)
        {
            base.CaptureState(save);
            save.State = _scraped ? 1 : 0;
        }

        public override void RestoreState(FixtureSave save)
        {
            base.RestoreState(save);
            _scraped = (save.State & 1) != 0;
        }
    }
}
