using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The real map's landmarks: the places a local would give directions by, and a survivor would make for. Each site
    // publishes its own (the courthouse, the police and fire stations, the hospital, the schools, the named churches, the
    // post office, the water tower, the pharmacies, the gas stations, the stores worth the trip, the named cemeteries...)
    // as the game's landmarks ("landmark:ky.<id>" in WorldMarkers): the Zone map pins them, HumanUtil describes places by
    // them ("120 m north-west of the Bardstown courthouse"), the A-Life squads head for them. Rank 5 and up are published
    // (a salon or a café downtown isn't one, however well known); the most important are there to be discovered: the first
    // time a survivor reaches one it's learnt for good (MapGameplayRuntime). Names are the real ones, else made from the
    // place and the street ("Bardstown fire station", "gas station on North 3rd Street").
    internal static partial class MapGameplay
    {
        /// <summary>The prefix of a real landmark's id (published as "landmark:ky.p123": p a point of interest, b a
        /// building, n a mapped point feature, a an area, then its OpenStreetMap id).</summary>
        internal const string LandmarkPrefix = "ky.";

        private const int PublishFrom = 5, DiscoverFrom = 6, RewardFrom = 8;

        /// <summary>One real landmark.</summary>
        private sealed class RealLandmark
        {
            public string Id, Name;
            /// <summary>The name is the place's own ("Hurst Discount Drug") rather than a description ("Bardstown fire
            /// station").</summary>
            public bool Proper;
            public Vector3 At;
            public int Rank;
            public float Radius;
        }

        private static readonly Dictionary<string, RealLandmark> s_Landmarks = new Dictionary<string, RealLandmark>();
        private static object s_LandmarkEpoch;

        /// <summary>A stretch of open ground near the start (a parking lot, a ball field, a school's grounds, a park, a
        /// field), for setting a helicopter down.</summary>
        private struct OpenGround
        {
            public Vector3 At;
            public float Size;
            public int Preference;
        }

        private static readonly List<OpenGround> s_Open = new List<OpenGround>();

        // The landmarks published and the open ground found, forgotten (a new raid's map).
        private static void ForgetLandmarks()
        {
            s_Landmarks.Clear();
            s_Open.Clear();
            s_LandmarkEpoch = null;
            MapGameplayRuntime.Reset();
        }
        private const float OpenReach = 700f;   // metres from the start town's middle

        /// <summary>Open ground near <paramref name="target"/> (world metres, within <paramref name="within"/>) in the
        /// start town, best first: paved lots, fields and grounds before grass, the bigger and the nearer the better.</summary>
        internal static void OpenGroundNear(Vector3 target, float within, List<Vector3> into)
        {
            into.Clear();
            var scored = new List<KeyValuePair<float, Vector3>>();
            foreach (OpenGround g in s_Open)
            {
                float d = Vector2.Distance(new Vector2(g.At.x, g.At.z), new Vector2(target.x, target.z));
                if (d > within) continue;
                scored.Add(new KeyValuePair<float, Vector3>(g.Preference * 2f + Mathf.Log10(g.Size) - d / 25f, g.At));
            }
            scored.Sort((a, b) => b.Key.CompareTo(a.Key));
            foreach (KeyValuePair<float, Vector3> kv in scored) into.Add(kv.Value);
        }

        private static int OpenPreference(string kind)
        {
            switch (kind)
            {
                case "parking": case "plaza": case "pitch": return 3;
                case "school": case "park": case "golf": case "farmland": case "meadow": case "grass": return 2;
                case "playground": case "lot": return 1;
                default: return 0;
            }
        }

        // Notes the site's open ground, if it's near the start.
        private static void RecordOpenGround(Site site)
        {
            if (site.WorldOffset.magnitude > OpenReach || s_Open.Count > 4000) return;
            foreach (Patch a in site.Area.Areas)
            {
                int pref = OpenPreference(a.Data.kind);
                if (pref == 0) continue;
                float size = Mathf.Abs(MapGeometry.SignedArea(a.Pts));
                if (size < 250f) continue;
                Rect r = MapGeometry.Bounds(a.Pts);
                Vector2 at = MapGeometry.Middle(a.Pts);
                if (!MapGeometry.Contains(a.Pts, at)) at = r.center;
                if (!MapGeometry.Contains(a.Pts, at)) continue;
                s_Open.Add(new OpenGround { At = site.WorldOffset + To3(at, 0f), Size = size, Preference = pref });
            }
        }

        /// <summary>The name of a real landmark by its id (as published, without "landmark:"), for the HUD, the map and
        /// the descriptions of places; false for any other id.</summary>
        internal static bool TryLandmarkName(string id, out string name) => TryLandmark(id, out name, out _);

        /// <summary>A real landmark's name and rank (how much it matters to someone finding their way: 10 the
        /// courthouse, 4 a named store) by its id; false for any other id.</summary>
        internal static bool TryLandmark(string id, out string name, out int rank)
        {
            name = null;
            rank = 0;
            if (id == null || !id.StartsWith(LandmarkPrefix, System.StringComparison.Ordinal)) return false;
            if (!s_Landmarks.TryGetValue(id, out RealLandmark l)) return false;
            name = l.Name;
            rank = l.Rank;
            return true;
        }

        /// <summary>How many real landmarks the current map has published.</summary>
        internal static int LandmarkCount => s_Landmarks.Count;

        /// <summary>"27 landmarks, 19 pinned on the Zone map (Court Square courthouse, ...)": the most important first, at
        /// most <paramref name="names"/> of them named (with their ranks when <paramref name="ranks"/>).</summary>
        internal static string LandmarkSummary(int names, bool ranks = false)
        {
            // Get a mutable copy of all published landmarks.
            var all = new List<RealLandmark>(s_Landmarks.Values);
            // Sort landmarks: most important first, then by id for deterministic order.
            all.Sort((x, y) => x.Rank != y.Rank ? y.Rank.CompareTo(x.Rank) : string.CompareOrdinal(x.Id, y.Id));
            // Count how many of the landmarks are pinned on the Zone map.
            int pinned = 0;
            foreach (KeyValuePair<string, Vector3> kv in HumanUtil.Landmarks())
                if (kv.Key.StartsWith(LandmarkPrefix, System.StringComparison.Ordinal)) pinned++;
            var sb = new System.Text.StringBuilder();
            sb.Append(all.Count).Append(" landmarks, ").Append(pinned).Append(" pinned on the Zone map");
            for (int i = 0; i < all.Count && i < names; i++)
            {
                sb.Append(i == 0 ? " (" : ", ");
                if (ranks) sb.Append(all[i].Rank).Append(' ');
                sb.Append(all[i].Name);
            }
            if (all.Count > 0 && names > 0) sb.Append(all.Count > names ? ", ...)" : ")");
            return sb.ToString();
        }

        // What makes a landmark of a point of interest's kind: its rank (how much it matters to someone finding their
        /// <summary>Determine rank and display noun for a POI kind.</summary>
        // way: 10 the courthouse, 4 a named store) and what to call it when it has no name.
        private static int LandmarkRank(string kind, bool named, out string noun)
        {
            noun = null;
            switch (kind)
            {
                case "courthouse": noun = "courthouse"; return 10;
                case "hospital": noun = "hospital"; return 9;
                case "townhall": noun = "city hall"; return 8;
                case "police": noun = "police station"; return 8;
                case "fire_station": noun = "fire station"; return 8;
                case "prison": noun = "jail"; return 8;
                case "school": noun = "school"; return 7;
                case "university": case "college": noun = "college"; return 7;
                case "post_office": noun = "post office"; return 6;
                case "library": noun = "library"; return 6;
                case "pharmacy": noun = "pharmacy"; return 6;
                case "shop:chemist": noun = "drugstore"; return 6;
                case "shop:weapons": noun = "gun shop"; return 6;
                case "place_of_worship": noun = "church"; return named ? 6 : 3;
                case "clinic": noun = "clinic"; return 5;
                case "fuel": noun = "gas station"; return 5;
                case "shop:supermarket": noun = "supermarket"; return 5;
                case "shop:department_store": case "shop:mall": noun = "department store"; return 5;
                case "shop:doityourself": case "shop:hardware": noun = "hardware store"; return 5;
                case "shop:agrarian": case "shop:farm": case "shop:country_store": noun = "feed store"; return named ? 5 : 4;
                case "shop:variety_store": noun = "dollar store"; return 5;
                case "shop:convenience": noun = "convenience store"; return 5;
                case "shop:alcohol": noun = "liquor store"; return 5;
                case "shop:pawnbroker": noun = "pawn shop"; return 5;
                case "doctors": case "dentist": noun = "doctor's office"; return 5;
                case "veterinary": noun = "vet"; return 5;
                case "community_centre": noun = "community center"; return 4;
                case "bank": noun = "bank"; return 4;
                default:
                    if (kind == null || !named) return 0;
                    return kind.StartsWith("shop:", System.StringComparison.Ordinal) || kind == "restaurant" || kind == "fast_food"
                        || kind == "cafe" || kind == "bar" || kind == "pub" || kind == "cinema" || kind == "theatre" ? 4 : 0;
            }
        }

        // The same for a building by its purpose (a building with no point of interest of its own).
        /// <summary>Determine rank and display noun for a building purpose.</summary>
        private static int LandmarkRank(string purpose, out string noun)
        {
            noun = null;
            switch (purpose)
            {
                case Purposes.Courthouse: noun = "courthouse"; return 10;
                case Purposes.Hospital: noun = "hospital"; return 9;
                case Purposes.Police: noun = "police station"; return 8;
                case Purposes.FireStation: noun = "fire station"; return 8;
                case Purposes.Prison: noun = "jail"; return 8;
                case Purposes.School: noun = "school"; return 7;
                case Purposes.Civic: noun = "town offices"; return 6;
                case Purposes.PostOffice: noun = "post office"; return 6;
                case Purposes.Library: noun = "library"; return 6;
                case Purposes.Pharmacy: noun = "pharmacy"; return 6;
                case Purposes.Guns: noun = "gun shop"; return 6;
                case Purposes.Distillery: noun = "distillery"; return 6;
                case Purposes.Church: noun = "church"; return 6;
                case Purposes.Clinic: noun = "clinic"; return 5;
                case Purposes.Fuel: noun = "gas station"; return 5;
                case Purposes.Grocery: noun = "grocery"; return 5;
                case Purposes.DepartmentStore: noun = "department store"; return 5;
                case Purposes.Hardware: noun = "hardware store"; return 5;
                case Purposes.FarmSupply: noun = "feed store"; return 5;
                case Purposes.Convenience: noun = "convenience store"; return 5;
                case Purposes.Liquor: noun = "liquor store"; return 5;
                case Purposes.Auto: noun = "garage"; return 5;
                case Purposes.Vet: noun = "vet"; return 5;
                case Purposes.Hotel: noun = "hotel"; return 4;
                case Purposes.Bank: noun = "bank"; return 4;
                case Purposes.Community: noun = "hall"; return 4;
                default: return 4;
            }
        }

        // Publishes the site's landmarks (each once a raid, however often its piece is built), the most important first.
        /// <summary>Publish a site's landmarks, ordering by importance and respecting limits.</summary>
        private static void Landmarks(Site site)
        {
            object epoch = Epoch(site);
            // Ensure we have a fresh epoch when the map changes, clearing prior landmarks.
            if (!ReferenceEquals(epoch, s_LandmarkEpoch))
            {
                ForgetLandmarks();
                s_LandmarkEpoch = epoch;
            }
            // Record open ground patches near the start area for helicopter landing spots.
            RecordOpenGround(site);
            // Analyse the site to gather structural info needed for landmark determination.
            SiteInfo info = Analyse(site);
            var found = new List<RealLandmark>();
            var hosts = new HashSet<Footprint>();
            float own = site.Chunked ? site.Half : site.Half + SceneryReach;

            // Process each point of interest (e.g., courthouse, fire station) as a potential landmark.
            foreach (Spot s in site.Area.Points)
            {
                bool named = !string.IsNullOrEmpty(s.Data.name);
                int rank = LandmarkRank(s.Data.kind, named, out string noun);
                if (rank < PublishFrom) continue;
                Vector2 at = s.At;
                float radius = 15f;
                if (info.HousedIn.TryGetValue(s, out Footprint home))
                {
                    at = home.Middle;
                    radius = Mathf.Clamp(Mathf.Sqrt(home.Size) * 0.5f + 9f, 15f, 40f);
                    hosts.Add(home);
                }
                Add(found, site, "p", s.Data.id, named ? s.Data.name : null, noun, at, rank, radius);
            }
            // Process each building footprint that might serve as a landmark (e.g., schools, hospitals).
            foreach (Footprint f in site.Area.Buildings)
            {
                if (hosts.Contains(f) || !f.Inside(own, 0f)) continue;
                string purpose = info.Purpose.TryGetValue(f, out string p) ? p : PurposeOf(f.Data);
                bool named = !string.IsNullOrEmpty(f.Data.name);
                int rank = LandmarkRank(purpose, out string noun);
                // Unnamed, only the ones that matter in a pinch; named, any that's more than a house.
                if (!named && rank < 7) continue;
                if (named && (purpose == Purposes.Home || purpose == Purposes.Apartments || purpose == Purposes.Trailer
                    || purpose == Purposes.Shed || purpose == Purposes.Outbuilding || purpose == Purposes.Garage || purpose == Purposes.None)) continue;
                if (named && purpose == Purposes.Commercial && f.Size < 1500f) rank = 4;
                Add(found, site, "b", f.Data.id, named ? f.Data.name : null, noun, f.Middle, rank,
                    Mathf.Clamp(Mathf.Sqrt(f.Size) * 0.5f + 9f, 15f, 40f));
            }
            // Process special nodes like water towers and memorials as landmarks.
            foreach (Node n in site.Area.Nodes)
            {
                string noun;
                int rank;
                switch (n.Data.kind)
                {
                    case "water_tower": noun = "water tower"; rank = 9; break;
                    case "memorial": noun = "memorial"; rank = string.IsNullOrEmpty(n.Data.name) ? 4 : 5; break;
                    case "mast": noun = "radio mast"; rank = 4; break;
                    default: continue;
                }
                Add(found, site, "n", n.Data.id, string.IsNullOrEmpty(n.Data.name) ? null : n.Data.name, noun, n.At, rank, 15f);
            }
            // Process larger area features such as cemeteries and parks as landmarks.
            foreach (Patch a in site.Area.Areas)
            {
                bool named = !string.IsNullOrEmpty(a.Data.name);
                string noun;
                int rank;
                switch (a.Data.kind)
                {
                    case "cemetery": noun = "cemetery"; rank = named ? 6 : 5; break;
                    case "airfield": noun = "airfield"; rank = 7; break;
                    case "military": noun = "military post"; rank = 7; break;
                    case "plaza": noun = "square"; rank = named ? 5 : 0; break;
                    case "park": case "golf": case "pitch": noun = "park"; rank = named ? 5 : 0; break;
                    default: continue;
                }
                if (rank < PublishFrom) continue;
                Vector2 mid = MapGeometry.Middle(a.Pts);
                Rect r = MapGeometry.Bounds(a.Pts);
                Add(found, site, "a", a.Data.id, named ? a.Data.name : null, noun, mid, rank,
                    Mathf.Clamp(Mathf.Min(r.width, r.height) * 0.5f, 15f, 60f));
            }

            found.Sort((x, y) => x.Rank != y.Rank ? y.Rank.CompareTo(x.Rank) : string.CompareOrdinal(x.Id, y.Id));
            int cap = site.Chunked ? 14 : 60, published = 0;
            Transform host = KentuckyStreamer.Active != null ? KentuckyStreamer.Active.World : site.Town.World;
            foreach (RealLandmark l in found)
            {
                if (published >= cap) break;
                if (l.Rank < PublishFrom || s_Landmarks.ContainsKey(l.Id)) continue;
                if (SameNearby(l)) continue;
                s_Landmarks[l.Id] = l;
                WorldMarkers.Add(WorldMarkers.Landmark(l.Id), l.At);
                if (l.Rank >= DiscoverFrom)
                    MapGameplayRuntime.Track(l.Id, l.Proper ? l.Name : "the " + l.Name, l.At, l.Radius, l.Rank >= RewardFrom, host);
                published++;
            }
        }

        // A landmark already published nearby under the same name (a church's point and its building, an area cut into
        // pieces by the chunks) stands for this one.
        private static bool SameNearby(RealLandmark l)
        {
            foreach (RealLandmark o in s_Landmarks.Values)
                if (o.Name == l.Name && (o.At - l.At).sqrMagnitude < 90f * 90f) return true;
            return false;
        }

        private static void Add(List<RealLandmark> into, Site site, string type, long id, string name, string noun, Vector2 at,
            int rank, float radius)
        {
            bool proper = name != null;
            if (!proper) name = Describe(site, noun, at);
            if (name == null) return;
            if (name.Length > 48) name = name.Substring(0, 48).TrimEnd();
            into.Add(new RealLandmark
            {
                Id = LandmarkPrefix + type + id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Name = name, Proper = proper, At = site.WorldOffset + To3(at, 0f), Rank = rank, Radius = radius,
            });
        }

        // What to call a landmark without a name of its own: by the square it stands on ("Court Square courthouse",
        // "memorial on Court Square"), the town it's in ("Bardstown fire station"), else the road it's on ("gas station
        // on KY 245").
        private static string Describe(Site site, string noun, Vector2 at)
        {
            if (noun == null) return null;
            string street = NearestStreetName(site, at, 45f);
            if (street != null && street.EndsWith(" Square", System.StringComparison.Ordinal))
                return noun == "courthouse" ? street + " courthouse" : noun + " on " + street;
            StrongestPlace(site.WorldOffset + To3(at, 0f), out MapPlace place, out float d);
            if (place != null && Within(place, d)) return place.name + " " + noun;
            if (street != null) return noun + " on " + street;
            return place != null ? noun + " near " + place.name : noun;
        }

        private static string NearestStreetName(Site site, Vector2 at, float within)
        {
            string best = null;
            float bestDist = within * within;
            foreach (Street s in site.Area.Streets)
            {
                if (s.Rank <= 0 || string.IsNullOrEmpty(s.Data.name)) continue;
                for (int i = 0; i + 1 < s.Pts.Count; i++)
                {
                    Vector2 a = s.Pts[i], ab = s.Pts[i + 1] - a;
                    float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(at - a, ab) / ab.sqrMagnitude) : 0f;
                    float d = (a + ab * t - at).sqrMagnitude;
                    if (d >= bestDist) continue;
                    bestDist = d;
                    best = s.Data.name;
                }
            }
            return best;
        }
    }
}
