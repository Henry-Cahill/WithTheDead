using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// The cordon (Roadside Picnic's illegal entry): an inner fence just inside the restricted belt along the map
    /// edge, guard towers with sweeping searchlights, and a checkpoint where the Warden on duty can be bribed or shown
    /// a permit. Inside the belt without a pass, the Wardens shoot. Getting in and out past the cordon is part of
    /// the run. Roads and exits leave gaps in the wire.
    /// </summary>
    internal static class ZoneCordon
    {
        private const float SegLen = 3f;
        private static Vector3 s_GatePos;
        private static bool s_HasGate;

        public static bool HasGate => s_HasGate;

        public static void Build(Transform world, RaidMapInfo map, ZoneDef zone, ZoneSiteMap site, System.Random rng)
        {
            s_HasGate = false;
            float d = site.Half - CordonRules.BeltWidth;
            var fence = new SiteBuilder(world, "CordonFence", Vector3.zero, 0f, rng, cutaway: false);
            var gaps = new List<Vector3>();

            // Four sides, a segment at a time; a road, an exit, a building or a wreck leaves a gap.
            for (int side = 0; side < 4; side++)
            {
                Vector3 axis = side % 2 == 0 ? Vector3.right : Vector3.forward;
                Vector3 normal = side == 0 ? Vector3.forward : side == 1 ? Vector3.right : side == 2 ? Vector3.back : Vector3.left;
                Vector3 runStart = Vector3.zero;
                int runLen = 0;
                for (float t = -d; t <= d; t += SegLen)
                {
                    Vector3 c = normal * d + axis * t;
                    bool gap = site.Any(c, 1.6f, SiteCell.Paved | SiteCell.Reserved | SiteCell.Building | SiteCell.Solid | SiteCell.Taken);
                    if (gap)
                    {
                        if (site.Any(c, 1.6f, SiteCell.Paved)) gaps.Add(c);
                        FlushRun(fence, runStart, axis, runLen);
                        runLen = 0;
                        continue;
                    }
                    if (runLen == 0) runStart = c;
                    runLen++;
                    Vector3 post = c - axis * (SegLen * 0.5f);
                    fence.Solid.Box(post + Vector3.up * 1.25f, new Vector3(0.09f, 2.5f, 0.09f), ZoneKit.Mt(ZoneKit.Galvanised));
                    // 12% chance per post to show a rusted metal plate on its outer face
                    if (ZoneKit.R(rng, 0f, 1f) < 0.12f)
                    {
                        // size 0.08 m × 0.02 m × 0.001 m, positioned 0.9 m above ground,
                        // offset outward by half the post width (0.045 m) so it sits on the front surface
                        fence.Solid.Box(
                            post + Vector3.up * 0.9f + normal * 0.045f,
                            new Vector3(side % 2 == 0 ? 0.08f : 0.001f, 0.02f, side % 2 == 0 ? 0.001f : 0.08f),
                            ZoneKit.Mt(ZoneKit.RustDark));
                    }
                    fence.Upper.Box(c + Vector3.up * 1.2f, side % 2 == 0 ? new Vector3(SegLen, 2.2f, 0.02f) : new Vector3(0.02f, 2.2f, SegLen), ZoneKit.ChainLink);
                    fence.Upper.Rod(post + Vector3.up * 2.45f, post + axis * SegLen + Vector3.up * 2.45f, 0.025f, ZoneKit.Mt(ZoneKit.Galvanised), 4);
                    fence.Upper.Rod(post + Vector3.up * 2.6f, post + axis * SegLen + Vector3.up * 2.6f, 0.015f, ZoneKit.Mt(ZoneKit.RustDark), 3);
                    site.Mark(ZoneSiteMap.Footprint(c, new Vector2(side % 2 == 0 ? SegLen : 0.6f, side % 2 == 0 ? 0.6f : SegLen), 0f), SiteCell.Taken);
                }
                FlushRun(fence, runStart, axis, runLen);
            }
            fence.Finish();

            // Guard towers at the corners of the belt.
            float tc = site.Half - CordonRules.BeltWidth * 0.5f;
            Vector3[] corners = { new Vector3(tc, 0f, tc), new Vector3(-tc, 0f, tc), new Vector3(tc, 0f, -tc), new Vector3(-tc, 0f, -tc) };
            foreach (Vector3 c in corners)
            {
                if (site.Any(c, 3f, SiteCell.Building | SiteCell.Reserved)) continue;
                BuildTower(world, c, rng);
                site.MarkCircle(c, 3.5f, SiteCell.Taken);
                Vector3 inward = -c.normalized;
                WorldMarkers.Add(WorldMarkers.CordonTower, c + inward * 3.5f);
            }

            // The checkpoint: at the road gap nearest the middle of a side.
            if (gaps.Count > 0)
            {
                Vector3 best = gaps[0];
                float bestScore = float.MaxValue;
                foreach (Vector3 g in gaps)
                {
                    float s = Mathf.Min(Mathf.Abs(g.x), Mathf.Abs(g.z));
                    if (s < bestScore) { bestScore = s; best = g; }
                }
                BuildCheckpoint(world, best, rng);
            }
        }

        // One collider per straight run of wire, rather than one per panel.
        private static void FlushRun(SiteBuilder fence, Vector3 start, Vector3 axis, int count)
        {
            if (count <= 0) return;
            Vector3 a = start - axis * (SegLen * 0.5f);
            Vector3 b = start + axis * (SegLen * (count - 0.5f));
            Vector3 mid = (a + b) * 0.5f;
            float len = Vector3.Distance(a, b);
            fence.Collider(mid + Vector3.up * 1.25f, axis == Vector3.right ? new Vector3(len, 2.5f, 0.12f) : new Vector3(0.12f, 2.5f, len));
        }

        private static void BuildTower(Transform world, Vector3 at, System.Random rng)
        {
            var sb = new SiteBuilder(world, "GuardTower", at, 0f, rng, cutaway: false);
            const float h = 5.5f;
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * 1.4f, z = (i < 2 ? -1f : 1f) * 1.4f;
                Vector3 post = new Vector3(x, h * 0.5f, z);
                Vector3 normal = new Vector3(x, 0f, z).normalized;
                sb.Solid.Box(post, new Vector3(0.22f, h, 0.22f), ZoneKit.M(ZoneKit.WetWood));
                // 15% chance per post to show a rusted metal plate on its outer face
                if (ZoneKit.R(rng, 0f, 1f) < 0.15f)
                {
                    sb.Solid.Box(post + Vector3.up * 1.0f + normal * 0.11f, new Vector3(0.08f, 0.02f, 0.001f), ZoneKit.Mt(ZoneKit.RustDark));
                }
                sb.Collider(post, new Vector3(0.22f, h, 0.22f));
            }
            sb.Upper.Box(new Vector3(0f, h, 0f), new Vector3(3.4f, 0.2f, 3.4f), ZoneKit.M(ZoneKit.WetWood));
            sb.Upper.Box(new Vector3(0f, h + 0.55f, 1.65f), new Vector3(3.4f, 0.9f, 0.1f), ZoneKit.M(ZoneKit.WardenOlive));
            sb.Upper.Box(new Vector3(0f, h + 0.55f, -1.65f), new Vector3(3.4f, 0.9f, 0.1f), ZoneKit.M(ZoneKit.WardenOlive));
            sb.Upper.Box(new Vector3(1.65f, h + 0.55f, 0f), new Vector3(0.1f, 0.9f, 3.4f), ZoneKit.M(ZoneKit.WardenOlive));
            sb.Upper.Box(new Vector3(-1.65f, h + 0.55f, 0f), new Vector3(0.1f, 0.9f, 3.4f), ZoneKit.M(ZoneKit.WardenOlive));
            sb.Roof.Box(new Vector3(0f, h + 2.3f, 0f), new Vector3(3.8f, 0.15f, 3.8f), ZoneKit.Mt(ZoneKit.RustDark));
            sb.Solid.Rod(new Vector3(1.4f, 0f, -1.6f), new Vector3(1.4f, h, -1.5f), 0.05f, ZoneKit.M(ZoneKit.WetWood), 4);
            sb.Finish();

            var lightRoot = ZoneKit.Child(ZoneWorld.Unbaked != null ? ZoneWorld.Unbaked : world, "Searchlight", at + Vector3.up * (h + 1.4f));
            var sweep = lightRoot.gameObject.AddComponent<SearchlightSweep>();
            sweep.Speed = ZoneKit.R(rng, 14f, 24f) * (rng.NextDouble() < 0.5 ? 1f : -1f);
            var spot = lightRoot.gameObject.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 22f;
            spot.range = 45f;
            spot.intensity = 3.5f;
            spot.color = new Color(1f, 0.96f, 0.85f);
            spot.shadows = LightShadows.None;
            ZoneKit.Beam(lightRoot, 30f, 22f, new Color(1f, 0.96f, 0.85f, 0.12f));
        }

        private static void BuildCheckpoint(Transform world, Vector3 at, System.Random rng)
        {
            Vector3 inward = Mathf.Abs(at.x) > Mathf.Abs(at.z) ? new Vector3(-Mathf.Sign(at.x), 0f, 0f) : new Vector3(0f, 0f, -Mathf.Sign(at.z));
            float yaw = Quaternion.LookRotation(inward).eulerAngles.y;
            Vector3 boothAt = at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(4.5f, 0f, 1.5f);
            var sb = new SiteBuilder(world, "Checkpoint", boothAt, yaw, rng);
            sb.Solid.Box(new Vector3(0f, 1.2f, 0f), new Vector3(2.4f, 2.4f, 2.4f), ZoneKit.M(ZoneKit.WardenOlive));
            // Occasionally add a weathered flag on a short pole for visual variety.
            if (ZoneKit.R(rng, 0f, 1f) < 0.6f)
            {
                // Short wooden pole.
                sb.Solid.Rod(new Vector3(0f, 0f, -0.6f), new Vector3(0f, 1f, -0.6f), 0.025f, ZoneKit.M(ZoneKit.WetWood), 4);
                // Faded orange cloth flag.
                sb.Solid.Box(new Vector3(0f, 1.02f, -0.6f), new Vector3(0.4f, 0.25f, 0.02f), ZoneKit.M(new Color(0.78f, 0.25f, 0.2f)));
            }
            sb.Solid.Box(new Vector3(0f, 1.6f, -1.21f), new Vector3(1.6f, 0.7f, 0.02f), ZoneKit.Gs(new Color(0.6f, 0.7f, 0.75f)));
            sb.Roof.Box(new Vector3(0f, 2.5f, 0f), new Vector3(2.8f, 0.15f, 2.8f), ZoneKit.Mt(ZoneKit.RustDark));
            sb.Collider(new Vector3(0f, 1.2f, 0f), new Vector3(2.4f, 2.4f, 2.4f));
            // Sandbags and the striped boom across the road.
            for (int i = 0; i < 5; i++) sb.Solid.Box(new Vector3(-1.8f + i * 0.9f, 0.3f, -2.2f), new Vector3(0.85f, 0.6f, 0.5f), ZoneKit.M(new Color(0.48f, 0.43f, 0.33f)));
            sb.Collider(new Vector3(0f, 0.3f, -2.2f), new Vector3(4.5f, 0.6f, 0.5f));
            sb.Upper.Box(new Vector3(-4.5f, 1.05f, -1.5f), new Vector3(6f, 0.12f, 0.12f), DecorMaterials.Hazard);
            Surf sign = DecorMaterials.Sign("CORDON CHECKPOINT", new Color(0.85f, 0.85f, 0.80f), new Color(0.6f, 0.1f, 0.08f), out float aspect);
            sb.Flat.Upright(new Vector3(0f, 2.05f, -1.23f), Vector3.back, 0.32f * aspect, 0.32f, sign);
            sb.Finish();

            var gate = sb.Root.gameObject.AddComponent<CordonGate>();
            gate.Point = sb.W(0f, 1.3f, -1.4f);
            ZoneFlameLight.Spawn(sb.Root, sb.W(0f, 2.3f, -1.6f), new Color(1f, 0.85f, 0.6f), 10f, 1.1f, 0.02f);
            WorldMarkers.Add(WorldMarkers.CordonGate, sb.W(0f, 0f, -2.8f));
            WorldMarkers.Add(WorldMarkers.Landmark("warden_checkpoint"), boothAt);
            s_GatePos = boothAt;
            s_HasGate = true;
        }

        /// <summary>Warning signs and coils of wire along the belt.</summary>
        public static void DressBelt(Transform world, ZoneDef zone, ZoneSiteMap site, System.Random rng)
        {
            var dress = new SiteBuilder(world, "CordonDressing", Vector3.zero, 0f, rng, cutaway: false);
            float d = site.Half - CordonRules.BeltWidth + 1.2f;
            Surf sign = DecorMaterials.Sign("RESTRICTED ZONE - LETHAL FORCE", new Color(0.9f, 0.8f, 0.2f), new Color(0.1f, 0.1f, 0.1f), out float aspect);
            for (int i = 0; i < 16; i++)
            {
                int side = i % 4;
                Vector3 normal = side == 0 ? Vector3.forward : side == 1 ? Vector3.right : side == 2 ? Vector3.back : Vector3.left;
                Vector3 axis = side % 2 == 0 ? Vector3.right : Vector3.forward;
                Vector3 p = normal * d + axis * ZoneKit.R(rng, -d + 6f, d - 6f);
                if (site.Any(p, 1f, SiteCell.Paved | SiteCell.Building | SiteCell.Reserved)) continue;
                dress.Solid.Box(p + Vector3.up * 0.8f, new Vector3(0.06f, 1.6f, 0.06f), ZoneKit.Mt(ZoneKit.Galvanised));
                dress.Flat.Upright(p + Vector3.up * 1.45f - normal * 0.04f, -normal, 0.22f * aspect, 0.22f, sign);
            }
            for (int i = 0; i < 30; i++)
            {
                int side = i % 4;
                Vector3 normal = side == 0 ? Vector3.forward : side == 1 ? Vector3.right : side == 2 ? Vector3.back : Vector3.left;
                Vector3 axis = side % 2 == 0 ? Vector3.right : Vector3.forward;
                Vector3 p = normal * (d + 2f) + axis * ZoneKit.R(rng, -d, d);
                if (site.Any(p, 1.2f, SiteCell.Paved | SiteCell.Building | SiteCell.Reserved)) continue;
                dress.Upper.CylinderX(p + Vector3.up * 0.4f, 0.4f, 2.5f, 8, ZoneKit.ChainLink, side % 2 == 0 ? 0f : 90f);
            }
            dress.Finish();
        }
    }

    /// <summary>The Warden on duty at the checkpoint: a bribe or a permit buys passage through the belt for this raid.</summary>
    public class CordonGate : Interactable
    {
        public Vector3 Point;
        public const int Bribe = 400;

        public override string DisplayName => CordonRules.PassBought ? "Checkpoint (passage paid)" : "Warden at the checkpoint";
        public override Vector3 InteractPoint => Point;
        public override float Reach => 2.8f;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool paid = CordonRules.PassBought;
            int carried = ctx.Inventory != null ? Money.Count(ctx.Inventory.Inventory) : 0;
            into.Add(new InteractAction("Bribe the Warden (" + Money.Format(Bribe) + ")", c =>
            {
                if (!Money.TrySpend(Bribe, c.Inventory)) { c.Notify("Not enough money on you."); return; }
                Grant(c, "He pockets it without looking at you. \"Didn't see you.\"");
                Factions.AdjustReputation(Factions.Wardens, 5f);
            }).DisabledIf(paid, "Already paid").DisabledIf(!paid && carried < Bribe, "You need " + Money.Format(Bribe) + " on you"));
            bool permit = ctx.Inventory != null && ctx.Inventory.Has("zone_permit");
            into.Add(new InteractAction("Show a Zone Permit", c =>
            {
                if (c.Inventory.Consume("zone_permit", 1) <= 0) return;
                Grant(c, "He stamps it and waves you through.");
                Factions.AdjustReputation(Factions.Wardens, 2f);
            }).DisabledIf(paid, "Already paid").DisabledIf(!paid && !permit, "You have no permit"));
            into.Add(new InteractAction("Ask about the Zone", c =>
                GameEvents.RaiseSubtitle("Warden", CordonRules.PassBought ? "Keep to the road. Stay off the belt after dark." : "No pass, no passage. Off the belt, or we shoot.")));
        }

        private static void Grant(InteractContext c, string line)
        {
            CordonRules.PassBought = true;
            ZoneWorldModule.NotePassBought();
            GameEvents.RaiseSubtitle("Warden", line);
            c.Notify("Passage through the cordon bought for this raid.");
            Sfx.PlayUI(SfxIds.Cash);
        }
    }

    /// <summary>A tower searchlight sweeping back and forth.</summary>
    public sealed class SearchlightSweep : MonoBehaviour
    {
        public float Speed = 18f;
        private float _yaw;

        private void Start() => _yaw = Random.value * 360f;

        private void Update()
        {
            _yaw += Speed * Time.deltaTime;
            float pitch = 28f + Mathf.Sin(Time.time * 0.37f) * 8f;
            transform.rotation = Quaternion.Euler(pitch, _yaw, 0f);
        }
    }
}
