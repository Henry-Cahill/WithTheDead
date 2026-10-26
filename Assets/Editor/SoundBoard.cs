using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// WithTheDead ▸ Sound Board: every procedural sound by family, any variation played on the spot (no play mode), and
    /// exports of the whole library (and the music previews) as WAV files for listening outside Unity. Sounds are
    /// rendered by the same synthesiser the game uses, so what plays here is exactly what the game plays.
    /// </summary>
    public sealed class SoundBoard : EditorWindow
    {
        private SfxSynth _synth;
        private Vector2 _scroll;
        private string _filter = "";
        private string _status = "";
        private readonly Dictionary<string, bool> _open = new Dictionary<string, bool>();
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private GameObject _host;
        private AudioSource _source;
        private static MethodInfo s_PlayPreview, s_StopPreviews;

        [MenuItem("WithTheDead/Sound Board")]
        private static void Open() => GetWindow<SoundBoard>("Sound Board");

        private void OnEnable()
        {
            _synth = new SfxSynth();
            BindPreview();
        }

        private void OnDisable()
        {
            Stop();
            foreach (AudioClip c in _clips.Values) if (c != null) DestroyImmediate(c);
            _clips.Clear();
            if (_host != null) DestroyImmediate(_host);
        }

        private void OnGUI()
        {
            if (_synth == null) _synth = new SfxSynth();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));
                if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(50f))) Stop();
                if (GUILayout.Button("Export all…", EditorStyles.toolbarButton, GUILayout.Width(90f))) ExportAll();
                if (GUILayout.Button("Export music previews…", EditorStyles.toolbarButton, GUILayout.Width(150f))) ExportMusic();
            }
            if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, MessageType.None);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (IGrouping<string, string> family in _synth.Ids.GroupBy(id => _synth.FamilyOf(id) ?? "Other"))
            {
                List<string> ids = family.Where(Matches).ToList();
                if (ids.Count == 0) continue;
                if (!_open.TryGetValue(family.Key, out bool open)) open = !string.IsNullOrEmpty(_filter);
                open = EditorGUILayout.Foldout(open || !string.IsNullOrEmpty(_filter), $"{family.Key} ({ids.Count})", true);
                _open[family.Key] = open;
                if (!open) continue;
                foreach (string id in ids) Row(id);
            }
            EditorGUILayout.EndScrollView();
        }

        private bool Matches(string id) => string.IsNullOrEmpty(_filter) || id.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0;

        private void Row(string id)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                SfxMeta meta = _synth.Meta(id);
                GUILayout.Space(14f);
                EditorGUILayout.LabelField(id, GUILayout.Width(190f));
                EditorGUILayout.LabelField(meta.Bus + (_synth.IsLoop(id) ? " · loop" : ""), EditorStyles.miniLabel, GUILayout.Width(110f));
                int n = _synth.VariationsOf(id);
                for (int v = 0; v < n; v++)
                    if (GUILayout.Button((v + 1).ToString(), EditorStyles.miniButton, GUILayout.Width(26f))) Play(id, v);
            }
        }

        private void Play(string id, int variation)
        {
            string key = id + "#" + variation;
            var sw = Stopwatch.StartNew();
            if (!_clips.TryGetValue(key, out AudioClip clip) || clip == null)
            {
                float[] data = _synth.RenderSamples(id, variation, out int ch, out int rate);
                if (data == null) return;
                clip = AudioClip.Create(key, data.Length / ch, ch, rate, false);
                clip.SetData(data, 0);
                clip.hideFlags = HideFlags.HideAndDontSave;
                _clips[key] = clip;
            }
            long ms = sw.ElapsedMilliseconds;
            Stop();
            bool loop = _synth.IsLoop(id);
            if (!PlayPreview(clip, loop))
            {
                if (_host == null)
                {
                    _host = EditorUtility.CreateGameObjectWithHideFlags("SoundBoard", HideFlags.HideAndDontSave, typeof(AudioSource));
                    _source = _host.GetComponent<AudioSource>();
                    _source.playOnAwake = false;
                    _source.spatialBlend = 0f;
                }
                _source.clip = clip;
                _source.loop = loop;
                _source.Play();
            }
            _status = $"{id} #{variation + 1}: {clip.length:0.00} s, {clip.channels} ch, {clip.frequency} Hz (rendered in {ms} ms)";
            Repaint();
        }

        private void Stop()
        {
            if (s_StopPreviews != null)
            {
                try { s_StopPreviews.Invoke(null, null); }
                catch (Exception) { }
            }
            if (_source != null) _source.Stop();
        }

        private void ExportAll()
        {
            string dir = EditorUtility.SaveFolderPanel("Export every sound as WAV", DefaultExportDir(), "");
            if (string.IsNullOrEmpty(dir)) return;
            List<string> ids = _synth.Ids.ToList();
            int done = 0, total = ids.Sum(id => _synth.VariationsOf(id));
            try
            {
                foreach (string id in ids)
                {
                    string family = Path.Combine(dir, _synth.FamilyOf(id) ?? "Other");
                    Directory.CreateDirectory(family);
                    for (int v = 0; v < _synth.VariationsOf(id); v++)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar("Exporting sounds", id, done / (float)Mathf.Max(1, total))) return;
                        float[] data = _synth.RenderSamples(id, v, out int ch, out int rate);
                        if (data != null) WavFile.Write(Path.Combine(family, $"{id}_{v + 1}.wav"), data, ch, rate);
                        done++;
                    }
                }
                _status = $"Exported {done} WAV files to {dir}";
                EditorUtility.RevealInFinder(dir);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private void ExportMusic()
        {
            string dir = EditorUtility.SaveFolderPanel("Export music previews as WAV", DefaultExportDir(), "");
            if (string.IsNullOrEmpty(dir)) return;
            MethodInfo render = typeof(SfxSynth).Assembly.GetType("CloneZ.MusicPreview")?.GetMethod("Render", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
            if (render == null) { _status = "No music previews in this build."; return; }
            try
            {
                EditorUtility.DisplayProgressBar("Exporting music previews", "Rendering…", 0.5f);
                render.Invoke(null, new object[] { dir });
                _status = "Music previews written to " + dir;
                EditorUtility.RevealInFinder(dir);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static string DefaultExportDir() => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AudioExport"));

        // The editor's own clip preview (internal AudioUtil) plays in edit mode regardless of scene audio; when it is
        // missing in this Unity, a hidden AudioSource plays instead.
        private static void BindPreview()
        {
            if (s_PlayPreview != null) return;
            Type util = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (util == null) return;
            foreach (MethodInfo m in util.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] p = m.GetParameters();
                if (m.Name == "PlayPreviewClip" && p.Length >= 1 && p[0].ParameterType == typeof(AudioClip)) s_PlayPreview = m;
                else if (m.Name == "StopAllPreviewClips" && p.Length == 0) s_StopPreviews = m;
            }
        }

        private static bool PlayPreview(AudioClip clip, bool loop)
        {
            if (s_PlayPreview == null) return false;
            ParameterInfo[] p = s_PlayPreview.GetParameters();
            var args = new object[p.Length];
            args[0] = clip;
            for (int i = 1; i < p.Length; i++)
            {
                Type t = p[i].ParameterType;
                args[i] = t == typeof(bool) ? (object)loop : t == typeof(int) ? (object)0 : p[i].HasDefaultValue ? p[i].DefaultValue : null;
            }
            try
            {
                s_PlayPreview.Invoke(null, args);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
