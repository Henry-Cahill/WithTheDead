using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;
using static CloneZ.StreetscapePass;

namespace CloneZ
{
    /// <summary>
    /// Traffic control and road signs the way a Kentucky town had them in 1993: signals hung from a span wire strung
    /// diagonally across the junction between two strain poles, one head facing each approach (left on night flash: amber
    /// for the main road, red for the side roads); stop and yield signs where they're mapped (facing the traffic they hold,
    /// on its right) and otherwise on the minor approaches (the side street of a T, the smaller road at a crossing, now and
    /// then an all-way stop); green street-name blades with the real names at the corners; US, KY and Interstate shields
    /// with their direction past the junctions; speed limits; one-way and do-not-enter signs on the one-way streets; deer
    /// crossings out in the country. Faces come from the shared sign atlas and the site's own (<see cref="NameAtlas"/>).
    /// </summary>
    internal static class SignPass
    {
        private const int SaltSignal = 0x5161, SaltStop = 0x5709, SaltRule = 0x7A11, SaltPost = 0x9057, SaltSpeed = 0x5BE3,
            SaltDeer = 0xDEE2, SaltBlade = 0xB1AD;
        private static readonly Color SignalYellow = new Color(0.82f, 0.64f, 0.10f);
        private static readonly Color SignalGreen = new Color(0.12f, 0.22f, 0.15f);
        private static readonly Color PostGrey = new Color(0.56f, 0.58f, 0.60f);
        private static readonly Color PostWood = new Color(0.40f, 0.31f, 0.22f);
        private static Surf SignBack => DecorPalette.Metal(new Color(0.58f, 0.59f, 0.60f));

        /// <summary>The span-wire signals at the mapped signals' junctions.</summary>
        internal static void Signals(Ctx ctx)
        {
            var done = new HashSet<Crossing>();
            foreach (Node n in ctx.Site.Area.Nodes)
            {
                if (n.Data.kind != "traffic_signals" || !ctx.Inside(n.At)) continue;
                Crossing c = ctx.NearestCrossing(n.At, 18f);
                if (c == null || !done.Add(c) || !ctx.Inside(c.At)) continue;
                SpanWire(ctx, c);
                c.Controlled = true;
            }
        }

        /// <summary>The signs: mapped stops and yields, then the crossings' own (stops, blades), shields, limits, one-way
        /// signs, deer crossings.</summary>
        internal static IEnumerable<string> Signs(Ctx ctx)
        {
            Site site = ctx.Site;
            MappedStops(ctx);
            foreach (Crossing c in ctx.Crossings)
            {
                if (!ctx.Inside(c.At)) continue;
                if (!c.Controlled) Stops(ctx, c);
                if (!c.Named) Blades(ctx, c, null);
                OneWays(ctx, c);
                if (site.StepDue) yield return "signs";
            }
            foreach (Crossing c in ctx.Crossings)
            {
                if (!ctx.Inside(c.At)) continue;
                RouteSigns(ctx, c);
                if (site.StepDue) yield return "signs";
            }
            Deer(ctx);
        }

        #region Signals
        private static void SpanWire(Ctx ctx, Crossing c)
        {
            var arms = new List<int>();
            for (int k = 0; k < c.Arms.Count; k++) if (RoadRank(c.Arms[k].Street) >= 1) arms.Add(k);
            if (arms.Count < 2) return;
            arms.Sort((x, y) =>
            {
                Vector2 a = c.Arms[x].Dir, b = c.Arms[y].Dir;
                int r = Mathf.Atan2(a.y, a.x).CompareTo(Mathf.Atan2(b.y, b.x));
                return r != 0 ? r : x.CompareTo(y);
            });
            // The kerb corners between neighbouring arms; the span runs between the two most nearly opposite.
            var corners = new List<Vector2>();
            for (int i = 0; i < arms.Count; i++)
            {
                int ka = arms[i], kb = arms[(i + 1) % arms.Count];
                JunctionArm a = c.Arms[ka], b = c.Arms[kb];
                float angA = Mathf.Atan2(a.Dir.y, a.Dir.x), angB = Mathf.Atan2(b.Dir.y, b.Dir.x);
                float gap = Mathf.Repeat(angB - angA, Mathf.PI * 2f);
                if (gap < 1e-3f) gap = Mathf.PI * 2f;
                float mid = angA + gap * 0.5f;
                var bis = new Vector2(Mathf.Cos(mid), Mathf.Sin(mid));
                // The corner lies left of arm a (anticlockwise from it) and right of arm b.
                float off = Mathf.Max(Roadside(a.Street, c.ApproachRight(ka)), Roadside(b.Street, -c.ApproachRight(kb)));
                float half = Mathf.Max(0.35f, Mathf.Sin(Mathf.Min(gap, Mathf.PI) * 0.5f));
                float r = Mathf.Clamp(off / half + 0.5f, 4f, 22f);
                corners.Add(c.At + bis * r);
            }
            int ia = 0, ib = 1;
            float far = -1f;
            for (int i = 0; i < corners.Count; i++)
                for (int j = i + 1; j < corners.Count; j++)
                {
                    float d = (corners[i] - corners[j]).sqrMagnitude;
                    if (d > far) { far = d; ia = i; ib = j; }
                }
            Vector2 p1 = Clear(ctx, corners[ia], (corners[ia] - c.At).normalized), p2 = Clear(ctx, corners[ib], (corners[ib] - c.At).normalized);
            long key = ctx.KeyOf(c.At);
            bool wood = MapStyle.Unit(key, SaltSignal) < 0.6f;
            const float attach = 7.7f, poleH = 8.6f;
            StrainPole(ctx, p1, poleH, wood, (p1 - c.At).normalized);
            StrainPole(ctx, p2, poleH, wood, (p2 - c.At).normalized);
            Vector3 a3 = To3(p1, attach), b3 = To3(p2, attach);
            float span = Vector2.Distance(p1, p2), sag = 0.25f + span * 0.025f;
            ctx.Wires.Add(a3, b3, sag, 0.014f, WireBlack, false);
            ctx.Wires.Add(a3 + Vector3.down * 0.5f, b3 + Vector3.down * 0.5f, sag, 0.01f, WireBlack, false);

            bool yellow = MapStyle.Unit(key, SaltSignal + 1) < 0.65f;
            bool plates = MapStyle.Unit(key, SaltSignal + 2) < 0.5f;
            var hung = new List<Vector2>();
            foreach (int k in arms)
            {
                JunctionArm a = c.Arms[k];
                if (!Approaches(a)) continue;
                Vector2 v = -a.Dir, right = new Vector2(v.y, -v.x);
                float lane = a.Street.Data.oneway ? 0f : EdgeOf(a.Street, c.ApproachRight(k)) * 0.5f;
                Vector2 o = c.At + right * lane;
                // Where the span crosses the approach's lane, or the nearest the span comes to it.
                if (!Cross(p1, p2, o, a.Dir, out float s)) s = 0.5f;
                s = Mathf.Clamp(s, 0.12f, 0.88f);
                Vector2 h = Vector2.Lerp(p1, p2, s);
                foreach (Vector2 other in hung)
                    if ((other - h).sqrMagnitude < 0.5f) h += (p2 - p1).normalized * 0.7f;
                hung.Add(h);
                float wireY = attach - sag * 4f * s * (1f - s) - 0.5f;
                bool main = RoadRank(a.Street) >= c.TopRank;
                Head(ctx, h, wireY, a.Dir, yellow, plates, main);
            }
            Blades(ctx, c, p1);
        }

        // An approach: traffic comes into the junction along this arm (a one-way street leaving it has none).
        private static bool Approaches(JunctionArm a)
        {
            if (!a.Street.Data.oneway) return true;
            // One-way streets run the way their points do: traffic comes in from the previous point.
            int n = a.Street.Pts.Count;
            Vector2 toPrev = a.Vertex > 0 ? (a.Street.Pts[a.Vertex - 1] - a.Street.Pts[a.Vertex]).normalized : Vector2.zero;
            return a.Vertex > 0 && Vector2.Dot(toPrev, a.Dir) > 0.5f || (a.Vertex == 0 && n < 2);
        }

        // Where the line from o along d crosses the segment p1-p2, as a fraction along the segment.
        private static bool Cross(Vector2 p1, Vector2 p2, Vector2 o, Vector2 d, out float s)
        {
            Vector2 e = p2 - p1;
            float den = e.x * d.y - e.y * d.x;
            s = 0f;
            if (Mathf.Abs(den) < 1e-4f) return false;
            Vector2 w = o - p1;
            s = (w.x * d.y - w.y * d.x) / den;
            return s > 0f && s < 1f;
        }

        // A spot for a pole near p, pushed out from the junction until it's clear.
        private static Vector2 Clear(Ctx ctx, Vector2 p, Vector2 outward)
        {
            for (int k = 0; k < 6; k++)
            {
                Vector2 q = p + outward * k;
                if (ctx.Roadside(q, 0.4f)) return q;
            }
            return p;
        }

        private static void StrainPole(Ctx ctx, Vector2 at, float h, bool wood, Vector2 away)
        {
            Vector3 foot = To3(at, 0f);
            RaidMesh low = ctx.Low(at);
            if (wood)
            {
                low.Cylinder(foot, 0.16f, h, 7, Matte(PoleWoods[MapStyle.Pick(ctx.KeyOf(at), 0x3D21, PoleWoods.Length)]), Quaternion.identity, 0.72f);
                Vector3 top = To3(at, h - 0.6f), ground = To3(at + away * (h * 0.4f), 0.02f);
                ctx.Wires.Add(top, ground, 0.02f, 0.01f, new Color(0.5f, 0.5f, 0.5f), false);
                ctx.Low(at + away * (h * 0.4f)).Box(ground + new Vector3(0f, 0.03f, 0f), new Vector3(0.18f, 0.06f, 0.18f), Concrete);
            }
            else
            {
                low.Cylinder(foot, 0.3f, 0.2f, 8, Concrete);
                low.Cylinder(foot + new Vector3(0f, 0.2f, 0f), 0.15f, h - 0.2f, 8, Galvanised, Quaternion.identity, 0.65f);
            }
            ctx.High(at).Box(To3(at, 7.7f), new Vector3(0.26f, 0.12f, 0.26f), DarkMetal);
            ctx.Collider(at, 0f, new Vector3(0.34f, 4f, 0.34f), 0f);
            ctx.Site.Take(at, 0.5f);
        }

        // A three-lamp signal head hung from the span at h (wire at wireY), facing traffic coming along -face.
        private static void Head(Ctx ctx, Vector2 h, float wireY, Vector2 face, bool yellow, bool plate, bool main)
        {
            RaidMesh m = ctx.High(h);
            const float height = 1.06f;
            float top = wireY - 0.42f, mid = top - height * 0.5f;
            m.SetFrame(To3(h, mid), Yaw(face));
            Surf body = Gloss(yellow ? SignalYellow : SignalGreen);
            m.Box(Vector3.zero, new Vector3(0.36f, height, 0.26f), body);
            if (plate) m.Box(new Vector3(0f, 0f, -0.14f), new Vector3(0.64f, 1.32f, 0.02f), Matte(new Color(0.06f, 0.06f, 0.06f)));
            m.Box(new Vector3(0f, height * 0.5f + 0.015f, 0f), new Vector3(0.34f, 0.03f, 0.25f), SnowSurf);
            m.Box(new Vector3(0f, height * 0.5f + 0.21f, 0f), new Vector3(0.04f, 0.42f, 0.04f), DarkMetal);
            Quaternion along = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.34f - i * 0.34f;
                Surf lens;
                if (i == 0) lens = main ? Gloss(new Color(0.30f, 0.05f, 0.04f)) : new Surf(Blinkers.Red, Vector2.zero, 0f);
                else if (i == 1) lens = main ? new Surf(Blinkers.Amber, Vector2.zero, 0f) : Gloss(new Color(0.32f, 0.22f, 0.05f));
                else lens = Gloss(new Color(0.05f, 0.22f, 0.12f));
                m.Cylinder(new Vector3(0f, y, 0.125f), 0.12f, 0.02f, 10, lens, along);
                m.Box(new Vector3(0f, y + 0.14f, 0.24f), new Vector3(0.3f, 0.02f, 0.24f), body);
                m.Box(new Vector3(0f, y + 0.16f, 0.24f), new Vector3(0.28f, 0.015f, 0.22f), SnowSurf);
            }
            m.ResetFrame();
        }
        #endregion

        #region Stops and yields
        // The mapped stop and yield signs, moved off the carriageway to the right of the traffic they hold.
        private static void MappedStops(Ctx ctx)
        {
            foreach (Node n in ctx.Site.Area.Nodes)
            {
                string k = n.Data.kind;
                if (k != "stop" && k != "give_way" || !ctx.Inside(n.At)) continue;
                Street s = NearestRoad(ctx, n.At, 12f, out Vector2 on, out Vector2 along);
                if (s == null) continue;
                Crossing c = ctx.NearestCrossing(n.At, 40f);
                Vector2 facing;
                if (n.Data.dir >= 0)
                {
                    float r = n.Data.dir * Mathf.Deg2Rad;
                    facing = new Vector2(Mathf.Sin(r), Mathf.Cos(r));
                }
                else if (c != null && (c.At - n.At).sqrMagnitude > 0.01f) facing = (n.At - c.At).normalized;
                else facing = along;
                // Square to the road, facing the way the sign does.
                facing = Vector2.Dot(facing, along) >= 0f ? along : -along;
                Vector2 v = -facing, right = new Vector2(v.y, -v.x);
                int side = Vector2.Dot(Left(along), right) >= 0f ? 1 : -1;
                Vector2 at = on + right * Roadside(s, side);
                if (c != null && (c.At - at).sqrMagnitude < 25f) at += facing * 2f;
                if (!ctx.Site.FreeBeside(at, 0.3f)) continue;
                if (c != null) c.Controlled = true;
                Sign(ctx, at, facing, k == "stop" ? "stop" : "yield", c != null && !c.Named ? c : null, false);
            }
        }

        // A crossing's own stop or yield signs, on its minor approaches.
        private static void Stops(Ctx ctx, Crossing c)
        {
            var names = new List<string>(4);
            string through = null;
            int throughRank = -1, throughArms = 0;
            foreach (JunctionArm a in c.Arms)
            {
                int r = RoadRank(a.Street);
                if (r < 2) continue;
                string n = NameKey(a.Street);
                int count = 0;
                foreach (JunctionArm b in c.Arms) if (NameKey(b.Street) == n) count++;
                if (!names.Contains(n)) names.Add(n);
                if (r > throughRank || (r == throughRank && count > throughArms)
                    || (r == throughRank && count == throughArms && string.CompareOrdinal(n, through) < 0))
                {
                    through = n;
                    throughRank = r;
                    throughArms = count;
                }
            }
            if (names.Count < 2 || through == null) return;
            long key = ctx.KeyOf(c.At);
            float rule = MapStyle.Unit(key, SaltRule);
            bool rural = !ctx.Density.Town(c.At);
            bool allWay = throughRank <= 2 && names.Count >= 2 && c.Arms.Count >= 4 && rule < 0.2f;
            if (throughRank <= 2 && !allWay && rule > 0.86f) return;   // left uncontrolled
            c.Controlled = true;
            for (int k = 0; k < c.Arms.Count; k++)
            {
                JunctionArm a = c.Arms[k];
                if (RoadRank(a.Street) < 2 || !Approaches(a)) continue;
                bool minor = NameKey(a.Street) != through;
                if (!minor && !allWay) continue;
                string face = rural && throughRank >= 3 && MapStyle.Unit(key ^ a.Street.Data.id, SaltStop) < 0.12f ? "yield" : "stop";
                Vector2 at = ApproachSpot(ctx, c, k, out bool ok);
                if (!ok) continue;
                Sign(ctx, at, a.Dir, face, c.Named ? null : c, allWay && face == "stop");
            }
        }

        // Beside an approach, on the right of its traffic, level with the stop line the roads painted (or just behind
        // it), on the sidewalk or the verge.
        private static Vector2 ApproachSpot(Ctx ctx, Crossing c, int k, out bool ok)
        {
            JunctionArm a = c.Arms[k];
            float back = RoadPass.StopLine(c.Junction, c.ArmIndex[k], out _, out float distance) ? distance + 0.3f : c.Setback(k) + 1.5f;
            float off = Roadside(a.Street, c.ApproachRight(k));
            Vector2 right = Left(a.Dir);
            for (int step = 0; step < 5; step++)
            {
                Vector2 at = c.At + a.Dir * (back + step * 1.2f) + right * off;
                if (!ctx.Inside(at) || !ctx.Site.FreeBeside(at, 0.3f)) continue;
                ok = true;
                return at;
            }
            ok = false;
            return c.At;
        }

        // A stop or yield sign on its post, facing along <paramref name="facing"/>; the crossing's blades on top of it when
        // <paramref name="blades"/> is given.
        private static void Sign(Ctx ctx, Vector2 at, Vector2 facing, string face, Crossing blades, bool allWay)
        {
            bool wood = !ctx.Density.Town(at) && MapStyle.Unit(ctx.KeyOf(at), SaltPost) < 0.5f;
            float top = blades != null ? 3.35f : 2.75f;
            Post(ctx, at, top, wood);
            RaidMesh m = ctx.Low(at);
            float size = face == "stop" ? 0.76f : 0.9f;
            float y = 2.1f + size * 0.5f;
            Vector2 front = at + facing * 0.045f;
            Shaped(m, front, y, facing, size, face, face + "back");
            if (allWay)
            {
                Rect uv = SignArt.Cell("allway");
                Flat(m, front + facing * 0.004f, 2.1f - 0.13f, facing, 0.46f, 0.18f, SignArt.Surf, uv);
            }
            if (blades != null) Blades(ctx, blades, at);
        }

        private static void Post(Ctx ctx, Vector2 at, float height, bool wood)
        {
            RaidMesh m = ctx.Low(at);
            if (wood) m.Box(To3(at, height * 0.5f), new Vector3(0.09f, height, 0.09f), Matte(PostWood));
            else m.Box(To3(at, height * 0.5f), new Vector3(0.06f, height, 0.045f), Metal(PostGrey));
            m.Box(To3(at, height + 0.01f), new Vector3(0.1f, 0.02f, 0.1f), SnowSurf);
            ctx.Collider(at, 0f, new Vector3(0.1f, 2.4f, 0.1f), 0f);
            ctx.Site.Take(at, 0.3f);
        }

        // A shaped (alpha-tested) sign: its face and its grey back, back to back.
        private static void Shaped(RaidMesh m, Vector2 at, float y, Vector2 facing, float size, string face, string back)
        {
            Vector3 n = To3(facing, 0f);
            Vector3 c = To3(at, y);
            m.Upright(c + n * 0.006f, n, size, size, SignArt.Surf, SignArt.Cell(face));
            m.Upright(c - n * 0.006f, -n, size, size, SignArt.Surf, SignArt.Cell(back));
            m.Box(c + new Vector3(0f, size * 0.47f, 0f), new Vector3(size * 0.38f, 0.025f, 0.03f), Quaternion.Euler(0f, Yaw(facing), 0f), SnowSurf);
        }

        // A rectangular sign: a thin metal plate with the face on its front.
        private static void Flat(RaidMesh m, Vector2 at, float y, Vector2 facing, float w, float h, Surf face, Rect uv)
        {
            Vector3 n = To3(facing, 0f);
            Vector3 c = To3(at, y);
            float yaw = Yaw(facing);
            m.Box(c, new Vector3(w, h, 0.016f), yaw, SignBack);
            m.Upright(c + n * 0.0095f, n, w, h, face, uv);
            m.Box(c + new Vector3(0f, h * 0.5f + 0.012f, 0f), new Vector3(w * 0.96f, 0.022f, 0.02f), yaw, SnowSurf);
        }
        #endregion

        #region Blades
        // The crossing's green street-name blades, crossed on top of a post at a corner (on <paramref name="on"/> when
        // given: a stop sign's post or a signal pole), each along the street it names.
        private static void Blades(Ctx ctx, Crossing c, Vector2? on)
        {
            if (c.Named) return;
            // The two most important named streets.
            int first = -1, second = -1;
            for (int k = 0; k < c.Arms.Count; k++)
            {
                Street s = c.Arms[k].Street;
                if (RoadRank(s) < 2 || string.IsNullOrEmpty(s.Data.name)) continue;
                if (first < 0 || RoadRank(s) > RoadRank(c.Arms[first].Street)) first = k;
            }
            if (first < 0) return;
            string firstName = c.Arms[first].Street.Data.name;
            for (int k = 0; k < c.Arms.Count; k++)
            {
                Street s = c.Arms[k].Street;
                if (RoadRank(s) < 2 || string.IsNullOrEmpty(s.Data.name) || s.Data.name == firstName) continue;
                if (second < 0 || RoadRank(s) > RoadRank(c.Arms[second].Street)) second = k;
            }
            if (second < 0) return;
            JunctionArm a = c.Arms[first], b = c.Arms[second];
            Vector2 at;
            float top;
            if (on.HasValue)
            {
                at = on.Value;
                top = 3.35f;
            }
            else
            {
                // A post of their own at the kerb corner between the two streets.
                Vector2 bis = a.Dir + b.Dir;
                bis = bis.sqrMagnitude > 1e-3f ? bis.normalized : Left(a.Dir);
                int sideA = Cross(a.Dir, bis) > 0f ? c.ApproachRight(first) : -c.ApproachRight(first);
                int sideB = Cross(b.Dir, bis) > 0f ? c.ApproachRight(second) : -c.ApproachRight(second);
                float off = Mathf.Max(Roadside(a.Street, sideA), Roadside(b.Street, sideB));
                float half = Mathf.Max(0.35f, Mathf.Sin(Vector2.Angle(a.Dir, b.Dir) * Mathf.Deg2Rad * 0.5f));
                float r = Mathf.Clamp(off / half + 0.3f, 3f, 18f);
                at = c.At + bis * r;
                bool ok = false;
                for (int k = 0; k < 4 && !ok; k++)
                {
                    Vector2 q = c.At + bis * (r + k);
                    if (ctx.Inside(q) && ctx.Site.FreeBeside(q, 0.3f)) { at = q; ok = true; }
                }
                if (!ok) return;
                top = 3.2f;
                Post(ctx, at, top, false);
            }
            c.Named = true;
            NameAtlas names = ctx.Names;
            RaidMesh m = ctx.Low(at);
            Blade(m, names, at, top - 0.08f, a, firstName);
            Blade(m, names, at, top + 0.12f, b, b.Street.Data.name);
        }

        private static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

        // One blade along an arm's street, text on both faces.
        private static void Blade(RaidMesh m, NameAtlas names, Vector2 post, float y, JunctionArm a, string name)
        {
            if (!names.Blade(name, out Rect uv, out float aspect)) return;
            const float h = 0.16f;
            float w = Mathf.Clamp(h * aspect, 0.6f, 1.35f);
            Vector2 along = a.Dir;
            Vector2 n = Left(along);
            Vector3 c = To3(post, y);
            m.Box(c, new Vector3(w, h, 0.014f), Yaw(n), Matte(new Color(0.02f, 0.30f, 0.16f)));
            m.Upright(c + To3(n, 0f) * 0.0085f, To3(n, 0f), w, h, names.Surf, uv);
            m.Upright(c - To3(n, 0f) * 0.0085f, -To3(n, 0f), w, h, names.Surf, uv);
            m.Box(c + new Vector3(0f, h * 0.5f + 0.01f, 0f), new Vector3(w * 0.96f, 0.02f, 0.018f), Yaw(n), SnowSurf);
            m.Box(To3(post, y), new Vector3(0.07f, 0.1f, 0.07f), Metal(PostGrey));
        }
        #endregion

        #region Route shields, limits, one way, deer
        // Past the crossing on every arm that carries a route: the route's direction over its shields; and a speed limit
        // on some arms further along.
        private static void RouteSigns(Ctx ctx, Crossing c)
        {
            foreach (JunctionArm a in c.Arms)
            {
                int rank = RoadRank(a.Street);
                if (rank < 2) continue;
                if (a.Street.Data.oneway && Approaches(a)) continue;   // nobody leaves the crossing this way
                Vector2 right = new Vector2(a.Dir.y, -a.Dir.x);
                List<string> routes = Routes(a.Street.Data.route);
                if (routes.Count > 0 && c.TopRank >= 3)
                {
                    Vector2 at = Along(ctx, a, 32f, right);
                    if (ctx.Inside(at) && ctx.Site.FreeBeside(at, 0.3f)) Reassurance(ctx, at, -a.Dir, a.Dir, routes);
                }
                if (MapStyle.Unit(ctx.KeyOf(c.At) ^ a.Street.Data.id, SaltSpeed) < 0.45f)
                {
                    Vector2 at = Along(ctx, a, 60f, right);
                    if (!ctx.Inside(at) || !ctx.Site.FreeBeside(at, 0.3f)) continue;
                    int limit = Limit(ctx, a.Street, at);
                    Post(ctx, at, 2.75f, !ctx.Density.Town(at));
                    Flat(ctx.Low(at), at - a.Dir * 0.04f, 2.35f, -a.Dir, 0.46f, 0.6f, SignArt.Surf, SignArt.Cell("speed" + limit));
                }
            }
        }

        // A spot along an arm's street, beside it on the right of traffic leaving the crossing.
        private static Vector2 Along(Ctx ctx, JunctionArm a, float distance, Vector2 right)
        {
            List<Vector2> pts = a.Street.Pts;
            int step = a.Vertex + 1 < pts.Count && Vector2.Dot(pts[a.Vertex + 1] - pts[a.Vertex], a.Dir) > 0f ? 1 : -1;
            float left = distance;
            Vector2 p = pts[a.Vertex], d = a.Dir;
            for (int i = a.Vertex; i + step >= 0 && i + step < pts.Count; i += step)
            {
                Vector2 q = pts[i + step];
                float len = Vector2.Distance(p, q);
                if (len < 1e-3f) continue;
                d = (q - p) / len;
                if (len >= left)
                {
                    p += d * left;
                    left = 0f;
                    break;
                }
                left -= len;
                p = q;
            }
            if (left > 0f) return new Vector2(1e6f, 1e6f);
            // On the right of the traffic: the street's own right where its points run the traffic's way.
            Vector2 r = new Vector2(d.y, -d.x);
            return p + r * (Roadside(a.Street, step > 0 ? -1 : 1) + 0.2f);
        }

        private static int Limit(Ctx ctx, Street s, Vector2 at)
        {
            bool town = ctx.Density.Town(at);
            switch (s.Data.kind)
            {
                case "residential": case "living_street": return 25;
                case "unclassified": return town ? 25 : 35;
                case "tertiary": return town ? 35 : 45;
                case "secondary": case "primary": return town ? 35 : 55;
                case "trunk": return town ? 45 : 55;
                default: return town ? 35 : 55;
            }
        }

        // A reassurance assembly: the direction plaque over a shield for each route, on two posts when wide.
        private static void Reassurance(Ctx ctx, Vector2 at, Vector2 facing, Vector2 travel, List<string> routes)
        {
            NameAtlas names = ctx.Names;
            var shields = new List<(Rect face, Rect back)>(3);
            foreach (string r in routes)
            {
                if (shields.Count >= 3) break;
                if (names.Shield(r, out Rect face, out Rect back)) shields.Add((face, back));
            }
            if (shields.Count == 0) return;
            const float size = 0.6f, gap = 0.06f;
            float width = shields.Count * size + (shields.Count - 1) * gap;
            Vector2 across = Left(facing);
            float bottom = 1.75f;
            if (shields.Count > 1)
            {
                Post(ctx, at + across * (width * 0.3f), bottom + size + 0.35f, false);
                Post(ctx, at - across * (width * 0.3f), bottom + size + 0.35f, false);
            }
            else Post(ctx, at, bottom + size + 0.35f, false);
            RaidMesh m = ctx.Low(at);
            Vector2 front = at + facing * 0.04f;
            for (int i = 0; i < shields.Count; i++)
            {
                float x = -width * 0.5f + size * 0.5f + i * (size + gap);
                Vector2 p = front + across * x;
                Vector3 n = To3(facing, 0f), c = To3(p, bottom + size * 0.5f);
                m.Upright(c + n * 0.006f, n, size, size, names.Surf, shields[i].face);
                m.Upright(c - n * 0.006f, -n, size, size, names.Surf, shields[i].back);
            }
            string word = Mathf.Abs(travel.y) >= Mathf.Abs(travel.x) ? (travel.y > 0f ? "NORTH" : "SOUTH") : (travel.x > 0f ? "EAST" : "WEST");
            Flat(m, front, bottom + size + 0.16f, facing, 0.6f, 0.22f, SignArt.Surf, SignArt.Cell(word));
        }

        // One-way streets: ONE WAY where traffic may turn in, DO NOT ENTER where it comes out.
        private static void OneWays(Ctx ctx, Crossing c)
        {
            for (int k = 0; k < c.Arms.Count; k++)
            {
                JunctionArm a = c.Arms[k];
                if (!a.Street.Data.oneway || RoadRank(a.Street) < 2) continue;
                bool crossed = false;
                foreach (JunctionArm b in c.Arms)
                    if (NameKey(b.Street) != NameKey(a.Street)) crossed = true;
                if (!crossed) continue;
                Vector2 right = new Vector2(a.Dir.y, -a.Dir.x);
                float back = c.Setback(k) + 0.9f;
                if (Approaches(a))
                {
                    // Traffic comes out of this street: nobody turns into it.
                    Vector2 at = c.At + a.Dir * back + right * Roadside(a.Street, -c.ApproachRight(k));
                    if (!ctx.Inside(at) || !ctx.Site.FreeBeside(at, 0.3f)) continue;
                    Post(ctx, at, 2.7f, false);
                    Shaped(ctx.Low(at), at - a.Dir * 0.04f, 2.35f, -a.Dir, 0.62f, "donotenter", "roundback");
                }
                else
                {
                    // Traffic may turn in: an arrow along it, read from the crossing street.
                    Vector2 at = c.At + a.Dir * back - right * Roadside(a.Street, c.ApproachRight(k));
                    if (!ctx.Inside(at) || !ctx.Site.FreeBeside(at, 0.3f)) continue;
                    Post(ctx, at, 2.9f, false);
                    RaidMesh m = ctx.Low(at);
                    Vector2 n = -right;
                    Vector3 rightOfViewer = Vector3.Cross(To3(n, 0f), Vector3.up);
                    bool toRight = Vector3.Dot(rightOfViewer, To3(a.Dir, 0f)) > 0f;
                    Flat(m, at + n * 0.03f, 2.55f, n, 0.9f, 0.3f, SignArt.Surf, SignArt.Cell(toRight ? "oneway_r" : "oneway_l"));
                    Vector3 rightBack = Vector3.Cross(To3(-n, 0f), Vector3.up);
                    bool backRight = Vector3.Dot(rightBack, To3(a.Dir, 0f)) > 0f;
                    m.Upright(To3(at - n * 0.03f, 2.55f) - To3(n, 0f) * 0.0095f, -To3(n, 0f), 0.9f, 0.3f, SignArt.Surf,
                        SignArt.Cell(backRight ? "oneway_r" : "oneway_l"));
                }
            }
        }

        // Deer crossings on the long country roads.
        private static void Deer(Ctx ctx)
        {
            foreach (Street s in ctx.Site.Area.Streets)
            {
                int rank = RoadRank(s);
                if (rank < 2 || rank > 5 || s.Data.tunnel || s.Data.bridge || s.Pts.Count < 2) continue;
                if (MapStyle.Unit(s.Data.id, SaltDeer) > 0.3f) continue;
                float len = 0f;
                for (int i = 1; i < s.Pts.Count; i++) len += Vector2.Distance(s.Pts[i - 1], s.Pts[i]);
                if (len < 120f) continue;
                Vector2 mid = s.Pts[s.Pts.Count / 2];
                if (!ctx.Inside(mid) || ctx.Density.Town(mid)) continue;
                Nearest(s.Pts, mid, out Vector2 on, out Vector2 d);
                Vector2 right = new Vector2(d.y, -d.x);
                Vector2 at = on + right * (Roadside(s, -1) + 0.2f);
                if (!ctx.Site.FreeBeside(at, 0.3f)) continue;
                Post(ctx, at, 2.6f, true);
                Shaped(ctx.Low(at), at - d * 0.05f, 2.15f, -d, 0.75f, "deer", "diamondback");
            }
        }
        #endregion
    }
}
