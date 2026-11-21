namespace CloneZ
{
    /// <summary>Every procedural sprite the HUD draws (see <see cref="HudIconArt"/> for the art).</summary>
    public enum HudIcon
    {
        // UI building blocks
        White, Disc, Ring, Glow, PanelFill, PanelFrame, Segment,
        // hand slots
        LongGun, Pistol, Blade, Blunt, Axe, Hammer, Fist, Flashlight, Goggles,
        // status row (an outline plus the interior that fills with the level)
        Battery, BatteryFill, Bottle, BottleFill, Cutlery, Thermometer, ThermometerFill, Cross, CrossFill,
        // watch row
        Magnifier, Bell,
        // movement
        Stand, Walk1, Walk2, Run1, Run2, Sneak1, Sneak2,
        // exits list
        Arrow,
        // moodle tiles
        Drop, Virus, Scared, Snowflake, Sun, Nausea, Moon, Burst, Droplets, Weight, Gas,
    }

    /// <summary>Texture size, authoring scale and slicing of one procedural sprite.</summary>
    public readonly struct HudIconSpec
    {
        public readonly int Width, Height;
        /// <summary>Authoring units across the texture's width.</summary>
        public readonly float Units;
        /// <summary>9-slice borders in pixels (0 = a simple sprite).</summary>
        public readonly int BorderX, BorderY;
        public readonly bool Mips;

        public HudIconSpec(int width, int height, float units, int borderX, int borderY, bool mips)
        {
            Width = width; Height = height; Units = units; BorderX = borderX; BorderY = borderY; Mips = mips;
        }
    }

    /// <summary>
    /// The HUD's line art, drawn with <see cref="HudRaster"/>: clean white strokes in the style of the
    /// Project Zomboid HUD mods (weapon silhouettes, status pictograms, moodle glyphs, a running man).
    /// Icons are authored on a 64-unit square, y up. Pure C# so the art can be previewed outside Unity.
    /// </summary>
    public static class HudIconArt
    {
        /// <summary>How big to bake an icon (and how it is sliced).</summary>
        public static HudIconSpec Spec(HudIcon id)
        {
            switch (id)
            {
                case HudIcon.White: return new HudIconSpec(4, 4, 4f, 0, 0, false);
                case HudIcon.Disc:
                case HudIcon.Ring: return new HudIconSpec(128, 128, 64f, 0, 0, true);
                case HudIcon.PanelFill:
                case HudIcon.PanelFrame: return new HudIconSpec(32, 32, 32f, 10, 10, false);
                case HudIcon.Segment: return new HudIconSpec(64, 16, 64f, 8, 0, false);
                case HudIcon.LongGun:
                case HudIcon.Pistol:
                case HudIcon.Blade:
                case HudIcon.Blunt:
                case HudIcon.Axe:
                case HudIcon.Hammer:
                case HudIcon.Fist:
                case HudIcon.Flashlight:
                case HudIcon.Goggles: return new HudIconSpec(96, 96, 64f, 0, 0, true);
                default: return new HudIconSpec(64, 64, 64f, 0, 0, true);
            }
        }

        /// <summary>The span (0..1 of the sprite) a level fill sweeps for a fill sprite: horizontal for the
        /// battery, bottom-up for the rest. Lets a 0..1 stat map onto the visible interior only.</summary>
        public static void FillRange(HudIcon id, out float min, out float max)
        {
            switch (id)
            {
                case HudIcon.BatteryFill: min = 9f / 64f; max = 49f / 64f; break;
                case HudIcon.BottleFill: min = 12f / 64f; max = 38f / 64f; break;
                case HudIcon.ThermometerFill: min = 20f / 64f; max = 51f / 64f; break;   // the bulb always holds some
                case HudIcon.CrossFill: min = 10.5f / 64f; max = 53.5f / 64f; break;
                case HudIcon.Cutlery: min = 4.5f / 64f; max = 59f / 64f; break;
                default: min = 0f; max = 1f; break;
            }
        }

        /// <summary>Draws one icon onto a raster sized by <see cref="Spec"/>.</summary>
        public static void Draw(HudIcon id, HudRaster r)
        {
            switch (id)
            {
                case HudIcon.White: r.Box(-1f, -1f, 5f, 5f, 0f); break;
                case HudIcon.Disc: r.Disc(32f, 32f, 31.2f); break;
                case HudIcon.Ring: r.Ring(32f, 32f, 30.8f, 1.9f); break;
                case HudIcon.Glow: r.Glow(32f, 32f, 32f, 2f); break;
                case HudIcon.PanelFill: r.Box(0.5f, 0.5f, 31.5f, 31.5f, 6f); break;
                case HudIcon.PanelFrame: r.BoxOutline(1.25f, 1.25f, 30.75f, 30.75f, 5f, 2f); break;
                case HudIcon.Segment: r.Polygon(0.6f, 8f, 8f, 0.6f, 56f, 0.6f, 63.4f, 8f, 56f, 15.4f, 8f, 15.4f); break;

                case HudIcon.LongGun: LongGun(r); break;
                case HudIcon.Pistol: Pistol(r); break;
                case HudIcon.Blade: Blade(r); break;
                case HudIcon.Blunt: Blunt(r); break;
                case HudIcon.Axe: Axe(r); break;
                case HudIcon.Hammer: Hammer(r); break;
                case HudIcon.Fist: Fist(r); break;
                case HudIcon.Flashlight: Flashlight(r); break;
                case HudIcon.Goggles: Goggles(r); break;

                case HudIcon.Battery: Battery(r); break;
                case HudIcon.BatteryFill: BatteryFill(r); break;
                case HudIcon.Bottle: Bottle(r); break;
                case HudIcon.BottleFill: r.Box(24.5f, 12f, 39.5f, 38f, 1.5f); break;
                case HudIcon.Cutlery: Cutlery(r); break;
                case HudIcon.Thermometer: Thermometer(r); break;
                case HudIcon.ThermometerFill: r.Disc(32f, 15f, 5f); r.Box(30f, 15f, 34f, 51f, 1.5f); break;
                case HudIcon.Cross: Cross(r); break;
                case HudIcon.CrossFill: CrossFill(r); break;

                case HudIcon.Magnifier: r.Ring(27f, 37f, 14f, 4.5f); r.Line(37.5f, 26.5f, 53f, 11f, 7f); break;
                case HudIcon.Bell: Bell(r); break;

                case HudIcon.Stand: Figure(r, 32f, 55f, 32f, 46f, 32f, 27f, 26f, 37f, 24.5f, 27f, 38f, 37f, 39.5f, 27f, 29f, 15.5f, 28f, 4f, 35f, 15.5f, 36f, 4f); break;
                case HudIcon.Walk1: Figure(r, 34f, 55f, 33f, 46f, 31f, 27f, 28f, 37f, 24f, 30f, 37.5f, 37f, 42f, 30f, 28f, 15.5f, 22f, 6f, 36f, 16.5f, 40f, 4.5f); break;
                case HudIcon.Walk2: Figure(r, 33.5f, 55.5f, 32.5f, 46.5f, 31f, 27.5f, 29f, 37f, 26.5f, 28.5f, 36f, 37f, 39f, 28.5f, 29f, 16f, 25.5f, 5f, 33.5f, 16f, 35.5f, 4f); break;
                case HudIcon.Run1: Figure(r, 39f, 54f, 36f, 45.5f, 29f, 28.5f, 28f, 38f, 21.5f, 42f, 42f, 39f, 47.5f, 45f, 25f, 17.5f, 14f, 13.5f, 39f, 24f, 36.5f, 12f); break;
                case HudIcon.Run2: Figure(r, 39f, 54.5f, 36f, 46f, 29.5f, 29f, 30f, 37.5f, 25.5f, 43f, 40.5f, 38.5f, 44f, 33f, 22f, 22.5f, 14f, 29f, 35.5f, 17f, 38f, 4.5f); break;
                case HudIcon.Sneak1: Figure(r, 44f, 41f, 38f, 35.5f, 25f, 25f, 35f, 27f, 39f, 21f, 43f, 29f, 48f, 25f, 21f, 13f, 15f, 4f, 35f, 15f, 33f, 4f); break;
                case HudIcon.Sneak2: Figure(r, 44f, 41.5f, 38f, 36f, 25f, 25.5f, 36f, 27.5f, 41f, 22f, 42f, 29f, 46.5f, 24f, 23.5f, 13f, 20f, 4f, 32f, 14.5f, 29f, 4f); break;

                case HudIcon.Arrow: r.Polygon(32f, 60f, 52f, 8f, 32f, 20f, 12f, 8f); break;

                case HudIcon.Drop: Drop(r, 32f, 23f, 15f, 36f); r.Erase = true; r.Arc(32f, 23f, 9f, 3f, 195f, 250f); r.Erase = false; break;
                case HudIcon.Virus: Virus(r); break;
                case HudIcon.Scared: Scared(r); break;
                case HudIcon.Snowflake: Snowflake(r); break;
                case HudIcon.Sun: Sun(r); break;
                case HudIcon.Nausea: Nausea(r); break;
                case HudIcon.Moon: Moon(r); break;
                case HudIcon.Burst: r.Star(32f, 32f, 8, 28f, 13f, 90f); break;
                case HudIcon.Droplets: Drop(r, 19f, 34f, 7.5f, 17f); Drop(r, 45f, 34f, 7.5f, 17f); Drop(r, 32f, 10f, 7.5f, 17f); break;
                case HudIcon.Weight: r.Disc(32f, 24f, 18f); r.Arc(32f, 41f, 11f, 6f, -25f, 205f); break;
                case HudIcon.Gas: GasCloud(r); break;
            }
        }

        #region Hand slots
        private static void LongGun(HudRaster r)
        {
            r.SetTransform(30f);
            r.Path(3f, true, 3f, 22f, 3f, 34f, 20f, 35f, 34f, 36.5f, 61f, 36.5f, 61f, 33.5f, 50f, 33.5f, 50f, 30f,
                37f, 30f, 37f, 32.5f, 29f, 32.5f, 22f, 28.5f);
            r.Arc(31f, 32.5f, 3.6f, 2.5f, 180f, 360f);
            r.ResetTransform();
        }

        private static void Pistol(HudRaster r)
        {
            r.SetTransform(8f);
            r.BoxOutline(9f, 37f, 56f, 47f, 2f, 3.2f);                                     // slide
            r.Path(3.2f, true, 11f, 37f, 25f, 37f, 22f, 24f, 20f, 11f, 8f, 11f, 9f, 24f);  // grip
            r.Arc(29f, 33f, 5f, 3f, 180f, 360f);                                           // trigger guard
            r.Line(34f, 33f, 34f, 37f, 3f);
            r.Line(28.5f, 37f, 27.5f, 32f, 2.6f);                                          // trigger
            r.ResetTransform();
        }

        private static void Blade(HudRaster r)
        {
            r.SetTransform(40f);
            r.BoxOutline(5f, 28.5f, 23f, 35.5f, 3f, 3.2f);                                 // handle
            r.Line(24.5f, 25f, 24.5f, 39f, 3.6f);                                          // guard
            r.Path(3.2f, true, 27f, 29.5f, 47f, 29.5f, 55f, 31.5f, 61f, 36f, 27f, 36f);    // blade
            r.ResetTransform();
        }

        private static void Blunt(HudRaster r)
        {
            r.SetTransform(40f);
            r.Path(3.2f, true, 8f, 30.5f, 26f, 30f, 46f, 27f, 57f, 27f, 61f, 30f, 61f, 34f, 57f, 37f, 46f, 37f, 26f, 34f, 8f, 33.5f);
            r.Disc(5.5f, 32f, 3.8f);                                                       // knob
            r.ResetTransform();
        }

        private static void Axe(HudRaster r)
        {
            r.SetTransform(-38f);
            r.BoxOutline(30f, 3f, 35f, 57f, 2.5f, 3.2f);                                   // haft
            r.Path(3.2f, true, 30f, 54f, 19f, 55.5f, 12f, 61f, 9f, 55f, 8f, 48.5f, 9f, 42f, 12f, 36f, 19f, 42f, 30f, 44.5f);  // bit
            r.Box(35f, 45f, 40.5f, 54f, 1f);                                               // poll
            r.ResetTransform();
        }

        private static void Hammer(HudRaster r)
        {
            r.SetTransform(-40f);
            r.BoxOutline(28.5f, 5f, 35.5f, 42f, 2.5f, 3.4f);                               // handle
            r.Path(3.4f, true, 20f, 42f, 46f, 42f, 46f, 53f, 20f, 53f);                    // head
            r.Path(3.4f, false, 20f, 53f, 13f, 52f, 8f, 47f);                              // claw
            r.Path(3.4f, false, 20f, 45f, 14f, 45f, 9f, 41f);
            r.ResetTransform();
        }

        private static void Fist(HudRaster r)
        {
            for (int i = 0; i < 4; i++)
                r.BoxOutline(13f + i * 9.5f, 33f, 22.5f + i * 9.5f, 50f, 4.6f, 3.2f);      // knuckles
            r.Path(3.2f, false, 13f, 40f, 12f, 27f, 16f, 15f, 26f, 10f, 44f, 10f, 51f, 17f, 51f, 40f);  // back of the hand
            r.Path(3.2f, false, 12.5f, 30f, 22f, 31.5f, 31f, 30f, 36f, 25.5f);             // thumb
        }

        private static void Flashlight(HudRaster r)
        {
            r.SetTransform(35f);
            r.BoxOutline(6f, 27f, 36f, 37f, 2.5f, 3.2f);                                   // barrel
            r.Path(3.2f, true, 36f, 26f, 36f, 38f, 50f, 43f, 50f, 21f);                    // head
            r.Line(54.5f, 23f, 54.5f, 41f, 3.2f);                                          // lens
            r.Box(16f, 37f, 23f, 40.5f, 1f);                                               // switch
            r.ResetTransform();
        }

        private static void Goggles(HudRaster r)
        {
            r.Ring(20f, 29f, 11f, 3.6f);
            r.Ring(44f, 29f, 11f, 3.6f);
            r.Disc(20f, 29f, 4.5f);
            r.Disc(44f, 29f, 4.5f);
            r.Line(30f, 34f, 34f, 34f, 3.6f);                                              // bridge
            r.BoxOutline(24f, 41f, 40f, 51f, 3f, 3.6f);                                    // mount
            r.Line(3f, 31f, 8.5f, 31f, 3.2f);                                              // strap
            r.Line(55.5f, 31f, 61f, 31f, 3.2f);
        }
        #endregion

        #region Status row
        private static readonly float[] BoltShape = { 34f, 41f, 24f, 30f, 30.5f, 30f, 27f, 22f, 38f, 33.5f, 31.5f, 33.5f };

        private static void Battery(HudRaster r)
        {
            r.BoxOutline(5f, 18f, 53f, 46f, 5f, 4f);
            r.Box(53f, 25f, 60f, 39f, 2f);                                                 // terminal
            r.Polygon(BoltShape);
        }

        private static void BatteryFill(HudRaster r)
        {
            r.Box(9f, 22f, 49f, 42f, 2f);
            r.Erase = true;                                                                // keep the bolt readable over the charge
            r.Polygon(BoltShape);
            r.Path(4f, true, BoltShape);
            r.Erase = false;
        }

        private static void Bottle(HudRaster r)
        {
            r.Box(26f, 52f, 38f, 59f, 1.5f);                                               // cap
            r.Path(4f, true, 27f, 50f, 27f, 46f, 21f, 40f, 21f, 9f, 43f, 9f, 43f, 40f, 37f, 46f, 37f, 50f);
        }

        private static void Cutlery(HudRaster r)
        {
            r.Line(15f, 57f, 15f, 43f, 3.5f);                                              // fork tines
            r.Line(22f, 57f, 22f, 40f, 3.5f);
            r.Line(29f, 57f, 29f, 43f, 3.5f);
            r.Path(3.5f, false, 15f, 43f, 16.5f, 38.5f, 22f, 36f, 27.5f, 38.5f, 29f, 43f);
            r.Line(22f, 36f, 22f, 7f, 4.5f);                                               // fork handle
            r.EllipseRing(43f, 47f, 7f, 10f, 4f);                                          // spoon bowl
            r.Line(43f, 37f, 43f, 7f, 4.5f);
        }

        private static void Thermometer(HudRaster r)
        {
            r.Arc(32f, 15f, 9f, 4f, 120f, 420f);                                           // bulb, open at the top
            r.Line(27.5f, 22.8f, 27.5f, 52f, 4f);                                          // tube
            r.Line(36.5f, 22.8f, 36.5f, 52f, 4f);
            r.Arc(32f, 52f, 4.5f, 4f, 0f, 180f);
            r.Line(42f, 30f, 46f, 30f, 3f);                                                // ticks
            r.Line(42f, 38f, 46f, 38f, 3f);
            r.Line(42f, 46f, 46f, 46f, 3f);
        }

        private static void Cross(HudRaster r)
        {
            r.Path(4f, true, 24f, 7f, 40f, 7f, 40f, 24f, 57f, 24f, 57f, 40f, 40f, 40f, 40f, 57f, 24f, 57f, 24f, 40f, 7f, 40f, 7f, 24f, 24f, 24f);
        }

        private static void CrossFill(HudRaster r)
        {
            r.Polygon(27.5f, 10.5f, 36.5f, 10.5f, 36.5f, 27.5f, 53.5f, 27.5f, 53.5f, 36.5f, 36.5f, 36.5f, 36.5f, 53.5f,
                27.5f, 53.5f, 27.5f, 36.5f, 10.5f, 36.5f, 10.5f, 27.5f, 27.5f, 27.5f);
        }
        #endregion

        #region Watch row
        private static void Bell(HudRaster r)
        {
            r.Path(4f, true, 13f, 19f, 18f, 24f, 20f, 33f, 22f, 41f, 26f, 47f, 32f, 49.5f, 38f, 47f, 42f, 41f, 44f, 33f, 46f, 24f, 51f, 19f);
            r.Disc(32f, 13f, 4.5f);                                                        // clapper
            r.Ring(32f, 53.5f, 3f, 3f);                                                    // loop
        }
        #endregion

        #region Movement
        // A pictogram figure from its joints: head, neck, hip, then elbow/hand x2 and knee/foot x2.
        private static void Figure(HudRaster r, params float[] j)
        {
            r.Disc(j[0], j[1] + 0.5f, 6.5f);
            r.Line(j[2], j[3], j[4], j[5], 7.5f);                                          // torso
            r.Path(6f, false, j[2], j[3] - 1f, j[6], j[7], j[8], j[9]);                    // far arm
            r.Path(6f, false, j[2], j[3] - 1f, j[10], j[11], j[12], j[13]);                // near arm
            r.Path(7f, false, j[4], j[5], j[14], j[15], j[16], j[17]);                     // far leg
            r.Path(7f, false, j[4], j[5], j[18], j[19], j[20], j[21]);                     // near leg
        }
        #endregion

        #region Moodles
        // A filled tear drop: a disc with its tangent lines meeting at a point above it.
        private static void Drop(HudRaster r, float cx, float cy, float radius, float apexHeight)
        {
            r.Disc(cx, cy, radius);
            float c = radius / apexHeight;                       // cos of the tangent angle from vertical
            float s = (float)System.Math.Sqrt(1f - c * c);
            r.Polygon(cx, cy + apexHeight, cx - radius * s, cy + radius * c, cx + radius * s, cy + radius * c);
        }

        private static void Virus(HudRaster r)
        {
            r.Disc(32f, 32f, 11f);
            for (int i = 0; i < 8; i++)
            {
                double a = (i * 45.0 + 22.5) * System.Math.PI / 180.0;
                float cx = (float)System.Math.Cos(a), sy = (float)System.Math.Sin(a);
                r.Line(32f + cx * 10f, 32f + sy * 10f, 32f + cx * 21f, 32f + sy * 21f, 4f);
                r.Disc(32f + cx * 23f, 32f + sy * 23f, 4.2f);
            }
            r.Erase = true;
            r.Disc(28f, 35f, 2.6f);
            r.Disc(36.5f, 29f, 2.2f);
            r.Erase = false;
        }

        private static void Scared(HudRaster r)
        {
            r.Ring(32f, 32f, 24f, 4.5f);
            r.Ring(24f, 37f, 4.6f, 2.8f);
            r.Ring(40f, 37f, 4.6f, 2.8f);
            r.Disc(24f, 37f, 1.8f);
            r.Disc(40f, 37f, 1.8f);
            r.EllipseRing(32f, 20.5f, 4.8f, 6.2f, 3.4f);                                   // gasping mouth
            r.Line(17.5f, 46f, 27f, 49.5f, 3.4f);                                          // raised brows
            r.Line(37f, 49.5f, 46.5f, 46f, 3.4f);
        }

        private static void Snowflake(HudRaster r)
        {
            for (int i = 0; i < 6; i++)
            {
                double a = (90.0 + i * 60.0) * System.Math.PI / 180.0;
                float dx = (float)System.Math.Cos(a), dy = (float)System.Math.Sin(a);
                r.Line(32f, 32f, 32f + dx * 26f, 32f + dy * 26f, 4.5f);
                float bx = 32f + dx * 15f, by = 32f + dy * 15f;
                for (int side = -1; side <= 1; side += 2)
                {
                    double b = a + side * 45.0 * System.Math.PI / 180.0;
                    r.Line(bx, by, bx + (float)System.Math.Cos(b) * 8.5f, by + (float)System.Math.Sin(b) * 8.5f, 3.8f);
                }
            }
        }

        private static void Sun(HudRaster r)
        {
            r.Disc(32f, 32f, 11.5f);
            for (int i = 0; i < 8; i++)
            {
                double a = i * 45.0 * System.Math.PI / 180.0;
                float dx = (float)System.Math.Cos(a), dy = (float)System.Math.Sin(a);
                r.Line(32f + dx * 17f, 32f + dy * 17f, 32f + dx * 27f, 32f + dy * 27f, 4.5f);
            }
        }

        private static void Nausea(HudRaster r)
        {
            r.Ring(32f, 32f, 24f, 4.5f);
            r.Path(3.4f, false, 18.5f, 37f, 23f, 40.5f, 27.5f, 37f);                       // screwed-up eyes
            r.Path(3.4f, false, 36.5f, 37f, 41f, 40.5f, 45.5f, 37f);
            r.Path(3.4f, false, 18.5f, 22f, 22.5f, 26f, 27f, 22f, 32f, 26f, 37f, 22f, 41.5f, 26f, 45.5f, 22f);  // queasy mouth
        }

        private static void Moon(HudRaster r)
        {
            r.Disc(27f, 29f, 21f);
            r.Erase = true;
            r.Disc(38f, 37f, 18f);
            r.Erase = false;
            r.Path(4f, false, 41f, 57f, 53f, 57f, 41f, 45f, 53f, 45f);                     // z
        }

        // A billowing cloud with fumes curling beneath it.
        private static void GasCloud(HudRaster r)
        {
            r.Disc(21f, 37f, 10f);
            r.Disc(33f, 43f, 13f);
            r.Disc(45f, 37f, 10f);
            r.Box(12f, 27f, 54f, 39f, 6f);
            r.Path(3.6f, false, 13f, 16f, 19f, 19.5f, 25f, 16f, 31f, 19.5f, 37f, 16f, 43f, 19.5f, 49f, 16f);
            r.Path(3.6f, false, 19f, 8f, 25f, 11.5f, 31f, 8f, 37f, 11.5f, 43f, 8f);
        }
        #endregion
    }
}
