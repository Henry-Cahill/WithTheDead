#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CloneZ
{
    // The cutaway harness's photo rig: the player camera's isometric pose, the game's overcast day and raid night (the
    // same sums MapShots replicates), a survivor stand-in, and the capture.
    public static partial class CutawayShots
    {
        private sealed partial class Harness
        {
            // IsoCameraFollow's numbers (read from its defaults in Setup).
            private float _isoYaw = 45f, _isoPitch = 38f, _isoClearance = 9f, _isoNear = 0.3f, _lookHeight = 1f;
            private float[] _zoom = { 5.5f, 7f, 9f, 11f, 12.5f };

            private GameObject _rig, _figure;
            private Transform _feetMark;
            private Camera _cam;
            private Light _sun;
            private Volume _volume;
            private bool _cameraRender, _pathConfirmed;
            private readonly List<Light> _mapLights = new List<Light>();
            // The game's other cutaway (rays from the camera, by collider), run for the pictures that ask for it.
            private readonly IsoCutaway _isoCut = new IsoCutaway();
            private const float ChestHeight = 1.2f;   // IsoCameraFollow's for a standing survivor

            private float Wide => _zoom[_zoom.Length - 1];
            private float Close => _zoom[0];

            private float IsoDistance(float size) => size / Mathf.Tan(Mathf.Clamp(_isoPitch, 15f, 89f) * Mathf.Deg2Rad) + _isoClearance;

            /// <summary>How far along the ground the cutaway follows the sight line at zoom <paramref name="size"/>: to the
            /// camera, as <see cref="MapCutaway.Tick"/> works it out.</summary>
            private float Reach(float size) => IsoDistance(size) * Mathf.Cos(Mathf.Clamp(_isoPitch, 10f, 89f) * Mathf.Deg2Rad) + 1f;

            private void ReadCamera()
            {
                var go = new GameObject("[CutawayShots] camera numbers");
                go.SetActive(false);
                try
                {
                    var f = go.AddComponent<IsoCameraFollow>();
                    _isoYaw = f.IsoYaw;
                    _isoPitch = f.IsoPitch;
                    _isoClearance = f.IsoClearance;
                    _isoNear = f.IsoNearClip;
                    _lookHeight = f.LookHeight;
                    if (f.IsoZoomSizes != null && f.IsoZoomSizes.Length > 0) _zoom = (float[])f.IsoZoomSizes.Clone();
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }

            private void BuildRig()
            {
                _rig = new GameObject("[CutawayShots]");
                var camGo = new GameObject("Camera");
                camGo.transform.SetParent(_rig.transform, false);
                _cam = camGo.AddComponent<Camera>();
                _cam.enabled = false;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.allowHDR = true;
                _cam.allowMSAA = false;
                _cam.farClipPlane = 400f;
                UniversalAdditionalCameraData data = _cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.renderShadows = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;

                var sunGo = new GameObject("Sun");
                sunGo.transform.SetParent(_rig.transform, false);
                _sun = sunGo.AddComponent<Light>();
                _sun.type = LightType.Directional;
                _sun.shadows = LightShadows.Soft;
                _sun.useColorTemperature = true;
                _sun.colorTemperature = 5000f;
                RenderSettings.sun = _sun;

                if (_profile != null)
                {
                    var volGo = new GameObject("Game Volume");
                    volGo.transform.SetParent(_rig.transform, false);
                    _volume = volGo.AddComponent<Volume>();
                    _volume.isGlobal = true;
                    _volume.sharedProfile = _profile;
                }

                // A survivor-sized stand-in (1.8 m), as MapShots has.
                _figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _figure.name = "Survivor";
                Object.DestroyImmediate(_figure.GetComponent<Collider>());
                _figure.transform.SetParent(_rig.transform, false);
                _figure.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
                Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                var m = new Material(sh != null ? sh : Shader.Find("Standard")) { name = "[CutawayShots] survivor" };
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.75f, 0.12f, 0.10f));
                _figure.GetComponent<Renderer>().sharedMaterial = m;
                _figure.SetActive(false);
                _feetMark = new GameObject("Feet").transform;
                _feetMark.SetParent(_rig.transform, false);
            }

            private void CollectLights(IEnumerable<Transform> roots)
            {
                _mapLights.Clear();
                foreach (Transform root in roots)
                    if (root != null) _mapLights.AddRange(root.GetComponentsInChildren<Light>(true));
            }

            // The game's overcast day (10:00) or the raid's night (the zone's start hour), by the sums MapShots replicates
            // from TimeOfDay, WeatherSystem and VisionController (no fog: the pictures are about geometry).
            private void Light(bool night, Vector3 survivor)
            {
                float hour = night ? _zone.StartHour : 10f;
                const float cloud = 0.7f;
                float elevation = Mathf.Sin(hour / 24f * Mathf.PI * 2f - Mathf.PI * 0.5f);
                float daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.18f, 0.28f, elevation));
                float weatherDim = Mathf.Lerp(1f, 0.5f, cloud);
                float level = Mathf.Clamp01(Mathf.Lerp(CloneZConfig.NightLightFloor, 1f, daylight) * weatherDim);
                float dark = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 0.55f, level));
                float ambientScale = Mathf.Lerp(1f, 0.25f, dark), moonScale = Mathf.Lerp(1f, 0.3f, dark);
                float warmth = Mathf.Clamp01(elevation * 1.6f);
                _sun.transform.rotation = Quaternion.Euler((hour - 6f) / 12f * 180f, -35f + hour / 24f * 30f, 0f);
                _sun.color = Color.Lerp(new Color(1f, 0.62f, 0.36f), new Color(1f, 0.96f, 0.86f), warmth);
                _sun.intensity = Mathf.Lerp(CloneZConfig.NightSunIntensity * moonScale, CloneZConfig.DaySunIntensity, daylight) * weatherDim;
                _sun.shadowStrength = Mathf.Lerp(0.35f, 0.8f, daylight);
                Color ambient = Color.Lerp(new Color(0.07f, 0.08f, 0.13f), new Color(0.40f, 0.42f, 0.47f), daylight)
                    * Mathf.Lerp(0.7f, 1f, weatherDim) * ambientScale;
                _cam.backgroundColor = Color.Lerp(new Color(0.02f, 0.03f, 0.06f), new Color(0.30f, 0.42f, 0.55f), daylight)
                    * Mathf.Lerp(0.6f, 1f, weatherDim) * ambientScale;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = ambient;
                var probe = new SphericalHarmonicsL2();
                probe.AddAmbientLight(ambient);
                RenderSettings.ambientProbe = probe;
                RenderSettings.fog = false;
                // The map's own lights by night, as their budgets would have them round the survivor (unshadowed here).
                foreach (Light l in _mapLights)
                {
                    if (l == null) continue;
                    l.enabled = night && (l.transform.position - survivor).sqrMagnitude < RoomLight.CullDistance * RoomLight.CullDistance;
                    l.shadows = LightShadows.None;
                }
            }

            // IsoCameraFollow.PoseIsometric: the fixed heading and pitch, centred on the survivor's chest, backed off with the zoom.
            private void PoseIso(Vector3 survivor, float yaw, float size)
            {
                Quaternion rot = Quaternion.Euler(_isoPitch, yaw, 0f);
                Vector3 focus = survivor + Vector3.up * _lookHeight;
                _cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * IsoDistance(size), rot);
                _cam.orthographic = true;
                _cam.orthographicSize = size;
                _cam.nearClipPlane = _isoNear;
                _cam.farClipPlane = 400f;
            }

            /// <summary>Renders the isometric view at <paramref name="at"/> into <paramref name="file"/>; with
            /// <paramref name="iso"/>, the game's ray cutaway (<see cref="IsoCutaway"/>) is run for the shot as well.</summary>
            private void Picture(string file, Vector3 at, float yaw, float size, bool survivor, bool night, bool iso = false)
            {
                Light(night, at);
                _figure.SetActive(survivor);
                if (survivor) _figure.transform.SetPositionAndRotation(at + Vector3.up * 0.9f, Quaternion.identity);
                PoseIso(at, yaw, size);
                if (iso)
                {
                    _feetMark.position = at;
                    Physics.SyncTransforms();
                    _isoCut.Tick(_cam.transform.position, _cam.transform.forward, true, _feetMark, ChestHeight, true);
                    _isoHidden = _isoCut.HiddenCount;
                }
                Texture2D tex = null;
                try
                {
                    tex = Capture(1600, 1000);
                    File.WriteAllBytes(Path.Combine(_out, file), tex.EncodeToPNG());
                }
                finally
                {
                    if (tex != null) Object.DestroyImmediate(tex);
                    _figure.SetActive(false);
                    if (iso) _isoCut.RestoreAll();
                }
            }

            /// <summary>Pieces the ray cutaway hid for the last picture that ran it.</summary>
            private int _isoHidden;

            private Texture2D Capture(int w, int h)
            {
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "[CutawayShots]", antiAliasing = 1 };
                rt.Create();
                RenderTexture active = RenderTexture.active;
                try
                {
                    // Twice: the first frame of a new view settles anything first-use (shader variants, volume blending).
                    RenderInto(rt);
                    RenderInto(rt);
                    Texture2D tex = Read(rt, w, h);
                    if (!_pathConfirmed)
                    {
                        if (Uniform(tex))
                        {
                            _cameraRender = !_cameraRender;
                            Note("the first picture came out one flat colour; switching to " + (_cameraRender ? "Camera.Render" : "a render request"));
                            Object.DestroyImmediate(tex);
                            RenderInto(rt);
                            RenderInto(rt);
                            tex = Read(rt, w, h);
                        }
                        _pathConfirmed = true;
                    }
                    return tex;
                }
                finally
                {
                    RenderTexture.active = active;
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }

            private static Texture2D Read(RenderTexture rt, int w, int h)
            {
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                tex.Apply(false);
                return tex;
            }

            private static bool Uniform(Texture2D tex)
            {
                Color32 first = tex.GetPixel(0, 0);
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        Color32 p = tex.GetPixel(x * tex.width / 16 + 3, y * tex.height / 16 + 3);
                        if (p.r != first.r || p.g != first.g || p.b != first.b) return false;
                    }
                return true;
            }

            private void RenderInto(RenderTexture rt)
            {
                _cam.enabled = true;
                try
                {
                    var request = new RenderPipeline.StandardRequest { destination = rt };
                    if (!_cameraRender && RenderPipeline.SupportsRenderRequest(_cam, request))
                    {
                        RenderPipeline.SubmitRenderRequest(_cam, request);
                        return;
                    }
                    _cam.targetTexture = rt;
                    try { _cam.Render(); }
                    finally { _cam.targetTexture = null; }
                }
                finally
                {
                    _cam.enabled = false;
                }
            }

            private void DropRig()
            {
                if (_rig != null) Object.DestroyImmediate(_rig);
                _rig = null;
            }
        }
    }
}
#endif
