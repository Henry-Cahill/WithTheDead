"""Turns a TRELLIS.2 mesh into the game's item-mesh files, and renders its previews on the GPU.

The mesh is fitted onto the item's existing primitive model (Assets/Resources/GeneratedItemModels/<id>.json, or the
hand-built model exported from Unity), which already holds everything the game expects of the item's pose: its real
size in metres, how it lies on the floor, and for held items which end is the handle (min X) and which way is up a
gun (+Z); see HumanoidRig.SetHeld. The mesh is first squared up (see frames: TRELLIS.2 keeps the picture's azimuth,
so a bat photographed at an angle lies diagonally). Then, of the 24 ways of turning it square onto the model's axes,
those that keep the model's proportions (long side long, thin side thin) are scaled to its extents, and the one whose
volume overlaps the model's best wins; that settles which end is which.

The game copy is a low-detail mesh, so the detail of TRELLIS.2's full mesh is baked into a tangent-space normal map
(bake_normal_map), and its metal and roughness into URP's metallic map (mask_map).

Assets/Resources/GeneratedItemMeshes/<id>@<variant>.bytes, read by GeneratedItemMeshes.cs (little-endian):
  "CZM2", uint16 version (3), uint16 LOD count, float32 base height, smoothness, metallic, uint32 flags
  (1: the body is raw-deflated; 2: LOD 0 has a normal map and a metallic map), uint32 body length before deflating
  body, per LOD: uint32 vertex count, uint32 index count, float32 box min x, y, z, float32 box size x, y, z,
                 vertices (18 bytes each): uint16 x, y, z across the box; int16 x 2 the normal, octahedral (snorm);
                           int8 x 4 the tangent's x, y, z and w (w, the bitangent's sign, +-1); uint16 u, v (unorm)
                 indices: uint16, three per triangle in Unity's clockwise winding; padded to 4 bytes
Beside it: <stem>_tex<lod>.jpg, each LOD's base colour; <stem>_nrm0.jpg, LOD 0's tangent-space normal map (OpenGL
convention, green up); <stem>_msk0.png, LOD 0's metallic map (metallic in red, smoothness in alpha).
"""
import itertools
import os
import struct
import zlib

import numpy as np

import geometry

MAGIC = b"CZM2"
VERSION = 3  # 2 had full-precision positions and 16-bit normals and tangents (30-byte vertices)
FLAG_DEFLATE = 1
FLAG_MAPS = 2
VERTEX = np.dtype([("p", "<u2", 3), ("n", "<i2", 2), ("t", "i1", 4), ("uv", "<u2", 2)])
PREVIEW_BG = (46, 48, 54)  # the primitive pipeline's preview background, so both galleries look alike


def _rotations():
    """The 24 rotations that map the axes onto the axes (signed permutation matrices with determinant +1)."""
    out = []
    for perm in itertools.permutations(range(3)):
        for signs in itertools.product((1.0, -1.0), repeat=3):
            m = np.zeros((3, 3))
            for row, (col, s) in enumerate(zip(perm, signs)):
                m[row, col] = s
            if np.linalg.det(m) > 0:
                out.append(m)
    return out


ROTATIONS = _rotations()


def unity_arrays(tm):
    """(vertices, faces, normals, uvs) of a to_glb mesh in Unity's frame.

    to_glb hands back a right-handed, Y-up mesh with counter-clockwise front faces and bottom-left UVs. Unity is
    left-handed with clockwise front faces, so X is mirrored and every triangle is reversed; the UVs already match.
    """
    v = np.asarray(tm.vertices, dtype=np.float64).copy()
    n = np.asarray(tm.vertex_normals, dtype=np.float64).copy()
    v[:, 0] *= -1.0
    n[:, 0] *= -1.0
    f = np.asarray(tm.faces, dtype=np.int64)[:, ::-1].copy()
    uv = np.asarray(tm.visual.uv, dtype=np.float64).copy()
    return v, f, n, uv


def high_res_unity(m):
    """TRELLIS.2's full-detail mesh, before to_glb remeshes and decimates it, in Unity's frame and on the GPU, for
    bake_normal_map: (vertices, faces) as CUDA tensors. to_glb turns its output to glTF's axes, (x, y, z) -> (x, z, -y),
    and unity_arrays mirrors X; the faces are reversed with the mirror, so they keep facing out."""
    import torch

    v = m.vertices.float().cuda()
    v = torch.stack([-v[:, 0], v[:, 2], -v[:, 1]], dim=1).contiguous()
    f = m.faces.int().cuda()[:, [0, 2, 1]].contiguous()
    return v, f


def tangents(v, f, n, uv):
    """Per-vertex tangents (x, y, z, w) as Unity's shaders read them: x, y, z run along +U on the surface, and the
    bitangent the shader rebuilds as cross(normal, tangent) * w runs along +V. Each triangle's UV gradient is summed
    into its corners (Lengyel's method), then made perpendicular to the vertex normal."""
    v, n, uv = (np.asarray(a, dtype=np.float64) for a in (v, n, uv))
    e1, e2 = v[f[:, 1]] - v[f[:, 0]], v[f[:, 2]] - v[f[:, 0]]
    d1, d2 = uv[f[:, 1]] - uv[f[:, 0]], uv[f[:, 2]] - uv[f[:, 0]]
    det = d1[:, 0] * d2[:, 1] - d2[:, 0] * d1[:, 1]
    usable = np.abs(det) > 1e-14
    r = np.where(usable, 1.0 / np.where(usable, det, 1.0), 0.0)[:, None]
    along_u = (e1 * d2[:, 1:2] - e2 * d1[:, 1:2]) * r
    along_v = (e2 * d1[:, 0:1] - e1 * d2[:, 0:1]) * r
    t_sum, b_sum = np.zeros_like(v), np.zeros_like(v)
    for k in range(3):
        np.add.at(t_sum, f[:, k], along_u)
        np.add.at(b_sum, f[:, k], along_v)
    nn = n / np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-12)
    t = t_sum - nn * np.einsum("ij,ij->i", nn, t_sum)[:, None]
    length = np.linalg.norm(t, axis=1, keepdims=True)
    # A vertex with no usable UV gradient (a degenerate corner) gets any direction across its normal.
    other = np.where(np.abs(nn[:, :1]) < 0.9, [[1.0, 0.0, 0.0]], [[0.0, 1.0, 0.0]])
    fallback = np.cross(nn, other)
    fallback /= np.maximum(np.linalg.norm(fallback, axis=1, keepdims=True), 1e-12)
    t = np.where(length > 1e-12, t / np.maximum(length, 1e-12), fallback)
    w = np.where(np.einsum("ij,ij->i", np.cross(nn, t), b_sum) < 0.0, -1.0, 1.0)
    return np.column_stack([t, w])


# A negative-volume piece lying (by its median vertex) within this fraction of the item's size of the positive pieces is
# the inner skin of one of them. Measured on the pilot: inner skins sit at 0.4-3% even after heavy decimation pushed
# them partly through their outer skins; a thin part that decimation turned inside out lay at 88%.
NEAR_SKIN = 0.05


def drop_inner_shells(lod):
    """Removes the inner skins TRELLIS.2's remesher leaves inside every solid part.

    to_glb's remesher (CuMesh's narrow-band dual contouring) extracts the surface lying a hair's breadth from the
    object's, which wraps each solid part in two skins: one just outside it, facing out, and one just inside, facing
    in. Thin parts (a towel, an apron) come out as one closed slab instead, which is why the remesher is kept. The game
    draws front faces only, so the inner skins never show, but they take half the triangles. An inner skin is a piece
    that encloses a negative volume and hugs the pieces enclosing a positive one (NEAR_SKIN); a thin part that heavy
    decimation turned inside out is not hugging anything, so it stays. Pieces are found once the vertices that UV
    seams split are welded together again. Works on (vertices, faces, any per-vertex arrays...) in either frame: the
    conversion to Unity's keeps a volume's sign.
    """
    from scipy.sparse import coo_matrix
    from scipy.sparse.csgraph import connected_components
    from scipy.spatial import cKDTree

    v, f, *per_vertex = lod
    if not len(f):
        return lod
    _, weld = np.unique(np.round(v, 6), axis=0, return_inverse=True)
    weld = np.asarray(weld).reshape(-1)
    fw = weld[f]
    count = int(weld.max()) + 1
    edges = coo_matrix((np.ones(2 * len(fw)), (np.concatenate([fw[:, 0], fw[:, 1]]), np.concatenate([fw[:, 1], fw[:, 2]]))),
                       shape=(count, count))
    _, labels = connected_components(edges, directed=False)
    piece = labels[fw[:, 0]]
    t = v[f]
    volume = np.einsum("ij,ij->i", t[:, 0], np.cross(t[:, 1], t[:, 2])) / 6.0
    area = 0.5 * np.linalg.norm(np.cross(t[:, 1] - t[:, 0], t[:, 2] - t[:, 0]), axis=1)
    outer = np.bincount(piece, weights=volume)[piece] >= 0.0
    if outer.all() or not outer.any():
        return lod

    # Points spread over the positive pieces' triangles, to measure how far each negative piece lies from them.
    shell = t[outer]
    r = np.random.default_rng(0).random((2, len(shell), 6))
    flip = r[0] + r[1] > 1.0
    r[0][flip], r[1][flip] = 1.0 - r[0][flip], 1.0 - r[1][flip]
    spread = (shell[:, None, 0] + r[0][..., None] * (shell[:, None, 1] - shell[:, None, 0])
              + r[1][..., None] * (shell[:, None, 2] - shell[:, None, 0]))
    tree = cKDTree(np.vstack([spread.reshape(-1, 3), shell.reshape(-1, 3)]))
    reach = NEAR_SKIN * float(np.ptp(v, axis=0).max())
    drop = np.zeros(len(f), dtype=bool)
    for p in np.unique(piece[~outer]):
        faces = np.flatnonzero(piece == p)
        if np.median(tree.query(v[np.unique(f[faces])])[0]) < reach:
            drop[faces] = True
    keep = ~drop
    if area[keep].sum() < 0.3 * area.sum():
        return lod  # not a skin inside a skin; leave the mesh as it is
    f = f[keep]
    used = np.unique(f)
    remap = np.full(len(v), -1, dtype=np.int64)
    remap[used] = np.arange(len(used))
    return (v[used], remap[f], *(a[used] for a in per_vertex))


# ── Fitting onto the primitive model ────────────────────────────────────────────────────────────

# How far (as a fraction of the model's size) a turn's box may miss the model's box and still be considered. Turns
# that keep the proportions (the long side long, the thin side thin) compete on overlap; the rest are ruled out.
EXTENT_SLACK = 0.1
# How much better than the mesh as generated a squared-up frame must overlap the model to be used, so a mesh that was
# already square isn't nudged a degree or two by its own asymmetry.
FRAME_MARGIN = 0.02
# Overlaps this close to the best count as equal, and the item's own up and front decide between them (see fit).
TIE = 0.03
# When even the best overlap is poor, the overlap says little about which way up the item goes (a hollow cap against a
# solid model scores about 0.2 every way up), so the window widens to this fraction of what the best falls short of 1.
POOR_FIT_TIE = 0.15


def _voxels(v, f, n=48):
    """The mesh as filled voxels, about n along its longest side. Both shapes go through this, so a hollow box or a
    thin garment is treated the same way on either side of the comparison."""
    import trimesh

    mesh = trimesh.Trimesh(v, f, process=False)
    vox = mesh.voxelized(max(float(np.ptp(v, axis=0).max()) / n, 1e-4))
    try:
        vox = vox.fill()  # solid where the surface is closed; an open shell just stays a shell
    except Exception:
        pass
    return vox


def _yaw_to_x(d):
    """The turn about the vertical (Y) that takes the horizontal direction d = (x, z) onto +X."""
    x, z = d / max(float(np.linalg.norm(d)), 1e-12)
    return np.array([[x, 0.0, z], [0.0, 1.0, 0.0], [-z, 0.0, x]])


def frames(v):
    """Ways to square the mesh up before the search over the 24 axis turns.

    TRELLIS.2 keeps the picture's azimuth, so a bat photographed lying at an angle comes out lying diagonally, though
    always the right way up. Besides the mesh as generated, its footprint is turned about the vertical so its long
    side runs along X (by its principal axis, and by its smallest bounding rectangle), and for anything tilted out of
    the vertical too, its smallest 3D box is squared to the axes.
    """
    out = [np.eye(3)]
    xz = v[:, [0, 2]] - v[:, [0, 2]].mean(axis=0)
    w, vecs = np.linalg.eigh(xz.T @ xz)
    out.append(_yaw_to_x(vecs[:, int(np.argmax(w))]))
    try:
        from scipy.spatial import ConvexHull

        hull = xz[ConvexHull(xz).vertices]
        best = None
        for i in range(len(hull)):
            e = hull[(i + 1) % len(hull)] - hull[i]
            if np.linalg.norm(e) < 1e-9:
                continue
            d = e / np.linalg.norm(e)
            n = np.array([-d[1], d[0]])
            ext = np.ptp(hull @ d), np.ptp(hull @ n)
            if best is None or ext[0] * ext[1] < best[0]:
                best = (ext[0] * ext[1], d if ext[0] >= ext[1] else n)
        if best is not None:
            out.append(_yaw_to_x(best[1]))
    except Exception:
        pass  # a degenerate (flat or tiny) footprint; the other frames still apply
    try:
        import trimesh

        r = np.array(trimesh.bounds.oriented_bounds(v)[0][:3, :3])
        if np.linalg.det(r) < 0:
            r[2] *= -1.0  # a proper turn; the axis search sorts out which way each axis points
        out.append(r)
    except Exception:
        pass
    return out


def fit(v, f, parts, up=(0.0, 1.0, 0.0), front=(0.0, 0.0, 1.0)):
    """Best way to lay the mesh onto the reference model.

    Returns (score, rotation, scale, centre): a point x of the mesh goes to scale * rotation @ (x - centre). The score
    is the F1 of the two volumes' overlap (1 = same shape), worth logging: a low one means the picture showed
    something shaped quite unlike the old model, and one of the two is probably wrong.

    Turns that overlap about as well as the best (within TIE, or more when the best overlap is poor: POOR_FIT_TIE) are
    as good as equal to the overlap: a near-symmetric shape can't tell its two ends or faces apart. Among them the one
    that keeps the mesh's up direction up wins, then the one that turns its front up. TRELLIS.2 builds things upright
    and facing the way the picture showed them, so this keeps a bottle the right way up and a badge or a towel face up
    rather than showing its guessed underside. up and front are where those directions lie in the mesh as given: +Y and
    +Z for a TRELLIS.2 mesh in Unity's frame (front is glTF's), and wherever an earlier fit turned them for a refit.
    """
    up, front = np.asarray(up, float), np.asarray(front, float)
    tris, _ = geometry._triangles(parts)
    rv = tris.reshape(-1, 3)
    rlo, rhi = rv.min(axis=0), rv.max(axis=0)
    ref_c, ref_ext = (rlo + rhi) / 2.0, rhi - rlo
    ref_vox = _voxels(rv - ref_c, np.arange(len(rv)).reshape(-1, 3))
    ref_pts = np.asarray(ref_vox.points)

    scored = []  # (score less any frame margin, score, rotation, scale, centre)
    for k, frame in enumerate(frames(v)):
        u = v @ frame.T
        lo, hi = u.min(axis=0), u.max(axis=0)
        cu = (lo + hi) / 2.0
        ext = hi - lo
        vox = _voxels(u - cu, f)
        ai_pts = np.asarray(vox.points)

        candidates = []
        for m in ROTATIONS:
            e = np.abs(m) @ ext
            s = float(e @ ref_ext / max(float(e @ e), 1e-12))  # least-squares match of the three extents
            miss = float(np.abs(s * e - ref_ext) @ ref_ext / max(float(ref_ext @ ref_ext), 1e-12))
            candidates.append((miss, m, s))
        floor = min(c[0] for c in candidates)

        margin = 0.0 if k == 0 else FRAME_MARGIN
        for miss, m, s in candidates:
            if miss > floor + EXTENT_SLACK:
                continue
            precision = float(np.asarray(ref_vox.is_filled(ai_pts @ m.T * s)).mean()) if len(ai_pts) else 0.0
            recall = float(np.asarray(vox.is_filled((ref_pts / s) @ m)).mean()) if len(ref_pts) else 0.0
            score = 2.0 * precision * recall / max(precision + recall, 1e-9)
            # The frame folds into the rotation; the box centre found in the frame goes back to mesh space.
            scored.append((score - margin, score, m @ frame, s, frame.T @ cu))

    top = max(c[0] for c in scored)
    near = [c for c in scored if c[0] >= top - max(TIE, POOR_FIT_TIE * (1.0 - top))]
    best = max(near, key=lambda c: (round(float((c[2] @ up)[1]), 3), round(float((c[2] @ front)[1]), 3), c[0]))
    return best[1:]


def place(lods, fitted):
    """Applies a fit to every LOD, then centres them on the near LOD's box as the engine expects.

    Returns (lods, base_height, offset); base height follows geometry.normalize: half the height plus the hover
    clearance, and offset is what centring subtracted after the fit's turn and scale.
    """
    _, m, s, centre = fitted

    def move(lod):
        v, f, n, uv, *rest = lod
        out = [s * ((v - centre) @ m.T), f, n @ m.T, uv]
        for t in rest:  # tangents: the direction turns with the mesh, the bitangent's sign stays
            t = np.array(t, dtype=np.float64)
            t[:, :3] = t[:, :3] @ m.T
            out.append(t)
        return out

    moved = [move(lod) for lod in lods]
    lo, hi = moved[0][0].min(axis=0), moved[0][0].max(axis=0)
    mid = (lo + hi) / 2.0
    out = [tuple([lod[0] - mid] + lod[1:]) for lod in moved]
    return out, round(float(hi[1] - lo[1]) / 2.0 + geometry.HOVER, 4), mid


# ── Maps baked from the full-detail mesh ────────────────────────────────────────────────────────

def _raster():
    import nvdiffrast.torch as dr

    global _ctx
    if _ctx is None:
        _ctx = dr.RasterizeCudaContext()
    return _ctx


def bake_normal_map(lod, high, size):
    """A tangent-space normal map that puts the full-detail mesh's surface onto the game mesh (a 5-tuple with
    tangents, in Unity's frame, before fitting: the turn the fit applies changes nothing in tangent space).

    Every texel of the game mesh's UV layout finds the nearest point of the full-detail mesh (`high`, from
    high_res_unity) and stores that point's normal in the texel's tangent frame, solved against URP's own
    reconstruction (tangent * x + bitangent * y + normal * z) so the shader gets back exactly the full-detail normal.
    The rasterising matches to_glb's texture bakes, so the map lines up with the base colour. Returns an RGB array,
    OpenGL convention (green up), as Unity's normal-map import expects.
    """
    import cv2
    import torch
    import torch.nn.functional as F
    import cumesh
    import nvdiffrast.torch as dr

    v, f, n, uv, t = lod
    hv, hf = high
    dev = "cuda"
    uvt = torch.tensor(uv, dtype=torch.float32, device=dev)
    # Row 0 of the image is the top of the texture (v = 1), as in to_glb's bakes.
    clip = torch.cat([uvt[:, :1] * 2 - 1, 1 - uvt[:, 1:2] * 2, torch.zeros_like(uvt[:, :1]), torch.ones_like(uvt[:, :1])], 1)
    tri = torch.tensor(f, dtype=torch.int32, device=dev)
    rast, _ = dr.rasterize(_raster(), clip[None].contiguous(), tri, resolution=[size, size])
    covered = rast[0, ..., 3] > 0

    def at(a):
        x, _ = dr.interpolate(torch.tensor(a, dtype=torch.float32, device=dev)[None].contiguous(), rast, tri)
        return x[0][covered]

    pos = at(v)
    normal = F.normalize(at(n), dim=1)
    t4 = at(t)
    tangent = F.normalize(t4[:, :3], dim=1)
    bitangent = torch.cross(normal, tangent, dim=1) * torch.where(t4[:, 3:4] < 0, -1.0, 1.0)

    corners = hv[hf.long()]
    face_n = torch.cross(corners[:, 1] - corners[:, 0], corners[:, 2] - corners[:, 0], dim=1)  # area-weighted
    vertex_n = torch.zeros_like(hv)
    for k in range(3):
        vertex_n.index_add_(0, hf[:, k].long(), face_n)
    vertex_n = F.normalize(vertex_n, dim=1)
    _, face, uvw = cumesh.cuBVH(hv, hf).unsigned_distance(pos.contiguous(), return_uvw=True)
    detail = F.normalize((vertex_n[hf[face.long()].long()] * uvw[..., None]).sum(dim=1), dim=1)
    # A single sheet of the full mesh (a towel) may face either way; take the side the game mesh faces.
    detail = torch.where((detail * normal).sum(1, keepdim=True) < 0, -detail, detail)

    frame = torch.stack([tangent, bitangent, normal], dim=2)
    xyz, info = torch.linalg.solve_ex(frame, detail)
    fallback = torch.stack([(detail * tangent).sum(1), (detail * bitangent).sum(1), (detail * normal).sum(1)], 1)
    xyz = torch.where((info != 0)[:, None] | ~torch.isfinite(xyz).all(1, keepdim=True), fallback, xyz)
    xyz[:, 2] = xyz[:, 2].clamp(min=0.05)  # a normal pointing into the surface only shades oddly
    xyz = F.normalize(xyz, dim=1)

    img = torch.tensor([128, 128, 255], dtype=torch.uint8, device=dev).repeat(size, size, 1)
    img[covered] = ((xyz * 0.5 + 0.5) * 255.0).round().clamp(0, 255).to(torch.uint8)
    img = img.cpu().numpy()
    # Fill the gaps between UV charts so filtering and mipmaps don't pull in the flat colour at the seams.
    return cv2.inpaint(img, (~covered).cpu().numpy().astype(np.uint8), 3, cv2.INPAINT_TELEA)


def mask_map(tm):
    """URP's metallic map from to_glb's glTF one (roughness in green, metallic in blue): metallic in red, smoothness
    (1 - roughness) in alpha. None when the mesh has no PBR map."""
    from PIL import Image

    tex = getattr(tm.visual.material, "metallicRoughnessTexture", None)
    if tex is None:
        return None
    a = np.asarray(tex.convert("RGB"))
    out = np.zeros(a.shape[:2] + (4,), dtype=np.uint8)
    out[..., 0] = a[..., 2]
    out[..., 3] = 255 - a[..., 1]
    return Image.fromarray(out, "RGBA")


def export_master(tm, path):
    """Writes to_glb's full-detail export as a GLB (WebP textures), the remesher's inner skins dropped, under a
    temporary name that is renamed once complete. It stays in TRELLIS.2's own frame (glTF, Y up); the fit that places
    the game copy is recorded beside it."""
    import trimesh

    v, f, n, uv = drop_inner_shells((np.asarray(tm.vertices), np.asarray(tm.faces), np.asarray(tm.vertex_normals),
                                     np.asarray(tm.visual.uv)))
    out = trimesh.Trimesh(vertices=v, faces=f, vertex_normals=n, process=False,
                          visual=trimesh.visual.TextureVisuals(uv=uv, material=tm.visual.material))
    tmp = path.with_name(f"{path.name}.{os.getpid()}.tmp")
    out.export(str(tmp), file_type="glb", extension_webp=True)
    os.replace(tmp, path)
    return len(f)


# ── Output files ────────────────────────────────────────────────────────────────────────────────

def surface(tm):
    """(smoothness, metallic) for the item's material, averaged from the PBR map at the vertices' UVs."""
    mat = tm.visual.material
    tex = getattr(mat, "metallicRoughnessTexture", None)
    if tex is None:
        return 0.2, 0.0
    arr = np.asarray(tex.convert("RGB"), dtype=np.float32) / 255.0
    h, w = arr.shape[:2]
    uv = np.asarray(tm.visual.uv)
    px = np.clip(np.round(uv[:, 0] * (w - 1)).astype(int), 0, w - 1)
    py = np.clip(np.round((1.0 - uv[:, 1]) * (h - 1)).astype(int), 0, h - 1)  # image rows run top-down
    rough = float(arr[py, px, 1].mean())
    metal = float(arr[py, px, 2].mean())
    return round(min(1.0, max(0.0, 1.0 - rough)), 3), round(min(1.0, max(0.0, metal)), 3)


def base_color(tm):
    return tm.visual.material.baseColorTexture.convert("RGB")


def write_atomic(path, data):
    """Writes bytes under a temporary name, then renames, so a reader never sees half a file."""
    tmp = path.with_name(f"{path.name}.{os.getpid()}.tmp")
    tmp.write_bytes(data)
    os.replace(tmp, path)


def save_image(img, path, **kw):
    """Saves a PIL image under a temporary name, then renames (the format comes from the real name)."""
    tmp = path.with_name(f"{path.name}.{os.getpid()}.tmp")
    img.save(tmp, format={".jpg": "JPEG", ".png": "PNG"}[path.suffix.lower()], **kw)
    os.replace(tmp, path)


def oct_encode(n):
    """Unit vectors to octahedral coordinates in [-1, 1]^2 (two numbers per direction, evenly precise all round)."""
    n = np.asarray(n, dtype=np.float64)
    n = n / np.maximum(np.abs(n).sum(axis=1, keepdims=True), 1e-12)
    x, y, z = n[:, 0], n[:, 1], n[:, 2]
    sx, sy = np.where(x >= 0, 1.0, -1.0), np.where(y >= 0, 1.0, -1.0)
    under = z < 0
    return np.column_stack([np.where(under, (1.0 - np.abs(y)) * sx, x), np.where(under, (1.0 - np.abs(x)) * sy, y)])


def oct_decode(e):
    e = np.asarray(e, dtype=np.float64)
    x, y = e[:, 0], e[:, 1]
    z = 1.0 - np.abs(x) - np.abs(y)
    under = z < 0
    sx, sy = np.where(x >= 0, 1.0, -1.0), np.where(y >= 0, 1.0, -1.0)
    nx = np.where(under, (1.0 - np.abs(y)) * sx, x)
    ny = np.where(under, (1.0 - np.abs(x)) * sy, y)
    n = np.column_stack([nx, ny, z])
    return n / np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-12)


def write_mesh_file(path, lods, base_height, smoothness, metallic, maps=False):
    """Writes the CZM2 file described at the top. lods are (vertices, faces, normals, uvs, tangents)."""
    body = bytearray()
    for v, f, n, uv, t in lods:
        if len(v) > 65535:
            raise ValueError(f"{len(v)} vertices is past the 16-bit index limit; lower the decimation target")
        lo = v.min(axis=0)
        size = np.maximum(v.max(axis=0) - lo, 1e-6)
        rec = np.zeros(len(v), dtype=VERTEX)
        rec["p"] = np.round((v - lo) / size * 65535.0)
        length = np.linalg.norm(n, axis=1, keepdims=True)
        # A degenerate corner can leave a zero normal, which Unity would shade black; point it up instead.
        nn = np.where(length > 1e-9, n / np.maximum(length, 1e-12), np.array([0.0, 1.0, 0.0]))
        rec["n"] = np.round(np.clip(oct_encode(nn), -1.0, 1.0) * 32767.0)
        tt = np.array(t, dtype=np.float64)
        tt[:, :3] /= np.maximum(np.linalg.norm(tt[:, :3], axis=1, keepdims=True), 1e-12)
        tt[:, 3] = np.where(tt[:, 3] < 0, -1.0, 1.0)
        rec["t"] = np.round(np.clip(tt, -1.0, 1.0) * 127.0)
        rec["uv"] = np.round(np.clip(uv, 0.0, 1.0) * 65535.0)
        body += struct.pack("<II6f", len(v), f.size, *lo, *size)
        body += rec.tobytes()
        body += np.ascontiguousarray(f, dtype="<u2").tobytes()
        body += b"\0" * (-len(body) % 4)
    squeeze = zlib.compressobj(9, zlib.DEFLATED, -15)  # raw deflate, as System.IO.Compression.DeflateStream reads it
    packed = squeeze.compress(bytes(body)) + squeeze.flush()
    flags = FLAG_DEFLATE | (FLAG_MAPS if maps else 0)
    head = MAGIC + struct.pack("<HHfffII", VERSION, len(lods), base_height, smoothness, metallic, flags, len(body))
    write_atomic(path, head + packed)


def read_mesh_file(path):
    """The inverse of write_mesh_file: (base_height, smoothness, metallic, flags, [(v, f, n, uv, t), ...])."""
    data = path.read_bytes()
    if data[:4] != MAGIC:
        raise ValueError(f"{path} is not a CZM2 file")
    version, count, base_height, smooth, metal, flags, size = struct.unpack_from("<HHfffII", data, 4)
    if version != VERSION:
        raise ValueError(f"{path} is version {version} of the format; this reads {VERSION} (generate it again)")
    body = data[28:]
    if flags & FLAG_DEFLATE:
        body = zlib.decompress(body, -15)
    if len(body) != size:
        raise ValueError(f"{path}: the body is {len(body)} bytes, the header says {size}")
    pos = 0
    lods = []
    for _ in range(count):
        nv, ni, *box = struct.unpack_from("<II6f", body, pos)
        pos += 32
        rec = np.frombuffer(body, dtype=VERTEX, count=nv, offset=pos)
        pos += rec.nbytes
        idx = np.frombuffer(body, dtype="<u2", count=ni, offset=pos).astype(np.int64).reshape(-1, 3)
        pos += ni * 2
        pos += -pos % 4
        lo, extent = np.array(box[:3]), np.array(box[3:])
        t = rec["t"].astype(np.float64) / 127.0
        t[:, :3] /= np.maximum(np.linalg.norm(t[:, :3], axis=1, keepdims=True), 1e-12)
        t[:, 3] = np.where(t[:, 3] < 0, -1.0, 1.0)
        lods.append((lo + rec["p"].astype(np.float64) / 65535.0 * extent, idx, oct_decode(rec["n"] / 32767.0),
                     rec["uv"] / 65535.0, t))
    return base_height, smooth, metal, flags, lods


# ── GPU previews (nvdiffrast) ───────────────────────────────────────────────────────────────────

_ctx = None


def front_faces(f, xy):
    """The faces the game would draw: front faces are clockwise on screen in Unity's convention (x right, y up)."""
    p = xy[f]
    signed = ((p[:, 1, 0] - p[:, 0, 0]) * (p[:, 2, 1] - p[:, 0, 1])
              - (p[:, 1, 1] - p[:, 0, 1]) * (p[:, 2, 0] - p[:, 0, 0]))
    return f[signed < 0]


def render(v, f, n, uv, texture, yaw, pitch, size=320, ss=2, t=None, normal_map=None):
    """Orthographic textured view with the primitive previews' camera and light, rasterised by nvdiffrast. Back faces
    are culled as in the game, so the preview (and the critic judging it) sees what a player would; with tangents and
    a normal map, the normal map shades it as URP does."""
    import torch
    import nvdiffrast.torch as dr
    from PIL import Image

    cam = geometry.euler_matrix((pitch, yaw, 0.0))  # camera looks along its +Z, like a Unity camera
    c = v @ cam
    lo, hi = c.min(axis=0), c.max(axis=0)
    mid = (lo + hi) / 2.0
    half = max(float(hi[0] - lo[0]), float(hi[1] - lo[1]), 1e-3) / 2.0
    depth = max(float(hi[2] - lo[2]), 1e-3) / 2.0
    clip = np.ones((len(v), 4))
    clip[:, :2] = (c[:, :2] - mid[:2]) / half * 0.84
    clip[:, 2] = (c[:, 2] - mid[2]) / depth * 0.9
    f = front_faces(f, clip[:, :2])
    if not len(f):
        return Image.new("RGB", (size, size), PREVIEW_BG)

    dev = "cuda"
    pos = torch.tensor(clip, dtype=torch.float32, device=dev)[None]
    tri = torch.tensor(f, dtype=torch.int32, device=dev)
    res = size * ss
    rast, _ = dr.rasterize(_raster(), pos, tri, resolution=[res, res])

    def at(a):
        x, _ = dr.interpolate(torch.tensor(a, dtype=torch.float32, device=dev)[None].contiguous(), rast, tri)
        return x

    st = uv.copy()
    st[:, 1] = 1.0 - st[:, 1]  # nvdiffrast's (0, 0) is the first row in memory, the top of the image
    texc = at(st).contiguous()
    tex = torch.tensor(np.asarray(texture, dtype=np.float32) / 255.0, device=dev)[None].contiguous()
    albedo = dr.texture(tex, texc, filter_mode="linear")

    nrm = torch.nn.functional.normalize(at(n), dim=-1)
    if t is not None and normal_map is not None:
        t4 = at(t)
        tan = torch.nn.functional.normalize(t4[..., :3], dim=-1)
        bit = torch.cross(nrm, tan, dim=-1) * torch.where(t4[..., 3:4] < 0, -1.0, 1.0)
        nm = torch.tensor(np.asarray(normal_map, dtype=np.float32) / 127.5 - 1.0, device=dev)[None].contiguous()
        ts = dr.texture(nm, texc, filter_mode="linear")
        nrm = torch.nn.functional.normalize(tan * ts[..., :1] + bit * ts[..., 1:2] + nrm * ts[..., 2:3], dim=-1)
    light = torch.tensor(geometry._LIGHT, dtype=torch.float32, device=dev)
    shade = 0.38 + 0.62 * (nrm * light).sum(-1, keepdim=True).clamp(min=0.0)

    covered = (rast[..., 3:4] > 0).float()
    bg = torch.tensor(PREVIEW_BG, dtype=torch.float32, device=dev) / 255.0
    img = albedo * shade * covered + bg * (1.0 - covered)
    img = dr.antialias(img.contiguous(), rast, pos, tri)
    arr = (img[0].flip(0).clamp(0, 1).cpu().numpy() * 255.0).astype(np.uint8)  # rows start at the bottom
    return Image.fromarray(arr).resize((size, size), Image.LANCZOS)


def render_pair(lod, texture, size=320, normal_map=None):
    """Three-quarter and near top-down views, as the primitive pipeline's previews."""
    v, f, n, uv, *rest = lod
    t = rest[0] if rest else None
    return (render(v, f, n, uv, texture, 35, 32, size, t=t, normal_map=normal_map),
            render(v, f, n, uv, texture, 0, 80, size, t=t, normal_map=normal_map))


def sheet(reference, views, title, size=320):
    """The reference picture beside the two renders, captioned: one gallery tile."""
    from PIL import Image, ImageDraw

    panels = [reference.convert("RGB").resize((size, size), Image.LANCZOS)] + list(views)
    out = Image.new("RGB", (size * len(panels), size + 22), (30, 31, 36))
    for i, p in enumerate(panels):
        out.paste(p, (i * size, 0))
    ImageDraw.Draw(out).text((6, size + 4), title[:110], fill=(220, 220, 220))
    return out
