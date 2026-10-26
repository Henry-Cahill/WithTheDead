using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Footsteps that sound like what is underfoot, for everything that walks. The survivor's steps keep time with
    /// their own body's stride (the controller's step event when there is no body to follow); everyone else's — the
    /// dead, stalkers, other runners, the Zone's beasts — are timed by <see cref="FootstepDirector"/> from how each one
    /// moves. The surface comes from what the foot stands on: water (the Water layer); a collider's material, texture and
    /// name (the decor's snow, asphalt, concrete, gravel, linoleum, wood floor, planks, metal, corrugated sheet, carpet,
    /// rugs, grass, mulch); under a roof, the floor of the building (boards in a house, linoleum in a shop, concrete in a
    /// shed); outdoors, the season's ground (snow in winter, otherwise grass, or leaf litter deep in the woods). Rain soaks
    /// the ground outdoors over half a minute and it dries over minutes: wet asphalt, boards and sheet metal splash, bare
    /// earth turns to mud and snow to slush.
    /// </summary>
    public static class FootstepAudio
    {
        /// <summary>What a foot can come down on.</summary>
        internal enum Surface : byte { Snow, Wood, Concrete, Grass, Gravel, Dirt, Mud, Metal, Carpet, Tile, Leaves, Water }

        /// <summary>What is under a foot: the surface, whether it is under a roof (rain wets only the outdoors), and whether
        /// it is wet whatever the weather (a wet-asphalt road).</summary>
        internal struct Ground
        {
            public Surface Surface;
            public bool Indoors, Wet;
        }

        private const int WaterMask = 1 << 4;   // the Water layer
        private const int WetBit = 64;

        // What each collider's names say about it (surface | WetBit, or -1 for nothing): read once per collider.
        private static readonly Dictionary<Collider, int> s_Named = new Dictionary<Collider, int>(256);
        private static readonly List<Bounds> s_Shelters = new List<Bounds>(4);
        private static float s_Wet;
        private static WeatherSystem s_WetFrom;
        private static PlayerController s_Hooked;

        #region Entry points
        /// <summary>Starts whatever keeps the footsteps of the world going (called once by the audio module).</summary>
        public static void Ensure() => FootstepDirector.Ensure();

        /// <summary>Hooks the survivor's own steps (and drops landed) to their body and controller.</summary>
        public static void OnPlayerBuilt(PlayerStats p)
        {
            if (p == null) return;
            FootstepDirector.Ensure();
            FootstepDirector.SetSurvivor(p);
            var controller = p.GetComponent<PlayerController>();
            if (controller == null || controller == s_Hooked) return;
            s_Hooked = controller;
            Transform t = p.transform;
            // The director keeps the steps in time with the body's stride; the controller's own beat is the fallback.
            controller.Footstep += (sprint, sneak) =>
            {
                if (t != null && !FootstepDirector.TimesSurvivor(t)) Survivor(t.position, sprint, sneak);
            };
            controller.Landed += speed =>
            {
                if (t != null) Landing(t.position, speed);
            };
        }

        /// <summary>The footstep id for the ground under <paramref name="feet"/> (wet or not, as the weather has it).</summary>
        public static string SurfaceAt(Vector3 feet) => IdOf(Probe(feet));

        /// <summary>A booted step at <paramref name="feet"/> on whatever is there, walking, sprinting or sneaking.</summary>
        public static void Play(Vector3 feet, bool sprinting, bool sneaking, float loudness = 1f)
        {
            string id = IdOf(Probe(feet));
            float vol = (sneaking ? 0.2f : sprinting ? 0.7f : 0.45f) * loudness * Trim(id);
            float range = sneaking ? 10f : sprinting ? 30f : 20f;
            float pitch = Random.Range(0.94f, 1.06f) * (sneaking ? 0.96f : sprinting ? 1.02f : 1f);
            Sfx.Play(id, feet + Vector3.up * 0.05f, vol, pitch, range);
        }
        #endregion

        #region The survivor
        /// <summary>One of the survivor's steps: the boot on the ground, and the kit jostling on a sprint.</summary>
        internal static void Survivor(Vector3 feet, bool sprinting, bool sneaking)
        {
            Play(feet, sprinting, sneaking);
            if (sprinting && Random.value < 0.7f)
                Sfx.Play(SfxIds.StepGear, feet + Vector3.up * 1.1f, Random.Range(0.14f, 0.22f), Random.Range(0.92f, 1.08f), 14f);
        }

        // A drop landed: both boots slam down on the ground there, the weight thumping through, harder from higher.
        private static void Landing(Vector3 feet, float speed)
        {
            float k = Mathf.InverseLerp(4.5f, 11f, speed);
            string id = IdOf(Probe(feet));
            Sfx.Play(id, feet + Vector3.up * 0.05f, (0.55f + 0.35f * k) * Trim(id), Random.Range(0.86f, 0.94f), 24f);
            Sfx.Play(SfxIds.StepLand, feet + Vector3.up * 0.4f, 0.35f + 0.55f * k, Random.Range(0.94f, 1.04f) - 0.06f * k, 18f + 20f * k);
        }
        #endregion

        #region Surfaces
        /// <summary>What is under <paramref name="feet"/> (one ray down; the names it meets are remembered per collider).</summary>
        internal static Ground Probe(Vector3 feet)
        {
            var g = new Ground { Indoors = Shelter.Count > 0 && Shelter.IsSheltered(feet + Vector3.up) };
            // Wading: anything on the Water layer, its trigger volumes included.
            if (Physics.CheckSphere(feet + Vector3.up * 0.2f, 0.3f, WaterMask, QueryTriggerInteraction.Collide))
            {
                g.Surface = Surface.Water;
                return g;
            }
            if (Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                int named = (WaterMask & (1 << hit.collider.gameObject.layer)) != 0 ? (int)Surface.Water : Named(hit.collider);
                if (named >= 0)
                {
                    var s = (Surface)(named & ~WetBit);
                    // Under a roof, the snow or earth a ray finds is the ground beneath a floor that has no collider of its own.
                    if (!(g.Indoors && OutdoorGround(s)))
                    {
                        g.Surface = s;
                        g.Wet = (named & WetBit) != 0;
                        return g;
                    }
                }
            }
            g.Surface = g.Indoors ? IndoorFloor(feet) : Outdoors(feet);
            return g;
        }

        /// <summary>Hard ground: claws click on it, hooves clop, the dead scuff rather than crunch.</summary>
        internal static bool IsHard(Surface s) => s == Surface.Wood || s == Surface.Concrete || s == Surface.Metal || s == Surface.Tile;

        /// <summary>The footstep id for <paramref name="g"/>: its wet version, now and then, as the ground soaks up the rain
        /// (puddles come in patches), and slush where snow meets rain or thaw.</summary>
        internal static string IdOf(Ground g)
        {
            float wet = g.Wet ? 1f : g.Indoors ? 0f : s_Wet;
            bool soaked = wet > 0.12f && Random.value < Mathf.Clamp01((wet - 0.12f) / 0.35f);
            switch (g.Surface)
            {
                case Surface.Snow:
                    float slush = Mathf.Clamp01((wet - 0.2f) / 0.4f);
                    if (!g.Indoors && Climate.Winter) slush += 0.5f * Mathf.Clamp01((Climate.AirTemperatureC - 3f) / 8f);
                    return Random.value < slush ? SfxIds.StepSlush : SfxIds.StepSnow;
                case Surface.Wood: return soaked ? SfxIds.StepWoodWet : SfxIds.StepWood;
                case Surface.Concrete: return soaked ? SfxIds.StepConcreteWet : SfxIds.StepConcrete;
                case Surface.Tile: return soaked ? SfxIds.StepConcreteWet : SfxIds.StepTile;
                case Surface.Metal: return soaked ? SfxIds.StepMetalWet : SfxIds.StepMetal;
                case Surface.Dirt: return soaked ? SfxIds.StepMud : SfxIds.StepDirt;
                case Surface.Grass: return SfxIds.StepGrass;
                case Surface.Gravel: return SfxIds.StepGravel;
                case Surface.Mud: return SfxIds.StepMud;
                case Surface.Carpet: return SfxIds.StepCarpet;
                case Surface.Leaves: return SfxIds.StepLeaves;
                case Surface.Water: return SfxIds.StepWater;
                default: return SfxIds.StepConcrete;
            }
        }

        /// <summary>How loud each surface's step plays beside the others (their renders differ in weight): metal and
        /// mud carry, carpet hushes.</summary>
        internal static float Trim(string id)
        {
            switch (id)
            {
                case SfxIds.StepWood: case SfxIds.StepWoodWet: case SfxIds.StepSlush: return 0.85f;
                case SfxIds.StepConcrete: case SfxIds.StepTile: case SfxIds.StepGravel: return 0.9f;
                case SfxIds.StepConcreteWet: case SfxIds.StepMud: case SfxIds.StepMetal: return 0.8f;
                case SfxIds.StepMetalWet: case SfxIds.StepCarpet: return 0.75f;
                case SfxIds.StepWater: return 0.95f;
                default: return 1f;
            }
        }

        /// <summary>How wet the ground outdoors is, 0..1: it soaks up the rain (and sleet) over half a minute and dries
        /// over minutes. Advanced by the director every frame.</summary>
        internal static void TickWetness(float dt)
        {
            WeatherSystem w = WeatherSystem.Instance;
            float target = w != null ? Mathf.Clamp01(w.WettingIntensity * 1.5f) : 0f;
            if (w != s_WetFrom)
            {
                // A new world: its ground is as wet as its weather.
                s_WetFrom = w;
                s_Wet = target;
                return;
            }
            float tau = target > s_Wet ? 25f : 240f;
            s_Wet += (target - s_Wet) * (1f - Mathf.Exp(-Mathf.Max(0f, dt) / tau));
        }

        // The ground an outdoor survey would expect: snow over everything in winter; otherwise grass, or leaf litter deep
        // in the woods.
        private static Surface Outdoors(Vector3 feet)
        {
            if (Climate.Winter || Climate.SnowFraction > 0.5f) return Surface.Snow;
            if (ZoneForest.TreeCount > 0 && ZoneForest.Density(feet) > 0.35f) return Surface.Leaves;
            return Surface.Grass;
        }

        private static bool OutdoorGround(Surface s)
            => s == Surface.Snow || s == Surface.Grass || s == Surface.Dirt || s == Surface.Mud || s == Surface.Leaves || s == Surface.Gravel;

        // Indoors, with nothing named underfoot: the floor guessed from the building, by the size of the sheltered span
        // over the feet — the hub's boards; a house's boards; a shop's, an office's or a school's linoleum (wide, or
        // tall-ceilinged); a warehouse's, a barn's or a garage's concrete (wider still).
        private static Surface IndoorFloor(Vector3 feet)
        {
            if (GameSession.IsHub) return Surface.Wood;
            Shelter.Overlapping(new Bounds(feet + Vector3.up, new Vector3(0.2f, 0.2f, 0.2f)), s_Shelters);
            float span = 0f, high = 0f;
            for (int i = 0; i < s_Shelters.Count; i++)
            {
                Vector3 size = s_Shelters[i].size;
                span = Mathf.Max(span, Mathf.Max(size.x, size.z));
                high = Mathf.Max(high, size.y);
            }
            if (span >= 20f) return Surface.Concrete;
            if (span >= 13f || high >= 3.5f) return Surface.Tile;
            return Surface.Wood;
        }

        private static int Named(Collider c)
        {
            if (s_Named.TryGetValue(c, out int s)) return s;
            if (s_Named.Count > 4096) s_Named.Clear();
            s = Classify(c);
            s_Named[c] = s;
            return s;
        }

        // A car's body is sheet steel whatever it is painted; otherwise the material's name, its texture's (the decor
        // names its textures for what they show: Raid_snow, Raid_asphalt_dry, Raid_wood…, Raid_corr…, Raid_rug3…) and the
        // object's own ("Deck", "Grate", "Puddle"), the most telling word winning.
        private static int Classify(Collider c)
        {
            if (c.GetComponentInParent<Vehicle>() != null) return (int)Surface.Metal;
            string names = c.name;
            var r = c.GetComponent<Renderer>();
            Material m = r != null ? r.sharedMaterial : null;
            if (m != null)
            {
                Texture tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                names = m.name + " " + (tex != null ? tex.name : "") + " " + names;
            }
            return FromNames(names);
        }

        private static int FromNames(string names)
        {
            if (string.IsNullOrEmpty(names)) return -1;
            string s = names.ToLowerInvariant();
            if (s.Contains("palettemetal")) return (int)Surface.Metal;   // the decor's flat metal colours
            int best = -1, bestWeight = 0;
            bool wet = false;
            for (int i = 0; i < s.Length;)
            {
                while (i < s.Length && !char.IsLetter(s[i])) i++;
                int start = i;
                while (i < s.Length && char.IsLetter(s[i])) i++;
                if (i == start) break;
                string word = s.Substring(start, i - start);
                if (word == "wet" || word == "puddle" || word == "puddles") wet = true;
                int surface = Word(word, out int weight);
                if (surface >= 0 && weight > bestWeight)
                {
                    best = surface;
                    bestWeight = weight;
                }
            }
            if (best < 0) return -1;
            return wet ? best | WetBit : best;
        }

        // The surface a word names (or -1), and how much it says: 3 for water and mud, 2 for a material, 1 for a kind of
        // place that usually has one ("floor", "road", "car").
        private static int Word(string w, out int weight)
        {
            weight = 2;
            switch (w)
            {
                case "water": case "waters": case "puddle": case "puddles": case "pond": case "creek": case "river": case "lake": case "swamp": case "marsh": case "stream":
                    weight = 3; return (int)Surface.Water;
                case "mud": case "muddy": case "bog": case "sludge": case "mire":
                    weight = 3; return (int)Surface.Mud;
                case "slush": case "drift": case "drifts": return (int)Surface.Snow;
                case "ballast": case "rubble": case "cinder": case "cinders": case "pebble": case "pebbles": case "scree": case "shingle": return (int)Surface.Gravel;
                case "rug": case "rugs": case "mat": case "doormat": return (int)Surface.Carpet;
                case "iron": case "tin": case "corr": case "grate": case "grates": case "sheet": case "hatch": case "manhole": case "drain": case "chrome": return (int)Surface.Metal;
                case "board": case "boards": case "crate": case "crates": case "lumber": return (int)Surface.Wood;
                case "woods": weight = 1; return (int)Surface.Leaves;
                case "leaf": case "leaves": case "litter": case "needles": case "foliage": return (int)Surface.Leaves;
                case "reed": case "reeds": case "hay": case "moss": case "scrub": case "weeds": return (int)Surface.Grass;
                case "sand": case "yard": case "bank": case "banks": return (int)Surface.Dirt;
                case "slab": case "slabs": case "kerb": case "curb": case "stone": case "stones": return (int)Surface.Concrete;
                case "floor": case "floors": case "deck": case "decking": case "porch": case "stage":
                    weight = 1; return (int)Surface.Wood;
                case "vehicle": case "car": case "truck": case "van": case "bus": case "hood": case "container": case "dumpster":
                    weight = 1; return (int)Surface.Metal;
                case "forest": weight = 1; return (int)Surface.Leaves;
                case "field": case "fields": case "park": weight = 1; return (int)Surface.Grass;
                case "path": case "trail": case "garden": weight = 1; return (int)Surface.Dirt;
                case "road": case "roads": case "street": case "lot": case "parking": case "garage": case "platform": case "ramp":
                    weight = 1; return (int)Surface.Concrete;
            }
            if (w.StartsWith("snow")) return (int)Surface.Snow;
            if (w.StartsWith("gravel")) return (int)Surface.Gravel;
            if (w.StartsWith("carpet")) return (int)Surface.Carpet;
            if (w.StartsWith("tile") || w.StartsWith("lino") || w.StartsWith("vinyl") || w.StartsWith("ceramic") || w.StartsWith("marble") || w.StartsWith("terrazzo"))
                return (int)Surface.Tile;
            if (w.StartsWith("metal") || w.StartsWith("steel") || w.StartsWith("corrugat") || w.StartsWith("grating")) return (int)Surface.Metal;
            if (w.StartsWith("wood") || w.StartsWith("plank") || w.StartsWith("timber") || w.StartsWith("parquet") || w.StartsWith("plywood") || w.StartsWith("pallet") || w.StartsWith("floorboard"))
                return (int)Surface.Wood;
            if (w.StartsWith("grass") || w.StartsWith("lawn") || w.StartsWith("turf") || w.StartsWith("meadow") || w.StartsWith("pasture") || w.StartsWith("stubble"))
                return (int)Surface.Grass;
            if (w.StartsWith("dirt") || w.StartsWith("soil") || w.StartsWith("earth") || w.StartsWith("mulch") || w.StartsWith("furrow") || w.StartsWith("farmyard") || w.StartsWith("trodden") || w.StartsWith("clay"))
                return (int)Surface.Dirt;
            if (w.StartsWith("asphalt") || w.StartsWith("tarmac") || w.StartsWith("concrete") || w.StartsWith("cement") || w.StartsWith("pavement") || w.StartsWith("paver")
                || w.StartsWith("sidewalk") || w.StartsWith("cobble") || w.StartsWith("brick") || w.StartsWith("forecourt"))
                return (int)Surface.Concrete;
            weight = 0;
            return -1;
        }
        #endregion
    }

    /// <summary>
    /// The footsteps of everyone near the ears but the survivor (and the survivor's own, kept in time with their body).
    /// Every frame it looks over a slice of <see cref="Actor.All"/> for walkers within earshot (about 30 m; a bear or a
    /// Thumper further) — skipping the dead, anyone driving, and the survivor — and follows them: how fast each moves (from
    /// its position, so the server's agents and the network's interpolated runners alike), and when each foot comes down:
    /// from a humanoid's own stride (the shuffle, lurch, sprint or crawl its body is animating), from a beast's gait cycle
    /// (as its body swings its legs). Only the nearest eight (the loudest counting for more) are heard. The dead shuffle
    /// and drag their feet (a limping one drags the bad leg), stumble, pound along at a sprint or haul themselves on their
    /// palms; stalkers and runners walk, creep and run in boots; wolves and hounds pad (claws ticking on hard ground), deer
    /// clop or thud, a bear's weight comes down, a Thumper shakes the ground, a Shade barely touches it and a Puppeteer
    /// drags. Hub folk, runners and all, are heard as well; nothing walks while the game is paused.
    /// </summary>
    [DefaultExecutionOrder(9500)]
    internal sealed class FootstepDirector : MonoBehaviour
    {
        private const float TrackRange = 30f, HeavyRange = 46f, ForgetSlack = 4f, MaxSpeed = 14f;
        private const int MaxTracked = 24, MaxVoices = 8, ScanPerFrame = 24;
        private const float HalfPi = Mathf.PI * 0.5f;

        private enum Body : byte { Boots, Shambler, Lurcher, Sprinter, Crawler, Returned, Wolf, Hound, Deer, Bear, Thumper, Shade, Puppeteer }

        private sealed class Walker
        {
            public Actor Actor;
            public Transform T;
            public IDamageable Damage;
            public Body Body;
            public HumanoidAnimator Anim;
            public Creature Creature;
            public CreatureBody Beast;
            public ZombieAI Zombie;
            public HumanNpc Human;
            public RemotePlayer Remote;
            public PlayerController Controller;
            public PlayerStats Stats;
            public float RunSpeed = 6f;
            public Vector3 Last;
            public float Speed, Cycle, Dist, Score;
            public int Half, Steps, Rank;
            public bool Primed, Driving;
            public float Recheck;
            public FootstepAudio.Ground Ground;
            public Vector3 ProbedAt;
            public float ProbeAgain;
        }

        private static FootstepDirector s_Instance;
        private static Dictionary<string, string> s_SpeciesByName;

        private readonly List<Walker> _walkers = new List<Walker>(MaxTracked + 1);
        private readonly HashSet<Actor> _tracked = new HashSet<Actor>();
        private Walker _survivor;
        private int _scan;

        /// <summary>Creates the director (it outlives scenes) if it is not running.</summary>
        internal static void Ensure()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[Footsteps]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<FootstepDirector>();
        }

        /// <summary>The survivor whose steps follow their own body from now on.</summary>
        internal static void SetSurvivor(PlayerStats p)
        {
            if (s_Instance == null || p == null) return;
            var w = new Walker
            {
                Actor = p.GetComponent<Actor>(),
                T = p.transform,
                Stats = p,
                Controller = p.GetComponent<PlayerController>(),
                Last = p.transform.position,
            };
            s_Instance._survivor = w;
            ClassifySurvivor(w);
        }

        /// <summary>True while the director keeps the survivor at <paramref name="t"/> in step with their body (the
        /// controller's own step beat is then not played).</summary>
        internal static bool TimesSurvivor(Transform t)
        {
            if (s_Instance == null || !s_Instance.isActiveAndEnabled) return false;
            Walker w = s_Instance._survivor;
            return w != null && w.T == t && (w.Anim != null || w.Beast != null);
        }

        private void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            FootstepAudio.TickWetness(dt);
            if (dt <= 0f || Time.timeScale <= 0f || !Sfx.Enabled) return;   // paused: nothing walks
            Vector3 ears = Sfx.ListenerPosition;
            Survivor(dt);
            Scan(ears);
            Follow(ears, dt);
        }

        #region Finding walkers
        // A slice of the actors a frame: anyone new within earshot is followed (the nearer replacing the farthest when full).
        private void Scan(Vector3 ears)
        {
            List<Actor> all = Actor.All;
            int n = all.Count;
            Actor local = Actor.LocalPlayer, survivor = _survivor != null ? _survivor.Actor : null;
            int far = _walkers.Count >= MaxTracked ? Farthest() : -1;
            for (int k = 0, budget = Mathf.Min(n, ScanPerFrame); k < budget; k++)
            {
                if (_scan >= n) _scan = 0;
                Actor a = all[_scan++];
                if (a == null || a == local || a == survivor || a.Kind == ActorKind.Player || !a.isActiveAndEnabled) continue;
                if (_tracked.Contains(a)) continue;
                bool beast = a.Kind == ActorKind.Mutant || a.Kind == ActorKind.Animal || a.Kind == ActorKind.PlayerMonster;
                float d2 = (a.transform.position - ears).sqrMagnitude;
                if (d2 > (beast ? HeavyRange * HeavyRange : TrackRange * TrackRange)) continue;
                float d = Mathf.Sqrt(d2);
                if (far >= 0 && _walkers[far].Dist < d + 2f) continue;   // full, and no nearer than those followed
                Walker w = Make(a);
                if (w == null || d > Reach(w.Body)) continue;
                w.Dist = d;
                if (far >= 0) Drop(far);
                _walkers.Add(w);
                _tracked.Add(a);
                far = _walkers.Count >= MaxTracked ? Farthest() : -1;
            }
        }

        private int Farthest()
        {
            int far = -1;
            for (int i = 0; i < _walkers.Count; i++)
                if (far < 0 || _walkers[i].Dist > _walkers[far].Dist) far = i;
            return far;
        }

        // Unity objects hash and compare by their id, so a destroyed actor's entry still comes out.
        private void Drop(int i)
        {
            _tracked.Remove(_walkers[i].Actor);
            _walkers.RemoveAt(i);
        }

        private static Walker Make(Actor a)
        {
            var w = new Walker { Actor = a, T = a.transform, Damage = a.Damageable };
            if (w.Damage != null && w.Damage.IsDead) return null;
            w.Last = w.T.position;
            w.Zombie = a.GetComponent<ZombieAI>();
            w.Creature = a.GetComponent<Creature>();
            w.Human = a.GetComponent<HumanNpc>();
            w.Remote = a.GetComponent<RemotePlayer>();
            w.Anim = a.GetComponentInChildren<HumanoidAnimator>(true);
            w.Beast = a.GetComponentInChildren<CreatureBody>(true);
            Classify(w);
            return w;
        }

        // What kind of feet: a beast by its species; the dead by their type (a proxy's from its pace, at each step); the
        // Returned; boots for the rest.
        private static void Classify(Walker w)
        {
            string species = Species(w);
            if (species != null)
            {
                w.Body = BodyOf(species);
                CreatureDef def = CreatureCatalog.Get(species);
                if (def != null) w.RunSpeed = def.RunSpeed;
                return;
            }
            Actor a = w.Actor;
            if (a.Kind == ActorKind.Zombie)
            {
                ZombieType type = w.Zombie != null ? w.Zombie.Type : ZombieType.Shambler;
                w.Body = type == ZombieType.Sprinter ? Body.Sprinter : type == ZombieType.FastShambler ? Body.Lurcher : type == ZombieType.Crawler ? Body.Crawler : Body.Shambler;
                return;
            }
            w.Body = a.GetComponent<ReturnedFigure>() != null ? Body.Returned : Body.Boots;
        }

        private static string Species(Walker w)
        {
            if (w.Creature != null && w.Creature.Def != null) return w.Creature.Def.Id;
            string held = w.Remote != null ? w.Remote.Held : null;
            if (!string.IsNullOrEmpty(held) && held.StartsWith("form:")) return held.Substring(5);
            Actor a = w.Actor;
            if (a.Kind != ActorKind.Mutant && a.Kind != ActorKind.Animal) return null;
            // A beast seen over the network: its name is its species'.
            if (s_SpeciesByName == null)
            {
                s_SpeciesByName = new Dictionary<string, string>();
                foreach (string id in CreatureCatalog.AllIds)
                {
                    CreatureDef def = CreatureCatalog.Get(id);
                    if (def != null && !string.IsNullOrEmpty(def.Name)) s_SpeciesByName[def.Name] = id;
                }
            }
            return a.DisplayName != null && s_SpeciesByName.TryGetValue(a.DisplayName, out string species) ? species : CreatureCatalog.Hound;
        }

        private static Body BodyOf(string species)
        {
            switch (species)
            {
                case CreatureCatalog.Wolf: return Body.Wolf;
                case CreatureCatalog.Deer: return Body.Deer;
                case CreatureCatalog.Bear: return Body.Bear;
                case CreatureCatalog.Thumper: return Body.Thumper;
                case CreatureCatalog.Shade: return Body.Shade;
                case CreatureCatalog.Puppeteer: return Body.Puppeteer;
                default: return Body.Hound;
            }
        }

        // How far a body's steps are worth following.
        private static float Reach(Body b) => b == Body.Thumper ? HeavyRange : b == Body.Bear ? 40f : b == Body.Shade ? 14f : TrackRange;

        // How much a body's steps count when only so many are heard (the loudest carry over nearer, quieter ones).
        private static float Weight(Body b)
        {
            switch (b)
            {
                case Body.Thumper: return 3f;
                case Body.Bear: return 2f;
                case Body.Sprinter: return 1.6f;
                case Body.Lurcher: case Body.Deer: return 1.2f;
                case Body.Shade: return 0.8f;
                default: return 1f;
            }
        }

        private static void ClassifySurvivor(Walker w)
        {
            PlayerMonster hunter = w.T != null ? w.T.GetComponent<PlayerMonster>() : null;
            if (hunter != null && hunter.Def != null)
            {
                w.Body = BodyOf(hunter.Def.Id);
                w.RunSpeed = hunter.Def.RunSpeed;
                w.Beast = w.T.GetComponentInChildren<CreatureBody>(true);
                w.Anim = null;
                return;
            }
            w.Body = Body.Boots;
            w.Beast = null;
            var avatar = w.T != null ? w.T.GetComponent<PlayerAvatar>() : null;
            w.Anim = avatar != null ? avatar.Animator : null;
        }
        #endregion

        #region Following them
        private void Survivor(float dt)
        {
            Walker w = _survivor;
            if (w == null) return;
            if (w.T == null) { _survivor = null; return; }
            if (Time.time >= w.Recheck)
            {
                w.Recheck = Time.time + 1f;
                ClassifySurvivor(w);
            }
            Track(w, dt);
            if (w.Stats != null && (w.Stats.IsDead || w.Stats.Incapacitated)) { w.Primed = false; return; }
            if (!Stride(w, dt)) return;
            Vector3 feet = w.T.position;
            if (w.Body == Body.Boots)
            {
                bool sneak = w.Controller != null && w.Controller.IsSneaking;
                bool sprint = !sneak && w.Stats != null && w.Stats.Exertion == Exertion.Sprinting;
                FootstepAudio.Survivor(feet, sprint, sneak);
            }
            else Step(w, feet);
        }

        private void Follow(Vector3 ears, float dt)
        {
            for (int i = _walkers.Count - 1; i >= 0; i--)
            {
                Walker w = _walkers[i];
                Actor a = w.Actor;
                if (a == null || !a.isActiveAndEnabled || w.T == null || (w.Damage != null && w.Damage.IsDead) || a == Actor.LocalPlayer)
                {
                    Drop(i);
                    continue;
                }
                w.Dist = Vector3.Distance(w.T.position, ears);
                if (w.Dist > Reach(w.Body) + ForgetSlack)
                {
                    Drop(i);
                    continue;
                }
                if (Time.time >= w.Recheck)
                {
                    w.Recheck = Time.time + Random.Range(0.8f, 1.2f);
                    w.Driving = Driving(a);
                    if (w.Remote != null)
                    {
                        // A runner who turned out to be a hunter wears the creature's body from then on.
                        w.Beast = a.GetComponentInChildren<CreatureBody>(true);
                        Classify(w);
                    }
                }
                Track(w, dt);
                // Only those on the move compete for the voices (a corpse-walker standing still takes none).
                w.Score = w.Speed > 0.15f ? w.Dist / Weight(w.Body) : float.MaxValue;
            }
            // The nearest few (by weighted distance) are heard; the rest keep their stride so none joins mid-step.
            for (int i = 0; i < _walkers.Count; i++)
            {
                int rank = 0;
                float s = _walkers[i].Score;
                for (int j = 0; j < _walkers.Count; j++)
                    if (j != i && (_walkers[j].Score < s || (_walkers[j].Score == s && j < i))) rank++;
                _walkers[i].Rank = rank;
            }
            for (int i = 0; i < _walkers.Count; i++)
            {
                Walker w = _walkers[i];
                if (w.Driving) { w.Primed = false; continue; }
                if (Stride(w, dt) && w.Rank < MaxVoices) Step(w, w.T.position);
            }
        }

        // Ground speed from where the body actually went (agents and interpolated network bodies alike).
        private static void Track(Walker w, float dt)
        {
            Vector3 p = w.T.position;
            Vector3 d = p - w.Last;
            w.Last = p;
            d.y = 0f;
            float raw = d.magnitude / dt;
            if (raw > MaxSpeed)
            {
                // A teleport, or a ride: no stride.
                raw = 0f;
                w.Primed = false;
            }
            w.Speed += (raw - w.Speed) * (1f - Mathf.Exp(-dt * 7f));
        }

        // Driving a car (a runner in someone's driver's seat): no steps.
        private static bool Driving(Actor a)
        {
            if (a.OwnerClientId == ulong.MaxValue) return false;
            for (int i = 0; i < Vehicle.All.Count; i++)
            {
                Vehicle v = Vehicle.All[i];
                if (v != null && v.RemoteDriver == a.OwnerClientId) return true;
            }
            return false;
        }

        // True when a foot comes down this frame. A humanoid's from its own animated stride (a heel strikes as the leg
        // reaches furthest forward; a crawler's palm as the arm does); a beast's from a gait cycle run as its body runs
        // it (two footfalls a cycle, diagonal pairs); anything else's from a stride cycle at the pace it moves.
        private static bool Stride(Walker w, float dt)
        {
            HumanoidAnimator anim = w.Anim;
            if (anim != null && w.Beast == null)
            {
                if (!anim.isActiveAndEnabled || anim.IsDead || anim.IsLying || anim.Seated) { w.Primed = false; return false; }
                float offset = anim.Gait == HumanoidGait.Crawler ? 0f : HalfPi;
                int half = Mathf.FloorToInt((anim.GaitPhase - offset) / Mathf.PI);
                if (!w.Primed)
                {
                    w.Primed = true;
                    w.Half = half;
                    return false;
                }
                int dh = half - w.Half;
                w.Half = half;
                if (dh == 0 || dh > 2 || dh < -2) return false;   // no step, or the phase wrapped
                return w.Speed > (anim.Gait == HumanoidGait.Crawler ? 0.12f : 0.3f);
            }
            float omega = Omega(w);
            if (omega <= 0f || w.Speed < 0.2f)
            {
                w.Primed = false;
                return false;
            }
            if (!w.Primed)
            {
                w.Primed = true;
                w.Cycle = Random.Range(0f, Mathf.PI);
                w.Half = Mathf.FloorToInt(w.Cycle / Mathf.PI);
                return false;
            }
            w.Cycle += omega * dt;
            if (w.Cycle > 2000f)
            {
                w.Cycle -= 600f * Mathf.PI;
                w.Half -= 600;
            }
            int h = Mathf.FloorToInt(w.Cycle / Mathf.PI);
            if (h == w.Half) return false;
            w.Half = h;
            return true;
        }

        // The gait cycle's speed (radians a second): a beast's as its body swings its legs (CreatureBody), else a
        // humanoid stride's at this pace (as HumanoidAnimator strides).
        private static float Omega(Walker w)
        {
            if (w.Creature != null || w.Beast != null)
            {
                float s01;
                if (w.Creature != null) s01 = w.Creature.Speed01;
                else if (w.Beast.Driven) s01 = w.Beast.ExternalSpeed01;
                else s01 = Mathf.Clamp01(w.Speed / Mathf.Max(1f, w.RunSpeed));
                return s01 > 0.02f ? Mathf.Lerp(2f, 11f, s01) : 0f;
            }
            float len = Mathf.Clamp(0.95f + 0.33f * w.Speed, 1f, 3f) * (w.Body == Body.Shambler || w.Body == Body.Crawler || w.Body == Body.Returned ? 0.8f : 1f);
            return 2f * Mathf.PI * w.Speed / len;
        }

        private static float Speed01(Walker w)
        {
            if (w.Creature != null) return w.Creature.Speed01;
            if (w.Beast != null && w.Beast.Driven) return w.Beast.ExternalSpeed01;
            return Mathf.Clamp01(w.Speed / Mathf.Max(1f, w.RunSpeed));
        }
        #endregion

        #region The steps
        private void Step(Walker w, Vector3 feet)
        {
            if (Time.time >= w.ProbeAgain || (feet - w.ProbedAt).sqrMagnitude > 1.44f)
            {
                w.Ground = FootstepAudio.Probe(feet);
                w.ProbedAt = feet;
                w.ProbeAgain = Time.time + 2.5f;
            }
            FootstepAudio.Ground g = w.Ground;
            bool hard = FootstepAudio.IsHard(g.Surface), water = g.Surface == FootstepAudio.Surface.Water;
            bool left = (w.Half & 1) == 0;
            w.Steps++;
            Vector3 at = feet + Vector3.up * 0.05f;
            Body body = w.Body;
            // A dead one seen over the network carries no type: its pace tells.
            if (body == Body.Shambler && w.Zombie == null && w.Actor != null && w.Actor.Kind == ActorKind.Zombie)
                body = w.Speed > 3.8f ? Body.Sprinter : w.Speed > 1.8f ? Body.Lurcher : Body.Shambler;
            switch (body)
            {
                case Body.Boots: Boots(w, at, g); break;
                case Body.Shambler: Shambler(w, at, g, hard, left, 1f); break;
                case Body.Returned: Shambler(w, at, g, hard, left, 0.45f); break;
                case Body.Lurcher: Lurcher(w, at, g, hard); break;
                case Body.Sprinter: Sprinter(w, at, g, hard); break;
                case Body.Crawler:
                    Emit(SfxIds.StepCrawl, at, 0.42f, Random.Range(0.9f, 1.1f), 16f, hard ? 0f : 2400f);
                    if (water) Emit(SfxIds.StepWater, at, 0.3f, Random.Range(1.1f, 1.25f), 14f);
                    break;
                default: Beast(w, at, g, hard, water); break;
            }
        }

        // Stalkers, companions and other runners: boots on the ground, creeping, walking or running (kit rattling).
        private void Boots(Walker w, Vector3 at, FootstepAudio.Ground g)
        {
            bool sneak = (w.Human != null && w.Human.Crouched) || (w.Anim != null && w.Anim.Sneaking) || w.Speed < 0.9f;
            bool run = !sneak && w.Speed > 3.3f;
            string id = FootstepAudio.IdOf(g);
            float vol = (sneak ? 0.15f : run ? 0.55f : 0.34f) * FootstepAudio.Trim(id);
            Emit(id, at, vol, Random.Range(0.9f, 1.02f), sneak ? 10f : run ? 30f : 22f);
            if (run && Random.value < 0.45f) Emit(SfxIds.StepGear, at + Vector3.up * 1.05f, 0.13f, Random.Range(0.9f, 1.08f), 14f);
        }

        // The slow dead: a scuffing, flat-footed plant on hard floors, a heavy flat crunch in snow or grass; the bad leg of
        // a limper (and any of them now and then) dragged instead. Hushed for the Returned.
        private void Shambler(Walker w, Vector3 at, FootstepAudio.Ground g, bool hard, bool left, float loud)
        {
            bool limp = w.Anim != null && w.Anim.Limp > 0.3f;
            bool drag = (limp && left) || Random.value < 0.18f;
            float range = loud < 1f ? 12f : 22f;
            if (hard)
            {
                if (drag) Emit(SfxIds.StepZombieDrag, at, 0.42f * loud, Random.Range(0.9f, 1.08f), range);
                else Emit(SfxIds.StepZombieShuffle, at, 0.5f * loud, Random.Range(0.88f, 1.06f), range);
                return;
            }
            string id = FootstepAudio.IdOf(g);
            Emit(id, at, 0.38f * loud * FootstepAudio.Trim(id), Random.Range(0.8f, 0.9f), range - 2f);
            if (drag) Emit(SfxIds.StepZombieDrag, at, 0.24f * loud, Random.Range(0.85f, 1f), range - 4f, 1500f);
        }

        // A fast shambler: quicker, heavier, stumbling now and then.
        private void Lurcher(Walker w, Vector3 at, FootstepAudio.Ground g, bool hard)
        {
            bool stumble = Random.value < 0.25f;
            if (hard)
            {
                if (stumble) Emit(SfxIds.StepZombieLurch, at, 0.62f, Random.Range(0.92f, 1.08f), 26f);
                else Emit(SfxIds.StepZombieShuffle, at, 0.56f, Random.Range(0.98f, 1.12f), 26f);
            }
            else
            {
                string id = FootstepAudio.IdOf(g);
                Emit(id, at, 0.5f * FootstepAudio.Trim(id), Random.Range(0.82f, 0.92f), 24f);
                if (stumble) Emit(SfxIds.StepZombieLurch, at, 0.32f, Random.Range(0.9f, 1.05f), 22f, 1200f);
            }
            if (Random.value < 0.1f) Emit(SfxIds.StepZombieDrag, at, 0.3f, Random.Range(1f, 1.15f), 20f, hard ? 0f : 1500f);
        }

        // A sprinter: a body thrown forward, pounding; in snow or grass the ground crunches under the thud.
        private void Sprinter(Walker w, Vector3 at, FootstepAudio.Ground g, bool hard)
        {
            Emit(SfxIds.StepZombieRun, at, 0.78f, Random.Range(0.9f, 1.08f), 36f, hard ? 0f : 1100f);
            if (hard) return;
            string id = FootstepAudio.IdOf(g);
            Emit(id, at, 0.5f * FootstepAudio.Trim(id), Random.Range(0.85f, 0.95f), 30f);
        }

        // The beasts, by species and pace: claws on hard ground, hooves clopping on it, the big ones heard from further.
        private void Beast(Walker w, Vector3 at, FootstepAudio.Ground g, bool hard, bool water)
        {
            float s01 = Speed01(w);
            bool run = s01 > 0.45f;
            switch (w.Body)
            {
                case Body.Wolf:
                case Body.Hound:
                {
                    float pitch = w.Body == Body.Hound ? Random.Range(0.86f, 0.96f) : Random.Range(0.95f, 1.08f);
                    float vol = (run ? 0.55f : 0.32f) * (w.Body == Body.Hound ? 1.1f : 1f);
                    if (water) Emit(SfxIds.StepWater, at, vol * 0.8f, pitch * 1.3f, run ? 26f : 18f);
                    else Emit(hard ? SfxIds.StepPawClaw : SfxIds.StepPaw, at, vol, pitch, run ? 28f : 18f);
                    break;
                }
                case Body.Deer:
                {
                    float vol = run ? 0.65f : 0.38f, range = run ? 34f : 22f, pitch = Random.Range(0.95f, 1.08f);
                    string id = water ? SfxIds.StepWater : hard ? SfxIds.StepHoofHard : SfxIds.StepHoof;
                    Emit(id, at, vol, pitch, range);
                    // At a gallop the hind pair follows the front a beat behind.
                    if (run && !water) Sfx.PlayDelayed(id, at, Random.Range(0.07f, 0.11f), vol * 0.8f, pitch * 1.03f, range);
                    break;
                }
                case Body.Bear:
                {
                    float vol = run ? 0.85f : 0.55f;
                    Emit(water ? SfxIds.StepWater : SfxIds.StepHeavy, at, vol, Random.Range(0.9f, 1.05f) * (water ? 0.7f : 1f), run ? 42f : 30f);
                    if (hard) Emit(SfxIds.StepPawClaw, at, 0.25f, Random.Range(0.6f, 0.7f), 18f);
                    break;
                }
                case Body.Thumper:
                {
                    bool fore = (w.Half & 1) == 0;
                    Emit(SfxIds.StepStomp, at, fore ? 0.95f : 0.7f, Random.Range(0.88f, 1.02f) * (fore ? 1f : 1.08f), 60f);
                    break;
                }
                case Body.Shade:
                    Emit(SfxIds.StepShade, at, run ? 0.32f : 0.24f, Random.Range(0.9f, 1.15f), 10f);
                    break;
                case Body.Puppeteer:
                    // One slow drag a stride (its other foot barely leaves the ground).
                    if ((w.Half & 1) == 0) Emit(SfxIds.StepPuppet, at, 0.42f, Random.Range(0.9f, 1.05f), 20f);
                    break;
            }
        }

        private static void Emit(string id, Vector3 at, float volume, float pitch, float range, float lowPass = 0f)
        {
            AudioSource src = Sfx.Play(id, at, volume, pitch, range);
            if (src != null && lowPass > 0f) Sfx.SetLowPass(src, lowPass);
        }
        #endregion
    }
}
