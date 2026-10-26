using UnityEngine;

namespace CloneZ
{
    /// <summary>The bodies of the cars and trucks left in Kentucky in January 1993 (mostly late seventies to early
    /// nineties: GM and Ford sedans, Country Squire wagons, square-body pickups, Caravans, Broncos and Econolines).</summary>
    internal enum CarBody : byte
    {
        /// <summary>A mid-size three-box sedan (Celebrity, Taurus, Cutlass Ciera, Accord).</summary>
        Sedan,
        /// <summary>A full-size sedan (Caprice, Crown Victoria, LTD, an old Impala).</summary>
        BigSedan,
        /// <summary>A long two-door with a formal roof (Monte Carlo, Cutlass Supreme, Thunderbird).</summary>
        Coupe,
        /// <summary>A long-roof station wagon, often with fake wood down the sides.</summary>
        Wagon,
        /// <summary>A small hatchback (Escort, Civic, Cavalier, Omni).</summary>
        Hatch,
        /// <summary>A full-size pickup (F-150, C/K), now and then with a camper shell.</summary>
        Pickup,
        /// <summary>A boxy two-door sport utility (Bronco, Blazer, Cherokee).</summary>
        Suv,
        /// <summary>A minivan (Caravan, Voyager, Aerostar, Astro).</summary>
        Minivan,
        /// <summary>A full-size van (Econoline, G-van): panel or windowed.</summary>
        Van,
        /// <summary>A police cruiser (a white Caprice with a light bar).</summary>
        Cruiser,
        /// <summary>A yellow school bus.</summary>
        Bus,
        /// <summary>A box truck (a moving van, a delivery truck).</summary>
        BoxTruck,
    }

    /// <summary>What's happened to a vehicle since it was left.</summary>
    [System.Flags]
    internal enum CarWear : byte { None = 0, DoorOpen = 1, Crashed = 2, Burnt = 4, Flat = 8 }

    /// <summary>How one vehicle looks: its body, paint and trim, how deep the snow lies on it and what's happened to it.</summary>
    internal struct CarLook
    {
        public CarBody Body;
        public Color Paint;
        /// <summary>The second colour: a two-tone's lower body, a vinyl roof, a cruiser's stripe, a box truck's box.</summary>
        public Color Second;
        public bool TwoTone, Vinyl, Woodgrain, Chrome, Topper;
        /// <summary>0: brushed off, 1: buried.</summary>
        public float Snow;
        /// <summary>Windows iced over.</summary>
        public bool Frosted;
        public CarWear Wear;
    }

    /// <summary>
    /// The nature passes' vehicles, drawn round the local frame's origin on the ground, nose towards +Z: a lower body with
    /// bumpers, grille, lights and a plate, a greenhouse with raked glass (frosted over on most), wheels with hubcaps, two-
    /// tone paint, vinyl roofs, woodgrain sides, a pickup's snow-filled bed or camper shell; snow on the roof, hood and
    /// trunk and drifted against the wheels; an open door, a crumpled front or a burnt-out shell. Palette colours only.
    /// </summary>
    internal static class CarKit
    {
        private static Surf M(Color c) => DecorPalette.Matte(c);
        private static Surf G(Color c) => DecorPalette.Gloss(c);
        private static Surf Mt(Color c) => DecorPalette.Metal(c);
        private static Surf Gs(Color c) => DecorPalette.Glass(c);
        private static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        private static readonly Color Snow = DecorProps.SnowWhite;
        private static readonly Color SnowShade = DecorProps.SnowShade;
        private static readonly Color Tyre = new Color(0.07f, 0.07f, 0.075f);
        private static readonly Color Chrome = new Color(0.66f, 0.67f, 0.68f);
        private static readonly Color Trim = new Color(0.13f, 0.13f, 0.14f);
        private static readonly Color DarkGlass = new Color(0.07f, 0.09f, 0.11f);
        private static readonly Color Frost = new Color(0.50f, 0.56f, 0.63f);
        private static readonly Color Charred = new Color(0.15f, 0.12f, 0.10f);
        private static readonly Color Rust = new Color(0.36f, 0.19f, 0.10f);

        /// <summary>A car-like body's measurements (metres; z from the middle, + forwards).</summary>
        private struct Spec
        {
            public float L, W, Roof, Belt, Wheel, AxleF, AxleR;
            /// <summary>The greenhouse: its foot's back and front, its roof's back and front.</summary>
            public float Back0, Front0, Back1, Front1;
            /// <summary>How far the roof's sides lean in.</summary>
            public float Inset;
        }

        /// <summary>Gets the geometric specifications for a given vehicle body type.</summary>
        // Returns the dimension set for the vehicle type, used throughout rendering to position parts correctly.
        // Choose dimensions based on vehicle body type; these specs drive placement of all parts.
        private static Spec SpecOf(CarBody body)
        {
            switch (body)
            {
                case CarBody.BigSedan: case CarBody.Cruiser:
                    return new Spec { L = 5.4f, W = 1.95f, Roof = 1.40f, Belt = 0.90f, Wheel = 0.33f, AxleF = 1.62f, AxleR = -1.45f,
                        Back0 = -1.15f, Front0 = 0.85f, Back1 = -0.80f, Front1 = 0.30f, Inset = 0.13f };
                case CarBody.Coupe:
                    return new Spec { L = 5.05f, W = 1.84f, Roof = 1.33f, Belt = 0.86f, Wheel = 0.32f, AxleF = 1.50f, AxleR = -1.38f,
                        Back0 = -1.15f, Front0 = 0.60f, Back1 = -0.75f, Front1 = 0.08f, Inset = 0.13f };
                case CarBody.Wagon:
                    return new Spec { L = 5.3f, W = 1.92f, Roof = 1.44f, Belt = 0.89f, Wheel = 0.32f, AxleF = 1.60f, AxleR = -1.40f,
                        Back0 = -2.55f, Front0 = 0.80f, Back1 = -2.48f, Front1 = 0.28f, Inset = 0.10f };
                case CarBody.Hatch:
                    return new Spec { L = 4.2f, W = 1.66f, Roof = 1.36f, Belt = 0.84f, Wheel = 0.30f, AxleF = 1.28f, AxleR = -1.18f,
                        Back0 = -1.95f, Front0 = 0.55f, Back1 = -1.55f, Front1 = 0.0f, Inset = 0.12f };
                case CarBody.Pickup:
                    return new Spec { L = 5.3f, W = 2.0f, Roof = 1.84f, Belt = 1.05f, Wheel = 0.38f, AxleF = 1.75f, AxleR = -1.45f,
                        Back0 = -0.62f, Front0 = 0.86f, Back1 = -0.62f, Front1 = 0.42f, Inset = 0.08f };
                case CarBody.Suv:
                    return new Spec { L = 4.7f, W = 1.94f, Roof = 1.80f, Belt = 1.04f, Wheel = 0.38f, AxleF = 1.45f, AxleR = -1.30f,
                        Back0 = -2.30f, Front0 = 0.62f, Back1 = -2.25f, Front1 = 0.25f, Inset = 0.08f };
                case CarBody.Minivan:
                    return new Spec { L = 4.65f, W = 1.84f, Roof = 1.68f, Belt = 0.95f, Wheel = 0.33f, AxleF = 1.52f, AxleR = -1.35f,
                        Back0 = -2.28f, Front0 = 1.15f, Back1 = -2.22f, Front1 = 0.25f, Inset = 0.08f };
                case CarBody.Van:
                    return new Spec { L = 5.4f, W = 2.02f, Roof = 2.06f, Belt = 1.06f, Wheel = 0.38f, AxleF = 1.75f, AxleR = -1.65f,
                        Back0 = -2.66f, Front0 = 1.55f, Back1 = -2.66f, Front1 = 1.15f, Inset = 0.04f };
                default:
                    return new Spec { L = 4.8f, W = 1.76f, Roof = 1.36f, Belt = 0.86f, Wheel = 0.31f, AxleF = 1.42f, AxleR = -1.30f,
                        Back0 = -1.00f, Front0 = 0.80f, Back1 = -0.68f, Front1 = 0.22f, Inset = 0.12f };
            }
        }

        /// <summary>The vehicle's box (width, height, length) for its collider, without drawing it.</summary>
        public static Vector3 Size(CarBody body)
        {
            if (body == CarBody.Bus) return new Vector3(2.44f, 3.0f, 10.8f);
            if (body == CarBody.BoxTruck) return new Vector3(2.36f, 3.3f, 7.2f);
            Spec s = SpecOf(body);
            return new Vector3(s.W, s.Roof, s.L);
        }

        /// <summary>Draws the vehicle at the frame's origin, nose to +Z, and returns its box (width, height, length).</summary>
        public static Vector3 Draw(RaidMesh m, in CarLook look, System.Random rng)
        {
            if (look.Body == CarBody.Bus) return Bus(m, look, rng);
            if (look.Body == CarBody.BoxTruck) return BoxTruck(m, look, rng);
            Spec s = SpecOf(look.Body);
            bool burnt = (look.Wear & CarWear.Burnt) != 0, crashed = (look.Wear & CarWear.Crashed) != 0;
            float sink = burnt ? s.Wheel * 0.45f : 0f;   // a burnt-out shell sits on its rims
            float clear = s.Wheel * 0.55f - sink, belt = s.Belt - sink, roof = s.Roof - sink;
            Color paint = burnt ? Charred : look.Paint;
            Surf body = burnt ? M(Charred) : G(paint);
            Surf snow = M(Snow);
            bool buried = look.Snow > 0.78f;
            Surf glass = burnt ? M(new Color(0.04f, 0.035f, 0.03f)) : look.Frosted ? M(Frost) : Gs(DarkGlass);
            Surf windscreen = buried ? snow : crashed && !burnt ? Gs(new Color(0.50f, 0.55f, 0.58f)) : glass;

            Wheels(m, s, look.Wear, rng, burnt);
            m.Box(new Vector3(0f, (clear + belt) * 0.5f, 0f), new Vector3(s.W, belt - clear, s.L - 0.1f), body);
            if (look.TwoTone && !burnt)
                m.Box(new Vector3(0f, clear + 0.13f, 0f), new Vector3(s.W + 0.012f, 0.26f, s.L - 0.08f), G(look.Second));
            if (look.Woodgrain && !burnt) Woodgrain(m, s, clear, belt);
            float hw0 = s.W * 0.5f - 0.04f, hw1 = s.W * 0.5f - s.Inset;
            bool panelSides = look.Body == CarBody.Van && !look.Vinyl;   // a cargo van ("Vinyl" stands for windows here)
            Surf roofSurf = look.Vinyl && look.Body != CarBody.Van && !burnt ? M(look.Second) : body;
            Cabin(m, s, belt, roof, hw0, hw1, roofSurf, windscreen, glass, panelSides ? body : glass, body, look.Body);
            if (look.Body == CarBody.Pickup) Bed(m, s, look, belt, clear, body, snow);
            Front(m, s, look, clear, belt, crashed, rng);
            Rear(m, s, look, clear, belt);
            if (look.Body == CarBody.Cruiser && !burnt) CruiserKit(m, s, look, clear, belt, roof);
            if (look.Snow > 0.05f) Snowfall(m, s, look, belt, roof, hw1, rng);
            if ((look.Wear & CarWear.DoorOpen) != 0) OpenDoor(m, s, look, clear, belt, roof, body, glass, rng);
            if (burnt) Scorch(m, s, clear, belt, rng);
            return new Vector3(s.W, roof, s.L);
        }

        #region Parts
        // A quad from four corners in order round it, turned to face along `outward`.
        private static void Face(RaidMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, in Surf s)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f)
            {
                Vector3 t = b;
                b = d;
                d = t;
            }
            m.Tri(a, b, c, s);
            m.Tri(a, c, d, s);
        }

        private static void Wheels(RaidMesh m, in Spec s, CarWear wear, System.Random rng, bool burnt)
        {
            int flat = (wear & CarWear.Flat) != 0 ? rng.Next(4) : -1;
            Surf tyre = burnt ? Mt(Rust) : M(Tyre);
            // Two wheel positions per axle: left (-1) and right (+1).
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (s.W * 0.5f - 0.13f);
                float z = i < 2 ? s.AxleF : s.AxleR;
                float r = burnt ? s.Wheel * 0.6f : i == flat ? s.Wheel * 0.8f : s.Wheel;
                m.CylinderX(new Vector3(x, r, z), r, 0.22f, 6, tyre);
            }
        }

        // Fake wood down both sides (a Country Squire's), framed by a pale strip.
        private static void Woodgrain(RaidMesh m, in Spec s, float clear, float belt)
        {
            float h = (belt - clear) * 0.55f, y = clear + (belt - clear) * 0.5f;
            Surf wood = M(new Color(0.42f, 0.26f, 0.14f)), rim = M(new Color(0.78f, 0.72f, 0.58f));
            for (int side = -1; side <= 1; side += 2)
            {
                var normal = new Vector3(side, 0f, 0f);
                float x = side * (s.W * 0.5f + 0.006f);
                m.Upright(new Vector3(x, y, -0.05f), normal, s.L * 0.8f, h, wood);
                m.Upright(new Vector3(side * (s.W * 0.5f + 0.008f), y + h * 0.5f + 0.02f, -0.05f), normal, s.L * 0.8f, 0.04f, rim);
            }
        }

        // The greenhouse: a roof, raked windscreen and back glass, sides of glass (or panel), and a pillar of paint up the
        // middle of each side.
        private static void Cabin(RaidMesh m, in Spec s, float y0, float y1, float hw0, float hw1, in Surf roof, in Surf windscreen,
            in Surf back, in Surf sides, in Surf body, CarBody kind)
        {
            var bl = new Vector3(-hw0, y0, s.Back0);
            var br = new Vector3(hw0, y0, s.Back0);
            var fl = new Vector3(-hw0, y0, s.Front0);
            var fr = new Vector3(hw0, y0, s.Front0);
            var tbl = new Vector3(-hw1, y1, s.Back1);
            var tbr = new Vector3(hw1, y1, s.Back1);
            var tfl = new Vector3(-hw1, y1, s.Front1);
            var tfr = new Vector3(hw1, y1, s.Front1);
            Face(m, tfl, tfr, tbr, tbl, Vector3.up, roof);
            Face(m, fl, fr, tfr, tfl, new Vector3(0f, 0.6f, 1f), windscreen);
            Face(m, bl, br, tbr, tbl, new Vector3(0f, 0.4f, -1f), back);
            Face(m, bl, fl, tfl, tbl, Vector3.left, sides);
            Face(m, br, fr, tfr, tbr, Vector3.right, sides);
            if (kind == CarBody.Pickup) return;
            // B pillars (and a wagon's or a van's D pillars near the back).
            for (int side = -1; side <= 1; side += 2)
            {
                // Choose pillar height proportion based on vehicle type (longer for coupes).
                float t = kind == CarBody.Coupe ? 0.62f : 0.5f;
                Pillar(m, s, hw0, hw1, y0, y1, side, t, body);
                if (kind == CarBody.Wagon || kind == CarBody.Suv || kind == CarBody.Minivan) Pillar(m, s, hw0, hw1, y0, y1, side, 0.22f, body);
            }
        }

        // A strip of paint up a side of the greenhouse, `t` of the way from its back to its front.
        private static void Pillar(RaidMesh m, in Spec s, float hw0, float hw1, float y0, float y1, int side, float t, in Surf body)
        {
            float x0 = side * (hw0 + 0.006f), x1 = side * (hw1 + 0.006f);
            float z0 = Mathf.Lerp(s.Back0, s.Front0, t), z1 = Mathf.Lerp(s.Back1, s.Front1, t);
            const float w = 0.05f;
            Face(m, new Vector3(x0, y0, z0 - w), new Vector3(x0, y0, z0 + w), new Vector3(x1, y1, z1 + w), new Vector3(x1, y1, z1 - w),
                new Vector3(side, 0f, 0f), body);
        }

        // A pickup's bed: rails along its sides and a tailgate, full of snow (or under a camper shell).
        /// <summary>Draws the pickup bed (rails, tailgate, optional camper shell) and handles snow accumulation.</summary>
        // Builds the pickup's cargo area: side rails, tailgate, and optionally a camper shell, then adds snow if present.
        private static void Bed(RaidMesh m, in Spec s, in CarLook look, float belt, float clear, in Surf body, in Surf snow)
        {
            float back = -s.L * 0.5f + 0.05f, front = s.Back0 - 0.04f, len = front - back, mid = (front + back) * 0.5f;
            if (look.Topper)
            {
                Color shell = look.Second;
                m.Box(new Vector3(0f, belt + 0.32f, mid), new Vector3(s.W - 0.06f, 0.64f, len), G(shell));
                for (int side = -1; side <= 1; side += 2)
                    m.Upright(new Vector3(side * (s.W * 0.5f - 0.025f), belt + 0.36f, mid), new Vector3(side, 0f, 0f), len * 0.7f, 0.3f, Gs(DarkGlass));
                m.Upright(new Vector3(0f, belt + 0.36f, back - 0.006f), Vector3.back, s.W * 0.7f, 0.32f, Gs(DarkGlass));
                if (look.Snow > 0.05f)
                    m.Box(new Vector3(0f, belt + 0.64f + look.Snow * 0.08f, mid), new Vector3(s.W - 0.2f, 0.04f + look.Snow * 0.16f, len - 0.15f),
                        Quaternion.identity, snow, true);
                return;
            }
            for (int side = -1; side <= 1; side += 2)
                m.Box(new Vector3(side * (s.W * 0.5f - 0.04f), belt + 0.06f, mid), new Vector3(0.08f, 0.12f, len), body);
            m.Box(new Vector3(0f, belt + 0.06f, back + 0.04f), new Vector3(s.W, 0.12f, 0.08f), body);
            if (look.Snow > 0.05f)
                m.Box(new Vector3(0f, belt + 0.02f + look.Snow * 0.08f, mid + 0.02f), new Vector3(s.W - 0.16f, 0.04f + look.Snow * 0.16f, len - 0.14f),
                    Quaternion.identity, snow, true);
            else m.Upright(new Vector3(0f, belt + 0.003f, mid), Vector3.up, s.W - 0.16f, len - 0.14f, M(Trim));
        }

        // Bumper, grille and headlights; on a wreck the hood buckled up and the bumper knocked askew.
        /// <summary>Creates the front end (bumper, grille, headlights) and applies crash deformation if needed.</summary>
        // Draws bumper, grille and headlights; if the vehicle is crashed, skews parts to suggest impact damage.
        // Add front bumper, grille and headlights; if the vehicle is crashed we skew them to suggest impact damage.
        private static void Front(RaidMesh m, in Spec s, in CarLook look, float clear, float belt, bool crashed, System.Random rng)
        {
            Surf bumper = look.Chrome ? Mt(Chrome) : M(Trim);
            float z = s.L * 0.5f;
            float grilleY = (clear + belt) * 0.5f + 0.06f, h = belt - clear;
            // If crashed, randomly offset and tilt bumper and hood to look damaged.
            if (crashed)
            {
                m.Box(new Vector3(R(rng, -0.1f, 0.1f), clear + 0.08f, z - 0.02f), new Vector3(s.W + 0.03f, 0.16f, 0.14f),
                    Quaternion.Euler(0f, R(rng, -12f, 12f), R(rng, -8f, 8f)), bumper);
                Color paint = (look.Wear & CarWear.Burnt) != 0 ? Charred : look.Paint;
                m.Box(new Vector3(0f, belt + 0.08f, z - 0.55f), new Vector3(s.W - 0.12f, 0.06f, 0.9f), Quaternion.Euler(-14f, R(rng, -6f, 6f), 0f), G(paint));
                m.Upright(new Vector3(0f, grilleY, z - 0.035f), Vector3.forward, s.W * 0.7f, h * 0.5f, M(new Color(0.08f, 0.08f, 0.08f)));
                return;
            }
            // Draw standard bumper, grille and headlights when vehicle is intact.
            // (No else block needed; this code runs when not crashed.)
            m.Box(new Vector3(0f, clear + 0.13f, z - 0.03f), new Vector3(s.W + 0.03f, 0.16f, 0.14f), bumper);
            // Add grille and headlights positioned relative to the bumper.
            m.Upright(new Vector3(0f, grilleY, z - 0.04f), Vector3.forward, s.W * 0.46f, h * 0.42f, look.Chrome ? Mt(Chrome * 0.8f) : M(Trim));
            Surf lamp = Gs(new Color(0.72f, 0.72f, 0.66f));
            // Place two headlamps on either side of the grille.
            for (int side = -1; side <= 1; side += 2)
                m.Upright(new Vector3(side * s.W * 0.34f, grilleY, z - 0.038f), Vector3.forward, 0.32f, 0.14f, lamp);
        }

        // Bumper, taillights and the plate.
        private static void Rear(RaidMesh m, in Spec s, in CarLook look, float clear, float belt)
        {
            float z = -s.L * 0.5f;
            m.Box(new Vector3(0f, clear + 0.13f, z + 0.03f), new Vector3(s.W + 0.03f, 0.16f, 0.14f), look.Chrome ? Mt(Chrome) : M(Trim));
            Surf red = Gs(new Color(0.46f, 0.06f, 0.05f));
            for (int side = -1; side <= 1; side += 2)
                m.Upright(new Vector3(side * s.W * 0.36f, belt - 0.16f, z + 0.04f), Vector3.back, 0.36f, 0.12f, red);
            m.Upright(new Vector3(0f, clear + 0.34f, z + 0.037f), Vector3.back, 0.3f, 0.15f, M(new Color(0.86f, 0.86f, 0.80f)));
        }

        // A cruiser's light bar and the stripe down its sides, "POLICE" on its doors.
        private static void CruiserKit(RaidMesh m, in Spec s, in CarLook look, float clear, float belt, float roof)
        {
            float mid = (s.Back1 + s.Front1) * 0.5f + 0.15f;
            m.Box(new Vector3(0f, roof + 0.06f, mid), new Vector3(1.2f, 0.1f, 0.26f), M(Trim));
            m.Box(new Vector3(-0.32f, roof + 0.14f, mid), new Vector3(0.5f, 0.1f, 0.22f), Gs(new Color(0.55f, 0.07f, 0.07f)));
            m.Box(new Vector3(0.32f, roof + 0.14f, mid), new Vector3(0.5f, 0.1f, 0.22f), Gs(new Color(0.08f, 0.14f, 0.55f)));
            Surf word = DecorMaterials.Sign("POLICE", look.Paint, look.Second, out float aspect);
            for (int side = -1; side <= 1; side += 2)
            {
                var normal = new Vector3(side, 0f, 0f);
                m.Upright(new Vector3(side * (s.W * 0.5f + 0.006f), belt - 0.32f, 0.1f), normal, s.L * 0.86f, 0.08f, G(look.Second));
                m.Upright(new Vector3(side * (s.W * 0.5f + 0.008f), belt - 0.16f, -0.2f), normal, 0.2f * aspect, 0.2f, word,
                    side > 0 ? new Rect(0f, 0f, 1f, 1f) : new Rect(0f, 0f, 1f, 1f));
            }
        }

        // Snow on the roof (heaped in a second, narrower layer when deep), the hood and the trunk, a rim of paint showing
        // round each (a car brushed off, or under a thin fall, shows most of its paint), and banked against the wheels of
        // a car that hasn't moved in a while.
        private static void Snowfall(RaidMesh m, in Spec s, in CarLook look, float belt, float roof, float hw1, System.Random rng)
        {
            Surf snow = M(Snow);
            float d = 0.03f + look.Snow * 0.2f, dh = d * 0.65f;
            float rim = Mathf.Lerp(0.34f, 0.12f, look.Snow);
            float roofLen = s.Front1 - s.Back1 - rim, roofMid = (s.Front1 + s.Back1) * 0.5f, roofW = hw1 * 2f - rim;
            m.Box(new Vector3(0f, roof + d * 0.5f, roofMid), new Vector3(roofW, d, roofLen), Quaternion.identity, snow, true);
            if (d > 0.1f)
                m.Box(new Vector3(0f, roof + d + d * 0.2f, roofMid), new Vector3(roofW - 0.34f, d * 0.4f, roofLen - 0.4f), Quaternion.identity, snow, true);
            if (look.Snow < 0.2f) return;   // a thin fall: the roof only
            float hood = s.L * 0.5f - s.Front0 - 0.14f - rim;
            if (hood > 0.3f && (look.Wear & CarWear.Crashed) == 0)
                m.Box(new Vector3(0f, belt + dh * 0.5f, s.Front0 + 0.06f + rim * 0.5f + hood * 0.5f), new Vector3(s.W - rim * 1.6f, dh, hood),
                    Quaternion.identity, snow, true);
            float trunk = s.Back0 + s.L * 0.5f - 0.14f - rim;
            if (trunk > 0.3f && look.Body != CarBody.Pickup)
                m.Box(new Vector3(0f, belt + dh * 0.5f, -s.L * 0.5f + 0.08f + rim * 0.5f + trunk * 0.5f), new Vector3(s.W - rim * 1.6f, dh, trunk),
                    Quaternion.identity, snow, true);
            if (look.Snow < 0.6f) return;
            Surf drift = M(R(rng, 0f, 1f) < 0.5f ? Snow : SnowShade);
            for (int side = -1; side <= 1; side += 2)
                m.Ellipsoid(new Vector3(side * (s.W * 0.5f + 0.04f), 0.05f, R(rng, -0.2f, 0.2f)),
                    new Vector3(0.62f, 0.45f + look.Snow * 0.3f, s.L * R(rng, 0.62f, 0.8f)), drift, 6, 3);
        }

        // The driver's door hanging open: its panel and window frame swung out on the front hinge, the dark cab behind.
        private static void OpenDoor(RaidMesh m, in Spec s, in CarLook look, float clear, float belt, float roof, in Surf body, in Surf glass,
            System.Random rng)
        {
            float len = Mathf.Min(1.15f, (s.Front0 - s.Back0) * (look.Body == CarBody.Coupe ? 0.75f : 0.52f));
            var hinge = new Vector3(-s.W * 0.5f, 0f, s.Front0 - 0.02f);
            float a = R(rng, 42f, 70f) * Mathf.Deg2Rad;
            var dir = new Vector3(-Mathf.Sin(a), 0f, -Mathf.Cos(a));
            Quaternion q = Quaternion.LookRotation(dir);
            Vector3 c = hinge + dir * (len * 0.5f);
            m.Box(new Vector3(c.x, (clear + 0.05f + belt) * 0.5f, c.z), new Vector3(0.06f, belt - clear - 0.05f, len), q, body);
            if (look.Body != CarBody.Van)
                m.Box(new Vector3(c.x, belt + (roof - belt) * 0.42f, c.z), new Vector3(0.03f, (roof - belt) * 0.8f, len * 0.92f), q, glass);
            m.Upright(new Vector3(-s.W * 0.5f - 0.004f, (clear + belt) * 0.5f + 0.08f, s.Front0 - 0.02f - len * 0.5f), Vector3.left,
                len - 0.06f, belt - clear - 0.12f, M(new Color(0.09f, 0.08f, 0.08f)));
        }

        // Rust blooms over a burnt-out shell.
        private static void Scorch(RaidMesh m, in Spec s, float clear, float belt, System.Random rng)
        {
            Surf rust = M(Rust);
            for (int i = 0; i < 3; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                m.Blob(new Vector3(side * (s.W * 0.5f + 0.005f), Mathf.Lerp(clear, belt, R(rng, 0.25f, 0.75f)), R(rng, -s.L * 0.35f, s.L * 0.35f)),
                    new Vector3(0f, 0f, side), Vector3.up, R(rng, 0.25f, 0.5f), R(rng, 0.1f, 0.2f), 8, 0.4f, rng, rust);
            }
        }
        #endregion

        #region Big vehicles
        // A conventional yellow school bus: hood out front, a long box with its black rub rails and a band of windows,
        // "SCHOOL BUS" over the windscreen and the back door, dual rear wheels, a white roof under the snow.
        /// <summary>Generates a yellow school bus model with its distinctive shape, windows, and signage.</summary>
        // Builds a conventional yellow school bus: long box, raised roof, dual rear wheels, signage and snow covering.
        // Generate a yellow school bus: long box, raised roof, dual rear wheels, signage and snow covering.
        private static Vector3 Bus(RaidMesh m, in CarLook look, System.Random rng)
        {
            const float L = 10.8f, W = 2.44f, H = 3.0f, wheel = 0.5f, clear = 0.42f, front = 3.9f;
            Color yellow = DecorPalette.Vary(new Color(0.92f, 0.66f, 0.10f), 0.05f, rng);
            Surf body = G(yellow), black = M(Trim), snow = M(Snow);
            Surf glass = look.Frosted ? M(Frost) : Gs(DarkGlass);
            Surf tyre = M(Tyre);
            float[] axles = { 3.45f, -2.55f };
            for (int a = 0; a < 2; a++)
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * (W * 0.5f - 0.16f);
                    m.CylinderX(new Vector3(x, wheel, axles[a]), wheel, 0.28f, 8, tyre);
                    // Add a second rear wheel for the rear axle (dual wheels).
                    if (a == 1) m.CylinderX(new Vector3(x - side * 0.3f, wheel, axles[a]), wheel, 0.26f, 8, tyre);
                }
            float boxLen = front + L * 0.5f;
            m.Box(new Vector3(0f, (clear + H) * 0.5f, front - boxLen * 0.5f), new Vector3(W, H - clear, boxLen), body);
            m.Box(new Vector3(0f, (clear + 1.55f) * 0.5f, (front + L * 0.5f) * 0.5f), new Vector3(W - 0.24f, 1.55f - clear, L * 0.5f - front), body);
            m.Box(new Vector3(0f, clear + 0.2f, L * 0.5f - 0.04f), new Vector3(W - 0.14f, 0.2f, 0.12f), black);
            m.Box(new Vector3(0f, clear + 0.2f, -L * 0.5f + 0.04f), new Vector3(W + 0.02f, 0.22f, 0.12f), black);
            for (int side = -1; side <= 1; side += 2)
            {
                var normal = new Vector3(side, 0f, 0f);
                float x = side * (W * 0.5f + 0.006f), mid = front - boxLen * 0.5f;
                m.Upright(new Vector3(x, 2.05f, mid - 0.3f), normal, boxLen - 1.6f, 0.78f, glass);
                m.Upright(new Vector3(side * (W * 0.5f + 0.008f), 1.12f, mid), normal, boxLen - 0.3f, 0.07f, black);
                m.Upright(new Vector3(side * (W * 0.5f + 0.008f), 1.55f, mid), normal, boxLen - 0.3f, 0.07f, black);
            }
            m.Upright(new Vector3(0f, 2.1f, front + 0.006f), Vector3.forward, W - 0.3f, 0.95f, look.Snow > 0.78f ? snow : glass);
            m.Upright(new Vector3(0f, 2.0f, -L * 0.5f - 0.006f), Vector3.back, 1.0f, 1.0f, glass);
            Surf sign = DecorMaterials.Sign("SCHOOL BUS", yellow, new Color(0.06f, 0.06f, 0.06f), out float aspect);
            m.Upright(new Vector3(0f, 2.78f, front + 0.008f), Vector3.forward, 0.26f * aspect, 0.26f, sign, new Rect(0f, 0f, 1f, 1f));
            m.Upright(new Vector3(0f, 2.78f, -L * 0.5f - 0.008f), Vector3.back, 0.26f * aspect, 0.26f, sign, new Rect(0f, 0f, 1f, 1f));
            float d = 0.04f + look.Snow * 0.18f;
            m.Box(new Vector3(0f, H + 0.02f, front - boxLen * 0.5f), new Vector3(W - 0.04f, 0.04f, boxLen - 0.04f), Quaternion.identity,
                M(new Color(0.88f, 0.88f, 0.84f)), true);
            m.Box(new Vector3(0f, H + 0.04f + d * 0.5f, front - boxLen * 0.5f), new Vector3(W - 0.2f, d, boxLen - 0.3f), Quaternion.identity, snow, true);
            m.Box(new Vector3(0f, 1.55f + d * 0.3f, (front + L * 0.5f) * 0.5f), new Vector3(W - 0.4f, d * 0.6f, L * 0.5f - front - 0.15f),
                Quaternion.identity, snow, true);
            return new Vector3(W, H, L);
        }

        // A box truck: a cab with its hood and the long box behind it (a moving van's orange and white, a delivery
        // company's plain colour), dual rear wheels.
        private static Vector3 BoxTruck(RaidMesh m, in CarLook look, System.Random rng)
        {
            const float L = 7.2f, W = 2.36f, wheel = 0.45f, clear = 0.4f;
            Surf cab = G(look.Paint), box = G(look.Second), black = M(Trim), snow = M(Snow);
            Surf glass = look.Frosted ? M(Frost) : Gs(DarkGlass);
            Surf tyre = M(Tyre);
            float[] axles = { 2.75f, -2.2f };
            for (int a = 0; a < 2; a++)
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * (W * 0.5f - 0.18f);
                    m.CylinderX(new Vector3(x, wheel, axles[a]), wheel, 0.26f, 8, tyre);
                    if (a == 1) m.CylinderX(new Vector3(x - side * 0.28f, wheel, axles[a]), wheel, 0.24f, 8, tyre);
                }
            m.Box(new Vector3(0f, 0.68f, 0f), new Vector3(W - 0.5f, 0.3f, L - 0.3f), black);   // the frame
            m.Box(new Vector3(0f, (0.85f + 3.3f) * 0.5f, -0.95f), new Vector3(W, 3.3f - 0.85f, 5.3f), box);
            m.Box(new Vector3(0f, (clear + 2.5f) * 0.5f, 2.3f), new Vector3(W - 0.08f, 2.5f - clear, 1.1f), cab);
            m.Box(new Vector3(0f, (clear + 1.3f) * 0.5f, 3.2f), new Vector3(W - 0.18f, 1.3f - clear, 0.8f), cab);
            m.Upright(new Vector3(0f, 1.95f, 2.856f), Vector3.forward, W - 0.3f, 0.7f, look.Snow > 0.78f ? snow : glass);
            for (int side = -1; side <= 1; side += 2)
                m.Upright(new Vector3(side * (W * 0.5f - 0.035f), 1.95f, 2.35f), new Vector3(side, 0f, 0f), 0.7f, 0.6f, glass);
            m.Box(new Vector3(0f, clear + 0.18f, L * 0.5f - 0.03f), new Vector3(W - 0.1f, 0.2f, 0.12f), look.Chrome ? Mt(Chrome) : black);
            float d = 0.04f + look.Snow * 0.18f;
            m.Box(new Vector3(0f, 3.3f + d * 0.5f, -0.95f), new Vector3(W - 0.12f, d, 5.15f), Quaternion.identity, snow, true);
            m.Box(new Vector3(0f, 2.5f + d * 0.5f, 2.3f), new Vector3(W - 0.24f, d, 0.9f), Quaternion.identity, snow, true);
            m.Box(new Vector3(0f, 1.3f + d * 0.4f, 3.2f), new Vector3(W - 0.34f, d * 0.8f, 0.6f), Quaternion.identity, snow, true);
            return new Vector3(W, 3.3f, L);
        }
        #endregion
    }
}
