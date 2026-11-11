using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The render thread's command ring (CommandRingWatch). A batch editor renders without a render thread, so without the
    // ring, unless it's started with -force-gfx-mt. Having no Game view, it draws no camera to the screen either. With
    // -clonez-smoke-view <w>x<h> (Smoke.ps1 -Threaded), the screen's cameras are drawn into a texture that size every
    // frame, as the Game view would draw them, so their commands go through the ring as they do in the editor. The run
    // fails if the main thread ever had to wait for room in it.
    public sealed partial class SmokeTest
    {
        private static int s_ViewWidth, s_ViewHeight;
        private static RenderTexture s_View;
        private static readonly List<Camera> s_ScreenCameras = new List<Camera>();

        private static void ReadView(string[] args)
        {
            s_ViewWidth = s_ViewHeight = 0;
            string[] size = (ArgAfter(args, "-clonez-smoke-view") ?? "").Split('x');
            if (size.Length == 2 && int.TryParse(size[0], out int w) && int.TryParse(size[1], out int h) && w > 0 && h > 0)
            {
                s_ViewWidth = w;
                s_ViewHeight = h;
                HookView(true);
            }
        }

        // Where the Game view draws: after the frame's FinishFrameRendering, so after the GPU Resident Drawer has caught up
        // with the frame (just before it), as it must; drawn any earlier, it draws meshes destroyed since ("A BatchDrawCommand
        // was submitted with an invalid ...").
        private struct DrawView { }

        private static void HookView(bool on)
        {
            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();
            for (int i = 0; i < root.subSystemList.Length; i++)
            {
                if (root.subSystemList[i].type != typeof(PostLateUpdate)) continue;
                var steps = new List<PlayerLoopSystem>(root.subSystemList[i].subSystemList);
                steps.RemoveAll(s => s.type == typeof(DrawView));
                int at = steps.FindIndex(s => s.type == typeof(PostLateUpdate.FinishFrameRendering));
                if (on) steps.Insert(at + 1, new PlayerLoopSystem { type = typeof(DrawView), updateDelegate = RenderView });
                root.subSystemList[i].subSystemList = steps.ToArray();
                PlayerLoop.SetPlayerLoop(root);
                return;
            }
        }

        private static void RenderView()
        {
            if (s_ViewWidth == 0 || !Application.isPlaying) return;
            if (s_View == null) s_View = new RenderTexture(s_ViewWidth, s_ViewHeight, 24) { name = "Smoke Game view" };
            s_ScreenCameras.Clear();
            foreach (Camera c in Camera.allCameras)
                if (c.targetTexture == null && c.targetDisplay == 0) s_ScreenCameras.Add(c);
            s_ScreenCameras.Sort((a, b) => a.depth.CompareTo(b.depth));
            foreach (Camera c in s_ScreenCameras)
            {
                c.targetTexture = s_View;
                try { c.Render(); }
                finally { c.targetTexture = null; }
            }
        }

        private static void ReleaseView()
        {
            if (s_ViewWidth > 0) HookView(false);
            s_ViewWidth = s_ViewHeight = 0;
            if (s_View != null)
            {
                s_View.Release();
                Destroy(s_View);
            }
            s_View = null;
        }

        private void CheckRing()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            const string name = "render command ring never full";
            if (!CommandRingWatch.Watching)
            {
                Skip(name, "rendering is " + SystemInfo.renderingThreadingMode + ", with no ring to fill (Smoke.ps1 -Threaded gives it one)");
                return;
            }
            string drawn = s_ViewWidth > 0 ? ", the screen drawn at " + s_ViewWidth + "x" + s_ViewHeight
                : Application.isBatchMode ? ", though nothing drew the screen (no -clonez-smoke-view)" : "";
            Check(name, CommandRingWatch.Waits == 0, CommandRingWatch.Waits + " waits, " + CommandRingWatch.WaitMs.ToString("0") + " ms" + drawn
                + (CommandRingWatch.Waits > 0 ? "; the longest, " + CommandRingWatch.WorstFrame : ""));
        }
    }
}
