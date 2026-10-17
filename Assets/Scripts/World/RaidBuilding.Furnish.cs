using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    internal sealed partial class RaidBuilding
    {
        /// <summary>A placement in the building's local frame: centre (x, z) and yaw (0 faces +Z).</summary>
        public struct Place
        {
            public float X, Z, Yaw;
            public Place(float x, float z, float yaw) { X = x; Z = z; Yaw = yaw; }
        }

        /// <summary>Overgrown rooms get heavier vines (the reference photo's army room).</summary>
        public readonly HashSet<RaidRoom> Overgrown = new HashSet<RaidRoom>();

        private static readonly Color DeskLampColor = new Color(1f, 0.72f, 0.42f);
        private static readonly Color CeilingLightColor = new Color(1f, 0.82f, 0.55f);

        #region Placement helpers (local)
        /// <summary>Against the room's local north wall (front facing -Z), centred at x.</summary>
        public Place N(RaidRoom r, float x, float depth) => new Place(x, r.Local.yMax - Half - depth * 0.5f - 0.02f, 180f);
        /// <summary>Against the room's local south wall (front facing +Z).</summary>
        public Place S(RaidRoom r, float x, float depth) => new Place(x, r.Local.yMin + Half + depth * 0.5f + 0.02f, 0f);
        /// <summary>Against the room's local west wall (front facing +X).</summary>
        public Place Wst(RaidRoom r, float z, float depth) => new Place(r.Local.xMin + Half + depth * 0.5f + 0.02f, z, 90f);
        /// <summary>Against the room's local east wall (front facing -X).</summary>
        public Place E(RaidRoom r, float z, float depth) => new Place(r.Local.xMax - Half - depth * 0.5f - 0.02f, z, 270f);

        /// <summary>World XZ rect of a footprint (width along the piece's X, depth along its Z).</summary>
        private static Rect FootRect(Vector3 pos, float yaw, Vector3 size)
        {
            Vector3 a = AabbSize(size, yaw);
            return new Rect(pos.x - a.x * 0.5f, pos.z - a.z * 0.5f, a.x, a.z);
        }

        private static Vector3 AabbSize(Vector3 size, float yaw)
        {
            float rad = yaw * Mathf.Deg2Rad;
            float c = Mathf.Abs(Mathf.Cos(rad)), s = Mathf.Abs(Mathf.Sin(rad));
            return new Vector3(size.x * c + size.z * s, size.y, size.x * s + size.z * c);
        }

        private void AddCollider(Vector3 center, Vector3 size, float yaw)
        {
            if (_colliders == null)
            {
                _colliders = new GameObject("Colliders");
                _colliders.transform.SetParent(Root, false);
            }
            float k = Mathf.Repeat(yaw, 90f);
            if (k < 1f || k > 89f)
            {
                var bc = _colliders.AddComponent<BoxCollider>();
                bc.center = center;
                bc.size = AabbSize(size, yaw);
            }
            else
            {
                var go = new GameObject("Collider");
                go.transform.SetParent(_colliders.transform, false);
                go.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
                go.AddComponent<BoxCollider>().size = size;
            }
        }

        private void CheckClear(RaidRoom room, Rect fp, string what)
        {
            Rect shrunk = new Rect(fp.x + 0.05f, fp.y + 0.05f, fp.width - 0.1f, fp.height - 0.1f);
            if (room.Overlaps(shrunk, true)) Town.Warn(Name + "/" + room.Name + ": " + what + " overlaps furniture or a doorway");
            Rect inner = room.Inner;
            if (fp.xMin < inner.xMin - 0.05f || fp.xMax > inner.xMax + 0.05f || fp.yMin < inner.yMin - 0.05f || fp.yMax > inner.yMax + 0.05f)
                Town.Warn(Name + "/" + room.Name + ": " + what + " pokes through a wall");
        }
        #endregion

        #region Furniture
        /// <summary>Solid pieces at least this tall get their own renderer and collider, so the iso
        /// cutaway can lift them out of the way when they hide the survivor.</summary>
        private const float TallPiece = 1.35f;

        /// <summary>Static furniture drawn by <paramref name="draw"/> at a local placement. Solid pieces
        /// get a box collider of <paramref name="size"/>; the footprint is marked taken either way.</summary>
        public void Furnish(RaidRoom room, Place p, Vector3 size, bool solid, System.Action<RaidMesh, System.Random> draw, string what = "furniture")
        {
            Vector3 pos = W(p.X, 0f, p.Z);
            float yaw = Yaw(p.Yaw);
            if (solid && size.y >= TallPiece) TallFurniture(pos, yaw, size, draw, what);
            else
            {
                Static.SetFrame(pos, yaw);
                draw(Static, Rng);
                Static.ResetFrame();
                if (solid) AddCollider(pos + Vector3.up * (size.y * 0.5f), size, yaw);
            }
            Rect fp = FootRect(pos, yaw, size);
            if (room != null)
            {
                CheckClear(room, fp, what);
                room.Furniture.Add(fp);
            }
        }

        // A tall piece (shelving, bookcase, cooler) as its own object: the collider on the rotated root and
        // the mesh on a child, which is what the iso cutaway's fallback hides when the piece blocks the view.
        private void TallFurniture(Vector3 pos, float yaw, Vector3 size, System.Action<RaidMesh, System.Random> draw, string what)
        {
            var go = new GameObject(what);
            go.transform.SetParent(Root, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, size.y * 0.5f, 0f);
            bc.size = size;

            var mesh = new RaidMesh();
            mesh.SetFrame(pos, yaw);
            draw(mesh, Rng);
            var vis = new GameObject("Mesh");
            vis.transform.SetParent(go.transform, false);
            vis.transform.SetPositionAndRotation(pos, Quaternion.identity);
            mesh.ApplyTo(vis, pos);
        }

        /// <summary>A searchable container dressed as furniture: the loot container keeps its own collider
        /// (resized to <paramref name="size"/>) and renders the furniture mesh instead of a plain box.</summary>
        public LootContainer Container(RaidRoom room, ContainerType type, string label, Place p, Vector3 size, System.Action<RaidMesh, System.Random> draw)
        {
            Vector3 pos = W(p.X, 0f, p.Z);
            float yaw = Yaw(p.Yaw);
            LootContainer c = LootContainer.Spawn(Root, type, pos, Town.Rng.Next());
            GameObject go = c.gameObject;
            go.transform.SetPositionAndRotation(pos, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            if (!string.IsNullOrEmpty(label)) c.DisplayName = label;
            var box = go.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.center = new Vector3(0f, size.y * 0.5f, 0f);
                box.size = AabbSize(size, yaw);
            }
            var mesh = new RaidMesh();
            mesh.SetFrame(pos, yaw);
            draw(mesh, Rng);
            mesh.ApplyTo(go, pos);
            Rect fp = FootRect(pos, yaw, size);
            if (room != null)
            {
                CheckClear(room, fp, label ?? type.ToString());
                room.Furniture.Add(fp);
            }
            Town.ContainerCount++;
            return c;
        }

        /// <summary>Marks a local rect as taken (e.g. the chair space in front of a desk).</summary>
        public void Block(RaidRoom room, float x0, float z0, float x1, float z1) => room.Furniture.Add(RectW(x0, z0, x1, z1));

        /// <summary>A rug lying on the floor at a local placement.</summary>
        public void RugAt(float lx, float lz, float w, float d, float lyaw, int variant)
            => FurnitureBuilder.Rug(Flat, Rng, W(lx, 0f, lz), w, d, Yaw(lyaw), variant);

        /// <summary>Offers a local point of a room as a possible survivor start (facing local yaw).</summary>
        public void SpawnPoint(RaidRoom room, float lx, float lz, float lyaw)
            => Town.SpawnCandidates.Add((W(lx, 0f, lz), Mathf.Repeat(Yaw(lyaw), 360f), this, room));

        /// <summary>A local-frame point of a mesh's current frame in world space.</summary>
        public static Vector3 WorldOf(RaidMesh m, Vector3 local) => m.ToMeshSpace(local);
        #endregion

        #region Lights
        /// <summary>A ceiling fitting in the style of the room at local (x, z); a lit room also gets the
        /// warm point light under it. <paramref name="height"/> overrides the ceiling height (tent ridge).</summary>
        public void CeilingLight(RaidRoom room, float lx, float lz, float lyaw = 0f, float height = -1f)
        {
            Vector3 pos = W(lx, height > 0f ? height : WallH, lz);
            float drop;
            switch (room.Light)
            {
                case LightStyle.Dome: FurnitureBuilder.CeilingDome(Ceil, pos, room.Lit); drop = 0.16f; break;
                case LightStyle.Bulb: FurnitureBuilder.HangingBulb(Ceil, pos, 0.35f, room.Lit); drop = 0.43f; break;
                case LightStyle.ShopLamp: FurnitureBuilder.ShopLamp(Ceil, pos, 0.45f, room.Lit); drop = 0.58f; break;
                default: FurnitureBuilder.CeilingPanel(Ceil, pos, Yaw(lyaw), room.Lit); drop = 0.16f; break;
            }
            if (!room.Lit) return;
            float size = Mathf.Max(room.World.width, room.World.height);
            float range = Mathf.Clamp(size * 0.95f, 6f, 9f);
            float intensity = 2.0f + (float)Rng.NextDouble() * 1.0f;
            // The fitting's own glowing bulb sits at the light, just under the panel / dome / shade.
            Town.Light(Root, pos - Vector3.up * drop, CeilingLightColor, range, intensity, room.Flicker, true);
        }

        /// <summary>A desk / table lamp's light at a world bulb position.</summary>
        public void LampLight(Vector3 bulb, bool shadows)
            => Town.Light(Root, bulb, DeskLampColor, 3.6f, 1.7f, Rng.NextDouble() < 0.12, shadows);
        #endregion

        #region Walls: decor, vines and grime
        /// <summary>The face point of a room wall at local coordinate <paramref name="t"/> along it and
        /// height <paramref name="y"/>; <paramref name="normal"/> points into the room.</summary>
        public Vector3 WallFace(RaidRoom room, char side, float t, float y, out Vector3 normal)
        {
            Rect l = room.Local;
            const float off = Half + 0.004f;
            switch (side)
            {
                case 'N': normal = Dir(0f, -1f); return W(t, y, l.yMax - off);
                case 'S': normal = Dir(0f, 1f); return W(t, y, l.yMin + off);
                case 'W': normal = Dir(1f, 0f); return W(l.xMin + off, y, t);
                default: normal = Dir(-1f, 0f); return W(l.xMax - off, y, t);
            }
        }

        /// <summary>The mesh of the wall segment at a world point (so wall-hung things vanish with it
        /// when the camera cuts it away); falls back to the building's furniture mesh.</summary>
        public RaidMesh WallMeshAt(Vector3 p)
        {
            foreach (WallPiece w in _pieces)
            {
                WallRun r = w.Run;
                float across = r.AlongX ? p.z : p.x;
                float along = r.AlongX ? p.x : p.z;
                if (Mathf.Abs(across - r.Coord) < 0.2f && along >= w.S - 0.01f && along <= w.E + 0.01f) return w.Mesh;
            }
            return Static;
        }

        /// <summary>A framed poster on a room wall (local coordinate along the wall).</summary>
        public void Poster(RaidRoom room, char side, float t, float y, float w, float h, int kind)
        {
            Vector3 at = WallFace(room, side, t, y, out Vector3 n);
            FurnitureBuilder.FramedPoster(WallMeshAt(at), Rng, at, n, w, h, kind);
        }

        /// <summary>A wall clock on a room wall.</summary>
        public void Clock(RaidRoom room, char side, float t, float y)
        {
            Vector3 at = WallFace(room, side, t, y, out Vector3 n);
            FurnitureBuilder.WallClock(WallMeshAt(at), Rng, at, n);
        }

        /// <summary>Wall cupboards above a counter on a room wall (local coordinate of their centre).</summary>
        public void UpperCabinets(RaidRoom room, char side, float t, float len, Color cabinet)
        {
            Vector3 at = WallFace(room, side, t, 0f, out Vector3 n);
            RaidMesh m = WallMeshAt(at + Vector3.up * 1.8f);
            m.SetFrame(at, Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg);
            FurnitureBuilder.UpperCabinets(m, Rng, len, cabinet);
            m.ResetFrame();
        }

        /// <summary>A painted sign board over the outside of a doorway at local (x, z) on a wall line.</summary>
        public void DoorSign(float lx, float lz, string text, Color bg, Color fg)
        {
            Vector3 p = W(lx, 0f, lz);
            foreach (Opening o in _openings)
            {
                if (o.Kind != OpeningKind.Door) continue;
                Vector3 c = RunPoint(o.Run, o.Pos, 0f, 0f);
                if ((c - p).sqrMagnitude > 1f) continue;
                int outside = OutsideSide(o.Run, o.Pos);
                if (outside == 0) return;
                Surf s = DecorMaterials.Sign(text, bg, fg, out float aspect);
                float h = 0.42f, w = Mathf.Min(h * aspect, 4.2f);
                h = w / aspect;
                Vector3 n = RunNormal(o.Run, outside);
                Vector3 at = RunPoint(o.Run, o.Pos, 2.5f, outside * (Half + 0.03f));
                RaidMesh m = WallMeshAt(at);
                Quaternion q = Quaternion.LookRotation(n);
                m.Box(at - n * 0.012f, new Vector3(w + 0.08f, h + 0.08f, 0.03f), q, DecorPalette.Matte(new Color(0.16f, 0.15f, 0.14f)));
                m.Upright(at + n * 0.005f, n, w, h, s, new Rect(0f, 0f, 1f, 1f));
                m.Box(at + new Vector3(0f, h * 0.5f + 0.06f, 0f) + n * 0.01f, new Vector3(w * 0.9f, 0.04f, 0.06f), q, DecorPalette.Matte(DecorProps.SnowWhite));
                return;
            }
            Town.Warn(Name + ": no doorway for sign '" + text + "'");
        }

        /// <summary>A rolled-up shutter drum over the inside of the doorway at local (x, z).</summary>
        public void RollUpDoor(float lx, float lz)
        {
            Vector3 p = W(lx, 0f, lz);
            foreach (Opening o in _openings)
            {
                if (o.Kind != OpeningKind.Door) continue;
                Vector3 c = RunPoint(o.Run, o.Pos, 0f, 0f);
                if ((c - p).sqrMagnitude > 1f) continue;
                int outside = OutsideSide(o.Run, o.Pos);
                int inside = outside == 0 ? 1 : -outside;
                Vector3 at = RunPoint(o.Run, o.Pos, 2.45f, inside * (Half + 0.2f));
                RaidMesh m = WallMeshAt(RunPoint(o.Run, o.Pos, 2.5f, 0f));
                m.CylinderX(at, 0.2f, o.Width + 0.2f, 10, DecorPalette.Metal(new Color(0.45f, 0.46f, 0.44f)), o.Run.AlongX ? 0f : 90f);
                m.Box(RunPoint(o.Run, o.Pos, 2.27f, inside * (Half + 0.05f)), RunSize(o.Run, o.Width + 0.1f, 0.12f, 0.04f), DecorPalette.Metal(new Color(0.40f, 0.41f, 0.40f)));
                return;
            }
            Town.Warn(Name + ": no doorway for a roll-up door");
        }

        private void DecorateWalls()
        {
            foreach (WallPiece p in _pieces)
            {
                if (p.Opening != null) continue;
                float len = p.E - p.S;
                if (len < 0.9f) continue;
                WallRun r = p.Run;
                for (int side = -1; side <= 1; side += 2)
                {
                    RoomsOnSide(r, side, _sideRooms);
                    RaidRoom room = null;
                    float a = p.S, b = p.E;
                    foreach (var sr in _sideRooms)
                        if (sr.b > p.S + 0.4f && sr.a < p.E - 0.4f) { room = sr.room; a = Mathf.Max(a, sr.a); b = Mathf.Min(b, sr.b); }
                    bool outside = room == null && _sideRooms.Count == 0;
                    if (room == null && !outside) continue;
                    if (b - a < 0.9f) continue;
                    Vector3 n = RunNormal(r, side);
                    float off = side * (Half + 0.004f);

                    double vineChance = outside ? 0.3 : (Overgrown.Contains(room) ? 0.75 : 0.12);
                    if (Rng.NextDouble() < vineChance)
                    {
                        float w = Mathf.Min(b - a - 0.3f, 0.8f + (float)Rng.NextDouble() * 1.3f);
                        float c = a + 0.15f + w * 0.5f + (float)Rng.NextDouble() * (b - a - 0.3f - w);
                        bool hang = Rng.NextDouble() < 0.35;
                        float h = WallH * (0.55f + (float)Rng.NextDouble() * 0.4f);
                        FurnitureBuilder.Vines(p.Mesh, Rng, RunPoint(r, c, hang ? WallH - 0.05f : 0.02f, off), n, w, h, hang);
                    }
                    double stainChance = outside ? 0.3 : 0.4;
                    if (Rng.NextDouble() < stainChance)
                    {
                        float c = a + 0.4f + (float)Rng.NextDouble() * (b - a - 0.8f);
                        float y = 0.5f + (float)Rng.NextDouble() * 1.7f;
                        Color wall = room != null ? room.Design.Paper : ExteriorColor;
                        // Indoors the patch stays on the wallpaper above the chair rail, so its tint
                        // matches what it's on.
                        if (room != null && room.Design.RailHeight < WallH - 0.5f)
                        {
                            float lo = room.Design.RailHeight + 0.75f, hi = Mathf.Max(lo, WallH - 0.7f);
                            y = lo + (y - 0.5f) / 1.7f * (hi - lo);
                        }
                        FurnitureBuilder.WallStain(p.Mesh, Rng, RunPoint(r, c, y, off), n, wall);
                    }
                }
            }
        }
        #endregion

        #region Litter and clutter
        /// <summary>Scatters floor litter over a room (about <paramref name="perSqm"/> pieces per m²),
        /// avoiding furniture footprints.</summary>
        public void Litter(RaidRoom room, float perSqm)
        {
            Rect inner = room.Inner;
            int count = Mathf.RoundToInt(inner.width * inner.height * perSqm);
            for (int i = 0; i < count; i++)
            {
                // Litter drifts toward the walls and corners.
                float u = (float)Rng.NextDouble(), v = (float)Rng.NextDouble();
                if (Rng.NextDouble() < 0.5) u = u < 0.5f ? u * u : 1f - (1f - u) * (1f - u);
                var p = new Vector3(Mathf.Lerp(inner.xMin + 0.15f, inner.xMax - 0.15f, u), 0.032f, Mathf.Lerp(inner.yMin + 0.15f, inner.yMax - 0.15f, v));
                var probe = new Rect(p.x - 0.1f, p.z - 0.1f, 0.2f, 0.2f);
                bool under = false;
                foreach (Rect f in room.Furniture) if (f.Overlaps(probe)) { under = true; break; }
                if (under) continue;
                DecorProps.Litter(Flat, Rng, p, false);
            }
        }

        /// <summary>Tries to stand a piece of clutter (footprint <paramref name="w"/> × <paramref name="d"/>)
        /// against a random free stretch of the room's walls, clear of doorways and furniture.</summary>
        public bool TryAgainstWall(RaidRoom room, float w, float d, out Place place)
        {
            Rect l = room.Local;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                int side = Rng.Next(4);
                float t;
                switch (side)
                {
                    case 0: t = Mathf.Lerp(l.xMin + Half + w * 0.5f + 0.05f, l.xMax - Half - w * 0.5f - 0.05f, (float)Rng.NextDouble()); place = N(room, t, d); break;
                    case 1: t = Mathf.Lerp(l.xMin + Half + w * 0.5f + 0.05f, l.xMax - Half - w * 0.5f - 0.05f, (float)Rng.NextDouble()); place = S(room, t, d); break;
                    case 2: t = Mathf.Lerp(l.yMin + Half + w * 0.5f + 0.05f, l.yMax - Half - w * 0.5f - 0.05f, (float)Rng.NextDouble()); place = Wst(room, t, d); break;
                    default: t = Mathf.Lerp(l.yMin + Half + w * 0.5f + 0.05f, l.yMax - Half - w * 0.5f - 0.05f, (float)Rng.NextDouble()); place = E(room, t, d); break;
                }
                Rect fp = FootRect(W(place.X, 0f, place.Z), Yaw(place.Yaw), new Vector3(w, 1f, d));
                Rect grown = new Rect(fp.x - 0.08f, fp.y - 0.08f, fp.width + 0.16f, fp.height + 0.16f);
                if (!room.Overlaps(grown, true)) return true;
            }
            place = default;
            return false;
        }

        /// <summary>A few random wall-side clutter pieces: box stacks, barrels, crates.</summary>
        public void Clutter(RaidRoom room, int count, bool military)
        {
            for (int i = 0; i < count; i++)
            {
                int k = Rng.Next(military ? 4 : 3);
                if (k == 0 && TryAgainstWall(room, 0.6f, 0.5f, out Place p))
                {
                    int n = 1 + Rng.Next(3);
                    Furnish(room, p, new Vector3(0.6f, 0.4f * n, 0.5f), n > 1, (m, r) => FurnitureBuilder.BoxStack(m, r, Vector3.zero, 0f, n), "boxes");
                }
                else if (k == 1 && TryAgainstWall(room, 0.62f, 0.62f, out p))
                {
                    Color c = new[] { new Color(0.16f, 0.26f, 0.42f), new Color(0.45f, 0.24f, 0.10f), new Color(0.24f, 0.28f, 0.18f), new Color(0.12f, 0.12f, 0.13f) }[Rng.Next(4)];
                    Furnish(room, p, new Vector3(0.62f, 0.9f, 0.62f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, c), "barrel");
                }
                else if (k == 2 && TryAgainstWall(room, 0.5f, 0.5f, out p))
                {
                    Furnish(room, p, new Vector3(0.5f, 0.35f, 0.5f), false, (m, r) =>
                    {
                        FurnitureBuilder.CardboardBox(m, r, Vector3.zero, new Vector3(0.48f, 0.34f, 0.4f), (float)r.NextDouble() * 30f - 15f);
                    }, "box");
                }
                else if (k == 3 && TryAgainstWall(room, 0.9f, 0.62f, out p))
                {
                    Furnish(room, p, new Vector3(0.9f, 0.55f, 0.62f), true, (m, r) => FurnitureBuilder.Crate(m, r, Vector3.zero, new Vector3(0.88f, 0.5f, 0.58f), 0f, true), "crate");
                }
            }
        }

        /// <summary>A little snow blown in through an outside door.</summary>
        private void TrackedSnow()
        {
            foreach (Opening o in _openings)
            {
                if (o.Kind != OpeningKind.Door) continue;
                int outside = OutsideSide(o.Run, o.Pos);
                if (outside == 0) continue;
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = RunPoint(o.Run, o.Pos + ((float)Rng.NextDouble() - 0.5f) * o.Width, 0.026f, -outside * (Half + 0.2f + (float)Rng.NextDouble() * 0.9f));
                    Flat.FlatBlob(p, 0.15f + (float)Rng.NextDouble() * 0.25f, 0.1f + (float)Rng.NextDouble() * 0.2f, (float)Rng.NextDouble() * 360f, 0.4f, Rng, DecorPalette.Matte(DecorProps.SnowShade));
                }
                // Outside: a step / slab in front of the door.
                Vector3 slab = RunPoint(o.Run, o.Pos, 0.036f, outside * (Half + 0.7f));
                Surf conc = DecorMaterials.Concrete;
                if (o.Run.AlongX) Town.Out.Flat(slab).Flat(slab, o.Width + 0.8f, 1.2f, 0f, conc);
                else Town.Out.Flat(slab).Flat(slab, 1.2f, o.Width + 0.8f, 0f, conc);
            }
        }

        /// <summary>Snow drifted against the outside of the walls (between the doors).</summary>
        private void Drifts()
        {
            foreach (WallPiece p in _pieces)
            {
                if (p.Opening != null && p.Opening.Kind == OpeningKind.Door) continue;
                int outside = OutsideSide(p.Run, (p.S + p.E) * 0.5f);
                if (outside == 0 || Rng.NextDouble() < 0.35) continue;
                float len = p.E - p.S;
                float c = (p.S + p.E) * 0.5f;
                Vector3 face = RunPoint(p.Run, c, 0f, outside * Half);
                Vector3 along = p.Run.AlongX ? Vector3.right : Vector3.forward;
                float d = 0.7f + (float)Rng.NextDouble() * 0.5f;
                RaidMesh m = Town.Out.Solid(face);
                DecorProps.WallDrift(m, Rng, face, along, RunNormal(p.Run, outside), len * 0.9f, d, 0.18f + (float)Rng.NextDouble() * 0.12f);
            }
        }
        #endregion

        #region Output
        /// <summary>Emits every mesh and collider, registers the ceilings and the sheltered interior.</summary>
        public void Finish()
        {
            PlaceAllLeaves();
            DecorateWalls();
            TrackedSnow();
            Drifts();

            foreach (WallPiece p in _pieces)
            {
                Vector3 c = RunPoint(p.Run, (p.S + p.E) * 0.5f, 0f, 0f);
                string name = p.Opening == null ? "Wall" : (p.Opening.Kind == OpeningKind.Door ? "DoorHead" : "Window");
                GameObject go = p.Mesh.Emit(name, Root, c);
                if (go == null) continue;
                foreach (Bounds b in p.Colliders)
                {
                    var bc = go.AddComponent<BoxCollider>();
                    bc.center = b.center - c;
                    bc.size = b.size;
                }
                go.AddComponent<CutawayOccluder>();
            }

            Vector3 center = W(0f, 0f, 0f);
            Static.Emit("Furniture", Root, center, ShadowCastingMode.On);
            Flat.Emit("Floors", Root, center, ShadowCastingMode.Off);
            GameObject ceil = Ceil.Emit("Ceiling_" + Name, Town.Overhead, center, ShadowCastingMode.On);
            if (ceil != null) CeilingVisibility.Register(ceil.GetComponent<MeshRenderer>());

            Rect f = Footprint;
            Shelter.Register(new Bounds(new Vector3(f.center.x, WallH * 0.5f, f.center.y), new Vector3(f.width - 0.4f, WallH, f.height - 0.4f)));
        }
        #endregion
    }
}
