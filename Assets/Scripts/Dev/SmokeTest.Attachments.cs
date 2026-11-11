using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// <c>attachments</c>: the attachments on the gun in the survivor's hands, and the light and laser on it. Into a raid by
    /// night, a target board set up a few metres off down the clearest line, then a pistol and four long guns in turn, each
    /// kitted out with the inventory's own verb (WeaponItems.AttachTo). Every attachment must build onto the held model and
    /// touch it; a red dot must raise the gun's outline and a suppressor lengthen it; the lot must hide with the body in first
    /// person and come back after; and the floating-geometry report must flag nothing of the gun's. The light: L (the real
    /// key) switches it, the spot light shines from its lens along the barrel and brightens where it lands, it swings onto
    /// the shot's line when the gun comes up to aim (right button), runs its battery flat and goes dark, comes back with a
    /// battery, and leaves the gun with its charge when a laser takes its place. The laser: its dot lands where the barrel
    /// points, on the shot's line once aimed, and L puts it out. With <c>-clonez-smoke-shots &lt;dir&gt;</c>, pictures of each
    /// gun from beside the survivor, of the scene, and through the bodycam.
    /// </summary>
    public sealed partial class SmokeTest
    {
        private const float AttachmentsNight = 23f;
        private Transform _board;

        private IEnumerator AttachmentsRun()
        {
            yield return ToHub();
            yield return Deploy(RaidKind.Operative);
            Actor me = Actor.LocalPlayer;
            var inv = me != null ? me.GetComponent<PlayerInventory>() : null;
            var gear = me != null ? me.GetComponent<PlayerGear>() : null;
            var avatar = me != null ? me.GetComponent<PlayerAvatar>() : null;
            var combat = me != null ? me.GetComponent<PlayerCombat>() : null;
            var tactical = me != null ? me.GetComponent<WeaponTactical>() : null;
            IsoCameraFollow cam = IsoCameraFollow.Instance;
            Check("attachments: the survivor and their kit", inv != null && gear != null && avatar != null && combat != null && tactical != null && cam != null);
            if (inv == null || gear == null || avatar == null || combat == null || tactical == null || cam == null) yield break;
            me.Untargetable = true;   // the dead mustn't end the run
            ReleaseAll();
            PointAtMiddle();

            Try("attachments: a weapon light's charge survives the text form", () =>
            {
                ItemExtra back = ItemExtra.Parse(ItemExtra.Serialize(new ItemExtra { LightUsed = 0.375f, Mods = { "mod_weaponlight" } }));
                if (back == null || Mathf.Abs(back.LightUsed - 0.375f) > 1e-4f) throw new Exception("came back as " + (back != null ? back.LightUsed.ToString(CultureInfo.InvariantCulture) : "null"));
            });

            AttachmentsSky();
            yield return PlaceBoard(me, cam);
            Check("attachments: a target board ahead", _board != null, _board != null ? Vector3.Distance(me.transform.position, _board.position).ToString("0.0") + " m off" : "");

            yield return PistolPass(me, inv, gear, avatar, combat, tactical, cam);
            yield return LongGunPass(me, inv, gear, avatar, tactical, cam, "ak74", "ammo545_ps",
                new[] { "mod_barrel_long", "mod_suppressor545", "mod_reddot", "mod_foregrip", "mod_stock_tactical", "mod_mag_extended", "mod_weaponlight" });
            yield return LongGunPass(me, inv, gear, avatar, tactical, cam, "shotgun", "shotgunshells",
                new[] { "mod_scope4x", "mod_barrel_long", "mod_weaponlight", "mod_stock_folding" });
            yield return LongGunPass(me, inv, gear, avatar, tactical, cam, "smg9", "ammo9mm",
                new[] { "mod_suppressor9", "mod_laser", "mod_stock_folding", "mod_foregrip", "mod_mag_extended", "mod_reddot" });
            yield return LongGunPass(me, inv, gear, avatar, tactical, cam, "mosin", "ammo762r_lps",
                new[] { "mod_scope4x", "mod_compensator", "mod_barrel_short", "mod_stock_tactical" });

            if (_board != null) Destroy(_board.gameObject);
            cam.SetView(CameraView.Isometric);
        }

        #region The pistol: the attachments, the light and the laser
        private IEnumerator PistolPass(Actor me, PlayerInventory inv, PlayerGear gear, PlayerAvatar avatar, PlayerCombat combat,
            WeaponTactical tactical, IsoCameraFollow cam)
        {
            const string P = "pistol: ";
            HumanoidRig rig = avatar.Rig;
            Unkit(inv);
            ItemStack gun = GiveGun(inv, "pistol", "ammo9mm");
            yield return null;
            yield return null;
            Check(P + "in hand", gun != null && rig.HeldDef == gun.Def && rig.Held != null, rig.HeldDef != null ? rig.HeldDef.Id : "nothing");
            if (gun == null || rig.Held == null) yield break;
            CheckMuzzleAhead(P, me, rig);
            MeshRenderer body = rig.Held.GetComponent<MeshRenderer>();
            Bounds plain = GunShape.Of(gun.Def, rig.Held.GetComponent<MeshFilter>().sharedMesh).Box;
            Check(P + "nothing built on a bare gun", rig.HeldAttachments == null);

            foreach (string mod in new[] { "mod_reddot", "mod_suppressor9", "mod_weaponlight" }) Check(P + "fit " + mod, Fit(inv, gun, mod));
            yield return null;
            yield return null;
            WeaponAttachments.Fitted fit = rig.HeldAttachments;
            Check(P + "the three attachments are built on the gun in hand", fit != null && fit.Renderers.Count == 3, fit != null ? fit.Renderers.Count + " built" : "none");
            if (fit == null) yield break;
            CheckSeated(P, fit, body);
            Bounds dressed = DressedBounds(rig.Held, gun.Def, fit);
            float rise = SightRise(rig.Held, gun.Def, fit);
            Check(P + "the red dot stands above the slide", rise > 0.015f, F3(rise) + " m above the slide where it sits");
            Check(P + "the suppressor lengthens it", dressed.max.x > plain.max.x + 0.1f, "muzzle " + F3(plain.max.x) + " -> " + F3(dressed.max.x) + " m");
            Check(P + "the light has a lens, facing down the barrel", fit.LightLens != null && Vector3.Dot(fit.LightLens.forward, fit.Root.right) > 0.99f);
            yield return Pictures("pistol-kit", me, rig, cam, false);
            yield return EveryLook(P, "pistol", me, rig, cam);
            fit = rig.HeldAttachments;
            if (fit == null || rig.Held == null) yield break;
            body = rig.Held.GetComponent<MeshRenderer>();

            // First person hides the body, and the gun's kit with it.
            cam.SetView(CameraView.FirstPerson);
            yield return Wait(0.8f);
            Check(P + "first person hides the attachments with the body", !rig.Body.enabled && AllHidden(fit), "body " + (rig.Body.enabled ? "shown" : "hidden"));
            cam.SetView(CameraView.ThirdPerson);
            yield return Wait(0.5f);
            Check(P + "and shows them again after", rig.Body.enabled && AllShown(fit));

            // The light: L, the beam from the lens, what it lights.
            Check(P + "the gun's light is found in hand, switched off", gear.HasWeaponLight && !gear.WeaponLightOn && !tactical.LightLit);
            float dark = BeamSpotLuma(tactical, fit, me, rig, out string where);
            yield return Tap(Key.L);
            yield return WaitGame(0.3f);
            Check(P + "L switches the weapon light on", gear.WeaponLightOn && gear.LightOn && tactical.LightLit && tactical.Light != null && tactical.Light.enabled,
                "intensity " + (tactical.Light != null ? tactical.Light.intensity.ToString("0.0") : "-"));
            yield return new WaitForEndOfFrame();
            Check(P + "the beam leaves the lens along the barrel", tactical.Light != null && fit.LightLens != null
                && Vector3.Distance(tactical.LightOrigin, fit.LightLens.position) < 0.06f && Vector3.Dot(tactical.LightDirection, fit.LightLens.forward) > 0.97f,
                "origin " + (fit.LightLens != null ? Vector3.Distance(tactical.LightOrigin, fit.LightLens.position).ToString("0.000") : "-") + " m from the lens");
            float lit = BeamSpotLuma(tactical, fit, me, rig, out where);
            Debug.Log("[Smoke] attachments light spot " + where + ": luminance " + dark.ToString("0.000") + " off, " + lit.ToString("0.000") + " on");
            Check(P + "the light lights where it points", lit > dark * 1.3f + 0.02f, "luminance " + dark.ToString("0.000") + " -> " + lit.ToString("0.000"));
            yield return Pictures("pistol-light", me, rig, cam, true);

            // Aimed (right button in the shoulder view), the beam swings onto the shot's line.
            yield return AimAndCheck(P + "aimed, the beam follows the shot's line", combat, avatar, () =>
            {
                Vector3 end = ShotEnd(combat, me);
                Vector3 to = (end - tactical.LightOrigin).normalized;
                float dot = Vector3.Dot(to, tactical.LightDirection);
                return (dot > 0.985f, "within " + (Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg).ToString("0.0") + " deg of it");
            });
            yield return AimedPictures("pistol-light", me, combat, avatar, cam);

            // The battery: run flat, dark; a battery brings it back.
            gun.Extra.LightUsed = 0.997f;   // a few seconds of light left
            yield return UntilGame(() => !gear.WeaponLightOn, 10f);
            Check(P + "the light runs its battery flat and goes out", !gear.WeaponLightOn && gun.Extra.LightUsed >= 1f, "used " + gun.Extra.LightUsed.ToString("0.000"));
            yield return Tap(Key.L);
            Check(P + "a flat light won't come on", !gear.WeaponLightOn);
            ItemStack battery = Give(inv, "battery");
            Try(P + "use a battery", () => inv.Use(battery));
            yield return null;
            Check(P + "a battery recharges the light on the gun", gun.Extra.LightUsed <= 0.001f && gear.WeaponLightCharge01 > 0.99f, "used " + gun.Extra.LightUsed.ToString("0.000"));

            // Swapped for a laser, the light leaves with its charge.
            gun.Extra.LightUsed = 0.4f;
            Check(P + "fit mod_laser in the light's place", Fit(inv, gun, "mod_laser"));
            ItemStack light = Newest(inv, "mod_weaponlight");
            Check(P + "the light comes off with its battery", light != null && light.Condition == 60 && light.Charge01 > 0.59f && light.Charge01 < 0.61f,
                light != null ? "condition " + light.Condition : "not in the pack");
            yield return null;
            yield return null;
            Check(P + "the gun keeps none of the light's charge", gun.Extra.LightUsed == 0f && !gear.HasWeaponLight && gear.HasLaser);
            fit = rig.HeldAttachments;
            Check(P + "the laser takes the light's place", fit != null && fit.LaserEmitter != null && fit.LightLens == null && fit.Renderers.Count == 3);
            if (fit == null || fit.LaserEmitter == null) yield break;
            CheckSeated(P, fit, body);

            // The laser: on when fitted, its dot where the barrel points.
            yield return WaitGame(0.2f);
            yield return new WaitForEndOfFrame();
            Vector3 from = fit.LaserEmitter.position, dir = fit.LaserEmitter.forward;
            bool hits = CastPast(me, from, dir, WeaponTactical.LaserRange, out Vector3 spot);
            Check(P + "the laser is on and its dot drawn", gear.LaserOn && tactical.LaserShown);
            Check(P + "the dot lands where the barrel points", hits && tactical.LaserShown && Vector3.Distance(spot, tactical.LaserDot) < 0.1f,
                hits ? Vector3.Distance(spot, tactical.LaserDot).ToString("0.000") + " m off, " + Vector3.Distance(from, spot).ToString("0.0") + " m out" : "the barrel points at nothing");
            yield return AimAndCheck(P + "aimed, the dot sits where the round will go", combat, avatar, () =>
            {
                Vector3 end = ShotEnd(combat, me);
                float off = Vector3.Distance(end, tactical.LaserDot);
                return (tactical.LaserShown && off < 0.15f, off.ToString("0.000") + " m from the shot's end, " + Vector3.Distance(me.transform.position, end).ToString("0.0") + " m out");
            });
            yield return Pictures("pistol-laser", me, rig, cam, true);
            yield return AimedPictures("pistol-laser", me, combat, avatar, cam);
            yield return Tap(Key.L);
            yield return WaitGame(0.1f);
            Check(P + "L puts the laser out", !gear.LaserOn && !tactical.LaserShown);
            yield return Tap(Key.L);
            FloatingCheck(P, "pistol");
        }
        #endregion

        #region Long guns
        private IEnumerator LongGunPass(Actor me, PlayerInventory inv, PlayerGear gear, PlayerAvatar avatar, WeaponTactical tactical,
            IsoCameraFollow cam, string id, string ammo, string[] mods)
        {
            string P = id + ": ";
            HumanoidRig rig = avatar.Rig;
            Unkit(inv);
            ItemStack gun = GiveGun(inv, id, ammo);
            yield return null;
            yield return null;
            Check(P + "in hand", gun != null && rig.HeldDef == gun.Def && rig.Held != null, rig.HeldDef != null ? rig.HeldDef.Id : "nothing");
            if (gun == null || rig.Held == null) yield break;
            CheckMuzzleAhead(P, me, rig);
            MeshRenderer body = rig.Held.GetComponent<MeshRenderer>();
            Vector3 plainSupport = rig.HeldSupport;
            Bounds plain = GunShape.Of(gun.Def, rig.Held.GetComponent<MeshFilter>().sharedMesh).Box;
            int fitted = 0;
            foreach (string mod in mods) if (Fit(inv, gun, mod)) fitted++;
            Check(P + "fit " + string.Join(", ", mods), fitted == mods.Length, fitted + " of " + mods.Length);
            yield return null;
            yield return null;
            WeaponAttachments.Fitted fit = rig.HeldAttachments;
            Check(P + "every attachment is built on the gun in hand", fit != null && fit.Renderers.Count == mods.Length, fit != null ? fit.Renderers.Count + " built" : "none");
            if (fit == null) yield break;
            CheckSeated(P, fit, body);
            Bounds dressed = DressedBounds(rig.Held, gun.Def, fit);
            if (Array.IndexOf(mods, "mod_reddot") >= 0 || Array.IndexOf(mods, "mod_scope4x") >= 0)
            {
                float rise = SightRise(rig.Held, gun.Def, fit);
                Check(P + "the sight stands above the gun", rise > 0.015f, F3(rise) + " m above the gun where it sits");
            }
            if (Array.IndexOf(mods, "mod_barrel_long") >= 0 || Array.IndexOf(mods, "mod_suppressor9") >= 0)
                Check(P + "the muzzle reaches further", dressed.max.x > plain.max.x + 0.08f, "muzzle " + F3(plain.max.x) + " -> " + F3(dressed.max.x) + " m");
            if (Array.IndexOf(mods, "mod_barrel_long") >= 0 && Array.IndexOf(mods, "mod_suppressor545") >= 0)
                Check(P + "the suppressor goes on the end of the long barrel", dressed.max.x > plain.max.x + 0.25f, "muzzle " + F3(plain.max.x) + " -> " + F3(dressed.max.x) + " m");
            if (Array.IndexOf(mods, "mod_foregrip") >= 0)
                Check(P + "the off hand takes the foregrip", fit.Support.HasValue && (rig.HeldSupport - plainSupport).magnitude > 0.02f,
                    "support moved " + (rig.HeldSupport - plainSupport).magnitude.ToString("0.000") + " m");
            yield return EveryLook(P, id, me, rig, cam);
            fit = rig.HeldAttachments;
            if (fit == null || rig.Held == null) yield break;

            bool light = Array.IndexOf(mods, "mod_weaponlight") >= 0, laser = Array.IndexOf(mods, "mod_laser") >= 0;
            if (light)
            {
                if (!gear.WeaponLightOn) yield return Tap(Key.L);
                yield return WaitGame(0.3f);
                yield return new WaitForEndOfFrame();
                Check(P + "the light shines from its lens", gear.WeaponLightOn && tactical.LightLit && fit.LightLens != null
                    && Vector3.Distance(tactical.LightOrigin, fit.LightLens.position) < 0.06f);
            }
            if (laser)
            {
                yield return WaitGame(0.2f);
                Check(P + "the laser's dot is drawn", gear.LaserOn && tactical.LaserShown);
            }
            yield return Pictures(id + "-kit", me, rig, cam, light || laser);
            if (light) yield return Tap(Key.L);
            FloatingCheck(P, id);
        }
        #endregion

        #region Helpers
        private static void AttachmentsSky()
        {
            Climate.Winter = false;
            if (WeatherSystem.Instance != null) WeatherSystem.Instance.Restore(Weather.Clear, 0f, 0.2f, 0.2f);
            if (TimeOfDay.Instance != null) TimeOfDay.Instance.RestoreClock(TimeOfDay.Instance.Day, AttachmentsNight);
        }

        // A board facing the survivor down the clearest line from where they stand, and the survivor facing it in the shoulder
        // view (its yaw is the survivor's while no mouse moves it).
        private IEnumerator PlaceBoard(Actor me, IsoCameraFollow cam)
        {
            Vector3 feet = me.transform.position;
            Vector3 chest = feet + Vector3.up * 1.2f;
            float yaw = ClearestHeading(chest);
            Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            float run = Physics.Raycast(chest, fwd, out RaycastHit wall, 25f, Actor.SightMask, QueryTriggerInteraction.Ignore) ? wall.distance : 25f;
            float distance = Mathf.Clamp(run - 0.6f, 2.5f, 5f);
            Vector3 at = feet + fwd * distance;
            float ground = Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out RaycastHit floor, 6f, Actor.SightMask, QueryTriggerInteraction.Ignore)
                ? floor.point.y : feet.y;
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "[Smoke] target board";
            board.transform.SetPositionAndRotation(new Vector3(at.x, ground + 1.15f, at.z), Quaternion.LookRotation(-fwd, Vector3.up));
            board.transform.localScale = new Vector3(3f, 2.4f, 0.15f);
            board.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(new Color(0.62f, 0.62f, 0.6f), 0.2f);
            _board = board.transform;

            var cc = me.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            me.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
            cam.SetView(CameraView.ThirdPerson);
            yield return Wait(0.6f);
        }

        private static ItemStack GiveGun(PlayerInventory inv, string id, string ammo)
        {
            ItemDef gun = ItemDatabase.Get(id), rounds = ItemDatabase.Get(ammo);
            if (gun == null) return null;
            inv.Inventory.Add(gun, 1, enforceWeight: false);
            if (rounds != null) inv.Inventory.Add(rounds, 30, enforceWeight: false);
            inv.Equip(gun);
            return inv.EquippedStack;
        }

        private static ItemStack Give(PlayerInventory inv, string id)
        {
            ItemDef d = ItemDatabase.Get(id);
            if (d == null) return null;
            inv.Inventory.Add(d, 1, enforceWeight: false);
            return Newest(inv, id);
        }

        private static ItemStack Newest(PlayerInventory inv, string id)
        {
            List<ItemStack> stacks = inv.Inventory.Stacks;
            for (int i = stacks.Count - 1; i >= 0; i--)
                if (stacks[i].Def != null && stacks[i].Def.Id == id) return stacks[i];
            return null;
        }

        // Fits a mod from the pack the way the inventory's Attach verb does.
        private static bool Fit(PlayerInventory inv, ItemStack gun, string mod)
        {
            ItemStack stack = Give(inv, mod);
            if (stack == null) return false;
            WeaponItems.AttachTo(inv.Inventory, gun, stack, inv);
            inv.Inventory.RaiseChanged();
            return gun.Extra != null && gun.Extra.Mods.Contains(mod);
        }

        // Each attachment touches the gun, or an attachment that does (a can on the end of a long barrel), as the
        // floating-geometry report judges it, and is a plausible size for one.
        private void CheckSeated(string prefix, WeaponAttachments.Fitted fit, MeshRenderer gun)
        {
            var seated = new List<Bounds> { gun.bounds };
            var left = new List<MeshRenderer>();
            string odd = "";
            foreach (MeshRenderer r in fit.Renderers)
            {
                float across = r.bounds.size.magnitude;
                if (across < 0.6f && across > 0.005f) left.Add(r);
                else odd += (odd.Length > 0 ? ", " : "") + r.name + " (" + F3(across) + " m across)";
            }
            for (bool grew = true; grew && left.Count > 0;)
            {
                grew = false;
                for (int i = left.Count - 1; i >= 0; i--)
                {
                    Bounds b = left[i].bounds;
                    b.Expand(0.01f);
                    if (!seated.Exists(s => s.Intersects(b))) continue;
                    seated.Add(left[i].bounds);
                    left.RemoveAt(i);
                    grew = true;
                }
            }
            foreach (MeshRenderer r in left) odd += (odd.Length > 0 ? ", " : "") + r.name + " (touches nothing)";
            Check(prefix + "every attachment sits on the gun", odd.Length == 0, odd.Length > 0 ? "loose: " + odd : (seated.Count - 1) + " seated");
        }

        // How far a sight's top stands above the gun's own top where the sight sits (gun space; the gun's median top there).
        private static float SightRise(Transform held, ItemDef def, WeaponAttachments.Fitted fit)
        {
            GunShape shape = GunShape.Of(def, held.GetComponent<MeshFilter>().sharedMesh);
            foreach (MeshRenderer r in fit.Renderers)
            {
                if (r.name != "Attachment_mod_reddot" && r.name != "Attachment_mod_scope4x") continue;
                Mesh m = r.GetComponent<MeshFilter>().sharedMesh;
                Matrix4x4 toGun = fit.Root.worldToLocalMatrix * r.transform.localToWorldMatrix;
                Bounds mb = m.bounds;
                var b = new Bounds(toGun.MultiplyPoint3x4(mb.center), Vector3.zero);
                for (int i = 0; i < 8; i++)
                    b.Encapsulate(toGun.MultiplyPoint3x4(new Vector3(i % 2 == 0 ? mb.min.x : mb.max.x, (i / 2) % 2 == 0 ? mb.min.y : mb.max.y, i / 4 == 0 ? mb.min.z : mb.max.z)));
                return b.max.z - shape.TopAt(b.min.x, b.max.x, out _);
            }
            return 0f;
        }

        // The gun and its attachments in gun space (the muzzle at +X, whichever way round the model was made).
        private static Bounds DressedBounds(Transform held, ItemDef def, WeaponAttachments.Fitted fit)
        {
            Bounds b = GunShape.Of(def, held.GetComponent<MeshFilter>().sharedMesh).Box;
            foreach (MeshRenderer r in fit.Renderers)
            {
                Mesh m = r.GetComponent<MeshFilter>().sharedMesh;
                if (m == null) continue;
                Matrix4x4 toGun = fit.Root.worldToLocalMatrix * r.transform.localToWorldMatrix;
                Bounds mb = m.bounds;
                for (int i = 0; i < 8; i++)
                    b.Encapsulate(toGun.MultiplyPoint3x4(new Vector3(i % 2 == 0 ? mb.min.x : mb.max.x, (i / 2) % 2 == 0 ? mb.min.y : mb.max.y, i / 4 == 0 ? mb.min.z : mb.max.z)));
            }
            return b;
        }

        // Takes the guns, their rounds, every attachment and the batteries out of the pack, so the next gun has room.
        private static void Unkit(PlayerInventory inv)
        {
            List<ItemStack> stacks = inv.Inventory.Stacks;
            for (int i = stacks.Count - 1; i >= 0; i--)
            {
                ItemDef d = stacks[i].Def;
                if (d != null && (d.IsFirearm || d.Category == ItemCategory.Ammo || Ballistics.Mod(d.Id) != null || d.Gear == GearKind.Battery))
                    inv.Inventory.Remove(stacks[i], stacks[i].Count);
            }
        }

        // The held gun points its muzzle ahead (the way the figure faces), whichever way round its model was made.
        private void CheckMuzzleAhead(string prefix, Actor me, HumanoidRig rig)
        {
            GunShape shape = GunShape.Of(rig.HeldDef, rig.Held.GetComponent<MeshFilter>().sharedMesh);
            Vector3 muzzle = rig.Held.TransformPoint(Quaternion.Inverse(shape.ToGun) * shape.Muzzle);
            Vector3 butt = rig.Held.TransformPoint(Quaternion.Inverse(shape.ToGun) * new Vector3(shape.Box.min.x, shape.Box.center.y, shape.Box.center.z));
            float ahead = Vector3.Dot(muzzle - butt, me.transform.forward);
            Check(prefix + "held muzzle first", ahead > 0f, (shape.Reversed ? "model made end for end, turned round; " : "") + "muzzle " + F3(ahead) + " m ahead of the butt");
        }

        private static bool AllHidden(WeaponAttachments.Fitted fit)
        {
            foreach (MeshRenderer r in fit.Renderers) if (r != null && r.enabled) return false;
            return true;
        }

        private static bool AllShown(WeaponAttachments.Fitted fit)
        {
            foreach (MeshRenderer r in fit.Renderers) if (r != null && !r.enabled) return false;
            return true;
        }

        // Where the next round would end without spread, as WeaponTactical works it out.
        private static Vector3 ShotEnd(PlayerCombat combat, Actor me)
        {
            combat.ShotLine(out Vector3 from, out Vector3 dir);
            return CastPast(me, from, dir, WeaponTactical.LaserRange, out Vector3 end) ? end : from + dir * WeaponTactical.LaserRange;
        }

        private static readonly RaycastHit[] s_AttachHits = new RaycastHit[24];

        private static bool CastPast(Actor me, Vector3 from, Vector3 dir, float range, out Vector3 point)
        {
            int n = Physics.RaycastNonAlloc(from, dir, s_AttachHits, range, CombatResolver.ShotMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            point = from + dir * range;
            for (int i = 0; i < n; i++)
            {
                if (s_AttachHits[i].distance >= best || s_AttachHits[i].collider.transform.IsChildOf(me.transform)) continue;
                best = s_AttachHits[i].distance;
                point = s_AttachHits[i].point;
            }
            return best < float.MaxValue;
        }

        // Holds the right button (aiming down the sights) in the shoulder view until the gun is up, then checks.
        private IEnumerator AimAndCheck(string name, PlayerCombat combat, PlayerAvatar avatar, Func<(bool ok, string detail)> check)
        {
            (bool ok, string detail) result = (false, "never aimed");
            bool aimed = false;
            yield return HoldButton(MouseButton.Right, 1.2f, () =>
            {
                aimed |= combat.AimingDownSights && avatar.Animator != null && avatar.Animator.Aiming;
                if (aimed) result = check();
            });
            Check(name, aimed && result.ok, (aimed ? "" : "the gun never came up; ") + result.detail);
            yield return Wait(0.6f);
        }

        // How bright the frame is where the weapon light's beam lands (with or without it on), seen from beside the survivor.
        private static float BeamSpotLuma(WeaponTactical tactical, WeaponAttachments.Fitted fit, Actor me, HumanoidRig rig, out string where)
        {
            where = "nowhere";
            Transform lens = fit != null ? fit.LightLens : null;
            if (lens == null) return 0f;
            Vector3 dir = tactical.LightLit ? tactical.LightDirection : lens.forward;
            if (!CastPast(me, lens.position, dir, WeaponTactical.LightRange, out Vector3 spot)) return 0f;
            where = Vector3.Distance(lens.position, spot).ToString("0.0") + " m from the lens";
            Vector3 eye = me.transform.position + Vector3.up * 2.3f + me.transform.right * 1.1f - me.transform.forward * 0.3f;
            var go = new GameObject("[Smoke] spot camera");
            Camera c = go.AddComponent<Camera>();
            if (Camera.main != null) c.CopyFrom(Camera.main);
            c.orthographic = false;
            c.fieldOfView = 25f;
            c.nearClipPlane = 0.05f;
            c.farClipPlane = 200f;
            c.cullingMask = ~0;
            c.enabled = false;
            go.transform.position = eye;
            go.transform.LookAt(spot);
            const int W = 160, H = 90, Patch = 16;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
            rt.Create();
            RenderTexture active = RenderTexture.active;
            float luma = 0f;
            try
            {
                c.aspect = W / (float)H;
                for (int pass = 0; pass < 2; pass++)
                {
                    var request = new RenderPipeline.StandardRequest { destination = rt };
                    if (RenderPipeline.SupportsRenderRequest(c, request)) RenderPipeline.SubmitRenderRequest(c, request);
                    else { c.targetTexture = rt; c.Render(); c.targetTexture = null; }
                }
                RenderTexture.active = rt;
                var tex = new Texture2D(Patch, Patch, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect((W - Patch) / 2, (H - Patch) / 2, Patch, Patch), 0, 0, false);
                tex.Apply(false);
                Color32[] px = tex.GetPixels32();
                for (int i = 0; i < px.Length; i++) luma += (0.2126f * px[i].r + 0.7152f * px[i].g + 0.0722f * px[i].b) / 255f;
                luma /= px.Length;
                Destroy(tex);
            }
            finally
            {
                RenderTexture.active = active;
                rt.Release();
                Destroy(rt);
                Destroy(go);
            }
            return luma;
        }

        // Pictures of the gun from beside the survivor and of the scene, and the bodycam's view.
        private IEnumerator Pictures(string label, Actor me, HumanoidRig rig, IsoCameraFollow cam, bool beam, bool bodycam = true)
        {
            if (string.IsNullOrEmpty(s_ShotDir) || rig.Held == null) yield break;
            yield return new WaitForEndOfFrame();
            Bounds gun = rig.Held.GetComponent<MeshRenderer>().bounds;
            if (rig.HeldAttachments != null) foreach (MeshRenderer r in rig.HeldAttachments.Renderers) gun.Encapsulate(r.bounds);
            Transform t = me.transform;
            float span = Mathf.Max(0.4f, gun.size.magnitude);
            ShotFrom("attachments-" + label + "-right", gun.center + t.right * span * 1.6f + Vector3.up * span * 0.25f, gun.center, 32f);
            ShotFrom("attachments-" + label + "-left", gun.center - t.right * span * 1.6f + Vector3.up * span * 0.4f + t.forward * span * 0.3f, gun.center, 32f);
            if (beam) ShotFrom("attachments-" + label + "-scene", t.position + Vector3.up * 2.4f - t.forward * 2.2f + t.right * 1.4f,
                _board != null ? _board.position - Vector3.up * 0.6f : t.position + t.forward * 4f, 55f);
            if (!bodycam) yield break;
            CameraView was = cam.View;
            cam.SetView(CameraView.Bodycam);
            yield return Wait(0.6f);
            Shot("attachments-" + label + "-bodycam", Camera.main);
            cam.SetView(was);
            yield return Wait(0.4f);
        }

        // Pictures with the gun up (the right button held): through the shoulder camera and the bodycam, and of the scene.
        private IEnumerator AimedPictures(string label, Actor me, PlayerCombat combat, PlayerAvatar avatar, IsoCameraFollow cam)
        {
            if (string.IsNullOrEmpty(s_ShotDir)) yield break;
            CameraView was = cam.View;
            foreach (CameraView view in new[] { CameraView.ThirdPerson, CameraView.Bodycam })
            {
                cam.SetView(view);
                yield return Wait(0.5f);
                float since = -1f;
                bool taken = false;
                yield return HoldButton(MouseButton.Right, 1.8f, () =>
                {
                    if (taken || !(combat.AimingDownSights && avatar.Animator != null && avatar.Animator.Aiming)) return;
                    if (since < 0f) since = Time.realtimeSinceStartup;
                    if (Time.realtimeSinceStartup - since < 0.9f) return;
                    taken = true;
                    string name = "attachments-" + label + "-aimed-" + (view == CameraView.Bodycam ? "bodycam" : "shoulder");
                    Shot(name, Camera.main);
                    if (view != CameraView.ThirdPerson) return;
                    Transform t = me.transform;
                    ShotFrom("attachments-" + label + "-aimed-scene", t.position + Vector3.up * 2.4f - t.forward * 2.2f + t.right * 1.4f,
                        _board != null ? _board.position : t.position + t.forward * 4f, 55f);
                });
                yield return Wait(0.5f);
            }
            cam.SetView(was);
            yield return Wait(0.4f);
        }

        // Every look of the gun in hand in turn: each held muzzle first, the kit seated on it.
        private IEnumerator EveryLook(string prefix, string id, Actor me, HumanoidRig rig, IsoCameraFollow cam)
        {
            ItemDef def = rig.HeldDef;
            int looks = ItemMeshBaker.VariantCount(def);
            for (int v = 0; v < looks && looks > 1; v++)
            {
                if (!ForceLook(rig, def, v))
                {
                    Check(prefix + "look " + (v + 1) + " in hand", false, "no seed picks it");
                    yield break;
                }
                yield return null;
                yield return null;
                string L = prefix + "look " + (v + 1) + " of " + looks + ": ";
                CheckMuzzleAhead(L, me, rig);
                if (rig.HeldAttachments != null) CheckSeated(L, rig.HeldAttachments, rig.Held.GetComponent<MeshRenderer>());
                else Check(L + "kit built on it", false);
                yield return Pictures(id + "-look" + (v + 1), me, rig, cam, false, bodycam: false);
            }
        }

        // Puts look `variant` of the gun in the figure's hand: the rig picks a look from its own seed, so give it one that picks
        // this look (HumanoidRig.SetHeldItem's hash), then hand the gun over again.
        private static bool ForceLook(HumanoidRig rig, ItemDef def, int variant)
        {
            int looks = ItemMeshBaker.VariantCount(def);
            FieldInfo seed = typeof(HumanoidRig).GetField("_lookSeed", BindingFlags.Instance | BindingFlags.NonPublic);
            if (seed == null || seed.FieldType != typeof(uint) || looks <= 1) return false;
            uint name = 2166136261u;
            foreach (char ch in def.Id) name = (name ^ ch) * 16777619u;
            // Odd seeds, as the rig's are; past 16 bits, so the hash's fold (pick >> 16) varies too, or an even number of
            // looks would only ever reach half of them.
            for (uint s = 1; s < 1u << 20; s += 2)
            {
                uint pick = name ^ s;
                if ((int)((pick ^ (pick >> 16)) % (uint)looks) != variant) continue;
                seed.SetValue(rig, s);
                HoldStyle style = rig.HeldStyle;
                rig.SetHeldItem(null, HoldStyle.None);
                rig.SetHeldItem(def, style);
                return true;
            }
            return false;
        }

        // The floating-geometry report on the world as it stands: nothing of the gun's kit, its light or its laser may be flagged.
        private void FloatingCheck(string prefix, string label)
        {
#if UNITY_EDITOR
            string dir = !string.IsNullOrEmpty(s_ShotDir) ? s_ShotDir : Path.Combine(Application.temporaryCachePath, "smoke");
            string file = Path.Combine(dir, "attachments-floating-" + label + ".txt");
            var ours = new List<string>();
            try
            {
                foreach (FloatingGeometryReport.Finding f in FloatingGeometryReport.Check(file))
                    if (f.Key.Contains("Attachment") || f.Key.Contains("Held_") || f.Key.Contains("Weapon devices") || f.Key.Contains("Laser"))
                        ours.Add(f.Key);
                Check(prefix + "nothing of the gun's kit floats", ours.Count == 0, ours.Count == 0 ? "report: " + file : string.Join("; ", ours));
            }
            catch (Exception e) { Check(prefix + "floating-geometry report", false, e.GetType().Name + ": " + e.Message); }
#else
            Skip(prefix + "floating-geometry report", "editor only");
#endif
        }

        private static string F3(float v) => v.ToString("0.000", CultureInfo.InvariantCulture);
        #endregion
    }
}
