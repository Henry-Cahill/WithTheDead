using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// One thing the survivor can do at a fixture — "Turn on the tap", "Close the curtains", "Sit down".
    /// Built fresh by <see cref="Interactable.GetActions"/> every time the prompt or the action menu asks,
    /// so its label and availability always reflect the fixture's current state. An action is either
    /// instant (<see cref="Duration"/> 0: <see cref="Run"/> fires at once) or timed (a progress bar fills
    /// while the survivor stays put; moving, getting hurt or walking off abandons it). A negative duration
    /// is an open-ended job ("Watch TV") that runs until the survivor stops it.
    /// </summary>
    public sealed class InteractAction
    {
        /// <summary>The menu / prompt text, phrased as an order: "Open the fridge".</summary>
        public string Label;
        /// <summary>False: listed greyed out with <see cref="DisabledReason"/>; [E] on it explains why instead.</summary>
        public bool Enabled = true;
        public string DisabledReason;
        /// <summary>0 = instant; &gt;0 = seconds of work; &lt;0 = runs until stopped (moving or [E] stops it).</summary>
        public float Duration;
        /// <summary>The progress bar's text while a timed action runs ("Washing up"); defaults to <see cref="Label"/>.</summary>
        public string BusyLabel;
        /// <summary>The survivor's pose while a timed action runs.</summary>
        public HumanoidActivity Activity = HumanoidActivity.Use;
        /// <summary>Timed actions only: true lets the survivor walk about without abandoning it (rare).</summary>
        public bool AllowMove;

        /// <summary>Instant: performed at once. Timed: performed when the bar fills (never for an open-ended job).</summary>
        public Action<InteractContext> Run;
        /// <summary>Timed: called once when the work starts (step into the shower, switch the TV on).</summary>
        public Action<InteractContext> Begin;
        /// <summary>Timed: called every frame while the work runs, with the frame time and progress 0..1
        /// (open-ended jobs pass the seconds elapsed instead).</summary>
        public Action<InteractContext, float, float> Tick;
        /// <summary>Timed: called when the work is abandoned or stopped before <see cref="Run"/> (step back out).</summary>
        public Action<InteractContext> Stopped;

        public InteractAction(string label, Action<InteractContext> run = null)
        {
            Label = label;
            Run = run;
        }

        /// <summary>Makes this a timed action of <paramref name="seconds"/> (negative: open-ended).</summary>
        public InteractAction Timed(float seconds, string busyLabel = null, HumanoidActivity activity = HumanoidActivity.Use)
        {
            Duration = seconds;
            BusyLabel = busyLabel;
            Activity = activity;
            return this;
        }

        /// <summary>Greys the action out (when <paramref name="disabled"/>) with a reason the survivor is told.</summary>
        public InteractAction DisabledIf(bool disabled, string reason)
        {
            if (disabled)
            {
                Enabled = false;
                DisabledReason = reason;
            }
            return this;
        }

        public bool IsTimed => Duration != 0f;
        public bool IsOpenEnded => Duration < 0f;
    }

    /// <summary>
    /// The survivor, as a fixture's actions see it: every gameplay component they might touch, plus a
    /// way to tell the player something (<see cref="Notify"/>, a HUD toast). One instance lives on the
    /// <see cref="PlayerInteractor"/> and is handed to every <see cref="Interactable"/> call.
    /// </summary>
    public sealed class InteractContext
    {
        public GameObject Player;
        public Transform Transform;
        public PlayerStats Stats;
        public PlayerInventory Inventory;
        public PlayerInteractor Interactor;
        public PlayerController Controller;
        public CharacterController Body;
        public PlayerSleep Sleep;
        public PlayerWounds Wounds;
        public PlayerPanic Panic;
        public PlayerTemperature Temperature;
        public PlayerFoodSickness Sickness;
        public PlayerSkills Skills;
        public PlayerSeat Seat;
        public PlayerHydration Hydration;

        /// <summary>The survivor's feet.</summary>
        public Vector3 Position => Transform != null ? Transform.position : Vector3.zero;

        /// <summary>A point at the survivor's chest (for sight lines).</summary>
        public Vector3 Chest => Position + Vector3.up * 1.2f;

        /// <summary>Shows a short message on the HUD (a toast above the prompts).</summary>
        public void Notify(string message)
        {
            if (Interactor != null) Interactor.Notify(message);
        }

        /// <summary>Moves the survivor (and their body's collider) to <paramref name="position"/>, facing
        /// <paramref name="yaw"/> degrees when given — for stepping through a window or into a shower.</summary>
        public void Teleport(Vector3 position, float? yaw = null)
        {
            if (Transform == null) return;
            bool cc = Body != null && Body.enabled;
            if (cc) Body.enabled = false;
            Transform.position = position;
            if (yaw.HasValue) Transform.rotation = Quaternion.Euler(0f, yaw.Value, 0f);
            if (cc) Body.enabled = true;
            var avatar = Player != null ? Player.GetComponent<PlayerAvatar>() : null;
            if (avatar != null && avatar.Animator != null) avatar.Animator.Snap();
        }
    }

    /// <summary>
    /// Anything in the world the survivor can walk up to and use: a door, a tap, a cupboard, a lamp, a chair.
    /// Subclasses describe themselves (<see cref="DisplayName"/>, <see cref="InteractPoint"/>) and list what
    /// can be done right now (<see cref="GetActions"/>); the <see cref="PlayerInteractor"/> does the rest —
    /// it picks the fixture under the cursor (or the nearest one in front of the survivor), shows
    /// "[E] main action  [Q] more" with the fixture's name hung over it, runs instant and timed actions,
    /// and opens the numbered action menu on Q.
    ///
    /// Persistence: a fixture with a <see cref="SaveKey"/> is written to the save's fixture table with
    /// whatever <see cref="CaptureState"/> records, and handed it back through <see cref="RestoreState"/>
    /// once a loaded world is fully built. Keys must be stable across runs — build them from the fixture's
    /// kind and its (saved) position with <see cref="KeyFor"/>. A fixture's storage lives in an ordinary
    /// <see cref="LootContainer"/> with a <see cref="LootContainer.FixtureKey"/> and is saved with the
    /// other containers (see <see cref="StorageFixture"/>).
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        /// <summary>Every live fixture, for the interactor's targeting and the save.</summary>
        public static readonly List<Interactable> All = new List<Interactable>();

        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        /// <summary>The short name shown over the fixture and atop its action menu: "Kitchen sink".</summary>
        public abstract string DisplayName { get; }

        /// <summary>The world point the survivor reaches for — the label hangs over it and the reach and the
        /// cursor pick are measured to it. Put it on the part you'd touch (a tap, a drawer front, a switch).</summary>
        public virtual Vector3 InteractPoint => transform.position;

        /// <summary>How close (flat metres) the survivor must stand to <see cref="InteractPoint"/>.</summary>
        public virtual float Reach => CloneZConfig.FixtureReach;

        /// <summary>False hides the fixture from targeting altogether (a part that's been torn away).</summary>
        public virtual bool IsAvailable => isActiveAndEnabled;

        /// <summary>Colliders under this transform don't block the sight line to <see cref="InteractPoint"/>
        /// (a piece of furniture's own box collider, a closed door's leaf). Defaults to the fixture's own
        /// transform; a fixture on a piece of furniture returns the furniture's root. Never return a wall
        /// piece: the sight line is what stops the survivor using a wall fixture from the far side.</summary>
        public virtual Transform OcclusionRoot => transform;

        /// <summary>Lists every action available right now, the [E] default first. Called for the prompt every
        /// frame the fixture is targeted, so keep it cheap (no allocations beyond the actions themselves).</summary>
        public abstract void GetActions(InteractContext ctx, List<InteractAction> into);

        #region Persistence
        /// <summary>A stable id for the save (see <see cref="KeyFor"/>), or null when there's nothing to keep.</summary>
        public virtual string SaveKey => null;

        /// <summary>Records the fixture's state (open, switched on, latched…) into <paramref name="save"/>.</summary>
        public virtual void CaptureState(FixtureSave save) { }

        /// <summary>Puts back a state recorded by <see cref="CaptureState"/> (called once the loaded world is built).</summary>
        public virtual void RestoreState(FixtureSave save) { }

        /// <summary>A save key from a fixture's kind and position (to the centimetre) and an index among
        /// several of the same kind on one host: <c>"cupboard@143,437#1"</c>. A fixture off the ground floor (a real
        /// building's upper floors and basement) has its height too, to the decimetre, so it never shares the key of the
        /// one standing at the same spot a floor below or above: <c>"bed@143,437^34"</c>.</summary>
        public static string KeyFor(string kind, Vector3 position, int index = 0)
        {
            int x = Mathf.RoundToInt(position.x * 100f), z = Mathf.RoundToInt(position.z * 100f);
            string key = index == 0 ? kind + "@" + x + "," + z : kind + "@" + x + "," + z + "#" + index;
            if (position.y >= UpperFloorKeys || position.y <= LowerFloorKeys) key += "^" + Mathf.RoundToInt(position.y * 10f);
            return key;
        }

        /// <summary>Fixtures this high (or this low) are on a floor above (or below) the ground floor: every storey is at
        /// least 3 m, a basement's floor 2.7 m down.</summary>
        private const float UpperFloorKeys = 2.9f, LowerFloorKeys = -0.3f;

        /// <summary>Puts every saved fixture state back on the live fixtures with the same key, and every saved
        /// fixture container's contents into the live container with the same <see cref="LootContainer.FixtureKey"/>.</summary>
        public static void RestoreAll(List<FixtureSave> fixtures, List<ContainerSave> containers)
        {
            if (fixtures != null && fixtures.Count > 0)
            {
var byKey = new Dictionary<string, FixtureSave>(fixtures.Count);
                  for (int i = 0; i < fixtures.Count; i++)
                  {
                      var fs = fixtures[i];
                      if (fs == null || string.IsNullOrEmpty(fs.Key)) continue;
                      if (byKey.ContainsKey(fs.Key))
                      {
                          Debug.LogWarning("[CloneZ] Two fixtures share the save key " + fs.Key + "; only the first is restored.");
                          continue;
                      }
                      byKey[fs.Key] = fs;
                  }
                // Restoring can add or remove fixtures (a lamp group, a part torn away), so walk a copy.
                var live = new List<Interactable>(All);
                for (int i = 0; i < live.Count; i++)
                {
                    Interactable f = live[i];
                    string key = f != null ? f.SaveKey : null;
                    if (key != null && byKey.TryGetValue(key, out FixtureSave s))
                    {
                        try { f.RestoreState(s); }
                        catch (Exception e) { Debug.LogWarning("[CloneZ] Could not restore fixture " + key + ": " + e.Message); }
                    }
                }
            }

            if (containers != null && containers.Count > 0)
            {
                // Matched one-to-one in save order, so a key that (wrongly) appears twice can never copy one
                // container's contents into another.
                var byKey = new Dictionary<string, Queue<ContainerSave>>();
                for (int i = 0; i < containers.Count; i++)
                {
                    ContainerSave cs = containers[i];
                    if (cs == null || string.IsNullOrEmpty(cs.FixtureKey)) continue;
                    if (!byKey.TryGetValue(cs.FixtureKey, out Queue<ContainerSave> q)) byKey[cs.FixtureKey] = q = new Queue<ContainerSave>(1);
                    q.Enqueue(cs);
                }
                if (byKey.Count == 0) return;
                for (int i = 0; i < LootContainer.All.Count; i++)
                {
                    LootContainer c = LootContainer.All[i];
                    if (c == null || c.FixtureKey == null || !byKey.TryGetValue(c.FixtureKey, out Queue<ContainerSave> q) || q.Count == 0) continue;
                    ContainerSave s = q.Dequeue();
                    c.RestoreContents(s.Searched, s.Items);
                }
            }
        }

        /// <summary>Snapshots every fixture that has a <see cref="SaveKey"/>.</summary>
        public static void CaptureAll(List<FixtureSave> into)
        {
            // Track which save keys have already been processed to avoid duplicates.
            var seen = new HashSet<string>();
            // Iterate over every live fixture to capture its save data.
            for (int i = 0; i < All.Count; i++)
            {
                Interactable f = All[i];
                // Skip fixtures that don't have a persistent save key.
                string key = f != null ? f.SaveKey : null;
                if (key == null) continue;
                // Emit a warning and skip this fixture if its key was already seen.
                if (!seen.Add(key))
                {
                    Debug.LogWarning("[CloneZ] Two fixtures share the save key " + key + "; only the first is saved.");
                    continue;
                }
                // Create a new fixture save record and ask the fixture to fill it with its current state.
                var s = new FixtureSave { Key = key };
                try { f.CaptureState(s); }
                catch (Exception e) { Debug.LogWarning("[CloneZ] Could not save fixture " + key + ": " + e.Message); continue; }
                into.Add(s);
            }
        }
        #endregion

        #region Helpers for subclasses
        // Example: a ToggleLamp creates a child Light component to visualise its on/off state.
        /// <summary>Which side of a plane (through <paramref name="point"/>, facing <paramref name="normal"/>) the
        /// survivor stands on: +1 in front, -1 behind.</summary>
        protected static int SideOf(Vector3 survivor, Vector3 point, Vector3 normal)
        {
            Vector3 d = survivor - point;
            d.y = 0f;
            return Vector3.Dot(d, normal) >= 0f ? 1 : -1;
        }

        /// <summary>Emits a noise the dead can hear (<see cref="NoiseSystem"/>) from the fixture.</summary>
        protected void MakeNoise(float radius)
        {
            if (radius > 0f) NoiseSystem.Emit(InteractPoint, radius);
        }
/// <summary>Adds a simple "Inspect" action that shows the fixture’s name in a HUD toast.</summary>
         protected void AddInspectAction(InteractContext ctx, List<InteractAction> into)
         {
             into.Add(new InteractAction("Inspect", c => c.Notify($"You look at the {DisplayName}.")));
         }

         /// <summary>Adds a generic "Shake" instant action that emits noise and shows a HUD toast.
         /// Useful for luring zombies or alerting the survivor.</summary>
         protected void AddShakeAction(InteractContext ctx, List<InteractAction> into, float radius = 2f, string toast = null)
         {
             string message = toast ?? $"You shake the {DisplayName}.";
             into.Add(new InteractAction("Shake", c => {
                 MakeNoise(radius);
                 c.Notify(message);
             }));
         }

         #endregion
    }
}
