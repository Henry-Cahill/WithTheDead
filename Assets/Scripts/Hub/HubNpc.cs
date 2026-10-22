using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Someone in the hub: a station keeper, a patron nursing a drink, a guitarist on the stage, an old-timer with
    /// a story, a beggar by the door. Metro's living stations: players return to people, not menus. They talk when
    /// you're near, look at you, and some of them have something worth hearing — rumours are knowledge, and
    /// knowledge is the real loot.
    /// </summary>
    public class HubNpc : MonoBehaviour
    {
        public enum Role { Keeper, Patron, Musician, Storyteller, Beggar, Guide }

        public Role Kind;
        public string Name;
        public string Script;
        public Vector3 LookAt;

        private HumanoidAnimator _anim;
        private float _nextLine;
        private int _lineIndex;
        private AudioSource _music;
        private System.Random _rng;

        private static readonly Dictionary<string, string[]> Lines = new Dictionary<string, string[]>
        {
            { "barkeep", new[] { "Rent's due on the seventh, same as always.", "You want a drink or you want to stand there?", "Heard the Spillway coughed up something big last week.", "Wipe your boots. Some of that mud glows.", "Freehold's taxes never sleep.", "The Wardens patrol after dark.", "The Choir's hymns echo near the ruins.", "Snow drifts block the road to Kestrel, but the lamp still flickers.", "The wind whistles through the broken panes, carrying whispers of the Zone.", "Rumor has it the old well still runs under the ice, feeding the dead.", "Lights on the porch are dim, but the barkeep keeps the fire alive.", "I saw a shape move past the snowbank, silent as a dead rat.", "When the night falls, the Zone sings louder than the choir." } },
            { "fence", new[] { "No receipts, no questions.", "The Institute pays in pennies and paperwork. I pay in rounds.", "Wardens were sniffing round yesterday. Keep your head down.", "Anything that hums, I'll take.", "Freehold's borders shift at night.", "Mirelock's mist hides contraband.", "Vanguard's guards demand loyalty.", "Winter snows seal the northern road, but the market finds a breach.", "Spare parts trade for whispers; a busted transmitter buys safety.", "The Wardens watch the docks; a stray lantern draws their gaze.", "Rumour says a hidden cache lies beneath the old well, guarded by frozen wires.", "Contraband moves in hushed tones, wrapped in stolen coats." } },
            { "institute", new[] { "Every artifact is a sentence in a language we can't read.", "Bring me samples, not stories.", "Legal work, legal pay. You'll sleep at night.", "Please don't lick anything you find.", "Vanguard's labs whisper of ghosts.", "The Passing's echo lingers here.", "Kestrel's winds bring news." } },
            { "gunsmith", new[] { "The round matters more than the rifle.", "Pick your branch carefully — I can't un-bore a barrel.", "Clean your gun. The Zone gets into everything.", "That vest won't stop what the Choir carries.", "The Vanguard's steel cuts deeper than any blade.", "Freehold's guns rarely miss, but they rust.", "Mirelock's damp keeps the powder soggy." } },
            { "medic", new[] { "Sit. Bleeding is not a conversation.", "Know your blood type. Out there nobody will ask.", "Splint first, run second.", "You came back. Most don't, the first winter.", "Freehold's medics never get a day off.", "Wardens' field kits run low quickly.", "Mirelock's chill slows every wound." } },
            { "guide", new[] { "The safe way is never the short way.", "Throw a bolt before every step you're not sure of.", "North Camp's further than it looks. Further than it is, sometimes.", "I've walked men to the edge of the Spillway. Fewer came back.", "Freehold's roads twist like the Zone's rivers.", "The Choir's songs guide you through fog.", "Vanguard's maps mark the safest paths." } },
            { "merc", new[] { "Two of us see twice as much.", "My rate's per day, not per corpse.", "I don't walk first. Not for any money.", "The roads are slick with ice; a slip can get you buried before you draw a weapon.", "I've heard the old well under the ice still whispers, and the dead listen.", "My fee climbs with the wind—extra for a night that drifts like a white shroud.", "I'll fight for the highest bidder, but I won’t turn my back on a comrade in a blizzard.", "Don't chase a shadow through the snowbank unless you’re ready to become one." } },
            { "patron", new[] { "Lost my cousin to a sinkhole. Didn't even see it.", "If you hear the siren, you've already waited too long.", "Don't shoot the ones walking home. They don't mean anything by it.", "The fog in Mirelock talks. Don't answer it.", "Saw a thing with no face near the cooling tower.", "Another round. For the ones who stayed out." } },
            { "beggar", new[] { "Spare a round for an old runner?", "I went in eleven times. The twelfth took my legs.", "Bless you. The Zone remembers." } },
            { "guitar", new[] { "This one's for the ones who didn't come back.", "Request? Only if you're buying.", "Same three chords since the Passing.", "Snow‑capped pines hum a low dirge when the wind sighs.", "Pay a copper and I’ll sing the tale of the frozen well.", "My fingers are numb, but the strings still bleed the old hymns.", "Ask for a ballad of the Spillway and you’ll hear the ice crack.", "Even in the gloom, a tune can warm a throat that’s lost its fire." } },
            { "story_pell", new[] { "Listen, then. Third year after the Passing, we found a sinkhole in Kestrel that ate a whole truck.",
                                    "Ripple in the air, nothing more. Truck went in, came out the size of a suitcase.",
                                    "We learned that day: throw a bolt, watch it fall, step where it didn't twitch.",
                                    "Kestrel's sinkholes sit by the north road. Keep to the ditch and you'll live." } },
            { "story_dana", new[] { "You want to know about the Lodestar? Everyone does, sooner or later.",
                                    "They say it sits at the bottom of the Spillway and listens.",
                                    "The Choir sings to it. The ones who reach it get what they ask for... or what they deserve.",
                                    "My brother went looking. He left a journal page in Mirelock. Find it, if you're mad enough." } },
        };
        public static readonly string[] GuideHints = new[] { "Take the left fork near the old mill.", "Avoid the frozen river – the ice cracks under weight.", "The road by the burnt barn saves you an hour.", "Keep to the high ground when the fog rolls in.", "Follow the charcoal markers; they’re the only reliable trail." };

        public static HubNpc Spawn(Transform parent, Vector3 position, Vector3 lookAt, string name, string faction, Role role, string script, bool seated = false)
        {
            var go = new GameObject("HubNpc " + name);
            go.transform.SetParent(parent, false);
            float seatHeight = position.y > 0.1f ? position.y : 0.46f;
            go.transform.position = new Vector3(position.x, role == Role.Musician ? 0.24f : 0f, position.z);
            Vector3 face = lookAt - go.transform.position;
            face.y = 0f;
            if (face.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);

            var npc = go.AddComponent<HubNpc>();
            npc.Kind = role;
            npc.Name = name;
            npc.Script = script;
            npc.LookAt = lookAt;
            npc._rng = new System.Random(name.GetHashCode());

            HumanoidLook look = HumanoidLook.Survivor();
            npc.Dress(look, faction, role);
            HumanoidRig rig = HumanoidRig.Create(go.transform, look);
            npc._anim = rig.gameObject.AddComponent<HumanoidAnimator>();
            npc._anim.AutoUpdate = false;
            npc._anim.Gait = HumanoidGait.Survivor;
            if (seated)
            {
                npc._anim.Seated = true;
                npc._anim.SeatPoint = new Vector3(position.x, role == Role.Musician ? 0.7f : seatHeight, position.z);
            }

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.8f;
            col.radius = 0.3f;

            Actor actor = go.AddComponent<Actor>();
            actor.Kind = ActorKind.Human;
            actor.Faction = faction ?? Factions.Freehold;
            actor.DisplayName = name;
            actor.Untargetable = true;

            var talk = go.AddComponent<HubNpcTalk>();
            talk.Npc = npc;
            return npc;
        }

        private void Dress(HumanoidLook look, string faction, Role role)
        {
            string[] tops = { "camojacket", "leatherjacket", "jacket", "hoodie", "parka" };
            string[] heads = { null, "greenbeanie", "cap", "beanie" };
            look.Wear(ClothingSlot.Torso, ItemDatabase.Get(role == Role.Keeper && faction == Factions.Institute ? "suit_halo" : tops[_rng.Next(tops.Length)]));
            look.Wear(ClothingSlot.Legs, ItemDatabase.Get(_rng.NextDouble() < 0.5 ? "cargopants" : "jeans"));
            look.Wear(ClothingSlot.Feet, ItemDatabase.Get("boots"));
            string head = heads[_rng.Next(heads.Length)];
            if (head != null) look.Wear(ClothingSlot.Head, ItemDatabase.Get(head));
            if (faction == Factions.Vanguard) look.Wear(ClothingSlot.Armor, ItemDatabase.Get("armor_vest_c3"));
        }

        private void Start()
        {
            _nextLine = Time.time + 4f + (float)_rng.NextDouble() * 10f;
            if (Kind == Role.Musician) _music = Sfx.Loop(SfxIds.Guitar, transform, 0.55f, 14f);
            if (_anim != null) _anim.Snap();
        }

        private void OnDestroy() { if (_music != null) Sfx.Stop(_music); }

        private void LateUpdate()
        {
            if (_anim == null) return;
            Actor me = Actor.LocalPlayer;
            float dist = me != null ? Vector3.Distance(me.transform.position, transform.position) : 99f;
            _anim.Velocity = Vector3.zero;
            _anim.HasLookTarget = true;
            _anim.LookTarget = dist < 4f && me != null ? me.EyePosition : LookAt + Vector3.up * 0.3f;
            _anim.Evaluate(Time.deltaTime);

            if (dist < 3.2f && Time.time >= _nextLine && Kind != Role.Storyteller && Lines.TryGetValue(Script, out string[] lines))
            {
                GameEvents.RaiseSubtitle(Name, lines[_lineIndex++ % lines.Length]);
                _nextLine = Time.time + 14f + (float)_rng.NextDouble() * 12f;
            }
        }

        /// <summary>Tells a whole story over a few seconds. Listening is remembered (quietly).</summary>
        public System.Collections.IEnumerator TellStory()
        {
            if (!Lines.TryGetValue(Script, out string[] lines)) yield break;
            for (int i = 0; i < lines.Length; i++)
            {
                GameEvents.RaiseSubtitle(Name, lines[i]);
                yield return new WaitForSeconds(4.5f);
            }
            AccountProfile a = ProfileStore.Account;
            CharacterProfile c = ProfileStore.ActiveCharacter;
            string flag = "heard:" + Script;
            if (c != null && !c.HasFlag(flag))
            {
                c.SetFlag(flag);
                MoralLedger.Record("listened to " + Name, MoralLedger.ListenedToStory);
                if (Script == "story_pell" && a.Learn("route:kestrel:north_road")) GameEvents.RaiseToast("Learned a route: Kestrel's north-road ditch skirts the sinkholes.");
                if (Script == "story_dana" && a.Learn("rumour:lodestar")) GameEvents.RaiseToast("You've heard of the Lodestar.");
                ProfileStore.Save();
            }
        }
    }

    /// <summary>The "talk to" side of a hub NPC (listen to a story, give to a beggar, request a song).</summary>
    public class HubNpcTalk : Interactable
    {
        public HubNpc Npc;
        private bool _telling;

        public override string DisplayName => Npc != null ? Npc.Name : "Someone";
        public override Vector3 InteractPoint => transform.position + Vector3.up * 1.2f;
        public override float Reach => 2.4f;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (Npc == null) return;
            switch (Npc.Kind)
            {
                case HubNpc.Role.Storyteller:
                    into.Add(new InteractAction(_telling ? "Listening..." : "Listen to the story", c =>
                    {
                        if (_telling) return;
                        _telling = true;
                        StartCoroutine(Tell());
                    }).DisabledIf(_telling, "Already telling it"));
                    break;
                case HubNpc.Role.Beggar:
                    // Cheaper donation option
                    into.Add(new InteractAction("Give 5 MG", c =>
                    {
                        if (!Money.TrySpend(5, c.Inventory)) { c.Notify("You don't have 5 MG."); return; }
                        c.Notify("\"The Zone remembers.\"");
                        MoralLedger.Record("gave to the beggar (5 MG)", MoralLedger.GaveToBeggar);
                        Sfx.PlayUI(SfxIds.Cash);
                        ProfileStore.Save();
                    }));
                    // Standard donation
                    into.Add(new InteractAction("Give 20 MG", c =>
                    {
                        if (!Money.TrySpend(20, c.Inventory)) { c.Notify("You don't have 20 MG."); return; }
                        c.Notify("\"The Zone remembers.\"");
                        MoralLedger.Record("gave to the beggar", MoralLedger.GaveToBeggar);
                        Sfx.PlayUI(SfxIds.Cash);
                        ProfileStore.Save();
                    }));
                    break;
                case HubNpc.Role.Musician:
                    into.Add(new InteractAction("Request a song (10 MG)", c =>
                    {
                        if (!Money.TrySpend(10, c.Inventory)) { c.Notify("You don't have 10 MG."); return; }
                        GameEvents.RaiseSubtitle(Npc.Name, "For you, then. Something slow.");
                        GameEvents.RaiseWorldEvent(transform.position, 10f, "music");
                    }));
                    // Added cheaper short tune option
                    into.Add(new InteractAction("Request a short tune (5 MG)", c =>
                    {
                        if (!Money.TrySpend(5, c.Inventory)) { c.Notify("You don't have 5 MG."); return; }
                        GameEvents.RaiseSubtitle(Npc.Name, "A quick melody for you.");
                        GameEvents.RaiseWorldEvent(transform.position, 5f, "music");
                        Sfx.PlayUI(SfxIds.Cash);
                    }));
                    break;
                        case HubNpc.Role.Guide:
                            // Ask for a route hint
                            into.Add(new InteractAction("Ask for a route hint (5 MG)", c =>
                            {
                                if (!Money.TrySpend(5, c.Inventory)) { c.Notify("You don't have 5 MG."); return; }
                                // pick a random hint
                                string hint = HubNpc.GuideHints[UnityEngine.Random.Range(0, HubNpc.GuideHints.Length)];
                                GameEvents.RaiseSubtitle(Npc.Name, hint);
                                MoralLedger.Record("asked guide for hint", MoralLedger.HintFromGuide);
                                Sfx.PlayUI(SfxIds.Cash);
                                ProfileStore.Save();
                            }));
                            break;
                default:
                    into.Add(new InteractAction("Chat", c => GameEvents.RaiseSubtitle(Npc.Name, "Mm.")));
                    break;
            }
        }

        private System.Collections.IEnumerator Tell()
        {
            yield return Npc.TellStory();
            _telling = false;
        }
    }
}
