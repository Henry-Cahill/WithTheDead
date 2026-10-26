using UnityEngine;

namespace CloneZ
{
    /// <summary>Footsteps that sound like what is underfoot: snow, boards, concrete and asphalt, grass and earth.</summary>
    public static class FootstepAudio
    {
        /// <summary>Starts whatever keeps the footsteps of the world going (called once by the audio module).</summary>
        public static void Ensure()
        {
        }

        /// <summary>Hooks the survivor's own steps to their controller.</summary>
        public static void OnPlayerBuilt(PlayerStats p)
        {
            var controller = p.GetComponent<PlayerController>();
            Transform t = p.transform;
            if (controller != null) controller.Footstep += (sprint, sneak) => Play(t.position, sprint, sneak);
        }

        /// <summary>Determines the surface type under the given foot position for appropriate footstep sounds.</summary>
        public static string SurfaceAt(Vector3 feet)
        {
            if (Physics.Raycast(feet + Vector3.up * 0.4f, Vector3.down, out RaycastHit hit, 1.4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                string s = SurfaceName(hit.collider);
                if (s != null)
                {
                    if (Has(s, "snow")) return SfxIds.StepSnow;
                    if (Has(s, "asphalt") || Has(s, "concrete") || Has(s, "road") || Has(s, "pavement") || Has(s, "tile") || Has(s, "gravel") || Has(s, "slab") || Has(s, "stair"))
                        return SfxIds.StepConcrete;
                    if (Has(s, "wood") || Has(s, "plank") || Has(s, "floor") || Has(s, "deck") || Has(s, "porch")) return SfxIds.StepWood;
                }
            }
            if (Shelter.Count > 0 && Shelter.IsSheltered(feet + Vector3.up)) return SfxIds.StepWood;
            if (GameSession.IsHub) return SfxIds.StepWood;
            if (Climate.SnowFraction > 0.5f) return SfxIds.StepSnow;
            return SfxIds.StepGrass;
        }

        public static void Play(Vector3 feet, bool sprinting, bool sneaking, float loudness = 1f)
        {
            float vol = (sneaking ? 0.2f : sprinting ? 0.7f : 0.45f) * loudness;
            float range = sneaking ? 6f : sprinting ? 24f : 14f;
            Sfx.Play(SurfaceAt(feet), feet + Vector3.up * 0.05f, vol, Random.Range(0.9f, 1.1f), range);
        }

        private static string SurfaceName(Collider c)
        {
            var r = c.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                Texture tex = r.sharedMaterial.mainTexture;
                if (tex != null && !string.IsNullOrEmpty(tex.name)) return (tex.name + " " + c.name).ToLowerInvariant();
                return (r.sharedMaterial.name + " " + c.name).ToLowerInvariant();
            }
            return c.name.ToLowerInvariant();
        }

        private static bool Has(string s, string part) => s.IndexOf(part, System.StringComparison.Ordinal) >= 0;
    }
}
