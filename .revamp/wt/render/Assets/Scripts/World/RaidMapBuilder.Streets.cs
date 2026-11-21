using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    // The streets as the plan has them: asphalt carriageways, concrete sidewalks with a kerb along the road, broken where
    // streets meet and dropped where a drive or a lot opens onto the street; zebra crossings and stop lines at the
    // crossroads, a stop line where each residential street meets Main St, a double yellow centre line down the arterials,
    // and a barricade where each residential street ends. Everything lies flat on the snow, each layer a few millimetres
    // over the one beneath it.
    public static partial class RaidMapBuilder
    {
        private static readonly Color KerbStone = new Color(0.68f, 0.68f, 0.66f);
        private static readonly Color LineYellow = new Color(0.80f, 0.64f, 0.20f);
        private static readonly Color LineWhite = new Color(0.86f, 0.86f, 0.84f);

        // Surface heights over the snow (the ground's top is y = 0).
        private const float RoadY = 0.010f, LotY = 0.012f, PathY = 0.014f, MarkY = 0.016f, PlazaY = 0.018f, WalkY = 0.022f, KerbY = 0.026f;
        private const float KerbWidth = 0.2f;

        /// <summary>A stretch along a street; a kerb runs across an end that a crossing carriageway cut.</summary>
        private struct Span
        {
            public float A, B;
            public bool KerbA, KerbB;
        }

        /// <summary>Removes <paramref name="a"/>..<paramref name="b"/> from every span it overlaps; the new ends get a kerb if
        /// <paramref name="kerb"/>.</summary>
        private static void Cut(List<Span> spans, float a, float b, bool kerb)
        {
            for (int i = spans.Count - 1; i >= 0; i--)
            {
                Span s = spans[i];
                if (b <= s.A || a >= s.B) continue;
                spans.RemoveAt(i);
                if (b < s.B) spans.Insert(i, new Span { A = b, B = s.B, KerbA = kerb, KerbB = s.KerbB });
                if (s.A < a) spans.Insert(i, new Span { A = s.A, B = a, KerbA = s.KerbA, KerbB = kerb });
            }
        }

        private sealed partial class Builder
        {
            /// <summary>Where a drive or a lot opens onto a street: the kerb is dropped along that stretch of the sidewalk.</summary>
            private readonly List<(Street street, int side, float a, float b)> _kerbGaps = new List<(Street, int, float, float)>();

            private void DropKerb(Street street, int side, float a, float b) => _kerbGaps.Add((street, side, Mathf.Min(a, b), Mathf.Max(a, b)));

            public void Streets()
            {
                foreach (Street s in AllStreets) Carriageway(s);
                foreach (Street s in AllStreets)
                {
                    Sidewalk(s, +1);
                    Sidewalk(s, -1);
                }
                Crossroads();
                foreach (Street s in AllStreets) if (s.Arterial) CentreLine(s);
                DeadEnd(ElmSt);
                DeadEnd(OakSt);
                Junction(ElmSt);
                Junction(OakSt);
            }

            /// <summary>Lays one flat rectangle in 10 m pieces along the street (each in its own outdoor chunk).</summary>
            private void Lay(Street s, float a, float b, float c0, float c1, float y, in Surf surf)
            {
                for (float t = a; t < b - 0.01f; t += 10f)
                {
                    Rect r = s.Band(t, Mathf.Min(b, t + 10f), c0, c1);
                    var c = new Vector3(r.center.x, y, r.center.y);
                    _town.Out.Flat(c).Flat(c, r.width, r.height, 0f, surf);
                }
            }

            // The asphalt, less where a higher-ranked street's carriageway already lies.
            private void Carriageway(Street s)
            {
                var spans = new List<Span> { new Span { A = s.From, B = s.To } };
                foreach (Street o in AllStreets)
                    if (o.Rank > s.Rank && s.CrossedBy(o, -s.HalfWidth, s.HalfWidth))
                        Cut(spans, o.Coord - o.HalfWidth, o.Coord + o.HalfWidth, false);
                foreach (Span sp in spans) Lay(s, sp.A, sp.B, -s.HalfWidth, s.HalfWidth, RoadY, DecorMaterials.DryAsphalt);
            }

            // One side's sidewalk: the concrete, and the kerb along the road except where a drive or a lot opens onto it. A
            // crossing street of lower rank cuts it only for its carriageway (a kerb runs across the cut ends); one of higher
            // rank cuts it for its whole width, sidewalks and all, which carry on through.
            private void Sidewalk(Street s, int side)
            {
                float inner = side * s.HalfWidth, outer = side * s.Outer, kerbEdge = side * (s.HalfWidth + KerbWidth);
                var runs = new List<Span> { new Span { A = s.From, B = s.To } };
                foreach (Street o in AllStreets)
                {
                    if (!s.CrossedBy(o, inner, outer)) continue;
                    if (o.Rank < s.Rank) Cut(runs, o.Coord - o.HalfWidth, o.Coord + o.HalfWidth, true);
                    else Cut(runs, o.Coord - o.Outer, o.Coord + o.Outer, false);
                }
                Surf kerb = DecorPalette.Matte(KerbStone);
                foreach (Span run in runs)
                {
                    Lay(s, run.A, run.B, inner, outer, WalkY, DecorMaterials.Concrete);
                    var kerbs = new List<Span> { new Span { A = run.A, B = run.B } };
                    foreach (var g in _kerbGaps)
                        if (g.street == s && g.side == side) Cut(kerbs, g.a, g.b, false);
                    foreach (Span k in kerbs) Lay(s, k.A, k.B, inner, kerbEdge, KerbY, kerb);
                    if (run.KerbA) Lay(s, run.A, run.A + KerbWidth, inner, outer, KerbY, kerb);
                    if (run.KerbB) Lay(s, run.B - KerbWidth, run.B, inner, outer, KerbY, kerb);
                }
            }

            // The double yellow line down an arterial, clear of the crossroads and its crossings.
            private void CentreLine(Street s)
            {
                Surf yellow = DecorPalette.Matte(LineYellow);
                float clear = DepotRd.Outer + 1.4f;
                var spans = new List<Span> { new Span { A = s.From, B = s.To } };
                Cut(spans, -clear, clear, false);
                foreach (Span sp in spans)
                    for (int k = -1; k <= 1; k += 2)
                        Lay(s, sp.A, sp.B, k * 0.08f, k * 0.2f, MarkY, yellow);
            }

            // Main St and Depot Rd cross: a zebra crossing over each of the four arms, in line with the sidewalks either side,
            // and a stop line across the lane coming in to it (traffic keeps to the right).
            private void Crossroads()
            {
                Surf white = DecorPalette.Matte(LineWhite);
                Street[] pair = { MainSt, DepotRd };
                foreach (Street arm in pair)
                {
                    Street across = arm == MainSt ? DepotRd : MainSt;
                    float c0 = across.HalfWidth + 0.3f, c1 = across.Outer - 0.3f;
                    int stripes = Mathf.FloorToInt((arm.HalfWidth * 2f - 0.5f) / 0.95f);
                    float first = -(stripes - 1) * 0.95f * 0.5f;
                    for (int end = -1; end <= 1; end += 2)
                    {
                        float a = end > 0 ? c0 : -c1, b = end > 0 ? c1 : -c0;
                        for (int i = 0; i < stripes; i++)
                        {
                            float x = first + i * 0.95f;
                            Lay(arm, a, b, x - 0.225f, x + 0.225f, MarkY, white);
                        }
                        // The lane coming in toward the crossroads keeps to the right: on Main St the west half from the north
                        // and the east half from the south; on Depot Rd the north half from the east, the south from the west.
                        float stop = end * (c1 + 0.9f);
                        int lane = arm == MainSt ? -end : end;
                        Lay(arm, stop - 0.2f, stop + 0.2f, lane * 0.25f, lane * (arm.HalfWidth - 0.15f), MarkY, white);
                    }
                }
            }

            // Where a residential street meets Main St: a stop line across its eastbound (south) lane.
            private void Junction(Street lane)
            {
                float x = -MainSt.Outer - 0.8f;
                Lay(lane, x - 0.2f, x + 0.2f, -lane.HalfWidth + 0.15f, -0.25f, MarkY, DecorPalette.Matte(LineWhite));
            }

            // The dead end of a residential street: a kerb across its end, a barricade just short of it, and the snow the plows
            // pushed up beyond.
            private void DeadEnd(Street lane)
            {
                Lay(lane, lane.From - KerbWidth, lane.From, -lane.HalfWidth, lane.HalfWidth, KerbY, DecorPalette.Matte(KerbStone));
                float len = lane.HalfWidth * 2f - 0.6f;
                Prop(_town, lane.At(lane.From + 0.6f, 0f), 90f, new Vector3(len, 1.3f, 0.3f), true, (m, r) => DecorProps.Barricade(m, len));
                Vector3 heap = lane.At(lane.From - 2.6f, R(_rng, -1f, 1f));
                Prop(_town, heap, R(_rng, 0f, 180f), new Vector3(3.2f, 1.3f, 2.4f), true,
                    (m, r) => DecorProps.SnowPile(m, r, Vector3.zero, 4.2f, 3.2f, 1.3f, 0f));
            }
        }
    }
}
