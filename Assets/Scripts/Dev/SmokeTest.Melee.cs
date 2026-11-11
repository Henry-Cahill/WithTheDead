using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.LowLevel;

namespace CloneZ
{
    /// <summary>
    /// <c>melee</c>: melee with weight (<see cref="MeleeStats"/>): the kitchen knife, the baseball bat and the fire axe against
    /// the dead in a corridor. On paper: the three (and bare hands) differ in reach, arc, wind-up, recovery, stagger and
    /// knockdown, and in how many bodies a swing carries through (one, two or three, by its arc); their item cards say so.
    /// Then in a raid, in a corridor built ahead of the survivor (two walls, 2.2 m apart), with each weapon as a player swings
    /// it (the cursor on the target, a click): three of the dead stood in it, one in front and two behind, and one swing: the
    /// blow lands its wind-up after the press, it strikes one of them (the knife), two (the axe) or three (the bat), and the
    /// first takes a full blow and each further one less; held, the attack swings again only once the recovery is over;
    /// and over a run of blows at one of the dead, the knife's mostly only make it flinch and never fell it, while most of the
    /// bat's and the axe's stagger or fell. The ground: a crawler at the feet is stomped rather than swung at, one further off
    /// with nothing standing is struck by a swing brought down on it, and a swing passes over it while one of the dead stands
    /// in the swing. Bare-handed, shoves until one of the dead goes down, then the attack stomps it: on its body for half while
    /// its head is out of reach, then, stood beside its head, on that in full, to death before it gets up.
    /// Last, each weapon is fought through three of the dead coming down the corridor at the survivor (who stands their
    /// ground with the attack held, the cursor on the nearest): it must clear the corridor, and the time, swings, bodies a
    /// blow, staggers, knockdowns, stomps, the lunges the dead got in and the hits that got through are logged side by side.
    /// With <c>-clonez-smoke-shots &lt;dir&gt;</c>, pictures of the opening blows, one of the dead on the ground and a stomp.
    /// </summary>
    public sealed partial class SmokeTest
    {
        private static readonly string[] MeleeWeapons = { "kitchenknife", "baseballbat", "fireaxe" };
        private const float CorridorWidth = 2.2f, CorridorLength = 11f;
        private const int RateBlows = 16;
        private const float SturdyDead = 1000f;   // the dead the blows are measured on: nothing here kills them
        private const float FightTime = 45f;

        private sealed class Corridor
        {
            public Vector3 Start, Forward, Right;
            public readonly List<GameObject> Walls = new List<GameObject>();
            public Vector3 At(float ahead, float across) => Start + Forward * ahead + Right * across;
        }

        private IEnumerator MeleeRun()
        {
            MeleeOnPaper();

            yield return ToHub();
            yield return Deploy(RaidKind.Operative);
            Actor me = Actor.LocalPlayer;
            PlayerInventory inv = me != null ? me.GetComponent<PlayerInventory>() : null;
            PlayerCombat combat = me != null ? me.GetComponent<PlayerCombat>() : null;
            PlayerStats stats = me != null ? me.GetComponent<PlayerStats>() : null;
            Check("melee: the survivor and their kit", inv != null && combat != null && stats != null);
            if (inv == null || combat == null || stats == null) yield break;
            EndSpawnProtection();
            ClearThePeople(true);
            if (TimeOfDay.Instance != null) TimeOfDay.Instance.RestoreClock(TimeOfDay.Instance.Day, 12f);   // noon: daylight for the pictures
            me.Untargetable = true;   // the dead stood up to be struck mustn't come for the survivor (the fights lift it)
            if (IsoCameraFollow.Instance != null) IsoCameraFollow.Instance.SetView(CameraView.Isometric);   // aimed by the cursor
            ReleaseAll();
            yield return null;

            Corridor c = BuildCorridor(me.transform.position, out string why);
            Check("melee: a corridor built ahead of the survivor", c != null, why);
            if (c == null) yield break;
            try
            {
                Teleport(me, c.Start);
                ClearDeadNear(c.At(CorridorLength * 0.5f, 0f), 60f);
                yield return WaitGame(1.5f);   // the walls' carves into the NavMesh

                var reacted = new Dictionary<string, int>();
                foreach (string id in MeleeWeapons)
                {
                    ItemDef weapon = ItemDatabase.Get(id);
                    if (weapon == null) continue;
                    yield return OpeningBlow(c, me, inv, combat, stats, weapon);
                    yield return Rhythm(c, me, inv, combat, weapon);
                    yield return Rates(c, me, inv, combat, stats, weapon, reacted);
                }
                if (reacted.Count == MeleeWeapons.Length)
                    Check("melee: the bat and the axe rock the dead back far more often than the knife",
                        reacted["kitchenknife"] < reacted["baseballbat"] && reacted["kitchenknife"] < reacted["fireaxe"],
                        "staggered or felled: knife " + reacted["kitchenknife"] + ", bat " + reacted["baseballbat"] + ", axe " + reacted["fireaxe"]
                        + " of " + RateBlows + " blows each");

                yield return GroundRules(c, me, inv, combat);
                yield return ShoveThenStomp(c, me, inv, combat, stats);

                var fights = new List<string>();
                foreach (string id in MeleeWeapons)
                {
                    ItemDef weapon = ItemDatabase.Get(id);
                    if (weapon != null) yield return Fight(c, me, inv, combat, stats, weapon, fights);
                }
                foreach (string f in fights) Debug.Log("[Smoke] melee fight " + f);
            }
            finally
            {
                ReleaseAll();
                foreach (GameObject w in c.Walls) if (w != null) Destroy(w);
                if (me != null) me.Untargetable = false;
            }
            inv.EquipBareHands();
        }

        #region On paper
        private void MeleeOnPaper()
        {
            ItemDef knifeDef = ItemDatabase.Get("kitchenknife"), batDef = ItemDatabase.Get("baseballbat"), axeDef = ItemDatabase.Get("fireaxe");
            Check("melee: on paper: the knife, the bat and the axe are melee weapons", knifeDef != null && knifeDef.IsWeapon
                && batDef != null && batDef.IsWeapon && axeDef != null && axeDef.IsWeapon);
            if (knifeDef == null || batDef == null || axeDef == null) return;
            MeleeStats knife = MeleeStats.For(knifeDef), bat = MeleeStats.For(batDef), axe = MeleeStats.For(axeDef), fists = MeleeStats.BareHands;
            foreach (MeleeStats m in new[] { fists, knife, bat, axe })
                Debug.Log("[Smoke] melee profile " + m.Id + ": reach " + F2(m.Reach) + " m, arc " + F2(m.Arc) + "°, wind-up " + F3(m.WindUp)
                    + " s, recovery " + F3(m.Recovery) + " s, damage " + F2(m.Damage) + ", knockback " + F2(m.Knockback) + ", stagger "
                    + F2(m.Stagger) + ", knockdown " + F2(m.Knockdown) + ", bodies a swing " + m.MaxTargets + ", endurance " + F3(m.Endurance));

            Check("melee: on paper: reach: bare hands < knife < axe < bat", fists.Reach < knife.Reach && knife.Reach < axe.Reach && axe.Reach < bat.Reach,
                F2(fists.Reach) + " / " + F2(knife.Reach) + " / " + F2(axe.Reach) + " / " + F2(bat.Reach) + " m");
            Check("melee: on paper: a swing carries through bodies by its arc: the knife's stab one, the axe two, the bat's sweep three, a punch one",
                knife.MaxTargets == 1 && axe.MaxTargets == 2 && bat.MaxTargets == 3 && fists.MaxTargets == 1,
                "knife " + F2(knife.Arc) + "° " + knife.MaxTargets + ", axe " + F2(axe.Arc) + "° " + axe.MaxTargets + ", bat " + F2(bat.Arc) + "° " + bat.MaxTargets);
            Check("melee: on paper: wind-up: knife < bat < axe", knife.WindUp < bat.WindUp && bat.WindUp < axe.WindUp,
                F3(knife.WindUp) + " / " + F3(bat.WindUp) + " / " + F3(axe.WindUp) + " s");
            Check("melee: on paper: recovery: knife < bat < axe", knife.Recovery < bat.Recovery && bat.Recovery < axe.Recovery,
                F3(knife.Recovery) + " / " + F3(bat.Recovery) + " / " + F3(axe.Recovery) + " s");
            Check("melee: on paper: stagger: knife < axe < bat", knife.Stagger < axe.Stagger && axe.Stagger < bat.Stagger,
                F2(knife.Stagger) + " / " + F2(axe.Stagger) + " / " + F2(bat.Stagger));
            Check("melee: on paper: knockdown: the knife never fells, the bat does, the axe most", knife.Knockdown == 0f && bat.Knockdown > 0f && axe.Knockdown > bat.Knockdown,
                F2(knife.Knockdown) + " / " + F2(bat.Knockdown) + " / " + F2(axe.Knockdown));
            Check("melee: on paper: the heavier the weapon, the more breath a swing costs", knife.Endurance < bat.Endurance && bat.Endurance < axe.Endurance,
                F3(knife.Endurance) + " / " + F3(bat.Endurance) + " / " + F3(axe.Endurance));
            foreach (ItemDef d in new[] { knifeDef, batDef, axeDef })
            {
                string card = RichText.Replace(d.Tooltip(), "");
                bool says = card.Contains("wind-up") && card.Contains("recovery") && card.Contains("Hits:") && card.Contains("Staggers:");
                Check("melee: on paper: the " + d.Id + "'s card says how it handles", says, says ? "" : card.Replace('\n', ' '));
            }
        }
        #endregion

        #region The corridor
        // A straight run of open floor near `near` (the survivor's own spot first, then further out), the NavMesh all the way
        // down it and a little to each side and nothing in the way at hip height, walled in on both sides.
        private static Corridor BuildCorridor(Vector3 near, out string why)
        {
            int tried = 0;
            foreach (float ring in new[] { 0f, 12f, 24f, 40f })
                for (float around = 0f; around < 360f; around += ring > 0f ? 45f : 360f)
                {
                    Vector3 from = near + Quaternion.Euler(0f, around, 0f) * Vector3.forward * ring;
                    if (!OnFloor(from, near.y, 2f, out Vector3 start)) continue;
                    for (float yaw = 0f; yaw < 360f; yaw += 30f)
                    {
                        tried++;
                        Vector3 fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                        Vector3 right = Vector3.Cross(Vector3.up, fwd);
                        if (!OpenRun(start, fwd, right)) continue;
                        var c = new Corridor { Start = start, Forward = fwd, Right = right };
                        float side = CorridorWidth * 0.5f + 0.15f;
                        c.Walls.Add(Wall(c, -side));
                        c.Walls.Add(Wall(c, side));
                        why = "at " + start + " heading " + yaw.ToString("0", CultureInfo.InvariantCulture) + "°, " + FlatDistance(start, near).ToString("0", CultureInfo.InvariantCulture)
                            + " m from the survivor, " + tried + " tried";
                        return c;
                    }
                }
            why = tried + " runs tried, none open for " + CorridorLength + " m";
            return null;
        }

        private static bool OpenRun(Vector3 start, Vector3 fwd, Vector3 right)
        {
            for (float across = -0.9f; across <= 0.91f; across += 0.9f)
            {
                Vector3 eye = start + right * across + Vector3.up * 0.9f - fwd * 0.5f;
                if (Physics.Raycast(eye, fwd, CorridorLength + 0.5f, CloneZConfig.WorldMask, QueryTriggerInteraction.Ignore)) return false;
                for (float s = 0f; s <= CorridorLength; s += 1f)
                    if (!OnFloor(start + fwd * s + right * across, start.y, 0.35f, out _)) return false;
            }
            return true;
        }

        // A wall down one side, carved out of the NavMesh so the dead keep to the corridor.
        private static GameObject Wall(Corridor c, float across)
        {
            GameObject go = CloneZUtil.Box("[Smoke] corridor wall", c.At(CorridorLength * 0.5f - 0.5f, across) + Vector3.up * 1.25f,
                new Vector3(0.3f, 2.5f, CorridorLength + 1f), new Color(0.46f, 0.44f, 0.4f));
            go.transform.rotation = Quaternion.LookRotation(c.Forward);
            var carve = go.AddComponent<NavMeshObstacle>();
            carve.shape = NavMeshObstacleShape.Box;
            carve.size = Vector3.one;
            carve.carving = true;
            return go;
        }

        // The dead already about the corridor would wander into the measurements.
        private static void ClearDeadNear(Vector3 at, float radius)
        {
            int gone = 0;
            foreach (ZombieAI ai in FindObjectsByType<ZombieAI>(FindObjectsSortMode.None))
                if (ai != null && FlatDistance(ai.transform.position, at) < radius) { Destroy(ai.gameObject); gone++; }
            Debug.Log("[Smoke] melee: " + gone + " of the dead about the corridor sent away");
        }

        // One of the dead stood in the corridor, `ahead` down it and `across` it, facing the survivor at its mouth.
        private static ZombieAI Stand(Corridor c, float ahead, float across, float health, ZombieType type = ZombieType.Shambler)
        {
            Vector3 p = c.At(ahead, across);
            if (OnFloor(p, c.Start.y, 0.6f, out Vector3 at)) p = at;
            GameObject z = ZombieSpawner.BuildZombie(p);
            z.transform.rotation = Quaternion.LookRotation(-c.Forward);
            var ai = z.GetComponent<ZombieAI>();
            ai.Configure(type);
            z.GetComponent<ZombieHealth>().SetMaxHealth(health);
            return ai;
        }

        // Back to its spot, facing the survivor (it wanders off between blows: the survivor is nobody's prey here).
        private static void Place(ZombieAI ai, Corridor c, float ahead, float across)
        {
            Vector3 p = c.At(ahead, across);
            if (OnFloor(p, c.Start.y, 0.6f, out Vector3 at)) p = at;
            var agent = ai.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled) agent.Warp(p);
            else ai.transform.position = p;
            ai.transform.rotation = Quaternion.LookRotation(-c.Forward);
        }

        // The survivor whole again between rounds: no wounds, no pain, no infection, full health and breath.
        private static void MakeWhole(Actor me)
        {
            var stats = me.GetComponent<PlayerStats>();
            var limbs = me.GetComponent<PlayerLimbs>();
            var wounds = me.GetComponent<PlayerWounds>();
            if (wounds != null) wounds.LoadState(null, 0f, 0f, 0f);
            if (limbs != null) limbs.FullHeal();
            if (stats == null) return;
            stats.Heal(CloneZConfig.HealthMax);
            stats.Infected = false;
            stats.Infection = 0f;
            stats.Pain = 0f;
            stats.Panic = 0f;
            stats.Endurance = CloneZConfig.EnduranceMax;
        }

        private static IEnumerator Wield(PlayerInventory inv, PlayerCombat combat, ItemDef weapon)
        {
            if (weapon == null) inv.EquipBareHands();
            else
            {
                if (!inv.Has(weapon.Id)) inv.Inventory.Add(weapon, 1, enforceWeight: false);
                inv.Equip(weapon);
            }
            yield return null;
            yield return UntilGame(() => !combat.MeleeBusy, 3f);
        }

        private static IEnumerator Aim(Vector3 at)
        {
            for (int i = 0; i < 3; i++)
            {
                PointAt(at);
                yield return null;
            }
        }

        private static float Hurt(ZombieAI ai) => ai != null ? SturdyDead - ai.GetComponent<ZombieHealth>().Health : 0f;
        #endregion

        #region One swing, the swing's rhythm, the blows' outcomes
        // Three of the dead, one in front and two a pace behind it to either side, and one swing at the front one: it lands its
        // wind-up after the press and strikes as many as the weapon's arc carries through, the first taking a full blow and each
        // further one MeleeCarryThrough of the one before (a crit aside, three times that).
        private IEnumerator OpeningBlow(Corridor c, Actor me, PlayerInventory inv, PlayerCombat combat, PlayerStats stats, ItemDef weapon)
        {
            string P = "melee: " + weapon.Id + ": ";
            yield return Wield(inv, combat, weapon);
            MakeWhole(me);
            Teleport(me, c.Start);
            MeleeStats m = combat.Melee;
            var dead = new List<ZombieAI> { Stand(c, 1.3f, 0f, SturdyDead), Stand(c, 1.95f, -0.55f, SturdyDead), Stand(c, 1.95f, 0.55f, SturdyDead) };
            float pressedAt = -1f, landedAt = -1f, landingFrame = 0f;
            int struck = -1;
            Action onSwing = () => { if (pressedAt < 0f) pressedAt = Time.time; };
            Action<int> onLanded = n =>
            {
                if (landedAt >= 0f) return;
                landedAt = Time.time;
                landingFrame = Time.deltaTime;
                struck = n;
            };
            combat.OnSwing += onSwing;
            combat.OnBlowLanded += onLanded;
            try
            {
                yield return null;
                yield return Aim(dead[0].transform.position);
                yield return Click(MouseButton.Left);
                yield return UntilGame(() => landedAt >= 0f, 3f);
            }
            finally
            {
                combat.OnSwing -= onSwing;
                combat.OnBlowLanded -= onLanded;
            }
            float windUp = combat.BlowWindUp, took = landedAt - pressedAt;
            Check(P + "the blow lands its wind-up after the press", pressedAt >= 0f && landedAt >= 0f && took >= windUp - 0.002f && took <= windUp + landingFrame + 0.002f,
                pressedAt < 0f ? "no swing" : landedAt < 0f ? "never landed" : F3(took) + " s for a " + F3(windUp) + " s wind-up (the profile's " + F3(m.WindUp)
                    + " s), the landing frame " + F3(landingFrame) + " s");

            int expect = m.MaxTargets;
            var hurt = new List<float> { Hurt(dead[0]), Hurt(dead[1]), Hurt(dead[2]) };
            int bodies = 0;
            foreach (float h in hurt) if (h > 1e-4f) bodies++;
            Check(P + "one swing strikes " + expect + " of the three", struck == expect && bodies == expect,
                struck + " struck, " + bodies + " hurt (" + F2(hurt[0]) + " / " + F2(hurt[1]) + " / " + F2(hurt[2]) + ")");

            PlayerSkills skills = me.GetComponent<PlayerSkills>();
            float full = m.Damage * (skills != null ? skills.MeleeDamageMult(m.Skill) : 1f) * PlayerModifiers.Current.MeleeDamage
                * stats.PainCombatMultiplier * CloneZConfig.NonPlayerReceivedDamageMultiplier;
            var rest = new List<float>();
            if (hurt[1] > 1e-4f) rest.Add(hurt[1]);
            if (hurt[2] > 1e-4f) rest.Add(hurt[2]);
            Check(P + "the front one takes a full blow, each further one less", hurt[0] > 1e-4f && FitsShares(hurt[0], rest, full),
                "a full blow " + F2(full) + "; " + F2(hurt[0]) + " to the front one" + (rest.Count > 0 ? ", " + string.Join(" and ", rest.ConvertAll(F2)) + " behind" : ""));

            if (weapon.Id == "baseballbat") Close("melee-bat-blow", c, c.At(1.6f, 0f) + Vector3.up * 0.9f);
            yield return WaitGame(0.2f);
            if (weapon.Id == "fireaxe") Close("melee-axe-follow-through", c, c.At(1.6f, 0f) + Vector3.up * 0.9f);
            Dismiss(dead);
        }

        // A picture from inside the corridor: above and behind the survivor's right shoulder, looking at `at`.
        private static void Close(string name, Corridor c, Vector3 at) => ShotFrom(name, c.At(-0.6f, 0.85f) + Vector3.up * 2.5f, at, 60f);

        // Whether the bodies' damage matches their places in the swing — the first a full blow, then each further one
        // MeleeCarryThrough of the last, within the blow's ±15% (or three times that, a crit) — the further ones in either order.
        private static bool FitsShares(float first, List<float> rest, float full)
        {
            if (!Fits(first / full)) return false;
            if (rest.Count == 0) return true;
            float second = full * MeleeStats.Carry(1), third = full * MeleeStats.Carry(2);
            if (rest.Count == 1) return Fits(rest[0] / second);
            return Fits(rest[0] / second) && Fits(rest[1] / third) || Fits(rest[1] / second) && Fits(rest[0] / third);
        }

        private static bool Fits(float ratio) => ratio >= 0.83f && ratio <= 1.17f || ratio >= 2.5f && ratio <= 3.5f;

        // Held, the attack swings again only once the blow's recovery is over: press to press is the whole swing.
        private IEnumerator Rhythm(Corridor c, Actor me, PlayerInventory inv, PlayerCombat combat, ItemDef weapon)
        {
            string P = "melee: " + weapon.Id + ": ";
            yield return Wield(inv, combat, weapon);
            Teleport(me, c.Start);
            me.GetComponent<PlayerStats>().Endurance = CloneZConfig.EnduranceMax;
            var at = new List<float>();
            var frames = new List<float>();
            Action onSwing = () =>
            {
                at.Add(Time.time);
                frames.Add(Time.deltaTime);
            };
            combat.OnSwing += onSwing;
            try
            {
                yield return Aim(c.At(4f, 0f));
                yield return HoldButton(MouseButton.Left, combat.Melee.Cycle * 2.4f + 0.3f, () => PointAt(c.At(4f, 0f)));
            }
            finally { combat.OnSwing -= onSwing; }
            float cycle = combat.BlowWindUp + combat.BlowRecovery;
            float gap = at.Count >= 2 ? at[1] - at[0] : -1f;
            Check(P + "held, it swings again once the recovery is over", at.Count >= 2 && gap >= cycle - 0.002f && gap <= cycle + frames[1] + 0.002f,
                at.Count + " swings; press to press " + F3(gap) + " s for " + F3(combat.BlowWindUp) + " s wind-up + " + F3(combat.BlowRecovery) + " s recovery");
            yield return UntilGame(() => !combat.MeleeBusy, 3f);

            // A click late in the recovery isn't lost: the next swing starts the moment the recovery is over.
            at.Clear();
            frames.Clear();
            combat.OnSwing += onSwing;
            try
            {
                yield return Click(MouseButton.Left);
                yield return UntilGame(() => at.Count >= 1, 2f);
                float first = at.Count > 0 ? at[0] : Time.time;
                yield return UntilGame(() => Time.time >= first + cycle - 0.2f, 2f);
                bool inRecovery = combat.MeleeBusy && !combat.WindingUp;
                yield return Click(MouseButton.Left);
                yield return UntilGame(() => at.Count >= 2, 2f);
                gap = at.Count >= 2 ? at[1] - at[0] : -1f;
                Check(P + "a click late in the recovery swings the moment it's over", inRecovery && at.Count >= 2 && gap <= cycle + frames[1] + 0.002f,
                    (inRecovery ? "clicked in the recovery" : "clicked outside the recovery") + "; " + at.Count + " swings, press to press " + F3(gap) + " s for a " + F3(cycle) + " s swing");
            }
            finally { combat.OnSwing -= onSwing; }
            yield return UntilGame(() => !combat.MeleeBusy, 3f);
        }

        // One of the dead, sturdy enough to take every blow, stood in front and struck RateBlows times (stood up and back in its
        // spot before each): every blow fells it, staggers it or only makes it flinch, as often as the weapon's chances say.
        private IEnumerator Rates(Corridor c, Actor me, PlayerInventory inv, PlayerCombat combat, PlayerStats stats, ItemDef weapon, Dictionary<string, int> reacted)
        {
            string P = "melee: " + weapon.Id + ": ";
            yield return Wield(inv, combat, weapon);
            MakeWhole(me);
            Teleport(me, c.Start);
            MeleeStats m = combat.Melee;
            ZombieAI ai = Stand(c, 1.3f, 0f, SturdyDead);
            string outcome = null;
            int landed = -1, felled = 0, staggered = 0, flinched = 0, missed = 0;
            Action<Vector3> onDown = d => outcome = "felled";
            Action<Vector3, float> onReel = (d, t) => outcome = outcome ?? "staggered";
            Action<Vector3> onFlinch = d => outcome = outcome ?? "flinched";
            Action<int> onLanded = n => landed = n;
            ai.KnockedDown += onDown;
            ai.Staggered += onReel;
            ai.Flinched += onFlinch;
            combat.OnBlowLanded += onLanded;
            bool pictured = false;
            try
            {
                for (int i = 0; i < RateBlows; i++)
                {
                    yield return UntilGame(() => ai == null || !ai.IsDowned && !ai.IsStaggered && !combat.MeleeBusy, 8f);
                    if (ai == null) break;
                    Place(ai, c, 1.3f, 0f);
                    stats.Endurance = CloneZConfig.EnduranceMax;
                    yield return Aim(ai.transform.position);
                    outcome = null;
                    landed = -1;
                    yield return Click(MouseButton.Left);
                    yield return UntilGame(() => landed >= 0, 3f);
                    if (landed < 1) missed++;
                    else if (outcome == "felled") felled++;
                    else if (outcome == "staggered") staggered++;
                    else flinched++;
                    if (outcome == "felled" && !pictured && !string.IsNullOrEmpty(s_ShotDir))
                    {
                        pictured = true;
                        yield return WaitGame(0.7f);
                        Close("melee-down", c, ai.transform.position + Vector3.up * 0.2f);
                    }
                }
            }
            finally
            {
                if (ai != null)
                {
                    ai.KnockedDown -= onDown;
                    ai.Staggered -= onReel;
                    ai.Flinched -= onFlinch;
                }
                combat.OnBlowLanded -= onLanded;
            }
            Dismiss(new List<ZombieAI> { ai });

            // Felled first (a knockdown is rolled before a stagger), with Strength's share; then staggered.
            PlayerSkills skills = me.GetComponent<PlayerSkills>();
            int strength = skills != null ? skills.GetLevel(PlayerSkills.Skill.Strength) : 0;
            float down = m.Knockdown > 0f ? Mathf.Clamp01(m.Knockdown + strength * CloneZConfig.KnockdownPerStrength) : 0f;
            float react = down + (1f - down) * m.Stagger;
            int n = RateBlows - missed;
            float mean = n * react, sd = Mathf.Sqrt(n * react * (1f - react));
            int rocked = felled + staggered;
            reacted[weapon.Id] = rocked;
            string tally = felled + " felled, " + staggered + " staggered, " + flinched + " only flinched" + (missed > 0 ? ", " + missed + " missed" : "")
                + " of " + RateBlows + " blows (chances: fell " + F2(down) + ", stagger " + F2(m.Stagger) + ")";
            Debug.Log("[Smoke] melee blows " + weapon.Id + ": " + tally);
            Check(P + "every blow lands", missed == 0, tally);
            Check(P + "its blows stagger or fell as often as its chances say", Mathf.Abs(rocked - mean) <= 3.5f * sd + 1f,
                tally + "; expected " + F2(mean) + " ± " + F2(3.5f * sd + 1f));
            if (down <= 0f) Check(P + "it never fells one of the dead", felled == 0, tally);
            else if (down >= 0.3f) Check(P + "it fells one of the dead now and then", felled >= 1, tally);
        }
        #endregion

        #region The ground
        // A crawler at the feet is stomped, not swung at; further off with nothing standing in the swing, the swing comes down on
        // it; and with one of the dead standing in the swing, the swing passes over it.
        private IEnumerator GroundRules(Corridor c, Actor me, PlayerInventory inv, PlayerCombat combat)
        {
            ItemDef bat = ItemDatabase.Get("baseballbat");
            if (bat == null) yield break;
            yield return Wield(inv, combat, bat);
            MakeWhole(me);
            Teleport(me, c.Start);
            ZombieAI crawler = Stand(c, 1.1f, 0f, SturdyDead, ZombieType.Crawler);
            ZombieAI standing = null;
            int swings = 0, stomps = 0, landed = -1;
            Action onSwing = () => swings++, onStomp = () => stomps++;
            Action<int> onLanded = n => landed = n;
            combat.OnSwing += onSwing;
            combat.OnStomp += onStomp;
            combat.OnBlowLanded += onLanded;
            try
            {
                yield return null;
                Check("melee: a crawler counts as on the ground", crawler.GetComponent<Actor>().Downed);

                float hurt = Hurt(crawler);
                yield return Aim(crawler.transform.position);
                yield return Click(MouseButton.Left);
                yield return UntilGame(() => landed >= 0, 3f);
                float share = (Hurt(crawler) - hurt) / FullStomp(me);
                Check("melee: a crawler at the feet is stomped on the head, not swung at", stomps == 1 && swings == 0 && landed == 1 && share >= 0.88f && share <= 1.12f,
                    stomps + " stomps, " + swings + " swings, " + F2(Hurt(crawler) - hurt) + " dealt, " + F2(share) + " of a full stomp");

                yield return UntilGame(() => !combat.MeleeBusy && !crawler.IsStaggered, 3f);
                Place(crawler, c, 2.9f, 0f);   // its head 2 m off: past a stomp, in the bat's reach
                swings = stomps = 0;
                landed = -1;
                hurt = Hurt(crawler);
                yield return Aim(crawler.transform.position);
                yield return Click(MouseButton.Left);
                yield return UntilGame(() => landed >= 0, 3f);
                Check("melee: with nothing standing in it, the swing comes down on one on the ground beyond a stomp",
                    swings == 1 && stomps == 0 && landed == 1 && Hurt(crawler) > hurt,
                    swings + " swings, " + stomps + " stomps, " + landed + " struck, " + F2(Hurt(crawler) - hurt) + " dealt");

                yield return UntilGame(() => !combat.MeleeBusy && !crawler.IsStaggered, 3f);
                standing = Stand(c, 2.3f, 0.45f, SturdyDead);
                Place(crawler, c, 2.6f, -0.35f);   // nearer than the one standing, in the swing, but past a stomp
                swings = stomps = 0;
                landed = -1;
                hurt = Hurt(crawler);
                yield return null;
                yield return Aim(standing.transform.position);
                yield return Click(MouseButton.Left);
                yield return UntilGame(() => landed >= 0, 3f);
                Check("melee: a swing passes over one on the ground while one of the dead stands in it",
                    swings == 1 && landed == 1 && Hurt(standing) > 0f && Hurt(crawler) <= hurt,
                    swings + " swings, " + landed + " struck; " + F2(Hurt(standing)) + " to the one standing, " + F2(Hurt(crawler) - hurt) + " to the crawler");
            }
            finally
            {
                combat.OnSwing -= onSwing;
                combat.OnStomp -= onStomp;
                combat.OnBlowLanded -= onLanded;
            }
            Dismiss(new List<ZombieAI> { crawler, standing });
        }

        // Bare-handed: shoves until one of the dead goes down (stood back in front each time it keeps its feet), then the attack
        // stomps it where it lies. With its head out of reach (it went over backward, away), the stomp lands on its body for half;
        // stepped over beside its head, the stomps land on that in full, until it's dead — before it ever gets back up.
        private IEnumerator ShoveThenStomp(Corridor c, Actor me, PlayerInventory inv, PlayerCombat combat, PlayerStats stats)
        {
            yield return Wield(inv, combat, null);
            MakeWhole(me);
            Teleport(me, c.Start);
            ZombieAI ai = Stand(c, 1.2f, 0f, CloneZConfig.ZombieHealthTough);   // tough enough to show a full stomp on its head
            Actor body = ai.GetComponent<Actor>();
            ZombieHealth health = ai.GetComponent<ZombieHealth>();
            int shoves = 0, swings = 0, stomps = 0, landed = -1;
            string killedBy = null;
            bool gotUp = false;
            Action onShove = () => shoves++, onSwing = () => swings++, onStomp = () => stomps++;
            Action<int> onLanded = n => landed = n;
            Action<KillInfo> onKill = k => { if (k.Victim == body && killedBy == null) killedBy = k.WeaponId ?? "nothing"; };
            combat.OnShove += onShove;
            combat.OnSwing += onSwing;
            combat.OnStomp += onStomp;
            combat.OnBlowLanded += onLanded;
            GameEvents.ActorKilled += onKill;
            try
            {
                for (int i = 0; i < 24 && !ai.IsDowned; i++)
                {
                    yield return UntilGame(() => !ai.IsStaggered && !combat.MeleeBusy, 3f);
                    yield return WaitGame(combat.ShoveCooldown);
                    Place(ai, c, 1.2f, 0f);
                    stats.Endurance = CloneZConfig.EnduranceMax;
                    yield return Aim(ai.transform.position);
                    yield return Click(MouseButton.Right);
                    yield return WaitGame(0.1f);
                }
                Check("melee: a shove puts one of the dead on the ground", ai.IsDowned && body.Downed, shoves + " shoves");
                if (!ai.IsDowned) yield break;

                float full = FullStomp(me), before = health.Health;
                yield return UntilGame(() => !combat.MeleeBusy, 2f);   // the shove's own recovery
                Transform head = PlayerCombat.HeadOf(body);
                float headAway = head != null ? FlatDistance(head.position, me.transform.position) : -1f;
                yield return Aim(ai.transform.position);
                yield return Click(MouseButton.Left);
                yield return UntilGame(() => landed >= 0, 3f);
                float share = (before - Mathf.Max(0f, health.Health)) / full;
                Check("melee: then the attack stomps it where it lies: on its body, its head out of reach, for half",
                    stomps == 1 && swings == 0 && landed == 1 && headAway > CloneZConfig.StompReach && share >= 0.44f && share <= 0.56f,
                    stomps + " stomps, " + swings + " swings; its head " + F2(headAway) + " m off; " + F2(share) + " of a full stomp");
                if (head == null) yield break;

                // Beside its head now, as a player steps up to finish one.
                Teleport(me, Snap(head.position + c.Right * 0.8f, c.Start.y));
                yield return UntilGame(() => !combat.MeleeBusy, 2f);
                before = health.Health;
                landed = -1;
                yield return Aim(head.position);
                yield return Click(MouseButton.Left);
                if (!string.IsNullOrEmpty(s_ShotDir))
                {
                    yield return WaitGame(Mathf.Max(0f, combat.BlowWindUp * 0.6f - 0.03f));   // the knee at its highest
                    Vector3 feet = me.transform.position;
                    ShotFrom("melee-stomp", feet - c.Forward * 1.6f + c.Right * 0.25f + Vector3.up * 2.3f, (feet + head.position) * 0.5f + Vector3.up * 0.3f, 60f);
                }
                yield return UntilGame(() => landed >= 0, 3f);
                share = (before - Mathf.Max(0f, health.Health)) / full;
                Check("melee: stood beside its head, the stomp lands on that, in full",
                    stomps == 2 && landed == 1 && (health.IsDead || share >= 0.88f && share <= 1.12f),
                    stomps + " stomps; " + F2(share) + " of a full stomp" + (health.IsDead ? " (it died of it)" : ""));

                // Held, it stomps again as soon as the last is over, until there's nothing left to stomp.
                s_MouseState = s_MouseState.WithButton(MouseButton.Left, true);
                SendMouse(Vector2.zero, Vector2.zero);
                float end = Time.time + 6f;
                while (!ai.IsDeadState && Time.time < end)
                {
                    if (!ai.IsDowned) gotUp = true;
                    PointAt(head.position);
                    yield return null;
                }
                s_MouseState = s_MouseState.WithButton(MouseButton.Left, false);
                SendMouse(Vector2.zero, Vector2.zero);
                Check("melee: stomped to death on the ground, before it got up", ai.IsDeadState && killedBy == PlayerCombat.StompId && !gotUp,
                    stomps + " stomps, killed by " + (killedBy ?? "nothing yet") + (gotUp ? ", but it got up first" : ""));
            }
            finally
            {
                combat.OnShove -= onShove;
                combat.OnSwing -= onSwing;
                combat.OnStomp -= onStomp;
                combat.OnBlowLanded -= onLanded;
                GameEvents.ActorKilled -= onKill;
                ReleaseAll();
            }
            if (ai != null && !ai.IsDeadState) Dismiss(new List<ZombieAI> { ai });
        }

        // A full stomp on the head as StompLands works it out (bar its ±10%), in the health it takes off one of the dead.
        private static float FullStomp(Actor me)
        {
            var skills = me.GetComponent<PlayerSkills>();
            var clothing = me.GetComponent<PlayerClothing>();
            var stats = me.GetComponent<PlayerStats>();
            int strength = skills != null ? skills.GetLevel(PlayerSkills.Skill.Strength) : 0;
            float raw = CloneZConfig.StompDamage * (1f + strength * CloneZConfig.StompStrengthPerLevel) * PlayerModifiers.Current.MeleeDamage;
            if (clothing != null && clothing.GetWorn(ClothingSlot.Feet) != null) raw *= CloneZConfig.StompBootsMult;
            if (stats != null) raw *= stats.PainCombatMultiplier;
            return raw * CloneZConfig.NonPlayerReceivedDamageMultiplier;
        }
        #endregion

        #region The fight
        // Three of the dead come down the corridor at the survivor, who stands their ground at its mouth with the weapon, the
        // attack held while the nearest is in reach and the cursor on it, until all three are down for good. Whatever bites get
        // through are counted and healed at once (the run is about the weapons, not the survivor).
        private IEnumerator Fight(Corridor c, Actor me, PlayerInventory inv, PlayerCombat combat, PlayerStats stats, ItemDef weapon, List<string> fights)
        {
            string P = "melee: " + weapon.Id + ": ";
            yield return Wield(inv, combat, weapon);
            MakeWhole(me);
            Teleport(me, c.Start);
            MeleeStats m = combat.Melee;
            float health = CloneZConfig.ZombieHealthNormal + CloneZConfig.ZombieHealthVariance * 0.5f;
            var dead = new List<ZombieAI> { Stand(c, 6f, 0f, health), Stand(c, 7.3f, -0.5f, health), Stand(c, 7.3f, 0.5f, health) };
            int swings = 0, stomps = 0, connected = 0, broad = 0, widest = 0, staggers = 0, downs = 0, flinches = 0, lunges = 0, bites = 0;
            Action onSwing = () => swings++, onStomp = () => stomps++, onHurt = () => bites++;
            Action<int> onLanded = n =>
            {
                if (n >= 1) connected++;
                if (n >= 2) broad++;
                widest = Mathf.Max(widest, n);
            };
            Action<Vector3, float> onReel = (d, t) => staggers++;
            Action<Vector3> onDown = d => downs++, onFlinch = d => flinches++;
            Action<bool> onLunge = wall => { if (!wall) lunges++; };
            combat.OnSwing += onSwing;
            combat.OnStomp += onStomp;
            combat.OnBlowLanded += onLanded;
            stats.OnDamaged += onHurt;
            foreach (ZombieAI ai in dead)
            {
                ai.Staggered += onReel;
                ai.KnockedDown += onDown;
                ai.Flinched += onFlinch;
                ai.AttackStarted += onLunge;
            }
            float started = Time.time, cleared = -1f;
            int healedFor = 0;
            bool holding = false;
            me.Untargetable = false;
            try
            {
                yield return null;
                foreach (ZombieAI ai in dead) ai.Command(me);
                while (Time.time - started < FightTime && !stats.IsDead)
                {
                    ZombieAI near = null;
                    float nearest = float.MaxValue;
                    foreach (ZombieAI ai in dead)
                    {
                        if (ai == null || ai.IsDeadState) continue;
                        float d = FlatDistance(ai.transform.position, me.transform.position);
                        if (d < nearest) { nearest = d; near = ai; }
                    }
                    if (near == null)
                    {
                        cleared = Time.time - started;
                        break;
                    }
                    if (bites > healedFor)
                    {
                        healedFor = bites;
                        MakeWhole(me);
                    }
                    PointAt(near.transform.position);
                    // Swung as a player swings: when the nearest is in reach, or coming on and about to step into it as the
                    // blow lands (not at one reeling back out of reach).
                    bool hold = nearest <= m.Reach + 0.3f
                        || near.IsHunting && nearest <= m.Reach + 0.2f + CloneZConfig.ZombieShamblerSpeed * m.WindUp;
                    if (hold != holding)
                    {
                        holding = hold;
                        s_MouseState = s_MouseState.WithButton(MouseButton.Left, hold);
                        SendMouse(Vector2.zero, Vector2.zero);
                    }
                    yield return null;
                }
            }
            finally
            {
                s_MouseState = s_MouseState.WithButton(MouseButton.Left, false);
                SendMouse(Vector2.zero, Vector2.zero);
                me.Untargetable = true;
                combat.OnSwing -= onSwing;
                combat.OnStomp -= onStomp;
                combat.OnBlowLanded -= onLanded;
                stats.OnDamaged -= onHurt;
                foreach (ZombieAI ai in dead)
                {
                    if (ai == null) continue;
                    ai.Staggered -= onReel;
                    ai.KnockedDown -= onDown;
                    ai.Flinched -= onFlinch;
                    ai.AttackStarted -= onLunge;
                }
            }
            string line = weapon.Id + ": " + (cleared >= 0f ? "cleared in " + F2(cleared) + " s" : "not cleared in " + FightTime + " s") + "; "
                + swings + " swings, " + connected + " connected (" + broad + " struck two or more, at most " + widest + "), " + stomps
                + " stomps; the dead staggered " + staggers + ", knocked down " + downs + ", only flinched " + flinches + "; they lunged "
                + lunges + " times, " + bites + " got through";
            fights.Add(line);
            Check(P + "fights its way through three of the dead in the corridor", cleared >= 0f && !stats.IsDead, line);
            Check(P + "no swing struck more bodies than its arc carries through", widest <= m.MaxTargets, "at most " + widest + " a swing, " + m.MaxTargets + " allowed");
            Dismiss(dead);
            MakeWhole(me);
        }
        #endregion

        private static string F2(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
