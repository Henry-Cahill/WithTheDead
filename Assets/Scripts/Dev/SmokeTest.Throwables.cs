using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>
    /// <c>throwables</c>: the molotov, the pipe bomb and the noise lures (Weapons/Throwables.cs) against the dead, in the
    /// procedural town (or the zone named with <c>-clonez-smoke-zone</c>). It stands the town's people down first (they come
    /// to a din and shoot the dead), picks a house with a door it can shut and room round it, and:
    /// <list type="bullet">
    /// <item>crafts a molotov and a pipe bomb in the field (the molotov's litre out of a gas can) and throws the pipe bomb, which
    /// must go off where it came down;</item>
    /// <item>shuts the survivor in the house, sets <see cref="LureHorde"/> of the dead on them from outside and has them go
    /// quiet (the horde keeps at the house, as the dead keep at a door), then tosses an alarm clock out of the door: it must
    /// come down where it was thrown and ring, and pull the horde off the house — every one off its chase within seconds of
    /// the ring, most of them away to the clock — and once it has rung out, lie where it stopped to be picked up again;</item>
    /// <item>throws firecrackers (they must bang, call the dead and be spent) and the timer radio (it must come on and call
    /// the dead);</item>
    /// <item>opens the door, sets <see cref="DoorwayDead"/> fresh dead on the survivor through it and throws a molotov into the
    /// doorway as they come: the bottle must burst into a fire there that spreads, and the dead that come through must catch,
    /// burn and fall to it.</item>
    /// </list>
    /// </summary>
    public sealed partial class SmokeTest
    {
        private const int LureHorde = 10, DoorwayDead = 6;
        // How far out from the door the clock is thrown, and where the dead stand round the house (from its door).
        private const float LureThrow = 14f, LureNear = 2f, LureFar = 8f;

        private static readonly MethodInfo s_DoorHolds = typeof(RaidDoor).GetMethod("Holds", BindingFlags.NonPublic | BindingFlags.Instance);

        private IEnumerator ThrowablesRun()
        {
            yield return ToHub();
            yield return Deploy(RaidKind.Operative);
            Actor me = Actor.LocalPlayer;
            PlayerInventory inv = me != null ? me.GetComponent<PlayerInventory>() : null;
            if (inv == null) yield break;
            EndSpawnProtection();
            ClearThePeople(true);

            if (!PickHouse(me, inv, out RaidDoor door, out Vector3 outward, out Vector3 inside, out Vector3 clockAt, out string why))
            {
                Check("throwables: a house to lure the dead off", false, why);
                yield break;
            }
            Vector3 doorAt = door.transform.position;
            Check("throwables: a house to lure the dead off", true, why);

            if (OnFloor(doorAt + outward * 3f, doorAt.y, 2f, out Vector3 doorstep)) Teleport(me, doorstep);
            yield return new WaitForSeconds(0.5f);
            Vector3 blastAt = OnFloor(doorAt + outward * 12f, doorAt.y, 3f, out Vector3 open) ? open : doorAt + outward * 12f;
            yield return CraftAndBlast(me, inv, blastAt);
            yield return LureOffTheHouse(me, inv, door, outward, inside, clockAt);
            yield return CrackersAndRadio(me, inv, doorAt + outward * 7f, outward);
            yield return MolotovInTheDoorway(me, inv, door, outward, inside, clockAt);
            me.Untargetable = false;
        }

        // The dead ignore a survivor fresh into a raid for a minute; this run has no minute to spare.
        private static void EndSpawnProtection()
        {
            ExtractionManager m = ExtractionManager.Instance;
            if (m == null) return;
            PropertyInfo p = typeof(ExtractionManager).GetProperty(nameof(ExtractionManager.SpawnProtectionRemaining));
            p?.GetSetMethod(true)?.Invoke(m, new object[] { 0f });
        }

        // The town's people come to a din and shoot the dead this run sets on the survivor (and the dead go after them rather
        // than the clock): the roaming squads are stood down for the run, and whoever is about is sent away.
        private static void ClearThePeople(bool log)
        {
            bool squads = ALifeDirector.Instance != null;
            if (squads) Destroy(ALifeDirector.Instance.gameObject);
            int people = 0;
            foreach (HumanNpc n in new List<HumanNpc>(HumanNpc.All))
                if (n != null) { Destroy(n.gameObject); people++; }
            if (log) Debug.Log("[Smoke] " + s_Mode + ": the town cleared for the run (" + people + " people sent away" + (squads ? ", the roaming squads stood down" : "") + ")");
        }

        private static void Stock(PlayerInventory inv, string id, int n)
        {
            ItemDef d = ItemDatabase.Get(id);
            if (d == null) throw new Exception("missing item " + id);
            inv.Inventory.Add(d, n, enforceWeight: false);
        }

        // An outside door of a house near the survivor, on the ground floor: the NavMesh in the house behind it and round it
        // outside, and a clear lob from the doorstep to a spot LureThrow out (where the clock goes).
        private bool PickHouse(Actor me, PlayerInventory inv, out RaidDoor pick, out Vector3 outward, out Vector3 inside, out Vector3 clockAt, out string why)
        {
            pick = null;
            outward = inside = clockAt = Vector3.zero;
            var doors = new List<RaidDoor>();
            foreach (RaidDoor d in RaidDoor.Registry)
                if (d != null && !d.IsBroken && FlatDistance(d.transform.position, me.transform.position) < 250f) doors.Add(d);
            doors.Sort((a, b) => FlatDistance(a.transform.position, me.transform.position).CompareTo(FlatDistance(b.transform.position, me.transform.position)));
            int tried = 0;
            foreach (RaidDoor d in doors)
            {
                if (++tried > 60) break;
                Vector3 at = d.transform.position;
                Vector3 n = CloneZUtil.Flat(d.StandingPoint(1) - at).normalized;
                if (n == Vector3.zero) continue;
                bool inPlus = Inside(d, at + n * 1.2f), inMinus = Inside(d, at - n * 1.2f);
                if (inPlus == inMinus) continue;
                Vector3 o = inPlus ? -n : n;
                if (!OnFloor(at - o * 4.5f, at.y, out Vector3 inner) && !OnFloor(at - o * 3f, at.y, out inner)) continue;
                if (!OnFloor(at + o * 3f, at.y, out _)) continue;
                for (int k = 0; k < 7; k++)
                {
                    float yaw = (k + 1) / 2 * 20f * ((k & 1) == 0 ? 1f : -1f);
                    Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * o;
                    if (!OnFloor(at + dir * LureThrow, at.y, 3f, out Vector3 spot)) continue;
                    if (!ClearLob(inv, at + o * 2f + Vector3.up * 1.5f, spot)) continue;
                    pick = d;
                    outward = o;
                    inside = inner;
                    clockAt = spot;
                    why = "door " + FlatDistance(at, me.transform.position).ToString("0") + " m off, " + tried + " looked at, the clock's spot "
                        + FlatDistance(spot, at).ToString("0.0") + " m out";
                    return true;
                }
            }
            why = doors.Count + " doors within 250 m, " + tried + " looked at, none with the NavMesh both sides and a clear throw";
            return false;
        }

        private static bool Inside(RaidDoor d, Vector3 p) => s_DoorHolds != null && (bool)s_DoorHolds.Invoke(d, new object[] { p });

        private static bool OnFloor(Vector3 p, float floor, out Vector3 at) => OnFloor(p, floor, 1f, out at);

        private static bool OnFloor(Vector3 p, float floor, float reach, out Vector3 at)
        {
            at = p;
            if (!NavMesh.SamplePosition(new Vector3(p.x, floor + 0.1f, p.z), out NavMeshHit hit, reach, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - floor) > 0.6f)
                return false;
            at = hit.position;
            return true;
        }

        // The throw's arc from `from` to `to` (as Throwables would throw it) runs clear of the world.
        private static bool ClearLob(PlayerInventory inv, Vector3 from, Vector3 to)
        {
            Throwables.Launch(inv, to, from, out Vector3 p, out Vector3 v);
            Vector3 g = Physics.gravity;
            for (int i = 0; i < 80; i++)
            {
                Vector3 q = p + v * 0.05f + g * (0.5f * 0.05f * 0.05f);
                v += g * 0.05f;
                if (q.y < to.y + 0.3f) return true;
                if (Physics.Linecast(p, q, CloneZConfig.WorldMask, QueryTriggerInteraction.Ignore)) return false;
                p = q;
            }
            return false;
        }

        // The dead round `around`, on its side of the house (`outward`), `near`..`far` out.
        private static List<ZombieAI> RaiseDead(RaidDoor door, Vector3 around, Vector3 outward, float near, float far, int count, float fast, System.Random rng)
        {
            var dead = new List<ZombieAI>(count);
            for (int tries = 0; dead.Count < count && tries < count * 40; tries++)
            {
                float yaw = ((float)rng.NextDouble() * 2f - 1f) * 75f;
                float r = near + (float)rng.NextDouble() * (far - near);
                Vector3 p = around + Quaternion.Euler(0f, yaw, 0f) * outward * r;
                if (!OnFloor(p, around.y, out Vector3 at) || Inside(door, at)) continue;
                GameObject z = ZombieSpawner.BuildZombie(at);
                var ai = z.GetComponent<ZombieAI>();
                ai.Configure(rng.NextDouble() < fast ? ZombieType.FastShambler : ZombieType.Shambler);
                dead.Add(ai);
            }
            return dead;
        }

        private static float MeanDistance(List<ZombieAI> dead, Vector3 to)
        {
            float sum = 0f;
            int n = 0;
            foreach (ZombieAI ai in dead)
                if (ai != null && !ai.IsDeadState) { sum += FlatDistance(ai.transform.position, to); n++; }
            return n > 0 ? sum / n : -1f;
        }

        private static int Hunting(List<ZombieAI> dead)
        {
            int n = 0;
            foreach (ZombieAI ai in dead) if (ai != null && ai.IsHunting) n++;
            return n;
        }

        // How many are still on their feet (the town's people shoot at the dead too).
        private static int Standing(List<ZombieAI> dead)
        {
            int n = 0;
            foreach (ZombieAI ai in dead) if (ai != null && !ai.IsDeadState) n++;
            return n;
        }

        private static void Dismiss(List<ZombieAI> dead)
        {
            foreach (ZombieAI ai in dead) if (ai != null) Destroy(ai.gameObject);
            dead.Clear();
        }

        // A molotov and a pipe bomb made from the pack, and the pipe bomb thrown: it goes off where it came down.
        private IEnumerator CraftAndBlast(Actor me, PlayerInventory inv, Vector3 target)
        {
            Try("throwables: kit packed", () =>
            {
                foreach (var (id, n) in new[] { ("emptybottle", 1), ("rag", 1), ("gascan", 1), ("scrap", 2), ("chemicals", 2), ("ducttape", 1),
                             (Throwables.AlarmClock, 1), (Throwables.Firecrackers, 1), (Throwables.TimerRadio, 1), (Throwables.Molotov, 1) })
                    Stock(inv, id, n);
            });
            int molotovs = inv.Count(Throwables.Molotov), litres = GasLitres(inv);
            bool made = Hideout.FieldCraft(Hideout.GetRecipe("field_molotov"), inv, out string msg);
            Check("throwables: molotov crafted from a bottle, a rag and a litre of fuel", made && inv.Count(Throwables.Molotov) == molotovs + 1
                && GasLitres(inv) == litres - 1 && inv.Count("emptybottle") == 0 && inv.Count("rag") == 0,
                msg + "; the can " + litres + " -> " + GasLitres(inv) + " L");
            made = Hideout.FieldCraft(Hideout.GetRecipe("field_pipebomb"), inv, out msg);
            Check("throwables: pipe bomb crafted", made && inv.Count(Throwables.PipeBomb) == 1, msg);

            Vector3 blast = Vector3.zero;
            bool boomed = false;
            Action<Vector3, float, string> heard = (at, r, kind) => { if (kind == "explosion") { boomed = true; blast = at; } };
            GameEvents.WorldEvent += heard;
            bool thrown = Throwables.Throw(inv, Throwables.PipeBomb, target);
            yield return Until(() => boomed, Explosives.PipeBombFuse + 3f);
            GameEvents.WorldEvent -= heard;
            Check("throwables: pipe bomb thrown and goes off where it came down", thrown && boomed && FlatDistance(blast, target) < 4f,
                boomed ? "blast " + FlatDistance(blast, target).ToString("0.0") + " m from where it was aimed" : "no blast");
            yield return new WaitForSeconds(1f);
        }

        private static int GasLitres(PlayerInventory inv)
        {
            int n = 0;
            foreach (ItemStack s in inv.Inventory.Stacks)
                if (s != null && s.Def != null && s.Def.Id == "gascan") n += s.Condition;
            return n;
        }

        // The horde on the house, the survivor shut in and gone quiet, and an alarm clock tossed out of the door.
        private IEnumerator LureOffTheHouse(Actor me, PlayerInventory inv, RaidDoor door, Vector3 outward, Vector3 inside, Vector3 clockAt)
        {
            Vector3 doorAt = door.transform.position;
            if (door.IsOpen) door.SetOpen(false, false);
            me.Untargetable = false;
            ClearThePeople(true);
            Teleport(me, inside);
            yield return new WaitForSeconds(1.5f);   // the shut door's carve in
            List<ZombieAI> horde = RaiseDead(door, doorAt + outward * 2.5f, outward, LureNear, LureFar, LureHorde, 0.3f, new System.Random(20261109));
            yield return null;
            foreach (ZombieAI ai in horde) ai.Command(me);
            Check("throwables: the horde on the house", horde.Count >= LureHorde - 2, horde.Count + " of " + LureHorde + " raised outside the door");
            yield return new WaitForSeconds(1.5f);
            // The survivor goes quiet behind the door: the dead lose sight of them and keep at the house.
            me.Untargetable = true;
            yield return new WaitForSeconds(1f);

            int clocks = inv.Count(Throwables.AlarmClock);
            bool thrown = Throwables.Throw(inv, Throwables.AlarmClock, clockAt, doorAt + outward * 2f + Vector3.up * 1.5f);
            NoiseLure clock = null;
            foreach (NoiseLure l in NoiseLure.All) if (l.Thrown && l.Def == NoiseLure.AlarmClock) clock = l;
            Check("throwables: alarm clock thrown out of the door", thrown && clock != null && inv.Count(Throwables.AlarmClock) == clocks - 1);
            if (clock == null) { Dismiss(horde); yield break; }

            // How the horde stands the last frame before the ring (the frame it rings in, the dead may already be answering it).
            int hunting = 0, standing = 0;
            float atHouse = 0f;
            yield return Until(() =>
            {
                if (clock == null || clock.Sounding) return true;
                hunting = Hunting(horde);
                standing = Standing(horde);
                atHouse = MeanDistance(horde, doorAt);
                return false;
            }, NoiseLure.AlarmClock.Delay + 2f);
            bool rang = clock != null && clock.Sounding;
            Vector3 rangAt = clock != null ? clock.transform.position : Vector3.zero;
            float toClock = MeanDistance(horde, rangAt);
            Check("throwables: the clock comes down where it was thrown and rings", rang && FlatDistance(rangAt, clockAt) < 4f,
                rang ? FlatDistance(rangAt, clockAt).ToString("0.0") + " m from the spot aimed at, " + FlatDistance(rangAt, doorAt).ToString("0.0") + " m from the door" : "silent");
            Check("throwables: the horde still on the house when it rings", standing >= LureHorde / 2 && hunting >= standing * 0.7f && atHouse < 9f,
                hunting + " of the " + standing + " standing still after the survivor, " + atHouse.ToString("0.0") + " m from the door on average");

            yield return new WaitForSeconds(3f);
            Check("throwables: the clock pulls them off the chase", Hunting(horde) == 0, Hunting(horde) + " of " + Standing(horde) + " still after the survivor 3 s on");

            float traceAt = 0f, t0 = Time.time;
            yield return Until(() =>
            {
                if (Time.time - t0 >= traceAt)
                {
                    traceAt += 5f;
                    Debug.Log("[Smoke] lure trace " + (Time.time - t0 + 3f).ToString("0") + " s into the ring: " + MeanDistance(horde, doorAt).ToString("0.0")
                        + " m from the door, " + MeanDistance(horde, rangAt).ToString("0.0") + " m from the clock on average");
                }
                return false;
            }, 17f);
            int off = 0, alive = 0;
            foreach (ZombieAI ai in horde)
            {
                if (ai == null || ai.IsDeadState) continue;
                alive++;
                float fromDoor = FlatDistance(ai.transform.position, doorAt), fromClock = FlatDistance(ai.transform.position, rangAt);
                if (fromDoor > 7f && fromClock < fromDoor) off++;
            }
            Check("throwables: a thrown alarm clock pulls the horde off the house", alive >= LureHorde / 2 && off >= alive * 0.7f,
                off + " of " + alive + " gone off to the clock; on average " + MeanDistance(horde, doorAt).ToString("0.0") + " m from the door (was "
                + atHouse.ToString("0.0") + "), " + MeanDistance(horde, rangAt).ToString("0.0") + " m from the clock (was " + toClock.ToString("0.0") + ")");

            // Once it has rung out it lies where it stopped, to be picked up and thrown again.
            float left = clock != null ? NoiseLure.AlarmClock.Duration - (Time.time - clock.SoundedAt) : 0f;
            Vector3 lastAt = clock != null ? clock.transform.position : rangAt;
            float waitUntil = Time.time + left + 3f;
            while (clock != null && Time.time < waitUntil)
            {
                lastAt = clock.transform.position;
                yield return null;
            }
            WorldItem lying = WorldItem.Nearest(lastAt, 1.5f);
            bool clockLies = lying != null && lying.Def != null && lying.Def.Id == Throwables.AlarmClock;
            int got = clockLies ? inv.PickUp(lying) : 0;
            Check("throwables: the clock rings out and lies there to be picked up again", clock == null && clockLies && got == 1
                && inv.Count(Throwables.AlarmClock) == clocks, clockLies ? "picked up" : "nothing lying where it stopped");
            Dismiss(horde);
        }

        // Firecrackers bang, call the dead and are spent; the timer radio comes on and calls them.
        private IEnumerator CrackersAndRadio(Actor me, PlayerInventory inv, Vector3 standAt, Vector3 outward)
        {
            if (OnFloor(standAt, standAt.y, 3f, out Vector3 stand)) Teleport(me, stand);
            yield return new WaitForSeconds(0.5f);
            Vector3 aim = me.transform.position + outward * 8f;

            Throwables.Throw(inv, Throwables.Firecrackers, aim);
            NoiseLure crackers = null;
            foreach (NoiseLure l in NoiseLure.All) if (l.Thrown && l.Def == NoiseLure.Firecrackers) crackers = l;
            bool calledDead = false;
            float until = Time.time + NoiseLure.Firecrackers.Delay + 3f;
            while (crackers != null && Time.time < until)
            {
                if (crackers.Sounding && NoiseSystem.TryHearLure(crackers.transform.position + Vector3.right * 40f, CloneZConfig.ZombieHearingRadius, out _)) calledDead = true;
                yield return null;
            }
            int bangs = crackers != null ? crackers.Bangs : 0;
            Check("throwables: firecrackers bang and call the dead from 40 m", crackers != null && bangs >= 5 && calledDead, bangs + " bangs");
            yield return new WaitForSeconds(NoiseLure.Firecrackers.Duration + 1f);
            bool leftLying = false;
            if (crackers == null)
            {
                WorldItem w = WorldItem.Nearest(aim, 4f);
                leftLying = w != null && w.Def != null && w.Def.Id == Throwables.Firecrackers;
            }
            Check("throwables: firecrackers are spent", inv.Count(Throwables.Firecrackers) == 0 && !leftLying && (crackers == null || !crackers.Sounding));
            yield return new WaitForSeconds(3f);

            Throwables.Throw(inv, Throwables.TimerRadio, aim);
            NoiseLure radio = null;
            foreach (NoiseLure l in NoiseLure.All) if (l.Thrown && l.Def == NoiseLure.TimerRadio) radio = l;
            yield return Until(() => radio == null || radio.Sounding, NoiseLure.TimerRadio.Delay + 2f);
            yield return new WaitForSeconds(0.5f);
            bool playing = radio != null && radio.Sounding;
            bool heard = playing && NoiseSystem.TryHearLure(radio.transform.position + Vector3.forward * 25f, CloneZConfig.ZombieHearingRadius, out _);
            Check("throwables: the timer radio comes on and calls the dead", playing && heard);
            if (radio != null) Destroy(radio.gameObject);   // a minute's din would call the next lot to it
            yield return new WaitForSeconds(0.5f);
        }

        // A fresh lot of the dead set on the survivor through the open door, and a molotov thrown into the doorway as they come
        // (then the survivor gets out of the way, to `clear`: a burning one passes the fire on to anyone it's against).
        private IEnumerator MolotovInTheDoorway(Actor me, PlayerInventory inv, RaidDoor door, Vector3 outward, Vector3 inside, Vector3 clear)
        {
            Vector3 doorAt = door.transform.position;
            if (!door.IsOpen) door.SetOpen(true, false);
            me.Untargetable = false;
            ClearThePeople(true);
            Teleport(me, inside);
            yield return new WaitForSeconds(1.5f);   // the open door's carve lifts
            List<ZombieAI> dead = RaiseDead(door, doorAt + outward * 1.5f, outward, 2.5f, 5.5f, DoorwayDead, 0f, new System.Random(1109));
            var actors = new List<Actor>();
            foreach (ZombieAI ai in dead) actors.Add(ai.GetComponent<Actor>());
            yield return null;
            foreach (ZombieAI ai in dead) ai.Command(me);
            yield return new WaitForSeconds(0.5f);
            me.Untargetable = true;   // they come on to where they last saw the survivor, through the doorway

            yield return Until(() =>
            {
                foreach (ZombieAI ai in dead) if (ai != null && FlatDistance(ai.transform.position, doorAt) < 3.2f) return true;
                return false;
            }, 10f);
            float nearest = float.MaxValue;
            foreach (ZombieAI ai in dead) if (ai != null) nearest = Mathf.Min(nearest, FlatDistance(ai.transform.position, doorAt));

            var burnt = new HashSet<Actor>();
            int killedByFire = 0, fellAlight = 0;
            var shotBy = new List<string>();
            float t0 = Time.time;
            Action<KillInfo> killed = k =>
            {
                int i = k.Victim != null ? actors.IndexOf(k.Victim) : -1;
                if (i < 0) return;
                if (k.WeaponId == Throwables.Molotov) killedByFire++;
                else shotBy.Add(k.WeaponId ?? "nothing");
                Debug.Log("[Smoke] doorway death: zombie " + i + " by " + (k.WeaponId ?? "nothing") + " at " + (Time.time - t0).ToString("0.0")
                    + " s, " + FlatDistance(k.Position, doorAt).ToString("0.0") + " m from the doorway");
            };
            GameEvents.ActorKilled += killed;
            for (int i = 0; i < dead.Count; i++)
            {
                int n = i;
                ZombieHealth h = dead[i].GetComponent<ZombieHealth>();
                if (h != null) h.OnDeath += z => { if (Aflame.IsOn(actors[n])) fellAlight++; };
            }
            int molotovs = inv.Count(Throwables.Molotov);
            bool thrown = Throwables.Throw(inv, Throwables.Molotov, doorAt);
            FirePatch fire = null;
            t0 = Time.time;
            yield return new WaitForSeconds(0.2f);
            Teleport(me, clear);
            yield return Until(() =>
            {
                foreach (FirePatch f in FirePatch.All) if (f.Authority) fire = f;
                return fire != null;
            }, 3f);
            Check("throwables: molotov thrown into the doorway bursts into a fire there", thrown && fire != null && inv.Count(Throwables.Molotov) == molotovs - 1
                && FlatDistance(fire.transform.position, doorAt) < 2.5f,
                fire != null ? "fire " + FlatDistance(fire.transform.position, doorAt).ToString("0.0") + " m from the doorway, the nearest of the dead "
                    + nearest.ToString("0.0") + " m off when it was thrown" : "no fire");

            int spread0 = Aflame.Spread, spots = 0;
            while (Time.time - t0 < 18f)
            {
                foreach (Actor a in actors) if (a != null && Aflame.IsOn(a)) burnt.Add(a);
                if (fire != null) spots = Mathf.Max(spots, fire.Spots);
                yield return null;
            }
            GameEvents.ActorKilled -= killed;
            int fallen = 0;
            for (int i = 0; i < dead.Count; i++)
            {
                ZombieAI ai = dead[i];
                if (ai == null || ai.IsDeadState) fallen++;
                Debug.Log("[Smoke] doorway zombie " + i + ": " + (ai == null ? "gone" : ai.IsDeadState ? "dead" : "standing "
                    + FlatDistance(ai.transform.position, doorAt).ToString("0.0") + " m from the doorway") + ", " + (burnt.Contains(actors[i]) ? "burnt" : "never alight"));
            }
            Check("throwables: the fire spreads", spots > 1, spots + " places alight (the pool and its tongues)");
            Check("throwables: a molotov burns the dead in a doorway", burnt.Count >= DoorwayDead - 1 && fellAlight >= DoorwayDead - 2
                && killedByFire >= DoorwayDead - 2,
                burnt.Count + " of " + dead.Count + " caught fire, " + fellAlight + " fell alight, " + killedByFire + " to the fire itself"
                + (shotBy.Count > 0 ? " (shot by someone: " + string.Join(", ", shotBy) + ")" : "") + ", " + fallen + " down in all; "
                + (Aflame.Spread - spread0) + " caught it from another");
            Dismiss(dead);
        }
    }
}
