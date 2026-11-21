using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    // The town plan, in metres (+x east, +z north): the streets, where every building stands and which way it faces. A
    // building's quarter turns say which way its front door looks: 0 south, 1 west, 2 north, 3 east. The builder draws
    // everything else round these numbers.
    public static partial class RaidMapBuilder
    {
        #region Streets
        /// <summary>A straight street: its carriageway between two kerbs, a sidewalk along either side.</summary>
        private sealed class Street
        {
            public readonly string Name;
            /// <summary>True for a street running east–west.</summary>
            public readonly bool AlongX;
            /// <summary>Its centre line: z for an east–west street, x for a north–south one.</summary>
            public readonly float Coord;
            /// <summary>Where it starts and ends along its length.</summary>
            public readonly float From, To;
            /// <summary>Half the carriageway's width, and each sidewalk's width (its kerb included).</summary>
            public readonly float HalfWidth, Walk;
            /// <summary>Where two streets meet, the higher rank's sidewalks run on across the lower's carriageway and the
            /// lower's stop at the higher's sidewalks: Main St outranks Depot Rd, which outranks the residential streets.</summary>
            public readonly int Rank;

            public Street(string name, bool alongX, float coord, float from, float to, float halfWidth, float walk, int rank)
            {
                Name = name;
                AlongX = alongX;
                Coord = coord;
                From = from;
                To = to;
                HalfWidth = halfWidth;
                Walk = walk;
                Rank = rank;
            }

            /// <summary>From the centre line to a sidewalk's outer edge.</summary>
            public float Outer => HalfWidth + Walk;

            /// <summary>The arterial roads (a centre line, crossings) as against the residential streets.</summary>
            public bool Arterial => Rank > 0;

            /// <summary>The world point <paramref name="along"/> the street and <paramref name="across"/> from its centre line
            /// (+ is the north or east side), at height <paramref name="y"/>.</summary>
            public Vector3 At(float along, float across, float y = 0f)
                => AlongX ? new Vector3(along, y, Coord + across) : new Vector3(Coord + across, y, along);

            /// <summary>The world rectangle from <paramref name="a"/> to <paramref name="b"/> along the street and from
            /// <paramref name="c0"/> to <paramref name="c1"/> across it.</summary>
            public Rect Band(float a, float b, float c0, float c1)
            {
                float a0 = Mathf.Min(a, b), a1 = Mathf.Max(a, b), w0 = Coord + Mathf.Min(c0, c1), w1 = Coord + Mathf.Max(c0, c1);
                return AlongX ? Rect.MinMaxRect(a0, w0, a1, w1) : Rect.MinMaxRect(w0, a0, w1, a1);
            }

            /// <summary>The yaw a prop on the <paramref name="side"/> sidewalk turns to face the carriageway with its +Z.</summary>
            public float FacingRoad(int side) => AlongX ? (side > 0 ? 180f : 0f) : (side > 0 ? 270f : 90f);

            /// <summary>True when <paramref name="other"/>, crossing this street, reaches the band from <paramref name="c0"/> to
            /// <paramref name="c1"/> across it.</summary>
            public bool CrossedBy(Street other, float c0, float c1)
                => other != this && other.AlongX != AlongX && other.From < Coord + Mathf.Max(c0, c1) && other.To > Coord + Mathf.Min(c0, c1);
        }

        /// <summary>Where Elm St and Oak St end, short of the west fence.</summary>
        private const float LaneEnd = -60f;

        private static readonly Street MainSt = new Street("Main St", false, 0f, -GroundHalf, GroundHalf, 4f, 2.4f, 2);
        private static readonly Street DepotRd = new Street("Depot Rd", true, 0f, -GroundHalf, GroundHalf, 4f, 2.4f, 1);
        // The residential streets run west from Main St's kerb.
        private static readonly Street ElmSt = new Street("Elm St", true, 38f, LaneEnd, -4f, 3.25f, 2f, 0);
        private static readonly Street OakSt = new Street("Oak St", true, -38f, LaneEnd, -4f, 3.25f, 2f, 0);
        private static readonly Street[] AllStreets = { MainSt, DepotRd, ElmSt, OakSt };

        /// <summary>The rail line's centre, running north–south past the depot and over Depot Rd at the Rail Bridge exit.</summary>
        private const float RailX = 66f;

        /// <summary>The exits' distance from the crossroads, out along the four roads.</summary>
        private const float ExitReach = 63f;
        #endregion

        #region Civic buildings
        // The four corners of the crossroads: the county office (facing Depot Rd, its side door on Main St), Veterans Park,
        // the National Guard post (facing Main St) and the clinic (facing Depot Rd).
        private static readonly Vector2 OfficeAt = new Vector2(19.5f, 17.5f);
        private const int OfficeTurns = 0;
        private static readonly Rect ParkLot = Rect.MinMaxRect(-25f, 6.4f, -6.4f, 32.75f);
        private static readonly Vector2 PostAt = new Vector2(21f, -19f);
        private const int PostTurns = 1;
        private static readonly Vector2 ClinicAt = new Vector2(-17.5f, -15.5f);
        private const int ClinicTurns = 2;

        // Up Main St the gas-station food mart behind its forecourt; out by the rail line the depot behind its yard; behind the
        // Guard post the quarantine tent inside its fenced compound.
        private static readonly Vector2 MartAt = new Vector2(20f, 49f);
        private const int MartTurns = 1;
        private static readonly Vector2 DepotAt = new Vector2(51f, 50f);
        private const int DepotTurns = 0;
        private static readonly Vector2 TentAt = new Vector2(47f, -27.5f);
        private const int TentTurns = 1;
        private static readonly Rect Compound = Rect.MinMaxRect(35f, -42.5f, 60f, -13f);
        #endregion

        #region Houses
        /// <summary>A house's front yard, sidewalk to front wall.</summary>
        private const float Setback = 4.5f;

        /// <summary>A house on Elm St or Oak St: which side of the street, where along it, ranch or cottage, and which side of
        /// the house its drive runs (the kitchen door opens onto the drive).</summary>
        private readonly struct HouseLot
        {
            public readonly string Name;
            public readonly Street Street;
            /// <summary>+1 north of the street (the house faces south), -1 south of it (facing north).</summary>
            public readonly int Side;
            /// <summary>The house's middle along the street.</summary>
            public readonly float X;
            /// <summary>The 10 × 9 cottage, else the 12 × 10 ranch.</summary>
            public readonly bool Small;
            /// <summary>The drive along the house's east side, else its west.</summary>
            public readonly bool DriveEast;

            public HouseLot(string name, Street street, int side, float x, bool small, bool driveEast)
            {
                Name = name;
                Street = street;
                Side = side;
                X = x;
                Small = small;
                DriveEast = driveEast;
            }

            public int Turns => Side > 0 ? 0 : 2;
            /// <summary>Mirrored as need be to bring the kitchen door (the plan's local +x wall) round to the drive.</summary>
            public bool Mirror => Side > 0 ? !DriveEast : DriveEast;
            public float HalfW => Small ? 5f : 6f;
            public float HalfD => Small ? 4.5f : 5f;
            /// <summary>The sidewalk's outer edge in front of the house.</summary>
            public float SidewalkZ => Street.Coord + Side * Street.Outer;
            /// <summary>The front wall's outer face.</summary>
            public float FrontZ => SidewalkZ + Side * Setback;
            /// <summary>The back wall's outer face.</summary>
            public float BackZ => FrontZ + Side * (HalfD + RaidBuilding.Half) * 2f;
            public Vector2 Center => new Vector2(X, FrontZ + Side * (HalfD + RaidBuilding.Half));
            /// <summary>The middle of the drive, across the street.</summary>
            public float DriveX => X + (DriveEast ? 1f : -1f) * (HalfW + RaidBuilding.Half + 1f + DriveHalf);
        }

        /// <summary>Half a drive's width.</summary>
        private const float DriveHalf = 1.6f;

        // Five houses to a street, two or three to a side, numbered outward from Main St (odd numbers on the north side).
        private static readonly HouseLot[] HouseLots =
        {
            new HouseLot("House_Elm_105", ElmSt, +1, -17f, false, true),
            new HouseLot("House_Elm_109", ElmSt, +1, -34.5f, true, true),
            new HouseLot("House_Elm_113", ElmSt, +1, -52f, false, true),
            new HouseLot("House_Elm_110", ElmSt, -1, -37f, false, true),
            new HouseLot("House_Elm_114", ElmSt, -1, -54f, true, true),
            new HouseLot("House_Oak_109", OakSt, +1, -38f, false, true),
            new HouseLot("House_Oak_113", OakSt, +1, -55.5f, true, true),
            new HouseLot("House_Oak_106", OakSt, -1, -17f, true, true),
            new HouseLot("House_Oak_110", OakSt, -1, -34.5f, false, true),
            new HouseLot("House_Oak_114", OakSt, -1, -52f, false, true),
        };

        /// <summary>The number on a house's door: the end of its lot name.</summary>
        private static string HouseNumberOf(in HouseLot lot) => lot.Name.Substring(lot.Name.LastIndexOf('_') + 1);

        /// <summary>The houses on one side of one street, west to east.</summary>
        private static List<int> Row(Street street, int side)
        {
            var row = new List<int>();
            for (int i = 0; i < HouseLots.Length; i++)
                if (HouseLots[i].Street == street && HouseLots[i].Side == side) row.Add(i);
            row.Sort((a, b) => HouseLots[a].X.CompareTo(HouseLots[b].X));
            return row;
        }
        #endregion
    }
}
