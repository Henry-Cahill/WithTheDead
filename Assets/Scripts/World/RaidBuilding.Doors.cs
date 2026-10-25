using Unity.AI.Navigation;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The raid town's doors: every doorway declared with a leaf gets the starter home's kind of door — a pair of panelled
    /// (or a shop's glazed) leaves hinged at the jambs and drawn shut across the gap, opening into the room whose swing is
    /// clear — as a working <see cref="RaidDoor"/>, named, locked and left the way a town emptied in a hurry would leave it.
    /// </summary>
    internal sealed partial class RaidBuilding
    {
        private const float LeafClear = 0.026f;   // each leaf's hinge, in from its jamb (just past the lining)
        private const float LeafMeet = 0.01f;     // the gap where the pair meet
        private const float LeafInset = 0.01f;    // a shut leaf's hinge line, in from the face it swings toward
        private const float LeafFoot = 0.03f;     // a leaf's foot, clear of the threshold
        private const float DoorKnobHeight = 1.05f;

        private static readonly Color[] FrontDoorPaints =
        {
            new Color(0.46f, 0.13f, 0.10f), new Color(0.17f, 0.27f, 0.21f), new Color(0.16f, 0.20f, 0.30f), new Color(0.80f, 0.76f, 0.64f),
        };

        /// <summary>What a door is called, how it locks and how a fresh raid finds it.</summary>
        private struct DoorPolicy
        {
            public string Name, LockFrom;
            public int LockSide;
            public bool Deadbolt, Peephole, Open, Locked;
            public float Health;
            public Color Wood;
        }

        // The door itself, once the doorway's segment is an object: the pair of leaves, drawn shut and hinged at the jambs to
        // swing open toward `swing` and rest leaning `leanA` / `leanB` degrees past square, under a root at the gap's middle.
        private void HangDoor(Opening o, int swing, float leafW, float leanA, float leanB)
        {
            WallRun r = o.Run;
            Transform segment = OpeningObject(o);
            var root = new GameObject("Door");
            root.transform.SetParent(segment, false);
            root.transform.SetPositionAndRotation(RunPoint(r, o.Pos, 0f, 0f), Quaternion.identity);
            root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            RaidRoom plus = RoomAt(RunPoint(r, o.Pos, 0f, 0.6f)), minus = RoomAt(RunPoint(r, o.Pos, 0f, -0.6f));
            DoorPolicy policy = PolicyFor(o, OutsideSide(r, o.Pos), swing, plus, minus);

            Vector3 along = r.AlongX ? Vector3.right : Vector3.forward;
            Vector3 into = RunNormal(r, swing);
            float across = swing * (Half - LeafInset), leafH = ShellDoorHeight - LeafFoot - 0.03f;
            var leaves = new FixturePart[2];
            var hinges = new Vector3[2];
            var reaches = new Vector3[2];
            var angles = new float[2];
            for (int i = 0; i < 2; i++)
            {
                bool atA = i == 0;
                Vector3 hinge = RunPoint(r, atA ? o.A + LeafClear : o.B - LeafClear, LeafFoot, across);
                Vector3 reach = atA ? along : -along;
                var m = new RaidMesh();
                m.SetFrame(hinge, Mathf.Atan2(reach.x, reach.z) * Mathf.Rad2Deg - 90f);   // the leaf's local +X along `reach`
                FurnitureBuilder.DoorLeaf(m, Rng, leafW, leafH, policy.Wood, o.Glass);
                m.ResetFrame();
                float angle = Mathf.Sign(Vector3.SignedAngle(reach, into, Vector3.up)) * (90f + (atA ? leanA : leanB));
                leaves[i] = FixturePart.Hinge(root.transform, "Leaf", m, hinge, Vector3.up, angle).WithSpeed(CloneZConfig.DoorSwingSeconds);
                hinges[i] = hinge;
                reaches[i] = reach;
                angles[i] = angle;
            }

            var setup = new HomeDoor.Setup
            {
                Name = policy.Name,
                Noun = OpeningNoun(policy.Name),
                Leaves = leaves,
                Point = RunPoint(r, o.Pos, DoorKnobHeight, 0f),
                Normal = RunNormal(r, 1),
                Leaf = new Bounds(RunPoint(r, o.Pos, ShellDoorHeight * 0.5f, 0f), RunSize(r, o.Width, ShellDoorHeight, Half * 2f)),
                Gap = new Bounds(RunPoint(r, o.Pos, WallH * 0.5f, 0f), RunSize(r, o.Width, WallH, Half * 2f)),
                Block = new Bounds(RunPoint(r, o.Pos, (0.25f + ShellDoorHeight) * 0.5f, 0f), RunSize(r, o.Width - 0.1f, ShellDoorHeight - 0.25f, Half * 2f + 0.1f)),
                LockSide = policy.LockSide,
                LockFrom = policy.LockFrom,
                Deadbolt = policy.Deadbolt,
                Peephole = policy.Peephole,
                Open = policy.Open,
                Locked = policy.Locked,
                SeeThrough = o.Glass,
                Occluder = segment.GetComponent<CutawayOccluder>(),
            };
            var raid = new RaidDoor.Raid
            {
                Health = policy.Health,
                Line = new OpeningLine(r.AlongX, r.Coord),
                Mid = o.Pos,
                Width = o.Width,
                Swing = swing,
                Building = Footprint,
                Hinges = hinges,
                Reaches = reaches,
                Angles = angles,
                Wood = policy.Wood,
                Seed = Rng.Next(),
            };
            RaidDoor.Attach(root, setup, raid);
        }

        // Outer doors: "Front door" (in the front wall, facing the street), "Back door", "Side door" — bolted from inside, a
        // house's front door with a peephole; half of them shut and a third of those locked. Inner doors are named for the
        // room they open into — a bathroom's always for the bathroom, which it locks from — and mostly left standing open.
        private DoorPolicy PolicyFor(Opening o, int outside, int swing, RaidRoom plus, RaidRoom minus)
        {
            var p = new DoorPolicy { Wood = DoorWood };
            if (outside != 0)
            {
                Vector3 outward = RunNormal(o.Run, outside);
                bool front = Vector3.Dot(outward, Dir(0f, -1f)) > 0.7f, back = Vector3.Dot(outward, Dir(0f, 1f)) > 0.7f;
                p.Name = front ? "Front door" : back ? "Back door" : "Side door";
                p.LockSide = -outside;
                p.LockFrom = "inside the " + DoorBuildingNoun;
                p.Deadbolt = true;
                p.Peephole = front && Kind == RaidBuildingKind.House && !o.Glass;
                bool shut = Rng.NextDouble() < CloneZConfig.RaidExteriorDoorShutChance;
                p.Open = !shut;
                p.Locked = shut && Rng.NextDouble() < CloneZConfig.RaidExteriorDoorLockChance;
                p.Health = o.Glass ? CloneZConfig.RaidDoorHealthGlass : CloneZConfig.RaidDoorHealthExterior;
                if (front && Kind == RaidBuildingKind.House && !o.Glass && Rng.NextDouble() < 0.6)
                    p.Wood = FrontDoorPaints[Rng.Next(FrontDoorPaints.Length)];
                return p;
            }

            RaidRoom into = swing > 0 ? plus : minus;
            RaidRoom wash = OpeningWashroom(plus) ? plus : OpeningWashroom(minus) ? minus : null;
            RaidRoom named = wash ?? into ?? plus ?? minus;
            p.Name = named != null ? OpeningRoomTitle(named.Name) + " door" : "Door";
            if (wash != null)
            {
                p.LockSide = wash == plus ? 1 : -1;
                p.LockFrom = "inside the " + OpeningNoun(OpeningRoomTitle(wash.Name));
            }
            p.Open = Rng.NextDouble() < CloneZConfig.RaidInteriorDoorOpenChance;
            p.Health = CloneZConfig.RaidDoorHealthInterior;
            return p;
        }

        private static bool OpeningWashroom(RaidRoom room)
        {
            if (room == null) return false;
            string n = room.Name.ToLowerInvariant();
            return n.Contains("bath") || n.Contains("restroom") || n.Contains("toilet") || n.Contains("washroom") || n.Contains("lavatory");
        }

        /// <summary>What the building is called from inside it: "the house", "the clinic".</summary>
        private string DoorBuildingNoun
        {
            get
            {
                switch (Kind)
                {
                    case RaidBuildingKind.Office: return "building";
                    case RaidBuildingKind.Store: return "store";
                    case RaidBuildingKind.Clinic: return "clinic";
                    case RaidBuildingKind.Army: return "post";
                    case RaidBuildingKind.Depot: return "depot";
                    case RaidBuildingKind.Tent: return "tent";
                    default: return "house";
                }
            }
        }

        /// <summary>A room's name as a door's or a window's starts with it: "Living room", "CO office", "Records room" — the
        /// first word capitalised, the rest lower case, acronyms kept.</summary>
        private static string OpeningRoomTitle(string room)
        {
            if (string.IsNullOrEmpty(room)) return "Room";
            switch (room.ToLowerInvariant())
            {
                case "manager": return "Manager's office";
                case "records": return "Records room";
                case "utility": return "Utility room";
                case "laundry": return "Laundry room";
                case "operations": return "Operations room";
            }
            string[] words = room.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];
                if (w.Length == 0 || OpeningAcronym(w)) continue;
                w = w.ToLowerInvariant();
                words[i] = i == 0 ? char.ToUpperInvariant(w[0]) + w.Substring(1) : w;
            }
            return string.Join(" ", words);
        }

        // "Front door" → "front door"; "CO office door" keeps its capitals.
        private static string OpeningNoun(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            int space = name.IndexOf(' ');
            string first = space < 0 ? name : name.Substring(0, space);
            return OpeningAcronym(first) ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        private static bool OpeningAcronym(string word)
        {
            if (word.Length < 2) return false;
            for (int i = 0; i < word.Length; i++)
                if (!char.IsUpper(word[i])) return false;
            return true;
        }
    }
}
