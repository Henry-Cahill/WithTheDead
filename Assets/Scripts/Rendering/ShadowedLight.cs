using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CloneZ
{
    /// <summary>
    /// Puts a light's shadows under the <see cref="ShadowBudget"/>: the light casts them only while the budget grants them,
    /// so no light switches its own shadows on. Made with <see cref="Add"/> once the light is set up; the shadow type the
    /// light has then (soft when it has none) is the one it casts when granted. Its owner says whether it wants them right
    /// now (<see cref="Wants"/>). A <see cref="Pinned"/> light, one of the survivor's own (the sight cone, the flashlight),
    /// is granted first and keeps its place in the shadow atlas even while it's off. In edit mode it does nothing: the
    /// light keeps whatever shadows it's given.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShadowedLight : MonoBehaviour
    {
        private bool _wants = true;
        private UniversalAdditionalLightData _data;

        /// <summary>The light whose shadows the budget hands out (on this object).</summary>
        public Light Light { get; private set; }
        /// <summary>The shadows it casts when granted.</summary>
        public LightShadows Shadows { get; private set; } = LightShadows.Soft;
        /// <summary>True for the survivor's own lights: granted first, their atlas space kept while they're off, and their
        /// shadow resolution left as their owner set it.</summary>
        public bool Pinned { get; private set; }
        /// <summary>True (the default) when a switched-off light gives its shadows up; false keeps them through the
        /// blackouts of a flickering fitting, whose light only blinks off (see <see cref="RoomLight"/>).</summary>
        public bool OnlyWhileEnabled { get; set; } = true;
        /// <summary>True while the budget lets the light cast shadows.</summary>
        public bool Granted { get; private set; }
        /// <summary>Its place in the budget's list (-1 while it isn't in it).</summary>
        internal int Index { get; set; } = -1;

        /// <summary>The owner's say: false gives the shadows up (the lamp is switched off, out of reach, not its turn).</summary>
        public bool Wants
        {
            get => _wants;
            set
            {
                if (_wants == value) return;
                _wants = value;
                ShadowBudget.Refresh();
            }
        }

        /// <summary>Puts <paramref name="light"/>'s shadows under the budget (<paramref name="pinned"/> for the survivor's
        /// own lights). Set the light up first: its shadow type is the one it casts when granted.</summary>
        public static ShadowedLight Add(Light light, bool pinned = false)
        {
            if (light == null) return null;
            if (!light.TryGetComponent(out ShadowedLight s)) s = light.gameObject.AddComponent<ShadowedLight>();
            // Taken over here, not just in Awake: that waits for an inactive object to wake, and never runs in edit mode.
            s.TakeLight();
            s.Pinned = pinned;
            ShadowBudget.Refresh();
            return s;
        }

        private void Awake() => TakeLight();

        private void OnEnable()
        {
            TakeLight();
            ShadowBudget.Add(this);
        }

        private void OnDisable()
        {
            ShadowBudget.Remove(this);
            Apply(false, LightShadows.None, -1);
        }

        // The light's own shadow setting becomes the one it's granted; until then it casts none.
        private void TakeLight()
        {
            if (Light != null) return;
            Light = GetComponent<Light>();
            if (Light == null) return;
            if (Light.shadows != LightShadows.None) Shadows = Light.shadows;
            Light.shadows = LightShadows.None;
        }

        /// <summary>The light's shadow resolution tier (a <see cref="UniversalAdditionalLightData"/> tier).</summary>
        internal int Tier
        {
            get
            {
                if (_data == null && Light != null) Light.TryGetComponent(out _data);
                return _data != null ? _data.additionalLightsShadowResolutionTier : UniversalAdditionalLightData.AdditionalLightsShadowDefaultResolutionTier;
            }
        }

        /// <summary>The budget's ruling: <paramref name="shadows"/> when <paramref name="granted"/>, none otherwise, at
        /// resolution <paramref name="tier"/> (-1 leaves it).</summary>
        internal void Apply(bool granted, LightShadows shadows, int tier)
        {
            Granted = granted;
            if (Light == null) return;
            LightShadows want = granted ? shadows : LightShadows.None;
            if (Light.shadows != want) Light.shadows = want;
            if (tier < 0 || !Application.isPlaying) return;
            if (_data == null) _data = Light.GetUniversalAdditionalLightData();
            if (_data.additionalLightsShadowResolutionTier != tier) _data.additionalLightsShadowResolutionTier = tier;
        }
    }
}
