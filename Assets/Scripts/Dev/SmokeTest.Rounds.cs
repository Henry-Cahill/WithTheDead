using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace CloneZ
{
    /// <summary>
    /// <c>rounds</c>: the special rounds (ZoneContent.Arms.cs). On paper: the tracer and the two incendiaries carry their effect
    /// and their cards name it, a trader sells each and some container holds it, no other round carries an effect, and the
    /// rounds whose cards promised what nothing did (Cold-Snap, Heavy-Velocity, Fuel-Gel) are gone. Then in a raid, the trigger
    /// pulled as a player pulls it with the cursor on the target: a plain round draws no trail and a tracer round draws its
    /// streak; a plain 9mm round sets nobody alight, while a 9mm and a 12g incendiary set the zombie they hit alight, and it
    /// burns and falls, its death counted as the gun's. With <c>-clonez-smoke-shots &lt;dir&gt;</c>, pictures of a tracer's
    /// streak in flight.
    /// </summary>
    public sealed partial class SmokeTest
    {
        private static readonly string[] TracerRounds = { "ammo545_tracer" };
        private static readonly string[] IncendiaryRounds = { "ammo9mm_inc", "ammo12g_inc" };
        private static readonly string[] DroppedRounds = { "ammo545_cold", "ammo762r_hv", "ammo9mm_fuel" };
        // The dead stood before the gun live through a round to the body (a normal one doesn't), so the fire has them to burn;
        // what's left of them still falls to it well within its afterburn.
        private const float StoodDeadHealth = 4f;

        private int _roundsFired;
        private readonly List<ShotResult> _roundsHit = new List<ShotResult>();

        private IEnumerator RoundsRun()
        {
            RoundsOnPaper();

            yield return ToHub();
            yield return Deploy(RaidKind.Operative);
            Actor me = Actor.LocalPlayer;
            PlayerInventory inv = me != null ? me.GetComponent<PlayerInventory>() : null;
            PlayerCombat combat = me != null ? me.GetComponent<PlayerCombat>() : null;
            Check("rounds: the survivor and their kit", inv != null && combat != null);
            if (inv == null || combat == null) yield break;
            ClearThePeople(true);
            me.Untargetable = true;   // the dead stood before the gun mustn't come for the survivor
            if (IsoCameraFollow.Instance != null) IsoCameraFollow.Instance.SetView(CameraView.Isometric);   // aimed by the cursor
            ReleaseAll();
            yield return null;

            yield return Trails(me, inv, combat);
            yield return Alight(me, inv, combat, "pistol", "ammo9mm", false);
            yield return Alight(me, inv, combat, "pistol", "ammo9mm_inc", true);
            yield return Alight(me, inv, combat, "shotgun", "ammo12g_inc", true);
            inv.EquipBareHands();
            Unkit(inv);
            me.Untargetable = false;
        }

        private void RoundsOnPaper()
        {
            var special = new HashSet<string>(TracerRounds.Concat(IncendiaryRounds));
            foreach (string id in special)
            {
                string P = "rounds: " + id + ": ";
                ItemDef def = ItemDatabase.Get(id);
                AmmoStats a = Ballistics.Ammo(id);
                bool tracer = TracerRounds.Contains(id);
                Check(P + (tracer ? "a tracer round" : "an incendiary round"), def != null && a != null && a.Tracer == tracer && a.Incendiary != tracer,
                    a == null ? "no ballistics" : "tracer " + a.Tracer + ", incendiary " + a.Incendiary);
                if (def == null) continue;

                string card = RichText.Replace(def.Tooltip(), "");
                string effect = tracer ? "Effect: Tracer" : "Effect: Incendiary";
                Check(P + "its card names its effect", card.Contains(effect), card.Contains(effect) ? effect : effect + " missing from: " + card.Replace('\n', ' '));

                var sellers = TraderCatalog.All.Where(t => t.Offers.Any(o => o.ItemId == id && o.Price > 0)).Select(t => t.Name).ToList();
                var places = new List<string>();
                foreach (ContainerType t in Enum.GetValues(typeof(ContainerType)))
                {
                    ItemDatabase.LootTable table = ItemDatabase.TableOf(t);
                    if (table?.Entries != null && table.Entries.Any(e => e.Id == id)) places.Add(t.ToString());
                }
                Check(P + "sold, and found in the world", sellers.Count > 0 && places.Count > 0,
                    (sellers.Count > 0 ? "sold by " + string.Join(", ", sellers) : "not sold") + "; in " + (places.Count > 0 ? string.Join(", ", places) : "no container"));
            }

            List<string> flagged = Ballistics.AllAmmo.Where(a => a.Tracer || a.Incendiary).Select(a => a.Id).ToList();
            Check("rounds: no other round carries an effect", flagged.Count == special.Count && flagged.All(special.Contains), string.Join(", ", flagged));
            List<string> left = DroppedRounds.Where(id => ItemDatabase.Get(id) != null || Ballistics.Ammo(id) != null).ToList();
            Check("rounds: the rounds that did nothing their cards said are gone", left.Count == 0,
                left.Count > 0 ? "still here: " + string.Join(", ", left) : string.Join(", ", DroppedRounds));
        }

        // A plain round draws no trail; a tracer round draws its streak (CombatFX.Tracer), one for each round fired.
        private IEnumerator Trails(Actor me, PlayerInventory inv, PlayerCombat combat)
        {
            yield return Arm(inv, combat, "ak74", "ammo545_ps");
            Vector3 spot = Ahead(me, 6f);
            int drawn = CombatFX.TracersDrawn;
            yield return Pull(me, combat, () => spot);
            Check("rounds: a plain round draws no trail", _roundsFired >= 1 && CombatFX.TracersDrawn == drawn,
                _roundsFired + " fired, " + (CombatFX.TracersDrawn - drawn) + " trails" + (_roundsFired == 0 ? FireBlockers(me) : ""));

            Stock(inv, "ammo545_tracer", 60);
            combat.SwitchAmmo("ammo545_tracer");
            yield return UntilGame(() => !combat.IsReloading, 15f);
            drawn = CombatFX.TracersDrawn;
            yield return Pull(me, combat, () => spot);
            Check("rounds: a tracer round draws its streak", _roundsFired >= 1 && combat.LoadedAmmoId == "ammo545_tracer" && CombatFX.TracersDrawn - drawn == _roundsFired,
                _roundsFired + " fired, " + (CombatFX.TracersDrawn - drawn) + " streaks, " + (combat.LoadedAmmoId ?? "nothing") + " loaded"
                + (_roundsFired == 0 ? FireBlockers(me) : ""));
            yield return TracerPictures(me, combat, spot);
        }

        // With -clonez-smoke-shots, a tracer's streak a frame or two after the shot: through the game's camera, and from beside the
        // survivor down the line of fire.
        private IEnumerator TracerPictures(Actor me, PlayerCombat combat, Vector3 spot)
        {
            if (string.IsNullOrEmpty(s_ShotDir) || combat.LoadedAmmo <= 0) yield break;
            ItemDef gun = combat.EquippedFirearm;
            yield return WaitGame(Mathf.Max(0.2f, (gun != null ? gun.FireCooldown : 0f) + 0.1f));
            for (int i = 0; i < 3; i++)
            {
                PointAt(spot);
                yield return null;
            }
            int drawn = CombatFX.TracersDrawn;
            yield return Click(MouseButton.Left);
            yield return new WaitForEndOfFrame();
            Vector3 from = me.transform.position, dir = CloneZUtil.Flat(spot - from), side = Vector3.Cross(Vector3.up, dir);
            Shot("rounds-tracer", Camera.main);
            ShotFrom("rounds-tracer-beside", from + Vector3.up * 1.7f - dir * 1.5f + side * 1.2f, spot + Vector3.up * 0.9f, 60f);
            Debug.Log("[Smoke] rounds: pictures of the tracer taken, " + (CombatFX.TracersDrawn - drawn) + " streak(s) drawn for the shot");
        }

        // A zombie stood before the gun and shot with `roundId`: an incendiary sets it alight, and it burns and falls, its death the
        // gun's; a plain round sets nobody alight.
        private IEnumerator Alight(Actor me, PlayerInventory inv, PlayerCombat combat, string gunId, string roundId, bool incendiary)
        {
            string P = "rounds: " + roundId + " from the " + gunId + ": ";
            yield return Arm(inv, combat, gunId, roundId);
            ZombieAI ai = StandDead(Ahead(me, 3.5f));
            Actor body = ai.GetComponent<Actor>();
            string killedBy = null;
            bool fellAlight = false;
            float shotAt = 0f, fellAt = 0f;
            Action<KillInfo> killed = k =>
            {
                if (k.Victim != body || killedBy != null) return;
                killedBy = k.WeaponId ?? "nothing";
                fellAlight = Aflame.IsOn(body);
                fellAt = Time.time;
            };
            GameEvents.ActorKilled += killed;
            try
            {
                yield return null;
                bool hit = false;
                int pulls = 0;
                while (!hit && pulls < 4 && combat.LoadedAmmo > 0 && killedBy == null)
                {
                    pulls++;
                    yield return Pull(me, combat, () => body != null ? body.transform.position : me.transform.position);
                    hit = _roundsHit.Any(r => r.TargetActor == body);
                }
                shotAt = Time.time;
                bool alight = Aflame.IsOn(body);
                Check(P + "hits the zombie it's aimed at", hit, pulls + " trigger pulls" + (killedBy != null ? "; the round itself killed it" : ""));
                if (!incendiary)
                {
                    yield return WaitGame(1f);
                    alight |= Aflame.IsOn(body);
                    Check(P + "a plain round sets nobody alight", hit && !alight);
                    yield break;
                }
                Check(P + "sets the zombie it hits alight", hit && alight);
                yield return UntilGame(() => ai == null || ai.IsDeadState, 10f);
                Check(P + "it burns and falls, its death the gun's", killedBy == gunId && fellAlight,
                    killedBy != null ? "killed by " + killedBy + (fellAlight ? ", alight, " : ", not alight, ") + F3(fellAt - shotAt) + " s after the shot"
                    : "still standing 10 s on");
            }
            finally
            {
                GameEvents.ActorKilled -= killed;
                Dismiss(new List<ZombieAI> { ai });
            }
        }

        // The gun in hand, loaded with `roundId`, the only rounds in the pack (the last gun's rounds handed back and taken out).
        private IEnumerator Arm(PlayerInventory inv, PlayerCombat combat, string gunId, string roundId)
        {
            inv.EquipBareHands();
            Unkit(inv);
            ItemDef gun = ItemDatabase.Get(gunId);
            if (gun == null) throw new Exception("missing gun " + gunId);
            inv.Inventory.Add(gun, 1, enforceWeight: false);
            Stock(inv, roundId, 60);
            inv.Equip(gun);
            yield return null;
            yield return UntilGame(() => !combat.IsReloading, 15f);
            if (combat.LoadedAmmo <= 0)
            {
                combat.TryReload();
                yield return UntilGame(() => !combat.IsReloading, 15f);
            }
            Check("rounds: the " + gunId + " in hand, loaded with " + roundId,
                combat.EquippedFirearm == gun && combat.LoadedAmmoId == roundId && combat.LoadedAmmo > 0,
                (combat.EquippedFirearm != null ? combat.EquippedFirearm.Id : "nothing") + ", " + combat.LoadedAmmo + " of " + (combat.LoadedAmmoId ?? "nothing"));
        }

        // One trigger pull with the cursor on `at`: the feet of whatever's aimed at, as the isometric view aims across the floor
        // the survivor stands on. Counts the rounds fired, and keeps where they went.
        private IEnumerator Pull(Actor me, PlayerCombat combat, Func<Vector3> at)
        {
            _roundsFired = 0;
            _roundsHit.Clear();
            Action onFire = () => _roundsFired++;
            Action<ShotRequest, ShotResult> resolved = (q, r) => { if (q.ShooterActor == me) _roundsHit.Add(r); };
            combat.OnFire += onFire;
            CombatResolver.ShotResolved += resolved;
            try
            {
                ItemDef gun = combat.EquippedFirearm;
                yield return WaitGame(Mathf.Max(0.2f, (gun != null ? gun.FireCooldown : 0f) + 0.1f));
                for (int i = 0; i < 3; i++)
                {
                    PointAt(at());
                    yield return null;
                }
                yield return Click(MouseButton.Left);
                yield return WaitGame(0.15f);
            }
            finally
            {
                combat.OnFire -= onFire;
                CombatResolver.ShotResolved -= resolved;
            }
        }

        // A spot on the floor `distance` ahead of the survivor, the way with the most room.
        private static Vector3 Ahead(Actor me, float distance)
        {
            Vector3 from = me.transform.position;
            Vector3 dir = Quaternion.Euler(0f, ClearestHeading(from + Vector3.up * 0.95f), 0f) * Vector3.forward;
            Vector3 p = from + dir * distance;
            return OnFloor(p, from.y, 1.5f, out Vector3 at) ? at : p;
        }

        private static ZombieAI StandDead(Vector3 at)
        {
            GameObject z = ZombieSpawner.BuildZombie(at);
            var ai = z.GetComponent<ZombieAI>();
            ai.Configure(ZombieType.Shambler);
            z.GetComponent<ZombieHealth>().SetMaxHealth(StoodDeadHealth);
            return ai;
        }
    }
}
