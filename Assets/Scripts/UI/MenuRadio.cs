using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The title screen's radio, small in the bottom-left corner: the music stations (<see cref="MusicStations"/>)
    /// round a dial of their own, stepped through with a pair of arrows — off, then station by station — while the
    /// label reads what's on. While it plays, the score's theme bows out (<see cref="MusicDirector"/> watches
    /// <see cref="Playing"/>). It remembers the last station picked and comes back on with the menu; it goes down
    /// with the menu when a run starts. Built onto the menu's root, so it plays on through the sub-screens.
    /// </summary>
    public sealed class MenuRadio : MonoBehaviour
    {
        private const string PrefKey = "clonez.menu.radio";   // the chosen station's folder ("" / unset: off)

        /// <summary>True while the menu radio plays a station (the title theme stands aside).</summary>
        public static bool Playing { get; private set; }

        private StationPlayer _player;
        private Text _label;
        private int _pick = -1;   // -1 off, else an index into the music stations
        private string _shown;

        /// <summary>Builds the radio onto the menu's <paramref name="root"/> (nothing when there are no stations).</summary>
        public static void Build(RectTransform root)
        {
            if (MusicStations.Count == 0) return;
            RectTransform row = UIKit.Row(root, 30f, null, 6f, 0);
            UIKit.Place(row, Vector2.zero, Vector2.zero, new Vector2(20f, 36f), new Vector2(660f, 30f));
            var radio = row.gameObject.AddComponent<MenuRadio>();
            Button prev = UIKit.Button(row, "\u25c2", () => radio.Step(-1), UIKit.ButtonColor, 14);
            UIKit.Size(prev.gameObject, 30f, 26f);
            Button next = UIKit.Button(row, "\u25b8", () => radio.Step(1), UIKit.ButtonColor, 14);
            UIKit.Size(next.gameObject, 30f, 26f);
            radio._label = UIKit.Label(row, "", 14, new Color(0.68f, 0.71f, 0.76f), TextAnchor.MiddleLeft);
            UIKit.Size(radio._label.gameObject, 580f, 26f);
        }

        private void Start()
        {
            _pick = MusicStations.IndexOf(PlayerPrefs.GetString(PrefKey, ""));
            Apply();
        }

        // Steps the dial: off, then each station in turn.
        private void Step(int d)
        {
            int n = MusicStations.Count + 1;
            _pick = ((_pick + 1 + d) % n + n) % n - 1;
            PlayerPrefs.SetString(PrefKey, _pick < 0 ? "" : MusicStations.Of(MusicStations.StationAt(_pick)).Folder);
            Apply();
        }

        private void Apply()
        {
            if (_pick < 0)
            {
                if (_player != null) Destroy(_player.gameObject);
                _player = null;
                Playing = false;
            }
            else
            {
                BroadcastStation s = MusicStations.StationAt(_pick);
                if (_player == null) _player = StationPlayer.AttachFlat(transform, s);
                else _player.Tune(s);
                _player.SetLoudness(1);
                Playing = true;
            }
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            string text;
            if (_pick < 0) text = "Radio off";
            else
            {
                BroadcastStation s = MusicStations.StationAt(_pick);
                text = Broadcasts.NameOf(s) + " \u2014 " + Broadcasts.TitleOf(s) + "    " + (MusicStations.NowPlayingLine(s) ?? "");
            }
            if (text == _shown || _label == null) return;
            _shown = text;
            _label.text = text;
        }

        private void OnDisable()
        {
            if (_player != null) Destroy(_player.gameObject);
            _player = null;
            Playing = false;
        }
    }
}
