"""Primitive-model geometry: sanitising LLM output, checking it, normalising it to the engine's
conventions, and rendering quick previews with Pillow.

Engine conventions (see Assets/Scripts/World/ItemModelDesc.cs, PrimitiveMesh.cs):
  * cube 1 m across, sphere 1 m diameter, cylinder 1 m diameter x 2 m tall (scale.y = height / 2)
  * the model is centred on its own origin and hovers with its centre at BaseHeight
  * Quaternion.Euler(x, y, z) rotates about Z, then X, then Y

In the JSON files every part's "size" is in real metres for all shapes (a cylinder is
[diameter, height, diameter]); GeneratedItemModels.cs does the cylinder half-height conversion.
"""
import math

import numpy as np

SHAPES = ("cube", "sphere", "cylinder")
HOVER = 0.06          # clearance under the model; matches the renderer's 0.06 m bob amplitude
MIN_SIZE = 0.002
MAX_SIZE = 3.0


# ── Sanitising ────────────────────────────────────────────────────────────────────────────────

def _vec(v, default, n=3):
    try:
        out = [float(x) for x in list(v)[:n]]
    except (TypeError, ValueError):
        return list(default)
    if len(out) != n or not all(math.isfinite(x) for x in out):
        return list(default)
    return out


def _clamp01(x, default=0.0):
    try:
        x = float(x)
    except (TypeError, ValueError):
        return default
    return min(1.0, max(0.0, x)) if math.isfinite(x) else default


def sanitize(raw_parts):
    """Coerces whatever the model produced into well-formed parts, dropping the unusable ones."""
    parts = []
    for p in raw_parts or []:
        if not isinstance(p, dict):
            continue
        shape = str(p.get("shape", "cube")).lower().strip()
        if shape not in SHAPES:
            shape = "cylinder" if "cyl" in shape else "sphere" if "sph" in shape or "ball" in shape else "cube"
        size = [min(MAX_SIZE, max(MIN_SIZE, abs(s))) for s in _vec(p.get("size"), (0.05, 0.05, 0.05))]
        color = _vec(p.get("color"), (0.5, 0.5, 0.5))
        if max(color) > 1.0:  # someone wrote 0..255
            color = [c / 255.0 for c in color]
        parts.append({
            "name": str(p.get("name", ""))[:40],
            "shape": shape,
            "pos": [round(x, 4) for x in _vec(p.get("pos"), (0, 0, 0))],
            "rot": [round(((x + 180.0) % 360.0) - 180.0, 2) for x in _vec(p.get("rot"), (0, 0, 0))],
            "size": [round(x, 4) for x in size],
            "color": [round(_clamp01(c), 3) for c in color],
            "smooth": round(_clamp01(p.get("smooth"), 0.1), 3),
            "metal": round(_clamp01(p.get("metal"), 0.0), 3),
        })
    return parts


# ── Transforms ────────────────────────────────────────────────────────────────────────────────

def euler_matrix(rot_deg):
    """Unity's Quaternion.Euler as a 3x3 matrix: R = Ry * Rx * Rz."""
    x, y, z = (math.radians(a) for a in rot_deg)
    cx, sx, cy, sy, cz, sz = math.cos(x), math.sin(x), math.cos(y), math.sin(y), math.cos(z), math.sin(z)
    rx = np.array([[1, 0, 0], [0, cx, -sx], [0, sx, cx]])
    ry = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]])
    rz = np.array([[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]])
    return ry @ rx @ rz


_CORNERS = np.array([[x, y, z] for x in (-0.5, 0.5) for y in (-0.5, 0.5) for z in (-0.5, 0.5)])


def part_aabb(p):
    pts = (_CORNERS * np.array(p["size"])) @ euler_matrix(p["rot"]).T + np.array(p["pos"])
    return pts.min(axis=0), pts.max(axis=0)


def bounds(parts):
    boxes = [part_aabb(p) for p in parts]
    return np.min([b[0] for b in boxes], axis=0), np.max([b[1] for b in boxes], axis=0)


# ── Checks ────────────────────────────────────────────────────────────────────────────────────

def check(parts, min_parts=4, max_extent=2.5):
    """Problems worth sending back to the LLM. An empty list means the model is acceptable."""
    issues = []
    if len(parts) < min_parts:
        issues.append(f"Only {len(parts)} usable parts; use at least {min_parts}.")
        return issues

    lo, hi = bounds(parts)
    ext = hi - lo
    if ext.max() > max_extent:
        issues.append(f"Model is {ext.max():.2f} m across, far too big for a dropped item; check units are metres.")
    if ext.max() < 0.015:
        issues.append(f"Model is only {ext.max() * 100:.1f} cm across; check units are metres.")

    # Connectivity: parts whose boxes (slightly inflated) touch belong together. Anything outside the
    # main cluster is floating in mid-air and will look broken in game.
    tol = 0.005 + 0.03 * float(ext.max())
    boxes = [part_aabb(p) for p in parts]
    parent = list(range(len(parts)))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    for i in range(len(parts)):
        for j in range(i + 1, len(parts)):
            a0, a1 = boxes[i]
            b0, b1 = boxes[j]
            if np.all(a0 - tol <= b1) and np.all(b0 - tol <= a1):
                parent[find(i)] = find(j)

    groups = {}
    for i in range(len(parts)):
        groups.setdefault(find(i), []).append(i)
    main = max(groups.values(), key=len)
    floating = [parts[i]["name"] or f"part {i}" for g in groups.values() if g is not main for i in g]
    if floating and len(floating) > max(1, 0.1 * len(parts)):
        issues.append("These parts float apart from the main body and must touch it: " + ", ".join(floating[:12]))
    return issues


def normalize(parts):
    """Centres the model on its origin and returns (parts, base_height) in the engine's convention."""
    lo, hi = bounds(parts)
    centre = (lo + hi) / 2.0
    out = []
    for p in parts:
        q = dict(p)
        q["pos"] = [round(float(v), 4) for v in np.array(p["pos"]) - centre]
        out.append(q)
    base_height = round(float(hi[1] - lo[1]) / 2.0 + HOVER, 4)
    return out, base_height


# ── Preview rendering ─────────────────────────────────────────────────────────────────────────

def _cube_mesh():
    v = _CORNERS.copy()
    idx = lambda x, y, z: int((x > 0) * 4 + (y > 0) * 2 + (z > 0))
    faces = []
    for axis in range(3):
        for sign in (-0.5, 0.5):
            quad = []
            for a, b in ((-0.5, -0.5), (0.5, -0.5), (0.5, 0.5), (-0.5, 0.5)):
                c = [0, 0, 0]
                c[axis] = sign
                c[(axis + 1) % 3] = a
                c[(axis + 2) % 3] = b
                quad.append(idx(*c))
            faces += [(quad[0], quad[1], quad[2]), (quad[0], quad[2], quad[3])]
    return v, np.array(faces)


def _sphere_mesh(seg=14, rings=9):
    verts = []
    for r in range(rings + 1):
        phi = math.pi * r / rings
        for s in range(seg):
            th = 2 * math.pi * s / seg
            verts.append((0.5 * math.sin(phi) * math.cos(th), 0.5 * math.cos(phi), 0.5 * math.sin(phi) * math.sin(th)))
    faces = []
    for r in range(rings):
        for s in range(seg):
            a, b = r * seg + s, r * seg + (s + 1) % seg
            c, d = a + seg, b + seg
            faces += [(a, b, d), (a, d, c)]
    return np.array(verts), np.array(faces)


def _cylinder_mesh(seg=16):
    verts = []
    for y in (-0.5, 0.5):
        for s in range(seg):
            th = 2 * math.pi * s / seg
            verts.append((0.5 * math.cos(th), y, 0.5 * math.sin(th)))
    verts += [(0, -0.5, 0), (0, 0.5, 0)]
    faces = []
    for s in range(seg):
        a, b = s, (s + 1) % seg
        faces += [(a, b, b + seg), (a, b + seg, a + seg), (2 * seg, b, a), (2 * seg + 1, a + seg, b + seg)]
    return np.array(verts), np.array(faces)


def _outward(mesh):
    """Every primitive is convex and centred on the origin, so wind each triangle to face away from it."""
    v, f = mesh
    t = v[f]
    n = np.cross(t[:, 1] - t[:, 0], t[:, 2] - t[:, 0])
    flip = np.einsum("ij,ij->i", n, t.mean(axis=1)) < 0
    f = f.copy()
    f[flip] = f[flip][:, ::-1]
    return v, f


_MESHES = {k: _outward(m) for k, m in
           {"cube": _cube_mesh(), "sphere": _sphere_mesh(), "cylinder": _cylinder_mesh()}.items()}
_LIGHT = np.array([-0.4, 0.8, -0.45])
_LIGHT /= np.linalg.norm(_LIGHT)


def _triangles(parts):
    tris, cols = [], []
    for p in parts:
        v, f = _MESHES[p["shape"]]
        world = (v * np.array(p["size"])) @ euler_matrix(p["rot"]).T + np.array(p["pos"])
        t = world[f]
        tris.append(t)
        cols.append(np.repeat([p["color"]], len(t), axis=0))
    return np.concatenate(tris), np.concatenate(cols)


def render(parts, size=320, yaw=35.0, pitch=35.0, bg=(46, 48, 54)):
    """Orthographic flat-shaded view, painter's-algorithm sorted. Good enough to judge a silhouette."""
    from PIL import Image, ImageDraw

    ss = 2  # supersample for cheap anti-aliasing
    w = size * ss
    img = Image.new("RGB", (w, w), bg)
    if not parts:
        return img.resize((size, size))

    tris, cols = _triangles(parts)
    n = np.cross(tris[:, 1] - tris[:, 0], tris[:, 2] - tris[:, 0])
    n /= np.linalg.norm(n, axis=1, keepdims=True) + 1e-12
    shade = 0.38 + 0.62 * np.clip(n @ _LIGHT, 0, 1)

    cam = euler_matrix((pitch, yaw, 0.0))  # camera looks along its +Z, like a Unity camera
    lo, hi = tris.reshape(-1, 3).min(0), tris.reshape(-1, 3).max(0)
    c = (tris - (lo + hi) / 2) @ cam  # world -> camera space (cam is orthonormal)
    nc = n @ cam

    flat = c.reshape(-1, 3)
    span = max(np.ptp(flat[:, 0]), np.ptp(flat[:, 1]), 1e-3)
    scale = w * 0.84 / span
    order = np.argsort(-c[:, :, 2].mean(axis=1))  # far to near

    draw = ImageDraw.Draw(img)
    for i in order:
        if nc[i, 2] > 0.0:  # facing away from the camera
            continue
        pts = [(w / 2 + x * scale, w / 2 - y * scale) for x, y, _ in c[i]]
        rgb = np.clip(cols[i] ** (1 / 2.2) * shade[i] * 255, 0, 255).astype(int)
        draw.polygon(pts, fill=tuple(int(k) for k in rgb))
    return img.resize((size, size), Image.LANCZOS)


def render_sheet(parts, title=None, size=320):
    """Three-quarter view beside a near-top-down view (how the player mostly sees floor items)."""
    from PIL import Image, ImageDraw

    a = render(parts, size, yaw=35, pitch=32)
    b = render(parts, size, yaw=0, pitch=80)
    sheet = Image.new("RGB", (size * 2, size + (22 if title else 0)), (30, 31, 36))
    sheet.paste(a, (0, 0))
    sheet.paste(b, (size, 0))
    if title:
        ImageDraw.Draw(sheet).text((6, size + 4), title[:80], fill=(220, 220, 220))
    return sheet
