using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Draws the survivor's gear on the body: a backpack on the back (sized by how much it holds, in the
    /// bag's colour, with shoulder straps, and a hip belt and bedroll on the big ones), a chest rig of
    /// magazine pouches on a harness, a thigh bag strapped to the right thigh (or a fanny pack on the
    /// belt), night-vision goggles on a head mount — flipped down over the eyes while on, stowed up while
    /// off — a gas mask with its side filter, and a small angle-head flashlight clipped to the left chest
    /// strap whose lens glows while it's on.
    ///
    /// Everything is built from collider-free primitives and ribbon straps hung on the bones of
    /// <see cref="PlayerAvatar.Rig"/>, so it moves with the animation. A piece is rebuilt only when the
    /// item behind it changes (<see cref="PlayerClothing.Changed"/> / <see cref="PlayerInventory.Changed"/>),
    /// and the lot is hidden whenever the camera hides the body (first person, or a shoulder camera
    /// squeezed against the survivor). Added by <see cref="PlayerGear"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerGearVisuals : MonoBehaviour
    {
        private const float PackStrapX = 0.115f;     // a backpack's shoulder straps, either side of the neck
        private const float PackStrapWidth = 0.04f;
        private const float HarnessX = 0.08f;        // a chest rig's harness, just inside the pack straps
        private const float HarnessWidth = 0.028f;
        private const float NvgStowedAngle = -105f;  // hinge pitch with the goggles flipped up off the eyes
        private const float NvgFlipSpeed = 420f;     // degrees per second

        private static readonly Color Webbing = new Color(0.09f, 0.09f, 0.09f);
        private static readonly Color Hardware = new Color(0.05f, 0.05f, 0.05f);
        private static readonly Color BedrollColor = new Color(0.25f, 0.30f, 0.22f);
        private static readonly Color FilterColor = new Color(0.33f, 0.35f, 0.26f);
        private static readonly Color TorchColor = new Color(0.20f, 0.24f, 0.18f);
        private static readonly Color TorchGlowColor = new Color(1f, 0.95f, 0.78f);
        private static readonly Color NvgGlowColor = new Color(0.45f, 1f, 0.5f);
        // reflective safety strip colour used on the backpack front panel
        private static readonly Color ReflectStripColor = new Color(1f, 0.85f, 0.5f);

        /// <summary>The chest flashlight's lens (null when none is carried); its forward is the beam's direction.</summary>
        public Transform FlashlightLens { get; private set; }

        /// <summary>Between the goggles' objective lenses (null when none are worn); its forward is where they look.</summary>
        public Transform NightVisionLens { get; private set; }

        // One drawn item: the item it shows, the roots hung on the bones and any meshes generated for it.
        private sealed class Piece
        {
            public ItemDef Def;
            public readonly List<GameObject> Roots = new List<GameObject>(2);
            public readonly List<Mesh> Meshes = new List<Mesh>(2);

            public void Clear()
            {
                for (int i = 0; i < Roots.Count; i++)
                    if (Roots[i] != null) Object.Destroy(Roots[i]);
                for (int i = 0; i < Meshes.Count; i++)
                    if (Meshes[i] != null) Object.Destroy(Meshes[i]);
                Roots.Clear();
                Meshes.Clear();
                Def = null;
            }

            public void SetActive(bool active)
            {
                for (int i = 0; i < Roots.Count; i++)
                    if (Roots[i] != null) Roots[i].SetActive(active);
            }
        }

        private readonly Piece _back = new Piece();
        private readonly Piece _chestRig = new Piece();
        private readonly Piece _thigh = new Piece();
        private readonly Piece _goggles = new Piece();
        private readonly Piece _mask = new Piece();
        private readonly Piece _torch = new Piece();

        private PlayerGear _gear;
        private PlayerClothing _clothing;
        private PlayerInventory _inventory;
        private PlayerAvatar _avatar;
        private HumanoidRig _rig;
        private HumanoidProportions _p;
        private PlayerGearTorso _torso;
        private readonly List<Vector3> _path = new List<Vector3>(24);

        private ItemDef _torsoDef;
        private bool _torsoKnown;
        private float _inflate;
        private bool _torchShown;
        private bool _dirty = true;
        private bool _hidden;
        private bool _subscribed;
        private int _nextRigProbe;

        private Transform _nvgHinge;
        private float _nvgAngle;
        private readonly Renderer[] _nvgLenses = new Renderer[2];
        private bool _nvgLit;
private Renderer _torchLens; // renderer for the flashlight lens
private Renderer _torchSwitch; // renderer for the new on‑off switch
private bool _torchLit; // whether flashlight is on
private Material _torchSwitchMat; // material for the switch (glow when on)
private Renderer _torchLED; // renderer for the LED indicator
private Material _torchLEDMat; // base (off) material for the LED

        private void Awake()
        {
            _gear = GetComponent<PlayerGear>();
            _clothing = GetComponent<PlayerClothing>();
            _inventory = GetComponent<PlayerInventory>();
        }

        private void OnEnable()
        {
            // Ensure the PlayerGear component is cached; Awake may have run before this object became active.
            if (_gear == null) _gear = GetComponent<PlayerGear>();
            Subscribe();
            _dirty = true;
        }

        private void OnDisable() => Unsubscribe();

        private void Start() => Subscribe();

        private void OnDestroy()
        {
            Unsubscribe();
            ClearAll();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            if (_clothing == null) _clothing = GetComponent<PlayerClothing>();
            if (_inventory == null) _inventory = GetComponent<PlayerInventory>();
            if (_clothing == null && _inventory == null) return;
            if (_clothing != null) _clothing.Changed += MarkDirty;
            if (_inventory != null) _inventory.Changed += MarkDirty;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            if (_clothing != null) _clothing.Changed -= MarkDirty;
            if (_inventory != null) _inventory.Changed -= MarkDirty;
        }

        private void MarkDirty() => _dirty = true;

        private void LateUpdate()
        {
            if (!EnsureRig()) return;
            if (_gear == null) _gear = GetComponent<PlayerGear>();

            bool carried = _gear != null && _gear.HasFlashlight;
            if (carried != _torchShown) _dirty = true;
            if (_dirty) Refresh();

            // Hidden whenever the camera hides the body: in first person, or with the shoulder camera
            // pushed up against the survivor by a wall.
            bool hide = ViewState.Current == CameraView.FirstPerson || IsoCameraFollow.SurvivorHidden;
            if (hide != _hidden)
            {
                _hidden = hide;
                SetAllActive(!hide);
            }

            AnimateGoggles(Time.deltaTime);
            UpdateTorchLens();
        }

        // The rig is built by PlayerAvatar (added after the gear), so it is looked up lazily.
        private bool EnsureRig()
        {
            if (_rig != null)
            {
                if (_rig.Proportions != _p)
                {
                    ClearAll();
                    _p = _rig.Proportions;
                    _torso = new PlayerGearTorso(_p);
                    _dirty = true;
                }
                return true;
            }
            if (Time.frameCount < _nextRigProbe) return false;
            _nextRigProbe = Time.frameCount + 15;
            if (_avatar == null) _avatar = GetComponent<PlayerAvatar>();
            if (_avatar == null || _avatar.Rig == null || _avatar.Rig.Bones == null || _avatar.Rig.Proportions == null) return false;

            ClearAll();
            _rig = _avatar.Rig;
            _p = _rig.Proportions;
            _torso = new PlayerGearTorso(_p);
            _dirty = true;
            return true;
        }

        // Rebuilds only the pieces whose item changed (a new top re-fits everything worn over it).
        private void Refresh()
        {
            _dirty = false;
            PlayerClothing c = _clothing;

            ItemDef top = Worn(c, ClothingSlot.Torso);
            bool refit = false;
            if (!_torsoKnown || top != _torsoDef)
            {
                float inflate = TopInflate(top);
                refit = !_torsoKnown || !Mathf.Approximately(inflate, _inflate);
                _torsoKnown = true;
                _torsoDef = top;
                _inflate = inflate;
            }

            ItemDef back = Worn(c, ClothingSlot.Back);
            ItemDef rig = Worn(c, ClothingSlot.Rig);
            ItemDef thigh = Worn(c, ClothingSlot.Thigh);
            ItemDef eyes = Worn(c, ClothingSlot.Eyes);
            ItemDef mask = Worn(c, ClothingSlot.Mask);
            bool torch = _gear != null && _gear.HasFlashlight;

            bool backChanged = back != _back.Def || refit;
            if (backChanged) BuildBackpack(back);
            if (rig != _chestRig.Def || refit) BuildChestRig(rig);
            if (thigh != _thigh.Def) BuildThighBag(thigh);
            bool maskChanged = mask != _mask.Def;
            if (maskChanged) BuildGasMask(mask);
            if (eyes != _goggles.Def || maskChanged) BuildGoggles(eyes);
            if (torch != _torchShown || backChanged) BuildFlashlight(torch);
        }

        private static ItemDef Worn(PlayerClothing c, ClothingSlot slot) => c != null ? c.GetWorn(slot) : null;

        // How far the worn top stands off the skin — the same allowance the body mesh gives each cut.
        private static float TopInflate(ItemDef top)
        {
            if (top == null) return 0f;
            switch (GarmentStyles.Top(GarmentStyles.Key(top)))
            {
                case TopStyle.Tank: return 0.011f;
                case TopStyle.Dress: return 0.012f;
                case TopStyle.TShirt: return 0.013f;
                case TopStyle.LongSleeve: return 0.015f;
                case TopStyle.Hoodie: return 0.022f;
                case TopStyle.Jacket: return 0.026f;
                default: return 0.03f;
            }
        }

        // A root hung on a bone, offset back to the model origin so its parts can be placed in model space
        // (a figure standing on y = 0, facing +Z) and still turn with the bone.
        private Transform Root(Piece piece, HumanBone bone, string name)
        {
            var go = new GameObject(name);
            go.layer = gameObject.layer;
            Transform t = go.transform;
            t.SetParent(_rig.Bone(bone), false);
            t.localPosition = -_p.Bind[(int)bone];
            piece.Roots.Add(go);
            if (_hidden) go.SetActive(false);
            return t;
        }

        private void StrapOverShoulder(Piece piece, Transform root, float x, float backY, float frontY, float lift, float width, float thickness, Material m)
        {
            _torso.ShoulderPath(_path, x, _inflate, backY, frontY, lift);
            Mesh mesh = PlayerGearParts.Ribbon(root, "Strap", _path, Vector3.right, width, thickness, m);
            if (mesh != null) piece.Meshes.Add(mesh);
        }

        private void SetAllActive(bool active)
        {
            _back.SetActive(active);
            _chestRig.SetActive(active);
            _thigh.SetActive(active);
            _goggles.SetActive(active);
            _mask.SetActive(active);
            _torch.SetActive(active);
        }

        private void ClearAll()
        {
            _back.Clear();
            _chestRig.Clear();
            _thigh.Clear();
            _goggles.Clear();
            _mask.Clear();
            _torch.Clear();
            _torsoKnown = false;
            _torchShown = false;
            _nvgHinge = null;
            _nvgLenses[0] = _nvgLenses[1] = null;
            _torchLens = null;
            FlashlightLens = null;
            NightVisionLens = null;
        }

        #region Backpack
        private void BuildBackpack(ItemDef def)
        {
            _back.Clear();
            _back.Def = def;
            if (def == null) return;

            // Grows with what it holds: a school bag (10 kg) is the reference, a military pack much bigger.
            float cap = def.ContainerCapacity > 0f ? def.ContainerCapacity : 8f;
            float k = Mathf.Clamp(Mathf.Pow(cap / 10f, 1f / 3f), 0.8f, 1.45f);
            float w = 0.30f * Mathf.Pow(k, 0.75f), h = 0.38f * Mathf.Pow(k, 1.1f), d = 0.13f * Mathf.Pow(k, 1.1f);
            const float top = 1.43f;
            float bottom = top - h;
            bool big = k > 1.08f;

            // The pack's face rests just off the deepest point of the back over its height.
            float backZ = float.MaxValue;
            for (float y = Mathf.Max(bottom, 0.95f); y <= top; y += 0.04f)
                backZ = Mathf.Min(backZ, _torso.Surface(y, 0f, _inflate, false));
            float cz = backZ - 0.012f - d * 0.5f;
            float outer = cz - d * 0.5f;

            Material body = CloneZUtil.Mat(def.Tint, 0.12f);
            Material shade = CloneZUtil.Mat(HumanoidLook.Shade(def.Tint, 0.72f), 0.1f);
            Material web = CloneZUtil.Mat(Webbing, 0.15f);
            Material hardware = CloneZUtil.Mat(Hardware, 0.4f);

            Transform root = Root(_back, HumanBone.Chest, "Gear_Backpack");
            float bodyH = h - d * 0.5f;
            PlayerGearParts.Box(root, "Bag", new Vector3(0f, bottom + bodyH * 0.5f, cz), new Vector3(w, bodyH, d), body, true);
            PlayerGearParts.Tube(root, "BagTop", new Vector3(0f, top - d * 0.5f, cz), Vector3.right, w, d * 0.5f, body, true);
            // Tiny carabiner loop indicating a worn attachment point.
            PlayerGearParts.Box(root, "Carabiner", new Vector3(0f, top - d * 0.5f + 0.018f, cz + d * 0.5f + 0.006f), new Vector3(0.008f, 0.018f, 0.008f), hardware);
            // Small rivet boxes on the top tube for a worn‑metal look.
            PlayerGearParts.Box(root, "RivetFront", new Vector3(0f, top - d * 0.5f, cz + d * 0.5f + 0.004f), new Vector3(0.02f, 0.02f, 0.008f), hardware);
            PlayerGearParts.Box(root, "RivetBack", new Vector3(0f, top - d * 0.5f, cz - d * 0.5f - 0.004f), new Vector3(0.02f, 0.02f, 0.008f), hardware);
                PlayerGearParts.Box(root, "Lid", new Vector3(0f, top - d * 0.5f - 0.035f, outer - 0.004f), new Vector3(w * 0.92f, 0.10f, 0.012f), shade);
                // Thin fabric stitching seam at the front edge of the backpack
                PlayerGearParts.Box(root, "Seam", new Vector3(0f, top - d * 0.5f - 0.039f, outer - 0.010f), new Vector3(w * 0.92f, 0.002f, 0.001f), web);

            // Rugged lid detailing calculations
            float lidY = top - d * 0.5f - 0.035f; // centre Y of the existing lid box
            float lidZ = outer - 0.004f; // centre Z of the lid surface
            var lidWearMat = CloneZUtil.Mat(HumanoidLook.Shade(TorchColor, 0.55f), 0.2f); // subtle wear colour

            // Fabric strap across the lid front edge
            PlayerGearParts.Box(root, "LidStrap", new Vector3(0f, lidY + 0.03f, lidZ + 0.006f), new Vector3(w * 0.78f, 0.008f, 0.004f), web);
            // Small metal latch on the left side of that strap
            PlayerGearParts.Box(root, "LidLatch", new Vector3(-w * 0.30f, lidY + 0.042f, lidZ + 0.008f), new Vector3(0.02f, 0.012f, 0.004f), hardware);
            // Tiny knob on the latch
            PlayerGearParts.Tube(root, "LatchKnob", new Vector3(-w * 0.30f, lidY + 0.048f, lidZ + 0.012f), Vector3.forward, 0.008f, 0.003f, hardware);
            // Rivet centered on the strap front edge
            PlayerGearParts.Box(root, "LidRivet", new Vector3(0f, lidY + 0.032f, lidZ + 0.008f), new Vector3(0.014f, 0.014f, 0.004f), hardware);
            // Subtle wear scar on the lid's top surface
            PlayerGearParts.Box(root, "LidWear", new Vector3(0f, lidY + 0.045f, lidZ - 0.002f), new Vector3(w * 0.70f, 0.002f, 0.001f), lidWearMat);
// thin metal frame bar across the front‑top of the pack
PlayerGearParts.Box(root, "FrontFrame", new Vector3(0f, lidY + 0.05f, lidZ + 0.006f), new Vector3(w * 0.95f, 0.004f, 0.006f), hardware);
// metal logo plate centred on the front
PlayerGearParts.Box(root, "LogoPlate", new Vector3(0f, lidY + 0.03f, lidZ + 0.006f), new Vector3(0.07f, 0.018f, 0.004f), hardware);
// reflective safety strip – glows faintly
var reflectMat = PlayerGearParts.Glow(ReflectStripColor);
PlayerGearParts.Box(root, "ReflectStrip", new Vector3(0f, lidY - 0.02f, lidZ + 0.0065f), new Vector3(w * 0.60f, 0.002f, 0.001f), reflectMat);
// subtle wear scratch across the front surface
PlayerGearParts.Box(root, "FrontWearScratch", new Vector3(0f, lidY + 0.04f, lidZ + 0.005f), new Vector3(w * 0.80f, 0.001f, 0.001f), lidWearMat);

            float ph = h * 0.42f, py = bottom + 0.03f + ph * 0.5f;
            PlayerGearParts.Box(root, "Pocket", new Vector3(0f, py, outer - 0.024f), new Vector3(w * 0.72f, ph, 0.048f), shade, true);
            PlayerGearParts.Box(root, "Zip", new Vector3(0f, py + ph * 0.5f - 0.012f, outer - 0.0485f), new Vector3(w * 0.64f, 0.006f, 0.003f), web);
            for (int s = -1; s <= 1; s += 2)
                PlayerGearParts.Box(root, "Compression", new Vector3(s * w * 0.32f, bottom + h * 0.66f, outer - 0.003f), new Vector3(0.022f, h * 0.42f, 0.006f), web);

            if (big)
            {
                for (int s = -1; s <= 1; s += 2)
                    PlayerGearParts.Box(root, "SidePocket", new Vector3(s * (w * 0.5f + 0.018f), bottom + h * 0.24f, cz), new Vector3(0.036f, h * 0.36f, d * 0.72f), shade);
                PlayerGearParts.Tube(root, "Bedroll", new Vector3(0f, bottom - 0.045f, cz + 0.01f), Vector3.right, w * 1.04f, 0.048f,
                    CloneZUtil.Mat(BedrollColor, 0.1f), true);
            }

            // Shoulder straps out of the top of the pack, over the shoulders and down the chest, joined by a sternum strap.
            for (int s = -1; s <= 1; s += 2)
            {
                StrapOverShoulder(_back, root, s * PackStrapX, 1.38f, 1.16f, 0.008f, PackStrapWidth, 0.01f, web);
                Vector3 end = _path[_path.Count - 1];
                PlayerGearParts.Box(root, "StrapBuckle", end + new Vector3(0f, 0.014f, 0.007f), new Vector3(0.046f, 0.022f, 0.008f), hardware);
            }
            const float sternumY = 1.30f;
            float sz = _torso.Surface(sternumY, 0f, _inflate, true) + 0.014f;
            PlayerGearParts.Strap(root, new Vector3(-PackStrapX, sternumY, sz), new Vector3(PackStrapX, sternumY, sz), 0.018f, 0.006f, web, Vector3.forward);
            PlayerGearParts.Box(root, "SternumClip", new Vector3(0f, sternumY, sz + 0.004f), new Vector3(0.03f, 0.022f, 0.008f), hardware);

            // The big packs ride on a hip belt.
            if (big)
            {
                Transform hips = Root(_back, HumanBone.Hips, "Gear_HipBelt");
                const float beltY = 1.0f;
                _torso.Sample(beltY, out float bw, out float bd, out float bz, out _);
                PlayerGearParts.Band(hips, "HipBelt", new Vector3(0f, beltY, bz), bw + 0.023f, bd + 0.023f, 0.045f, Quaternion.identity, web);
                PlayerGearParts.Box(hips, "BeltBuckle", new Vector3(0f, beltY, bz + bd + 0.026f), new Vector3(0.05f, 0.035f, 0.01f), hardware);
            }
        }
        #endregion

        #region Chest rig
        private void BuildChestRig(ItemDef def)
        {
            _chestRig.Clear();
            _chestRig.Def = def;
            if (def == null) return;

            Material body = CloneZUtil.Mat(def.Tint, 0.1f);
            Material shade = CloneZUtil.Mat(HumanoidLook.Shade(def.Tint, 0.7f), 0.1f);
            Material web = CloneZUtil.Mat(HumanoidLook.Shade(def.Tint, 0.45f), 0.12f);
            Material hardware = CloneZUtil.Mat(Hardware, 0.4f);

            Transform root = Root(_chestRig, HumanBone.Chest, "Gear_ChestRig");
            const float y0 = 1.07f, y1 = 1.27f, panelD = 0.022f;
            float front = 0f;
            for (float y = y0; y <= y1 + 1e-4f; y += 0.02f) front = Mathf.Max(front, _torso.Surface(y, 0f, _inflate, true));
            front += 0.008f;
            PlayerGearParts.Box(root, "Panel", new Vector3(0f, (y0 + y1) * 0.5f, front + panelD * 0.5f), new Vector3(0.30f, y1 - y0, panelD), shade, true);
            // Added detailed front panel features
            float panelY = (y0 + y1) * 0.5f; // centre Y of the panel
            float panelZ = front + panelD * 0.5f; // centre Z of the panel front face
            Material wearMat = CloneZUtil.Mat(HumanoidLook.Shade(TorchColor, 0.55f), 0.2f); // subtle wear colour

            // Thin reinforcement bar across the top of the panel (stiffening rib)
            PlayerGearParts.Box(root, "FrontBar", new Vector3(0f, panelY + 0.05f, panelZ + 0.003f), new Vector3(0.30f * 0.95f, 0.004f, 0.006f), hardware);
            // Small metal logo plate centred on the front bar
            PlayerGearParts.Box(root, "LogoPlate", new Vector3(0f, panelY + 0.03f, panelZ + 0.003f), new Vector3(0.07f, 0.018f, 0.004f), hardware);
            // Reflective safety strip that glows faintly
            var reflectMat = PlayerGearParts.Glow(ReflectStripColor);
            PlayerGearParts.Box(root, "ReflectStrip", new Vector3(0f, panelY - 0.02f, panelZ + 0.0035f), new Vector3(0.30f * 0.60f, 0.002f, 0.001f), reflectMat);
            // Subtle wear scar across the front surface
            PlayerGearParts.Box(root, "FrontWearScratch", new Vector3(0f, panelY + 0.04f, panelZ + 0.0045f), new Vector3(0.30f * 0.80f, 0.001f, 0.001f), wearMat);
            // Central rivet on the reinforcement bar
            PlayerGearParts.Box(root, "BarRivet", new Vector3(0f, panelY + 0.05f, panelZ + 0.006f), new Vector3(0.014f, 0.014f, 0.004f), hardware);

            // A row of four magazine pouches with flaps.
            float pz = front + panelD + 0.022f;
            for (int i = 0; i < 4; i++)
            {
                float x = -0.105f + 0.07f * i;
                PlayerGearParts.Box(root, "Pouch", new Vector3(x, 1.155f, pz), new Vector3(0.06f, 0.11f, 0.044f), body, true);
                PlayerGearParts.Box(root, "Flap", new Vector3(x, 1.197f, pz + 0.001f), new Vector3(0.064f, 0.032f, 0.047f), shade);
            }

// Harness straps from the top of the panel over the shoulders, and a strap round the waist.
        for (int s = -1; s <= 1; s += 2)
        {
            StrapOverShoulder(_chestRig, root, s * HarnessX, 1.18f, y1 - 0.01f, 0.007f, HarnessWidth, 0.008f, web);
            Vector3 end = _path[_path.Count - 1];
            PlayerGearParts.Box(root, "StrapBuckle", end + new Vector3(0f, 0.014f, 0.007f), new Vector3(0.046f, 0.022f, 0.008f), hardware);
        }

        // Add four tiny rivet boxes to suggest worn‑metal fastening on the front panel.
        float rivetZ = front + panelD * 0.5f + 0.004f;
        float rivetSizeX = 0.02f, rivetSizeY = 0.02f, rivetSizeZ = 0.008f;
        // Top left
        PlayerGearParts.Box(root, "RivetTL", new Vector3(-0.12f, y0 + 0.07f, rivetZ), new Vector3(rivetSizeX, rivetSizeY, rivetSizeZ), hardware);
        // Top right
        PlayerGearParts.Box(root, "RivetTR", new Vector3(0.12f, y0 + 0.07f, rivetZ), new Vector3(rivetSizeX, rivetSizeY, rivetSizeZ), hardware);
        // Bottom left
        PlayerGearParts.Box(root, "RivetBL", new Vector3(-0.12f, y1 - 0.07f, rivetZ), new Vector3(rivetSizeX, rivetSizeY, rivetSizeZ), hardware);
        // Bottom right
        PlayerGearParts.Box(root, "RivetBR", new Vector3(0.12f, y1 - 0.07f, rivetZ), new Vector3(rivetSizeX, rivetSizeY, rivetSizeZ), hardware);

        Transform spine = Root(_chestRig, HumanBone.Spine, "Gear_ChestRigStrap");
        const float waistY = 1.09f;
        _torso.Sample(waistY, out float ww, out float wd, out float wz, out _);
        PlayerGearParts.Band(spine, "WaistStrap", new Vector3(0f, waistY, wz), ww + _inflate + 0.008f, wd + _inflate + 0.008f, 0.03f, Quaternion.identity, web);
        }
        #endregion

        #region Thigh bag / fanny pack
        private void BuildThighBag(ItemDef def)
        {
            _thigh.Clear();
            _thigh.Def = def;
            if (def == null) return;

            Material body = CloneZUtil.Mat(def.Tint, 0.12f);
            Material shade = CloneZUtil.Mat(HumanoidLook.Shade(def.Tint, 0.72f), 0.1f);
            Material web = CloneZUtil.Mat(Webbing, 0.15f);
            Material hardware = CloneZUtil.Mat(Hardware, 0.4f);

            if (GarmentStyles.Key(def).Contains("fanny"))
            {
                // A pouch on a belt round the front of the hips.
                Transform hips = Root(_thigh, HumanBone.Hips, "Gear_FannyPack");
                const float y = 0.985f;
                _torso.Sample(y, out float fw, out float fd, out float fz, out _);
                PlayerGearParts.Band(hips, "Belt", new Vector3(0f, y + 0.012f, fz), fw + 0.017f, fd + 0.017f, 0.025f, Quaternion.identity, web);
                float front = _torso.Surface(y, 0f, 0.009f, true) + 0.004f;
                PlayerGearParts.Box(hips, "Pouch", new Vector3(0f, y - 0.008f, front + 0.03f), new Vector3(0.17f, 0.085f, 0.06f), body, true);
                PlayerGearParts.Box(hips, "Zip", new Vector3(0f, y + 0.02f, front + 0.0605f), new Vector3(0.15f, 0.005f, 0.003f), web);
                PlayerGearParts.Box(hips, "Buckle", new Vector3(fw * 0.62f, y + 0.012f, front + 0.004f), new Vector3(0.03f, 0.026f, 0.008f), hardware);
                return;
            }

            // A drop-leg pouch on the outside of the right thigh (below the hanging hand), two leg straps
            // and a hanger from the belt.
            Transform root = Root(_thigh, HumanBone.ThighR, "Gear_ThighBag");
            float hipX = _p.HipX;
            const float bagY = 0.66f;
            float bx = hipX + ThighRadius(bagY) + 0.006f + 0.022f;
            PlayerGearParts.Box(root, "Bag", new Vector3(bx, bagY, 0.004f), new Vector3(0.044f, 0.16f, 0.125f), body, true);
            PlayerGearParts.Box(root, "Flap", new Vector3(bx + 0.002f, bagY + 0.062f, 0.004f), new Vector3(0.047f, 0.04f, 0.128f), shade);
            // ----- added rugged side‑pocket details -----
            // side pocket (adds usable space on the bag’s right side)
            PlayerGearParts.Box(root, "SidePocket", new Vector3(bx + 0.032f, bagY + 0.04f, 0.006f), new Vector3(0.022f, 0.06f, 0.056f), shade, true);
            // zip strip on the pocket front
            PlayerGearParts.Box(root, "PocketZip", new Vector3(bx + 0.032f, bagY + 0.075f, 0.036f), new Vector3(0.020f, 0.004f, 0.001f), web);
            // tiny metal clasp that holds the zip
            PlayerGearParts.Box(root, "Clasp", new Vector3(bx + 0.032f, bagY + 0.07f, 0.038f), new Vector3(0.008f, 0.008f, 0.004f), hardware);
            // subtle wear‑scar on the bag’s front
            PlayerGearParts.Box(root, "FrontWear", new Vector3(bx, bagY + 0.08f, 0.014f), new Vector3(0.040f, 0.003f, 0.001f), CloneZUtil.Mat(HumanoidLook.Shade(TorchColor, 0.55f), 0.2f));
            // small metal label/tag on the pocket’s outer edge
            PlayerGearParts.Box(root, "PocketLabel", new Vector3(bx + 0.032f, bagY + 0.042f, 0.064f), new Vector3(0.010f, 0.006f, 0.001f), hardware);
            PlayerGearParts.Box(root, "Buckle", new Vector3(bx + 0.025f, bagY + 0.04f, 0.004f), new Vector3(0.006f, 0.022f, 0.03f), hardware);
            for (int i = 0; i < 2; i++)
            {
                float y = i == 0 ? 0.725f : 0.60f;
                float r = ThighRadius(y) + 0.005f;
                PlayerGearParts.Band(root, "LegStrap", new Vector3(hipX, y, 0.004f), r, r, 0.022f, Quaternion.identity, web);
            }
            PlayerGearParts.Strap(root, new Vector3(bx - 0.008f, bagY + 0.08f, 0.004f),
                new Vector3(hipX + ThighRadius(0.93f) + 0.006f, 0.99f, 0.004f), 0.03f, 0.006f, web, Vector3.right);
        }

        // The right thigh's radius (with trousers on) at a height, from the body builder's thigh keys.
        private float ThighRadius(float y)
        {
            float t = Mathf.InverseLerp(HumanoidProportions.HipY + 0.02f, HumanoidProportions.KneeY, y);
            float r;
            if (t < 0.3f) r = _p.Thigh;
            else if (t < 0.72f) r = Mathf.Lerp(_p.Thigh, _p.Knee * 1.2f, (t - 0.3f) / 0.42f);
            else r = Mathf.Lerp(_p.Knee * 1.2f, _p.Knee * 1.02f, (t - 0.72f) / 0.28f);
            return r + 0.013f;
        }
        #endregion

        #region Night vision
        private void BuildGoggles(ItemDef def)
        {
            _goggles.Clear();
            _goggles.Def = def;
            _nvgHinge = null;
            _nvgLenses[0] = _nvgLenses[1] = null;
            NightVisionLens = null;
            if (def == null || def.Gear != GearKind.NightVision) return;

            Material body = CloneZUtil.Mat(def.Tint, 0.3f, 0.2f);
            Material dark = CloneZUtil.Mat(HumanoidLook.Shade(def.Tint, 0.6f), 0.25f);
            Material web = CloneZUtil.Mat(Webbing, 0.15f);

            // With a gas mask on, the goggles stand a little further off the face to clear its lenses.
            float standOff = _mask.Def != null && _mask.Def.Gear == GearKind.GasMask ? 0.03f : 0f;
            Transform root = Root(_goggles, HumanBone.Head, "Gear_NightVision");
            PlayerGearParts.Band(root, "HeadStrap", new Vector3(0f, 1.775f, -0.024f), 0.092f, 0.107f, 0.016f, Quaternion.Euler(-10f, 0f, 0f), web);
            PlayerGearParts.Box(root, "Mount", new Vector3(0f, 1.787f, 0.094f + standOff * 0.4f), new Vector3(0.042f, 0.042f, 0.02f),
                Quaternion.Euler(-14f, 0f, 0f), dark);

            // Everything forward of the hinge flips together.
            var hinge = new GameObject("Hinge").transform;
            hinge.gameObject.layer = gameObject.layer;
            hinge.SetParent(root, false);
            hinge.localPosition = new Vector3(0f, 1.79f, 0.108f + standOff);
            _nvgHinge = hinge;

            PlayerGearParts.Box(hinge, "Arm", new Vector3(0f, -0.04f, 0.012f), new Vector3(0.022f, 0.06f, 0.014f), dark);
            PlayerGearParts.Box(hinge, "Bridge", new Vector3(0f, -0.082f, 0.034f), new Vector3(0.076f, 0.03f, 0.04f), body, true);
            PlayerGearParts.Box(hinge, "Illuminator", new Vector3(0f, -0.062f, 0.05f), new Vector3(0.014f, 0.012f, 0.02f), dark);
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.032f : 0.032f;
                PlayerGearParts.Tube(hinge, "Tube", new Vector3(x, -0.085f, 0.05f), Vector3.forward, 0.075f, 0.0175f, body, true);
                PlayerGearParts.Tube(hinge, "Eyecup", new Vector3(x, -0.085f, 0.0125f), Vector3.forward, 0.006f, 0.019f, dark);
                Transform lens = PlayerGearParts.Tube(hinge, "Lens", new Vector3(x, -0.085f, 0.0885f), Vector3.forward, 0.004f, 0.0148f, NvgGlass);
                _nvgLenses[i] = lens.GetComponent<Renderer>();
            }

            var mid = new GameObject("LensMid").transform;
            mid.gameObject.layer = gameObject.layer;
            mid.SetParent(hinge, false);
            mid.localPosition = new Vector3(0f, -0.085f, 0.09f);
            NightVisionLens = mid;

            bool on = _gear != null && _gear.NightVisionOn;
            _nvgAngle = on ? 0f : NvgStowedAngle;
            hinge.localRotation = Quaternion.Euler(_nvgAngle, 0f, 0f);
            _nvgLit = !on;
            SetGogglesLit(on);
        }

        // Flips the goggles down over the eyes while they're on, up onto the head mount while off.
        private void AnimateGoggles(float dt)
        {
            if (_nvgHinge == null) return;
            bool on = _gear != null && _gear.NightVisionOn;
            float target = on ? 0f : NvgStowedAngle;
            if (_nvgAngle != target)
            {
                _nvgAngle = Mathf.MoveTowards(_nvgAngle, target, NvgFlipSpeed * dt);
                _nvgHinge.localRotation = Quaternion.Euler(_nvgAngle, 0f, 0f);
            }
            SetGogglesLit(on);
        }

        private void SetGogglesLit(bool lit)
        {
            if (lit == _nvgLit) return;
            _nvgLit = lit;
            Material m = lit ? PlayerGearParts.Glow(NvgGlowColor) : NvgGlass;
            for (int i = 0; i < _nvgLenses.Length; i++)
                if (_nvgLenses[i] != null) _nvgLenses[i].sharedMaterial = m;
        }

        private static Material NvgGlass => CloneZUtil.Mat(new Color(0.07f, 0.11f, 0.09f), 0.9f);
        #endregion

        #region Gas mask
        private void BuildGasMask(ItemDef def)
        {
            _mask.Clear();
            _mask.Def = def;
            if (def == null || def.Gear != GearKind.GasMask) return;

            Material rubber = CloneZUtil.Mat(def.Tint, 0.35f);
            Material trim = CloneZUtil.Mat(HumanoidLook.Shade(def.Tint, 0.7f), 0.3f);
            Material glass = CloneZUtil.Mat(new Color(0.10f, 0.12f, 0.13f), 0.92f);
            Material canister = CloneZUtil.Mat(FilterColor, 0.3f, 0.2f);
            Material web = CloneZUtil.Mat(Webbing, 0.15f);
            Material hardware = CloneZUtil.Mat(Hardware, 0.4f);

            Transform root = Root(_mask, HumanBone.Head, "Gear_GasMask");
            PlayerGearParts.Part(root, "FacePiece", PrimitiveType.Sphere, new Vector3(0f, 1.662f, 0.058f), Quaternion.identity,
                new Vector3(0.152f, 0.172f, 0.124f), rubber, true);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 dir = new Vector3(s * 0.22f, 0.05f, 1f).normalized;
                Vector3 at = new Vector3(s * 0.032f, 1.703f, 0.104f);
                PlayerGearParts.Tube(root, "LensRim", at, dir, 0.012f, 0.0245f, trim);
                PlayerGearParts.Tube(root, "Lens", at + dir * 0.0065f, dir, 0.003f, 0.0205f, glass);
            }
            PlayerGearParts.Tube(root, "Valve", new Vector3(0f, 1.622f, 0.118f), new Vector3(0f, -0.25f, 1f), 0.03f, 0.022f, trim);

            // The filter canister screws into the left cheek.
            Vector3 filterDir = new Vector3(-0.62f, -0.18f, 0.76f).normalized;
            var port = new Vector3(-0.055f, 1.625f, 0.09f);
            PlayerGearParts.Tube(root, "FilterPort", port + filterDir * 0.012f, filterDir, 0.024f, 0.022f, trim);
            PlayerGearParts.Tube(root, "Filter", port + filterDir * 0.045f, filterDir, 0.044f, 0.037f, canister, true);
            PlayerGearParts.Tube(root, "FilterCap", port + filterDir * 0.069f, filterDir, 0.006f, 0.032f, trim);
            PlayerGearParts.Box(root, "FilterLabel", port + filterDir * 0.073f, new Vector3(0.01f, 0.006f, 0.001f), hardware);
            // left‑side rivet
            PlayerGearParts.Box(root, "RivetL",
                port + filterDir * 0.050f + new Vector3(-0.006f, 0f, 0f),
                new Vector3(0.014f, 0.014f, 0.004f), hardware);
            // right‑side rivet
            PlayerGearParts.Box(root, "RivetR",
                port + filterDir * 0.050f + new Vector3( 0.006f, 0f, 0f),
                new Vector3(0.014f, 0.014f, 0.004f), hardware);

            // canister top screw
            PlayerGearParts.Tube(root, "CanisterScrew",
                port + filterDir * 0.090f,
                filterDir, 0.006f, 0.012f, hardware);

            // rear strap anchor points
            Vector3 strapStart = new Vector3(0f, 1.68f, -0.04f); // center‑back of mask
            Vector3 strapEnd = new Vector3(-0.08f, 1.71f, 0.00f); // left‑temple area
            PlayerGearParts.Strap(root, strapStart, strapEnd, 0.018f, 0.006f, rubber, Vector3.up);

            // rear strap buckle
            PlayerGearParts.Box(root, "StrapBuckle",
                strapEnd + new Vector3(0f, 0.004f, 0.002f),
                new Vector3(0.022f, 0.014f, 0.008f), hardware);

            // vent grill (three parallel slats)
            float ventY = 1.64f; // below the lenses
            float ventZ = 0.095f;
            float ventXStart = -0.03f;
            for (int i = 0; i < 3; i++)
            {
                float x = ventXStart + i * 0.03f;
                PlayerGearParts.Box(root, $"Vent{i}",
                    new Vector3(x, ventY, ventZ),
                    new Vector3(0.06f, 0.002f, 0.001f), hardware);
            }

            PlayerGearParts.Band(root, "Harness", new Vector3(0f, 1.695f, -0.024f), 0.101f, 0.118f, 0.014f, Quaternion.Euler(16f, 0f, 0f), web);
        }
        #endregion

        #region Flashlight
        // An angle-head flashlight clipped to the left chest strap (or straight onto the jacket).
        private void BuildFlashlight(bool carried)
        {
            _torch.Clear();
            _torchShown = carried;
            _torchLens = null;
            FlashlightLens = null;
            if (!carried) return;

            Material metal = CloneZUtil.Mat(TorchColor, 0.35f, 0.3f);
            Material dark = CloneZUtil.Mat(Hardware, 0.3f);

            Transform root = Root(_torch, HumanBone.Chest, "Gear_Flashlight");
            const float x = -PackStrapX, y = 1.335f;
            float lift = _back.Def != null ? 0.013f : 0.002f;   // on the outer face of the pack strap, or the jacket
            float z = _torso.Surface(y, x, _inflate, true) + lift;
            PlayerGearParts.Box(root, "Clip", new Vector3(x, y, z + 0.004f), new Vector3(0.024f, 0.05f, 0.008f), dark);
            // faint wear scar on the clip
            var wearMat = CloneZUtil.Mat(HumanoidLook.Shade(TorchColor, 0.55f), 0.2f);
            PlayerGearParts.Box(root, "ClipWear",
                new Vector3(x, y, z + 0.006f),
                new Vector3(0.018f, 0.004f, 0.001f), wearMat);
            PlayerGearParts.Tube(root, "Body", new Vector3(x, y - 0.005f, z + 0.025f), Vector3.up, 0.1f, 0.016f, metal, true);
            Vector3 head = new Vector3(x, y + 0.05f, z + 0.036f);
            PlayerGearParts.Tube(root, "Head", head, Vector3.forward, 0.05f, 0.021f, metal, true);
// tactile on‑off switch on the flashlight body
var switchObj = PlayerGearParts.Box(root, "Switch",
    new Vector3(x - 0.006f, y - 0.002f, z + 0.03f),
    new Vector3(0.006f, 0.008f, 0.004f), dark);
_torchSwitch = switchObj.GetComponent<Renderer>();
// tiny LED indicator that glows when the flashlight is on
var ledObj = PlayerGearParts.Box(root, "LED",
    new Vector3(x - 0.008f, y - 0.001f, z + 0.032f),
    new Vector3(0.004f, 0.004f, 0.001f), dark);
_torchLED = ledObj.GetComponent<Renderer>();
_torchLEDMat = dark; // remember the dark material
// thin reinforcement ring around the head
PlayerGearParts.Box(root, "HeadRing",
    new Vector3(x, y + 0.07f, z + 0.045f),
    new Vector3(0.022f, 0.001f, 0.004f), dark);
            Transform lens = PlayerGearParts.Tube(root, "Lens", head + new Vector3(0f, 0f, 0.0255f), Vector3.forward, 0.003f, 0.0175f, TorchGlass);
            _torchLens = lens.GetComponent<Renderer>();

            var beam = new GameObject("Beam").transform;
            beam.gameObject.layer = gameObject.layer;
            beam.SetParent(root, false);
            beam.localPosition = head + new Vector3(0f, 0f, 0.028f);
            FlashlightLens = beam;

            _torchSwitchMat = dark;
            _torchLit = false;
            UpdateTorchLens();
        }

        private void UpdateTorchLens()
        {
            if (_torchLens == null) return;
            bool lit = _gear != null && _gear.FlashlightOn;
            if (lit == _torchLit && _torchLens.sharedMaterial != null && (_torchSwitch == null || _torchSwitch.sharedMaterial != null)) return;
            _torchLit = lit;
            _torchLens.sharedMaterial = lit ? PlayerGearParts.Glow(TorchGlowColor) : TorchGlass;
            if (_torchSwitch != null)
                _torchSwitch.sharedMaterial = lit ? PlayerGearParts.Glow(TorchGlowColor) : _torchSwitchMat;
            // set LED colour
            if (_torchLED != null)
                _torchLED.sharedMaterial = lit ? PlayerGearParts.Glow(TorchGlowColor) : _torchLEDMat;
        }

        private static Material TorchGlass => CloneZUtil.Mat(new Color(0.62f, 0.66f, 0.64f), 0.92f);
        #endregion
    }
}
