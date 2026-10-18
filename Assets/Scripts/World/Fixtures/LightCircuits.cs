using System.Collections.Generic;

namespace CloneZ
{
    /// <summary>
    /// The starter home's lighting circuits — the living room's, the porch's, the store room's and the bathroom's —
    /// each worked by a toggle on a wall switch by a door (<see cref="LightSwitch"/>) and feeding the wall lights
    /// wired to it (<see cref="HomeLamp"/>). A circuit remembers whether it's switched on even with the power out,
    /// so its lights come back with the power. A fresh home has everything off but the porch light, left on for
    /// the lantern's dusk sensor. The states belong to the world being played: they start afresh with each new one
    /// (a new <see cref="PowerGrid"/>), and the switches save and restore them. Any other building wires up circuits
    /// of its own as it's built (<see cref="Define"/>) — the raid town has one for every room and every porch.
    /// </summary>
    internal static class LightCircuits
    {
        public const string Living = "living", Porch = "porch", Store = "store", Bath = "bath";

        private static readonly Dictionary<string, bool> s_On = new Dictionary<string, bool>(8);
        private static readonly Dictionary<string, Wiring> s_Wired = new Dictionary<string, Wiring>(64);
        private static object s_World;

        // A circuit wired up at run time: its light as prompts name it, and whether a fresh world finds it switched on.
        private readonly struct Wiring
        {
            public readonly string Noun;
            public readonly bool DefaultOn;

            public Wiring(string noun, bool defaultOn)
            {
                Noun = noun;
                DefaultOn = defaultOn;
            }
        }

        /// <summary>True while <paramref name="circuit"/> is switched on (whether or not there's power to light it).</summary>
        public static bool IsOn(string circuit)
        {
            if (string.IsNullOrEmpty(circuit)) return false;
            Sync();
            return s_On.TryGetValue(circuit, out bool on) ? on : DefaultOn(circuit);
        }

        /// <summary>Switches <paramref name="circuit"/> on or off; its lights follow at once.</summary>
        public static void Set(string circuit, bool on)
        {
            if (string.IsNullOrEmpty(circuit)) return;
            Sync();
            if (s_On.TryGetValue(circuit, out bool was) && was == on) return;
            s_On[circuit] = on;
            HomeLamp.RefreshAll();
        }

        /// <summary>Wires up a circuit beyond the home's four — a raid building's room lights, its porch lantern:
        /// <paramref name="noun"/> names its light in prompts ("kitchen light") and <paramref name="defaultOn"/> is how
        /// its switch is found. Defining a circuit again wires it afresh, its switch back where it started. Definitions
        /// outlast the reset a new world brings (a town built a moment before its power grid keeps its circuits), and the
        /// next build simply defines its own again.</summary>
        public static void Define(string circuit, string noun, bool defaultOn)
        {
            if (string.IsNullOrEmpty(circuit)) return;
            Sync();
            bool was = IsOn(circuit);
            s_Wired[circuit] = new Wiring(string.IsNullOrEmpty(noun) ? "light" : noun, defaultOn);
            s_On.Remove(circuit);
            if (was != defaultOn) HomeLamp.RefreshAll();
        }

        /// <summary>True once <paramref name="circuit"/> has been wired up with <see cref="Define"/>.</summary>
        public static bool IsDefined(string circuit) => !string.IsNullOrEmpty(circuit) && s_Wired.ContainsKey(circuit);

        /// <summary>The circuit's light for prompts: "living room light".</summary>
        public static string Noun(string circuit)
        {
            if (circuit != null && s_Wired.TryGetValue(circuit, out Wiring w)) return w.Noun;
            return circuit switch
            {
                Living => "living room light",
                Porch => "porch light",
                Store => "store room light",
                Bath => "bathroom light",
                _ => "light",
            };
        }

        private static bool DefaultOn(string circuit) => s_Wired.TryGetValue(circuit, out Wiring w) ? w.DefaultOn : circuit == Porch;

        // A new world (a new power grid) starts from the defaults.
        private static void Sync()
        {
            PowerGrid grid = PowerGrid.Instance;
            if (ReferenceEquals(s_World, grid)) return;
            s_World = grid;
            s_On.Clear();
        }
    }
}
