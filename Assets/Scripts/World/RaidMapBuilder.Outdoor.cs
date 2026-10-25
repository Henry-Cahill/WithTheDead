using UnityEngine;

namespace CloneZ
{
    // The street furniture: cobra-head street lights (a few still burning), the STOP signs, the bus stop by the park,
    // hydrants and mail drop boxes on the sidewalks, the cars left in the street when the town was evacuated, and the
    // trees along Main St and in the corners by the fence. Plus the fences every lot and the map's edge are built from.
    public static partial class RaidMapBuilder
    {
        private enum FenceKind { Picket, Privacy, ChainLink }

        private sealed partial class Builder
        {
            public void StreetFurniture()
            {
                StreetLights();
                StopSigns();
                BusStop();
                Hydrants();
                MailDrops();
                Evacuation();
                Trees();
            }

            /// <summary>A spot on a sidewalk <paramref name="across"/> from the centre line at <paramref name="along"/>, or the
            /// nearest within 7.5 m of it that keeps <paramref name="clearance"/> from the crossings, the dropped kerbs and
            /// everything already placed.</summary>
            private bool OnSidewalk(Street s, int side, float along, float across, float clearance, out Vector3 at)
            {
                for (int k = 0; k < 11; k++)
                {
                    float t = along + (k + 1) / 2 * 1.5f * (k % 2 == 0 ? -1f : 1f);
                    if (t < s.From + 1f || t > s.To - 1f || NearCrossing(s, side, t, clearance) || NearDroppedKerb(s, side, t, clearance)) continue;
                    at = s.At(t, side * across);
                    if (Free(Around(at, clearance, clearance))) return true;
                }
                at = default;
                return false;
            }

            private static bool NearCrossing(Street s, int side, float t, float clearance)
            {
                foreach (Street o in AllStreets)
                    if (s.CrossedBy(o, side * s.HalfWidth, side * s.Outer) && Mathf.Abs(t - o.Coord) < o.Outer + clearance + 0.5f) return true;
                return false;
            }

            private bool NearDroppedKerb(Street s, int side, float t, float clearance)
            {
                foreach (var g in _kerbGaps)
                    if (g.street == s && g.side == side && t > g.a - clearance - 0.3f && t < g.b + clearance + 0.3f) return true;
                return false;
            }

            // Cobra-head lights every 20-odd metres along the arterials and a couple down each residential street, their arms
            // out over the road. Most went dark with the power; a few still burn sodium-orange.
            private void StreetLights()
            {
                var spots = new (Street s, int side, float along)[]
                {
                    (MainSt, +1, -50f), (MainSt, +1, -18f), (MainSt, +1, 30f), (MainSt, +1, 50f),
                    (MainSt, -1, -54f), (MainSt, -1, -26f), (MainSt, -1, 15f), (MainSt, -1, 52f),
                    (DepotRd, +1, -46f), (DepotRd, +1, -14f), (DepotRd, +1, 26f), (DepotRd, +1, 48f),
                    (DepotRd, -1, -55f), (DepotRd, -1, -30f), (DepotRd, -1, 34f), (DepotRd, -1, 54f),
                    (ElmSt, +1, -46f), (ElmSt, -1, -21f), (OakSt, +1, -45f), (OakSt, -1, -29f),
                };
                int lit = 0;
                foreach (var (s, side, along) in spots)
                {
                    if (!OnSidewalk(s, side, along, s.Outer - 0.45f, 0.35f, out Vector3 p))
                    {
                        _town.Warn("no room for a street light on " + s.Name + " at " + along);
                        continue;
                    }
                    bool on = Chance(_rng, 0.4);
                    float yaw = s.FacingRoad(side);
                    RaidMesh m = _town.Out.Solid(p);
                    m.SetFrame(p, yaw);
                    Vector3 lamp = m.ToMeshSpace(DecorProps.StreetLight(m, _rng, on, 0f));
                    m.ResetFrame();
                    _town.PropCollider(p + Vector3.up * 2.75f, new Vector3(0.3f, 5.5f, 0.3f), yaw);
                    Reserve(Around(p, 0.6f, 0.6f));
                    if (on) _town.Light(_town.World, lamp, new Color(1f, 0.72f, 0.42f), 13f, 2.2f, false, lit++ % 2 == 0);
                }
            }

            // An all-way stop at the crossroads, a sign on each corner facing the lane coming in, and a stop where each
            // residential street meets Main St.
            private void StopSigns()
            {
                float k = MainSt.HalfWidth + 0.6f, o = DepotRd.Outer + 0.4f;
                StopSign(new Vector3(k, 0f, -o), 180f);
                StopSign(new Vector3(-k, 0f, o), 0f);
                StopSign(new Vector3(o, 0f, k), 90f);
                StopSign(new Vector3(-o, 0f, -k), 270f);
                foreach (Street lane in new[] { ElmSt, OakSt })
                    StopSign(new Vector3(-MainSt.Outer - 0.4f, 0f, lane.Coord - lane.HalfWidth - 0.6f), 270f);
            }

            private void StopSign(Vector3 at, float yaw) => Prop(_town, at, yaw, new Vector3(0.3f, 2.4f, 0.3f), true, (m, r) => DecorProps.StopSign(m));

            // The bus stop on Main St by the park: a bench facing the road and the stop's sign.
            private void BusStop()
            {
                float across = -(MainSt.Outer - 0.55f);
                Prop(_town, MainSt.At(13f, across), 90f, new Vector3(1.8f, 0.9f, 0.6f), true, (m, r) => DecorProps.Bench(m));
                Prop(_town, MainSt.At(15.4f, -(MainSt.HalfWidth + 0.5f)), 90f, new Vector3(0.2f, 2.5f, 0.2f), true, (m, r) =>
                {
                    m.Box(new Vector3(0f, 1.2f, 0f), new Vector3(0.06f, 2.4f, 0.06f), DecorPalette.Metal(new Color(0.55f, 0.56f, 0.57f)));
                    Surf plate = DecorMaterials.Sign("BUS STOP", new Color(0.14f, 0.30f, 0.56f), new Color(0.92f, 0.92f, 0.90f), out float aspect);
                    float w = 0.22f * aspect;
                    m.Box(new Vector3(0f, 2.15f, 0.035f), new Vector3(w + 0.04f, 0.26f, 0.02f), DecorPalette.Matte(new Color(0.2f, 0.2f, 0.2f)));
                    m.Upright(new Vector3(0f, 2.15f, 0.047f), Vector3.forward, w, 0.22f, plate, new Rect(0f, 0f, 1f, 1f));
                });
            }

            // A hydrant near each block's corner, by the kerb.
            private void Hydrants()
            {
                var spots = new (Street s, int side, float along)[]
                {
                    (MainSt, +1, 10f), (MainSt, -1, -10f), (MainSt, +1, -46f), (DepotRd, -1, 11f), (DepotRd, +1, -31f),
                    (ElmSt, +1, -27f), (OakSt, -1, -44f),
                };
                foreach (var (s, side, along) in spots)
                    if (OnSidewalk(s, side, along, s.HalfWidth + 0.6f, 0.4f, out Vector3 p))
                    {
                        HydrantProp(_town, p, R(_rng, 0f, 360f));
                        Reserve(Around(p, 0.5f, 0.5f));
                    }
            }

            // The blue mail drop boxes: by the county office on Main St and by the clinic on Depot Rd.
            private void MailDrops()
            {
                var spots = new (Street s, int side, float along)[] { (MainSt, +1, 27f), (DepotRd, -1, -30f) };
                foreach (var (s, side, along) in spots)
                    if (OnSidewalk(s, side, along, s.Outer - 0.55f, 0.5f, out Vector3 p)) MailDropProp(_town, p, s.FacingRoad(side));
            }

            // The cars that didn't get out: queued in Main St's northbound lane short of the road block, and one left in
            // Depot Rd heading for the dock.
            private void Evacuation()
            {
                ParkedCar(_town, new Vector3(MainSt.HalfWidth * 0.5f, 0f, 44f), R(_rng, -1.5f, 1.5f), DecorProps.CarKind.Wagon, Paint(_rng));
                ParkedCar(_town, new Vector3(MainSt.HalfWidth * 0.5f, 0f, 50.5f), R(_rng, -1.5f, 1.5f), FamilyCar(_rng), Paint(_rng));
                ParkedCar(_town, new Vector3(-44f, 0f, DepotRd.HalfWidth * 0.5f), 270f + R(_rng, -1.5f, 1.5f), FamilyCar(_rng), Paint(_rng));
            }

            // Bare maples on the verge between the office and the food mart, and stands of pines in the corners inside the
            // fence (wherever nothing else stands).
            private void Trees()
            {
                Vector2[] maples = { new Vector2(8.8f, 28.5f), new Vector2(8.8f, 34f) };
                foreach (Vector2 t in maples)
                {
                    var p = new Vector3(t.x, 0f, t.y);
                    if (Free(Around(p, 0.8f, 0.8f))) Tree(p, false);
                }
                Vector2[] stands =
                {
                    new Vector2(44f, 63f), new Vector2(50.5f, 65.5f), new Vector2(57f, 62.5f), new Vector2(62f, 66f),
                    new Vector2(-66.5f, 13f), new Vector2(-66f, 24f), new Vector2(-66.5f, 51f), new Vector2(-65.5f, 62f),
                    new Vector2(-66.5f, -14f), new Vector2(-66f, -26f), new Vector2(-66.5f, -50f), new Vector2(-65.5f, -62f),
                    new Vector2(40f, -50f), new Vector2(47f, -56f), new Vector2(55f, -50.5f), new Vector2(61f, -58f),
                    new Vector2(33f, -60f), new Vector2(22f, -55f), new Vector2(52f, -64f), new Vector2(42f, -65f),
                };
                foreach (Vector2 t in stands)
                {
                    var p = new Vector3(t.x + R(_rng, -1.2f, 1.2f), 0f, t.y + R(_rng, -1.2f, 1.2f));
                    if (Free(Around(p, 1.2f, 1.2f))) Tree(p, Chance(_rng, 0.75), true);
                }
            }

            /// <summary>A straight fence on the ground from <paramref name="a"/> to <paramref name="b"/>, in pieces of at most
            /// 8 m (each in its own outdoor chunk, with its own collider); its back (the chain-link's banked snow) is on the
            /// right going from a to b.</summary>
            private void Fence(Vector3 a, Vector3 b, FenceKind kind, Color paint)
            {
                Vector3 d = b - a;
                d.y = 0f;
                float len = d.magnitude;
                if (len < 0.3f) return;
                Vector3 dir = d / len;
                float yaw = Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg;
                float h = kind == FenceKind.Picket ? 1.0f : kind == FenceKind.Privacy ? 1.8f : 2.2f;
                int pieces = Mathf.Max(1, Mathf.CeilToInt(len / 8f));
                float step = len / pieces;
                for (int i = 0; i < pieces; i++)
                {
                    Vector3 c = a + dir * (step * (i + 0.5f));
                    RaidMesh m = _town.Out.Solid(c);
                    m.SetFrame(c, yaw);
                    if (kind == FenceKind.Picket) DecorProps.PicketFence(m, step, paint);
                    else if (kind == FenceKind.Privacy) DecorProps.PrivacyFence(m, step, paint);
                    else DecorProps.ChainLinkFence(m, step, h);
                    m.ResetFrame();
                    _town.PropCollider(c + Vector3.up * (h * 0.5f), new Vector3(step, h, 0.25f), yaw);
                }
                Reserve(Grow(Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z)), 0.4f));
            }
        }
    }
}
