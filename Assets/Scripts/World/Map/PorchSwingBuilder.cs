using UnityEngine;
using System;

namespace CloneZ
{
    // Builds a simple wooden swing hanging from a porch railing, now with a wooden seat, back‑rest, rope material, and metal bracket supports.
    // Call only after the porch posts and top rail have been placed.
    // The swing length varies slightly per house, and the occupied footprint prevents intersecting props.
    public static class PorchSwingBuilder
    {
        internal static void Build(KentuckyMap.Site site, Vector3 leftPost, Vector3 rightPost,
            float railYaw, Color woodColor, System.Random rng)
        {
            // Randomly decide whether this house gets a swing (30 % chance).
            if (rng.NextDouble() >= 0.30) return;
            // Mid‑point between the two railing posts (horizontal).
            Vector3 mid = (leftPost + rightPost) * 0.5f;
            // Slight random variation in swing length (±0.1 m).
            float lengthVar = (float)(rng.NextDouble() * 0.2 - 0.1);
            float ropeLength = 0.85f + lengthVar;
            // Rope geometry – thin rectangular box.
            const float ropeThickness = 0.02f;
            Vector3 ropeSize = new Vector3(ropeThickness, ropeLength, ropeThickness);
            // Rope sits centred at the rail height, extending downward.
            Vector3 ropePos = mid - new Vector3(0f, ropeLength * 0.5f, 0f);
            var woodMat = DecorMaterials.WoodFloor(woodColor);
            var ropeMat = DecorMaterials.Rope(Color.Lerp(woodColor, Color.black, 0.5f));
            site.Town.Out.Solid(ropePos).Box(ropePos, ropeSize, railYaw, ropeMat);
            // No collider for rope – player can walk through.
            // Bracket geometry – small metal L‑shaped support on each post.
            float bracketThickness = 0.04f;
            Vector3 bracketSize = new Vector3(bracketThickness, bracketThickness, 0.02f);
            float bracketOffset = 0.06f; // outward from rail
            Quaternion yawRot = Quaternion.Euler(0f, railYaw, 0f);
            Vector3 outward = yawRot * Vector3.right * bracketOffset;
            // left bracket
            Vector3 leftBracketPos = leftPost + outward - new Vector3(0f, bracketThickness * 0.5f, 0f);
            site.Town.Out.Solid(leftBracketPos).Box(leftBracketPos, bracketSize, railYaw, DecorMaterials.Metal(Color.gray));
            // right bracket
            Vector3 rightBracketPos = rightPost - outward - new Vector3(0f, bracketThickness * 0.5f, 0f);
            site.Town.Out.Solid(rightBracketPos).Box(rightBracketPos, bracketSize, railYaw, DecorMaterials.Metal(Color.gray));
            // Seat geometry – a simple wooden plank.
            Vector3 seatSize = new Vector3(0.60f, 0.08f, 0.30f);
            // Seat sits at the bottom of the rope.
            Vector3 seatPos = mid - new Vector3(0f, ropeLength, 0f);
            var seatMat = woodMat; // seat stays wood‑colored
            site.Town.Out.Solid(seatPos).Box(seatPos, seatSize, railYaw, seatMat);
            site.Town.PropCollider(seatPos, seatSize, railYaw);
            // Back‑rest – taller thin plank attached to the seat.
            Vector3 backSize = new Vector3(0.60f, 0.40f, 0.08f);
            Vector3 backOffset = new Vector3(0f, (seatSize.y + backSize.y) * 0.5f, - (seatSize.z * 0.5f + backSize.z * 0.5f));
            Vector3 backPos = seatPos + backOffset;
            site.Town.Out.Solid(backPos).Box(backPos, backSize, railYaw, seatMat);
            site.Town.PropCollider(backPos, backSize, railYaw);
            // Mark the swing’s footprint as taken so other props (mailbox, etc.) won’t intersect.
            float footprintRadius = Mathf.Max(seatSize.x, seatSize.z, backSize.x, backSize.z) * 0.5f;
            site.Take(new Vector2(seatPos.x, seatPos.z), footprintRadius);
        }
    }
}
