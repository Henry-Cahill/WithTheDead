using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Workplaces' ground floors, and every room's finishes: wallpaper and wainscot, floor, ceiling fitting, whether its light
    // was left on.
    internal static partial class InteriorPass
    {
        #region Corridors
        /// <summary>A bay off a corridor: its rectangle, and the side of it the corridor runs along (0 south, 1 east, 2 north,
        /// 3 west).</summary>
        private struct Bay
        {
            public Rect R;
            public int Side;
            public float Order;
            /// <summary>The run of bays it was cut from, and its place along it (bays next along a run can be one room).</summary>
            public int Run, Pos;
        }

        private const float CorridorWidth = 1.8f, MinBay = 2.8f;

        // A corridor down the long axis of `m` from a lobby at the front door, with bays of about `bay` metres either side of
        // it (or along one side of a block too narrow for two): the lobby and the corridor go into the plan, the bays come back
        // nearest the lobby first. False (and nothing added) when the block can't take a corridor.
        /// <summary>Build a corridor with surrounding bays for a given building block.</summary>
        private static bool Corridor(InteriorPlan plan, Rect m, Vector2 door, RoomRole lobbyRole, float lobbyDepth, float bay, List<Bay> bays)
        {
            // Cache block width and depth for later calculations.
            float w = m.width, d = m.height;
            bays.Clear();
            // Choose layout based on block aspect ratio (deep vs wide).
            if (d >= w)
            {
                // Deep: the lobby across the front, the corridor from it to the back wall.
                if (w < CorridorWidth + MinBay || d < lobbyDepth + 2f * MinBay) return false;
                Rect[] z = SplitZ(m, false, 2.6f, lobbyDepth, 0f);
                PlanRoom lobby = Add(plan, lobbyRole, z[0]);
                Rect rest = z[1];
                bool two = w >= CorridorWidth + 2f * MinBay;
                float half = CorridorWidth * 0.5f;
                float cx = two ? Mathf.Clamp(door.x, rest.xMin + MinBay + half, rest.xMax - MinBay - half)
                    : door.x < rest.center.x ? rest.xMin + half : rest.xMax - half;
                var corr = Rect.MinMaxRect(cx - half, rest.yMin, cx + half, rest.yMax);
                PlanRoom hall = Add(plan, RoomRole.Hall, corr);
                hall.Name = "Corridor";
                OpenLater(plan, hall, lobby);
                if (corr.xMin - rest.xMin >= MinBay - 0.01f)
                    AddBays(Rect.MinMaxRect(rest.xMin, rest.yMin, corr.xMin, rest.yMax), false, 1, bay, bays);
                if (rest.xMax - corr.xMax >= MinBay - 0.01f)
                    AddBays(Rect.MinMaxRect(corr.xMax, rest.yMin, rest.xMax, rest.yMax), false, 3, bay, bays);
                for (int i = 0; i < bays.Count; i++)
                {
                    Bay b = bays[i];
                    b.Order = b.R.center.y - rest.yMin + Mathf.Abs(b.R.center.x - door.x) * 0.1f;
                    bays[i] = b;
                }
            }
            else
            {
                // Wide: the corridor along the building through its middle, the lobby cut from the front row at the door.
                if (d < CorridorWidth + MinBay || w < 2f * MinBay) return false;
                bool two = d >= CorridorWidth + 2f * MinBay;
                Rect front = default, corr, back;
                if (two)
                {
                    Rect[] z = SplitZ(m, false, MinBay, (d - CorridorWidth) * 0.5f, CorridorWidth, 0f);
                    front = z[0];
                    corr = z[1];
                    back = z[2];
                }
                else
                {
                    Rect[] z = SplitZ(m, false, 1.8f, CorridorWidth, 0f);
                    corr = z[0];
                    back = z[1];
                }
                PlanRoom hall = Add(plan, RoomRole.Hall, corr);
                hall.Name = "Corridor";
                if (two)
                {
                    float lw = Mathf.Clamp(w * 0.18f, 3.2f, 6f);
                    float lx0 = Mathf.Clamp(door.x - lw * 0.5f, front.xMin, front.xMax - lw);
                    if (lx0 - front.xMin < MinBay) lx0 = front.xMin;
                    float lx1 = Mathf.Min(front.xMax, lx0 + lw);
                    if (front.xMax - lx1 < MinBay) lx1 = front.xMax;
                    PlanRoom lobby = Add(plan, lobbyRole, Rect.MinMaxRect(lx0, front.yMin, lx1, front.yMax));
                    OpenLater(plan, lobby, hall);
                    if (lx0 - front.xMin >= MinBay - 0.01f) AddBays(Rect.MinMaxRect(front.xMin, front.yMin, lx0, front.yMax), true, 2, bay, bays);
                    if (front.xMax - lx1 >= MinBay - 0.01f) AddBays(Rect.MinMaxRect(lx1, front.yMin, front.xMax, front.yMax), true, 2, bay, bays);
                }
                AddBays(back, true, 0, bay, bays);
                for (int i = 0; i < bays.Count; i++)
                {
                    Bay b = bays[i];
                    b.Order = Mathf.Abs(b.R.center.x - door.x) + (b.Side == 0 ? 0.5f : 0f);
                    bays[i] = b;
                }
            }
            bays.Sort((a, b) => a.Order.CompareTo(b.Order));
            return true;
        }

        private static void AddBays(Rect run, bool alongX, int side, float bay, List<Bay> into)
        {
            float len = alongX ? run.width : run.height;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / bay));
            while (n > 1 && len / n < MinBay) n--;
            int runIndex = into.Count > 0 ? into[into.Count - 1].Run + 1 : 0;
            for (int k = 0; k < n; k++)
            {
                float a = k / (float)n, c = (k + 1) / (float)n;
                Rect r = alongX
                    ? Rect.MinMaxRect(Mathf.Lerp(run.xMin, run.xMax, a), run.yMin, Mathf.Lerp(run.xMin, run.xMax, c), run.yMax)
                    : Rect.MinMaxRect(run.xMin, Mathf.Lerp(run.yMin, run.yMax, a), run.xMax, Mathf.Lerp(run.yMin, run.yMax, c));
                into.Add(new Bay { R = r, Side = side, Run = runIndex, Pos = k });
            }
        }

        // The bays along the same run next to bay `i` (`step` places on), or -1.
        private static int Neighbour(List<Bay> bays, bool[] taken, int i, int step)
        {
            for (int j = 0; j < bays.Count; j++)
                if (!taken[j] && bays[j].Run == bays[i].Run && bays[j].Pos == bays[i].Pos + step) return j;
            return -1;
        }
        #endregion

        #region Shops, diners, garages
        // A shop: the sales floor from the front door back, and across the back the stock room, an office and a restroom (two
        // in a big store); a pharmacy's back room is its dispensary.
        private static void Shop(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            float w = m.width, d = m.height, area = w * d;
            bool big = plan.Use == InteriorUse.BigStore || area > 600f;
            RoomRole stock = plan.Use == InteriorUse.Pharmacy ? RoomRole.Dispensary : RoomRole.Stock;
            if (area < 42f || d < 5.6f)
            {
                if (w >= 7.4f && area >= 30f)
                {
                    Rect[] s = SplitX(m, east, 2.4f, 0f, 2.4f);
                    Add(plan, RoomRole.Sales, s[0]);
                    Add(plan, area >= 50f ? stock : RoomRole.Restroom, s[1]);
                }
                else Add(plan, RoomRole.Sales, m);
                return;
            }
            float backD = Mathf.Clamp(d * (big ? 0.22f : 0.3f), 2.6f, big ? 8f : 5f);
            Rect[] z = SplitZ(m, false, 3.0f, 0f, backD);
            Add(plan, RoomRole.Sales, z[0]);
            Rect back = z[1];
            float bw = back.width, office = Mathf.Clamp(bw * 0.22f, 2.8f, 4.2f);
            if (big && bw >= 2.6f + office + 4.6f)
            {
                Rect[] b = SplitX(back, !east, 2.2f, 2.3f, 2.3f, office, 0f);
                Add(plan, RoomRole.Restroom, b[0]).Name = "Men's Room";
                Add(plan, RoomRole.Restroom, b[1]).Name = "Ladies' Room";
                Add(plan, RoomRole.BackOffice, b[2]);
                Add(plan, stock, b[3]);
            }
            else if (bw >= 2.6f + office + 2.3f)
            {
                Rect[] b = SplitX(back, !east, 2.2f, 2.3f, office, 0f);
                Add(plan, RoomRole.Restroom, b[0]);
                Add(plan, RoomRole.BackOffice, b[1]);
                Add(plan, stock, b[2]);
            }
            else if (bw >= 2.6f + 2.3f)
            {
                Rect[] b = SplitX(back, !east, 2.2f, 2.3f, 0f);
                Add(plan, RoomRole.Restroom, b[0]);
                Add(plan, stock, b[1]);
            }
            else Add(plan, stock, back);
        }

        // A diner or a bar: the dining room (or the barroom) at the front, the kitchen (a bar's stock room) behind it with
        // the restrooms beside.
        private static void Eatery(InteriorPlan plan, Rect m, bool east, bool bar, System.Random rng)
        {
            float w = m.width, d = m.height, area = w * d;
            RoomRole hall = bar ? RoomRole.Bar : RoomRole.Eatery;
            RoomRole kitchen = bar && rng.NextDouble() < 0.5 ? RoomRole.Stock : RoomRole.CommercialKitchen;
            if (area < 45f || d < 6f)
            {
                if (w >= 7f)
                {
                    Rect[] s = SplitX(m, east, 2.6f, 0f, 2.8f);
                    Add(plan, hall, s[0]);
                    Add(plan, kitchen, s[1]);
                }
                else Add(plan, hall, m);
                return;
            }
            Rect[] z = SplitZ(m, false, 3.0f, 0f, Mathf.Clamp(d * 0.36f, 2.8f, 6.5f));
            Add(plan, hall, z[0]);
            Rect back = z[1];
            if (back.width >= 2.8f + 4.6f)
            {
                Rect[] b = SplitX(back, !east, 2.2f, 2.3f, 2.3f, 0f);
                Add(plan, RoomRole.Restroom, b[0]).Name = "Men's Room";
                Add(plan, RoomRole.Restroom, b[1]).Name = "Ladies' Room";
                Add(plan, kitchen, b[2]);
            }
            else if (back.width >= 2.8f + 2.3f)
            {
                Rect[] b = SplitX(back, !east, 2.2f, 2.3f, 0f);
                Add(plan, RoomRole.Restroom, b[0]);
                Add(plan, kitchen, b[1]);
            }
            else Add(plan, kitchen, back);
        }

        // A garage that works on cars: the parts counter by the door (and a restroom behind it), the service bays beyond.
        private static void AutoShop(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            float w = m.width, d = m.height;
            if (w * d < 50f || w < 7f)
            {
                Add(plan, RoomRole.Bays, m);
                return;
            }
            Rect[] x = SplitX(m, east, 2.8f, Mathf.Clamp(w * 0.3f, 3f, 6f), 0f);
            if (d >= 5.4f)
            {
                Rect[] c = SplitZ(x[0], false, 2.2f, 0f, 2.3f);
                Add(plan, RoomRole.Sales, c[0]).Name = "Parts Counter";
                Add(plan, RoomRole.Restroom, c[1]);
            }
            else Add(plan, RoomRole.Sales, x[0]).Name = "Parts Counter";
            Add(plan, RoomRole.Bays, x[1]);
        }
        #endregion

        #region Churches, schools, firehouses, works, barns
        // A church: the sanctuary along the building's length with its pews facing the altar at the far end. Deep (the door in
        // a gable end): the vestibule inside the doors, the sanctuary, and behind the altar wall a pastor's study and a
        // fellowship room when it's long enough. Wide (the door in a long side, as a country church often has it): the door
        // opens into the sanctuary's side, the study and the fellowship room across the end farther from the door.
        private static void Church(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            float w = m.width, d = m.height;
            bool funeral = plan.Use == InteriorUse.Funeral;
            if (w * d < 60f || Mathf.Min(w, d) < 5f)
            {
                Add(plan, funeral ? RoomRole.Chapel : RoomRole.Nave, m);
                return;
            }
            if (d >= w)
            {
                float narthex = Mathf.Clamp(d * 0.15f, 2.4f, 4f);
                bool behind = w >= 9f && d >= 15f;
                Rect[] z = behind ? SplitZ(m, false, 2.6f, narthex, 0f, Mathf.Clamp(d * 0.16f, 3f, 4.5f)) : SplitZ(m, false, 2.6f, narthex, 0f);
                Add(plan, RoomRole.Narthex, z[0]);
                Add(plan, funeral ? RoomRole.Chapel : RoomRole.Nave, z[1]);
                if (!behind) return;
                Rect[] b = SplitX(z[2], east, 2.8f, -0.45f, 0f);
                Add(plan, RoomRole.Sacristy, b[0]);
                Add(plan, funeral ? RoomRole.Office : RoomRole.Meeting, b[1]);
                return;
            }
            if (w < 14f)
            {
                Add(plan, funeral ? RoomRole.Chapel : RoomRole.Nave, m);
                return;
            }
            Rect[] x = SplitX(m, !east, 3f, Mathf.Clamp(w * 0.2f, 3f, 6.5f), 0f);
            Rect[] e = SplitZ(x[0], false, 2.6f, -0.45f, 0f);
            Add(plan, RoomRole.Sacristy, e[0]);
            Add(plan, funeral ? RoomRole.Office : RoomRole.Meeting, e[1]);
            Add(plan, funeral ? RoomRole.Chapel : RoomRole.Nave, x[1]);
        }

        // A hall from the front door back (a library's reading room, a bank's banking hall, a community centre's hall), and
        // across the back a strip of the smaller rooms it needs, as many as fit.
        private static void FrontHall(InteriorPlan plan, Rect m, bool east, RoomRole hall, float backShare, params RoomRole[] back)
        {
            float w = m.width, d = m.height;
            if (w * d < 45f || d < 6.2f || w < 5f)
            {
                Add(plan, hall, m);
                return;
            }
            Rect[] z = SplitZ(m, false, 3.4f, 0f, Mathf.Clamp(d * backShare, 2.8f, 6f));
            Add(plan, hall, z[0]);
            BackStrip(plan, z[1], east, back);
        }

        // Rooms side by side along a strip, about 3.4 m each (a restroom 2.4), from the end away from the door; as many of
        // `roles` as fit, the last taking what's left.
        private static void BackStrip(InteriorPlan plan, Rect strip, bool east, RoomRole[] roles)
        {
            int n = Mathf.Clamp(Mathf.FloorToInt(strip.width / 3.4f), 1, roles.Length);
            var sizes = new float[n];
            for (int i = 0; i < n - 1; i++) sizes[i] = roles[i] == RoomRole.Restroom ? 2.4f : -1f / n;
            Rect[] parts = SplitX(strip, !east, 2.2f, sizes);
            for (int i = 0; i < n; i++) Add(plan, roles[i], parts[i]);
        }

        // A post office: the lobby inside the doors (the counter, the boxes), the sorting room behind it, and across the back
        // the postmaster's office, a restroom, the store and the break room.
        private static void PostOffice(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            float w = m.width, d = m.height;
            if (w * d < 60f || d < 7.5f)
            {
                FrontHall(plan, m, east, RoomRole.Lobby, 0.45f, RoomRole.Sorting, RoomRole.Restroom);
                return;
            }
            bool strip = d >= 13f;
            float lobby = Mathf.Clamp(d * 0.32f, 3.2f, 7f);
            Rect[] z = strip ? SplitZ(m, false, 3f, lobby, 0f, Mathf.Clamp(d * 0.2f, 2.8f, 4.5f)) : SplitZ(m, false, 3f, lobby, 0f);
            Add(plan, RoomRole.Lobby, z[0]);
            Add(plan, RoomRole.Sorting, z[1]);
            if (strip) BackStrip(plan, z[2], east, new[] { RoomRole.Office, RoomRole.Restroom, RoomRole.Stock, RoomRole.BreakRoom });
        }

        // A funeral home: the foyer inside the doors, the chapel behind it (rows of chairs facing the casket), and across the
        // back the arrangement room, the office, a restroom and the preparation room.
        private static void Funeral(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            float w = m.width, d = m.height;
            if (w * d < 60f || d < 8f)
            {
                FrontHall(plan, m, east, RoomRole.Chapel, 0.3f, RoomRole.Office, RoomRole.Restroom);
                return;
            }
            bool strip = d >= 13f;
            float foyer = Mathf.Clamp(d * 0.2f, 2.8f, 4.5f);
            Rect[] z = strip ? SplitZ(m, false, 3f, foyer, 0f, Mathf.Clamp(d * 0.26f, 3f, 5f)) : SplitZ(m, false, 3f, foyer, 0f);
            Add(plan, RoomRole.Narthex, z[0]);
            Add(plan, RoomRole.Chapel, z[1]);
            if (strip) BackStrip(plan, z[2], east, new[] { RoomRole.Sacristy, RoomRole.Office, RoomRole.Restroom, RoomRole.Stock });
        }

        // A school: a corridor from the entrance hall with classrooms either side, the office by the entrance, restrooms, and
        // in a big one the gym at the far end.
        private static void School(InteriorPlan plan, Rect m, Vector2 door, System.Random rng)
        {
            float w = m.width, d = m.height, area = w * d;
            bool east = door.x > m.center.x;
            if (area < 200f)
            {
                if (w >= 9f)
                {
                    Rect[] x = SplitX(m, east, 3f, Mathf.Clamp(w * 0.3f, 3f, 4.5f), 0f);
                    Rect[] c = SplitZ(x[0], false, 2.2f, 0f, 2.3f);
                    Add(plan, RoomRole.Office, c[0]);
                    Add(plan, RoomRole.Restroom, c[1]);
                    Add(plan, RoomRole.Classroom, x[1]);
                }
                else Add(plan, RoomRole.Classroom, m);
                return;
            }
            Rect body = m;
            if (area >= 1400f)
            {
                // The gym across the far end of the long axis.
                bool alongX = w >= d;
                float gym = Mathf.Clamp((alongX ? w : d) * 0.28f, 12f, 30f);
                Rect[] g = alongX ? SplitX(m, !east, 8f, gym, 0f) : SplitZ(m, true, 8f, gym, 0f);
                Add(plan, RoomRole.Gym, g[0]);
                body = g[1];
            }
            var bays = new List<Bay>(16);
            if (!Corridor(plan, body, door, RoomRole.Lobby, 4f, 8f, bays))
            {
                Add(plan, RoomRole.Classroom, body);
                return;
            }
            for (int i = 0; i < bays.Count; i++)
            {
                RoomRole role = i == 0 ? RoomRole.Office : i == 1 || i == 6 ? RoomRole.Restroom : i == 4 ? RoomRole.BreakRoom : RoomRole.Classroom;
                if (role == RoomRole.Restroom && bays[i].R.width * bays[i].R.height > 40f) role = RoomRole.Classroom;
                PlanRoom r = Add(plan, role, bays[i].R);
                if (role == RoomRole.Restroom) r.Name = i == 1 ? "Boys' Room" : "Girls' Room";
            }
        }

        // A firehouse: the apparatus bay by the door, and beside it the office, the day room with its kitchen, the bunk room
        // and a restroom.
        private static void FireStation(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            float w = m.width, d = m.height;
            if (w * d < 70f || w < 9f || d < 8f)
            {
                Add(plan, RoomRole.Bays, m);
                return;
            }
            Rect[] x = SplitX(m, east, 3.4f, 0f, Mathf.Clamp(w * 0.4f, 3.6f, 9f));
            Add(plan, RoomRole.Bays, x[0]);
            Rect q = x[1];
            Rect[] z = SplitZ(q, false, 2.6f, 3.2f, 0f, Mathf.Clamp(d * 0.3f, 3.2f, 6f));
            if (q.width >= 5.6f)
            {
                Rect[] o = SplitX(z[0], !east, 2.3f, 2.3f, 0f);
                Add(plan, RoomRole.Restroom, o[0]);
                Add(plan, RoomRole.Office, o[1]);
            }
            else Add(plan, RoomRole.Office, z[0]);
            Add(plan, RoomRole.DayRoom, z[1]);
            Add(plan, RoomRole.Bunks, z[2]);
        }

        // A warehouse or a workshop: the floor open from wall to wall, with an office, a restroom and a store room boxed in at
        // the corner by the door.
        private static void Works(InteriorPlan plan, Rect m, bool east, RoomRole role, System.Random rng)
        {
            float w = m.width, d = m.height;
            if (w * d < 70f || w < 8f)
            {
                Add(plan, role, m);
                return;
            }
            float cw = Mathf.Clamp(w * 0.22f, 3.2f, 5.2f);
            Rect[] x = SplitX(m, east, 3.2f, cw, 0f);
            Rect corner = x[0];
            if (d >= 3.4f + 2.4f + 2.8f + 4f)
            {
                Rect[] c = SplitZ(corner, false, 2.2f, 3.4f, 2.4f, 2.8f, 0f);
                Add(plan, RoomRole.Office, c[0]);
                Add(plan, RoomRole.Restroom, c[1]);
                Add(plan, RoomRole.Stock, c[2]);
                // The rest of the corner strip stays part of the floor.
                PlanRoom floor = Add(plan, role, x[1]);
                PlanRoom rest = Add(plan, role, c[3]);
                OpenLater(plan, floor, rest);
            }
            else if (d >= 3.4f + 2.4f)
            {
                Rect[] c = SplitZ(corner, false, 2.2f, 0f, 2.4f);
                Add(plan, RoomRole.Office, c[0]);
                Add(plan, RoomRole.Restroom, c[1]);
                Add(plan, role, x[1]);
            }
            else
            {
                Add(plan, RoomRole.Office, corner);
                Add(plan, role, x[1]);
            }
        }

        // A barn: the floor of stalls and hay, with a tack and feed room across its far end.
        private static void Barn(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            float w = m.width, d = m.height;
            bool alongX = w >= d;
            float len = alongX ? w : d, across = alongX ? d : w;
            if (w * d < 60f || len < 8f || across < 4f)
            {
                Add(plan, RoomRole.Barn, m);
                return;
            }
            Rect[] s = alongX ? SplitX(m, !east, 3f, 3f, 0f) : SplitZ(m, true, 3f, 3f, 0f);
            Add(plan, RoomRole.Barn, s[1]);
            if (across >= 6f)
            {
                Rect[] t = alongX ? SplitZ(s[0], false, 2.6f, -0.5f, 0f) : SplitX(s[0], east, 2.6f, -0.5f, 0f);
                Add(plan, RoomRole.Stock, t[0]).Name = "Tack Room";
                Add(plan, RoomRole.Stock, t[1]).Name = "Feed Room";
            }
            else Add(plan, RoomRole.Stock, s[0]).Name = "Tack Room";
        }
        #endregion

        #region Offices and the like
        // An office building — or a bank, a post office, a library, a town hall, a clinic, a police station, a funeral home: a
        // lobby at the door (a waiting room, a banking hall, the library itself) and the rooms off a corridor from it, given out
        // nearest the door first in the order that kind of place has them.
        private static void Offices(InteriorPlan plan, Rect m, Vector2 door, System.Random rng)
        {
            float w = m.width, d = m.height, area = w * d;
            bool east = door.x > m.center.x;
            RoomRole front = FrontRole(plan.Use);
            RoomRole[] order = BayRoles(plan.Use);
            if (area < 45f)
            {
                Add(plan, order[0] == RoomRole.Restroom ? RoomRole.Office : order[0], m);
                return;
            }
            if (area < 115f || d < 7f || w < 6f)
            {
                Rect[] z = d >= 6f ? SplitZ(m, false, 2.6f, -0.55f, 0f) : SplitX(m, east, 2.6f, -0.6f, 0f);
                Add(plan, front, z[0]);
                bool deep = d >= 6f;
                float len = deep ? z[1].width : z[1].height;
                if (len >= 2.8f + 2.3f)
                {
                    Rect[] b = deep ? SplitX(z[1], !east, 2.2f, 2.3f, 0f) : SplitZ(z[1], true, 2.2f, 2.3f, 0f);
                    Add(plan, RoomRole.Restroom, b[0]);
                    Add(plan, order[0] == RoomRole.Restroom ? order[1] : order[0], b[1]);
                }
                else Add(plan, order[0] == RoomRole.Restroom ? order[1] : order[0], z[1]);
                return;
            }
            var bays = new List<Bay>(16);
            float bay = plan.Use == InteriorUse.Hospital ? 6.5f : 4.2f;
            if (!Corridor(plan, m, door, front, Mathf.Clamp(Mathf.Min(w, d) * 0.3f, 3.2f, 6f), bay, bays))
            {
                Rect[] z = SplitZ(m, false, 2.6f, -0.55f, 0f);
                Add(plan, front, z[0]);
                Add(plan, order[0], z[1]);
                return;
            }
            bool restroom = false, cells = false, vault = false;
            var taken = new bool[bays.Count];
            int next = 0;
            for (int i = 0; i < bays.Count; i++)
            {
                if (taken[i]) continue;
                RoomRole role = RoleAt(order, Tail(plan.Use), next++);
                float a = bays[i].R.width * bays[i].R.height;
                if (role == RoomRole.Restroom && (restroom || a > 30f)) role = RoomRole.Office;
                if (role == RoomRole.Cells && cells) role = RoomRole.Office;
                if (role == RoomRole.Vault && (vault || a > 25f)) role = RoomRole.Records;
                restroom |= role == RoomRole.Restroom;
                cells |= role == RoomRole.Cells;
                vault |= role == RoomRole.Vault;
                // A room that wants more floor takes the bays next to it along the corridor too.
                taken[i] = true;
                Rect r = bays[i].R;
                for (int k = 1, want = BaysFor(role, a); k < want; k++)
                {
                    int j = Neighbour(bays, taken, i, k);
                    if (j < 0) break;
                    taken[j] = true;
                    r = Rect.MinMaxRect(Mathf.Min(r.xMin, bays[j].R.xMin), Mathf.Min(r.yMin, bays[j].R.yMin),
                        Mathf.Max(r.xMax, bays[j].R.xMax), Mathf.Max(r.yMax, bays[j].R.yMax));
                }
                Add(plan, role, r);
            }
        }

        // How many bays of about `bay` m² a room of a role takes.
        private static int BaysFor(RoomRole role, float bay)
        {
            switch (role)
            {
                case RoomRole.Squad: case RoomRole.OpenOffice: case RoomRole.Ward: case RoomRole.Meeting: case RoomRole.Cells:
                    return bay < 25f ? 3 : 2;
                case RoomRole.BreakRoom: case RoomRole.Records: case RoomRole.Stacks: case RoomRole.Exam:
                    return bay < 14f ? 2 : 1;
                default:
                    return 1;
            }
        }

        private static RoomRole RoleAt(RoomRole[] head, RoomRole[] tail, int i) => i < head.Length ? head[i] : tail[(i - head.Length) % tail.Length];

        private static RoomRole FrontRole(InteriorUse use)
        {
            switch (use)
            {
                case InteriorUse.Clinic: case InteriorUse.Hospital: return RoomRole.Waiting;
                case InteriorUse.Library: return RoomRole.Stacks;
                case InteriorUse.Bank: case InteriorUse.PostOffice: case InteriorUse.Police: return RoomRole.Lobby;
                case InteriorUse.Funeral: return RoomRole.Narthex;
                case InteriorUse.Motel: return RoomRole.Lobby;
                default: return RoomRole.Lobby;
            }
        }

        // The rooms off a corridor, nearest the lobby first; after these, Tail's in turn.
        private static RoomRole[] BayRoles(InteriorUse use)
        {
            switch (use)
            {
                case InteriorUse.Bank: return new[] { RoomRole.Office, RoomRole.Vault, RoomRole.Restroom, RoomRole.Office, RoomRole.Records, RoomRole.BreakRoom };
                case InteriorUse.PostOffice: return new[] { RoomRole.Sorting, RoomRole.Office, RoomRole.Restroom, RoomRole.Stock };
                case InteriorUse.Library: return new[] { RoomRole.Stacks, RoomRole.Office, RoomRole.Restroom, RoomRole.Records };
                case InteriorUse.Clinic: return new[] { RoomRole.Exam, RoomRole.Exam, RoomRole.Office, RoomRole.Restroom, RoomRole.Dispensary };
                case InteriorUse.Hospital: return new[] { RoomRole.Exam, RoomRole.Ward, RoomRole.Restroom, RoomRole.Office, RoomRole.Dispensary };
                case InteriorUse.Police: return new[] { RoomRole.Squad, RoomRole.Office, RoomRole.Cells, RoomRole.Armory, RoomRole.Restroom, RoomRole.Records, RoomRole.BreakRoom };
                case InteriorUse.Funeral: return new[] { RoomRole.Chapel, RoomRole.Office, RoomRole.Restroom, RoomRole.Stock };
                case InteriorUse.Civic: return new[] { RoomRole.Meeting, RoomRole.Office, RoomRole.Restroom, RoomRole.Office, RoomRole.Records, RoomRole.BreakRoom };
                case InteriorUse.Motel: return new[] { RoomRole.Office, RoomRole.Guest };
                default: return new[] { RoomRole.Office, RoomRole.Office, RoomRole.Restroom, RoomRole.OpenOffice, RoomRole.Records, RoomRole.BreakRoom };
            }
        }

        // What the rest of a corridor's rooms are, in turn.
        private static RoomRole[] Tail(InteriorUse use)
        {
            switch (use)
            {
                case InteriorUse.Clinic: return new[] { RoomRole.Exam, RoomRole.Exam, RoomRole.Office, RoomRole.Records };
                case InteriorUse.Hospital: return new[] { RoomRole.Ward, RoomRole.Exam, RoomRole.Office, RoomRole.Stock };
                case InteriorUse.Police: return new[] { RoomRole.Office, RoomRole.Records, RoomRole.Office, RoomRole.Stock };
                case InteriorUse.Motel: return new[] { RoomRole.Guest };
                case InteriorUse.Civic: return new[] { RoomRole.Office, RoomRole.Meeting, RoomRole.Office, RoomRole.Records, RoomRole.Stock };
                default: return new[] { RoomRole.Office, RoomRole.OpenOffice, RoomRole.Office, RoomRole.Records, RoomRole.Stock };
            }
        }
        #endregion

        #region Finishes
        private static readonly WallDesign Panelled = new WallDesign
        {
            Key = "kypanelled", Wainscot = new Color(0.34f, 0.22f, 0.13f), Paper = new Color(0.44f, 0.30f, 0.19f), Trim = new Color(0.22f, 0.14f, 0.08f),
            Lower = WainscotStyle.Panels, Upper = PaperStyle.Pinstripe,
        };
        private static readonly WallDesign KitchenYellow = new WallDesign
        {
            Key = "kykitchenyellow", Wainscot = new Color(0.80f, 0.76f, 0.62f), Paper = new Color(0.84f, 0.76f, 0.46f), Trim = new Color(0.40f, 0.32f, 0.20f),
            Lower = WainscotStyle.Paint, Upper = PaperStyle.Pinstripe, RailHeight = 1.2f,
        };
        private static readonly WallDesign BathPink = new WallDesign
        {
            Key = "kybathpink", Wainscot = new Color(0.78f, 0.60f, 0.60f), Paper = new Color(0.86f, 0.82f, 0.78f), Trim = new Color(0.50f, 0.40f, 0.40f),
            Lower = WainscotStyle.Paint, Upper = PaperStyle.Plain, RailHeight = 1.3f,
        };
        private static readonly WallDesign ShopCream = new WallDesign
        {
            Key = "kyshopcream", Wainscot = new Color(0.36f, 0.30f, 0.24f), Paper = new Color(0.84f, 0.80f, 0.68f), Trim = new Color(0.24f, 0.18f, 0.12f),
            Lower = WainscotStyle.Panels, Upper = PaperStyle.Plain,
        };
        private static readonly WallDesign ChurchWhite = new WallDesign
        {
            Key = "kychurch", Wainscot = new Color(0.30f, 0.20f, 0.12f), Paper = new Color(0.86f, 0.84f, 0.78f), Trim = new Color(0.22f, 0.14f, 0.08f),
            Lower = WainscotStyle.Panels, Upper = PaperStyle.Plain, RailHeight = 1.0f,
        };
        private static readonly WallDesign SchoolCream = new WallDesign
        {
            Key = "kyschool", Wainscot = new Color(0.40f, 0.50f, 0.44f), Paper = new Color(0.82f, 0.80f, 0.70f), Trim = new Color(0.28f, 0.30f, 0.28f),
            Lower = WainscotStyle.Paint, Upper = PaperStyle.Plain, RailHeight = 1.2f,
        };
        private static readonly WallDesign CellBlock = new WallDesign
        {
            Key = "kycells", Wainscot = new Color(0.46f, 0.50f, 0.48f), Paper = new Color(0.70f, 0.72f, 0.68f), Trim = new Color(0.30f, 0.32f, 0.30f),
            Lower = WainscotStyle.Paint, Upper = PaperStyle.Plain, RailHeight = 1.3f,
        };

        private static readonly Color[] CarpetTints =
        {
            new Color(0.36f, 0.30f, 0.22f), new Color(0.26f, 0.30f, 0.36f), new Color(0.40f, 0.24f, 0.18f), new Color(0.34f, 0.36f, 0.20f),
            new Color(0.42f, 0.30f, 0.30f), new Color(0.30f, 0.34f, 0.32f),
        };
        private static readonly Color[] OfficeCarpets = { new Color(0.30f, 0.32f, 0.36f), new Color(0.24f, 0.28f, 0.34f), new Color(0.32f, 0.30f, 0.28f) };

        // Every room's wallpaper, floor, fitting and whether its light was left on, by what it's for and what the building is.
        /// <summary>Assign finishes, lighting and design to each room based on its role and building type.</summary>
        private static void Style(InteriorPlan plan, System.Random rng)
        {
            bool bare = plan.Use == InteriorUse.Warehouse || plan.Use == InteriorUse.Workshop || plan.Use == InteriorUse.Barn
                || plan.Use == InteriorUse.Shed || plan.Use == InteriorUse.Greenhouse || plan.Use == InteriorUse.Garage;
            bool home = plan.Kind == RaidBuildingKind.House;
            WallDesign homeLiving = Pick(rng, RaidLayouts.Mustard, RaidLayouts.TealBeige, RaidLayouts.Rose, RaidLayouts.BrownGreen, Panelled);
            WallDesign homeWet = Pick(rng, RaidLayouts.KitchenMint, BathPink, RaidLayouts.ClinicGreen);
            WallDesign kitchen = Pick(rng, RaidLayouts.KitchenMint, KitchenYellow, KitchenYellow);
            WallDesign work = Pick(rng, RaidLayouts.OfficeBlue, RaidLayouts.BrownGreen, RaidLayouts.NavyGrey, Panelled);
            WallDesign shop = Pick(rng, RaidLayouts.StoreRed, ShopCream, RaidLayouts.OfficeBlue, RaidLayouts.TealBeige);
            double lit = home ? 0.28 : plan.Kind == RaidBuildingKind.Store ? 0.42 : 0.3;
            foreach (PlanRoom r in plan.Rooms)
            {
                r.Light = LightStyle.Panel;
                r.Lit = rng.NextDouble() < lit;
                r.Flicker = !home && rng.NextDouble() < 0.2;
                switch (r.Role)
                {
                    case RoomRole.Living: case RoomRole.Guest:
                        r.Design = r.Unit >= 0 ? Pick(rng, RaidLayouts.Mustard, RaidLayouts.TealBeige, RaidLayouts.Rose, RaidLayouts.BrownGreen) : homeLiving;
                        r.Floor = rng.NextDouble() < 0.45 ? FloorKind.Wood : FloorKind.Carpet;
                        r.FloorTint = Pick(rng, CarpetTints);
                        r.Light = LightStyle.Dome;
                        break;
                    case RoomRole.Dining:
                        r.Design = Pick(rng, RaidLayouts.Rose, RaidLayouts.TealBeige, homeLiving);
                        r.Floor = FloorKind.Wood;
                        r.Light = LightStyle.Dome;
                        break;
                    case RoomRole.Kitchen:
                        r.Design = kitchen;
                        r.Floor = Pick(rng, FloorKind.Checker, FloorKind.LinoBeige, FloorKind.LinoGreen);
                        r.Light = LightStyle.Dome;
                        break;
                    case RoomRole.Bathroom:
                        r.Design = homeWet;
                        r.Floor = Pick(rng, FloorKind.LinoGreen, FloorKind.Checker, FloorKind.LinoBeige);
                        r.Light = LightStyle.Bulb;
                        r.Lit = rng.NextDouble() < 0.15;
                        break;
                    case RoomRole.Bedroom:
                        r.Design = Pick(rng, RaidLayouts.Rose, RaidLayouts.BrownGreen, RaidLayouts.TealBeige, RaidLayouts.NavyGrey);
                        r.Floor = rng.NextDouble() < 0.72 ? FloorKind.Carpet : FloorKind.Wood;
                        r.FloorTint = Pick(rng, CarpetTints);
                        r.Light = LightStyle.Dome;
                        r.Lit = rng.NextDouble() < 0.18;
                        break;
                    case RoomRole.Hall:
                        r.Design = home ? homeLiving : plan.Use == InteriorUse.School ? SchoolCream : Pick(rng, RaidLayouts.TealBeige, RaidLayouts.OfficeBlue);
                        r.Floor = home ? (rng.NextDouble() < 0.5 ? FloorKind.Wood : FloorKind.Carpet) : FloorKind.LinoGrey;
                        r.FloorTint = Pick(rng, CarpetTints);
                        r.Light = home ? LightStyle.Dome : LightStyle.Panel;
                        break;
                    case RoomRole.Den:
                        r.Design = Pick(rng, Panelled, RaidLayouts.BrownGreen, RaidLayouts.NavyGrey);
                        r.Floor = rng.NextDouble() < 0.6 ? FloorKind.Carpet : FloorKind.WoodDark;
                        r.FloorTint = Pick(rng, CarpetTints);
                        r.Light = LightStyle.Dome;
                        break;
                    case RoomRole.Laundry: case RoomRole.Closet:
                        r.Design = home ? RaidLayouts.GarageGrey : RaidLayouts.GarageGrey;
                        r.Floor = r.Role == RoomRole.Closet ? FloorKind.Wood : FloorKind.LinoGrey;
                        r.Light = LightStyle.Bulb;
                        r.Lit = rng.NextDouble() < 0.1;
                        break;
                    case RoomRole.HomeGarage: case RoomRole.Bays:
                        r.Design = bare ? null : RaidLayouts.GarageGrey;
                        r.Floor = FloorKind.Concrete;
                        r.Light = LightStyle.ShopLamp;
                        break;
                    case RoomRole.Sales:
                        r.Design = plan.Use == InteriorUse.Pharmacy ? RaidLayouts.ClinicGreen : plan.Use == InteriorUse.AutoShop ? RaidLayouts.GarageGrey : shop;
                        r.Floor = Pick(rng, FloorKind.LinoBeige, FloorKind.Checker, FloorKind.LinoGrey);
                        break;
                    case RoomRole.Stock: case RoomRole.Vault: case RoomRole.Records: case RoomRole.Sorting: case RoomRole.Dispensary:
                        r.Design = bare && r.Role == RoomRole.Stock ? null : RaidLayouts.GarageGrey;
                        r.Floor = r.Role == RoomRole.Stock || r.Role == RoomRole.Vault ? (plan.Use == InteriorUse.Barn ? FloorKind.Plywood : FloorKind.Concrete) : FloorKind.LinoGrey;
                        r.Light = r.R.width * r.R.height > 30f ? LightStyle.ShopLamp : LightStyle.Bulb;
                        r.Lit = rng.NextDouble() < 0.15;
                        break;
                    case RoomRole.BackOffice: case RoomRole.Office: case RoomRole.OpenOffice: case RoomRole.Squad:
                        r.Design = work;
                        r.Floor = rng.NextDouble() < 0.55 ? FloorKind.Carpet : FloorKind.LinoBeige;
                        r.FloorTint = Pick(rng, OfficeCarpets);
                        break;
                    case RoomRole.Restroom:
                        r.Design = Pick(rng, RaidLayouts.KitchenMint, RaidLayouts.ClinicGreen);
                        r.Floor = Pick(rng, FloorKind.Checker, FloorKind.LinoGrey);
                        r.Light = LightStyle.Bulb;
                        r.Lit = rng.NextDouble() < 0.12;
                        break;
                    case RoomRole.Lobby: case RoomRole.Waiting:
                        r.Design = r.Role == RoomRole.Waiting ? RaidLayouts.ClinicGreen : plan.Use == InteriorUse.School ? SchoolCream
                            : Pick(rng, RaidLayouts.TealBeige, RaidLayouts.OfficeBlue, Panelled);
                        r.Floor = Pick(rng, FloorKind.LinoGrey, FloorKind.Checker, FloorKind.LinoGreen);
                        break;
                    case RoomRole.BreakRoom: case RoomRole.DayRoom:
                        r.Design = Pick(rng, RaidLayouts.KitchenMint, RaidLayouts.BrownGreen);
                        r.Floor = Pick(rng, FloorKind.LinoBeige, FloorKind.Checker);
                        break;
                    case RoomRole.Stacks: case RoomRole.Meeting:
                        r.Design = Pick(rng, RaidLayouts.BrownGreen, Panelled, RaidLayouts.TealBeige);
                        r.Floor = rng.NextDouble() < 0.6 ? FloorKind.Carpet : FloorKind.Wood;
                        r.FloorTint = Pick(rng, new Color(0.24f, 0.32f, 0.24f), new Color(0.40f, 0.18f, 0.16f), new Color(0.28f, 0.30f, 0.36f));
                        break;
                    case RoomRole.Eatery:
                        r.Design = Pick(rng, RaidLayouts.StoreRed, RaidLayouts.Mustard, RaidLayouts.Rose, RaidLayouts.TealBeige);
                        r.Floor = rng.NextDouble() < 0.6 ? FloorKind.Checker : FloorKind.Wood;
                        break;
                    case RoomRole.CommercialKitchen:
                        r.Design = Pick(rng, RaidLayouts.ClinicGreen, RaidLayouts.KitchenMint);
                        r.Floor = FloorKind.LinoGrey;
                        break;
                    case RoomRole.Bar:
                        r.Design = Pick(rng, Panelled, RaidLayouts.Rose, RaidLayouts.BrownGreen);
                        r.Floor = FloorKind.WoodDark;
                        r.Light = LightStyle.Bulb;
                        r.Lit = rng.NextDouble() < 0.5;
                        break;
                    case RoomRole.Exam: case RoomRole.Ward:
                        r.Design = RaidLayouts.ClinicGreen;
                        r.Floor = FloorKind.LinoGreen;
                        break;
                    case RoomRole.Cells:
                        r.Design = CellBlock;
                        r.Floor = FloorKind.Concrete;
                        r.Lit = rng.NextDouble() < 0.6;
                        r.Flicker = rng.NextDouble() < 0.4;
                        break;
                    case RoomRole.Armory:
                        r.Design = RaidLayouts.NavyGrey;
                        r.Floor = FloorKind.Concrete;
                        r.Light = LightStyle.ShopLamp;
                        break;
                    case RoomRole.Bunks:
                        r.Design = Pick(rng, RaidLayouts.NavyGrey, RaidLayouts.BrownGreen);
                        r.Floor = rng.NextDouble() < 0.5 ? FloorKind.LinoGrey : FloorKind.Carpet;
                        r.FloorTint = Pick(rng, OfficeCarpets);
                        break;
                    case RoomRole.Narthex: case RoomRole.Nave: case RoomRole.Chapel:
                        r.Design = plan.Use == InteriorUse.Funeral ? RaidLayouts.Rose : ChurchWhite;
                        r.Floor = r.Role == RoomRole.Narthex ? FloorKind.Wood : rng.NextDouble() < 0.55 ? FloorKind.Carpet : FloorKind.Wood;
                        r.FloorTint = Pick(rng, new Color(0.42f, 0.14f, 0.14f), new Color(0.36f, 0.12f, 0.16f), new Color(0.24f, 0.28f, 0.40f));
                        r.Light = LightStyle.Dome;
                        r.Lit = rng.NextDouble() < 0.2;
                        break;
                    case RoomRole.Sacristy:
                        r.Design = Panelled;
                        r.Floor = FloorKind.Carpet;
                        r.FloorTint = Pick(rng, CarpetTints);
                        r.Light = LightStyle.Dome;
                        break;
                    case RoomRole.Classroom:
                        r.Design = Pick(rng, SchoolCream, RaidLayouts.ClinicGreen, RaidLayouts.OfficeBlue);
                        r.Floor = Pick(rng, FloorKind.LinoBeige, FloorKind.LinoGreen, FloorKind.LinoGrey);
                        break;
                    case RoomRole.Gym:
                        r.Design = SchoolCream;
                        r.Floor = FloorKind.Wood;
                        r.Light = LightStyle.ShopLamp;
                        break;
                    case RoomRole.Warehouse: case RoomRole.Workshop: case RoomRole.Barn: case RoomRole.Greenhouse: case RoomRole.Shed:
                        r.Design = bare ? null : RaidLayouts.GarageGrey;
                        r.Floor = r.Role == RoomRole.Barn || r.Role == RoomRole.Shed ? FloorKind.Plywood : FloorKind.Concrete;
                        r.Light = r.Role == RoomRole.Shed || r.R.width * r.R.height < 30f ? LightStyle.Bulb : LightStyle.ShopLamp;
                        r.Lit = rng.NextDouble() < 0.2;
                        break;
                    default:
                        r.Design = work;
                        r.Floor = FloorKind.LinoGrey;
                        break;
                }
            }
        }

        private static T Pick<T>(System.Random rng, params T[] options) => options[rng.Next(options.Length)];

        /// <summary>The <see cref="RoomKinds"/> kind of a room, as the loot tables know it.</summary>
        internal static string KindFor(RoomRole role)
        {
            switch (role)
            {
                case RoomRole.Living: return RoomKinds.Living;
                case RoomRole.Dining: case RoomRole.Kitchen: case RoomRole.CommercialKitchen: return RoomKinds.Kitchen;
                case RoomRole.Bathroom: case RoomRole.Restroom: return RoomKinds.Bathroom;
                case RoomRole.Bedroom: case RoomRole.Guest: case RoomRole.Bunks: return RoomKinds.Bedroom;
                case RoomRole.Hall: case RoomRole.Lobby: case RoomRole.Narthex: case RoomRole.Waiting: case RoomRole.Meeting: return RoomKinds.Hall;
                case RoomRole.Den: case RoomRole.BackOffice: case RoomRole.Office: case RoomRole.OpenOffice: case RoomRole.Squad:
                case RoomRole.Sacristy: case RoomRole.Stacks: return RoomKinds.Office;
                case RoomRole.Laundry: case RoomRole.Closet: case RoomRole.Records: case RoomRole.Vault: case RoomRole.Sorting: case RoomRole.Armory:
                case RoomRole.Shed: return RoomKinds.Storage;
                case RoomRole.HomeGarage: case RoomRole.Bays: return RoomKinds.Garage;
                case RoomRole.Sales: case RoomRole.Eatery: case RoomRole.Bar: return RoomKinds.Sales;
                case RoomRole.Stock: case RoomRole.Dispensary: return RoomKinds.Stock;
                case RoomRole.BreakRoom: case RoomRole.DayRoom: return RoomKinds.Kitchenette;
                case RoomRole.Exam: case RoomRole.Ward: return RoomKinds.Ward;
                case RoomRole.Cells: return RoomKinds.Cells;
                case RoomRole.Nave: case RoomRole.Chapel: return RoomKinds.Nave;
                case RoomRole.Classroom: return RoomKinds.Classroom;
                case RoomRole.Workshop: return RoomKinds.Workshop;
                default: return RoomKinds.Open;
            }
        }

        private static string NameFor(RoomRole role, InteriorUse use)
        {
            switch (role)
            {
                case RoomRole.Living: return "Living Room";
                case RoomRole.Dining: return "Dining Room";
                case RoomRole.Kitchen: return "Kitchen";
                case RoomRole.Bathroom: return "Bathroom";
                case RoomRole.Bedroom: return "Bedroom";
                case RoomRole.Hall: return "Hall";
                case RoomRole.Den: return "Den";
                case RoomRole.Laundry: return "Laundry";
                case RoomRole.Closet: return "Pantry";
                case RoomRole.HomeGarage: return "Garage";
                case RoomRole.Sales: return use == InteriorUse.Pharmacy ? "Pharmacy" : use == InteriorUse.Salon ? "Salon" : "Sales Floor";
                case RoomRole.Stock: return "Stock Room";
                case RoomRole.BackOffice: return "Back Office";
                case RoomRole.Restroom: return "Restroom";
                case RoomRole.Lobby: return use == InteriorUse.Bank ? "Banking Hall" : "Lobby";
                case RoomRole.Office: return use == InteriorUse.Police ? "Chief's Office" : "Office";
                case RoomRole.OpenOffice: return "Office Floor";
                case RoomRole.Records: return "Records";
                case RoomRole.BreakRoom: return use == InteriorUse.Apartments || use == InteriorUse.Motel ? "Common Room" : "Break Room";
                case RoomRole.Vault: return "Vault";
                case RoomRole.Sorting: return "Sorting Room";
                case RoomRole.Stacks: return "Library";
                case RoomRole.Meeting: return use == InteriorUse.Church ? "Fellowship Hall" : "Meeting Room";
                case RoomRole.Eatery: return "Dining Room";
                case RoomRole.CommercialKitchen: return "Kitchen";
                case RoomRole.Bar: return "Bar";
                case RoomRole.Waiting: return "Waiting Room";
                case RoomRole.Exam: return "Exam Room";
                case RoomRole.Ward: return "Ward";
                case RoomRole.Dispensary: return "Dispensary";
                case RoomRole.Squad: return "Squad Room";
                case RoomRole.Cells: return "Cells";
                case RoomRole.Armory: return "Evidence Room";
                case RoomRole.Bays: return use == InteriorUse.FireStation ? "Apparatus Bay" : "Service Bay";
                case RoomRole.DayRoom: return "Day Room";
                case RoomRole.Bunks: return "Bunk Room";
                case RoomRole.Narthex: return use == InteriorUse.Funeral ? "Foyer" : "Vestibule";
                case RoomRole.Nave: return "Sanctuary";
                case RoomRole.Sacristy: return use == InteriorUse.Funeral ? "Arrangement Room" : "Pastor's Study";
                case RoomRole.Chapel: return "Chapel";
                case RoomRole.Classroom: return "Classroom";
                case RoomRole.Gym: return "Gymnasium";
                case RoomRole.Warehouse: return "Warehouse";
                case RoomRole.Workshop: return "Workshop";
                case RoomRole.Barn: return "Barn";
                case RoomRole.Greenhouse: return "Greenhouse";
                case RoomRole.Shed: return use == InteriorUse.Garage ? "Garage" : "Shed";
                case RoomRole.Guest: return "Room";
                default: return "Room";
            }
        }
        #endregion
    }
}
