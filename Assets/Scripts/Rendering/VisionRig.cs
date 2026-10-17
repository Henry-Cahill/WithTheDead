using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CloneZ
{
    /// <summary>
    /// Installs everything that decides what the survivor can see and how the world is lit: the
    /// line-of-sight darkness (a shadow-casting sight light at the eye, near-black raid nights), zombies
    /// hidden outside the survivor's view, warm room lights' budget, the night-vision effect, the
    /// flashlight beam, falling snow and breath. The bootstrapper calls <see cref="Install"/> once the
    /// camera and the player exist; the rig lives on a "[VisionRig]" object that is torn down with the
    /// scene. See <see cref="VisionController"/> for the per-frame work and its tunables.
    /// </summary>
    public static class VisionRig
    {
        /// <summary>Name of the rig's root object.</summary>
        public const string RootName = "[VisionRig]";

        /// <summary>
        /// The rendering layer ("Light Layer 7") the survivor's own renderers are moved to while a rig is
        /// installed. The sight light leaves this layer out, so it neither lights nor is shadowed by the
        /// survivor's own body (it shines from inside their head). Every other light that should light the
        /// survivor must include it: the sun, room lights, the flashlight and the peripheral glow do —
        /// add it to any new light with <c>light.GetUniversalAdditionalLightData().renderingLayers |=
        /// VisionRig.SurvivorRenderingLayerMask</c>.
        /// </summary>
        public const int SurvivorRenderingLayer = 7;
        /// <summary>Bit mask of <see cref="SurvivorRenderingLayer"/>.</summary>
        public const uint SurvivorRenderingLayerMask = 1u << SurvivorRenderingLayer;

        private static GameObject s_Root;

        /// <summary>The running controller, or null when no rig is installed.</summary>
        public static VisionController Controller => VisionController.Active;

        /// <summary>True while a rig is installed.</summary>
        public static bool IsInstalled => s_Root != null;

        /// <summary>
        /// Builds the vision rig for <paramref name="camera"/> (the main camera if null) and
        /// <paramref name="player"/>. Idempotent: an existing rig is destroyed first. Also makes sure the
        /// camera renders post-processing (the night-vision look is a volume).
        /// </summary>
        public static void Install(Camera camera, PlayerStats player)
        {
            Uninstall();
            if (camera == null) camera = Camera.main;
            if (camera != null)
            {
                UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }

            s_Root = new GameObject(RootName);
            var controller = s_Root.AddComponent<VisionController>();
            controller.Bind(camera, player);
        }

        /// <summary>Destroys the installed rig (zombies are all shown again, the night returns to normal).</summary>
        public static void Uninstall()
        {
            if (s_Root != null)
            {
                s_Root.SetActive(false);
                Object.Destroy(s_Root);
            }
            s_Root = null;

            // A rig this class lost track of (e.g. statics reset between play sessions).
            GameObject stray = GameObject.Find(RootName);
            while (stray != null)
            {
                stray.SetActive(false);
                Object.Destroy(stray);
                stray = GameObject.Find(RootName);
            }
        }

        /// <summary>True when the survivor can see <paramref name="point"/> right now (always true
        /// without a rig). Same rule as the zombies' visibility.</summary>
        public static bool CanSee(Vector3 point)
        {
            VisionController c = Controller;
            return c == null || c.CanSee(point);
        }
    }
}
