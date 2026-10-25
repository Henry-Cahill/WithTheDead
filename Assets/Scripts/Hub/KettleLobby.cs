using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// The starter lobby standing in the town: The Rusty Kettle is built on a lot in Bardstown that the Kentucky map
    /// keeps clear for it (<see cref="KentuckyMap.ReservedLot"/>), with the whole statewide map around it, and its front
    /// door opens straight onto the street — walking out of it starts the raid where the survivor stands, with no reload
    /// (the bootstrapper's in-place start, <see cref="BeginRaid"/>). Once they're out, the bar shuts behind them until
    /// they're back through an exit. The other ways in (a scav run, the open Zone, a hunt, the helicopter) still go
    /// through the door's deploy screen. A networked session, the other hubs, or a town the world store can't build
    /// fall back to the hub on its own (<see cref="HubWorld.Build"/>).
    /// </summary>
    public static class KettleLobby
    {
        /// <summary>The zone the Kettle stands in.</summary>
        public const string ZoneId = "bardstown";

        /// <summary>The bar's lot: its walls, the porch and the pole sign out front, and a margin (in the bar's own plan,
        /// its door on the -z side).</summary>
        public static readonly KentuckyMap.LotSize LotSize = new KentuckyMap.LotSize(14.9f, 13.4f, 10.9f);

        /// <summary>How far round the bar's lot the town is kept clear of the Zone's hazards and set pieces (no anomaly,
        /// no radiation, nothing the Zone's overlays put down): the safe zone the survivor walks out into.</summary>
        public const float SafeMargin = 20f;

        /// <summary>Set when the town couldn't hold the bar this session: the hub is built on its own until the game restarts.</summary>
        public static bool Disabled;

        /// <summary>The bar standing in the town (null when the hub is on its own, or there's no hub).</summary>
        public static Transform Room { get; private set; }

        /// <summary>Its front door.</summary>
        public static KettleDoor Door { get; private set; }

        /// <summary>True while the hub stands in the town.</summary>
        public static bool Active => Room != null;

        /// <summary>True once the survivor has walked out and the raid is under way.</summary>
        public static bool RaidUnderway { get; private set; }

        private static System.Action s_RaidStarter;

        /// <summary>Whether <paramref name="hub"/> should stand in the town this time.</summary>
        public static bool Wants(HubDef hub)
            => hub != null && hub.Id == HubCatalog.Kettle && !GameSession.Networked && !Disabled
            && !string.IsNullOrEmpty(ZoneCatalog.Get(ZoneId).RealPlace);

        /// <summary>A line naming where a hub is and when, for the loading card.</summary>
        public static string Where(HubDef hub)
            => (hub != null && hub.Id == HubCatalog.Kettle ? "Bardstown, Kentucky  \u00b7  " : "") + LoadingScreen.NightOf();

        /// <summary>True when a world point is inside the bar's walls.</summary>
        public static bool Inside(Vector3 p) => HubWorld.Inside(Room, p);

        /// <summary>True when a world point lies in the safe zone round the bar standing in the town (its lot and
        /// <see cref="SafeMargin"/> round it), grown by <paramref name="reach"/>: no anomaly or other hazard of the Zone
        /// is ever put there. Always false while the bar isn't standing in the town.</summary>
        public static bool InSafeZone(Vector3 p, float reach = 0f)
        {
            KentuckyMap.Lot lot = KentuckyMap.ReservedLot;
            if (lot != null) return lot.Contains(new Vector2(p.x, p.z), SafeMargin + reach);
            // The bar stands in the town only on its lot; should it ever stand there without one, its walls still count.
            return Room != null && HubWorld.Inside(Room, p, SafeMargin + reach);
        }

        /// <summary>The ground (x, z) the safe zone covers, grown by <paramref name="reach"/>, when the town keeps a lot.</summary>
        public static bool SafeArea(float reach, out Rect area)
        {
            area = default;
            KentuckyMap.Lot lot = KentuckyMap.ReservedLot;
            if (lot == null) return false;
            float m = SafeMargin + reach, hw = lot.Size.HalfWidth + m;
            Vector2 lo = new Vector2(float.MaxValue, float.MaxValue), hi = -lo;
            foreach (Vector2 local in new[] { new Vector2(-hw, -lot.Size.Front - m), new Vector2(hw, -lot.Size.Front - m),
                new Vector2(hw, lot.Size.Back + m), new Vector2(-hw, lot.Size.Back + m) })
            {
                Vector2 w = lot.ToWorld(local);
                lo = Vector2.Min(lo, w);
                hi = Vector2.Max(hi, w);
            }
            area = Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
            return true;
        }

        /// <summary>Forgets the last lobby (a new world is being built).</summary>
        internal static void Reset()
        {
            Room = null;
            Door = null;
            RaidUnderway = false;
            s_RaidStarter = null;
            GameSession.HubInRaidTown = false;
            KentuckyMap.ForgetLot();   // a lot outlives the scene; only the map that asks for one keeps it
        }

        /// <summary>The lobby is up: the bar is <paramref name="room"/>, and <paramref name="raidStarter"/> starts the raid
        /// where the survivor stands.</summary>
        internal static void Opened(Transform room, System.Action raidStarter)
        {
            Room = room;
            Door = room != null ? room.GetComponentInChildren<KettleDoor>(true) : null;
            RaidUnderway = false;
            s_RaidStarter = raidStarter;
        }

        /// <summary>Opens the front door (the deploy screen's "Open the front door").</summary>
        public static void OpenDoor()
        {
            if (Door != null) Door.SetOpen(true, true);
        }

        /// <summary>The survivor has walked out of the door: the raid starts here.</summary>
        internal static void BeginRaid()
        {
            if (RaidUnderway || s_RaidStarter == null || !Active) return;
            RaidUnderway = true;
            s_RaidStarter();
        }

        /// <summary>The raid is under way: the door is barred behind the survivor, the counters close, and the stash goes
        /// back in its locker (it's no part of the Zone's loot).</summary>
        internal static void CloseUp()
        {
            if (Room == null) return;
            if (Door != null) Door.Bar();
            foreach (HubStation s in Room.GetComponentsInChildren<HubStation>(true)) s.enabled = false;
            foreach (HubStashLocker l in Room.GetComponentsInChildren<HubStashLocker>(true)) Object.Destroy(l.gameObject);
            foreach (HubNpcTalk t in Room.GetComponentsInChildren<HubNpcTalk>(true)) t.enabled = false;
        }

        /// <summary>The card over the street as the survivor steps out: the town, the hour and what's asked of them.</summary>
        public static void ShowArrival(string place, string line)
        {
            var go = new GameObject("ArrivalCard");
            go.AddComponent<ArrivalCard>().Show(place, line);
        }
    }

    /// <summary>
    /// The Kettle's front door in the town: two oak leaves with frosted glass, opening out onto the porch. While the hub
    /// is up, open it and walk out, and the raid begins where you stand (<see cref="KettleLobby.BeginRaid"/>); then it's
    /// barred behind you until you come back through an exit. "Other ways in" opens the deploy screen, for the runs that
    /// start somewhere else.
    /// </summary>
    public sealed class KettleDoor : Interactable
    {
        private const float SwingDegrees = 100f, SwingSeconds = 0.55f;

        public Transform Room, LeftHinge, RightHinge;

        /// <summary>True when the door stands open (or is swinging open).</summary>
        public bool IsOpen { get; private set; }

        /// <summary>True once the raid has begun: shut and barred from inside.</summary>
        public bool Barred { get; private set; }

        private float _swing;
        private bool _wasInside;
        private float _nextKnock;

        public override string DisplayName => Barred ? "The Kettle's door" : "Front door";
        public override Vector3 InteractPoint => transform.position + transform.rotation * new Vector3(0f, 1.2f, 0f);
        public override float Reach => 2.4f;
        public override Transform OcclusionRoot => transform;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (Barred)
            {
                into.Add(new InteractAction("Knock", c => Knock()));
                return;
            }
            if (!GameSession.IsHub) return;
            if (!IsOpen) into.Add(new InteractAction("Head out into Bardstown", c => SetOpen(true, true)));
            else into.Add(new InteractAction("Close the door", c => SetOpen(false, false)));
            into.Add(new InteractAction("Other ways in (scav run, open Zone, hunt, helicopter)", c =>
            {
                Sfx.PlayUI(SfxIds.UiClick);
                HubScreens.Open(HubScreens.Deploy);
            }));
            into.Add(new InteractAction("Look out through the glass", c => c.Notify(
                "Snow coming down on the street under the lamps. Somewhere out there, the dead are listening.")));
        }

        /// <summary>Opens or closes the door; <paramref name="warn"/> reminds the survivor what walking out means.</summary>
        public void SetOpen(bool open, bool warn)
        {
            if (Barred || IsOpen == open) return;
            IsOpen = open;
            Sfx.Play(SfxIds.DoorBash, transform.position + Vector3.up, 0.35f);
            if (open && warn)
                GameEvents.RaiseToast("The door's open. Step outside and the raid begins: everything you carry is at risk.");
        }

        /// <summary>Shuts the door behind the survivor and bars it (the raid has begun).</summary>
        public void Bar()
        {
            IsOpen = false;
            Barred = true;
        }

        private void Knock()
        {
            if (Time.time < _nextKnock) return;
            _nextKnock = Time.time + 4f;
            Sfx.Play(SfxIds.DoorBash, transform.position + Vector3.up, 0.5f);
            NoiseSystem.Emit(transform.position, 14f);
            GameEvents.RaiseSubtitle("Mott (through the door)", "We're shut till you come back through an exit, friend. Go on.");
        }

        private void Update()
        {
            float target = IsOpen ? 1f : 0f;
            if (!Mathf.Approximately(_swing, target))
            {
                _swing = Mathf.MoveTowards(_swing, target, Time.deltaTime / SwingSeconds);
                float a = Mathf.SmoothStep(0f, 1f, _swing) * SwingDegrees;
                if (LeftHinge != null) LeftHinge.localRotation = Quaternion.Euler(0f, a, 0f);
                if (RightHinge != null) RightHinge.localRotation = Quaternion.Euler(0f, -a, 0f);
            }

            // Walking out of the open door is what starts the raid.
            if (Barred || !GameSession.IsHub || Room == null) return;
            Actor me = Actor.LocalPlayer;
            if (me == null) return;
            Vector3 p = me.transform.position;
            if (HubWorld.Inside(Room, p, -0.1f)) _wasInside = true;
            else if (_wasInside && IsOpen && !HubWorld.Inside(Room, p, 0.25f)) KettleLobby.BeginRaid();
        }
    }

    /// <summary>
    /// Lifts the Kettle's roof (and its sign and the porch roof) away in the isometric view while the survivor is inside,
    /// as the map's buildings' roofs lift round the survivor, so the camera looks down into the bar. Hidden parts keep
    /// casting shadows, so the moon never lights the room from above.
    /// </summary>
    public sealed class KettleRoof : MonoBehaviour
    {
        public Transform Room, Parts;

        private Renderer[] _renderers;
        private bool _hidden;

        private void Start()
        {
            _renderers = Parts != null ? Parts.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        }

        private void LateUpdate()
        {
            Actor me = Actor.LocalPlayer;
            bool hide = ViewState.Current == CameraView.Isometric && me != null && HubWorld.Inside(Room, me.transform.position, 0.3f);
            if (hide == _hidden || _renderers == null) return;
            _hidden = hide;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        }
    }

    /// <summary>A title over the street as the survivor steps out: fades in, holds, fades away.</summary>
    public sealed class ArrivalCard : MonoBehaviour
    {
        public void Show(string place, string line)
        {
            Canvas canvas = UIKit.CreateCanvas("Arrival Canvas", 450, transform);
            var root = (RectTransform)canvas.transform;
            canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled = false;
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            UnityEngine.UI.Text title = UIKit.Label(root, place, 54, new Color(0.96f, 0.92f, 0.84f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 70f));
            UnityEngine.UI.Text sub = UIKit.Label(root, line, 22, new Color(0.78f, 0.80f, 0.84f), TextAnchor.MiddleCenter);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1600f, 34f));
            StartCoroutine(Run(group));
        }

        private IEnumerator Run(CanvasGroup group)
        {
            for (float t = 0f; t < 0.8f; t += Time.unscaledDeltaTime) { group.alpha = t / 0.8f; yield return null; }
            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(3.2f);
            for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / 1.2f; yield return null; }
            Destroy(gameObject);
        }
    }
}
