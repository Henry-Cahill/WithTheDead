using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// A car (DayZ's vehicle assembly, Zomboid's hotwiring): they're found broken at the Zone's parking spots —
    /// wheels gone, no battery, no spark plug, a dry tank. Find the parts, fit them, pour in fuel, hotwire it (or
    /// carry a key), and drive. Loud, fast, and it runs the dead down — and overheats without a radiator. The car is
    /// remembered as you left it, per Zone, so the next raid it's still where you parked it.
    /// </summary>
    public sealed class Vehicle : Interactable
    {
        public static new readonly List<Vehicle> All = new List<Vehicle>();
        public const float TankLitres = 40f, MaxSpeed = 19f, ReverseSpeed = 5f;

        /// <summary>Spot index of the car in the world.</summary>
        public int Spot { get; private set; }
        public int Wheels;
        public bool Battery, SparkPlug, Radiator, Hotwired;
        public float Fuel, Engine = 1f, Charge = 1f, Heat, Tyres = 1f;
        /// <summary>Whether the engine is currently running.</summary>
        public bool EngineOn { get; private set; }
        /// <summary>Current speed of the car (m/s).</summary>
        public float Speed { get; private set; }
        /// <summary>Driver's player stats (null when no one is driving).</summary>
        public PlayerStats Driver { get; private set; }
        public ulong RemoteDriver = ulong.MaxValue;
        /// <summary>Transform of the driver's seat.</summary>
public Transform Seat { get; private set; }

        private Rigidbody _rb;
        private Transform _bodyRoot;
        private readonly Transform[] _wheels = new Transform[4];
        private Light[] _lights;
        private AudioSource _engine;
        private float _steer, _noiseAt, _fuelWarnAt, _wheelSpin;
        private readonly Dictionary<Actor, float> _hitAt = new Dictionary<Actor, float>();
        private Vector3 _netFrom, _netTo;
        private float _netYawFrom, _netYawTo, _netT = 1f;

        /// <summary>Whether the car can be driven (has all wheels and a functional engine).</summary>
public bool Drivable => Wheels == 4 && Engine > 0f;
        public string Missing
        {
            get
            {
                var parts = new List<string>();
                if (Wheels < 4) parts.Add((4 - Wheels) + (4 - Wheels == 1 ? " wheel" : " wheels"));
                if (!Battery) parts.Add("battery");
                if (!SparkPlug) parts.Add("spark plug");
                if (!Radiator) parts.Add("radiator");
                if (Fuel < 0.5f) parts.Add("fuel");
                return parts.Count == 0 ? "" : "needs " + string.Join(", ", parts);
            }
        }

        public override string DisplayName => "Car" + (Driver != null || RemoteDriver != ulong.MaxValue ? " (occupied)" : Missing.Length > 0 ? " — " + Missing : Engine <= 0f ? " — engine seized" : "");
        public override Vector3 InteractPoint => transform.position + transform.right * -1f + Vector3.up * 1f;
        public override float Reach => 2.6f;
        public override bool IsAvailable => isActiveAndEnabled && Driver == null && RemoteDriver == ulong.MaxValue;

        protected override void OnEnable() { base.OnEnable(); if (!All.Contains(this)) All.Add(this); }
        protected override void OnDisable() { base.OnDisable(); All.Remove(this); }

        #region Building
        /// <summary>Creates a new car at the given spot, position, yaw and random seed.</summary>
public static Vehicle Spawn(int spot, Vector3 at, float yaw, int seed)
        {
            var go = new GameObject("Car " + spot);
            go.transform.SetPositionAndRotation(new Vector3(at.x, 0f, at.z), Quaternion.Euler(0f, yaw, 0f));
            var v = go.AddComponent<Vehicle>();
            v.Spot = spot;
            v.Build(new System.Random(seed));
            return v;
        }

        private void Build(System.Random rng)
        {
            Color[] paints = { new Color(0.45f, 0.12f, 0.1f), new Color(0.2f, 0.28f, 0.22f), new Color(0.55f, 0.52f, 0.45f), new Color(0.18f, 0.22f, 0.32f), new Color(0.5f, 0.38f, 0.18f) };
            Color paint = paints[rng.Next(paints.Length)];
            _bodyRoot = new GameObject("Body").transform;
            _bodyRoot.SetParent(transform, false);
            Box(_bodyRoot, new Vector3(0f, 0.62f, 0f), new Vector3(1.8f, 0.55f, 4.2f), paint);
            Box(_bodyRoot, new Vector3(0f, 1.15f, -0.25f), new Vector3(1.62f, 0.55f, 2.1f), paint * 0.95f);
            Color glass = new Color(0.08f, 0.1f, 0.12f);
            Box(_bodyRoot, new Vector3(0f, 1.15f, 0.83f), new Vector3(1.5f, 0.45f, 0.06f), glass);
            Box(_bodyRoot, new Vector3(0f, 1.15f, -1.32f), new Vector3(1.5f, 0.42f, 0.06f), glass);
            Box(_bodyRoot, new Vector3(0.82f, 1.15f, -0.25f), new Vector3(0.04f, 0.42f, 1.9f), glass);
            Box(_bodyRoot, new Vector3(-0.82f, 1.15f, -0.25f), new Vector3(0.04f, 0.42f, 1.9f), glass);
            Box(_bodyRoot, new Vector3(0f, 0.52f, 2.12f), new Vector3(1.7f, 0.22f, 0.08f), new Color(0.15f, 0.15f, 0.15f));
            Box(_bodyRoot, new Vector3(0f, 0.52f, -2.12f), new Vector3(1.7f, 0.22f, 0.08f), new Color(0.15f, 0.15f, 0.15f));
            float[] xs = { -0.86f, 0.86f, -0.86f, 0.86f };
            float[] zs = { 1.35f, 1.35f, -1.35f, -1.35f };
            for (int i = 0; i < 4; i++)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(w.GetComponent<Collider>());
                w.name = "Wheel";
                w.transform.SetParent(_bodyRoot, false);
                w.transform.localPosition = new Vector3(xs[i], 0.36f, zs[i]);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                w.transform.localScale = new Vector3(0.72f, 0.13f, 0.72f);
                w.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(new Color(0.07f, 0.07f, 0.07f));
                _wheels[i] = w.transform;
            }
            _lights = new Light[2];
            for (int i = 0; i < 2; i++)
            {
                var l = new GameObject("Headlight").AddComponent<Light>();
                l.transform.SetParent(_bodyRoot, false);
                l.transform.localPosition = new Vector3(i == 0 ? -0.6f : 0.6f, 0.7f, 2.15f);
                l.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
                l.type = LightType.Spot;
                l.spotAngle = 60f;
                l.range = 28f;
                l.intensity = 0f;
                l.color = new Color(1f, 0.95f, 0.8f);
                _lights[i] = l;
            }
            Seat = new GameObject("Driver seat").transform;
            Seat.SetParent(transform, false);
            Seat.localPosition = new Vector3(-0.4f, 0.75f, -0.2f);

            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.85f, 0f);
            col.size = new Vector3(1.8f, 1.1f, 4.2f);
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            var obstacle = gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            obstacle.center = col.center;
            obstacle.size = col.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            RefreshParts();
        }

        private static void Box(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(b.GetComponent<Collider>());
            b.transform.SetParent(parent, false);
            b.transform.localPosition = pos;
            b.transform.localScale = size;
            b.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(c, 0.35f);
        }

        /// <summary>Shows the wheels that are on; a car short of wheels sits on its belly.</summary>
        /// <summary>Refreshes wheel visibility and body position based on current parts.</summary>
public void RefreshParts()
        {
            for (int i = 0; i < 4; i++) if (_wheels[i] != null) _wheels[i].gameObject.SetActive(i < Wheels);
            if (_bodyRoot != null) _bodyRoot.localPosition = Wheels < 4 ? new Vector3(0f, -0.22f, 0f) : Vector3.zero;
        }

        /// <summary>Capture the car's current state for saving.</summary>
public VehicleSave Capture() => new VehicleSave
        {
            Spot = Spot, Position = transform.position, Yaw = transform.eulerAngles.y, Wheels = Wheels, Battery = Battery, SparkPlug = SparkPlug,
            Radiator = Radiator, Hotwired = Hotwired, Fuel = Fuel, Engine = Engine, Charge = Charge, Tyres = Tyres,
        };

        /// <summary>Load a saved car state, applying parts and fuel.</summary>
public void Load(VehicleSave s)
        {
            Wheels = Mathf.Clamp(s.Wheels, 0, 4);
            Battery = s.Battery; SparkPlug = s.SparkPlug; Radiator = s.Radiator; Hotwired = s.Hotwired;
            Fuel = Mathf.Clamp(s.Fuel, 0f, TankLitres); Engine = Mathf.Clamp01(s.Engine); Charge = Mathf.Clamp01(s.Charge);
            Tyres = s.Tyres > 0f ? Mathf.Clamp01(s.Tyres) : 1f;
            RefreshParts();
        }
        #endregion

        #region Working on it
        /// <summary>Provide interactable actions for the car (enter, fix, fuel, hotwire, etc.).</summary>
public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            PlayerInventory inv = ctx.Inventory;
            if (inv == null || GameSession.IsMonsterRun) return;
            into.Add(new InteractAction("Get in the driver's seat", c => Enter(c.Stats)).DisabledIf(Driver != null || RemoteDriver != ulong.MaxValue, "Someone's driving"));
            float work = ctx.Skills != null ? 1f / ctx.Skills.MechanicsMult : 1f;
            Part(into, inv, "carwheel", Wheels < 4, "Fit a wheel", 8f * work, () => { Wheels++; });
            Part(into, inv, "carbattery", !Battery, "Fit the battery", 5f * work, () => { Battery = true; });
            Part(into, inv, "sparkplug", !SparkPlug, "Fit a spark plug", 4f * work, () => { SparkPlug = true; });
            Part(into, inv, "radiator", !Radiator, "Fit the radiator", 8f * work, () => { Radiator = true; });

            ItemStack can = GasCan(inv, true);
            if (Fuel < TankLitres - 0.5f)
                into.Add(new InteractAction("Pour in fuel (" + Mathf.RoundToInt(Fuel) + "/" + TankLitres + " L)", c =>
                {
                    ItemStack s = GasCan(c.Inventory, true);
                    if (s == null) return;
                    float pour = Mathf.Min(s.Condition, TankLitres - Fuel);
                    Fuel += pour;
                    s.Condition -= Mathf.CeilToInt(pour);
                    c.Inventory.Inventory.RaiseChanged();
                    Sfx.Play(SfxIds.Drink, transform.position, 0.6f, 0.6f, 10f);
                    Changed();
                }).Timed(5f, "Pouring fuel...").DisabledIf(can == null, "Needs a gas can with fuel in it"));
            ItemStack empty = GasCan(inv, false);
            if (Fuel > 1f && empty != null)
                into.Add(new InteractAction("Siphon fuel into a can", c =>
                {
                    ItemStack s = GasCan(c.Inventory, false);
                    if (s == null) return;
                    float take = Mathf.Min(Fuel, s.Def.MaxCondition - s.Condition);
                    Fuel -= take;
                    s.Condition += Mathf.FloorToInt(take);
                    Changed();
                }).Timed(8f, "Siphoning..."));

            if (!Hotwired && !inv.Has("carkey"))
            {
                int mech = ctx.Skills != null ? ctx.Skills.GetLevel(PlayerSkills.Skill.Mechanics) : 0;
                into.Add(new InteractAction("Hotwire it (" + Mathf.RoundToInt(HotwireChance(mech) * 100f) + "%)", c =>
                {
                    int lvl = c.Skills != null ? c.Skills.GetLevel(PlayerSkills.Skill.Mechanics) : 0;
                    if (c.Skills != null) c.Skills.AddXp(PlayerSkills.Skill.Mechanics, 6f);
                    if (Random.value < HotwireChance(lvl)) { Hotwired = true; Changed(); c.Notify("The dash lights flicker. It'll start."); }
                    else { c.Notify("Sparks, a burnt finger, nothing."); Sfx.Play(SfxIds.AnomalyElectric, transform.position, 0.4f, 1.6f, 8f); }
                }).Timed(12f, "Hotwiring...").DisabledIf(!Battery, "No battery to work with"));
            }

            Remove(into, "carwheel", Wheels > 0, "Take a wheel off", 6f * work, () => { Wheels--; });
            Remove(into, "carbattery", Battery, "Take the battery out", 4f * work, () => { Battery = false; });
            Remove(into, "sparkplug", SparkPlug, "Take the spark plug out", 3f * work, () => { SparkPlug = false; });
            Remove(into, "radiator", Radiator, "Take the radiator out", 6f * work, () => { Radiator = false; });
        }

        private static float HotwireChance(int mechanics) => Mathf.Clamp01(0.3f + mechanics * 0.12f);

        private void Part(List<InteractAction> into, PlayerInventory inv, string id, bool needed, string label, float seconds, System.Action fit)
        {
            if (!needed) return;
            ItemDef def = ItemDatabase.Get(id);
            into.Add(new InteractAction(label, c =>
            {
                if (c.Inventory.Consume(id, 1) <= 0) return;
                fit();
                if (c.Skills != null) c.Skills.AddXp(PlayerSkills.Skill.Mechanics, 5f);
                Sfx.Play(SfxIds.Crank, transform.position, 0.6f, 1.1f, 12f);
                RefreshParts();
                Changed();
            }).Timed(seconds, label + "...", HumanoidActivity.Build).DisabledIf(!inv.Has(id), "You need a " + (def != null ? def.DisplayName.ToLowerInvariant() : id)));
        }

        private void Remove(List<InteractAction> into, string id, bool present, string label, float seconds, System.Action take)
        {
            if (!present) return;
            into.Add(new InteractAction(label, c =>
            {
                ItemDef def = ItemDatabase.Get(id);
                if (def == null) return;
                take();
                c.Inventory.Inventory.Add(def, 1, enforceWeight: false);
                if (c.Skills != null) c.Skills.AddXp(PlayerSkills.Skill.Mechanics, 2f);
                RefreshParts();
                Changed();
            }).Timed(seconds, label + "...", HumanoidActivity.Build));
        }

        private static ItemStack GasCan(PlayerInventory inv, bool withFuel)
        {
            if (inv == null) return null;
            foreach (ItemStack s in inv.Inventory.Stacks)
                if (s != null && s.Def != null && s.Def.Id == "gascan" && (withFuel ? s.Condition > 0 : s.Condition < s.Def.MaxCondition)) return s;
            return null;
        }

        private void Changed() => VehicleNet.SendState(this);
        #endregion

        #region Driving
        /// <summary>Tell the car a player has entered as driver.</summary>
public void Enter(PlayerStats who)
        {
            if (who == null || Driver != null || RemoteDriver != ulong.MaxValue) return;
            Driver = who;
            var d = who.gameObject.AddComponent<VehicleDriver>();
            d.Begin(this);
            _rb.isKinematic = true;
            VehicleNet.Claim(this, true);
        }

        /// <summary>Tell the car the driver has exited.</summary>
public void Exit()
        {
            if (Driver == null) return;
            EngineOn = false;
            UpdateEngineFx();
            Speed = 0f;
            Driver = null;
            VehicleNet.Claim(this, false);
            Changed();
        }

        /// <summary>The driver turns the key (or touches the wires together).</summary>
        /// <summary>Attempt to start the engine; returns true if successful and sets why if not.</summary>
public bool TryStart(out string why)
        {
            why = null;
            if (EngineOn) return true;
            if (Engine <= 0f) why = "The engine is seized.";
            else if (!Battery || Charge < 0.08f) why = Battery ? "The battery is flat." : "There's no battery.";
            else if (!SparkPlug) why = "It turns over but won't catch — no spark plug.";
            else if (Fuel <= 0.05f) why = "It turns over, coughs, nothing. No fuel.";
            else if (!Hotwired && (Driver == null || Driver.GetComponent<PlayerInventory>() == null || !Driver.GetComponent<PlayerInventory>().Has("carkey"))) why = "No key — it needs hotwiring.";
            if (why != null)
            {
                Sfx.Play(SfxIds.EngineStartFail, transform.position, 0.9f, 1f, 30f);
                if (Battery) Charge = Mathf.Max(0f, Charge - 0.04f);
                return false;
            }
            EngineOn = true;
            UpdateEngineFx();
            NoiseSystem.Emit(transform.position, 45f);
            return true;
        }

        private void UpdateEngineFx()
        {
            if (_lights != null) foreach (Light l in _lights) if (l != null) l.intensity = EngineOn ? 2.2f : 0f;
            if (EngineOn && _engine == null) _engine = Sfx.Loop(SfxIds.EngineLoop, transform, 0.9f, 70f);
            else if (!EngineOn && _engine != null) { Sfx.Stop(_engine); _engine = null; }
        }

        private void OnDestroy() { if (_engine != null) Sfx.Stop(_engine); }

        /// <summary>One physics step of driving: throttle -1..1, steer -1..1, brake.</summary>
        /// <summary>Drive the car for one physics step based on throttle, steer, brake and delta time.</summary>
public void Drive(float throttle, float steer, bool brake, float dt)
        {
            if (!EngineOn || !Drivable) throttle = 0f;
            float target = throttle > 0f ? throttle * MaxSpeed * Mathf.Lerp(0.55f, 1f, Engine) : throttle * ReverseSpeed;
            float accel = brake ? 14f : Mathf.Abs(target) > Mathf.Abs(Speed) ? 5f : 3f;
            if (brake) target = 0f;
            Speed = Mathf.MoveTowards(Speed, target, accel * dt);
            if (!Drivable) Speed = 0f;
            _steer = Mathf.MoveTowards(_steer, steer, 3f * dt);
            float turn = _steer * Mathf.Clamp(Mathf.Abs(Speed) / 4f, 0f, 1f) * 55f * Mathf.Sign(Speed == 0f ? 1f : Speed) * dt;
            Quaternion rot = Quaternion.Euler(0f, transform.eulerAngles.y + turn, 0f);
            Vector3 step = rot * Vector3.forward * Speed * dt;

            // Walls and wrecks stop a car (and hurt it); the dead and the living go under it.
            if (step.sqrMagnitude > 1e-6f && _rb.SweepTest(step.normalized, out RaycastHit hit, step.magnitude + 0.05f, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<Actor>() == null)
            {
                float impact = Mathf.Abs(Speed);
                if (impact > 7f)
                {
                    Engine = Mathf.Max(0f, Engine - impact * 0.012f);
                    Tyres = Mathf.Max(0f, Tyres - impact * 0.01f);
                    Sfx.Play(SfxIds.DoorBash, transform.position, 1f, 0.6f, 40f);
                    NoiseSystem.Emit(transform.position, 40f);
                    if (Driver != null) Driver.LimbDamage(impact * 1.5f, "a car crash");
                    CinemachineViews.Shake(transform.position, impact * 0.3f);
                }
                Speed = 0f;
                step = Vector3.zero;
            }
            Vector3 next = transform.position + step;
            next.y = GroundAt(next);
            _rb.MovePosition(next);
            _rb.MoveRotation(rot);
            RunOver();

            // Tyres wear with distance; a worn one bursts (a wheel to replace).
            Tyres = Mathf.Max(0f, Tyres - step.magnitude * 0.00003f);
            if (Tyres <= 0f && Wheels > 0)
            {
                Wheels--;
                Tyres = 0.7f;
                RefreshParts();
                Sfx.Play(SfxIds.Ricochet, transform.position, 1f, 0.5f, 40f);
                GameEvents.RaiseToast("A tyre bursts. You'll need another wheel.");
                Changed();
            }

            // The engine: fuel, heat, charge.
            if (EngineOn)
            {
                Fuel = Mathf.Max(0f, Fuel - (0.012f + 0.07f * Mathf.Abs(throttle)) * dt);
                Charge = Mathf.Min(1f, Charge + 0.01f * dt);
                Heat = Radiator ? Mathf.Max(0f, Heat - 0.05f * dt) : Heat + dt / 75f;
                if (Heat >= 1f) { Engine = Mathf.Max(0f, Engine - 0.35f); Heat = 0.5f; Stall("Steam pours from the bonnet. The engine dies."); }
                if (Fuel <= 0f) Stall("The engine coughs and dies. Out of fuel.");
                if (Time.time >= _noiseAt) { _noiseAt = Time.time + 0.5f; NoiseSystem.Emit(transform.position, 30f + Mathf.Abs(Speed) * 2f); }
                if (Fuel < 3f && Time.time >= _fuelWarnAt) { _fuelWarnAt = Time.time + 30f; GameEvents.RaiseToast("Fuel light's on."); }
            }
            if (_engine != null) {
                float newPitch = 0.7f + Mathf.Abs(Speed) / MaxSpeed * 0.9f;
                if (Mathf.Abs(newPitch - _engine.pitch) > 0.001f) _engine.pitch = newPitch;
            }
            SpinWheels(dt);
        }

        private void Stall(string message)
        {
            EngineOn = false;
            UpdateEngineFx();
            GameEvents.RaiseToast(message);
            Changed();
        }

        private void SpinWheels(float dt)
        {
            _wheelSpin += Speed * dt / 0.36f * Mathf.Rad2Deg;
            for (int i = 0; i < 4; i++)
                if (_wheels[i] != null)
                    _wheels[i].localRotation = Quaternion.Euler(0f, i < 2 ? _steer * 25f : 0f, 90f) * Quaternion.Euler(0f, _wheelSpin, 0f);
        }

        // The surface under a point, never the car's own body (whose roof the ray starts above) nor anyone under the wheels.
        private float GroundAt(Vector3 p) => GroundProbe.Below(p + Vector3.up * 2f, 4f, transform, out float y) ? Mathf.Max(0f, y) : 0f;

        // Anything alive in front of the bumper at speed is hit.
        private void RunOver()
        {
            float s = Mathf.Abs(Speed);
            if (s < 3f) return;
            Vector3 front = transform.position + transform.forward * Mathf.Sign(Speed) * 2.2f;
            Actor me = Driver != null ? Driver.GetComponent<Actor>() : null;
            for (int i = Actor.All.Count - 1; i >= 0; i--)
            {
                Actor a = Actor.All[i];
                if (a == null || a == me || !a.IsAlive) continue;
                Vector3 to = a.transform.position - front;
                to.y = 0f;
                if (to.magnitude > 1.6f) continue;
                if (_hitAt.TryGetValue(a, out float t) && Time.time - t < 0.8f) continue;
                _hitAt[a] = Time.time;
                var info = new DamageInfo(s * 14f, DamageKind.Melee, a.Center, transform.forward * Mathf.Sign(Speed), Driver != null ? Driver.gameObject : gameObject)
                { HpScale = true, Knockback = s * 0.4f, FractureChance = 0.4f, WeaponId = "car" }.WithZone(s > 10f ? BodyZone.Thorax : BodyZone.LeftLeg);
                CombatResolver.Apply(a.Damageable, info, me);
                Speed *= 0.85f;
                Sfx.Play(SfxIds.HitFlesh, a.Center, 1f, 0.7f, 20f);
            }
        }

        // Someone else is driving: follow their updates smoothly.
        /// <summary>Send the car's position, rotation and speed to remote clients.</summary>
public void PushRemote(Vector3 pos, float yaw, float speed, bool engine)
        {
            _netFrom = transform.position;
            _netTo = pos;
            _netYawFrom = transform.eulerAngles.y;
            _netYawTo = yaw;
            _netT = 0f;
            Speed = speed;
            if (engine != EngineOn) { EngineOn = engine; UpdateEngineFx(); }
        }

        private void Update()
        {
            if (Driver != null || RemoteDriver == ulong.MaxValue || _netT >= 1f) return;
            _netT = Mathf.Min(1f, _netT + Time.deltaTime / 0.08f);
            transform.position = Vector3.Lerp(_netFrom, _netTo, _netT);
            transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(_netYawFrom, _netYawTo, _netT), 0f);
            SpinWheels(Time.deltaTime);
            if (_engine != null) {
                float newPitch = 0.7f + Mathf.Abs(Speed) / MaxSpeed * 0.9f;
                if (Mathf.Abs(newPitch - _engine.pitch) > 0.001f) _engine.pitch = newPitch;
            }
        }

        /// <summary>Update the remote driver's seat status, handling disconnection and engine state.</summary>
public void RemoteSeat(ulong who)
        {
            RemoteDriver = who;
            if (who == ulong.MaxValue && EngineOn) { EngineOn = false; UpdateEngineFx(); }
        }
        #endregion
    }

    /// <summary>
    /// The survivor behind the wheel: the body sits in the car (no walking, no shooting); W/S drive and reverse,
    /// A/D steer, Space brakes, W turns the engine over when it's off; E gets out.
    /// </summary>
    public sealed class VehicleDriver : MonoBehaviour
    {
        public static VehicleDriver Local { get; private set; }
        public Vehicle Car { get; private set; }

        private PlayerController _ctrl;
        private CharacterController _cc;
        private PlayerCombat _combat;
        private PlayerInteractor _interactor;
        private PlayerAvatar _avatar;
        private float _startAt, _enteredAt;

        /// <summary>Begin control of this vehicle for the given player (sets driver state and disables input).</summary>
public void Begin(Vehicle car)
        {
            Car = car;
            Local = this;
            _enteredAt = Time.time;
            _ctrl = GetComponent<PlayerController>();
            _cc = GetComponent<CharacterController>();
            _combat = GetComponent<PlayerCombat>();
            _interactor = GetComponent<PlayerInteractor>();
            _avatar = GetComponent<PlayerAvatar>();
            if (_ctrl != null) _ctrl.enabled = false;
            if (_cc != null) _cc.enabled = false;
            if (_combat != null) _combat.enabled = false;
            if (_interactor != null) _interactor.enabled = false;
            if (_avatar != null) _avatar.ExternalSeat = car.Seat;
            GameEvents.RaiseToast("W to start and drive, A/D steer, Space brakes, E to get out.");
            if (UnityEngine.Object.FindAnyObjectByType<HudVehicle>() == null) HudVehicle.Create();
        }

        /// <summary>FixedUpdate drives the car each physics tick and handles hotwiring input.</summary>
private void FixedUpdate()
        {
            if (Car == null) { Leave(); return; }
            Keyboard kb = Keyboard.current;
            bool blocked = GameInput.Blocked || kb == null;
            float throttle = blocked ? 0f : (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            float steer = blocked ? 0f : (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            bool brake = !blocked && kb.spaceKey.isPressed;
            if (throttle > 0f && !Car.EngineOn && Time.time >= _startAt)
            {
                _startAt = Time.time + 1.5f;
                if (!Car.TryStart(out string why)) GameEvents.RaiseToast(why);
            }
            Car.Drive(throttle, steer, brake, Time.fixedDeltaTime);
            VehicleNet.SendDrive(Car);
        }

        private void LateUpdate()
        {
            if (Car == null) return;
            transform.SetPositionAndRotation(Car.Seat.position - Vector3.up * 0.45f, Car.transform.rotation);
            Keyboard kb = Keyboard.current;
            PlayerStats st = GetComponent<PlayerStats>();
            bool exitKey = kb != null && kb.eKey.wasPressedThisFrame && !GameInput.Blocked && Time.time - _enteredAt > 0.4f;
            if (exitKey || (st != null && (st.IsDead || st.Incapacitated))) Leave();
        }

        /// <summary>Leave the vehicle, resetting driver state and repositioning the character.</summary>
public void Leave()
        {
            Vehicle car = Car;
            Car = null;
            if (car != null)
            {
                car.Exit();
                Vector3 side = car.transform.position - car.transform.right * 1.6f;
                side.y = 0.05f;
                transform.SetPositionAndRotation(side, Quaternion.Euler(0f, car.transform.eulerAngles.y, 0f));
            }
            if (_avatar != null) _avatar.ExternalSeat = null;
            if (_cc != null) _cc.enabled = true;
            if (_ctrl != null) _ctrl.enabled = true;
            if (_combat != null && !GameSession.IsMonsterRun) _combat.enabled = true;
            if (_interactor != null) _interactor.enabled = true;
            if (Local == this) Local = null;
            Destroy(this);
        }

        private void OnDestroy() { if (Local == this) Local = null; }
    }

    /// <summary>The dashboard: speed, fuel, heat, the engine.</summary>
    public sealed class HudVehicle : MonoBehaviour
    {
        private Text _text;

        /// <summary>Create the HUD element showing vehicle speed, fuel, heat, engine status, etc.</summary>
public static HudVehicle Create()
        {
            RectTransform slot = HudLayer.Anchor("Vehicle", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(900f, 40f));
            var h = slot.gameObject.AddComponent<HudVehicle>();
            h._text = UIKit.Label(slot, "", 17, new Color(0.9f, 0.88f, 0.75f), TextAnchor.MiddleCenter);
            UIKit.Stretch(h._text.rectTransform);
            return h;
        }

        private string _lastHudText;

        private void LateUpdate()
        {
            VehicleDriver d = VehicleDriver.Local;
            if (d == null || d.Car == null)
            {
                if (!string.IsNullOrEmpty(_text.text))
                {
                    _text.text = "";
                    _lastHudText = null;
                }
                return;
            }
            Vehicle c = d.Car;
            string newText = Mathf.RoundToInt(Mathf.Abs(c.Speed) * 3.6f) + " km/h   fuel " + c.Fuel.ToString("0.0") + " L   "
                + (c.EngineOn ? (c.Radiator ? "running" : UIKit.Colored("heat " + Mathf.RoundToInt(c.Heat * 100f) + "%", UIKit.Warn)) : UIKit.Colored("engine off", UIKit.TextDim))
                + "   engine " + Mathf.RoundToInt(c.Engine * 100f) + "%  tyres " + Mathf.RoundToInt(c.Tyres * 100f) + "%  battery " + Mathf.RoundToInt(c.Charge * 100f) + "%"
                + "   [E] get out";
            if (newText != _lastHudText)
            {
                _text.text = newText;
                _lastHudText = newText;
            }
        }
    }

    /// <summary>
    /// Cars on the wire: the driver sends where the car is (15 Hz) and the server passes it on; whoever sits in the
    /// driver's seat is settled by the server; parts and fuel changes go to everyone. The server's account keeps
    /// the cars between raids.
    /// </summary>
    public static class VehicleNet
    {
        private static float s_SendAt;

        internal static void Register()
        {
            NetSession.Register("cz.veh", OnDrive);
            NetSession.Register("cz.vehseat", OnSeat);
            NetSession.Register("cz.vehstate", OnState);
            NetSession.Register("cz.vehq", (s, m) =>
            {
                if (!NetSession.NM.IsServer) return;
                foreach (Vehicle v in Vehicle.All)
                    if (v != null) using (var o = new NetOut(96)) { Write(o, v); NetSession.SendTo(s, "cz.vehstate", o); }
            });
        }

        private static Vehicle Find(int spot)
        {
            foreach (Vehicle v in Vehicle.All) if (v != null && v.Spot == spot) return v;
            return null;
        }

        public static void SendDrive(Vehicle v)
        {
            if (!NetSession.Active || Time.time < s_SendAt) return;
            s_SendAt = Time.time + 1f / 15f;
            using (var o = new NetOut(48))
            {
                o.I(v.Spot).V(v.transform.position).F(v.transform.eulerAngles.y).F(v.Speed).B(v.EngineOn);
                if (NetSession.NM.IsServer) NetSession.ToClients("cz.veh", o, Unity.Netcode.NetworkDelivery.UnreliableSequenced);
                else NetSession.ToServer("cz.veh", o, Unity.Netcode.NetworkDelivery.UnreliableSequenced);
            }
        }

        private static void OnDrive(ulong sender, NetIn m)
        {
            int spot = m.I();
            Vector3 pos = m.V();
            float yaw = m.F(), speed = m.F();
            bool engine = m.B();
            if (NetSession.NM.IsServer && sender != NetSession.NM.LocalClientId)
                using (var o = new NetOut(48)) { o.I(spot).V(pos).F(yaw).F(speed).B(engine); NetSession.ToClients("cz.veh", o, Unity.Netcode.NetworkDelivery.UnreliableSequenced, sender); }
            Vehicle v = Find(spot);
            if (v != null && v.Driver == null) v.PushRemote(pos, yaw, speed, engine);
        }

        public static void Claim(Vehicle v, bool sitting)
        {
            if (!NetSession.Active) return;
            using (var o = new NetOut(24))
            {
                o.I(v.Spot).B(sitting).UL(NetSession.NM.LocalClientId);
                if (NetSession.NM.IsServer) NetSession.ToClients("cz.vehseat", o);
                else NetSession.ToServer("cz.vehseat", o);
            }
        }

        private static void OnSeat(ulong sender, NetIn m)
        {
            int spot = m.I();
            bool sitting = m.B();
            ulong who = m.UL();
            Vehicle v = Find(spot);
            if (NetSession.NM.IsServer && sender != NetSession.NM.LocalClientId)
            {
                who = sender;
                // Two runners reached for the door at once: the one already in keeps it.
                if (sitting && v != null && (v.Driver != null || (v.RemoteDriver != ulong.MaxValue && v.RemoteDriver != sender)))
                {
                    using (var no = new NetOut(24)) { no.I(spot).B(false).UL(NetSession.NM.LocalClientId); NetSession.SendTo(sender, "cz.vehseat", no); }
                    return;
                }
                using (var o = new NetOut(24)) { o.I(spot).B(sitting).UL(who); NetSession.ToClients("cz.vehseat", o, Unity.Netcode.NetworkDelivery.ReliableSequenced, sender); }
            }
            if (v == null) return;
            if (who == NetSession.NM.LocalClientId)
            {
                // The server refused our seat.
                if (!sitting && VehicleDriver.Local != null && VehicleDriver.Local.Car == v) { VehicleDriver.Local.Leave(); GameEvents.RaiseToast("Someone else got in first."); }
                return;
            }
            v.RemoteSeat(sitting ? who : ulong.MaxValue);
        }

        /// <summary>Send the car's state (parts, fuel, engine) to all clients.</summary>
public static void SendState(Vehicle v)
        {
            if (!NetSession.Active || v == null) return;
            using (var o = new NetOut(96))
            {
                Write(o, v);
                if (NetSession.NM.IsServer) NetSession.ToClients("cz.vehstate", o);
                else NetSession.ToServer("cz.vehstate", o);
            }
        }

        /// <summary>Write the vehicle's data into a network message for syncing.</summary>
private static void Write(NetOut o, Vehicle v)
            => o.I(v.Spot).V(v.transform.position).F(v.transform.eulerAngles.y).I(v.Wheels).B(v.Battery).B(v.SparkPlug).B(v.Radiator).B(v.Hotwired).F(v.Fuel).F(v.Engine).F(v.Charge).F(v.Tyres);

        private static void OnState(ulong sender, NetIn m)
        {
            int spot = m.I();
            var s = new VehicleSave { Spot = spot, Position = m.V(), Yaw = m.F(), Wheels = m.I(), Battery = m.B(), SparkPlug = m.B(), Radiator = m.B(), Hotwired = m.B(), Fuel = m.F(), Engine = m.F(), Charge = m.F(), Tyres = m.F() };
            if (NetSession.NM.IsServer && sender != NetSession.NM.LocalClientId)
                using (var o = new NetOut(96))
                {
                    o.I(spot).V(s.Position).F(s.Yaw).I(s.Wheels).B(s.Battery).B(s.SparkPlug).B(s.Radiator).B(s.Hotwired).F(s.Fuel).F(s.Engine).F(s.Charge).F(s.Tyres);
                    NetSession.ToClients("cz.vehstate", o, Unity.Netcode.NetworkDelivery.ReliableSequenced, sender);
                }
            Vehicle v = Find(spot);
            if (v == null || v.Driver != null) return;
            v.Load(s);
            if (v.RemoteDriver == ulong.MaxValue) v.transform.SetPositionAndRotation(s.Position, Quaternion.Euler(0f, s.Yaw, 0f));
        }

        internal static void AskForCars()
        {
            if (NetSession.Active && !NetSession.NM.IsServer) using (var o = new NetOut(8)) NetSession.ToServer("cz.vehq", o);
        }
    }

    /// <summary>The ground under things that move themselves over it (cars, ground drones): the highest surface below,
    /// skipping the mover's own body and anyone underfoot.</summary>
    internal static class GroundProbe
    {
        private static readonly RaycastHit[] s_Hits = new RaycastHit[8];

        /// <summary>Check if a point is above ground, ignoring the car's own collider and any actors below.</summary>
public static bool Below(Vector3 from, float distance, Transform self, out float y)
        {
            int n = Physics.RaycastNonAlloc(from, Vector3.down, s_Hits, distance, Actor.SightMask, QueryTriggerInteraction.Ignore);
            y = float.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                Collider c = s_Hits[i].collider;
                if (c == null || (self != null && c.transform.IsChildOf(self)) || c.GetComponentInParent<Actor>() != null) continue;
                y = Mathf.Max(y, s_Hits[i].point.y);
            }
            return y > float.NegativeInfinity;
        }
    }

    /// <summary>Parks a broken car on each of the Zone's vehicle spots (or puts back the ones you left), and remembers them.</summary>
    public sealed class VehicleModule : IGameModule
    {
        public int Order => 95;

        public void Init()
        {
            WorldHooks.RaidStarted += (world, map, zone) =>
            {
                zone = zone ?? ZoneCatalog.Current;
                IReadOnlyList<Vector3> spots = WorldMarkers.Get(WorldMarkers.VehicleSpot);
                AccountProfile acct = ProfileStore.Account;
                ZoneWorldState z = acct != null ? acct.Zone(zone.Id) : null;
                for (int i = 0; i < spots.Count; i++)
                {
                    var rng = new System.Random(GameSession.RaidSeed ^ (i * 7919 + 17));
                    VehicleSave saved = null;
                    if (z != null && NetAuthority.IsServer) saved = z.Vehicles.Find(s => s.Spot == i);
                    Vector3 at = saved != null ? saved.Position : spots[i];
                    float yaw = saved != null ? saved.Yaw : (float)rng.NextDouble() * 360f;
                    Vehicle v = Vehicle.Spawn(i, at, yaw, zone.LayoutSeed ^ i);
                    if (saved != null) v.Load(saved);
                    else
                    {
                        // Found as the Zone left it: picked over, dry, dead.
                        v.Load(new VehicleSave
                        {
                            Spot = i, Wheels = rng.Next(0, 5), Battery = rng.NextDouble() < 0.3, SparkPlug = rng.NextDouble() < 0.4, Radiator = rng.NextDouble() < 0.5,
                            Fuel = (float)rng.NextDouble() * 6f, Engine = 0.6f + (float)rng.NextDouble() * 0.4f, Charge = 0.2f + (float)rng.NextDouble() * 0.6f,
                        });
                    }
                }
                VehicleNet.AskForCars();
            };
            GameEvents.RaidEnded += (o, exit) =>
            {
                if (VehicleDriver.Local != null) VehicleDriver.Local.Leave();
                AccountProfile acct = ProfileStore.Account;
                if (acct == null || !NetAuthority.IsServer || GameSession.IsMonsterRun) return;
                ZoneWorldState z = acct.Zone(GameSession.ZoneId);
                z.Vehicles.Clear();
                foreach (Vehicle v in Vehicle.All) if (v != null) z.Vehicles.Add(v.Capture());
                ProfileStore.Save();
            };
            NetModuleHooks.All.Add(s => VehicleNet.Register());
        }
    }
}
