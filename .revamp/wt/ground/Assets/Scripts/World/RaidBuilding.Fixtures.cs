using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The raid town's interactive layer, shared by every building: the starter home's fittings — <see cref="Interactable"/>s
    /// worked with [E] / Q, <see cref="FixturePart"/> doors and drawers, <see cref="StorageFixture"/> cupboards — placed in a
    /// raid building's rooms. A piece of interactive furniture gets a body of its own (<see cref="FixtureBody"/>) or is one of
    /// the home's own pieces (<see cref="HomePiece"/>); anything hung on a wall waits until <see cref="Finish"/> has made
    /// the wall segments objects (<see cref="Later"/>, <see cref="WallObjectAt"/>), so it hides with its wall when the
    /// camera cuts the wall away.
    /// </summary>
    internal sealed partial class RaidBuilding
    {
        private readonly List<System.Action> _later = new List<System.Action>();
        private bool _finished;

        #region Deferred wall work
        /// <summary>Queues <paramref name="work"/> for the end of <see cref="Finish"/>, once every wall segment exists as its
        /// own object: fixtures that hang on a wall (a switch, a cupboard, a mirror cabinet, a door's leaves, a window's
        /// sash) go under their segment (<see cref="WallObjectAt"/>). Work queued after Finish runs at once.</summary>
        public void Later(System.Action work)
        {
            if (work == null) return;
            if (_finished) work();
            else _later.Add(work);
        }

        // Runs the queued work in order; one fixture failing to fit must not take the rest of the town with it.
        private void RunLater()
        {
            _finished = true;
            for (int i = 0; i < _later.Count; i++)
            {
                try { _later[i](); }
                catch (System.Exception e)
                {
                    Town.Warn(Name + ": a fixture failed to fit (" + e.GetType().Name + ": " + e.Message + ")");
                    Debug.LogException(e);
                }
            }
            _later.Clear();
        }

        /// <summary>The wall segment's object at a world point on a wall line — valid once <see cref="Finish"/> has emitted
        /// the segments, i.e. inside <see cref="Later"/> work — or the building's root when no segment is there.</summary>
        public Transform WallObjectAt(Vector3 p)
        {
            WallPiece w = PieceAt(p);
            if (w != null) return ObjectOf(w);
            Transform foreign = ForeignWallObject?.Invoke(p);
            return foreign != null ? foreign : Root;
        }

        // The segment at a world point (the same test as WallMeshAt).
        private WallPiece PieceAt(Vector3 p)
        {
            foreach (WallPiece w in _pieces)
            {
                WallRun r = w.Run;
                float across = r.AlongX ? p.z : p.x;
                float along = r.AlongX ? p.x : p.z;
                if (Mathf.Abs(across - r.Coord) < 0.2f && along >= w.S - 0.01f && along <= w.E + 0.01f) return w;
            }
            return null;
        }

        /// <summary>The segment an opening was cut in (the door's or the window's own), as an object to hang fittings under.</summary>
        private Transform OpeningObject(Opening o)
        {
            foreach (WallPiece w in _pieces)
                if (w.Opening == o) return ObjectOf(w);
            return Root;
        }

        // A segment's object; a segment that drew nothing (a doorway through a wall lower than the door) gets an empty one.
        private Transform ObjectOf(WallPiece w)
        {
            if (w.Object == null)
            {
                w.Object = new GameObject(w.Opening == null ? "Wall" : "Opening");
                w.Object.transform.SetParent(Root, false);
                w.Object.transform.position = RunPoint(w.Run, (w.S + w.E) * 0.5f, 0f, 0f);
            }
            return w.Object.transform;
        }
        #endregion

        #region Furniture frames
        /// <summary>A placement's world position (on the floor).</summary>
        public Vector3 At(Place p) => W(p.X, 0f, p.Z);

        /// <summary>A point in a placement's own frame — x across its front, y up, z out through its front — in the world.
        /// (Pieces are turned, never mirrored, so their own frame is the placement's world yaw.)</summary>
        public Vector3 At(Place p, float x, float y, float z) => W(p.X, 0f, p.Z) + Quaternion.Euler(0f, Yaw(p.Yaw), 0f) * new Vector3(x, y, z);

        /// <summary>A direction in a placement's own frame, in the world.</summary>
        public Vector3 DirOf(Place p, Vector3 local) => Quaternion.Euler(0f, Yaw(p.Yaw), 0f) * local;

        /// <summary>A placement's frame for the part builders (<see cref="KitchenParts.Door"/>, <see cref="KitchenParts.Drawer"/>,
        /// <see cref="KitchenParts.PieceAt"/>): origin on the floor at the piece's middle, turned by its world yaw.</summary>
        public KitchenFrame FrameOf(Place p) => new KitchenFrame(W(p.X, 0f, p.Z), Yaw(p.Yaw));
        #endregion

        #region Interactive furniture
        /// <summary>The body whose <see cref="FixtureBody"/> draw callback is running right now (null otherwise): anything a
        /// draw callback hangs on the piece — a desk lamp's light and switch — belongs under it.</summary>
        public Transform CurrentBody { get; private set; }

        /// <summary>
        /// A piece of interactive furniture's body: an unrotated root object at the placement carrying the piece's static
        /// mesh, drawn turned by <paramref name="draw"/> in the piece's own frame (front toward +Z, origin on the floor at its
        /// middle); a box collider of <paramref name="size"/> when <paramref name="solid"/>; and its footprint marked taken in
        /// <paramref name="room"/>. Hang its moving parts (<see cref="FixturePart"/>s, built with <see cref="FrameOf"/>) and
        /// its fixtures (<see cref="StorageFixture"/>, <see cref="SeatFixture"/>, …) under the returned root, and pass the
        /// root as their occlusion root so the piece's own collider never blocks the reach. <paramref name="bake"/> false
        /// keeps the piece out of the navmesh bake (as the home does for its bathroom fittings, so the dead can still
        /// follow the survivor into a small room).
        /// </summary>
        public Transform FixtureBody(RaidRoom room, Place p, Vector3 size, bool solid, System.Action<RaidMesh, System.Random> draw,
            string name, bool bake = true)
        {
            Vector3 pos = W(p.X, 0f, p.Z);
            float yaw = Yaw(p.Yaw);
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.identity);
            if (draw != null)
            {
                var m = new RaidMesh();
                m.SetFrame(pos, yaw);
                Transform outer = CurrentBody;
                CurrentBody = go.transform;
                try { draw(m, Rng); }
                finally { CurrentBody = outer; }
                m.ResetFrame();
                m.ApplyTo(go, pos);
            }
            if (solid) BodyCollider(go, size, yaw);
            if (!bake) go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            Rect fp = FootRect(pos, yaw, size);
            if (room != null)
            {
                CheckClear(room, fp, name);
                room.Furniture.Add(fp);
            }
            DecorPalette.Apply();
            return go.transform;
        }

        // A body's solid box: on the root when the piece is square to the world, else on a turned child.
        private static void BodyCollider(GameObject go, Vector3 size, float yaw)
        {
            float k = Mathf.Repeat(yaw, 90f);
            if (k < 1f || k > 89f)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, size.y * 0.5f, 0f);
                bc.size = AabbSize(size, yaw);
                return;
            }
            var turned = new GameObject("Collider");
            turned.transform.SetParent(go.transform, false);
            turned.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var c = turned.AddComponent<BoxCollider>();
            c.center = new Vector3(0f, size.y * 0.5f, 0f);
            c.size = size;
        }

        /// <summary>One of the starter home's own interactive pieces (<see cref="HomeFurniture"/>: a counter, a fridge, a
        /// toilet, a basin, a shower, a dresser, a bedside table, a bookcase, an armchair, a dining table and chairs, steel
        /// shelving, boxes, a standard lamp, a TV, a radio) at a placement, exactly as the home has it, its footprint
        /// marked taken in <paramref name="room"/>. A surface item (the TV, the radio) stands at its usual height — on the
        /// dresser's top, the worktop — and claims no floor. Sizes: <see cref="HomeFurniture.SizeOf"/>.</summary>
        public HomeFurniture HomePiece(RaidRoom room, Place p, HomeFurnitureKind kind)
        {
            Vector3 pos = W(p.X, 0f, p.Z);
            float yaw = Yaw(p.Yaw);
            HomeFurniture f = HomeFurniture.Spawn(Root, kind, pos, yaw, Rng.Next());
            if (room != null && !HomeFurniture.IsSurfaceItem(kind))
            {
                Rect fp = HomeFurniture.Footprint(kind, pos, yaw);
                CheckClear(room, fp, kind.ToString());
                room.Furniture.Add(fp);
            }
            return f;
        }
        #endregion

        #region Stock
        /// <summary>How a raid stocks one of its main containers (a wardrobe, a locker, a fridge, a medicine cabinet): seldom
        /// empty, and whatever the loot table rolls — the raid's usual haul, not the starter home's lean pickings.</summary>
        public static StorageOptions MainStock(ContainerType type, string noun = null) => new StorageOptions
        {
            LootType = type,
            Noun = noun,
            EmptyChance = CloneZConfig.RaidMainStorageEmptyChance,
            MaxStacks = int.MaxValue,
            RaidBoost = false,
        };

        /// <summary>How a raid stocks its smaller places (a drawer, a cupboard, a shelf under the counter): often empty, a
        /// few things at most.</summary>
        public static StorageOptions SmallStock(ContainerType type, string noun = null) => new StorageOptions
        {
            LootType = type,
            Noun = noun,
            EmptyChance = CloneZConfig.RaidSmallStorageEmptyChance,
            MaxStacks = CloneZConfig.RaidSmallStorageMaxStacks,
            RaidBoost = false,
        };
        #endregion
    }
}
