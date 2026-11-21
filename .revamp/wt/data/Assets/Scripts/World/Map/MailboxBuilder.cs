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
            Vector3 forward;
            if (fp.Pts.Count >= 2) {
                // edge from first to second point (counter‑clockwise order)
                Vector2 edge = fp.Pts[1] - fp.Pts[0];
                // rotate 90° clockwise to get outward normal (front)
                forward = new Vector3(-edge.y, 0f, edge.x).normalized;
            } else {
                forward = Vector3.back;
            }
            Vector3 pos = centre + forward * FrontOffset;



            // Painted colours from the shared cache: the store's porch wood isn't among this project's Resources (cloning
            // its null threw and took the whole map build down), and a material per mailbox would never be freed as a
            // streamed map lets its pieces go.
            Color trim = fp.Data.GetTrimColour();
            trim.a = 1f;
            Material bodyMat = CloneZUtil.Mat(trim);
            Material wood = WorldStore.GetPorchMaterial();
            Material poleMat = wood != null ? wood : CloneZUtil.Mat(new Color(0.45f, 0.33f, 0.22f));

            var mesh = site.Overhead.At(pos);
            // align the mesh frame with the mailbox position (no rotation needed)
            // after taking space, add random yaw and flag colour
            float yaw = (float)(build.Random.NextDouble() * 20.0 - 10.0); // ±10°
            float baseYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            float finalYaw = baseYaw + yaw;
            Color[] flagColours = new[] { Color.red, new Color(0.9f, 0.6f, 0.2f), Color.white };
            Color flagCol = flagColours[build.Random.Next(flagColours.Length)];
            mesh.SetFrame(pos, finalYaw);
            // create a temporary GameObject for collider and interaction
            var root = new GameObject("Mailbox");
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, finalYaw, 0f);
            // parent under the world root (Transform) for proper collider registration
            root.transform.SetParent(site.Town.World, false);
            var coll = root.AddComponent<BoxCollider>();
            coll.center = new Vector3(0f, BodyHeight * 0.5f, -BodyDepth * 0.5f);
            coll.size = new Vector3(BodyWidth, BodyHeight, BodyDepth + PoleRadius * 2f);
            // geometry will be added to site.Overhead later
            // Build the mailbox body as a box
            var bodySurf = new Surf(bodyMat, Vector2.zero, 0f);
            mesh.Box(Vector3.zero,
                new Vector3(BodyWidth, BodyHeight, BodyDepth), bodySurf);

            // door flap (thin box)
            Vector3 flapPos = new Vector3(0f, BodyHeight * 0.5f, BodyDepth * 0.5f + 0.01f);
            Color flap = trim * 0.85f; // slightly darker tint
            flap.a = 1f;
            Material flapMat = CloneZUtil.Mat(flap);
            var flapSurf = new Surf(flapMat, Vector2.zero, 0f);
            mesh.Box(flapPos,
                new Vector3(BodyWidth * 0.6f, BodyHeight * 0.7f, 0.02f), flapSurf);

            var polePos = new Vector3(0f, PoleHeight * 0.5f, -BodyDepth * 0.5f - PoleRadius);
            var poleSurf = new Surf(poleMat, Vector2.zero, 0f);
            mesh.Box(polePos,
                new Vector3(PoleRadius * 2f, PoleHeight, PoleRadius * 2f), poleSurf);

            // flag on pole
            Vector3 flagPos = polePos + new Vector3(0f, PoleHeight * 0.9f, 0f);
            Material flagMat = CloneZUtil.Mat(flagCol);
            var flagSurf = new Surf(flagMat, Vector2.zero, 0f);
            mesh.Box(flagPos,
                new Vector3(0.02f, 0.12f, 0.02f), flagSurf);
        }
    }
}
