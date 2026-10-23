using UnityEngine;
namespace CloneZ
{
    internal static class MailboxBuilder
    {
        // size constants (metres)
        const float BodyWidth = 0.25f;
        const float BodyHeight = 0.40f;
        const float BodyDepth = 0.15f;
        const float PoleRadius = 0.025f;
        const float PoleHeight = 0.60f;
        const float FrontOffset = 1.0f; // distance from building centre to mailbox

        public static void Build(KentuckyMap.Site site, KentuckyMap.Footprint fp, MapBuild build)
        {
            // skip if the deterministic helper says no mailbox
            if (!build.HasMailbox(fp.Data.id)) return;

            // compute a simple front‑position: take the footprint centre,
            // move forward along the building's forward (negative Z) axis.
            // This is a coarse approximation that works for the mostly‑rectangular homes.
            Vector3 centre = new Vector3(fp.Middle.x, 0f, fp.Middle.y);
            Vector3 forward = Vector3.back; // assume front faces -Z; most OSM houses are oriented this way
            Vector3 pos = centre + forward * FrontOffset;

            // ensure we have free space (radius covers body + pole)
            if (!site.Free(new Vector2(pos.x, pos.z), BodyWidth)) return;
            site.Take(new Vector2(pos.x, pos.z), BodyWidth);

            // set material – reuse porch material (plain wood) for the pole,
            // and the building's trim colour for the box
            Material bodyMat = new Material(WorldStore.GetPorchMaterial()); // clone to allow colour tint
            bodyMat.color = fp.Data.GetTrimColour();
            Material poleMat = WorldStore.GetPorchMaterial();

            var mesh = site.Overhead.At(pos);
            // align the mesh frame with the mailbox position (no rotation needed)
            mesh.SetFrame(pos, 0f);
            // Build the mailbox body as a box
            var bodySurf = new Surf(bodyMat, Vector2.zero, 0f);
            mesh.Box(Vector3.zero,
                new Vector3(BodyWidth, BodyHeight, BodyDepth), bodySurf);

            var polePos = new Vector3(0f, PoleHeight * 0.5f, -BodyDepth * 0.5f - PoleRadius);
            var poleSurf = new Surf(poleMat, Vector2.zero, 0f);
            mesh.Box(polePos,
                new Vector3(PoleRadius * 2f, PoleHeight, PoleRadius * 2f), poleSurf);
        }
    }
}
