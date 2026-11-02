using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Fits the working windows of the statewide map's furnished floors near the survivors (<see cref="InteriorBuild.FitSomeWindows"/>)
    /// and takes them out again behind them, so a town of hundreds of buildings carries only its nearest windows' working parts:
    /// every window's fixed sash is in its wall, and each storey's stand-in glass fills the openings of a floor whose windows
    /// aren't in. A floor's windows go in, a few a frame, once a survivor comes within <see cref="FitReach"/> of its building,
    /// nearest first, and come out past <see cref="DropReach"/>; what each was left as (opened, latched, drawn, smashed, boarded)
    /// is kept by its save key and put back when it's fitted again. In the editor (the harnesses), where nothing streams, a
    /// floor's windows go in as it's built. One object for the whole map, made on first use.
    /// </summary>
    internal sealed class WindowStreamer : MonoBehaviour
    {
        internal const float FitReach = 24f, DropReach = 32f;
        private const int WindowsPerFrame = 6;
        private const float ScanInterval = 0.4f;

        private static WindowStreamer s_Active;
        private static readonly List<InteriorBuild> s_Floors = new List<InteriorBuild>(64);
        private static readonly Dictionary<string, FixtureSave> s_Kept = new Dictionary<string, FixtureSave>(256);

        private readonly List<Vector3> _focus = new List<Vector3>(4);
        private readonly List<InteriorBuild> _wanted = new List<InteriorBuild>(16);
        private readonly Dictionary<InteriorBuild, float> _distance = new Dictionary<InteriorBuild, float>(64);
        private float _nextScan;

        /// <summary>True when a floor's windows go in as it's built: in the editor's harnesses, where nothing streams.</summary>
        internal static bool FitAtOnce => !Application.isPlaying;

        /// <summary>Floors waiting for their windows or holding them, and windows kept while out (for the harness).</summary>
        internal static int Floors => s_Floors.Count;
        internal static int Kept => s_Kept.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_Active = null;
            s_Floors.Clear();
            s_Kept.Clear();
        }

        /// <summary>A furnished floor whose windows go in when a survivor comes near.</summary>
        internal static void Add(InteriorBuild floor)
        {
            if (floor == null) return;
            if (s_Active == null) s_Active = new GameObject("WindowStreamer").AddComponent<WindowStreamer>();
            if (!s_Floors.Contains(floor)) s_Floors.Add(floor);
        }

        /// <summary>A floor taken down (its windows taken out first, <see cref="InteriorBuild.DropWindows"/>).</summary>
        internal static void Remove(InteriorBuild floor)
        {
            s_Floors.Remove(floor);
            if (s_Active != null) s_Active._wanted.Remove(floor);
        }

        /// <summary>What a window taken out was left as, kept by its save key.</summary>
        internal static void Keep(Interactable window)
        {
            string key = window != null ? window.SaveKey : null;
            if (key == null) return;
            var save = new FixtureSave { Key = key };
            window.CaptureState(save);
            s_Kept[key] = save;
        }

        /// <summary>A window fitted again put back as it was left, if it was ever taken out.</summary>
        internal static void Restore(Interactable window)
        {
            string key = window != null ? window.SaveKey : null;
            if (key != null && s_Kept.TryGetValue(key, out FixtureSave save)) window.RestoreState(save);
        }

        private void Update()
        {
            GatherFocus();
            if (_focus.Count == 0) return;
            float now = Time.unscaledTime;
            if (now >= _nextScan)
            {
                _nextScan = now + ScanInterval;
                Scan();
            }
            int budget = WindowsPerFrame;
            for (int i = 0; i < _wanted.Count && budget > 0; i++)
            {
                InteriorBuild floor = _wanted[i];
                if (floor != null && floor.Root != null) budget -= Mathf.Max(1, floor.FitSomeWindows(budget));
            }
            for (int i = _wanted.Count - 1; i >= 0; i--)
                if (_wanted[i] == null || _wanted[i].Root == null || _wanted[i].WindowsFitted) _wanted.RemoveAt(i);
        }

        // The floors a survivor has come near enough to want their windows (nearest first), and those left far behind them
        // taken out.
        private void Scan()
        {
            _distance.Clear();
            for (int i = s_Floors.Count - 1; i >= 0; i--)
            {
                InteriorBuild floor = s_Floors[i];
                if (floor == null || floor.Root == null)
                {
                    s_Floors.RemoveAt(i);
                    continue;
                }
                float d = Distance(floor);
                _distance[floor] = d;
                if (d <= FitReach)
                {
                    if (!floor.WindowsFitted && !_wanted.Contains(floor)) _wanted.Add(floor);
                }
                else if (d >= DropReach && floor.WindowsStarted)
                {
                    _wanted.Remove(floor);
                    floor.DropWindows();
                }
            }
            _wanted.Sort((a, b) => Near(a).CompareTo(Near(b)));
        }

        private float Near(InteriorBuild floor) => floor != null && _distance.TryGetValue(floor, out float d) ? d : float.MaxValue;

        // How far (flat) the nearest survivor is from the floor's building.
        private float Distance(InteriorBuild floor)
        {
            Bounds b = floor.WorldBounds;
            float best = float.MaxValue;
            for (int i = 0; i < _focus.Count; i++)
            {
                Vector3 p = _focus[i];
                float dx = Mathf.Max(0f, Mathf.Max(b.min.x - p.x, p.x - b.max.x));
                float dz = Mathf.Max(0f, Mathf.Max(b.min.z - p.z, p.z - b.max.z));
                best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dz * dz));
            }
            return best;
        }

        private void GatherFocus()
        {
            _focus.Clear();
            Actor local = Actor.LocalPlayer;
            if (local != null) _focus.Add(local.transform.position);
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a != null && a != local && a.IsPlayerControlled) _focus.Add(a.transform.position);
            }
        }

        private void OnDestroy()
        {
            if (s_Active == this) s_Active = null;
        }
    }
}
