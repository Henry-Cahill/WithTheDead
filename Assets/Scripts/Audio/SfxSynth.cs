using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Every sound in the game, synthesised in code (there are no audio assets). The recipes live by family in
    /// Assets/Scripts/Audio/Synth/Recipes, one partial file each (weapons, footsteps, foley, creatures, zone, ambience,
    /// music). A recipe renders one variation from a seeded generator — the same id and variation always come out
    /// sample-for-sample identical, in the game and in the offline tools — and carries the routing the game plays it
    /// with (<see cref="SfxMeta"/>). Everything is rendered ahead of time on background threads (<see cref="Prewarm"/>)
    /// and becomes a clip on the main thread; a sound asked for before it is ready is rendered on the spot. Frequent
    /// sounds get a few variations, loops are cross-faded so they wrap without a click, and beds may be stereo.
    /// </summary>
    public sealed partial class SfxSynth : ISfxBackend
    {
        /// <summary>Full bandwidth, for anything with a sharp attack or air: gunshots, clicks, glass, footsteps.</summary>
        internal const int HiRate = 44100;
        /// <summary>For long beds and loops whose content tops out below 16 kHz (wind, rain, rumble, drones): half the memory.</summary>
        internal const int BedRate = 32000;
        private const int MaxVariations = 16;

        /// <summary>One sound: how to render it and how the game plays it. The chainable setters fill in the routing.</summary>
        internal sealed class Recipe
        {
            public readonly string Id;
            public readonly int SampleRate, Variations, Index;
            public string Family;
            public Func<SynthGen, float[]> Make;
            public Func<SynthGen, StereoBuf> MakeStereo;
            public bool IsLoop;
            public float PeakLevel = 0.8f;
            public int WarmOrder = 100;
            public readonly SfxMeta Meta = new SfxMeta();

            internal readonly AudioClip[] Clips;
            internal readonly Rendered[] Pending;
            internal readonly int[] Done, Queued, QueuedUrgent;

            public Recipe(string id, int rate, int variations, int index)
            {
                Id = id;
                SampleRate = rate;
                Variations = variations;
                Index = index;
                Clips = new AudioClip[variations];
                Pending = new Rendered[variations];
                Done = new int[variations];
                Queued = new int[variations];
                QueuedUrgent = new int[variations];
            }

            /// <summary>Loops seamlessly (no fades at the ends; build it with Loop/LoopStereo).</summary>
            public Recipe Loop() { IsLoop = true; return this; }
            /// <summary>The normalised peak, 0..1 (callers' volumes are tuned against it).</summary>
            public Recipe Peak(float level) { PeakLevel = level; return this; }
            public Recipe Bus(SfxBus bus) { Meta.Bus = bus; return this; }
            /// <summary>Unity voice priority: 0 never culled .. 256 first to go.</summary>
            public Recipe Priority(int priority) { Meta.Priority = priority; return this; }
            /// <summary>Audible to at least <paramref name="metres"/>, whatever range the caller passes.</summary>
            public Recipe Range(float metres) { Meta.MinRange = metres; return this; }
            /// <summary>Beyond <paramref name="metres"/> from the listener, <paramref name="farId"/> plays instead.</summary>
            public Recipe Far(string farId, float metres) { Meta.Far = farId; Meta.FarDistance = metres; return this; }
            /// <summary>A follow-up sound <paramref name="delay"/> (± <paramref name="jitter"/>) seconds later at the same spot.</summary>
            public Recipe Then(string id, float delay, float jitter = 0.1f, float volume = 0.5f)
            {
                Meta.Then = id;
                Meta.ThenDelay = delay;
                Meta.ThenJitter = jitter;
                Meta.ThenVolume = volume;
                return this;
            }
            /// <summary>Ducks ambience and music by <paramref name="db"/> (negative) when played within <paramref name="range"/> of the listener.</summary>
            public Recipe Duck(float db, float range = 40f, float hold = 0.6f, float release = 2.5f)
            {
                Meta.DuckDb = db;
                Meta.DuckRange = range;
                Meta.DuckHold = hold;
                Meta.DuckRelease = release;
                return this;
            }
            /// <summary>Within <paramref name="range"/> of the listener the blast deafens (muffled mix, ringing ears).</summary>
            public Recipe Concuss(float range) { Meta.ConcussRange = range; return this; }
            /// <summary>How much room reverb the sound picks up, 0..1.</summary>
            public Recipe Reverb(float mix) { Meta.Reverb = mix; return this; }
            /// <summary>Carries its own space: no room reverb.</summary>
            public Recipe Dry() { Meta.Reverb = 0f; return this; }
            /// <summary>Inside the survivor's head or on their body: never muffled by walls or distance.</summary>
            public Recipe Inside() { Meta.NoOcclusion = true; return this; }
            public Recipe Doppler(float level) { Meta.Doppler = level; return this; }
            /// <summary>3D spread in degrees, for wide sources.</summary>
            public Recipe Spread(float degrees) { Meta.Spread = degrees; return this; }
            /// <summary>Background build order: lower is built sooner (default 100).</summary>
            public Recipe Warm(int order) { WarmOrder = order; return this; }
        }

        /// <summary>A finished render: interleaved samples.</summary>
        internal sealed class Rendered
        {
            public float[] Data;
            public int Channels, Rate;
        }

        private readonly Dictionary<string, Recipe> _recipes = new Dictionary<string, Recipe>();
        private readonly List<Recipe> _order = new List<Recipe>();
        private readonly System.Random _pick = new System.Random();
        private string _family = "";
        private bool _prewarmed;

        public SfxSynth()
        {
            _family = "Weapons";
            RegisterWeapons();
            _family = "Footsteps";
            RegisterFootsteps();
            _family = "Foley";
            RegisterFoley();
            _family = "Creatures";
            RegisterCreatures();
            _family = "Zone";
            RegisterZone();
            _family = "Ambience";
            RegisterAmbience();
            _family = "Music";
            RegisterMusic();
            _family = "";
        }

        // One per family file in Synth/Recipes (and Synth/Music); a family with no file simply registers nothing.
        partial void RegisterWeapons();
        partial void RegisterFootsteps();
        partial void RegisterFoley();
        partial void RegisterCreatures();
        partial void RegisterZone();
        partial void RegisterAmbience();
        partial void RegisterMusic();

        #region Queries
        /// <summary>True when <paramref name="id"/> has a registered recipe.</summary>
        public bool Has(string id) => id != null && _recipes.ContainsKey(id);

        /// <summary>Every registered id, in registration order.</summary>
        public IEnumerable<string> Ids
        {
            get { foreach (Recipe r in _order) yield return r.Id; }
        }

        /// <summary>How many variations <paramref name="id"/> has, or zero if unknown.</summary>
        public int VariationsOf(string id) => id != null && _recipes.TryGetValue(id, out Recipe r) ? r.Variations : 0;
        /// <summary>True when <paramref name="id"/> is a seamless loop.</summary>
        public bool IsLoop(string id) => id != null && _recipes.TryGetValue(id, out Recipe r) && r.IsLoop;
        /// <summary>The recipe family (file) that registered <paramref name="id"/>: Weapons, Footsteps, Foley, ...</summary>
        public string FamilyOf(string id) => id != null && _recipes.TryGetValue(id, out Recipe r) ? r.Family : null;

        /// <summary>The routing metadata for <paramref name="id"/>; never null.</summary>
        public SfxMeta Meta(string id) => id != null && _recipes.TryGetValue(id, out Recipe r) ? r.Meta : SfxMeta.Default;

        /// <summary>True when any variation of <paramref name="id"/> is already built or pending.</summary>
        public bool IsReady(string id)
        {
            if (id == null || !_recipes.TryGetValue(id, out Recipe r)) return false;
            for (int v = 0; v < r.Variations; v++)
                if (r.Clips[v] != null || Volatile.Read(ref r.Pending[v]) != null) return true;
            return false;
        }

        /// <summary>Renders one variation without touching the cache (tools): interleaved samples, normalised and finished.</summary>
        public float[] RenderSamples(string id, int variation, out int channels, out int rate)
        {
            channels = 1;
            rate = HiRate;
            if (id == null || !_recipes.TryGetValue(id, out Recipe r)) return null;
            Rendered d = Render(r, Math.Max(0, Math.Min(r.Variations - 1, variation)));
            channels = d.Channels;
            rate = d.Rate;
            return d.Data;
        }
        #endregion

        #region Clips
        /// <summary>A clip for <paramref name="id"/>, picking a random variation; renders on-the-spot if nothing is ready and another variation stands in if one exists.</summary>
        public AudioClip Get(string id)
        {
            if (id == null || !_recipes.TryGetValue(id, out Recipe r)) return null;
            int n = r.Variations;
            int v = n > 1 ? _pick.Next(n) : 0;
            AudioClip c = Built(r, v);
            if (c != null) return c;
            // Another variation already built beats stalling for this one.
            for (int k = 1; k < n; k++)
            {
                AudioClip other = Built(r, (v + k) % n);
                if (other == null) continue;
                Queue(r, v, true);
                return other;
            }
            Interlocked.Exchange(ref r.Done[v], 1);
            c = r.Clips[v] = MakeClip(r, v, Render(r, v));
            return c;
        }

        /// <summary>The given variation of <paramref name="id"/> (wrapped into range); another variation stands in while it is still being built or renders on-the-spot if needed.</summary>
        public AudioClip Get(string id, int variation)
        {
            if (id == null || !_recipes.TryGetValue(id, out Recipe r)) return null;
            int n = r.Variations;
            int v = ((variation % n) + n) % n;
            AudioClip c = Built(r, v);
            if (c != null) return c;
            Queue(r, v, true);
            for (int k = 1; k < n; k++)
            {
                AudioClip other = Built(r, (v + k) % n);
                if (other != null) return other;
            }
            Interlocked.Exchange(ref r.Done[v], 1);
            c = r.Clips[v] = MakeClip(r, v, Render(r, v));
            return c;
        }

        /// <summary>Asks for every variation of <paramref name="id"/> to be built in the background ahead of the queue.</summary>
        public void Request(string id)
        {
            if (id == null || !_recipes.TryGetValue(id, out Recipe r)) return;
            for (int v = 0; v < r.Variations; v++) Queue(r, v, true);
        }

        /// <summary>
        /// Forgets clips lost with the last play session, so they are built again: in the editor (no domain reload) the
        /// synthesiser outlives a play session but the clips it made are destroyed when play mode ends.
        /// </summary>
        public void ResetRuntime()
        {
            foreach (Recipe r in _order)
            {
                for (int v = 0; v < r.Variations; v++)
                {
                    if (r.Clips[v] != null) continue;
                    r.Clips[v] = null;
                    Volatile.Write(ref r.Pending[v], null);
                    Interlocked.Exchange(ref r.Done[v], 0);
                    Interlocked.Exchange(ref r.Queued[v], 0);
                    Interlocked.Exchange(ref r.QueuedUrgent[v], 0);
                }
            }
            _prewarmed = false;
        }

        /// <summary>Queues every variation of every sound for background rendering: the interface first, then the beds,
        /// then everything else in registration order (a recipe's <see cref="Recipe.Warm"/> order comes first). Stereo
        /// beds are the exception: they are most of the memory and the render time and a session hears only some of them
        /// (the bar, the rain, winter), so each is built when its director first asks for it (Sfx.Ready/Request).</summary>
        public void Prewarm()
        {
            if (_prewarmed) return;
            _prewarmed = true;
            var list = new List<Recipe>(_order);
            list.RemoveAll(r => r.IsLoop && r.MakeStereo != null);
            list.Sort((a, b) =>
            {
                int c = a.WarmOrder.CompareTo(b.WarmOrder);
                if (c != 0) return c;
                c = BusRank(a.Meta.Bus).CompareTo(BusRank(b.Meta.Bus));
                return c != 0 ? c : a.Index.CompareTo(b.Index);
            });
            foreach (Recipe r in list)
                for (int v = 0; v < r.Variations; v++) Queue(r, v, false);
        }

        private static int BusRank(SfxBus bus) => bus == SfxBus.Interface ? 0 : bus == SfxBus.Ambience ? 1 : bus == SfxBus.Effects ? 2 : 3;

        // The clip for a variation, installing a finished background render if one is waiting (a render that lost the
        // race to an on-the-spot one is dropped here).
        private static AudioClip Built(Recipe r, int v)
        {
            Rendered d = Interlocked.Exchange(ref r.Pending[v], null);
            if (r.Clips[v] != null) return r.Clips[v];
            if (d == null) return null;
            r.Clips[v] = MakeClip(r, v, d);
            return r.Clips[v];
        }

        private static void Queue(Recipe r, int v, bool urgent)
        {
            int[] flags = urgent ? r.QueuedUrgent : r.Queued;
            if (Volatile.Read(ref r.Done[v]) != 0 || Interlocked.CompareExchange(ref flags[v], 1, 0) != 0) return;
            AudioJobs.Run(() =>
            {
                if (Interlocked.CompareExchange(ref r.Done[v], 1, 0) != 0) return false;
                Volatile.Write(ref r.Pending[v], Render(r, v));
                return true;
            }, made => { if (made) Built(r, v); }, urgent);
        }

        private static AudioClip MakeClip(Recipe r, int v, Rendered d)
        {
            int frames = Math.Max(1, Math.Min(d.Data.Length / d.Channels, 20_000_000));
            var clip = AudioClip.Create(r.Id + "_" + v, frames, d.Channels, d.Rate, false);
            clip.SetData(d.Data, 0);
            return clip;
        }
        #endregion

        #region Rendering
        // Thread-safe: recipes use only their own generator and pure DSP.
        internal static Rendered Render(Recipe r, int variation)
        {
            var g = new SynthGen(r.SampleRate, Seed(r.Id), variation);
            float[] left = null, right = null;
            try
            {
                if (r.MakeStereo != null)
                {
                    StereoBuf s = r.MakeStereo(g);
                    left = s?.L;
                    right = s?.R;
                }
                else left = r.Make(g);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Sound '" + r.Id + "' failed: " + e.Message);
                left = null;
            }
            if (left == null || left.Length == 0) { left = new float[64]; right = null; }
            if (right != null && right.Length != left.Length) Array.Resize(ref right, left.Length);
            Finish(left, right, r);
            if (right == null) return new Rendered { Data = left, Channels = 1, Rate = r.SampleRate };
            var inter = new float[left.Length * 2];
            for (int i = 0; i < left.Length; i++)
            {
                inter[2 * i] = left[i];
                inter[2 * i + 1] = right[i];
            }
            return new Rendered { Data = inter, Channels = 2, Rate = r.SampleRate };
        }

        // Sanitise, centre, normalise (both sides together) and, for one-shots, fade the ends so nothing clicks.
        private static void Finish(float[] left, float[] right, Recipe r)
        {
            Dsp.Sanitize(left);
            Dsp.RemoveMean(left);
            float peak = Dsp.Peak(left);
            if (right != null)
            {
                Dsp.Sanitize(right);
                Dsp.RemoveMean(right);
                peak = Math.Max(peak, Dsp.Peak(right));
            }
            if (peak > 1e-9f)
            {
                float k = r.PeakLevel / peak;
                Dsp.Gain(left, k);
                if (right != null) Dsp.Gain(right, k);
            }
            if (r.IsLoop) return;
            int fadeIn = Math.Min(r.SampleRate / 689, left.Length / 4), fadeOut = Math.Min(r.SampleRate / 50, left.Length / 3);
            Dsp.FadeIn(left, fadeIn);
            Dsp.FadeOut(left, fadeOut);
            if (right == null) return;
            Dsp.FadeIn(right, fadeIn);
            Dsp.FadeOut(right, fadeOut);
        }

        // A stable seed per id (FNV-1a): string.GetHashCode differs between runtimes and runs.
        private static uint Seed(string id)
        {
            uint h = 2166136261u;
            for (int i = 0; i < id.Length; i++)
            {
                h ^= id[i];
                h *= 16777619u;
            }
            return (h & 0x7fffffffu) + 17u;
        }
        #endregion

        #region Registration
        /// <summary>A mono recipe at <paramref name="rate"/> (use <c>g.Rate</c> inside it); chain the routing onto the result.</summary>
        private Recipe Add(string id, Func<SynthGen, float[]> make, int variations = 1, int rate = HiRate)
        {
            Recipe r = Put(id, rate, variations);
            r.Make = make;
            return r;
        }

        /// <summary>A stereo recipe (beds and music; positional sounds should stay mono).</summary>
        private Recipe AddStereo(string id, Func<SynthGen, StereoBuf> make, int variations = 1, int rate = BedRate)
        {
            Recipe r = Put(id, rate, variations);
            r.MakeStereo = make;
            return r;
        }

        private Recipe Put(string id, int rate, int variations)
        {
            var r = new Recipe(id, rate, Math.Max(1, Math.Min(MaxVariations, variations)), _order.Count) { Family = _family };
            if (id.StartsWith("amb_", StringComparison.Ordinal)) { r.Meta.Bus = SfxBus.Ambience; r.Meta.Priority = 32; }
            else if (id.StartsWith("ui_", StringComparison.Ordinal)) { r.Meta.Bus = SfxBus.Interface; r.Meta.Priority = 16; r.Meta.NoOcclusion = true; r.Meta.Reverb = 0f; }
            else if (id.StartsWith("mus_", StringComparison.Ordinal)) { r.Meta.Bus = SfxBus.Music; r.Meta.Priority = 0; r.Meta.NoOcclusion = true; r.Meta.Reverb = 0f; }
            if (_recipes.TryGetValue(id, out Recipe old))
            {
                Debug.LogWarning("[CloneZ] Sound '" + id + "' is registered twice; the later recipe wins.");
                _order.Remove(old);
            }
            _recipes[id] = r;
            _order.Add(r);
            return r;
        }

        // A loop: render a little longer than wanted, then cross-fade the tail into the head.
        private static float[] Loop(SynthGen g, float seconds, Action<float[]> fill)
        {
            int len = g.N(seconds);
            var x = new float[len + g.N(0.3f)];
            fill(x);
            return Dsp.CrossfadeLoop(x, len);
        }

        /// <summary>A stereo loop: rendered <paramref name="overhang"/> seconds long, then each side cross-faded into its head.</summary>
        private static StereoBuf LoopStereo(SynthGen g, float seconds, Action<StereoBuf> fill, float overhang = 0.3f)
        {
            int len = g.N(seconds);
            var s = new StereoBuf(len + g.N(overhang));
            fill(s);
            return new StereoBuf(Dsp.CrossfadeLoop(s.L, len), Dsp.CrossfadeLoop(s.R, len));
        }
        #endregion
    }
}
