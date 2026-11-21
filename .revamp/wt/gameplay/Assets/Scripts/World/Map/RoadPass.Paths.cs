using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Footways, paths, steps and pedestrian streets: a trodden band through the snow (paved, for a pedestrian street),
    // a darker line where most feet went. Not where a footway is a street's sidewalk (the street draws that), crosses a
    // carriageway (a crosswalk, or nothing), runs over a sidewalk, or goes into a building.
    internal static partial class RoadPass
    {
        private static void Paths(Net net)
        {
            Look look = net.Look;
            foreach (Way p in net.Ways)
            {
                if (p.Cls != Cls.Path) continue;
                Chain line = ChainOf(p.P);
                if (line.Length < 0.3f) continue;
                bool paved = p.Winter == Winter.Paved;
                float width = Mathf.Clamp(p.Half * 2f, 1.0f, paved ? 12f : 1.8f);
                List<Sample> s = net.Samples;
                SamplesOf(line, 0f, line.Length, net.Full ? 1f : 2f, s);
                int n = s.Count;
                if (n < 2) continue;
                FitSides(n);
                int covers = p.Covered != null ? p.Covered.Length : 0;
                for (int i = 0; i < n; i++)
                {
                    Vector2 q = s[i].P;
                    bool beside = covers > 0 && p.Covered[Mathf.Clamp((int)(s[i].S / p.Length * covers), 0, covers - 1)];
                    s_KerbOk[i] = !beside && !OnPavement(net, q, null, 0.3f) && !OnSidewalk(net, q, null, 0.15f) && !InBuilding(net, q, 0.15f);
                }
                for (int i = 0; i + 1 < n; i++) s_Keep[i] = s_KerbOk[i] && s_KerbOk[i + 1];
                for (int i = 0; i < n; i++)
                {
                    s_A[i] = -width * 0.5f;
                    s_B[i] = width * 0.5f;
                }
                Band(net, s, s_A, s_B, PathY, PathY, paved ? look.Paved : look.Trodden, null, s_Keep);
                if (net.Full && !paved)
                {
                    int seed = Seed(p, 0x7A7);
                    for (int i = 0; i < n; i++)
                    {
                        float mid = 0.12f * (Noise(s[i].P, 3.5f, seed) - 0.5f) * 2f;
                        float half = Mathf.Min(width * 0.35f, 0.16f + 0.1f * Noise(s[i].P, 2.25f, seed + 1));
                        s_A[i] = mid - half;
                        s_B[i] = mid + half;
                    }
                    for (int i = 0; i + 1 < n; i++) s_Keep[i] = s_KerbOk[i] && s_KerbOk[i + 1] && Noise(s[i].P, 4.5f, seed + 2) > 0.25f;
                    Band(net, s, s_A, s_B, PathY + 0.0015f, PathY + 0.0015f, look.TroddenLine, null, s_Keep);
                }
                // Keep the drawn stretches clear of props.
                List<Vector2> run = null;
                for (int i = 0; i < n; i++)
                {
                    if (s_KerbOk[i])
                    {
                        (run ??= new List<Vector2>()).Add(s[i].P);
                        continue;
                    }
                    if (run != null && run.Count >= 2) net.Site.TakeRoad(run, width * 0.5f, 0.2f);
                    run = null;
                }
                if (run != null && run.Count >= 2) net.Site.TakeRoad(run, width * 0.5f, 0.2f);
            }
        }
    }
}
