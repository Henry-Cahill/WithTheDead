using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Combat feedback for the views that look through the survivor's eyes or over their shoulder: a red arc around
    /// the screen centre pointing at whatever just hurt them (it keeps pointing as the view turns, then fades), and
    /// hit markers at the crosshair — a quick white tick when a round or a blow lands on someone, a bigger red cross
    /// for a kill, amber for a headshot kill. The isometric view sees everything already and skips both.
    /// </summary>
    public sealed class HudCombatFeedback
    {
        private const int ArcCount = 4;
        private const float ArcLife = 1.6f, ArcFade = 0.7f, HitLife = 0.22f, KillLife = 0.55f, MeleeReach = 4f;
        private static readonly Color ArcColor = new Color(0.92f, 0.08f, 0.06f, 1f);
        private static readonly Color HitColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color KillColor = new Color(1f, 0.2f, 0.16f, 1f);
        private static readonly Color HeadColor = new Color(1f, 0.72f, 0.2f, 1f);

        private readonly HudContext _ctx;
        private readonly RectTransform[] _arcRoots = new RectTransform[ArcCount];
        private readonly Image[] _arcs = new Image[ArcCount];
        private readonly Vector3[] _arcFrom = new Vector3[ArcCount];
        private readonly float[] _arcLife = new float[ArcCount];
        private readonly GameObject _marker;
        private readonly RectTransform[] _tickRects = new RectTransform[4];
        private readonly Image[] _ticks = new Image[4];
        private float _markerLife, _markerSpan = 1f;
        private bool _kill, _headshot;
        private PlayerCombat _combat;
        private Camera _cam;

        /// <summary>Damage arcs show (the mouse-look views).</summary>
        public bool Arcs;

        /// <summary>Hit markers show (only with a crosshair: the shoulder and first-person views).</summary>
        public bool Markers;

        public HudCombatFeedback(HudContext ctx, RectTransform root)
        {
            _ctx = ctx;
            var centre = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < ArcCount; i++)
            {
                RectTransform rt = HudUI.Rect(root, "DamageArc", centre, centre, Vector2.zero, new Vector2(460f, 460f));
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = HudSprites.DamageArc;
                img.color = HudUI.WithAlpha(ArcColor, 0f);
                img.raycastTarget = false;
                img.enabled = false;
                _arcRoots[i] = rt;
                _arcs[i] = img;
            }

            RectTransform marker = HudUI.Rect(root, "HitMarker", centre, centre, Vector2.zero, new Vector2(48f, 48f));
            for (int i = 0; i < 4; i++)
            {
                RectTransform t = HudUI.Rect(marker, "Tick", centre, centre, Vector2.zero, new Vector2(12f, 2.5f));
                t.localRotation = Quaternion.Euler(0f, 0f, 45f + 90f * i);
                var img = t.gameObject.AddComponent<Image>();
                img.sprite = HudIcons.Get(HudIcon.White);
                img.raycastTarget = false;
                var outline = t.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
                outline.effectDistance = new Vector2(1f, -1f);
                _tickRects[i] = t;
                _ticks[i] = img;
            }
            _marker = marker.gameObject;
            _marker.SetActive(false);

            CombatResolver.ShotResolved += OnShot;
            GameEvents.ActorKilled += OnKill;
        }

        /// <summary>Stops listening (the HUD is going away).</summary>
        public void Dispose()
        {
            CombatResolver.ShotResolved -= OnShot;
            GameEvents.ActorKilled -= OnKill;
            BindCombat(null);
        }

        /// <summary>Listens to the survivor's melee blows (rounds arrive through <see cref="CombatResolver"/>).</summary>
        public void BindCombat(PlayerCombat combat)
        {
            if (_combat != null) _combat.OnMeleeHit -= OnMelee;
            _combat = combat;
            if (_combat != null) _combat.OnMeleeHit += OnMelee;
        }

        #region Events
        /// <summary>The survivor was just hurt: point an arc at the source (the blow's own, else the nearest threat in reach).</summary>
        public void OnDamaged()
        {
            PlayerStats s = _ctx.Stats;
            if (s == null || s.IsDead) return;
            Vector3 me = s.transform.position;
            Vector3 from;
            if (s.LastHitTime >= 0f && Time.time - s.LastHitTime < 0.05f) from = s.LastHitFrom;
            else if (!NearestThreat(me, out from)) return;

            // Refresh an arc already pointing that way, else take a free one, else the oldest.
            Vector3 dir = Flat(from - me);
            int slot = -1;
            float oldest = float.MaxValue;
            for (int i = 0; i < ArcCount; i++)
            {
                if (_arcLife[i] > 0f && Vector3.Angle(dir, Flat(_arcFrom[i] - me)) < 22f) { slot = i; break; }
                if (_arcLife[i] < oldest) { oldest = _arcLife[i]; slot = i; }
            }
            _arcFrom[slot] = from;
            _arcLife[slot] = ArcLife;
        }

        private static bool NearestThreat(Vector3 pos, out Vector3 at)
        {
            at = pos;
            Actor me = Actor.LocalPlayer;
            float best = MeleeReach * MeleeReach;
            bool found = false;
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a == null || a == me || !a.IsAlive) continue;
                if (me != null && !a.IsHostileTo(me)) continue;
                float d = (a.transform.position - pos).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                at = a.transform.position;
                found = true;
            }
            return found;
        }

        private void OnShot(ShotRequest req, ShotResult r)
        {
            Actor me = Actor.LocalPlayer;
            if (me == null || r.TargetActor == null || r.TargetActor == me || req.ShooterActor != me) return;
            Hit(r.Killed, r.Zone == BodyZone.Head);
        }

        private void OnKill(KillInfo k)
        {
            Actor me = Actor.LocalPlayer;
            if (me != null && k.Killer == me && k.Victim != null && k.Victim != me) Hit(true, k.Zone == BodyZone.Head);
        }

        private void OnMelee(IDamageable target, bool killed)
        {
            Transform t = target != null ? target.Transform : null;
            if (!killed && t != null && t.GetComponent<Actor>() != null) Hit(false, false);
        }

        private void Hit(bool kill, bool head)
        {
            // Always update the marker state to reflect the most recent hit.
            // Previously, non‑kill hits would preserve a stale kill/headshot visual if the prior marker was still fading.
            // By assigning the flags unconditionally we ensure a normal hit shows the correct hit visual.
            _kill = kill;
            _headshot = head;
            _markerSpan = _kill ? KillLife : HitLife;
            _markerLife = _markerSpan;
        }
        #endregion

        public void Tick(float unscaledDt)
        {
            UpdateArcs(unscaledDt);
            UpdateMarker(unscaledDt);
        }

        private void UpdateArcs(float dt)
        {
            PlayerStats s = _ctx.Stats;
            if (_cam == null) _cam = Camera.main;
            bool can = Arcs && s != null && _cam != null;
            Vector3 fwd = can ? Flat(_cam.transform.forward) : Vector3.forward;
            Vector3 me = s != null ? s.transform.position : Vector3.zero;
            for (int i = 0; i < ArcCount; i++)
            {
                if (_arcLife[i] > 0f) _arcLife[i] -= dt;
                bool on = can && _arcLife[i] > 0f;
                Image img = _arcs[i];
                if (img.enabled != on) img.enabled = on;
                if (!on) continue;
                float angle = Vector3.SignedAngle(fwd, Flat(_arcFrom[i] - me), Vector3.up);
                _arcRoots[i].localRotation = Quaternion.Euler(0f, 0f, -angle);
                img.color = HudUI.WithAlpha(ArcColor, 0.9f * Mathf.Clamp01(_arcLife[i] / ArcFade));
            }
        }

        private void UpdateMarker(float dt)
        {
            if (_markerLife > 0f) _markerLife -= dt;
            bool on = Markers && _markerLife > 0f;
            HudUI.Show(_marker, on);
            if (!on) return;

            float k = 1f - Mathf.Clamp01(_markerLife / _markerSpan);   // 0 at the hit .. 1 as it vanishes
            float pop = 1f - (1f - k) * (1f - k);
            float dist = _kill ? 9f + 8f * pop : 8f + 4f * pop;
            Vector2 size = _kill ? new Vector2(15f, 3f) : new Vector2(11f, 2.5f);
            Color c = _kill ? (_headshot ? HeadColor : KillColor) : HitColor;
            c.a = Mathf.Clamp01(_markerLife / (_markerSpan * 0.6f));
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + 90f * i) * Mathf.Deg2Rad;
                RectTransform t = _tickRects[i];
                t.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * dist;
                t.sizeDelta = size;
                _ticks[i].color = c;
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
