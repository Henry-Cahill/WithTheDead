using UnityEngine;
using System;

namespace CloneZ
{
    // Builds a simple wooden swing hanging from a porch railing.
    // Call only after the porch posts and top rail have been placed.
    public static class PorchSwingBuilder
    {
        internal static void Build(KentuckyMap.Site site, Vector3 leftPost, Vector3 rightPost,
            float railYaw, Color woodColor, System.Random rng)
        {
            // Randomly decide whether this house gets a swing (30 % chance).
            if (rng.NextDouble() >= 0.30) return;
            // Mid‑point between the two railing posts (horizontal).
            Vector3 mid = (leftPost + rightPost) * 0.5f;
            // Rope length from the rail down to the seat centre.
            const float ropeLength = 0.85f;
            // Rope geometry – thin rectangular box.
            const float ropeThickness = 0.02f;
            Vector3 ropeSize = new Vector3(ropeThickness, ropeLength, ropeThickness);
            // Rope sits centred at the rail height, extending downward.
            Vector3 ropePos = mid - new Vector3(0f, ropeLength * 0.5f, 0f);
            var woodMat = DecorMaterials.WoodFloor(woodColor);
            site.Town.Out.Solid(ropePos).Box(ropePos, ropeSize, railYaw, woodMat);
            // No collider for rope – player can walk through.
            // Seat geometry – a simple wooden plank.
            Vector3 seatSize = new Vector3(0.60f, 0.08f, 0.30f);
            // Seat sits at the bottom of the rope.
            Vector3 seatPos = mid - new Vector3(0f, ropeLength, 0f);
            site.Town.Out.Solid(seatPos).Box(seatPos, seatSize, railYaw, woodMat);
            site.Town.PropCollider(seatPos, seatSize, railYaw);
            // Mark the seat’s footprint as taken so other props (mailbox, etc.) won’t intersect.
            site.Take(new Vector2(seatPos.x, seatPos.z), Mathf.Max(seatSize.x, seatSize.z) * 0.5f);
        }
    }
}
