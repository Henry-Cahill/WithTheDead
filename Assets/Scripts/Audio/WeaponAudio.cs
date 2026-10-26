using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The sounds of the fight that no single weapon owns. Every resolved round (single-player and the server; a
    /// networked client feeds in its own shots and the server's shot effects) may strike a body (a wet thud), strike the
    /// world — earth, stone, steel, timber or glass, by what the collider is made of, hard surfaces now and then sending
    /// it whining off — or pass close by the survivor's head: the crack of a supersonic round or the zip of a pistol
    /// bullet, heard where it went by, so you know you are being shot at and from which side.
    /// </summary>
    public static class WeaponAudio
    {
        private const float WhizRadius = 4f, MuzzleClear = 3f, ImpactRange = 45f, SpeedOfSound = 343f;
        private const int WorldMask = 1 << 0;
        private const string Unknown = "";

        private static bool s_Hooked;
        private static int s_Frame = -1, s_Flesh, s_Impacts;
        private static float s_LastWhiz = -10f;
        private static readonly Dictionary<Collider, string> s_Named = new Dictionary<Collider, string>(256);

        /// <summary>Hooks the combat events (once per run of the game).</summary>
        public static void Ensure()
        {
            if (s_Hooked) return;
            s_Hooked = true;
            CombatResolver.ShotResolved += Shot;
        }

        /// <summary>The sounds of one resolved round (a networked client also calls it for its own shots).</summary>
        public static void Shot(ShotRequest req, ShotResult r)
        {
            if (!Sfx.Enabled) return;
            Frame();
            if (r.Hit && (r.TargetActor != null || r.Target != null)) Flesh(r.End);
            else if (r.Hit) Impact(r.End, req.Direction);
            if (FiredByMe(req)) return;
            Vector3 end = r.Hit ? r.End : req.Origin + req.Direction.normalized * req.Range;
            NearMiss(req.Origin, end, req.AmmoId, req.WeaponId);
        }

        /// <summary>Another player's shot as a networked client hears it (the server says where it went).</summary>
        public static void RemoteShot(Vector3 from, Vector3 to, bool flesh, string weaponId)
        {
            if (!Sfx.Enabled) return;
            Frame();
            if (flesh) Flesh(to);
            else Impact(to, to - from);
            NearMiss(from, to, null, weaponId);
        }

        /// <summary>The reload for a gun: magazine (9 mm), rifle magazine and charging handle, shotgun tube and pump,
        /// stripper clip and bolt; the generic one for anything else.</summary>
        public static string ReloadSound(ItemDef gun)
        {
            switch (gun != null ? Ballistics.CaliberOfGun(gun) : null)
            {
                case "9x19": return SfxIds.ReloadPistol;
                case "545": return SfxIds.ReloadRifle;
                case "12g": return SfxIds.ReloadShotgun;
                case "762r": return SfxIds.ReloadBolt;
                default: return SfxIds.Reload;
            }
        }

        /// <summary>
        /// The range to play a gun's report at. Up close it is loud and falls away over this; beyond 55–80 m the distant
        /// version takes over (see the Weapons recipes), which carries 450–650 m on its own.
        /// </summary>
        public static float Reach(string gunSound)
        {
            switch (gunSound)
            {
                case SfxIds.GunPistol: return 110f;
                case SfxIds.GunRifle:
                case SfxIds.GunShotgun: return 130f;
                case SfxIds.GunSniper: return 160f;
                case SfxIds.GunSuppressed: return 35f;
                case SfxIds.GunSuppressedRifle: return 55f;
                case SfxIds.GunFlare: return 90f;
                case SfxIds.Explosion: return 250f;
                default: return 120f;
            }
        }

        /// <summary>
        /// Plays a report (a shot, a blast) where it went off, heard when its sound would get here: at 343 m/s a shot
        /// across the valley lands a second after its flash, and a near miss cracks past before the report of the rifle
        /// that fired it.
        /// </summary>
        public static void Report(string id, Vector3 at, float volume, float pitch)
        {
            float delay = SfxMixer.DistanceToEars(at) / SpeedOfSound;
            if (delay < 0.02f) Sfx.Play(id, at, volume, pitch, Reach(id));
            else Sfx.PlayDelayed(id, at, delay, volume, pitch, Reach(id));
        }

        private static void Frame()
        {
            int f = Time.frameCount;
            if (f == s_Frame) return;
            s_Frame = f;
            s_Flesh = 0;
            s_Impacts = 0;
        }

        private static bool FiredByMe(in ShotRequest req)
        {
            Actor me = Actor.LocalPlayer;
            return me != null && (req.ShooterActor == me || (req.Shooter != null && req.Shooter == me.gameObject));
        }

        // A round into a body: the shooter hears the hit land. A load of buckshot into one body is a wet thud or two, not seven.
        private static void Flesh(Vector3 at)
        {
            if (s_Flesh >= 2) return;
            s_Flesh++;
            Sfx.Play(SfxIds.HitFlesh, at, s_Flesh == 1 ? 0.75f : 0.5f, Random.Range(0.9f, 1.1f), 30f);
        }

        // A round into the world: what it struck, found by a short cast through the end of its path.
        private static void Impact(Vector3 at, Vector3 dir)
        {
            if (s_Impacts >= 3 || dir.sqrMagnitude < 1e-6f) return;
            if ((at - Sfx.ListenerPosition).sqrMagnitude > ImpactRange * ImpactRange) return;
            dir.Normalize();
            if (!Physics.Raycast(at - dir * 0.3f, dir, out RaycastHit hit, 0.6f, WorldMask, QueryTriggerInteraction.Ignore)) return;
            string id = SurfaceOf(hit);
            s_Impacts++;
            bool ringing = id == SfxIds.ImpactMetal || id == SfxIds.ImpactGlass;
            Vector3 p = hit.point + hit.normal * 0.05f;
            Sfx.Play(id, p, s_Impacts == 1 ? 0.7f : 0.45f, Random.Range(0.92f, 1.08f), ringing ? 45f : 38f);
            if ((id == SfxIds.ImpactConcrete || id == SfxIds.ImpactMetal) && Random.value < 0.15f)
                Sfx.Play(SfxIds.Ricochet, p, 0.5f, Random.Range(0.9f, 1.15f), 35f);
        }

        // A round passing by: where its path came closest to the ears, if that was close, and not our own.
        private static void NearMiss(Vector3 from, Vector3 to, string ammoId, string weaponId)
        {
            if (Actor.LocalPlayer == null && !ListenerRig.Active) return;
            Vector3 ears = Sfx.ListenerPosition;
            Vector3 path = to - from;
            float len2 = path.sqrMagnitude;
            if (len2 < 1f) return;
            float t = Vector3.Dot(ears - from, path) / len2;
            if (t <= 0f || t >= 1f) return;                                       // behind the gun, or it stopped short
            if ((ears - from).sqrMagnitude < MuzzleClear * MuzzleClear) return;   // beside the muzzle the blast is all there is
            Vector3 p = from + path * t;
            float d = Vector3.Distance(p, ears);
            if (d > WhizRadius) return;
            float now = Time.unscaledTime;
            if (now - s_LastWhiz < 0.05f) return;                                 // one per volley: buckshot passes together
            s_LastWhiz = now;
            Sfx.Play(Supersonic(ammoId, weaponId) ? SfxIds.BulletWhiz : SfxIds.BulletFlyby, p, Mathf.Lerp(1f, 0.4f, d / WhizRadius),
                Random.Range(0.93f, 1.07f), 30f);
        }

        // Rifle and sniper rounds (and slugs) break the sound barrier and crack past; 9 mm, buckshot and flares zip.
        private static bool Supersonic(string ammoId, string weaponId)
        {
            AmmoStats a = Ballistics.Ammo(ammoId);
            string cal = a != null ? a.Caliber : null;
            if (cal == null)
            {
                ItemDef gun = ItemDatabase.Get(weaponId);
                cal = gun != null ? Ballistics.CaliberOfGun(gun) : null;
            }
            if (cal == null) return true;
            if (cal == "9x19" || cal == "flare") return false;
            if (cal == "12g") return a != null && a.Pellets <= 1;
            return true;
        }

        private static string SurfaceOf(in RaycastHit hit)
        {
            Collider c = hit.collider;
            string id = Unknown;
            if (c != null && !s_Named.TryGetValue(c, out id))
            {
                id = Named(c);
                if (s_Named.Count > 2048) s_Named.Clear();
                s_Named[c] = id;
            }
            if (!string.IsNullOrEmpty(id)) return id;
            // Most of the world is plain box colliders: judge by where it was struck. Floors indoors are boards and
            // lino, the ground outdoors is earth; walls and ceilings are brick, block and plaster.
            if (hit.normal.y > 0.65f)
                return GameSession.IsHub || (Shelter.Count > 0 && Shelter.IsSheltered(hit.point + Vector3.up)) ? SfxIds.ImpactWood : SfxIds.ImpactDirt;
            return SfxIds.ImpactConcrete;
        }

        // What a collider is made of, read off its renderer's material and texture and its own name ("" when nothing says).
        private static string Named(Collider c)
        {
            if (c is TerrainCollider) return SfxIds.ImpactDirt;
            string s = c.name;
            Renderer r = c.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                Material m = r.sharedMaterial;
                s += " " + m.name;
                Texture t = m.mainTexture;
                if (t != null) s += " " + t.name;
            }
            s = s.ToLowerInvariant();
            if (Any(s, "glass", "window", "windshield", "windscreen", "bottle", "mirror")) return SfxIds.ImpactGlass;
            if (Any(s, "metal", "steel", "iron", "raid_corr", "corrugat", "galvan", "chrome", "vehicle", "truck", "wreck", "barrel",
                "dumpster", "locker", "container", "fridge", "pipe", "drone", "lamp", "pole", "tank")) return SfxIds.ImpactMetal;
            if (Any(s, "wood", "plank", "timber", "trunk", "bark", "crate", "pallet", "table", "shelf", "shelves", "cabinet", "desk",
                "chair", "bench", "fence", "deck", "porch", "barn", "shed", "furniture", "raid_ext0", "lino", "carpet")
                || (Any(s, "door") && !Any(s, "outdoor", "indoor"))) return SfxIds.ImpactWood;
            if (Any(s, "concrete", "asphalt", "road", "brick", "stone", "rock", "boulder", "cliff", "tile", "slab", "pavement", "curb", "kerb",
                "cement", "cinder", "block", "wall", "ceiling", "stucco", "plaster", "gravel", "raid_ext")) return SfxIds.ImpactConcrete;
            if (Any(s, "grass", "dirt", "ground", "terrain", "mud", "sand", "soil", "snow", "mulch", "field", "earth", "hay", "bush", "hedge"))
                return SfxIds.ImpactDirt;
            if (Any(s, "tree", "log") && !Any(s, "street")) return SfxIds.ImpactWood;
            return Unknown;
        }

        private static bool Any(string s, params string[] parts)
        {
            for (int i = 0; i < parts.Length; i++)
                if (s.IndexOf(parts[i], System.StringComparison.Ordinal) >= 0) return true;
            return false;
        }
    }
}
