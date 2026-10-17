using UnityEngine;

namespace CloneZ
{
    /// <summary>Whole-body poses: the gait cycle, crouching, posture, crawling, lying and death.
    /// Angles are degrees about each bone's bind axes: +X pitch swings a hanging limb backward
    /// (so a leg or arm swings forward with a negative pitch, and a knee bends with a positive one),
    /// and +Z rolls a hanging limb toward +X.</summary>
    public sealed partial class HumanoidAnimator
    {
        private void PoseUpright()
        {
            HumanoidGait gait = Gait;
            bool zombie = gait != HumanoidGait.Survivor;
            float m = _move;
            float run = gait == HumanoidGait.Survivor ? Mathf.InverseLerp(2.6f, 5.0f, _speed)
                      : gait == HumanoidGait.Runner ? Mathf.InverseLerp(2.4f, 5.0f, _speed) : 0f;

            float amp, knee, stance, armAmp, elbow, lean;
            switch (gait)
            {
                case HumanoidGait.Shambler:
                    amp = 15f; knee = 28f; stance = 12f; armAmp = 6f; elbow = 10f; lean = 10f;
                    break;
                case HumanoidGait.Lurcher:
                    amp = 24f; knee = 50f; stance = 14f; armAmp = 10f; elbow = 14f; lean = 14f;
                    break;
                case HumanoidGait.Runner:
                    amp = Mathf.Lerp(28f, 46f, run); knee = Mathf.Lerp(60f, 105f, run); stance = 16f;
                    armAmp = Mathf.Lerp(25f, 48f, run); elbow = Mathf.Lerp(30f, 80f, run); lean = Mathf.Lerp(10f, 22f, run);
                    break;
                default:
                    amp = Mathf.Lerp(24f, 44f, run); knee = Mathf.Lerp(58f, 100f, run); stance = Mathf.Lerp(5f, 16f, run);
                    armAmp = Mathf.Lerp(20f, 46f, run); elbow = Mathf.Lerp(12f, 85f, run); lean = Mathf.Lerp(2f, 12f, run);
                    break;
            }
            if (Sneaking) { amp *= 0.8f; knee *= 0.85f; armAmp *= 0.35f; }

            // The pelvis turns partway toward the direction of travel and the legs swing along it, so the
            // survivor can strafe and back-pedal while the chest keeps facing the aim.
            float travel = Mathf.Atan2(_moveLocal.x, _moveLocal.z) * Mathf.Rad2Deg;
            if (_moveLocal.z < -0.25f) travel = Mathf.DeltaAngle(0f, travel + 180f);
            travel *= m;
            float pelvisYaw = Mathf.Clamp(travel * 0.5f, -35f, 35f);
            float legYaw = Mathf.Clamp(travel - pelvisYaw, -60f, 60f);

            float ph = _phase;
            float limp = Mathf.Clamp01(Limp);
            Leg(ThighL, ph + limp * 0.35f * Mathf.Sin(ph), amp * m * (1f - limp * 0.35f), knee * m * (1f - limp * 0.7f), stance, legYaw, -1f);
            Leg(ThighR, ph + Mathf.PI, amp * m, knee * m, stance, legYaw, 1f);

            float idle = 1f - m;
            float breathe = Mathf.Sin(_time * 2f * Mathf.PI / 3.6f + _seed);
            float twist = 7f * m * Mathf.Sin(ph) * (zombie ? 0.6f : 1f);
            float sway = (2.5f + limp * 5f) * m * Mathf.Sin(ph);
            float shift = idle * Mathf.Sin(_time * 0.55f + _seed);
            _q[Hips] = E(0f, pelvisYaw + twist, sway * 0.5f + shift * 1.2f);
            _hips.x = shift * 0.012f;

            float hunch = _hunchW;
            float spinePitch = lean * m * 0.5f + _crouch * 16f + hunch * 12f;
            float chestPitch = lean * m * 0.5f + _crouch * 10f + hunch * 14f + breathe * 1.2f;
            float upperYaw = -(pelvisYaw + twist);

            ActionTorso(out float actYaw, out float actPitch, out float actNeck);
            float stag = _stagger > 0f ? Mathf.Sin(Mathf.Clamp01(_stagger / _staggerDur) * Mathf.PI) : 0f;
            float flinch = Mathf.Sin(_flinch * Mathf.PI);
            float recoilKick = _rig.HeldStyle == HoldStyle.Firearm ? _recoil * 3f : 0f;
            spinePitch += actPitch * 0.5f - stag * 12f - flinch * 5f;
            chestPitch += actPitch * 0.5f - stag * 10f - flinch * 6f - recoilKick;

            _q[Spine] = E(spinePitch, upperYaw * 0.5f + actYaw * 0.4f, -sway * 0.3f);
            _q[Chest] = E(chestPitch, upperYaw * 0.5f + actYaw * 0.6f, -sway * 0.3f);

            // The head stays level with the horizon; the dead let it loll to one side.
            float roll = zombie ? 12f * Mathf.Sin(_seed) + 4f * Mathf.Sin(_time * 0.7f + _seed) : 0f;
            float level = -(spinePitch + chestPitch);
            float glance = zombie ? 0f : idle * 6f * Mathf.Sin(_time * 0.31f + _seed);
            _q[Neck] = E(level * 0.4f + hunch * 8f + actNeck - flinch * 8f, -actYaw * 0.4f, roll * 0.4f);
            _q[HeadB] = E(level * 0.45f - hunch * 4f - flinch * 10f, glance - actYaw * 0.4f, roll * 0.6f);

            ArmsFK(ph, armAmp * m, elbow, run, zombie, stag, flinch);
        }

        private void Leg(int thigh, float phase, float amp, float kneeAmp, float stance, float legYaw, float sx)
        {
            float s = Mathf.Sin(phase);
            float swing = Mathf.Max(0f, Mathf.Cos(phase + 0.7f));    // the leg is swinging through
            float pitch = -amp * s - _crouch * 52f;
            float knee = 3f + stance * _move + kneeAmp * Mathf.Pow(swing, 1.6f) + _crouch * 100f;
            // Keep the sole flat on the ground, rolling off the toes after push-off and landing heel first.
            float foot = -(pitch + knee) + 22f * _move * swing * swing * Mathf.Clamp01(amp) - 8f * _move * Mathf.Pow(Mathf.Max(0f, s), 3f);

            Quaternion q = E(pitch, 0f, sx * 1f);
            if (Mathf.Abs(legYaw) > 0.1f)
                q = Quaternion.AngleAxis(legYaw, Vector3.up) * q * Quaternion.AngleAxis(-legYaw, Vector3.up);
            _q[thigh] = q;
            _q[thigh + 1] = E(knee, 0f, 0f);
            _q[thigh + 2] = E(foot, sx * 4f, 0f);
        }

        private void ArmsFK(float ph, float amp, float elbow, float run, bool zombie, float stagger, float flinch)
        {
            float s = Mathf.Sin(ph);
            float idle = 1f - _move;
            float breathe = Mathf.Sin(_time * 2f * Mathf.PI / 3.6f + _seed);
            bool oneHanded = _rig.HeldStyle == HoldStyle.Melee || _rig.HeldStyle == HoldStyle.Carry;

            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                int ua = side == 0 ? UArmL : UArmR;
                bool holding = side == 1 && oneHanded;
                float a = holding ? amp * 0.45f : amp;

                // Arms swing against the legs: the left arm goes back as the left leg comes forward.
                float swing = (side == 0 ? 1f : -1f) * a * s;
                float forward = Mathf.Max(0f, -swing) / Mathf.Max(1f, a);
                float pitch = swing - _crouch * 18f - (Sneaking ? 14f : 0f) - (holding ? 8f : 0f);
                float roll = sx * (7f + 6f * run + breathe * idle + stagger * 40f + flinch * 10f);
                float bend = elbow * (0.6f + 0.4f * forward) + 6f + _crouch * 25f + (Sneaking ? 25f : 0f) + (holding ? 14f : 0f);

                if (stagger > 0f) pitch -= stagger * (30f + 20f * Mathf.Sin(_time * 9f + side));
                _q[ua] = E(pitch, 0f, roll);
                _q[ua + 1] = E(-bend, 0f, 0f);
                _q[ua + 2] = E(holding ? -8f : 0f, 0f, sx * -4f);

                if (zombie && _armsOutW > 0.001f)
                {
                    // The classic reach: arms up and out toward prey, wrists limp, never quite still.
                    float t = _time * 1.3f + side * 1.7f + _seed;
                    Quaternion up = E(-80f + 6f * Mathf.Sin(t) + a * 0.3f * s, 0f, sx * (9f + 4f * Mathf.Sin(t * 0.7f)));
                    _q[ua] = Quaternion.Slerp(_q[ua], up, _armsOutW);
                    _q[ua + 1] = Quaternion.Slerp(_q[ua + 1], E(-12f - 6f * Mathf.Sin(t * 1.3f), 0f, 0f), _armsOutW);
                    _q[ua + 2] = Quaternion.Slerp(_q[ua + 2], E(28f, 0f, 0f), _armsOutW);
                }
            }
        }

        /// <summary>Twist and lean of the upper body for the current one-shot action.</summary>
        private void ActionTorso(out float yaw, out float pitch, out float neck)
        {
            yaw = pitch = neck = 0f;
            if (_act == Act.None) return;
            float t = Mathf.Clamp01(_actT / Mathf.Max(0.01f, _actDur));
            switch (_act)
            {
                case Act.Swing:
                    yaw = KeyLerp(SwingT, _actTwoHanded ? Swing2Yaw : Swing1Yaw, t);
                    pitch = KeyLerp(SwingT, _actTwoHanded ? Swing2Pitch : Swing1Pitch, t);
                    break;
                case Act.Punch:
                    yaw = -22f * Bell(t, 0f, 0.8f);
                    pitch = 6f * Bell(t, 0f, 0.8f);
                    break;
                case Act.Shove:
                    pitch = 12f * Bell(t, 0f, 0.9f);
                    break;
                case Act.Lunge:
                {
                    float w = Mathf.Clamp01(_actT / Mathf.Max(0.01f, _actWindup));
                    float strike = Bell(_actT - _actWindup, 0f, 0.3f);
                    float recover = 1f - Mathf.Clamp01((_actT - _actWindup - 0.15f) / 0.25f);
                    pitch = (16f * w + 14f * strike) * recover;
                    neck = 7f * strike;
                    break;
                }
                case Act.Bash:
                {
                    float w = Mathf.Clamp01(_actT / Mathf.Max(0.01f, _actWindup));
                    float slam = Bell(_actT - _actWindup, 0f, 0.35f);
                    pitch = _actT < _actWindup ? -8f * w : 25f * slam;
                    break;
                }
            }
        }

        private static readonly float[] SwingT = { 0f, 0.32f, 0.56f, 0.8f, 1f };
        private static readonly float[] Swing1Yaw = { 0f, 28f, -22f, -32f, 0f };
        private static readonly float[] Swing1Pitch = { 0f, -4f, 10f, 8f, 0f };
        private static readonly float[] Swing2Yaw = { 0f, 40f, -15f, -48f, 0f };
        private static readonly float[] Swing2Pitch = { 0f, -2f, 6f, 4f, 0f };

        // ── Crawling, lying and death ────────────────────────────────────────────────────────────────

        /// <summary>Dragging along on the belly, legs trailing. The root lies face-down, so "forward" for
        /// the arms is up past the head; the hands themselves are planted with IK in CrawlArms.</summary>
        private void PoseCrawl()
        {
            for (int i = 0; i < BoneCount; i++) _q2[i] = Quaternion.identity;
            _hips2 = Vector3.zero;
            float ph = _phase;
            float m = Mathf.Max(_move, 0.2f);

            _q2[Spine] = E(0f, 4f * Mathf.Sin(ph) * m, 0f);
            _q2[Chest] = E(-4f, 6f * Mathf.Sin(ph) * m, 0f);
            _q2[Neck] = E(-40f, 0f, 0f);
            _q2[HeadB] = E(-32f, 0f, 6f * Mathf.Sin(_seed));

            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                float a = ph + side * Mathf.PI;
                float reach = 0.5f + 0.5f * Mathf.Cos(a);
                int ua = side == 0 ? UArmL : UArmR;
                _q2[ua] = E(Mathf.Lerp(-60f, -150f, reach), 0f, sx * 25f);
                _q2[ua + 1] = E(-Mathf.Lerp(80f, 15f, reach), 0f, 0f);
                _q2[ua + 2] = E(-20f, 0f, 0f);

                int th = side == 0 ? ThighL : ThighR;
                float drag = Mathf.Sin(a + 1f);
                _q2[th] = E(-5f + 4f * drag * m, 0f, sx * 6f);        // knees down on the ground
                _q2[th + 1] = E(6f + 14f * Mathf.Max(0f, drag) * m, 0f, 0f);
                _q2[th + 2] = E(70f, 0f, 0f);                          // toes trailing behind
            }
        }

        /// <summary>Asleep on the back: legs out, arms at the sides, head turned on the pillow.</summary>
        private void PoseLying()
        {
            for (int i = 0; i < BoneCount; i++) _q2[i] = Quaternion.identity;
            _hips2 = Vector3.zero;
            float breathe = Mathf.Sin(_time * 2f * Mathf.PI / 5f + _seed);
            _q2[Chest] = E(breathe * 1.5f, 0f, 0f);
            _q2[Neck] = E(10f, 0f, 0f);
            _q2[HeadB] = E(6f, 18f, 0f);
            _q2[UArmL] = E(4f, 0f, -12f); _q2[FArmL] = E(-20f, 0f, 0f);
            _q2[UArmR] = E(4f, 0f, 12f); _q2[FArmR] = E(-25f, 0f, 0f);
            _q2[ThighL] = E(-3f, 0f, -3f); _q2[ShinL] = E(4f, 0f, 0f); _q2[FootL] = E(30f, -12f, 0f);
            _q2[ThighR] = E(-8f, 0f, 4f); _q2[ShinR] = E(10f, 0f, 0f); _q2[FootR] = E(28f, 14f, 0f);
        }

        /// <summary>Sprawled where the body fell. Arms are flung out sideways (which keeps them on the
        /// floor either way up); knees and feet bend only the way that doesn't push them through it —
        /// a raised knee face up, a lifted heel and pointed toes face down.</summary>
        private void PoseDead()
        {
            for (int i = 0; i < BoneCount; i++) _q2[i] = Quaternion.identity;
            _hips2 = Vector3.zero;
            if (_fallBackward)
            {
                _q2[Spine] = E(3f, 4f, 0f);
                _q2[Chest] = E(3f, 6f, 0f);
                _q2[Neck] = E(-6f, 15f, 0f);
                _q2[HeadB] = E(-4f, 25f, 8f);
                _q2[UArmL] = E(6f, 0f, -70f); _q2[FArmL] = E(-15f, 0f, 0f);
                _q2[UArmR] = E(6f, 0f, 50f); _q2[FArmR] = E(-25f, 0f, 0f);
                _q2[ThighL] = E(4f, 0f, -8f); _q2[ShinL] = E(4f, 0f, 0f); _q2[FootL] = E(30f, -25f, 0f);
                _q2[ThighR] = E(-25f, 0f, 10f); _q2[ShinR] = E(45f, 0f, 0f); _q2[FootR] = E(20f, 20f, 0f);
            }
            else
            {
                _q2[Spine] = E(-3f, 5f, 0f);
                _q2[Chest] = E(-4f, 8f, 0f);
                _q2[Neck] = E(-15f, 55f, 0f);
                _q2[HeadB] = E(-8f, 30f, 0f);
                _q2[UArmL] = E(-8f, 0f, -60f); _q2[FArmL] = E(-5f, 0f, 0f);
                _q2[UArmR] = E(-8f, 0f, 75f); _q2[FArmR] = E(-10f, 0f, 0f);
                _q2[ThighL] = E(-5f, 0f, -6f); _q2[ShinL] = E(8f, 0f, 0f); _q2[FootL] = E(75f, -10f, 0f);
                _q2[ThighR] = E(-5f, 0f, 10f); _q2[ShinR] = E(25f, 0f, 0f); _q2[FootR] = E(70f, 10f, 0f);
            }
        }

        private static float KeyLerp(float[] ts, float[] vs, float t)
        {
            if (t <= ts[0]) return vs[0];
            for (int i = 0; i < ts.Length - 1; i++)
                if (t <= ts[i + 1]) return Mathf.Lerp(vs[i], vs[i + 1], Mathf.SmoothStep(0f, 1f, (t - ts[i]) / (ts[i + 1] - ts[i])));
            return vs[vs.Length - 1];
        }

        private static Vector3 KeyLerp(float[] ts, Vector3[] vs, float t)
        {
            if (t <= ts[0]) return vs[0];
            for (int i = 0; i < ts.Length - 1; i++)
                if (t <= ts[i + 1]) return Vector3.Lerp(vs[i], vs[i + 1], Mathf.SmoothStep(0f, 1f, (t - ts[i]) / (ts[i + 1] - ts[i])));
            return vs[vs.Length - 1];
        }
    }
}
