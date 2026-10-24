#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CloneZ
{
    // The cutaway harness's pictures: the spots the survivor stand-in is put on, and the walk in through a house's door and
    // out again.
    public static partial class CutawayShots
    {
        private sealed partial class Harness
        {
            private void Pictures()
            {
                MapCutaway.Building house = PickHouse(), block = PickBlock(), church = PickChurch(block);
                _md.Append("## Pictures\n\nEach picture is the game's isometric camera at its widest zoom (").Append(Wide)
                    .Append(" m half-height) unless noted, in plain overcast daylight; the red capsule stands in for the survivor. ")
                    .Append("\"roofs on\" = what the view showed before the cutaway; \"cut\" = after one scan. The heading is the camera's ")
                    .Append("(the game's starts at ").Append(_isoYaw).Append("° and turns a quarter at a time). The last column says what the old ")
                    .Append("roof test (each building's footprint up to its highest point) would have cut instead, where that differs.\n\n")
                    .Append("| file | shows | cut away | old test |\n|---|---|---|---|\n");
                if (block != null)
                    Shot("01_downtown_no_survivor.png", "Downtown with no survivor: every roof and upper storey shows", Centre(block), _isoYaw, Wide, false, false, false);
                if (house != null)
                {
                    Vector3 inside = Inside(house);
                    string what = Describe(house);
                    Shot("02_house_roofs_on.png", "Inside " + what + ", roofs on (before)", inside, _isoYaw, Wide, true, false, false);
                    for (int k = 0; k < 4; k++)
                    {
                        float yaw = Heading(k);
                        Shot("0" + (3 + k) + "_house_cut_" + Deg(yaw) + ".png", "Inside " + what + ", heading " + Deg(yaw) + "°", inside, yaw, Wide, true, true, false);
                    }
                    Shot("07_house_cut_close.png", "The same, closest zoom", inside, _isoYaw, Close, true, true, false);
                    Shot("08_house_cut_night.png", "The same by night (the map's lights on round the survivor)", inside, _isoYaw, Wide, true, true, true);
                    Shot("09_house_both_cutaways.png", "The same with the game's other cutaway too (IsoCutaway: the interiors' partitions and props in the way)",
                        inside, _isoYaw, Wide, true, true, false, true);
                }
                else _md.Append("| | no house with a shell of its own found | | |\n");
                if (block != null)
                {
                    string what = Describe(block);
                    Vector3? first = BehindOf(block, _isoYaw);
                    if (first.HasValue)
                        Shot("10_block_roofs_on.png", "Behind " + what + " as the camera sees it, roofs on (before)", first.Value, _isoYaw, Wide, true, false, false);
                    for (int k = 0; k < 4; k++)
                    {
                        float yaw = Heading(k);
                        Vector3? behind = BehindOf(block, yaw);
                        if (behind.HasValue)
                            Shot((11 + k) + "_block_behind_" + Deg(yaw) + ".png", "Behind " + what + ", heading " + Deg(yaw) + "°", behind.Value, yaw, Wide, true, true, false);
                        else _md.Append("| | no free spot behind ").Append(what).Append(" at heading ").Append(Deg(yaw)).Append("° | | |\n");
                    }
                    Shot("15_block_inside.png", "Inside " + what, Inside(block), _isoYaw, Wide, true, true, false);
                }
                else _md.Append("| | no building with an upper storey found downtown | | |\n");
                if (church != null)
                {
                    string what = Describe(church);
                    Vector3? behind = BehindOf(church, _isoYaw);
                    if (behind.HasValue) Shot("16_church_behind.png", "Behind " + what, behind.Value, _isoYaw, Wide, true, true, false);
                    Shot("17_church_inside.png", "Inside " + what, Inside(church), _isoYaw, Wide, true, true, false);
                }
                MapCutaway.RestoreAll();
                _md.Append('\n');
            }

            private float Heading(int k) => Mathf.Repeat(_isoYaw + 90f * k, 360f);

            private static string Deg(float yaw) => Mathf.RoundToInt(Mathf.Repeat(yaw, 360f)).ToString("000");

            // One picture at `at`: the cutaway run first (when `cut`), with the old roof test too to compare its list.
            private void Shot(string file, string title, Vector3 at, float yaw, float size, bool survivor, bool cut, bool night, bool iso = false)
            {
                MapCutaway.RestoreAll();
                string cutList = "", old = "";
                if (cut)
                {
                    MapCutaway.UseHeights = false;
                    MapCutaway.Run(at, yaw, _isoPitch, Reach(size), true, _t += 10f, true);
                    old = CutList();
                    MapCutaway.RestoreAll();
                    MapCutaway.UseHeights = true;
                    MapCutaway.Run(at, yaw, _isoPitch, Reach(size), true, _t += 10f, true);
                    cutList = CutList();
                }
                try
                {
                    Picture(file, at, yaw, size, survivor, night, iso);
                    if (iso) title += "; the ray cutaway hid " + _isoHidden + " pieces";
                    _md.Append("| `").Append(file).Append("` | ").Append(title).Append(" | ").Append(cutList).Append(" | ")
                        .Append(old == cutList ? "" : old).Append(" |\n");
                }
                catch (System.Exception e)
                {
                    _failures++;
                    _md.Append("| `").Append(file).Append("` | **failed**: ").Append(e.Message).Append(" | | |\n");
                    Debug.LogError("[CutawayShots] picture " + file + " failed: " + e);
                }
            }

            private static string CutList()
            {
                var sb = new StringBuilder();
                IReadOnlyList<MapCutaway.Building> cut = MapCutaway.Cut;
                for (int i = 0; i < cut.Count; i++)
                {
                    if (cut[i].Applied == 0) continue;
                    if (sb.Length > 0) sb.Append("; ");
                    sb.Append(cut[i].Kind).Append(' ').Append(cut[i].Id).Append(": ").Append(Parts(cut[i].Applied));
                }
                return sb.Length > 0 ? sb.ToString() : "nothing";
            }

            private static string Parts(int mask)
            {
                var sb = new StringBuilder();
                if ((mask & 16) != 0) sb.Append("upper");
                if ((mask & 15) == 0) return sb.ToString();
                sb.Append(sb.Length > 0 ? " + walls " : "walls ");
                const string names = "NESW";
                for (int g = 0; g < 4; g++)
                    if ((mask & (1 << g)) != 0) sb.Append(names[g]);
                return sb.ToString();
            }

            private static string Describe(MapCutaway.Building b)
                => b.Kind + " " + b.Id + " (" + F0(Area(b.Local)) + " m², walls " + F1(b.WallTop) + " m, top " + F1(b.Top) + " m)";

            #region Picking spots
            // The house nearest the middle of the square that's big enough to read and has a working door to walk in by.
            private MapCutaway.Building PickHouse()
            {
                MapCutaway.Building best = null, anyBest = null;
                float bestD = float.MaxValue, anyD = float.MaxValue;
                foreach (MapCutaway.Building b in _buildings)
                {
                    if (b.Walls == null || b.Upper == null || b.Kind != "house") continue;
                    float area = Area(b.Local);
                    if (area < 70f || area > 400f) continue;
                    float d = b.Middle.sqrMagnitude;
                    if (d < anyD) { anyD = d; anyBest = b; }
                    if (d < bestD && FindDoor(b, out _, out _)) { bestD = d; best = b; }
                }
                return best ?? anyBest;
            }

            // A block downtown with an upper storey (not a home, not a church): the one nearest the middle.
            private MapCutaway.Building PickBlock()
            {
                MapCutaway.Building best = null;
                float bestD = float.MaxValue;
                foreach (MapCutaway.Building b in _buildings)
                {
                    if (b.Walls == null || b.Upper == null || b.Kind == "house" || b.Kind == "trailer" || b.Kind == "church") continue;
                    if (b.Top < 6f || b.Top - b.WallTop < 2.8f || Area(b.Local) < 80f || b.Middle.magnitude > 160f) continue;
                    float d = b.Middle.sqrMagnitude;
                    if (d < bestD) { bestD = d; best = b; }
                }
                return best;
            }

            // The tallest church (a steeple), else the tallest building other than the block.
            private MapCutaway.Building PickChurch(MapCutaway.Building block)
            {
                MapCutaway.Building best = null;
                float bestTop = float.MinValue;
                foreach (MapCutaway.Building b in _buildings)
                {
                    if (b.Walls == null || b.Upper == null || b == block || b.Middle.magnitude > 190f) continue;
                    float score = (b.Kind == "church" ? 1000f : 0f) + b.Top;
                    if (score > bestTop) { bestTop = score; best = b; }
                }
                return best;
            }

            private static Vector3 Centre(MapCutaway.Building b) => new Vector3(Mid(b.Pts).x, 0f, Mid(b.Pts).y);

            // A point well inside the footprint: its middle if that's inside, else the inside point farthest from the walls
            // on a half-metre grid.
            private static Vector3 Inside(MapCutaway.Building b)
            {
                Vector2[] p = b.Pts;
                Vector2 mid = Mid(p);
                Vector2 best = mid;
                float bestD = Contains(p, mid) ? EdgeDistance(p, mid) : -1f;
                if (bestD < 1.2f)
                {
                    Rect r = BoundsOf(p);
                    for (float x = r.xMin; x <= r.xMax; x += 0.5f)
                        for (float z = r.yMin; z <= r.yMax; z += 0.5f)
                        {
                            var q = new Vector2(x, z);
                            if (!Contains(p, q)) continue;
                            float d = EdgeDistance(p, q);
                            if (d > bestD) { bestD = d; best = q; }
                        }
                }
                return new Vector3(best.x, 0f, best.y);
            }

            // Just past the building's far side as the camera looks (so it stands between them), on open ground.
            private Vector3? BehindOf(MapCutaway.Building b, float yaw)
            {
                var look = new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));
                var side = new Vector2(look.y, -look.x);
                Vector2 mid = Mid(b.Pts);
                foreach (float lateral in new[] { 0f, 2f, -2f, 4f, -4f, 6f, -6f })
                {
                    Vector2 q = mid + side * lateral;
                    if (!Contains(b.Pts, q)) continue;
                    float t = 0f;
                    while (t < 80f && Contains(b.Pts, q + look * t)) t += 0.2f;
                    for (float extra = 1.1f; extra < 3.5f; extra += 0.3f)
                    {
                        Vector2 p = q + look * (t + extra);
                        if (!InAnyBuilding(p)) return new Vector3(p.x, 0f, p.y);
                    }
                }
                return null;
            }

            private bool InAnyBuilding(Vector2 p)
            {
                foreach (MapCutaway.Building b in _buildings)
                    if (b.Pts != null && BoundsOf(b.Pts).Contains(p) && Contains(b.Pts, p)) return true;
                return false;
            }

            // A working door in the building's outside walls: where it is in plan and which way is out.
            private bool FindDoor(MapCutaway.Building b, out Vector2 at, out Vector2 outward)
            {
                at = outward = Vector2.zero;
                float best = 0.45f;
                bool found = false;
                foreach (Interactable i in _doors)
                {
                    if (i == null) continue;
                    var q = new Vector2(i.InteractPoint.x, i.InteractPoint.z);
                    float d = EdgeDistance(b.Pts, q, out int edge);
                    if (d >= best) continue;
                    Vector2 e = b.Pts[(edge + 1) % b.Pts.Length] - b.Pts[edge];
                    if (e.sqrMagnitude < 1e-6f) continue;
                    best = d;
                    found = true;
                    at = q;
                    outward = new Vector2(e.y, -e.x).normalized;   // the footprint runs anticlockwise
                }
                return found;
            }
            #endregion

            #region Walking in and out
            // The stand-in walks in through the house's door at a walking pace (from 4 m out on the camera's side, the camera
            // turned to face the door), waits, and walks back out, a cutaway frame every tenth of a second: when is the house
            // cut, when does it come back, and what does it look like on the way.
            private void Walk()
            {
                MapCutaway.Building house = PickHouse();
                if (house == null)
                {
                    _md.Append("## Walking in and out\n\nNo house to walk into.\n\n");
                    return;
                }
                Vector3 mid3 = Inside(house);
                var mid = new Vector2(mid3.x, mid3.z);
                float yaw = _isoYaw;
                Vector2 entry, outward;
                bool door = FindDoor(house, out entry, out outward);
                if (door)
                {
                    // The heading (of the game's four) that looks most squarely at the door's wall.
                    float bestDot = float.MaxValue;
                    for (int k = 0; k < 4; k++)
                    {
                        float h = Heading(k) * Mathf.Deg2Rad;
                        float dot = Vector2.Dot(new Vector2(Mathf.Sin(h), Mathf.Cos(h)), outward);
                        if (dot < bestDot) { bestDot = dot; yaw = Heading(k); }
                    }
                }
                else
                {
                    outward = new Vector2(-Mathf.Sin(yaw * Mathf.Deg2Rad), -Mathf.Cos(yaw * Mathf.Deg2Rad));
                    float t = 0f;
                    while (t < 60f && Contains(house.Pts, mid + outward * t)) t += 0.1f;
                    entry = mid + outward * t;
                }
                var path = new List<Vector2>();
                const float step = 0.1f, speed = 1.4f;
                AddLeg(path, entry + outward * 4f, entry, speed * step);
                AddLeg(path, entry, entry - outward * 1.2f, speed * step);
                AddLeg(path, entry - outward * 1.2f, mid, speed * step);
                int waitIn = path.Count;
                for (int i = 0; i < 10; i++) path.Add(mid);
                int back = path.Count;
                AddLeg(path, mid, entry - outward * 1.2f, speed * step);
                AddLeg(path, entry - outward * 1.2f, entry, speed * step);
                AddLeg(path, entry, entry + outward * 2.5f, speed * step);
                for (int i = 0; i < 12; i++) path.Add(entry + outward * 2.5f);

                MapCutaway.RestoreAll();
                _md.Append("## Walking in and out\n\nThe stand-in walks at 1.4 m/s ").Append(door ? "through the front door of " : "(no working door found: straight through the wall) into ")
                    .Append(Describe(house)).Append(" from 4 m out, the camera at heading ").Append(Deg(yaw)).Append("° facing that wall, to the middle, waits 1 s and walks ")
                    .Append("back out; one cutaway frame every 0.1 s, as the game runs it. Pictures `18`-`23` are taken on the way. Changes of the house's cut:\n\n")
                    .Append("| t (s) | stand-in | house cut |\n|--:|---|---|\n");
                float now = _t += 100f, start = now;
                int last = -1, leftFrame = -1;
                bool wasInside = false, everInside = false;
                float enteredAt = -1f, cutAt = -1f, leftAt = -1f, restoredAt = -1f;
                for (int f = 0; f < path.Count; f++)
                {
                    Vector2 p = path[f];
                    now += step;
                    var feet = new Vector3(p.x, 0f, p.y);
                    MapCutaway.Run(feet, yaw, _isoPitch, Reach(Wide), true, now, false);
                    bool inHouse = Contains(house.Pts, p);
                    if (inHouse && !wasInside && enteredAt < 0f) enteredAt = now - start;
                    if (!inHouse && wasInside && f >= back && leftAt < 0f)
                    {
                        leftAt = now - start;
                        leftFrame = f;
                    }
                    everInside |= inHouse;
                    wasInside = inHouse;
                    string picture = null, what = null;
                    if (f == 0) { picture = "18_walk_a_outside.png"; what = "4 m outside the door, walking in"; }
                    else if (inHouse && enteredAt >= 0f && Mathf.Abs(now - start - enteredAt) < 1e-3f) { picture = "19_walk_b_entering.png"; what = "Just through the door"; }
                    else if (f == back - 1) { picture = "20_walk_c_inside.png"; what = "Inside, after a second"; }
                    else if (leftFrame >= 0 && f == leftFrame) { picture = "21_walk_d_leaving.png"; what = "Just out of the door again"; }
                    else if (leftFrame >= 0 && f == leftFrame + 3) { picture = "22_walk_e_stepped_out.png"; what = "0.3 s later"; }
                    else if (leftFrame >= 0 && f == leftFrame + 8) { picture = "23_walk_f_restored.png"; what = "0.8 s later"; }
                    if (picture != null)
                    {
                        Picture(picture, feet, yaw, Wide, true, false);
                        _pictureRows.Append("| `").Append(picture).Append("` | ").Append(what).Append(" (").Append(Describe(house)).Append(") | ")
                            .Append(house.Applied == 0 ? "nothing" : Parts(house.Applied)).Append(" | |\n");
                    }
                    if (house.Applied == last) continue;
                    if (house.Applied != 0 && cutAt < 0f) cutAt = now - start;
                    if (house.Applied == 0 && leftAt >= 0f && restoredAt < 0f) restoredAt = now - start;
                    last = house.Applied;
                    _md.Append("| ").Append(F1(now - start)).Append(" | ").Append(inHouse ? "inside" : F1(EdgeDistance(house.Pts, p)) + " m outside")
                        .Append(" | ").Append(house.Applied == 0 ? "nothing" : Parts(house.Applied)).Append(" |\n");
                }
                _md.Append("\nEntered at ").Append(F1(enteredAt)).Append(" s, cut at ").Append(F1(cutAt)).Append(" s; left at ").Append(F1(leftAt))
                    .Append(" s, all back at ").Append(F1(restoredAt)).Append(" s (restore delay ").Append(F1(MapCutaway.RestoreDelay)).Append(" s).\n\n");
                if (_pictureRows.Length > 0)
                    _md.Append("| file | shows | house cut | |\n|---|---|---|---|\n").Append(_pictureRows).Append('\n');
                _pictureRows.Clear();
                if (!everInside || cutAt < 0f) Fail("walking into " + Describe(house) + " never cut it");
                if (restoredAt < 0f) Fail("walking out of " + Describe(house) + " never brought it back");
                MapCutaway.RestoreAll();
            }

            private readonly StringBuilder _pictureRows = new StringBuilder();

            private static void AddLeg(List<Vector2> path, Vector2 from, Vector2 to, float stride)
            {
                float len = Vector2.Distance(from, to);
                int n = Mathf.Max(1, Mathf.CeilToInt(len / stride));
                for (int i = 0; i < n; i++) path.Add(Vector2.Lerp(from, to, i / (float)n));
            }
            #endregion
        }
    }
}
#endif
