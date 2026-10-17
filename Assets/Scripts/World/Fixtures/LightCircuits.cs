using System.Collections.Generic;

namespace CloneZ
{
    /// <summary>
    /// The starter home's lighting circuits — the living room's, the porch's, the store room's and the bathroom's —
    /// each worked by a toggle on a wall switch by a door (<see cref="LightSwitch"/>) and feeding the wall lights
    /// wired to it (<see cref="HomeLamp"/>). A circuit remembers whether it's switched on even with the power out,
    /// so its lights come back with the power. A fresh home has everything off but the porch light, left on for
    /// the lantern's dusk sensor. The states belong to the world being played: they start afresh with each new one
    /// (a new <see cref="PowerGrid"/>), and the switches save and restore them.
    /// </summary>
    internal static class LightCircuits
    {
        public const string Living = "living", Porch = "porch", Store = "store", Bath = "bath";

        private static readonly Dictionary<string, bool> s_On = new Dictionary<string, bool>(8);
        private static object s_World;

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

        /// <summary>The circuit's light for prompts: "living room light".</summary>
        public static string Noun(string circuit) => circuit switch
        {
            Living => "living room light",
            Porch => "porch light",
            Store => "store room light",
            Bath => "bathroom light",
            _ => "light",
        };

        private static bool DefaultOn(string circuit) => circuit == Porch;

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
