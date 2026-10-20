using UnityEngine;

namespace CloneZ
{
    /// <summary>Hands and head: two-bone IK for weapon holds, swings, shoves, lunges and jobs (working a
    /// fixture, reading), the hands in the lap when seated or behind the head when resting in bed, and
    /// the head turning toward a target. Hand targets are offsets from the chest in the character's
    /// facing frame (x right, y up, z forward), so they follow a crouch or a lean.</summary>
    public sealed partial class HumanoidAnimator
    {
        private struct HandGoal
        {
            public bool On;
            public Vector3 Pos;       // grip centre, world space
            public Quaternion Rot;    // hand rotation, world space
            public float Weight;
        }

        private static readonly Vector3[] Swing1Pos =
        {
            new Vector3(0.20f, -0.30f, 0.15f), new Vector3(0.26f, 0.26f, -0.04f), new Vector3(0.02f, 0.0f, 0.52f),
            new Vector3(-0.18f, -0.20f, 0.34f), new Vector3(0.20f, -0.30f, 0.15f),
        };
        private static readonly Vector3[] Swing1Dir =
        {
            new Vector3(0f, -0.3f, 1f), new Vector3(-0.15f, 0.85f, -0.5f), new Vector3(-0.25f, -0.35f, 1f),
            new Vector3(-0.85f, -0.45f, 0.3f), new Vector3(0f, -0.3f, 1f),
        };
        private static readonly Vector3 PortPos = new Vector3(0.12f, -0.24f, 0.24f);
        private static readonly Vector3 PortDir = new Vector3(-0.5f, 0.7f, 0.45f);
        private static readonly Vector3[] Swing2Pos =
        {
            PortPos, new Vector3(0.26f, 0.10f, -0.02f), new Vector3(0.04f, -0.06f, 0.42f), new Vector3(-0.24f, -0.06f, 0.24f), PortPos,
        };
        private static readonly Vector3[] Swing2Dir =
        {
            PortDir, new Vector3(0.3f, 0.55f, -0.8f), new Vector3(-0.9f, 0.05f, 0.42f), new Vector3(-0.45f, 0.15f, -0.85f), PortDir,
        };

        private Quaternion _frame;
        private Vector3 _chest;
        private float _scale = 1f;

        private Vector3 At(Vector3 offset) => _chest + _frame * (offset * _scale);
        private Vector3 Dir(Vector3 d) => _frame * d.normalized;

        private void Arms(float weight)
        {
            _frame = _root.rotation;
            _chest = _b[Chest].position;
            _scale = _root.lossyScale.y;

            var right = new HandGoal();
            var left = new HandGoal();
            bool leftOnItem = false;

            HoldStyle hold = _rig.HeldStyle;
            bool twoHand = hold == HoldStyle.Melee && _rig.HeldDef != null && _rig.HeldDef.TwoHanded;

            // Standing holds come first; actions and jobs blend over them.
            if (hold == HoldStyle.Firearm) GunHold(ref right, ref left, out leftOnItem);
            else if (twoHand)
            {
                right = Goal(At(PortPos), MeleeRot(Dir(PortDir)), 1f);
                leftOnItem = true;
            }

            if (_act != Act.None) ActionGoals(ref right, ref left, ref leftOnItem, twoHand);
            if (_activityW > 0.001f) ActivityGoals(ref right, ref left, ref leftOnItem);

            // Sitting, a hand nothing else wants rests on its thigh.
            if (_sitW > 0.001f && !_dead)
            {
                float sw = Mathf.SmoothStep(0f, 1f, _sitW);
                if (!right.On) right = Goal(LapPoint(ThighR, 1f), LapRot(ThighR, true), sw);
                if (!left.On && !leftOnItem) left = Goal(LapPoint(ThighL, -1f), LapRot(ThighL, false), sw);
            }

            if (right.On) SolveArm(UArmR, 1f, right.Pos, right.Rot, right.Weight * weight);
            if (leftOnItem && _rig.Held != null)
            {
                Vector3 support = _rig.GripR.TransformPoint(_rig.HeldSupport);
                Quaternion rot = _b[HandR].rotation;
                SolveArm(UArmL, -1f, support, rot, weight * (right.On ? right.Weight : 1f));
            }
            else if (left.On) SolveArm(UArmL, -1f, left.Pos, left.Rot, left.Weight * weight);
        }

        private static HandGoal Goal(Vector3 pos, Quaternion rot, float weight) =>
            new HandGoal { On = true, Pos = pos, Rot = rot, Weight = weight };

        /// <summary>A resting hand's place on top of a thigh, two-thirds of the way to the knee.</summary>
        private Vector3 LapPoint(int thigh, float sx)
        {
            Vector3 hip = _b[thigh].position, knee = _b[thigh + 1].position;
            return Vector3.Lerp(hip, knee, 0.66f) + _frame * new Vector3(-sx * 0.012f, _bound.Thigh + 0.03f, 0f) * _scale;
        }

        /// <summary>A hand laid flat on a thigh, the fingers toward the knee.</summary>
        private Quaternion LapRot(int thigh, bool rightHand) =>
            HandRot(_b[thigh + 1].position - _b[thigh].position, _frame * Vector3.down, rightHand);

        /// <summary>Hand rotation that points a held melee weapon along <paramref name="dir"/>.</summary>
        private Quaternion MeleeRot(Vector3 dir)
        {
            Vector3 hint = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.8f ? _frame * Vector3.back : Vector3.up;
            return Quaternion.LookRotation(dir, hint);
        }

        /// <summary>Hand rotation that points a gun's barrel along <paramref name="barrel"/>, top up.</summary>
        private Quaternion GunRot(Vector3 barrel)
        {
            Vector3 top = Vector3.Cross(barrel, _frame * Vector3.right);
            if (top.sqrMagnitude < 1e-6f) top = Vector3.up;
            return Quaternion.LookRotation(top.normalized, -barrel);
        }

        /// <summary>Hand rotation from where the fingers point and which way the palm faces.</summary>
        private static Quaternion HandRot(Vector3 fingers, Vector3 palm, bool rightHand)
        {
            Vector3 y = -fingers.normalized;
            Vector3 x = Vector3.ProjectOnPlane(rightHand ? -palm : palm, y).normalized;
            if (x.sqrMagnitude < 1e-6f) x = Vector3.Cross(y, Vector3.forward).normalized;
            return Quaternion.LookRotation(Vector3.Cross(x, y), y);
        }

        private void GunHold(ref HandGoal right, ref HandGoal left, out bool leftOnItem)
        {
            leftOnItem = true;
            bool pistol = _rig.HeldLength < 0.42f;
            float aim = _aimW;
            float recoil = _recoil;

            Vector3 barrel = Vector3.Slerp(Dir(new Vector3(0f, pistol ? -0.55f : -0.5f, 1f)), Dir(Vector3.forward), aim);
            if (Reloading) barrel = Dir(pistol ? new Vector3(-0.3f, 0.45f, 1f) : new Vector3(-0.3f, 0.2f, 1f));
            barrel = Quaternion.AngleAxis(-12f * recoil, _frame * Vector3.right) * barrel;
            Quaternion rot = GunRot(barrel);

            Vector3 pos;
            if (pistol)
            {
                pos = Vector3.Lerp(At(new Vector3(0.07f, -0.10f, 0.34f)), At(new Vector3(0.05f, 0.16f, 0.47f)), aim);
                if (Reloading) pos = At(new Vector3(0.03f, -0.02f, 0.30f));
            }
            else
            {
                // Long guns: seat the butt in the shoulder and let the grip fall where the gun puts it.
                Vector3 pocket = Vector3.Lerp(At(new Vector3(0.13f, 0.06f, 0.06f)), At(new Vector3(0.12f, 0.17f, 0.05f)), aim);
                if (Reloading) pocket = At(new Vector3(0.10f, 0.0f, 0.10f));
                pos = pocket - rot * (_rig.HeldButt * _scale);
            }
            pos -= barrel * (0.03f * recoil * _scale);
            right = Goal(pos, rot, 1f);

            if (Reloading)
            {
                // The off hand goes to the belt for a fresh magazine and back again.
                leftOnItem = false;
                float cycle = 0.5f - 0.5f * Mathf.Cos(_time * Mathf.PI * 2f);
                Vector3 support = pos + rot * (_rig.HeldSupport * _scale);
                Vector3 belt = At(new Vector3(-0.12f, -0.30f, 0.06f));
                left = Goal(Vector3.Lerp(support, belt, cycle), rot, 1f);
            }
        }

        private void ActionGoals(ref HandGoal right, ref HandGoal left, ref bool leftOnItem, bool twoHand)
        {
            float t = Mathf.Clamp01(_actT / Mathf.Max(0.01f, _actDur));
            float env = Mathf.SmoothStep(0f, 1f, t / 0.12f) * (1f - Mathf.SmoothStep(0f, 1f, (t - 0.82f) / 0.18f));
            switch (_act)
            {
                case Act.Swing:
                    if (twoHand)
                    {
                        right = Goal(At(KeyLerp(SwingT, Swing2Pos, t)), MeleeRot(Dir(KeyLerp(SwingT, Swing2Dir, t))), 1f);
                        leftOnItem = true;
                    }
                    else right = Goal(At(KeyLerp(SwingT, Swing1Pos, t)), MeleeRot(Dir(KeyLerp(SwingT, Swing1Dir, t))), env);
                    break;

                case Act.Punch:
                {
                    float jab = Bell(t, 0f, 0.75f);
                    Quaternion fist = HandRot(Dir(Vector3.forward), Dir(Vector3.down), true);
                    right = Goal(At(Vector3.Lerp(new Vector3(0.12f, 0.05f, 0.22f), new Vector3(0.03f, 0.18f, 0.6f), jab)), fist, env);
                    left = Goal(At(new Vector3(-0.12f, 0.08f, 0.2f)), HandRot(Dir(Vector3.forward), Dir(Vector3.right), false), env);
                    break;
                }

                case Act.Shove:
                {
                    float push = Bell(t, 0f, 0.9f);
                    leftOnItem = false;
                    right = Goal(At(Vector3.Lerp(new Vector3(0.14f, 0.02f, 0.22f), new Vector3(0.17f, 0.06f, 0.56f), push)),
                        HandRot(Dir(Vector3.up), Dir(Vector3.forward), true), env);
                    left = Goal(At(Vector3.Lerp(new Vector3(-0.14f, 0.02f, 0.22f), new Vector3(-0.17f, 0.06f, 0.56f), push)),
                        HandRot(Dir(Vector3.up), Dir(Vector3.forward), false), env);
                    break;
                }

                case Act.Lunge:
                {
                    float reach = Mathf.Clamp01(_actT / Mathf.Max(0.01f, _actWindup));
                    float grab = Bell(_actT - _actWindup, 0f, 0.3f);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 p = new Vector3(side * Mathf.Lerp(0.16f, 0.07f, grab), 0.06f, 0.42f + 0.12f * reach + 0.08f * grab);
                        HandGoal g = Goal(At(p), HandRot(Dir(Vector3.forward), Dir(new Vector3(-side * 0.4f, -1f, 0f)), side > 0), env);
                        if (side > 0) right = g; else left = g;
                    }
                    break;
                }

                case Act.Bash:
                {
                    float rise = Mathf.Clamp01(_actT / Mathf.Max(0.01f, _actWindup));
                    float slam = Mathf.Clamp01((_actT - _actWindup) / 0.12f);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 up = new Vector3(side * 0.12f, 0.42f, 0.22f), down = new Vector3(side * 0.12f, -0.08f, 0.5f);
                        Vector3 p = _actT < _actWindup ? Vector3.Lerp(new Vector3(side * 0.15f, 0f, 0.35f), up, rise) : Vector3.Lerp(up, down, slam);
                        HandGoal g = Goal(At(p), HandRot(Dir(new Vector3(0f, 0.3f, 1f)), Dir(Vector3.down), side > 0), env);
                        if (side > 0) right = g; else left = g;
                    }
                    break;
                }
            }
        }

        private void ActivityGoals(ref HandGoal right, ref HandGoal left, ref bool leftOnItem)
        {
            float w = Mathf.SmoothStep(0f, 1f, _activityW);
            float t = _time;
            Vector3 groundUp = _root.up;
            float floor = _root.position.y + 0.07f * _scale;

            switch (_shownActivity)
            {
                case HumanoidActivity.Search:
                {
                    leftOnItem = false;
                    Vector3 r = At(new Vector3(0.14f, -0.55f + 0.05f * Mathf.Sin(t * 3f), 0.42f + 0.06f * Mathf.Sin(t * 5f)));
                    Vector3 l = At(new Vector3(-0.14f, -0.55f + 0.05f * Mathf.Sin(t * 3f + 2f), 0.42f + 0.06f * Mathf.Cos(t * 4f)));
                    right = Goal(AboveFloor(r, floor), HandRot(Dir(new Vector3(0f, -0.6f, 1f)), -groundUp, true), w);
                    left = Goal(AboveFloor(l, floor), HandRot(Dir(new Vector3(0f, -0.6f, 1f)), -groundUp, false), w);
                    break;
                }

                case HumanoidActivity.Build:
                {
                    // Steady the plank with the left hand and hammer with the right: slow lift, fast blow.
                    leftOnItem = false;
                    float cycle = Mathf.Repeat(t * 2.2f, 1f);
                    float u = cycle < 0.7f ? Mathf.SmoothStep(0f, 1f, cycle / 0.7f) : 1f - Mathf.Pow((cycle - 0.7f) / 0.3f, 2f);
                    Vector3 pos = Vector3.Lerp(new Vector3(0.03f, -0.12f, 0.47f), new Vector3(0.10f, 0.10f, 0.30f), u);
                    Vector3 dir = Vector3.Slerp(new Vector3(0f, -0.3f, 1f).normalized, new Vector3(0f, 0.85f, -0.3f).normalized, u);
                    right = Goal(At(pos), MeleeRot(Dir(dir)), w);
                    left = Goal(At(new Vector3(-0.06f, -0.12f, 0.45f)), HandRot(Dir(Vector3.forward), Dir(Vector3.down), false), w);
                    break;
                }

                case HumanoidActivity.Dig:
                {
                    leftOnItem = false;
                    Vector3 r = At(new Vector3(0.10f, -0.62f + 0.06f * Mathf.Sin(t * 5f), 0.42f + 0.05f * Mathf.Cos(t * 5f)));
                    right = Goal(AboveFloor(r, floor), MeleeRot(Dir(new Vector3(0f, -1f, 0.5f))), w);
                    left = Goal(AboveFloor(At(new Vector3(-0.15f, -0.62f, 0.32f)), floor), HandRot(Dir(Vector3.forward), -groundUp, false), w);
                    break;
                }

                case HumanoidActivity.Plant:
                {
                    leftOnItem = false;
                    float a = Mathf.Sin(t * 4f), b = Mathf.Sin(t * 4f + Mathf.PI);
                    Vector3 r = At(new Vector3(0.12f, -0.6f + 0.05f * Mathf.Max(0f, a), 0.4f));
                    Vector3 l = At(new Vector3(-0.12f, -0.6f + 0.05f * Mathf.Max(0f, b), 0.4f));
                    right = Goal(AboveFloor(r, floor), HandRot(Dir(new Vector3(0f, -0.7f, 1f)), -groundUp, true), w);
                    left = Goal(AboveFloor(l, floor), HandRot(Dir(new Vector3(0f, -0.7f, 1f)), -groundUp, false), w);
                    break;
                }

                case HumanoidActivity.Water:
                {
                    Vector3 spout = Dir(new Vector3(0f, -0.6f, 1f));
                    right = Goal(At(new Vector3(0.18f, -0.2f, 0.36f)), Quaternion.LookRotation(spout, Vector3.up), w);
                    break;
                }

                case HumanoidActivity.Use:
                {
                    // One hand out to whatever is being worked — a tap, a switch, a knob, a drawer — the fingers
                    // busy at it; with nothing to aim at, straight out in front at chest height.
                    leftOnItem = false;
                    Vector3 reach = new Vector3(0.1f, -0.06f, 0.44f);
                    if (HasReachTarget) reach = ReachOffset(ReachTarget);
                    reach += new Vector3(0.008f * Mathf.Sin(t * 4.1f), 0.012f * Mathf.Sin(t * 5.3f), 0.01f * Mathf.Sin(t * 3.7f + 1f));
                    Vector3 fingers = Dir(new Vector3(reach.x * 0.6f - 0.06f, reach.y * 0.5f, 1f));
                    right = Goal(HasReachTarget ? _chest + _frame * ReachOffset(ReachTarget) : At(reach), HandRot(fingers, Dir(new Vector3(-0.45f, -1f, 0.15f)), true), w);
                    break;
                }

                case HumanoidActivity.Read:
                {
                    // A book held open in both hands below the chin; now and then the right hand turns a page.
                    leftOnItem = false;
                    float page = Bell(Mathf.Repeat(t / 6.5f + _seed, 1f), 0.84f, 1f);
                    Vector3 r = new Vector3(0.1f - 0.07f * page, -0.2f + 0.03f * page, 0.3f);
                    Vector3 l = new Vector3(-0.1f, -0.2f, 0.3f);
                    right = Goal(At(r), HandRot(Dir(new Vector3(-0.25f, 0.75f, 0.55f)), Dir(new Vector3(-0.8f, 0.5f, -0.2f)), true), w);
                    left = Goal(At(l), HandRot(Dir(new Vector3(0.25f, 0.75f, 0.55f)), Dir(new Vector3(0.8f, 0.5f, -0.2f)), false), w);
                    break;
                }
            }
        }

        /// <summary>Where a working hand goes for a world point: toward it from the chest, but never behind the body
        /// and never past arm's length from the right shoulder (a thing out of reach is reached toward).</summary>
        private Vector3 ReachOffset(Vector3 world)
        {
            Vector3 d = Quaternion.Inverse(_frame) * (world - _chest) / Mathf.Max(0.01f, _scale);
            var shoulder = new Vector3(0.17f, 0.2f, 0f);
            Vector3 v = d - shoulder;
            v.z = Mathf.Max(v.z, 0.22f);
            v.x = Mathf.Clamp(v.x, -0.32f, 0.3f);
            if (v.sqrMagnitude > 0.54f * 0.54f) v = v.normalized * 0.54f;
            v.z = Mathf.Max(v.z, 0.18f);
            return shoulder + v;
        }

        private static Vector3 AboveFloor(Vector3 p, float floor)
        {
            if (p.y < floor) p.y = floor;
            return p;
        }

        /// <summary>
        /// A crawler's hands: each reaches out past the head, plants on the floor and stays put while the
        /// body drags itself over it (the reach shrinks as fast as the body travels), then lifts and
        /// reaches again while the other hand pulls.
        /// </summary>
        private void CrawlArms(float weight)
        {
            Transform parent = _root.parent;
            if (parent == null) return;
            _frame = _root.rotation;
            _scale = _root.lossyScale.y;
            Vector3 fwd = Flat(parent.forward);
            if (fwd == Vector3.zero) fwd = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            float floor = parent.position.y + _groundOffset + 0.035f * _scale;

            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                int ua = side == 0 ? UArmL : UArmR;
                float a = _phase + side * Mathf.PI;
                float reach = 0.5f + 0.5f * Mathf.Cos(a);
                float lift = Mathf.Max(0f, -Mathf.Sin(a));   // airborne only while swinging forward again
                Vector3 shoulder = _b[ua].position;
                Vector3 grip = new Vector3(shoulder.x, floor + 0.07f * lift * _scale, shoulder.z)
                             + fwd * (Mathf.Lerp(0.02f, 0.42f, reach) * _scale) + right * (sx * 0.05f * _scale);
                SolveArm(ua, sx, grip, HandRot(fwd, Vector3.down, side == 1), weight);
            }
        }

        /// <summary>
        /// Two-bone IK: turns the upper arm and forearm so the grip centre lands on
        /// <paramref name="grip"/>, the elbow bending down and out (or toward <paramref name="pole"/>, a world
        /// direction, when given), then sets the hand's rotation.
        /// </summary>
        private void SolveArm(int upper, float sx, Vector3 grip, Quaternion handRot, float weight, Vector3? pole = null)
        {
            if (weight <= 0.001f) return;
            Transform ua = _b[upper], fa = _b[upper + 1], hand = _b[upper + 2];
            Quaternion ua0 = ua.rotation, fa0 = fa.rotation, h0 = hand.rotation;

            Vector3 gripLocal = new Vector3(HumanoidRig.GripOffset.x * sx, HumanoidRig.GripOffset.y, HumanoidRig.GripOffset.z) * _rig.Proportions.Hand;
            Vector3 wrist = grip - handRot * (gripLocal * _scale);

            Vector3 s = ua.position, e = fa.position, w = hand.position;
            float a = (e - s).magnitude, b = (w - e).magnitude;
            Vector3 toTarget = wrist - s;
            float c = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(a - b) + 1e-3f, a + b - 1e-3f);
            Vector3 dir = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : _frame * Vector3.forward;

            Vector3 bend = Vector3.ProjectOnPlane(pole ?? _frame * new Vector3(sx * 0.5f, -0.45f, -0.75f), dir);
            if (bend.sqrMagnitude < 1e-8f) bend = Vector3.ProjectOnPlane(_frame * Vector3.down, dir);
            bend.Normalize();

            float cosA = Mathf.Clamp((a * a + c * c - b * b) / (2f * a * c), -1f, 1f);
            Vector3 elbow = s + a * (cosA * dir + Mathf.Sqrt(1f - cosA * cosA) * bend);

            ua.rotation = Quaternion.FromToRotation(e - s, elbow - s) * ua.rotation;
            fa.rotation = Quaternion.FromToRotation(hand.position - fa.position, s + dir * c - fa.position) * fa.rotation;
            hand.rotation = handRot;

            if (weight < 0.999f)
            {
                Quaternion uaIk = ua.rotation, faIk = fa.rotation;
                ua.rotation = Quaternion.Slerp(ua0, uaIk, weight);
                fa.rotation = Quaternion.Slerp(fa0, faIk, weight);
                hand.rotation = Quaternion.Slerp(h0, handRot, weight);
            }
        }

        /// <summary>Lying awake: the hands laced behind the head (under it, on the pillow), the elbows out to the sides.</summary>
        private void RestArms(float weight)
        {
            if (weight <= 0.001f) return;
            _frame = _root.rotation;
            _scale = _root.lossyScale.y;
            Vector3 head = _b[HeadB].position;
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                Vector3 grip = head + _frame * (new Vector3(sx * 0.055f, 0.07f, -0.11f) * _scale);
                Quaternion rot = HandRot(_frame * new Vector3(-sx, 0.35f, 0f), _frame * Vector3.forward, side == 1);
                SolveArm(side == 0 ? UArmL : UArmR, sx, grip, rot, weight, _frame * new Vector3(sx, 0.35f, 0.3f));
            }
        }

        /// <summary>Turns the neck and head toward <see cref="LookTarget"/>, within comfortable limits.</summary>
        private void Look(float dt, float weight)
        {
            Vector2 want = Vector2.zero;
            if (HasLookTarget && weight > 0.01f)
            {
                Vector3 d = Quaternion.Inverse(_root.rotation) * (LookTarget - _b[HeadB].position);
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
                if (Mathf.Abs(yaw) < 110f) want = new Vector2(Mathf.Clamp(yaw, -60f, 60f), Mathf.Clamp(pitch, -30f, 35f));
            }
            _look = Vector2.Lerp(_look, want * weight, 1f - Mathf.Exp(-dt * 6f));
            if (_look.sqrMagnitude < 0.01f) return;
            _b[Neck].localRotation *= Quaternion.Euler(_look.y * 0.4f, _look.x * 0.4f, 0f);
            _b[HeadB].localRotation *= Quaternion.Euler(_look.y * 0.6f, _look.x * 0.6f, 0f);
        }
    }
}
