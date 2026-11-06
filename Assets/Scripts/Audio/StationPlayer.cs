using UnityEngine;
using UnityEngine.Networking;

namespace CloneZ
{
    /// <summary>
    /// The music coming out of one set: streams its station's rotation (<see cref="MusicStations"/>) from the disk,
    /// one track after the next, through a positional source at the set — or a flat one for the title screen's radio
    /// (<see cref="AttachFlat"/>). The set drives it — attach it while a music station plays, retune it with
    /// <see cref="Tune"/>, keep <see cref="SetLoudness"/> at the set's volume step, and destroy it (its GameObject)
    /// when the set goes off or the dial moves on. On a dedicated server there's no audio backend: the player quietly
    /// does nothing.
    /// </summary>
    public sealed class StationPlayer : MonoBehaviour
    {
        private MusicStations.Station _station;
        private AudioSource _src;
        private UnityWebRequest _loading, _playingReq;
        private AudioClip _clip;
        private string _loadingPath;
        private float _retryAt;
        private int _loudness = -1;
        private bool _dead, _flat;

        /// <summary>Puts a player for <paramref name="station"/> on <paramref name="host"/> (the set).</summary>
        public static StationPlayer Attach(Transform host, BroadcastStation station)
        {
            var go = new GameObject("Station player");
            go.transform.SetParent(host, false);
            var p = go.AddComponent<StationPlayer>();
            p.Tune(station);
            return p;
        }

        /// <summary>A flat (unpositioned) player for <paramref name="station"/> — the title screen's radio.</summary>
        public static StationPlayer AttachFlat(Transform host, BroadcastStation station)
        {
            StationPlayer p = Attach(host, station);
            p._flat = true;
            return p;
        }

        /// <summary>Moves the player to another station (a fresh track at once).</summary>
        public void Tune(BroadcastStation station)
        {
            MusicStations.Station st = MusicStations.Of(station);
            if (st == _station) return;
            _station = st;
            DropLoading();
            DropPlaying();
        }

        /// <summary>The set's volume step (0 low, 1 medium, 2 loud) onto the music's gain and reach.</summary>
        public void SetLoudness(int volume)
        {
            volume = Mathf.Clamp(volume, 0, CloneZConfig.MusicGain.Length - 1);
            if (volume == _loudness) return;
            _loudness = volume;
            Apply();
        }

        private void Apply()
        {
            if (_src == null || _loudness < 0) return;
            Sfx.SetVolume(_src, CloneZConfig.MusicGain[_loudness]);
            _src.maxDistance = CloneZConfig.MusicHeardRange * CloneZConfig.ReceiverHearingScale[_loudness];
        }

        private void Update()
        {
            if (_dead || _station == null) return;
            if (_src == null)
            {
                _src = Sfx.CreateSource("Radio music", SfxBus.Music, transform, !_flat, CloneZConfig.MusicHeardRange);
                if (_src == null) { _dead = true; return; }   // no audio backend (a dedicated server)
                _src.loop = false;
                Apply();
            }
            if (_loading != null)
            {
                if (!_loading.isDone) return;
                UnityWebRequest req = _loading;
                _loading = null;
                AudioClip clip = MusicStations.FinishLoad(req, out string error);
                if (clip == null)
                {
                    req.Dispose();
                    Debug.LogWarning("[CloneZ] The radio couldn't play \"" + _loadingPath + "\": " + error);
                    _retryAt = Time.unscaledTime + 3f;   // on to the next track in a moment
                    return;
                }
                DropPlaying();
                _clip = clip;
                _playingReq = req;
                _src.clip = clip;
                _src.Play();
                MusicStations.PutOn(_station, _loadingPath);
                return;
            }
            // Nothing on (or the track has run out): put the next one on.
            if (_clip != null && _src.isPlaying) return;
            if (Time.unscaledTime < _retryAt) return;
            string path = MusicStations.NextTrack(_station);
            if (path == null) { _retryAt = Time.unscaledTime + 10f; return; }
            _loadingPath = path;
            _loading = MusicStations.BeginLoad(path);
        }

        private void OnDestroy()
        {
            _dead = true;
            DropLoading();
            DropPlaying();
            if (_src != null) Sfx.Stop(_src);
            _src = null;
        }

        private void DropLoading()
        {
            if (_loading == null) return;
            _loading.Dispose();
            _loading = null;
        }

        // The request must outlive its streamed clip: both go together.
        private void DropPlaying()
        {
            if (_src != null)
            {
                _src.Stop();
                _src.clip = null;
            }
            if (_clip != null) Destroy(_clip);
            _clip = null;
            if (_playingReq != null) _playingReq.Dispose();
            _playingReq = null;
        }
    }
}
