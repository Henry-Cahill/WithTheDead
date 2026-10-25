using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Builds a raid map from a real place in Kentucky: the streets, buildings, water and land use around the zone's
    /// <see cref="ZoneDef.RealPlace"/>, from the world store's current build (OpenStreetMap data, made by Tools/World;
    /// see <see cref="WorldStore"/>). Laid out like <see cref="RaidMapBuilder"/>'s town: ground-floor walls, floors and
    /// props under <c>world</c> with colliders, so the NavMesh bake carves them; roofs and upper storeys under the
    /// overhead root, shown from outside and cut away around the survivor in the isometric view, Project Zomboid-style
    /// (<see cref="BuildingShell"/>, <see cref="MapCutaway"/>), and interior ceilings shown only from inside views; the
    /// surroundings past the playable square as scenery without colliders; exits where streets leave the map.
    /// Everything is in metres from the place's centre, x east, z north.
    /// Builds nothing and returns null (with the reason) when the store or the place can't be read, so the caller can
    /// build the procedural town instead. A statewide zone streams the whole state instead (KentuckyMap.Statewide,
    /// <see cref="KentuckyStreamer"/>). Map data © OpenStreetMap contributors (ODbL).
    /// </summary>
    /// <remarks>
    /// The map is drawn on a <see cref="Site"/> (KentuckyMap.Site.cs: the place's data in local metres, the batches, a
    /// 1 m occupancy grid and what the passes hand each other, typed in MapContracts.cs) by a pipeline of passes
    /// (<see cref="Passes"/>). Each is a static class of its own with one entry point,
    /// <c>IEnumerable&lt;string&gt; Build(Site)</c>, that yields a step name between its heavy steps (and wherever
    /// <see cref="Site.StepDue"/> says the frame's streaming budget is spent); they run in this order:
    /// <list type="number">
    /// <item><see cref="GroundPass"/>: snow, land use (yards, fields, woods floor, cemeteries, pitches, ploughed parking
    /// with stalls), frozen lakes and rivers with their banks.</item>
    /// <item><see cref="RoadPass"/>: the street network with real junctions (<see cref="Site.Junctions"/>), winter
    /// carriageways, kerbs, sidewalks, driveways, faded markings, paths, bridges and railways.</item>
    /// <item><see cref="BuildingPass"/>: every building's shell, walls and roof drawn through its
    /// <see cref="BuildingShell"/>, from an archetype read off its kind, use, shape and surroundings; doors, windows,
    /// storefronts, porches, chimneys, roof snow; the enterable ones listed in <see cref="Site.Enterables"/>.</item>
    /// <item><see cref="InteriorPass"/>: floor plans, furniture, fixtures, lights and loot (room by room, from
    /// <see cref="MapGameplay.Allot"/>) for the enterable buildings: at once on a fixed square, lazily near the survivors
    /// on a streamed map (<see cref="InteriorStreamer"/>).</item>
    /// <item><see cref="CutawayPass"/>: each building's own shell meshes, which <see cref="MapCutaway"/> lifts off around
    /// the survivor in the isometric view.</item>
    /// <item><see cref="StreetscapePass"/>: utility lines (<see cref="PowerPass"/>; the "Wires" meshes' layout is read
    /// back by the vegetation), street lights (no light of their own in play: <see cref="StreetLamp"/>'s pool lends a few
    /// to the nearest), signals, signs and street-name blades (<see cref="SignPass"/>), street furniture, landmarks such as
    /// water towers, fences and walls (<see cref="BarrierPass"/>).</item>
    /// <item><see cref="VegetationPass"/>: winter trees (mapped, woods, yards, fence rows, street trees).</item>
    /// <item><see cref="VehiclePass"/>: 1993 cars in driveways, at kerbs, in lot stalls, wrecks on the highways.</item>
    /// <item><see cref="SnowPass"/>: plough piles and drifts.</item>
    /// <item><see cref="MapGameplay"/>: points of interest, landmarks and their signs.</item>
    /// </list>
    /// A pass only finds the spots the passes before it left free in the occupancy grid, so their order matters; their
    /// randomness comes from <see cref="MapStyle"/>'s per-feature hashes, so every machine and every rebuild of a piece
    /// gets the same map. A streamed piece is built in <see cref="Detail.Full"/> near the survivors and in
    /// <see cref="Detail.Lite"/> further out (and upgraded as they come near; see <see cref="KentuckyStreamer"/>).
    /// A fixed map then lays out its exits and spawn (<see cref="MapGameplay"/>) and emits its batches; a streamed piece
    /// emits its own in <see cref="ChunkSteps"/>.
    /// </remarks>
    public static partial class KentuckyMap
    {
        /// <summary>A one-line summary of the last map built.</summary>
        public static string LastStats { get; private set; } = "";

        /// <summary>Builds the map for <paramref name="zone"/> under <paramref name="world"/>, or returns null with the
        /// reason in <paramref name="why"/>, having built nothing. A statewide zone (<see cref="ZoneDef.RealStatewide"/>)
        /// gets the whole state, streamed around the survivor (see <see cref="BuildStatewide"/>).</summary>
        public static RaidMapInfo Build(Transform world, ZoneDef zone, out string why)
        {
            // A new raid's map: nothing the last one learned or had switched carries over.
            MapGameplay.ForgetMap();
            StreetLamp.ForgetSwitches();
            if (zone.RealStatewide) return BuildStatewide(world, zone, out why);
            ForgetLot();
            MapBuild build = WorldStore.OpenCurrent(out why);
            if (build == null) return null;
            float half = Mathf.Clamp(zone.RealHalfSize, 60f, 400f);
            Area area;
            try
            {
                MapPlace place = build.FindPlace(zone.RealPlace);
                if (place == null)
                {
                    why = "build " + build.Id + " has no place called " + zone.RealPlace;
                    return null;
                }
                area = new Area(build, place, half);
            }
            catch (System.Exception e)
            {
                why = "reading build " + build.Id + ": " + e.GetType().Name + ": " + e.Message;
                return null;
            }

            var info = new RaidMapInfo { HalfSize = half, PlayerSpawn = new Vector3(0f, 0.6f, 0f) };
            CeilingVisibility.Clear();
            var town = new RaidTown(world, zone.LayoutSeed, info);
            var site = new Site(build, town, area);
            try
            {
                foreach (string _ in Passes(site)) { }
                MapGameplay.DefineExits(site);
                MapGameplay.PickSpawn(site);
                site.Overhead.Emit("Overhead", town.Overhead, true);
                site.Roofs.Emit("Roofs", town.Overhead, false);
                town.EmitOutdoor();
                DecorPalette.Apply();
            }
            catch (System.Exception e)
            {
                why = "building " + area.Place + " failed: " + e;
                Discard(world, town);
                return null;
            }
            LastStats = "Kentucky map " + area.Place + " (build " + area.BuildId + "): " + site.Buildings + " buildings, "
                + site.SceneryBuildings + " beyond the edge, " + area.Streets.Count + " street pieces, " + site.Containers
                + " containers, " + town.LightCount + " lights, " + site.Trees + " trees, " + info.Exits.Count + " exits. "
                + area.Attribution;
            Debug.Log("[CloneZ] " + LastStats);
            return info;
        }

        /// <summary>Everything the map draws on a site, in order, a step at a time (a streamed piece is built a few steps
        /// a frame; the fixed square runs them all at once).</summary>
        internal static IEnumerable<string> Passes(Site site)
        {
            foreach (string step in GroundPass.Build(site)) yield return step;
            foreach (string step in RoadPass.Build(site)) yield return step;
            foreach (string step in BuildingPass.Build(site)) yield return step;
            foreach (string step in InteriorPass.Build(site)) yield return step;
            foreach (string step in CutawayPass.Build(site)) yield return step;
            foreach (string step in StreetscapePass.Build(site)) yield return step;
            foreach (string step in VegetationPass.Build(site)) yield return step;
            foreach (string step in VehiclePass.Build(site)) yield return step;
            foreach (string step in SnowPass.Build(site)) yield return step;
            foreach (string step in MapGameplay.Build(site)) yield return step;
        }

        // A failed build mustn't leave half a town for the NavMesh bake or the procedural map to land on.
        private static void Discard(Transform world, RaidTown town)
        {
            for (int i = world.childCount - 1; i >= 0; i--) Object.DestroyImmediate(world.GetChild(i).gameObject);
            if (town.Overhead != null) Object.DestroyImmediate(town.Overhead.gameObject);
            if (town.Scenery != null) Object.DestroyImmediate(town.Scenery.gameObject);
            CeilingVisibility.Clear();
        }

        /// <summary>A plan point (x, z) at height <paramref name="y"/>.</summary>
        internal static Vector3 To3(Vector2 p, float y) => new Vector3(p.x, y, p.y);
    }
}
