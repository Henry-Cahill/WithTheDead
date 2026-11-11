using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CloneZ
{
    /// <summary>
    /// <c>guns</c>: the period guns (ZoneContent.KentuckyGuns and ZoneGuns). First on paper: each one's stats, its rounds in
    /// its calibre, a model of its own the right length, a hold of its own, a report and a reload of its own that the
    /// synthesiser renders, a trader's price and a place in the loot. Each kind of building that keeps guns (a farmhouse's gun
    /// cabinet, a gun shop's counter, a police station's weapons locker) has a table with guns of its own, which its rooms put
    /// in and which, rolled, turns them up; the Kentucky map keeps the military gun locker out of its buildings, and the Zone's
    /// caches hold its Soviet guns; every gunsmith loyalty level sells guns of its own. Then into a raid with each gun in turn,
    /// in every look it has: in the hand muzzle first, the right fist on its grip and the left hand (the IK) on its hold (the
    /// forend, pump or handguard, within the stance's reach), a sight seated on it, a round fired, the trigger held (a burst
    /// from the automatics, one round from the rest), and a reload. With <c>-clonez-smoke-shots &lt;dir&gt;</c>, the guns'
    /// models and generated looks laid out side by side, and each gun in the hands from either side.
    /// </summary>
    public sealed partial class SmokeTest
    {
        private static IEnumerable<string> PeriodGuns => ZoneContent.KentuckyGuns.Concat(ZoneContent.ZoneGuns);

        private static readonly ContainerType[] BuildingGunTables = { ContainerType.GunCabinet, ContainerType.GunCounter, ContainerType.PoliceLocker };

        private IEnumerator GunsRun()
        {
            GunsOnPaper();
            GunsInBuildings();
            GunsAtTraders();

            yield return ToHub();
            yield return Deploy(RaidKind.Operative);
            Actor me = Actor.LocalPlayer;
            var inv = me != null ? me.GetComponent<PlayerInventory>() : null;
            var avatar = me != null ? me.GetComponent<PlayerAvatar>() : null;
            var combat = me != null ? me.GetComponent<PlayerCombat>() : null;
            var ik = me != null ? me.GetComponent<WeaponHandIK>() : null;
            IsoCameraFollow cam = IsoCameraFollow.Instance;
            Check("guns: the survivor, their hands and their kit", inv != null && avatar != null && avatar.Rig != null && combat != null && ik != null && cam != null);
            if (inv == null || avatar == null || avatar.Rig == null || combat == null || cam == null) yield break;
            me.Untargetable = true;   // the dead mustn't end the run
            ReleaseAll();
            PointAtMiddle();
            GunsSky();
            yield return PlaceBoard(me, cam);
            var setAside = new List<GameObject>();
            SetPeopleAside(me, setAside);
            yield return GunGallery(me);
            foreach (string id in PeriodGuns)
            {
                SetPeopleAside(me, setAside);
                yield return GunPass(me, inv, avatar, combat, ik, cam, id);
            }
            foreach (GameObject go in setAside) if (go != null) go.SetActive(true);
            Unkit(inv);
            if (_board != null) Destroy(_board.gameObject);
            cam.SetView(CameraView.Isometric);
        }

        #region On paper
        private void GunsOnPaper()
        {
            var synth = new SfxSynth();
            var reports = new Dictionary<string, string>();
            float quietest = float.MaxValue;
            string quietestId = null;
            foreach (string id in PeriodGuns)
            {
                string P = id + ": ";
                ItemDef def = ItemDatabase.Get(id);
                GunStats g = Ballistics.Gun(id);
                Check(P + "a firearm with stats of its own", def != null && def.IsFirearm && g != null,
                    def == null ? "no such item" : g == null ? "no GunStats" : def.DisplayName + ", " + g.Caliber);
                if (def == null || g == null) continue;

                AmmoStats round = Ballistics.Ammo(def.AmmoId);
                Check(P + "its rounds exist, in its calibre", ItemDatabase.Get(def.AmmoId) != null && round != null && round.Caliber == g.Caliber
                    && Ballistics.Accepts(def, def.AmmoId),
                    def.AmmoId + (round != null ? " (" + round.Caliber + ", damage " + round.Damage + ", penetration " + round.Penetration + ")" : ""));

                bool handgun = !def.TwoHanded;
                ItemModelDesc model = ItemModels.Describe(def);
                ItemMeshBaker.Baked baked = ItemMeshBaker.Get(def, ItemMeshBaker.Lod.Near, 0);
                float len = baked != null ? baked.Bounds.size.x : 0f;
                int looks = GeneratedItemMeshes.VariantCount(def);
                Check(P + "a model of its own, " + (handgun ? "a handgun's" : "a long gun's") + " length",
                    model != null && model.Parts.Count >= 20 && baked != null
                    && (handgun ? len > 0.15f && len < GunShape.PistolLength : len >= 0.6f && len < 1.35f),
                    F3(len) + " m, " + (model != null ? model.Parts.Count : 0) + " parts; " + (looks > 0 ? looks + " generated looks" : "no generated look yet"));

                GunHold h = g.Hold;
                // The off hand a hand's width ahead of the grip at least (0.08 of a long gun's length is about 0.08-0.1 m).
                bool holdOk = h.IsSet && In01(h.GripX) && In01(h.GripZ)
                    && (handgun || In01(h.SupportX) && In01(h.SupportZ) && h.SupportX > h.GripX + 0.08f);
                Check(P + "a hold of its own", holdOk,
                    "grip " + F3(h.GripX) + ", " + F3(h.GripZ) + (handgun ? "" : "; off hand " + F3(h.SupportX) + ", " + F3(h.SupportZ)));

                string report = WeaponAudio.ShotSound(def), reload = WeaponAudio.ReloadSound(def);
                bool own = report == g.Sound && reload == g.ReloadSound && synth.Has(report) && synth.Has(reload) && !reports.ContainsKey(report);
                Check(P + "a report and a reload of its own", own,
                    report + (reports.TryGetValue(report, out string twin) ? " (also " + twin + "'s)" : "") + ", " + reload);
                reports[report] = id;
                float[] shot = synth.RenderSamples(report, 0, out int channels, out int rate);
                float seconds = shot != null ? shot.Length / (float)(channels * rate) : 0f, rms = Rms(shot);
                float[] loading = synth.RenderSamples(reload, 0, out int lc, out int lr);
                Check(P + "the report and the reload render", seconds > 0.5f && seconds < 3.5f && rms > 0.005f && Rms(loading) > 0.002f,
                    "report " + F3(seconds) + " s, rms " + F3(rms) + "; reload " + F3(loading != null ? loading.Length / (float)(lc * lr) : 0f) + " s");
                if (def.FirearmNoise < quietest) { quietest = def.FirearmNoise; quietestId = id; }

                bool sold = TraderCatalog.All.Any(t => t.Offers.Any(o => o.ItemId == id));
                var places = new List<string>();
                foreach (ContainerType t in BuildingGunTables.Append(ContainerType.GunLocker))
                {
                    ItemDatabase.LootTable table = ItemDatabase.TableOf(t);
                    if (table?.Entries != null && table.Entries.Any(e => e.Id == id)) places.Add(t.ToString());
                }
                Check(P + "a trader's price and a place in the loot", sold && places.Count > 0, (sold ? "sold" : "not sold") + "; found in " +
                    (places.Count > 0 ? string.Join(", ", places) : "nothing"));
            }
            Check("guns: the .22 is the quietest gun of them", quietestId == "rifle22", quietestId + " (noise " + quietest + ")");
        }

        private void GunsInBuildings()
        {
            var guns = new Dictionary<ContainerType, HashSet<string>>();
            foreach (ContainerType t in BuildingGunTables.Append(ContainerType.GunLocker)) guns[t] = GunsIn(t);

            // Each kind of building that keeps guns has guns of its own.
            foreach (ContainerType t in BuildingGunTables)
            {
                var only = new HashSet<string>(guns[t]);
                foreach (ContainerType o in BuildingGunTables) if (o != t) only.ExceptWith(guns[o]);
                ItemDatabase.LootTable table = ItemDatabase.TableOf(t);
                Check("guns: a " + Noun(t) + " holds guns of its own", table != null && guns[t].Count >= 3 && only.Count >= 1,
                    string.Join(", ", guns[t]) + "; only here: " + (only.Count > 0 ? string.Join(", ", only) : "none"));
            }
            Check("guns: the farmhouse cabinet, the gun shop and the police each list their own",
                SameSet(guns[ContainerType.GunCabinet], ZoneContent.FarmhouseGuns) && SameSet(guns[ContainerType.GunCounter], ZoneContent.GunShopGuns)
                && SameSet(guns[ContainerType.PoliceLocker], ZoneContent.PoliceGuns));
            Check("guns: the Zone's military caches hold its Soviet guns", ZoneContent.ZoneGuns.All(guns[ContainerType.GunLocker].Contains),
                string.Join(", ", guns[ContainerType.GunLocker].Where(ZoneContent.ZoneGuns.Contains)));

            // Rolled, each table turns up every gun it lists and only those (the Central Economy kept out of it).
            Func<string, bool> gate = ItemDatabase.LootGate;
            Action<string, int> rolled = ItemDatabase.LootRolled;
            ItemDatabase.LootGate = null;
            ItemDatabase.LootRolled = null;
            try
            {
                foreach (ContainerType t in BuildingGunTables)
                {
                    var rng = new System.Random(1993 + (int)t);
                    var found = new HashSet<string>();
                    int containers = 400, withGun = 0;
                    for (int i = 0; i < containers; i++)
                    {
                        var into = new Inventory(999f);
                        ItemDatabase.RollLoot(t, rng, into);
                        bool any = false;
                        foreach (ItemStack s in into.Stacks)
                            if (s.Def != null && s.Def.IsFirearm) { found.Add(s.Def.Id); any = true; }
                        if (any) withGun++;
                    }
                    Check("guns: a " + Noun(t) + " turns up its guns when searched", found.SetEquals(guns[t]) && withGun > containers / 10,
                        withGun + " of " + containers + " with a gun; " + string.Join(", ", found));
                }
            }
            finally
            {
                ItemDatabase.LootGate = gate;
                ItemDatabase.LootRolled = rolled;
            }

            // The buildings put those containers in their rooms.
            ContainerType[] farmLiving = MapGameplay.PoolFor(MapGameplay.Farmhouse, KentuckyMap.RoomKinds.Living, false);
            Check("guns: a farmhouse's living room starts with its gun cabinet", farmLiving.Length > 0 && farmLiving[0] == ContainerType.GunCabinet,
                string.Join(", ", farmLiving));
            ContainerType[] shopFloor = MapGameplay.PoolFor(MapGameplay.Purposes.Guns, KentuckyMap.RoomKinds.Sales, false);
            Check("guns: a gun shop's floor is its counters", shopFloor.Length > 0 && shopFloor[0] == ContainerType.GunCounter, string.Join(", ", shopFloor));
            ContainerType[] policeStore = MapGameplay.PoolFor(MapGameplay.Purposes.Police, KentuckyMap.RoomKinds.Storage, false);
            Check("guns: a police station's storeroom is its weapons lockers", policeStore.Length > 0 && policeStore[0] == ContainerType.PoliceLocker,
                string.Join(", ", policeStore));
            Check("guns: a town home keeps a gun cabinet only where the Zone is well stocked",
                !MapGameplay.PoolFor(MapGameplay.Purposes.Home, KentuckyMap.RoomKinds.Living, false).Contains(ContainerType.GunCabinet)
                && MapGameplay.PoolFor(MapGameplay.Purposes.Home, KentuckyMap.RoomKinds.Living, true).Contains(ContainerType.GunCabinet));

            // No Kentucky building keeps the military gun locker any more: its guns are the Zone's.
            var military = new List<string>();
            string[] rooms = typeof(KentuckyMap.RoomKinds).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue()).Append(null).ToArray();
            foreach (FieldInfo f in typeof(MapGameplay.Purposes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!f.IsLiteral || f.FieldType != typeof(string)) continue;
                string purpose = (string)f.GetRawConstantValue();
                foreach (string room in rooms)
                    foreach (bool rich in new[] { false, true })
                        if (MapGameplay.PoolFor(purpose, room, rich).Contains(ContainerType.GunLocker)) military.Add(purpose + "/" + (room ?? "any"));
            }
            Check("guns: no Kentucky building keeps the Zone's military gun locker", military.Count == 0, string.Join(", ", military.Distinct()));
        }

        private void GunsAtTraders()
        {
            TraderDef smith = TraderCatalog.Get(TraderCatalog.Gunsmith);
            Check("guns: the gunsmith", smith != null);
            if (smith == null) return;
            var byLevel = new SortedDictionary<int, HashSet<string>>();
            foreach (TraderOffer o in smith.Offers)
            {
                ItemDef d = ItemDatabase.Get(o.ItemId);
                if (d == null || !d.IsFirearm) continue;
                if (!byLevel.TryGetValue(o.Loyalty, out HashSet<string> set)) byLevel[o.Loyalty] = set = new HashSet<string>();
                set.Add(o.ItemId);
            }
            for (int level = 1; level <= smith.Levels.Count; level++)
            {
                byLevel.TryGetValue(level, out HashSet<string> set);
                var elsewhere = new HashSet<string>(byLevel.Where(kv => kv.Key != level).SelectMany(kv => kv.Value));
                int own = set != null ? set.Count(id => !elsewhere.Contains(id)) : 0;
                Check("guns: the gunsmith's loyalty " + level + " sells guns of its own", own >= 1, set != null ? string.Join(", ", set) : "none");
            }
            foreach (string id in ZoneContent.ZoneGuns)
            {
                TraderOffer o = smith.Offers.FirstOrDefault(x => x.ItemId == id && x.Price > 0);
                Check("guns: the gunsmith sells the " + id + " from an upper tier", o != null && o.Loyalty >= 2,
                    o != null ? "loyalty " + o.Loyalty + ", " + o.Price.ToString("N0", CultureInfo.InvariantCulture) : "not sold");
            }
        }

        private static HashSet<string> GunsIn(ContainerType t)
        {
            var set = new HashSet<string>();
            ItemDatabase.LootTable table = ItemDatabase.TableOf(t);
            if (table?.Entries == null) return set;
            foreach (ItemDatabase.LootEntry e in table.Entries)
            {
                ItemDef d = ItemDatabase.Get(e.Id);
                if (d != null && d.IsFirearm && Ballistics.Gun(d.Id) != null) set.Add(e.Id);
            }
            return set;
        }

        private static string Noun(ContainerType t)
        {
            switch (t)
            {
                case ContainerType.GunCabinet: return "farmhouse gun cabinet";
                case ContainerType.GunCounter: return "gun shop counter";
                case ContainerType.PoliceLocker: return "police weapons locker";
                default: return t.ToString();
            }
        }

        private static bool SameSet(HashSet<string> a, string[] b) => a.SetEquals(b);
        private static bool In01(float v) => v > 0f && v < 1f;

        private static float Rms(float[] x)
        {
            if (x == null || x.Length == 0) return 0f;
            double s = 0;
            for (int i = 0; i < x.Length; i++) s += x[i] * x[i];
            return (float)Math.Sqrt(s / x.Length);
        }
        #endregion

        #region In the hands
        private IEnumerator GunPass(Actor me, PlayerInventory inv, PlayerAvatar avatar, PlayerCombat combat, WeaponHandIK ik, IsoCameraFollow cam, string id)
        {
            string P = id + ": ";
            HumanoidRig rig = avatar.Rig;
            ItemDef def = ItemDatabase.Get(id);
            GunStats stats = Ballistics.Gun(id);
            if (def == null || stats == null) yield break;
            Unkit(inv);
            ItemDef rounds = ItemDatabase.Get(def.AmmoId);
            inv.Inventory.Add(def, 1, enforceWeight: false);
            if (rounds != null) inv.Inventory.Add(rounds, def.MagazineSize * 2 + 4, enforceWeight: false);
            inv.Equip(def);
            yield return null;
            yield return null;
            Check(P + "in hand", rig.HeldDef == def && rig.Held != null && combat.EquippedFirearm == def, rig.HeldDef != null ? rig.HeldDef.Id : "nothing");
            if (rig.Held == null) yield break;
            bool handgun = !def.TwoHanded;
            Check(P + "held as " + (handgun ? "a handgun" : "a long gun"), handgun == rig.HeldLength < GunShape.PistolLength, F3(rig.HeldLength) + " m");

            // Every look it has, in the hands: muzzle first, the fist on the grip, the off hand on the forend.
            int looks = ItemMeshBaker.VariantCount(def);
            for (int v = 0; v < looks; v++)
            {
                if (looks > 1 && !ForceLook(rig, def, v)) { Check(P + "look " + (v + 1) + " in hand", false, "no seed picks it"); break; }
                yield return null;
                yield return null;
                string L = looks > 1 ? P + "look " + (v + 1) + " of " + looks + ": " : P;
                CheckMuzzleAhead(L, me, rig);
                yield return CheckHands(L, rig, ik, stats, handgun);
                yield return GunPictures(id + (looks > 1 ? "-look" + (v + 1) : ""), me, rig);
            }

            // A sight on it, seated on the model (WeaponAttachments measures every gun's outline).
            ItemStack gun = inv.EquippedStack;
            string sight = handgun ? "mod_reddot" : "mod_scope4x";
            if (gun != null && Ballistics.Gun(id).Slots.Contains(ModSlot.Sight))
            {
                Check(P + "takes the " + sight, Fit(inv, gun, sight));
                yield return null;
                yield return null;
                WeaponAttachments.Fitted fit = rig.HeldAttachments;
                Check(P + "the sight is built on it", fit != null && fit.Renderers.Count == 1, fit != null ? fit.Renderers.Count + " built" : "none");
                if (fit != null && rig.Held != null)
                {
                    CheckSeated(P, fit, rig.Held.GetComponent<MeshRenderer>());
                    float rise = SightRise(rig.Held, def, fit);
                    Check(P + "the sight stands above the gun", rise > 0.015f, F3(rise) + " m above the gun where it sits");
                }
            }

            // A round, then the trigger held: a burst from an automatic, a single round from the rest.
            int fired = 0, reloads = 0;
            Action onFire = () => fired++, onReload = () => reloads++;
            combat.OnFire += onFire;
            combat.OnReload += onReload;
            try
            {
                yield return UntilGame(() => !combat.IsReloading, 15f);
                if (combat.LoadedAmmo <= 0)
                {
                    combat.TryReload();
                    yield return UntilGame(() => !combat.IsReloading, 15f);
                }
                yield return WaitGame(Mathf.Max(0.2f, def.FireCooldown + 0.1f));
                int before = combat.LoadedAmmo, f0 = fired;
                yield return Click(MouseButton.Left);
                yield return WaitGame(0.15f);
                Check(P + "fires a round", fired == f0 + 1 && combat.LoadedAmmo == before - 1,
                    before + " -> " + combat.LoadedAmmo + " in the magazine" + (fired == f0 ? FireBlockers(me) : ""));
                Check(P + "with its own report", WeaponAudio.ShotSound(def, combat.Build.Suppressed) == stats.Sound, WeaponAudio.ShotSound(def, combat.Build.Suppressed));

                yield return WaitGame(def.FireCooldown + 0.15f);
                int f1 = fired;
                yield return HoldButton(MouseButton.Left, 0.7f, null);
                int burst = fired - f1;
                Check(P + (stats.FullAuto ? "the trigger held fires a burst" : "the trigger held fires one round"),
                    stats.FullAuto ? burst >= 3 : burst == 1, burst + " rounds");

                yield return WaitGame(def.FireCooldown + 0.15f);
                int r0 = reloads, a0 = combat.LoadedAmmo;
                yield return Tap(Key.R);
                yield return UntilGame(() => reloads > r0 && !combat.IsReloading, def.ReloadTime * 2f + 2f);
                Check(P + "reloads, with its own sound", reloads > r0 && combat.LoadedAmmo > a0 && WeaponAudio.ReloadSound(def) == stats.ReloadSound,
                    a0 + " -> " + combat.LoadedAmmo + " in the magazine; " + WeaponAudio.ReloadSound(def));
            }
            finally
            {
                combat.OnFire -= onFire;
                combat.OnReload -= onReload;
            }
        }

        // The right fist closes on the gun where its hold says, and for a long gun the left hand (the IK) takes the forend.
        private IEnumerator CheckHands(string prefix, HumanoidRig rig, WeaponHandIK ik, GunStats stats, bool handgun)
        {
            MeshFilter mf = rig.Held.GetComponent<MeshFilter>();
            GunShape shape = mf != null ? GunShape.Of(rig.HeldDef, mf.sharedMesh) : null;
            if (shape == null) { Check(prefix + "its outline measured", false); yield break; }
            Bounds box = shape.Box;
            Vector3 grip = shape.ToGun * rig.Held.InverseTransformPoint(rig.GripR.position);
            float wantX = box.min.x + stats.Hold.GripX * box.size.x;
            Check(prefix + "the right fist closes on its grip", OnGun(shape, grip, 0.012f) && Mathf.Abs(grip.x - wantX) < 0.01f, Where(shape, grip));
            if (handgun) yield break;

            Vector3 supportWorld = rig.GripR.TransformPoint(rig.HeldSupport);
            Vector3 support = shape.ToGun * rig.Held.InverseTransformPoint(supportWorld);
            Check(prefix + "the off hand's hold is on the gun, a hand ahead of the grip", OnGun(shape, support, 0.012f) && support.x > grip.x + 0.10f, Where(shape, support));

            yield return UntilGame(() => ik == null || ik.Weight > 0.99f, 3f);
            yield return null;   // the bones as the last LateUpdate left them: posed, then the hand IK applied
            if (ik == null) { Check(prefix + "the left hand takes the hold (IK)", false, "no hand IK on the survivor"); yield break; }
            Transform handL = rig.Bone(HumanBone.HandL);
            Vector3 palmOffset = new Vector3(-HumanoidRig.GripOffset.x, HumanoidRig.GripOffset.y, HumanoidRig.GripOffset.z) * rig.Proportions.Hand;
            Vector3 palm = handL.position + handL.rotation * palmOffset;
            float miss = Vector3.Distance(palm, rig.GripR.TransformPoint(rig.HeldSupport));
            Check(prefix + "the left hand takes the hold (IK)", ik.Weight > 0.99f && miss < 0.05f, "IK weight " + F3(ik.Weight) + ", palm " + F3(miss) + " m off it");
        }

        // The raid's people wander up to the survivor; one at their elbow (or the name and prompt over them, under the cursor)
        // takes the clicks meant for the trigger. The gun passes want the room to themselves: anyone near is set aside, and put
        // back once the passes are done.
        private static void SetPeopleAside(Actor me, List<GameObject> setAside)
        {
            Vector3 here = me.transform.position;
            for (int i = HumanNpc.All.Count - 1; i >= 0; i--)
            {
                HumanNpc npc = HumanNpc.All[i];
                if (npc == null || !npc.gameObject.activeSelf || (npc.transform.position - here).sqrMagnitude > 30f * 30f) continue;
                npc.gameObject.SetActive(false);
                setAside.Add(npc.gameObject);
            }
        }

        // Why a trigger pull did nothing, for the check that missed it.
        private static string FireBlockers(Actor me)
        {
            var why = new List<string>();
            PlayerStats stats = me.GetComponent<PlayerStats>();
            if (stats != null && (stats.IsDead || stats.Incapacitated)) why.Add("down");
            if (UIState.GameplayBlocked) why.Add("a menu has the screen");
            if (UIState.BuildMode) why.Add("build mode");
            if (me.Surrendered || me.Restrained) why.Add("hands up or tied");
            UnityEngine.EventSystems.EventSystem events = UnityEngine.EventSystems.EventSystem.current;
            if (!ViewState.MouseLook && events != null && events.IsPointerOverGameObject()) why.Add("the cursor is over the UI");
            PlayerInventory inv = me.GetComponent<PlayerInventory>();
            if (inv != null && inv.EquippedStack != null && inv.EquippedStack.Def != null)
                why.Add("jam chance " + F3(Ballistics.Build(inv.EquippedStack).JamChance));
            return "; " + (why.Count > 0 ? string.Join(", ", why) : "nothing in the way");
        }

        // A point (gun space) within the gun's outline: along its length, and between its underside and its top there.
        private static bool OnGun(GunShape s, Vector3 p, float tolerance)
        {
            Bounds b = s.Box;
            if (p.x < b.min.x - tolerance || p.x > b.max.x + tolerance) return false;
            float top = s.TopAt(p.x - 0.015f, p.x + 0.015f, out _), bottom = s.BottomAt(p.x - 0.015f, p.x + 0.015f, out _);
            return p.z >= bottom - tolerance && p.z <= top + tolerance;
        }

        private static string Where(GunShape s, Vector3 p)
        {
            Bounds b = s.Box;
            float top = s.TopAt(p.x - 0.015f, p.x + 0.015f, out _), bottom = s.BottomAt(p.x - 0.015f, p.x + 0.015f, out _);
            return F3((p.x - b.min.x) / Mathf.Max(1e-4f, b.size.x)) + " of the way along, " + F3(p.z) + " m up between " + F3(bottom) + " and " + F3(top);
        }
        #endregion

        #region Pictures
        private static void GunsSky()
        {
            Climate.Winter = false;
            if (WeatherSystem.Instance != null) WeatherSystem.Instance.Restore(Weather.Clear, 0f, 0.2f, 0.2f);
            if (TimeOfDay.Instance != null) TimeOfDay.Instance.RestoreClock(TimeOfDay.Instance.Day, 12.5f);
        }

        // Every period gun's model and generated looks laid out on the ground beside the survivor, a row each, seen from above:
        // the hand-built model first, then each look the asset pipeline made of it.
        private IEnumerator GunGallery(Actor me)
        {
            if (string.IsNullOrEmpty(s_ShotDir)) yield break;
            var root = new GameObject("[Smoke] gun gallery").transform;
            Transform t = me.transform;
            Vector3 origin = t.position - t.forward * 2.5f + t.right * 1.5f;
            float ground = Physics.Raycast(origin + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 8f, Actor.SightMask, QueryTriggerInteraction.Ignore)
                ? hit.point.y : t.position.y;
            root.SetPositionAndRotation(new Vector3(origin.x, ground + 0.02f, origin.z), Quaternion.LookRotation(-t.right, Vector3.up));
            int row = 0, columns = 1;
            foreach (string id in PeriodGuns)
            {
                ItemDef def = ItemDatabase.Get(id);
                if (def == null) continue;
                if (ItemModels.TryBuild(def, root, out Transform model, out float baseHeight))
                    model.localPosition = new Vector3(0f, 0.05f, row * 0.45f);
                int looks = GeneratedItemMeshes.VariantCount(def);
                for (int v = 0; v < looks; v++)
                {
                    ItemMeshBaker.Baked baked = ItemMeshBaker.Get(def, ItemMeshBaker.Lod.Near, v);
                    if (baked == null || baked.Mesh == null) continue;
                    var go = new GameObject(id + "@" + (v + 1));
                    go.transform.SetParent(root, false);
                    go.transform.localPosition = new Vector3((v + 1) * 1.45f, 0.05f, row * 0.45f);
                    go.AddComponent<MeshFilter>().sharedMesh = baked.Mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterials = baked.Materials;
                    columns = Mathf.Max(columns, v + 2);
                }
                row++;
            }
            // ShotFrom draws its own camera there and then; a batch editor draws no screen, so no end of frame comes.
            yield return null;
            yield return null;
            Vector3 middle = root.TransformPoint(new Vector3((columns - 1) * 0.725f, 0f, (row - 1) * 0.225f));
            // Indoors the camera is above the ceiling: everything more than 1.5 m over the guns is clipped away.
            ShotFrom("guns-gallery", middle + Vector3.up * 5.2f - t.right * 0.01f, middle, 52f, 5.2f - 1.5f);
            ShotFrom("guns-gallery-near", root.TransformPoint(new Vector3(0.6f, 2.4f, (row - 1) * 0.225f)), root.TransformPoint(new Vector3(0.6f, 0f, (row - 1) * 0.225f)), 60f);
            Destroy(root.gameObject);
        }

        // The gun in the hands, from either side.
        private IEnumerator GunPictures(string label, Actor me, HumanoidRig rig)
        {
            if (string.IsNullOrEmpty(s_ShotDir) || rig.Held == null) yield break;
            yield return null;
            Bounds gun = rig.Held.GetComponent<MeshRenderer>().bounds;
            Transform t = me.transform;
            float span = Mathf.Max(0.4f, gun.size.magnitude);
            ShotFrom("guns-" + label + "-right", gun.center + t.right * span * 1.6f + Vector3.up * span * 0.25f, gun.center, 32f);
            ShotFrom("guns-" + label + "-left", gun.center - t.right * span * 1.6f + Vector3.up * span * 0.4f + t.forward * span * 0.3f, gun.center, 32f);
        }
        #endregion
    }
}
