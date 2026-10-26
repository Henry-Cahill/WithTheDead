using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The space the survivor stands in, heard. A reverb zone rides the ears and is retuned a few times a second from
    /// where they are: indoors by the size of the room (the shelter around them, cross-checked by a ring of line casts —
    /// a closet or a car is near-dry, a house room short and close, a hall or a barn long), outdoors by what surrounds
    /// them (open fields: a faint, far slap-back; forest: soft and diffuse; a street between buildings: hard early
    /// reflections). The space glides from one to the next, so stepping through a door is heard as a change of air.
    /// Each sound takes as much of it as its routing allows (<see cref="SfxMeta.Reverb"/>; flat sounds take none).
    /// </summary>
    [DefaultExecutionOrder(9000)]
    public sealed class SfxAcoustics : MonoBehaviour
    {
        // A reverb character in the units AudioReverbZone takes (millibels and seconds).
        private struct Space
        {
            public float Room, RoomHF, Decay, DecayHF, Reflections, ReflectionsDelay, Reverb, ReverbDelay, Diffusion, Density;

            public Space(float room, float roomHF, float decay, float decayHF, float reflections, float reflectionsDelay, float reverb, float reverbDelay, float diffusion = 100f, float density = 100f)
            {
                Room = room;
                RoomHF = roomHF;
                Decay = decay;
                DecayHF = decayHF;
                Reflections = reflections;
                ReflectionsDelay = reflectionsDelay;
                Reverb = reverb;
                ReverbDelay = reverbDelay;
                Diffusion = diffusion;
                Density = density;
            }

            public static Space Lerp(Space a, Space b, float t) => new Space(
                Mathf.Lerp(a.Room, b.Room, t), Mathf.Lerp(a.RoomHF, b.RoomHF, t), Mathf.Lerp(a.Decay, b.Decay, t), Mathf.Lerp(a.DecayHF, b.DecayHF, t),
                Mathf.Lerp(a.Reflections, b.Reflections, t), Mathf.Lerp(a.ReflectionsDelay, b.ReflectionsDelay, t), Mathf.Lerp(a.Reverb, b.Reverb, t),
                Mathf.Lerp(a.ReverbDelay, b.ReverbDelay, t), Mathf.Lerp(a.Diffusion, b.Diffusion, t), Mathf.Lerp(a.Density, b.Density, t));

            public static Space Mix(Space a, float wa, Space b, float wb, Space c, float wc)
            {
                float sum = Mathf.Max(1e-4f, wa + wb + wc);
                wa /= sum;
                wb /= sum;
                wc /= sum;
                return new Space(
                    a.Room * wa + b.Room * wb + c.Room * wc, a.RoomHF * wa + b.RoomHF * wb + c.RoomHF * wc, a.Decay * wa + b.Decay * wb + c.Decay * wc,
                    a.DecayHF * wa + b.DecayHF * wb + c.DecayHF * wc, a.Reflections * wa + b.Reflections * wb + c.Reflections * wc,
                    a.ReflectionsDelay * wa + b.ReflectionsDelay * wb + c.ReflectionsDelay * wc, a.Reverb * wa + b.Reverb * wb + c.Reverb * wc,
                    a.ReverbDelay * wa + b.ReverbDelay * wb + c.ReverbDelay * wc, a.Diffusion * wa + b.Diffusion * wb + c.Diffusion * wc,
                    a.Density * wa + b.Density * wb + c.Density * wc);
            }
        }

        // Tamed I3DL2-style characters: the game had no room sound at all, so these sit under the dry sound, not over it.
        private static readonly Space Silent = new Space(-10000f, -10000f, 1f, 1f, -10000f, 0.02f, -10000f, 0.04f);
        private static readonly Space Cab = new Space(-2600f, -1400f, 0.25f, 0.5f, -1900f, 0.001f, -2600f, 0.002f);
        private static readonly Space SmallRoom = new Space(-1100f, -650f, 0.45f, 0.75f, -1350f, 0.002f, -350f, 0.003f);
        private static readonly Space HouseRoom = new Space(-1000f, -550f, 0.8f, 0.7f, -1400f, 0.005f, -300f, 0.007f);
        private static readonly Space Hall = new Space(-950f, -750f, 1.8f, 0.55f, -1300f, 0.012f, -200f, 0.02f);
        private static readonly Space Barn = new Space(-950f, -950f, 2.8f, 0.45f, -1150f, 0.02f, -250f, 0.03f);
        private static readonly Space Street = new Space(-1350f, -950f, 1.4f, 0.6f, -1700f, 0.008f, -1350f, 0.012f, 50f, 100f);
        private static readonly Space Forest = new Space(-1550f, -2800f, 1.4f, 0.5f, -2600f, 0.15f, -950f, 0.08f, 79f, 100f);
        private static readonly Space Open = new Space(-1750f, -2300f, 1.4f, 0.3f, -2650f, 0.18f, -2050f, 0.1f, 30f, 100f);

        private const int Directions = 8;
        private const float ProbeRange = 40f, CycleSeconds = 0.25f;
        private const int WorldMask = 1 << 0;

        private static SfxAcoustics s_Instance;
        private static readonly List<Bounds> s_Shelters = new List<Bounds>(8);

        private AudioReverbZone _zone;
        private Space _current = Silent, _target = Silent;
        private readonly float[] _hit = new float[Directions + 1];
        private int _probe;
        private float _ringTurn, _nextApply;

        /// <summary>True while the ears are inside (a sheltered interior or an enclosed space with a roof).</summary>
        public static bool Indoors { get; private set; }

        /// <summary>How closed-in the surroundings are, 0 (open field) .. 1 (walls all round).</summary>
        public static float Enclosure { get; private set; }

        /// <summary>Ensures the acoustics run (called once by the audio module).</summary>
        public static void Ensure()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[Acoustics]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<SfxAcoustics>();
        }

        private void Awake()
        {
            _zone = gameObject.AddComponent<AudioReverbZone>();
            _zone.reverbPreset = AudioReverbPreset.User;
            _zone.minDistance = 6f;
            _zone.maxDistance = 12f;
            for (int i = 0; i < _hit.Length; i++) _hit[i] = ProbeRange;
            Apply(Silent);
        }

        private void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (!ListenerRig.Active)
            {
                Indoors = false;
                Enclosure = 0f;
                _target = Silent;
            }
            else
            {
                Vector3 ears = ListenerRig.Position;
                transform.position = ears;
                Probe(ears);
            }
            _current = Space.Lerp(_current, _target, 1f - Mathf.Exp(-dt / 0.35f));
            if (Time.unscaledTime >= _nextApply)
            {
                _nextApply = Time.unscaledTime + 0.1f;
                Apply(_current);
            }
        }

        // Two line casts a frame round a ring of eight directions plus one straight up; a full ring re-judges the space.
        private void Probe(Vector3 ears)
        {
            for (int k = 0; k < 2; k++)
            {
                Vector3 dir;
                if (_probe < Directions)
                {
                    float a = (_probe / (float)Directions + _ringTurn) * Mathf.PI * 2f;
                    dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                }
                else dir = Vector3.up;
                _hit[_probe] = Physics.Raycast(ears, dir, out RaycastHit hit, ProbeRange, WorldMask, QueryTriggerInteraction.Ignore) ? hit.distance : ProbeRange;
                if (++_probe > Directions)
                {
                    _probe = 0;
                    _ringTurn = Random.value / Directions;
                    Judge(ears);
                }
            }
        }

        private void Judge(Vector3 ears)
        {
            int near = 0, walls = 0;
            float wallSum = 0f;
            for (int i = 0; i < Directions; i++)
            {
                if (_hit[i] < 20f) near++;
                if (_hit[i] < ProbeRange)
                {
                    walls++;
                    wallSum += _hit[i];
                }
            }
            float enclosure = near / (float)Directions;
            bool roof = _hit[Directions] < 8f;
            Enclosure = enclosure;

            float shelterSize = ShelterSize(ears);
            bool inside = shelterSize > 0f || (roof && enclosure >= 0.6f);
            Indoors = inside;

            if (inside)
            {
                float across = walls > 0 ? 2f * wallSum / walls : 10f;
                float size = shelterSize > 0f ? Mathf.Min(shelterSize, Mathf.Max(across, 2f)) : across;
                if (walls >= 6 && wallSum / walls < 1.3f) _target = Cab;
                else if (size < 4f) _target = SmallRoom;
                else if (size < 8f) _target = Space.Lerp(SmallRoom, HouseRoom, (size - 4f) / 4f);
                else if (size < 16f) _target = Space.Lerp(HouseRoom, Hall, (size - 8f) / 8f);
                else _target = Space.Lerp(Hall, Barn, Mathf.Clamp01((size - 16f) / 16f));
                return;
            }

            float street = Mathf.Clamp01((enclosure - 0.25f) / 0.5f);
            float forest = Mathf.Clamp01(ZoneForest.Density(ears) * 1.3f) * (1f - street);
            float open = Mathf.Max(0f, 1f - street - forest);
            _target = Space.Mix(Street, street, Forest, forest, Open, open);
        }

        // The size across (cube root of the volume) of the smallest sheltered interior holding the ears, or 0.
        private static float ShelterSize(Vector3 ears)
        {
            if (Shelter.Count == 0 || !Shelter.IsSheltered(ears)) return 0f;
            s_Shelters.Clear();
            Shelter.Overlapping(new Bounds(ears, Vector3.one * 0.1f), s_Shelters);
            float best = float.MaxValue;
            for (int i = 0; i < s_Shelters.Count; i++)
            {
                Bounds b = s_Shelters[i];
                if (!b.Contains(ears)) continue;
                float v = b.size.x * b.size.y * b.size.z;
                if (v < best) best = v;
            }
            return best < float.MaxValue ? Mathf.Pow(Mathf.Max(1f, best), 1f / 3f) : 6f;
        }

        private void Apply(Space s)
        {
            _zone.room = Mathf.RoundToInt(s.Room);
            _zone.roomHF = Mathf.RoundToInt(s.RoomHF);
            _zone.roomLF = 0;
            _zone.decayTime = s.Decay;
            _zone.decayHFRatio = s.DecayHF;
            _zone.reflections = Mathf.RoundToInt(s.Reflections);
            _zone.reflectionsDelay = s.ReflectionsDelay;
            _zone.reverb = Mathf.RoundToInt(s.Reverb);
            _zone.reverbDelay = s.ReverbDelay;
            _zone.diffusion = s.Diffusion;
            _zone.density = s.Density;
        }
    }
}
