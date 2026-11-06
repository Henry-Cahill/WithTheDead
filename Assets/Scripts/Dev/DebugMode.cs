using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CloneZ
{
    /// <summary>
    /// Debug Mode (F8; in the editor and development builds only): point at anything on screen and it says what it is.
    /// Every renderer the camera draws, and the ground items (drawn instanced, with no objects of their own), is drawn
    /// again flat in a colour that names it, into a small patch round the cursor, along with each pixel's world
    /// position. So whatever is under the cursor is named exactly, with or without a collider or a readable mesh, along
    /// with the submesh and the exact spot it was hit; if nothing is, what you see there isn't a renderer at all (UI,
    /// a gizmo or post-processing).
    /// <para>While it is on, gameplay input is blocked and the cursor is free. Click locks what's under the cursor
    /// (selected in the Hierarchy in the editor, logged, and copied to the clipboard), the wheel steps through
    /// everything under that spot, right-click lets go, Delete hides the locked thing and Backspace shows everything
    /// again. 1-6 hide whole categories, F9 freezes time, F10 writes the floating-geometry report (editor), F11 draws
    /// the bounds of everything in view.</para>
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class DebugMode : MonoBehaviour
    {
        /// <summary>True while Debug Mode is on.</summary>
        public static bool Active { get; private set; }

        private const int Patch = 33;              // the picking patch round the cursor, in pixels (odd: it has a centre)
        private const int SubmeshBits = 5;         // an id is (thing << SubmeshBits) | submesh
        private const float PickEvery = 0.12f;
        private const float RefreshEvery = 0.5f;   // the renderer list, and category hiding for things that appear
        private const float PanelWidth = 640f;
        private const int Listed = 8;

        private enum Category { Characters, World, Particles, Lines, GroundItems, Other }
        private static readonly string[] CategoryNames = { "Characters", "World", "Particles", "Lines", "Ground items", "Everything else" };

        private static readonly int PickIdProp = Shader.PropertyToID("_DebugPickId");
        private static readonly int PickVpProp = Shader.PropertyToID("_DebugPickVP");
        private static readonly int ColorProp = Shader.PropertyToID("_Color");

        /// <summary>One thing drawn in the patch: a renderer's submesh, or a ground item.</summary>
        private sealed class Thing
        {
            public Renderer Renderer;
            public ItemDef Item;
            public Mesh ItemMesh;
            public Matrix4x4 ItemMatrix;
            public int Submesh;
            public int Pixels;
            public float Dist2 = float.MaxValue;   // its pixel nearest the cursor, squared
            public Vector3 Point;                  // the world position of that pixel
        }

        private Material _mat;
        private RenderTexture _idRT, _posRT;
        private RenderTargetIdentifier[] _targets;
        private Texture2D _idTex, _posTex;

        private Renderer[] _all = new Renderer[0];
        private readonly List<Renderer> _drawn = new List<Renderer>(1024);
        private readonly List<(Matrix4x4 m, Mesh mesh, ItemDef def)> _items = new List<(Matrix4x4, Mesh, ItemDef)>(256);
        private Action<Matrix4x4, Mesh, ItemDef> _visitItem;
        private readonly Plane[] _planes = new Plane[6];
        private readonly Dictionary<int, Thing> _byId = new Dictionary<int, Thing>();
        private readonly List<Thing> _hits = new List<Thing>();
        private readonly List<(Renderer r, float d)> _rayHits = new List<(Renderer, float)>();
        private readonly List<RaycastResult> _uiResults = new List<RaycastResult>();
        private readonly List<string> _ui = new List<string>();

        private readonly bool[] _hidden = new bool[6];
        private readonly Dictionary<Renderer, Category> _catHidden = new Dictionary<Renderer, Category>();
        private readonly HashSet<Renderer> _oneHidden = new HashSet<Renderer>();
        private readonly List<Renderer> _scratch = new List<Renderer>();
        private GroundItemRenderer _groundItems;

        private Thing _locked;
        private int _lockIndex;
        private string _lockedText = "";
        private string _text = "";
        private string _error;
        private bool _frozen, _allBounds;
        private float _prevTimeScale = 1f, _nextPick, _nextRefresh;
        private bool _prevBlocked;
        private Rect _panel;
        private GUIStyle _style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Active = false;   // statics outlive a play session when Play Mode skips the domain reload
            if (!(Application.isEditor || Debug.isDebugBuild) || Application.isBatchMode) return;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            if (FindAnyObjectByType<DebugMode>() != null) return;
            var go = new GameObject("[DebugMode]");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugMode>();
        }

        private void Awake()
        {
            _visitItem = (m, mesh, def) => _items.Add((m, mesh, def));
            StartCoroutine(PickLoop());
        }

        private void OnEnable() => RenderPipelineManager.endCameraRendering += OnEndCamera;

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCamera;
            if (Active) Deactivate();
        }

        private void OnDestroy()
        {
            if (_idRT != null) { _idRT.Release(); Destroy(_idRT); }
            if (_posRT != null) { _posRT.Release(); Destroy(_posRT); }
            if (_idTex != null) Destroy(_idTex);
            if (_posTex != null) Destroy(_posTex);
            if (_mat != null) Destroy(_mat);
        }

        #region On and off
        private void Activate()
        {
            if (!EnsureResources())
            {
                Debug.LogWarning("[DebugMode] the picking shader (Resources/DebugPick) is missing or unsupported: " + _error);
                return;
            }
            Active = true;
            _prevBlocked = UIState.GameplayBlocked;
            _locked = null;
            _lockedText = "";
            _nextRefresh = 0f;
            _nextPick = 0f;
            Debug.Log("[DebugMode] on (F8 to leave)");
        }

        private void Deactivate()
        {
            Active = false;
            ShowAll();
            if (_frozen) ToggleFreeze();
            UIState.GameplayBlocked = _prevBlocked || UIScreen.Current != null;
            _hits.Clear();
            _locked = null;
            Debug.Log("[DebugMode] off");
        }

        private bool EnsureResources()
        {
            if (_mat != null) return true;
            Shader shader = Resources.Load<Shader>("DebugPick");
            if (shader == null || !shader.isSupported) { _error = shader == null ? "not found" : "not supported here"; return false; }
            _mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _idRT = new RenderTexture(Patch, Patch, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                { antiAliasing = 1, filterMode = FilterMode.Point, name = "DebugMode ids" };
            _posRT = new RenderTexture(Patch, Patch, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
                { antiAliasing = 1, filterMode = FilterMode.Point, name = "DebugMode positions" };
            _idRT.Create();
            _posRT.Create();
            _targets = new RenderTargetIdentifier[] { _idRT, _posRT };
            _idTex = new Texture2D(Patch, Patch, TextureFormat.RGBA32, false, true);
            _posTex = new Texture2D(Patch, Patch, TextureFormat.RGBAFloat, false, true);
            return true;
        }
        #endregion

        #region Input
        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f8Key.wasPressedThisFrame)
            {
                if (Active) Deactivate();
                else Activate();
            }
            if (!Active) return;

            // Keep the game's hands off the mouse and keys, and the cursor free to point (screens closing clear the block).
            UIState.GameplayBlocked = true;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshEvery;
                _all = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
                if (AnyHidden()) ApplyCategories();
            }

            for (int c = 0; c < _hidden.Length; c++)
                if (kb[Key.Digit1 + c].wasPressedThisFrame || kb[Key.Numpad1 + c].wasPressedThisFrame)
                {
                    _hidden[c] = !_hidden[c];
                    ApplyCategories();
                }
            if (kb.f9Key.wasPressedThisFrame) ToggleFreeze();
            if (kb.f10Key.wasPressedThisFrame) Report();
            if (kb.f11Key.wasPressedThisFrame) _allBounds = !_allBounds;
            if (kb.deleteKey.wasPressedThisFrame) HideLocked();
            if (kb.backspaceKey.wasPressedThisFrame) ShowAll();

            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            if (mouse.leftButton.wasPressedThisFrame && !OverPanel(mouse.position.ReadValue())) Lock(0);
            if (mouse.rightButton.wasPressedThisFrame) { _locked = null; _lockedText = ""; }
            float wheel = mouse.scroll.ReadValue().y;
            if (wheel != 0f && _hits.Count > 0) Lock(_lockIndex + (wheel > 0f ? -1 : 1));
        }

        private bool OverPanel(Vector2 screen) => _panel.Contains(new Vector2(screen.x, Screen.height - screen.y));

        private static Vector2 CursorPosition()
        {
            Mouse mouse = Mouse.current;
            return mouse != null ? mouse.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        private void ToggleFreeze()
        {
            if (!_frozen)
            {
                _prevTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                _frozen = true;
            }
            else
            {
                Time.timeScale = _prevTimeScale > 0f ? _prevTimeScale : 1f;
                _frozen = false;
            }
        }

        private void Report()
        {
#if UNITY_EDITOR
            FloatingGeometryReport.Run();
#else
            Debug.Log("[DebugMode] the floating-geometry report runs in the editor only");
#endif
        }
        #endregion

        #region Picking
        private IEnumerator PickLoop()
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                // After the frame is drawn, so the camera and every skinned body are where they were drawn.
                yield return endOfFrame;
                if (!Active || Time.unscaledTime < _nextPick) continue;
                _nextPick = Time.unscaledTime + PickEvery;
                Camera cam = Camera.main;
                if (cam == null) continue;
                try
                {
                    Pick(cam, CursorPosition());
                    _error = null;
                }
                catch (Exception e) { _error = e.GetType().Name + ": " + e.Message; }
                _text = BuildText();
            }
        }

        private void Pick(Camera cam, Vector2 screen)
        {
            // What the camera draws: switched on and shown, on a layer it renders, bounds in its view. Unity culls by
            // bounds the same way, so a piece hanging outside them shows only while they're in view; so here too.
            GeometryUtility.CalculateFrustumPlanes(cam, _planes);
            _drawn.Clear();
            foreach (Renderer r in _all)
            {
                if (r == null || !r.enabled || r.forceRenderingOff || !r.gameObject.activeInHierarchy) continue;
                if ((cam.cullingMask & (1 << r.gameObject.layer)) == 0) continue;
                if (!GeometryUtility.TestPlanesAABB(_planes, r.bounds)) continue;
                _drawn.Add(r);
            }
            _items.Clear();
            GroundItemRenderer gir = GroundItemRenderer.Instance;
            if (gir != null && gir.enabled) gir.VisitDrawn(_visitItem);

            // Draw them all, flat in their id colours, into the patch round the cursor.
            Matrix4x4 vp = GL.GetGPUProjectionMatrix(PatchProjection(cam, screen), true) * cam.worldToCameraMatrix;
            var cmd = new CommandBuffer { name = "Debug Mode pick" };
            cmd.SetRenderTarget(_targets, _idRT.depthBuffer);
            cmd.ClearRenderTarget(true, true, Color.clear);
            cmd.SetGlobalMatrix(PickVpProp, vp);
            int maxSub = 1 << SubmeshBits;
            for (int i = 0; i < _drawn.Count; i++)
            {
                Renderer r = _drawn[i];
                int subs = Mathf.Min(SubmeshCount(r), maxSub);
                for (int s = 0; s < subs; s++)
                {
                    cmd.SetGlobalVector(PickIdProp, Encode(((i + 1) << SubmeshBits) | s));
                    cmd.DrawRenderer(r, _mat, s, 0);
                }
            }
            for (int k = 0; k < _items.Count; k++)
            {
                Mesh mesh = _items[k].mesh;
                if (mesh == null) continue;
                int subs = Mathf.Min(mesh.subMeshCount, maxSub);
                for (int s = 0; s < subs; s++)
                {
                    cmd.SetGlobalVector(PickIdProp, Encode(((_drawn.Count + k + 1) << SubmeshBits) | s));
                    cmd.DrawMesh(mesh, _items[k].m, _mat, s, 0);
                }
            }
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _idRT;
            _idTex.ReadPixels(new Rect(0, 0, Patch, Patch), 0, 0, false);
            RenderTexture.active = _posRT;
            _posTex.ReadPixels(new Rect(0, 0, Patch, Patch), 0, 0, false);
            RenderTexture.active = prev;
            Color32[] ids = _idTex.GetPixels32();
            Color[] pos = _posTex.GetPixels();

            // Everything in the patch, nearest the cursor first.
            _byId.Clear();
            _hits.Clear();
            int c = Patch / 2;
            for (int y = 0; y < Patch; y++)
                for (int x = 0; x < Patch; x++)
                {
                    int p = y * Patch + x;
                    Color32 k = ids[p];
                    int id = k.r | (k.g << 8) | (k.b << 16);
                    if (id == 0) continue;
                    if (!_byId.TryGetValue(id, out Thing t))
                    {
                        t = Decode(id);
                        if (t == null) continue;
                        _byId[id] = t;
                        _hits.Add(t);
                    }
                    t.Pixels++;
                    float d2 = (x - c) * (x - c) + (y - c) * (y - c);
                    if (d2 < t.Dist2)
                    {
                        t.Dist2 = d2;
                        t.Point = new Vector3(pos[p].r, pos[p].g, pos[p].b);
                    }
                }
            _hits.Sort((a, b) => a.Dist2 != b.Dist2 ? a.Dist2.CompareTo(b.Dist2) : b.Pixels.CompareTo(a.Pixels));

            // Renderers whose bounds the cursor's ray passes through, nearest first.
            Ray ray = cam.ScreenPointToRay(screen);
            _rayHits.Clear();
            foreach (Renderer r in _drawn)
                if (r.bounds.IntersectRay(ray, out float d)) _rayHits.Add((r, d));
            _rayHits.Sort((a, b) => a.d.CompareTo(b.d));

            // And the UI under it.
            _ui.Clear();
            EventSystem es = EventSystem.current;
            if (es != null)
            {
                _uiResults.Clear();
                es.RaycastAll(new PointerEventData(es) { position = screen }, _uiResults);
                foreach (RaycastResult u in _uiResults)
                    if (u.gameObject != null) _ui.Add(PathOf(u.gameObject.transform));
            }
        }

        // The camera's projection narrowed to the patch round the cursor (a picking matrix), so the patch is drawn at
        // the screen's own pixel size.
        private static Matrix4x4 PatchProjection(Camera cam, Vector2 screen)
        {
            Rect view = cam.pixelRect;
            float cx = 2f * (screen.x - view.x) / view.width - 1f, cy = 2f * (screen.y - view.y) / view.height - 1f;
            float sx = view.width / Patch, sy = view.height / Patch;
            Matrix4x4 m = Matrix4x4.identity;
            m.m00 = sx;
            m.m03 = -sx * cx;
            m.m11 = sy;
            m.m13 = -sy * cy;
            return m * cam.projectionMatrix;
        }

        private static Vector4 Encode(int id) => new Vector4((id & 255) / 255f, ((id >> 8) & 255) / 255f, ((id >> 16) & 255) / 255f, 1f);

        private Thing Decode(int id)
        {
            int index = (id >> SubmeshBits) - 1, sub = id & ((1 << SubmeshBits) - 1);
            if (index >= 0 && index < _drawn.Count) return new Thing { Renderer = _drawn[index], Submesh = sub };
            index -= _drawn.Count;
            if (index >= 0 && index < _items.Count)
                return new Thing { Item = _items[index].def, ItemMesh = _items[index].mesh, ItemMatrix = _items[index].m, Submesh = sub };
            return null;
        }

        private static int SubmeshCount(Renderer r)
        {
            Mesh mesh = MeshOf(r);
            return mesh != null ? Mathf.Max(1, mesh.subMeshCount) : 1;
        }

        private static Mesh MeshOf(Renderer r)
        {
            if (r is SkinnedMeshRenderer smr) return smr.sharedMesh;
            if (r is MeshRenderer)
            {
                var mf = r.GetComponent<MeshFilter>();
                return mf != null ? mf.sharedMesh : null;
            }
            return null;
        }
        #endregion

        #region Locking and hiding
        private void Lock(int index)
        {
            if (_hits.Count == 0)
            {
                _locked = null;
                _lockedText = "Nothing drawn by a renderer under the cursor.";
                return;
            }
            int n = _hits.Count;
            _lockIndex = ((index % n) + n) % n;
            _locked = _hits[_lockIndex];
            _lockedText = Describe(_locked);
            string plain = StripTags(_lockedText);
            Debug.Log("[DebugMode] " + plain);
            GUIUtility.systemCopyBuffer = plain;
#if UNITY_EDITOR
            if (_locked.Renderer != null)
            {
                UnityEditor.Selection.activeGameObject = _locked.Renderer.gameObject;
                UnityEditor.EditorGUIUtility.PingObject(_locked.Renderer.gameObject);
            }
#endif
        }

        private void HideLocked()
        {
            if (_locked == null || _locked.Renderer == null) return;
            Renderer r = _locked.Renderer;
            if (r.enabled)
            {
                r.enabled = false;
                _oneHidden.Add(r);
            }
            else if (_oneHidden.Remove(r)) r.enabled = true;
        }

        private void ShowAll()
        {
            for (int c = 0; c < _hidden.Length; c++) _hidden[c] = false;
            ApplyCategories();
            foreach (Renderer r in _oneHidden) if (r != null) r.enabled = true;
            _oneHidden.Clear();
        }

        private bool AnyHidden()
        {
            foreach (bool h in _hidden) if (h) return true;
            return false;
        }

        // Hides every renderer in a hidden category, and brings back what Debug Mode hid that no longer should be.
        private void ApplyCategories()
        {
            _scratch.Clear();
            foreach (KeyValuePair<Renderer, Category> kv in _catHidden)
                if (!_hidden[(int)kv.Value]) _scratch.Add(kv.Key);
            foreach (Renderer r in _scratch)
            {
                _catHidden.Remove(r);
                if (r != null && !_oneHidden.Contains(r)) r.enabled = true;
            }
            if (AnyHidden())
                foreach (Renderer r in _all)
                {
                    if (r == null || !r.enabled) continue;
                    Category c = CategoryOf(r);
                    if (!_hidden[(int)c]) continue;
                    r.enabled = false;
                    _catHidden[r] = c;
                }

            // Ground items are drawn by one component, so the category hides that.
            if (_groundItems == null) _groundItems = GroundItemRenderer.Instance != null ? GroundItemRenderer.Instance
                : FindAnyObjectByType<GroundItemRenderer>(FindObjectsInactive.Include);
            if (_groundItems != null) _groundItems.enabled = !_hidden[(int)Category.GroundItems];
        }

        private static Category CategoryOf(Renderer r)
        {
            if (r.GetComponentInParent<HumanoidRig>() != null || r.GetComponentInParent<ZombieAI>() != null
                || r.GetComponentInParent<PlayerStats>() != null) return Category.Characters;
            if (r is ParticleSystemRenderer) return Category.Particles;
            if (r is LineRenderer || r is TrailRenderer) return Category.Lines;
            if (r.transform.root.name == "World") return Category.World;
            return Category.Other;
        }
        #endregion

        #region Text
        private string BuildText()
        {
            var sb = new StringBuilder(2048);
            sb.Append("<b>DEBUG MODE</b>   <color=#8cf>F8</color> leave   <color=#8cf>F9</color> time ")
              .Append(_frozen ? "<color=#f96>frozen</color>" : "running").Append("   <color=#8cf>F10</color> report   <color=#8cf>F11</color> all bounds ")
              .Append(_allBounds ? "on" : "off").Append('\n');
            sb.Append("Click lock · wheel next under it · right-click let go · <color=#8cf>Delete</color> hide locked · <color=#8cf>Backspace</color> show all\n");
            sb.Append("Hide: ");
            for (int c = 0; c < CategoryNames.Length; c++)
                sb.Append("<color=#8cf>").Append(c + 1).Append("</color> ")
                  .Append(_hidden[c] ? "<color=#f66><s>" + CategoryNames[c] + "</s> (hidden)</color>" : CategoryNames[c]).Append("   ");
            sb.Append('\n');
            sb.Append("<color=#999>").Append(_drawn.Count).Append(" renderers and ").Append(_items.Count).Append(" ground items in view</color>\n");
            if (_error != null) sb.Append("<color=#f66>pick failed: ").Append(_error).Append("</color>\n");

            sb.Append("\n<b>Under the cursor</b> <color=#999>(").Append(Patch).Append(" px square, nearest first)</color>\n");
            if (_hits.Count == 0)
                sb.Append("  <color=#fc6>Nothing drawn by any renderer or ground item here. If you can see something, it is UI, a gizmo or post-processing.</color>\n");
            for (int i = 0; i < _hits.Count && i < Listed; i++)
                sb.Append(i == 0 ? "  <color=#ff6>▶</color> " : "     ").Append(Short(_hits[i])).Append('\n');

            if (_ui.Count > 0)
            {
                sb.Append("<b>UI under the cursor</b>\n");
                for (int i = 0; i < _ui.Count && i < 4; i++) sb.Append("     ").Append(_ui[i]).Append('\n');
            }
            if (_rayHits.Count > 0)
            {
                sb.Append("<b>Bounds the cursor's ray passes through</b>\n");
                for (int i = 0; i < _rayHits.Count && i < 5; i++)
                    sb.Append("     ").Append(PathOf(_rayHits[i].r.transform)).Append(" <color=#999>(")
                      .Append(_rayHits[i].r.GetType().Name).Append(", from ").Append(_rayHits[i].d.ToString("0.0")).Append(" m)</color>\n");
            }
            if (_lockedText.Length > 0) sb.Append("\n<b>Locked</b> <color=#999>(logged and copied to the clipboard)</color>\n").Append(_lockedText);
            return sb.ToString();
        }

        private static string Short(Thing t)
        {
            var sb = new StringBuilder(160);
            if (t.Renderer != null)
            {
                sb.Append(PathOf(t.Renderer.transform)).Append(" <color=#999>").Append(t.Renderer.GetType().Name);
                Material[] mats = t.Renderer.sharedMaterials;
                if (t.Submesh < mats.Length && mats[t.Submesh] != null) sb.Append(" · ").Append(mats[t.Submesh].name);
                sb.Append(" · submesh ").Append(t.Submesh);
            }
            else sb.Append("ground item ").Append(t.Item != null ? t.Item.DisplayName + " (" + t.Item.Id + ")" : "?").Append(" <color=#999>instanced");
            sb.Append(" · ").Append(t.Pixels).Append(" px · at ").Append(t.Point.ToString("F2")).Append("</color>");
            return sb.ToString();
        }

        private static string Describe(Thing t)
        {
            var sb = new StringBuilder(512);
            if (t.Renderer == null)
            {
                sb.Append("Ground item ").Append(t.Item != null ? t.Item.DisplayName + " (" + t.Item.Id + ")" : "?")
                  .Append(", drawn instanced by GroundItemRenderer\n  mesh ").Append(t.ItemMesh != null ? t.ItemMesh.name : "?")
                  .Append(", placed at ").Append(((Vector3)t.ItemMatrix.GetColumn(3)).ToString("F2"))
                  .Append("\n  hit at ").Append(t.Point.ToString("F2")).Append('\n');
                return sb.ToString();
            }
            Renderer r = t.Renderer;
            Transform tr = r.transform;
            Bounds b = r.bounds;
            Mesh mesh = MeshOf(r);
            Material[] mats = r.sharedMaterials;
            Material mat = t.Submesh < mats.Length ? mats[t.Submesh] : null;
            sb.Append(PathOf(tr)).Append('\n');
            sb.Append("  ").Append(r.GetType().Name).Append(", layer ").Append(LayerMask.LayerToName(tr.gameObject.layer))
              .Append(", shadows ").Append(r.shadowCastingMode).Append('\n');
            sb.Append("  hit at ").Append(t.Point.ToString("F2")).Append(" (height ").Append(t.Point.y.ToString("0.00")).Append(" m)");
            if (!b.Contains(t.Point) && b.SqrDistance(t.Point) > 0.0025f)
                sb.Append("  <color=#f66>outside the renderer's own bounds by ").Append(Mathf.Sqrt(b.SqrDistance(t.Point)).ToString("0.00")).Append(" m</color>");
            sb.Append('\n');
            sb.Append("  submesh ").Append(t.Submesh).Append(", material ").Append(mat != null ? mat.name : "none")
              .Append(", shader ").Append(mat != null && mat.shader != null ? mat.shader.name : "none").Append('\n');
            if (mesh != null)
                sb.Append("  mesh ").Append(mesh.name).Append(": ").Append(mesh.vertexCount).Append(" vertices, ")
                  .Append(mesh.subMeshCount).Append(" submeshes").Append(mesh.isReadable ? "" : ", not readable").Append('\n');
            if (r is SkinnedMeshRenderer smr)
                sb.Append("  skinned: root bone ").Append(smr.rootBone != null ? smr.rootBone.name : "none").Append(", ")
                  .Append(smr.bones != null ? smr.bones.Length : 0).Append(" bones, update when offscreen ").Append(smr.updateWhenOffscreen).Append('\n');
            sb.Append("  object at ").Append(tr.position.ToString("F2")).Append(", scale ").Append(tr.lossyScale.ToString("F2"))
              .Append("\n  bounds centre ").Append(b.center.ToString("F2")).Append(", size ").Append(b.size.ToString("F2")).Append('\n');
            return sb.ToString();
        }

        private static string StripTags(string s) => System.Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "");

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (t = t.parent; t != null; t = t.parent) path = t.name + "/" + path;
            return path;
        }
        #endregion

        #region Drawing
        private void OnGUI()
        {
            if (!Active) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = true };
                _style.normal.textColor = Color.white;
            }
            float h = _style.CalcHeight(new GUIContent(_text), PanelWidth - 16f) + 12f;
            _panel = new Rect(10f, 10f, PanelWidth, h);
            Color was = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(_panel, Texture2D.whiteTexture);
            GUI.color = was;
            GUI.Label(new Rect(_panel.x + 8f, _panel.y + 6f, _panel.width - 16f, h), _text, _style);

            // The picking square round the cursor.
            Vector2 m = CursorPosition();
            float half = Patch * 0.5f, x = m.x - half, y = Screen.height - m.y - half;
            GUI.color = new Color(1f, 0.9f, 0.2f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, Patch, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y + Patch - 1f, Patch, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y, 1f, Patch), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + Patch - 1f, y, 1f, Patch), Texture2D.whiteTexture);
            GUI.color = was;
        }

        // Bounds boxes over the view: the locked thing in yellow, what's under the cursor in cyan, and with F11
        // everything in view in grey; a cross where the locked thing was hit.
        private void OnEndCamera(ScriptableRenderContext context, Camera cam)
        {
            if (!Active || _mat == null || cam != Camera.main) return;
            GL.PushMatrix();
            GL.LoadProjectionMatrix(cam.projectionMatrix);
            GL.modelview = cam.worldToCameraMatrix;
            _mat.SetColor(ColorProp, Color.white);
            _mat.SetPass(1);
            GL.Begin(GL.LINES);
            if (_allBounds)
                for (int i = 0; i < _drawn.Count && i < 600; i++)
                    if (_drawn[i] != null) Box(_drawn[i].bounds, new Color(0.7f, 0.7f, 0.7f, 0.35f));
            for (int i = 0; i < _hits.Count && i < 3; i++)
                if (_hits[i].Renderer != null) Box(_hits[i].Renderer.bounds, new Color(0.3f, 0.9f, 1f, 0.8f));
            if (_locked != null)
            {
                if (_locked.Renderer != null) Box(_locked.Renderer.bounds, new Color(1f, 0.9f, 0.1f, 1f));
                Cross(_locked.Point, 0.25f, new Color(1f, 0.3f, 0.2f, 1f));
            }
            GL.End();
            GL.PopMatrix();
        }

        private static void Box(Bounds b, Color c)
        {
            Vector3 n = b.min, x = b.max;
            GL.Color(c);
            Line(new Vector3(n.x, n.y, n.z), new Vector3(x.x, n.y, n.z)); Line(new Vector3(n.x, x.y, n.z), new Vector3(x.x, x.y, n.z));
            Line(new Vector3(n.x, n.y, x.z), new Vector3(x.x, n.y, x.z)); Line(new Vector3(n.x, x.y, x.z), new Vector3(x.x, x.y, x.z));
            Line(new Vector3(n.x, n.y, n.z), new Vector3(n.x, x.y, n.z)); Line(new Vector3(x.x, n.y, n.z), new Vector3(x.x, x.y, n.z));
            Line(new Vector3(n.x, n.y, x.z), new Vector3(n.x, x.y, x.z)); Line(new Vector3(x.x, n.y, x.z), new Vector3(x.x, x.y, x.z));
            Line(new Vector3(n.x, n.y, n.z), new Vector3(n.x, n.y, x.z)); Line(new Vector3(x.x, n.y, n.z), new Vector3(x.x, n.y, x.z));
            Line(new Vector3(n.x, x.y, n.z), new Vector3(n.x, x.y, x.z)); Line(new Vector3(x.x, x.y, n.z), new Vector3(x.x, x.y, x.z));
        }

        private static void Cross(Vector3 p, float s, Color c)
        {
            GL.Color(c);
            Line(p - Vector3.right * s, p + Vector3.right * s);
            Line(p - Vector3.up * s, p + Vector3.up * s);
            Line(p - Vector3.forward * s, p + Vector3.forward * s);
        }

        private static void Line(Vector3 a, Vector3 b)
        {
            GL.Vertex(a);
            GL.Vertex(b);
        }
        #endregion
    }
}
