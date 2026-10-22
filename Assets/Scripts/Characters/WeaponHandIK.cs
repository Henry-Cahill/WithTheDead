using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;

namespace CloneZ
{
    /// <summary>
    /// Hand IK on the weapon with the Animation Rigging package (the doc's "one full-body rig for every view"): the
    /// body is posed procedurally (<see cref="HumanoidAnimator"/>), then a <see cref="RigBuilder"/> with a
    /// <see cref="TwoBoneIKConstraint"/> puts the support hand exactly on the gun's handguard, whichever camera is
    /// looking — the same rig the first-person eye, the shoulder camera and the isometric view all see. The graph is
    /// evaluated by hand after the procedural pose; a first job copies that pose into the animation stream so the
    /// constraint starts from the body as it is this frame rather than the bind pose.
    /// </summary>
    [DefaultExecutionOrder(10)]   // after the avatar has posed the body (0), before the camera reads the head (50)
    public sealed class WeaponHandIK : MonoBehaviour
    {
        private struct SceneToStreamJob : IAnimationJob
        {
            public NativeArray<TransformStreamHandle> Stream;
            public NativeArray<TransformSceneHandle> Scene;

            public void ProcessRootMotion(AnimationStream stream) { }

            public void ProcessAnimation(AnimationStream stream)
            {
                for (int i = 0; i < Stream.Length; i++)
                {
                    Stream[i].SetLocalPosition(stream, Scene[i].GetLocalPosition(stream));
                    Stream[i].SetLocalRotation(stream, Scene[i].GetLocalRotation(stream));
                }
            }
        }

        private PlayerAvatar _avatar;
        private HumanoidRig _rig;
        private Animator _animator;
        private RigBuilder _builder;
        private Rig _ikRig;
        private TwoBoneIKConstraint _left;
        private Transform _target, _hint;
        // Cached frequently accessed transforms to avoid per‑frame bone lookups
        private Transform _handL, _chest, _gripR;
        private Vector3 _palmOffset;
        private PlayableGraph _graph;
        private NativeArray<TransformStreamHandle> _stream;
        private NativeArray<TransformSceneHandle> _scene;
        private bool _built, _failed;

        /// <summary>How much the support hand is held by the IK right now (0 with no long gun in hand).</summary>
        public float Weight => _ikRig != null ? _ikRig.weight : 0f;

        private void Awake() => _avatar = GetComponent<PlayerAvatar>();

        private void TryBuild()
        {
            if (_failed || _avatar == null || _avatar.Rig == null || _avatar.Rig.Bones == null) return;
            _rig = _avatar.Rig;
            try
            {
                _animator = _rig.GetComponent<Animator>();
                if (_animator == null) _animator = _rig.gameObject.AddComponent<Animator>();
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _animator.applyRootMotion = false;

                var rigGo = new GameObject("HandIK Rig");
                rigGo.transform.SetParent(_rig.transform, false);
                _ikRig = rigGo.AddComponent<Rig>();
                _ikRig.weight = 0f;
                var cGo = new GameObject("SupportHandIK");
                cGo.transform.SetParent(rigGo.transform, false);
                _left = cGo.AddComponent<TwoBoneIKConstraint>();
                _left.weight = 1f;

                _target = new GameObject("IK_Target_L").transform;
                _target.SetParent(_rig.transform, false);
                _hint = new GameObject("IK_Hint_L").transform;
                _hint.SetParent(_rig.transform, false);

                _left.data.root = _rig.Bone(HumanBone.UpperArmL);
                _left.data.mid = _rig.Bone(HumanBone.ForearmL);
                _left.data.tip = _rig.Bone(HumanBone.HandL);
                // Cache frequently used transforms for per‑frame use
                _handL = _rig.Bone(HumanBone.HandL);
                _chest = _rig.Bone(HumanBone.Chest);
                _gripR = _rig.GripR;
                _palmOffset = new Vector3(-HumanoidRig.GripOffset.x, HumanoidRig.GripOffset.y, HumanoidRig.GripOffset.z) * _rig.Proportions.Hand;
                _left.data.target = _target;
                _left.data.hint = _hint;
                _left.data.targetPositionWeight = 1f;
                _left.data.targetRotationWeight = 0.35f;
                _left.data.hintWeight = 0.8f;

                _builder = _rig.gameObject.AddComponent<RigBuilder>();
                _builder.layers.Add(new RigLayer(_ikRig, true));

                _graph = PlayableGraph.Create("CloneZ HandIK");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                Transform[] bones = _rig.Bones;
                _stream = new NativeArray<TransformStreamHandle>(bones.Length, Allocator.Persistent);
                _scene = new NativeArray<TransformSceneHandle>(bones.Length, Allocator.Persistent);
                for (int i = 0; i < bones.Length; i++)
                {
                    _stream[i] = _animator.BindStreamTransform(bones[i]);
                    _scene[i] = _animator.BindSceneTransform(bones[i]);
                }
                var sync = AnimationScriptPlayable.Create(_graph, new SceneToStreamJob { Stream = _stream, Scene = _scene });
                var output = AnimationPlayableOutput.Create(_graph, "SceneToStream", _animator);
                output.SetAnimationStreamSource(AnimationStreamSource.DefaultValues);
                output.SetSortingOrder(500);
                output.SetSourcePlayable(sync);
                if (!_builder.Build(_graph)) throw new System.Exception("the rig did not build");
                _built = true;
            }
            catch (System.Exception e)
            {
                _failed = true;
                Debug.LogWarning("[CloneZ] Hand IK unavailable (" + e.Message + "); the procedural grip stays in charge.");
                Teardown();
            }
        }

        private void LateUpdate()
        {
            if (!_built) TryBuild();
            if (!_built || _rig == null) return;

            bool longGun = _rig.HeldStyle == HoldStyle.Firearm && _rig.Held != null && _rig.HeldLength >= 0.42f;
            _ikRig.weight = Mathf.MoveTowards(_ikRig.weight, longGun ? 1f : 0f, Time.deltaTime * 6f);
            if (_ikRig.weight <= 0.001f) return;

            // Where the left wrist must be for its palm to close on the handguard.
            Transform handL = _handL;
            Vector3 support = _gripR.TransformPoint(_rig.HeldSupport);
            Vector3 palm = _palmOffset;
            _target.SetPositionAndRotation(support - handL.rotation * palm, handL.rotation);

            // The elbow points down and out, under the gun.
            Transform chest = _chest;
            Transform root = transform;
            _hint.position = chest.position - root.right * 0.45f - Vector3.up * 0.35f + root.forward * 0.1f;

            _builder.SyncLayers();
            _graph.Evaluate(Time.deltaTime);
        }

        private void OnDestroy() => Teardown();

        private void Teardown()
        {
            if (_builder != null) _builder.Clear();
            if (_graph.IsValid()) _graph.Destroy();
            if (_stream.IsCreated) _stream.Dispose();
            if (_scene.IsCreated) _scene.Dispose();
            _built = false;
        }
    }
}
