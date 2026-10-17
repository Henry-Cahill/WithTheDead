using UnityEngine;

namespace CloneZ
{
    /// <summary>The bones of the humanoid rig, in hierarchy order (a parent always precedes its
    /// children). The order is also the bone index baked into the skinned mesh.</summary>
    public enum HumanBone : byte
    {
        Hips, Spine, Chest, Neck, Head,
        UpperArmL, ForearmL, HandL,
        UpperArmR, ForearmR, HandR,
        ThighL, ShinL, FootL,
        ThighR, ShinR, FootR,
    }

    /// <summary>
    /// Joint layout and body measurements for one <see cref="BodyBuild"/>, in metres, for a figure
    /// standing on y = 0 and facing +Z (its right hand on +X). The rig's bones sit at
    /// <see cref="Bind"/> with no rotation, which makes "standing straight, arms hanging" the zero pose
    /// every animation angle is measured from.
    /// </summary>
    public sealed class HumanoidProportions
    {
        public const int BoneCount = 17;

        /// <summary>Parent of each bone (-1 = the rig root).</summary>
        public static readonly int[] Parent = { -1, 0, 1, 2, 3, 2, 5, 6, 2, 8, 9, 0, 11, 12, 0, 14, 15 };

        public static readonly string[] Names =
        {
            "Hips", "Spine", "Chest", "Neck", "Head",
            "UpperArm.L", "Forearm.L", "Hand.L", "UpperArm.R", "Forearm.R", "Hand.R",
            "Thigh.L", "Shin.L", "Foot.L", "Thigh.R", "Shin.R", "Foot.R",
        };

        // Joint heights shared by every build, so every body stands 1.8 m tall.
        public const float PelvisY = 0.95f;
        public const float SpineY = 1.07f;
        public const float ChestY = 1.24f;
        public const float NeckY = 1.475f;
        public const float HeadY = 1.585f;
        public const float ShoulderY = 1.435f;
        public const float HipY = 0.915f;
        public const float KneeY = 0.495f;
        public const float AnkleY = 0.085f;
        public const float UpperArmLength = 0.295f;
        public const float ForearmLength = 0.255f;
        public const float ThighLength = HipY - KneeY;
        public const float ShinLength = KneeY - AnkleY;

        public readonly BodyBuild Build;

        /// <summary>Half the spacing of the shoulder and hip joints.</summary>
        public float ShoulderX, HipX;

        /// <summary>Torso half-widths (W) and half-depths (D), and how far a belly pushes forward.</summary>
        public float HipW, HipD, WaistW, WaistD, ChestW, ChestD, ShoulderW, Belly;

        /// <summary>Limb radii, and a scale for the hands.</summary>
        public float Deltoid, UpperArm, Elbow, Forearm, Wrist, Thigh, Knee, Calf, Ankle, Neck, Hand;

        /// <summary>Bind position of every bone in model space.</summary>
        public readonly Vector3[] Bind = new Vector3[BoneCount];

        private static readonly HumanoidProportions[] s_ByBuild = new HumanoidProportions[4];

        public static HumanoidProportions For(BodyBuild build)
        {
            int i = Mathf.Clamp((int)build, 0, s_ByBuild.Length - 1);
            return s_ByBuild[i] ??= new HumanoidProportions((BodyBuild)i);
        }

        private HumanoidProportions(BodyBuild build)
        {
            Build = build;
            switch (build)
            {
                case BodyBuild.Athletic:
                    ShoulderX = 0.188f; HipX = 0.093f;
                    HipW = 0.160f; HipD = 0.103f; WaistW = 0.138f; WaistD = 0.095f;
                    ChestW = 0.176f; ChestD = 0.116f; ShoulderW = 0.178f; Belly = 0f;
                    Deltoid = 0.057f; UpperArm = 0.053f; Elbow = 0.042f; Forearm = 0.046f; Wrist = 0.030f;
                    Thigh = 0.084f; Knee = 0.053f; Calf = 0.059f; Ankle = 0.035f; Neck = 0.060f; Hand = 1.04f;
                    break;

                case BodyBuild.Slim:
                    ShoulderX = 0.170f; HipX = 0.088f;
                    HipW = 0.150f; HipD = 0.095f; WaistW = 0.125f; WaistD = 0.085f;
                    ChestW = 0.146f; ChestD = 0.098f; ShoulderW = 0.156f; Belly = 0f;
                    Deltoid = 0.045f; UpperArm = 0.042f; Elbow = 0.035f; Forearm = 0.038f; Wrist = 0.026f;
                    Thigh = 0.070f; Knee = 0.045f; Calf = 0.048f; Ankle = 0.031f; Neck = 0.050f; Hand = 0.96f;
                    break;

                case BodyBuild.Heavy:
                    ShoulderX = 0.192f; HipX = 0.098f;
                    HipW = 0.185f; HipD = 0.125f; WaistW = 0.180f; WaistD = 0.145f;
                    ChestW = 0.186f; ChestD = 0.135f; ShoulderW = 0.185f; Belly = 0.035f;
                    Deltoid = 0.062f; UpperArm = 0.058f; Elbow = 0.047f; Forearm = 0.050f; Wrist = 0.033f;
                    Thigh = 0.095f; Knee = 0.058f; Calf = 0.064f; Ankle = 0.038f; Neck = 0.066f; Hand = 1.06f;
                    break;

                default:
                    ShoulderX = 0.180f; HipX = 0.092f;
                    HipW = 0.163f; HipD = 0.104f; WaistW = 0.142f; WaistD = 0.098f;
                    ChestW = 0.162f; ChestD = 0.108f; ShoulderW = 0.168f; Belly = 0f;
                    Deltoid = 0.052f; UpperArm = 0.048f; Elbow = 0.040f; Forearm = 0.043f; Wrist = 0.029f;
                    Thigh = 0.080f; Knee = 0.051f; Calf = 0.055f; Ankle = 0.034f; Neck = 0.056f; Hand = 1f;
                    break;
            }

            const float armZ = -0.012f;
            Set(HumanBone.Hips, 0f, PelvisY, 0f);
            Set(HumanBone.Spine, 0f, SpineY, 0f);
            Set(HumanBone.Chest, 0f, ChestY, 0f);
            Set(HumanBone.Neck, 0f, NeckY, -0.012f);
            Set(HumanBone.Head, 0f, HeadY, -0.006f);
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                int arm = side == 0 ? (int)HumanBone.UpperArmL : (int)HumanBone.UpperArmR;
                Bind[arm] = new Vector3(sx * ShoulderX, ShoulderY, armZ);
                Bind[arm + 1] = new Vector3(sx * ShoulderX, ShoulderY - UpperArmLength, armZ);
                Bind[arm + 2] = new Vector3(sx * ShoulderX, ShoulderY - UpperArmLength - ForearmLength, armZ);

                int leg = side == 0 ? (int)HumanBone.ThighL : (int)HumanBone.ThighR;
                Bind[leg] = new Vector3(sx * HipX, HipY, 0f);
                Bind[leg + 1] = new Vector3(sx * HipX, KneeY, 0f);
                Bind[leg + 2] = new Vector3(sx * HipX, AnkleY, 0f);
            }
        }

        private void Set(HumanBone b, float x, float y, float z) => Bind[(int)b] = new Vector3(x, y, z);

        /// <summary>A bone's bind position relative to its parent (the rig root for the hips).</summary>
        public Vector3 LocalBind(int bone)
        {
            int parent = Parent[bone];
            return parent < 0 ? Bind[bone] : Bind[bone] - Bind[parent];
        }
    }
}
