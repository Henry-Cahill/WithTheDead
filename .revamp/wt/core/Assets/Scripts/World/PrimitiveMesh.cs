using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The unit primitives item models are assembled from, generated rather than taken from
    /// <see cref="GameObject.CreatePrimitive"/>.
    ///
    /// This exists for one reason: vertex budget. Unity's built-in sphere is 515 vertices and its
    /// cylinder 88. The item models are built from a great many small primitives — a single dress runs
    /// to three or four hundred — so borrowing the built-ins would bake a garment at well over a
    /// hundred thousand vertices, for an object that is thirty centimetres wide and usually a few
    /// pixels on screen. Generating them means the tessellation can be chosen per level of detail: a
    /// coarse sphere is perfectly convincing at the size these parts are actually drawn.
    ///
    /// Every primitive is built once per level of detail and shared by every part that uses it, since
    /// the baker transforms the vertices as it copies them in.
    /// </summary>
    internal sealed class PrimitiveMesh
    {
        public readonly Vector3[] Verts;
        public readonly Vector3[] Norms;
        public readonly int[] Tris;

        private PrimitiveMesh(Vector3[] verts, Vector3[] norms, int[] tris)
        {
            Verts = verts;
            Norms = norms;
            Tris = tris;
        }

        // Near detail is what the player sees when standing over an item; far detail is for the rest
        // of the visible world, where only the silhouette survives.
        private static PrimitiveMesh s_CubeNear, s_SphereNear, s_CylNear;
        private static PrimitiveMesh s_CubeFar, s_SphereFar, s_CylFar;

        public static PrimitiveMesh For(PartShape shape, ItemMeshBaker.Lod lod)
        {
            bool near = lod == ItemMeshBaker.Lod.Near;

            switch (shape)
            {
                case PartShape.Sphere:
                    if (near) return s_SphereNear ??= Sphere(8, 6);
                    return s_SphereFar ??= Sphere(5, 4);

                case PartShape.Cylinder:
                    if (near) return s_CylNear ??= Cylinder(10);
                    return s_CylFar ??= Cylinder(6);

                default:
                    if (near) return s_CubeNear ??= Cube();
                    return s_CubeFar ??= Cube();
            }
        }

        /// <summary>A unit cube, one metre across, with hard-edged faces (24 vertices).</summary>
        private static PrimitiveMesh Cube()
        {
            Vector3[] faceNormals =
            {
                Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left,
            };

            var verts = new Vector3[24];
            var norms = new Vector3[24];
            var tris = new int[36];

            for (int f = 0; f < 6; f++)
            {
                Vector3 n = faceNormals[f];
                // Two axes across the face, so the quad's corners can be walked in order.
                Vector3 u = new Vector3(n.y, n.z, n.x);
                Vector3 v = Vector3.Cross(n, u);

                int b = f * 4;
                verts[b + 0] = (n - u - v) * 0.5f;
                verts[b + 1] = (n + u - v) * 0.5f;
                verts[b + 2] = (n + u + v) * 0.5f;
                verts[b + 3] = (n - u + v) * 0.5f;
                for (int i = 0; i < 4; i++) norms[b + i] = n;

                int t = f * 6;
                tris[t + 0] = b; tris[t + 1] = b + 1; tris[t + 2] = b + 2;
                tris[t + 3] = b; tris[t + 4] = b + 2; tris[t + 5] = b + 3;
            }

            return new PrimitiveMesh(verts, norms, tris);
        }

        /// <summary>A unit-diameter sphere, so a part's scale reads directly as its diameter on each
        /// axis and the builders can squash it into an ellipsoid.</summary>
        private static PrimitiveMesh Sphere(int segments, int rings)
        {
            int vCount = (segments + 1) * (rings + 1);
            var verts = new Vector3[vCount];
            var norms = new Vector3[vCount];

            for (int y = 0; y <= rings; y++)
            {
                float v = (float)y / rings;
                float pitch = v * Mathf.PI;            // 0 at the top pole, PI at the bottom
                float sy = Mathf.Cos(pitch);
                float r = Mathf.Sin(pitch);

                for (int x = 0; x <= segments; x++)
                {
                    float u = (float)x / segments;
                    float yaw = u * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(yaw) * r, sy, Mathf.Cos(yaw) * r);

                    int i = y * (segments + 1) + x;
                    norms[i] = n;
                    verts[i] = n * 0.5f;               // unit diameter, not unit radius
                }
            }

            var tris = new int[segments * rings * 6];
            int t = 0;
            for (int y = 0; y < rings; y++)
            {
                for (int x = 0; x < segments; x++)
                {
                    int a = y * (segments + 1) + x;
                    int b = a + segments + 1;

                    tris[t++] = a; tris[t++] = b; tris[t++] = a + 1;
                    tris[t++] = a + 1; tris[t++] = b; tris[t++] = b + 1;
                }
            }

            return new PrimitiveMesh(verts, norms, tris);
        }

        /// <summary>A cylinder one unit across and two units tall, matching the built-in Unity cylinder
        /// the model builders were written against — which is why they all halve the height in scale.</summary>
        private static PrimitiveMesh Cylinder(int sides)
        {
            // Side wall gets its own ring of vertices per end so the caps can stay flat-shaded.
            int wall = (sides + 1) * 2;
            int caps = (sides + 1) * 2 + 2;          // a fan per end, plus the two centres
            var verts = new Vector3[wall + caps];
            var norms = new Vector3[wall + caps];

            for (int i = 0; i <= sides; i++)
            {
                float yaw = (float)i / sides * Mathf.PI * 2f;
                float sx = Mathf.Sin(yaw), cz = Mathf.Cos(yaw);
                var outward = new Vector3(sx, 0f, cz);
                var rim = new Vector3(sx * 0.5f, 0f, cz * 0.5f);

                verts[i] = rim + Vector3.up;                 // top rim
                norms[i] = outward;
                verts[sides + 1 + i] = rim + Vector3.down;   // bottom rim
                norms[sides + 1 + i] = outward;

                verts[wall + i] = rim + Vector3.up;          // top cap ring
                norms[wall + i] = Vector3.up;
                verts[wall + sides + 1 + i] = rim + Vector3.down;
                norms[wall + sides + 1 + i] = Vector3.down;
            }

            int topCentre = wall + (sides + 1) * 2;
            int botCentre = topCentre + 1;
            verts[topCentre] = Vector3.up; norms[topCentre] = Vector3.up;
            verts[botCentre] = Vector3.down; norms[botCentre] = Vector3.down;

            var tris = new int[sides * 12];
            int t = 0;
            for (int i = 0; i < sides; i++)
            {
                int tA = i, tB = i + 1;
                int bA = sides + 1 + i, bB = sides + 2 + i;

                // Wall.
                tris[t++] = tA; tris[t++] = bA; tris[t++] = tB;
                tris[t++] = tB; tris[t++] = bA; tris[t++] = bB;

                // Caps.
                tris[t++] = topCentre; tris[t++] = wall + i; tris[t++] = wall + i + 1;
                tris[t++] = botCentre; tris[t++] = wall + sides + 2 + i; tris[t++] = wall + sides + 1 + i;
            }

            return new PrimitiveMesh(verts, norms, tris);
        }
    }
}
