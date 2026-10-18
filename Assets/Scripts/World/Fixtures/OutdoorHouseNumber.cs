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
            into.Add(new InteractAction("Read the house number", c =>
                c.Notify(_remark ?? "Number " + _number + ", says the little enamel plate by the door. Home \u2014 for now.")));
        }
    }
}
