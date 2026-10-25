using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's street furniture, what makes a place read as small-town Kentucky in 1993: wooden utility poles
    /// with sagging wires along nearly every road, transformers and service drops to the houses, the mapped power lines on
    /// their towers (<see cref="PowerPass"/>); street lights of the period, a few still burning (<see cref="StreetLights"/>,
    /// <see cref="StreetLamp"/>); span-wire signals, stop and yield signs, green street-name blades, route shields and speed
    /// limits (<see cref="SignPass"/>); hydrants, collection boxes, kerbside mailboxes, benches, trash cans, payphones,
    /// newspaper boxes, vending machines, bollards, gates and bus stops (<see cref="StreetFurniture"/>); flagpoles, memorials
    /// and historical markers, water towers and masts (<see cref="Landmarks"/>); fences, rock fences, walls, hedges and
    /// guard rails (<see cref="BarrierPass"/>); and litter in the parks. From the format-2 data where it's mapped (power,
    /// nodes, barriers), from heuristics where it's thin. Roadside things stand where the roads drew their roadside
    /// (<see cref="RoadPass.RoadsideOffset"/>), never on a carriageway or a driveway, and no wire passes over a roof. A Lite
    /// site gets only what's seen from afar: transmission towers, water towers, masts, and each fence or wall as one plain
    /// shape; past a fixed map's edge the poles carry only their crossarm and primaries, the fences are plain. Every step
    /// yields when a streamed piece's time slice is used (<see cref="Site.StepDue"/>). Everything claims its spot in the
    /// occupancy grid; nothing here draws from the town's shared random stream (every choice comes from a feature's id or a
    /// world-anchored spot, so streamed pieces agree).
    /// </summary>
    internal static partial class StreetscapePass
    {
        private const int SaltLitter = 0x1177;
        private const int MaxParkProps = 200;

        // Every step below may also yield part way through (when the streamed piece's step has used its time slice: see
        // Site.StepDue); what's built never depends on where it yields.
        /// <summary>Builds the full streetscape for a site, adding power lines, poles, signs, lights, furniture, landmarks, barriers and litter.</summary>
        internal static IEnumerable<string> Build(Site site)
        {
            var ctx = new Ctx(site);
            // If the site is not fully generated (e.g., streamed piece early), only add essential elements.
            if (!ctx.Full)
            {
                // Add the concrete power line geometry after the plan step.
                PowerPass.MappedLines(ctx);
                Landmarks.Lite(ctx);
                foreach (string step in BarrierPass.Lite(ctx)) yield return step;
                // Finish up any remaining work and register the built wires.
                foreach (string step in ctx.Finish()) yield return step;
                yield return "landmarks";
                yield break;
            }
            // Lay out the main utility lines for streets before adding detailed features.
            foreach (string step in PowerPass.PlanStreetLines(ctx)) yield return step;
            yield return "utility lines";
            PowerPass.MappedLines(ctx);
            yield return "power lines";
            // Place signal poles and wires for traffic control.
            SignPass.Signals(ctx);
            // Build the streetlights and plan where each will be placed based on the site layout.
            StreetLights.Plan(ctx);
            foreach (string step in PowerPass.DrawPoles(ctx)) yield return step;
            yield return "utility poles";
            // Create service drop wires linking power lines to nearby buildings.
            foreach (string step in PowerPass.ServiceDrops(ctx)) yield return step;
            // Render the street lights after utility poles are in place.
            foreach (string step in StreetLights.Draw(ctx)) yield return step;
            yield return "street lights";
            // Add road signs (stop, yield, speed limits, etc.).
            foreach (string step in SignPass.Signs(ctx)) yield return step;
            yield return "signs";
            // Populate the streets with generic furniture like benches, trash cans, vending machines.
            foreach (string step in StreetFurniture.Build(ctx)) yield return step;
            yield return "street furniture";
            // Place larger landmarks (water towers, memorials) now that major infrastructure exists.
            foreach (string step in Landmarks.Build(ctx)) yield return step;
            yield return "landmarks";
            // Build fences and walls that bound the map and separate areas.
            foreach (string step in BarrierPass.Build(ctx)) yield return step;
            yield return "fences and walls";
            foreach (string step in Litter(site)) yield return step;
            yield return "park litter";
            foreach (string step in ctx.Finish()) yield return step;
            yield return "wires";
        }

        /// <summary>Up to five trash bags or bits of litter in every park, anywhere still free, each with a small
        /// collider.</summary>
        internal static void ParkLitter(Site site)
        {
            foreach (string _ in Litter(site)) { }
        }

        /// <summary>Adds up to five pieces of trash or litter to each park, respecting free space and placement limits.</summary>
        private static IEnumerable<string> Litter(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            int placed = 0;
            foreach (Patch p in site.Area.Areas)
            {
                if (p.Data.kind != "park") continue;
                // A streamed piece's areas are cut to its square already; the fixed square's reach past it.
                List<Vector2> inside = site.Chunked ? p.Pts : MapGeometry.ClipPolygon(p.Pts, square);
                if (inside.Count < 3) continue;
                System.Random rng = MapStyle.Rng(p.Data.id, SaltLitter);
                MapGeometry.Extent(inside, out float x0, out float z0, out float x1, out float z1);
                int want = Mathf.Min(5, MaxParkProps - placed);
                for (int i = 0, tries = 0; i < want && tries < want * 6; ++tries)
                {
                    var at = new Vector2(MapStyle.Range(rng, x0, x1), MapStyle.Range(rng, z0, z1));
                    if (!MapGeometry.Contains(inside, at) || !site.Free(at, 0.7f)) continue;
                    float yaw = (float)rng.NextDouble() * 360f;
                    Vector3 at3 = To3(at, 0f);
                    RaidMesh m = site.Town.Out.Solid(at3);
                    m.SetFrame(at3, yaw);
                    if (rng.NextDouble() < 0.5) DecorProps.TrashBag(m, rng, Vector3.zero);
                    else DecorProps.Litter(m, rng, Vector3.zero, true);
                    m.ResetFrame();
                    site.Town.PropCollider(at3 + Vector3.up * 0.25f, new Vector3(0.4f, 0.4f, 0.4f), yaw);
                    site.Take(at, 0.7f);
                    placed++;
                    i++;
                }
                if (placed >= MaxParkProps) break;
                if (site.StepDue) yield return "park litter";
            }
        }
    }
}
