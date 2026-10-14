"""Builds Runeheir's scenery kit (Art Pass 3) in Blender and exports it to the Unity project.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup -P Tools/Blender/build_environment.py
    ... -- --only tree_round_0,rock_1 --no-render

Writes:
  Assets/_Runeheir/Resources/Environment/<model>.fbx   static meshes in meters, material slots named for what they are
  Tools/Blender/Previews/environment_kit.png          a quick Blender lineup (not the game shader)

Every model is generated from a seed, like the characters, so the kit rebuilds identically. The look is the "Option B"
concept's: soft, chunky anime shapes that read at the isometric camera. Canopies are clusters of lumpy blobs (smooth, so
the toon shader paints them in two clean bands), rocks are faceted, trunks taper and flare at the root. Slots (Bark,
Leaf, LeafLight, Needle, BirchBark, BirchMark, Rock, RockDark, Moss, Wood, Petal, PetalCenter, Stem) are painted in game
per map theme (green meadows, autumn birches, snowy firs), so one kit dresses every land.
"""

import math
import os
import random
import sys

import bpy
from mathutils import Vector, noise

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.normpath(os.path.join(HERE, "..", ".."))
OUT_DIR = os.path.join(PROJECT, "Assets", "_Runeheir", "Resources", "Environment")
PREVIEW_DIR = os.path.join(HERE, "Previews")

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ONLY = set(ARGS[ARGS.index("--only") + 1].split(",")) if "--only" in ARGS else set()
RENDER = "--no-render" not in ARGS

# Preview colors (the game paints these slots per theme).
PREVIEW = {
    "Bark": "#6B4A32", "Wood": "#C9A577", "Leaf": "#4E8A3A", "LeafLight": "#79B04A", "Needle": "#2F6B45", "NeedleLight": "#4C8C58",
    "BirchBark": "#E8E2D6", "BirchMark": "#3A3532", "Rock": "#8C8A86", "RockDark": "#6A6864", "Moss": "#6C8C3E",
    "Petal": "#E8B4D0", "PetalCenter": "#F3D35A", "Stem": "#4E7A36", "Berry": "#C0392B",
    "Wall": "#8C6440", "WallDark": "#5A3E28", "Roof": "#6E8C44", "RoofDark": "#4E3A28", "Door": "#4A2E1C", "Window": "#FFD27A",
    "Stone": "#8E8C88", "StoneDark": "#6E6C68", "Gold": "#D8A43A", "ShieldA": "#A8322A", "ShieldB": "#2F5E9C", "WoodDark": "#5A3E28",
    "Awning": "#B8322A", "AwningStripe": "#EADFC8", "Crate": "#A07850", "Sack": "#C9B38A", "Goods": "#C0392B", "Water": "#3A6E9A",
    "Statue": "#A8A6A0", "StatueDark": "#7E7C78", "Runes": "#7FD6FF",
    "Frond": "#4E8A3A", "FrondLight": "#79B04A", "Canvas": "#DCCFB4", "CanvasDark": "#B8A98C", "TentShadow": "#2A2420", "Rope": "#9C8A6A",
}


# ---------------------------------------------------------------------------------------------- mesh parts
class Part:
    """Vertices, faces, one material slot and smooth or flat shading."""

    def __init__(self, verts, faces, slot, smooth=True):
        self.verts = verts
        self.faces = faces
        self.slot = slot
        self.smooth = smooth


def icosphere(subdiv):
    """Unit icosphere vertices and triangles."""
    t = (1.0 + 5 ** 0.5) / 2.0
    verts = [Vector(v).normalized() for v in ((-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0), (0, -1, t), (0, 1, t), (0, -1, -t),
                                              (0, 1, -t), (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1))]
    faces = [(0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11), (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6), (7, 1, 8),
             (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9), (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1)]
    for _ in range(subdiv):
        cache = {}

        def middle(a, b):
            key = (min(a, b), max(a, b))
            if key not in cache:
                verts.append(((verts[a] + verts[b]) * 0.5).normalized())
                cache[key] = len(verts) - 1
            return cache[key]

        new_faces = []
        for a, b, c in faces:
            ab, bc, ca = middle(a, b), middle(b, c), middle(c, a)
            new_faces += [(a, ab, ca), (b, bc, ab), (c, ca, bc), (ab, bc, ca)]
        faces = new_faces
    return verts, faces


def blob(center, radius, seed, slot, subdiv=2, bumps=0.22, freq=1.6, squash=(1.0, 1.0, 0.82), flat_bottom=0.35, smooth=True):
    """A lumpy ball (canopy cluster, bush, rock): an icosphere pushed in and out by noise."""
    verts, faces = icosphere(subdiv)
    out = []
    offset = Vector((seed * 1.37, seed * 2.11, seed * 0.73))
    for v in verts:
        n = noise.noise(v * freq + offset)
        r = radius * (1.0 + bumps * n)
        p = Vector((v.x * r * squash[0], v.y * r * squash[1], v.z * r * squash[2]))
        if p.z < -radius * flat_bottom:
            p.z = -radius * flat_bottom + (p.z + radius * flat_bottom) * 0.25
        out.append(center + p)
    return Part(out, faces, slot, smooth)


def tube(points, radii, slot, sides=8, cap=True, lobes=0, lobe_depth=0.0, seed=0.0):
    """A tapered tube along a polyline (trunks, branches, stems). lobes adds root buttresses to the first ring."""
    verts, faces = [], []
    n = len(points)
    for i, (p, r) in enumerate(zip(points, radii)):
        d = (points[min(i + 1, n - 1)] - points[max(i - 1, 0)]).normalized()
        side = d.cross(Vector((0.0, 1.0, 0.0)) if abs(d.y) < 0.9 else Vector((1.0, 0.0, 0.0))).normalized()
        up = side.cross(d).normalized()
        for k in range(sides):
            a = 2.0 * math.pi * k / sides
            rr = r
            if lobes and i == 0:
                rr *= 1.0 + lobe_depth * max(0.0, math.cos(a * lobes + seed))
            rr *= 1.0 + 0.06 * noise.noise(Vector((math.cos(a) * 2.0, math.sin(a) * 2.0, i * 0.7 + seed)))
            verts.append(p + (side * math.cos(a) + up * math.sin(a)) * rr)
    for i in range(n - 1):
        for k in range(sides):
            a, b = i * sides + k, i * sides + (k + 1) % sides
            faces.append((a, a + sides, b + sides, b))
    if cap:
        verts.append(points[-1] + (points[-1] - points[-2]).normalized() * radii[-1] * 0.3)
        tip = len(verts) - 1
        base = (n - 1) * sides
        for k in range(sides):
            faces.append((base + (k + 1) % sides, base + k, tip))
    return Part(verts, faces, slot)


def cone_layer(base_z, top_z, radius, points, jag, slot, seed):
    """One tier of a fir: a star-edged skirt rising to a point."""
    verts, faces = [], []
    count = points * 2
    for k in range(count):
        a = 2.0 * math.pi * k / count + seed
        r = radius * (1.0 if k % 2 == 0 else 1.0 - jag)
        droop = -0.12 * radius if k % 2 == 0 else 0.0
        verts.append(Vector((math.cos(a) * r, math.sin(a) * r, base_z + droop)))
    inner = len(verts)
    for k in range(count):
        a = 2.0 * math.pi * k / count + seed
        verts.append(Vector((math.cos(a) * radius * 0.35, math.sin(a) * radius * 0.35, base_z + (top_z - base_z) * 0.15)))
    verts.append(Vector((0.0, 0.0, top_z)))
    tip = len(verts) - 1
    for k in range(count):
        k2 = (k + 1) % count
        faces.append((k, k2, tip))
        faces.append((k2, k, inner + k, inner + k2))
    return Part(verts, faces, slot)


def curve(start, direction, length, bend, segments=5):
    pts = []
    for i in range(segments + 1):
        t = i / segments
        p = start + direction * (length * t)
        p += Vector((bend[0], bend[1], 0.0)) * (t * t) * length
        pts.append(p)
    return pts


# ---------------------------------------------------------------------------------------------- models
def tree_round(seed):
    """A broadleaf with a chunky clustered crown: one big blob and four to six around it, the upper ones lighter."""
    rnd = random.Random(seed)
    height = rnd.uniform(3.2, 4.2)
    lean = Vector((rnd.uniform(-0.25, 0.25), rnd.uniform(-0.25, 0.25), 0.0))
    trunk = curve(Vector((0, 0, -0.1)), Vector((0, 0, 1)), height * 0.75, lean, 6)
    parts = [tube(trunk, [0.32 * (1 - 0.55 * i / 6) for i in range(7)], "Bark", sides=9, lobes=4, lobe_depth=0.45, seed=seed)]
    top = trunk[-1]
    for b in range(3):
        a = rnd.uniform(0, 2 * math.pi)
        start = trunk[3 + b % 3]
        d = Vector((math.cos(a), math.sin(a), 0.9)).normalized()
        parts.append(tube(curve(start, d, rnd.uniform(0.8, 1.2), (0, 0)), [0.11, 0.08, 0.06, 0.05, 0.04, 0.03], "Bark", sides=6))
    crown = top + Vector((0, 0, 0.6))
    parts.append(blob(crown, rnd.uniform(1.45, 1.7), seed, "Leaf"))
    for k in range(rnd.randint(4, 6)):
        a = 2 * math.pi * k / 5 + rnd.uniform(-0.3, 0.3)
        r = rnd.uniform(0.85, 1.15)
        c = crown + Vector((math.cos(a) * 1.15, math.sin(a) * 1.15, rnd.uniform(-0.55, 0.45)))
        parts.append(blob(c, r, seed * 7 + k, "LeafLight" if c.z > crown.z + 0.1 else "Leaf"))
    parts.append(blob(crown + Vector((0, 0, 1.05)), rnd.uniform(0.9, 1.1), seed * 3 + 1, "LeafLight"))
    return parts


def tree_pine(seed):
    """A fir: a straight trunk under four or five star-edged tiers, each narrower and darker at the base."""
    rnd = random.Random(seed)
    height = rnd.uniform(5.0, 6.4)
    parts = [tube([Vector((0, 0, -0.1)), Vector((0, 0, height * 0.5)), Vector((0, 0, height * 0.9))], [0.26, 0.17, 0.08], "Bark", sides=8,
                  lobes=3, lobe_depth=0.35, seed=seed)]
    tiers = rnd.randint(4, 5)
    for i in range(tiers):
        t = i / tiers
        base = 0.9 + t * (height - 1.6)
        radius = (1.9 - 1.35 * t) * rnd.uniform(0.92, 1.08)
        parts.append(cone_layer(base, base + 1.9 - 0.4 * t, radius, rnd.randint(7, 9), 0.32, "Needle" if i % 2 == 0 else "NeedleLight",
                                rnd.uniform(0, 1)))
    return parts


def tree_birch(seed):
    """A slender white birch with dark marks and a small airy crown of light leaves."""
    rnd = random.Random(seed)
    height = rnd.uniform(4.2, 5.2)
    lean = Vector((rnd.uniform(-0.3, 0.3), rnd.uniform(-0.3, 0.3), 0.0))
    trunk = curve(Vector((0, 0, -0.1)), Vector((0, 0, 1)), height * 0.82, lean, 7)
    parts = [tube(trunk, [0.16 * (1 - 0.5 * i / 7) for i in range(8)], "BirchBark", sides=8, lobes=3, lobe_depth=0.25, seed=seed)]
    for m in range(6):
        i = rnd.randint(1, 6)
        p = trunk[i]
        a = rnd.uniform(0, 2 * math.pi)
        r = 0.16 * (1 - 0.5 * i / 7) + 0.006
        d = Vector((math.cos(a), math.sin(a), 0))
        mark = [p + d * r + Vector((0, 0, -0.06)), p + d * r + Vector((0, 0, 0.06))]
        parts.append(tube(mark, [0.07, 0.07], "BirchMark", sides=4))
    top = trunk[-1]
    for k in range(5):
        a = 2 * math.pi * k / 5 + rnd.uniform(-0.3, 0.3)
        c = top + Vector((math.cos(a) * 0.75, math.sin(a) * 0.75, rnd.uniform(-0.8, 0.5)))
        parts.append(blob(c, rnd.uniform(0.6, 0.85), seed * 5 + k, "LeafLight" if k % 2 else "Leaf", bumps=0.3))
    parts.append(blob(top + Vector((0, 0, 0.6)), 0.8, seed + 11, "Leaf", bumps=0.3))
    return parts


def tree_dead(seed):
    """A bare, crooked tree for crypts, tundra and haunted ground."""
    rnd = random.Random(seed)
    trunk = curve(Vector((0, 0, -0.1)), Vector((0, 0, 1)), 3.4, (rnd.uniform(-0.3, 0.3), rnd.uniform(-0.3, 0.3)), 6)
    parts = [tube(trunk, [0.3 * (1 - 0.7 * i / 6) for i in range(7)], "Bark", sides=8, lobes=4, lobe_depth=0.5, seed=seed)]
    for b in range(6):
        start = trunk[2 + b % 4]
        a = rnd.uniform(0, 2 * math.pi)
        d = Vector((math.cos(a), math.sin(a), rnd.uniform(0.3, 1.0))).normalized()
        pts = curve(start, d, rnd.uniform(0.9, 1.6), (rnd.uniform(-0.4, 0.4), rnd.uniform(-0.4, 0.4)), 4)
        parts.append(tube(pts, [0.09, 0.07, 0.05, 0.035, 0.02], "Bark", sides=5))
    return parts


def bush(seed):
    rnd = random.Random(seed)
    parts = []
    for k in range(rnd.randint(4, 6)):
        a = 2 * math.pi * k / 5 + rnd.uniform(-0.4, 0.4)
        d = rnd.uniform(0.25, 0.55)
        c = Vector((math.cos(a) * d, math.sin(a) * d, rnd.uniform(0.35, 0.6)))
        parts.append(blob(c, rnd.uniform(0.45, 0.65), seed * 9 + k, "LeafLight" if c.z > 0.5 else "Leaf", bumps=0.25))
    for k in range(rnd.randint(0, 6) if seed % 2 else 0):
        a = rnd.uniform(0, 2 * math.pi)
        c = Vector((math.cos(a) * 0.6, math.sin(a) * 0.6, rnd.uniform(0.4, 0.8)))
        parts.append(blob(c, 0.07, seed + k, "Berry", subdiv=1, bumps=0.0))
    return parts


def rock(seed):
    """A faceted boulder; some wear a cap of moss."""
    rnd = random.Random(seed)
    r = rnd.uniform(0.7, 1.0)
    parts = [blob(Vector((0, 0, r * 0.35)), r, seed, "Rock", subdiv=1, bumps=0.32, freq=1.2,
                  squash=(rnd.uniform(1.0, 1.4), rnd.uniform(0.8, 1.1), rnd.uniform(0.6, 0.85)), flat_bottom=0.35, smooth=False)]
    for k in range(rnd.randint(1, 2)):
        a = rnd.uniform(0, 2 * math.pi)
        c = Vector((math.cos(a) * r * 0.9, math.sin(a) * r * 0.9, r * 0.12))
        parts.append(blob(c, r * rnd.uniform(0.3, 0.45), seed * 3 + k, "RockDark", subdiv=1, bumps=0.3, smooth=False))
    if seed % 2 == 0:
        parts.append(blob(Vector((0, 0, r * 0.82)), r * 0.62, seed + 50, "Moss", subdiv=2, bumps=0.15, squash=(1.2, 1.0, 0.28), flat_bottom=0.1))
    return parts


def stump(seed):
    rnd = random.Random(seed)
    parts = [tube([Vector((0, 0, -0.05)), Vector((0, 0, 0.35)), Vector((0, 0, 0.55))], [0.42, 0.36, 0.34], "Bark", sides=10, cap=False, lobes=4,
                  lobe_depth=0.5, seed=seed)]
    rings = []
    for k in range(10):
        a = 2 * math.pi * k / 10
        rings.append(Vector((math.cos(a) * 0.33, math.sin(a) * 0.33, 0.55 + rnd.uniform(-0.02, 0.02))))
    rings.append(Vector((0, 0, 0.57)))
    parts.append(Part(rings, [(k, (k + 1) % 10, 10) for k in range(10)], "Wood"))
    return parts


def flowers(seed):
    """A little clump of wildflowers."""
    rnd = random.Random(seed)
    parts = []
    for k in range(rnd.randint(4, 6)):
        a = rnd.uniform(0, 2 * math.pi)
        d = rnd.uniform(0.0, 0.35)
        base = Vector((math.cos(a) * d, math.sin(a) * d, 0))
        h = rnd.uniform(0.38, 0.62)
        top = base + Vector((rnd.uniform(-0.06, 0.06), rnd.uniform(-0.06, 0.06), h))
        parts.append(tube([base, top], [0.015, 0.012], "Stem", sides=4, cap=False))
        for p in range(5):
            pa = 2 * math.pi * p / 5
            parts.append(blob(top + Vector((math.cos(pa) * 0.1, math.sin(pa) * 0.1, 0)), 0.085, seed + p, "Petal", subdiv=1, bumps=0.0,
                              squash=(1.0, 1.0, 0.35)))
        parts.append(blob(top + Vector((0, 0, 0.015)), 0.06, seed, "PetalCenter", subdiv=1, bumps=0.0, squash=(1, 1, 0.6)))
    return parts


def fern(seed):
    """A fern: arching fronds, each two skins 1 cm apart (wound opposite ways) so either side shows."""
    rnd = random.Random(seed)
    parts = []
    for k in range(rnd.randint(6, 8)):
        a = 2 * math.pi * k / 7 + rnd.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0))
        side = Vector((-d.y, d.x, 0))
        length = rnd.uniform(0.6, 0.85)
        segs = 6
        for skin, lift in ((0, 0.0), (1, -0.01)):
            verts = []
            for i in range(segs + 1):
                t = i / segs
                w = 0.16 * math.sin(math.pi * min(1.0, t * 1.1))
                p = d * (length * t) + Vector((0, 0, 0.45 * math.sin(math.pi * t * 0.8) + lift))
                verts += [p + side * w, p - side * w]
            faces = [(2 * i, 2 * i + 1, 2 * i + 3, 2 * i + 2) for i in range(segs)]
            if skin:
                faces = [tuple(reversed(f)) for f in faces]
            parts.append(Part(verts, faces, "Frond" if k % 2 else "FrondLight"))
    return parts


def tent(seed):
    """An A-frame camp tent: canvas sagging between ridge and pegs, a dark doorway, poles, guy ropes and stakes."""
    rnd = random.Random(seed)
    length, half_width, rise = 3.2, 1.75, 2.1
    parts = []
    segs_u, segs_v = 8, 4
    for side in (1, -1):
        # Two skins 2 cm apart, wound opposite ways: whichever faces the camera has the right normal.
        for skin, inset in ((0, 0.0), (1, 0.02)):
            verts, faces = [], []
            for i in range(segs_u + 1):
                u = i / segs_u
                x = (u - 0.5) * length
                for j in range(segs_v + 1):
                    v = j / segs_v  # 0 at the ridge, 1 at the hem
                    sag = 0.12 * math.sin(math.pi * u) * math.sin(math.pi * v)
                    y = side * (half_width * v - sag * 0.6 - inset)
                    z = rise * (1.0 - v) - sag - inset
                    verts.append(Vector((x, y, z)))
            for i in range(segs_u):
                for j in range(segs_v):
                    a = i * (segs_v + 1) + j
                    quad = (a, a + 1, a + segs_v + 2, a + segs_v + 1)
                    faces.append(quad if (side > 0) == (skin == 0) else tuple(reversed(quad)))
            parts.append(Part(verts, faces, "Canvas" if side > 0 else "CanvasDark"))
    # the closed back and the open doorway, both two-skinned
    back_x, front_x = -length * 0.5, length * 0.5
    for dx, flip in ((0.0, False), (0.02, True)):
        tri = [Vector((back_x + dx, half_width, 0)), Vector((back_x + dx, -half_width, 0)), Vector((back_x + dx, 0, rise))]
        parts.append(Part(tri, [(2, 1, 0) if flip else (0, 1, 2)], "CanvasDark"))
        door = [Vector((front_x - dx, half_width * 0.55, 0)), Vector((front_x - dx, -half_width * 0.55, 0)), Vector((front_x - dx, 0, rise * 0.72))]
        parts.append(Part(door, [(2, 1, 0) if flip else (0, 1, 2)], "TentShadow"))
    for flap_side in (1, -1):
        for dx, flip in ((0.0, False), (0.02, True)):
            flap = [Vector((front_x + dx, flap_side * half_width, 0)), Vector((front_x + dx, flap_side * half_width * 0.55, 0)), Vector((front_x + dx, 0, rise)),
                    Vector((front_x + 0.25 + dx, flap_side * half_width * 0.62, 0.05))]
            faces = [(0, 1, 2), (1, 3, 2)]
            parts.append(Part(flap, [tuple(reversed(f)) for f in faces] if flip else faces, "Canvas" if flap_side > 0 else "CanvasDark"))
    # poles and ridge
    for x in (back_x - 0.05, front_x + 0.05):
        parts.append(tube([Vector((x, 0, -0.05)), Vector((x, 0, rise + 0.3))], [0.05, 0.04], "Wood", sides=6))
    parts.append(tube([Vector((back_x - 0.1, 0, rise + 0.02)), Vector((front_x + 0.1, 0, rise + 0.02))], [0.045, 0.045], "Wood", sides=6))
    # guy ropes to stakes
    for x in (back_x - 0.05, front_x + 0.05):
        for side in (1, -1):
            top = Vector((x, 0, rise + 0.2))
            peg = Vector((x + (0.6 if x > 0 else -0.6), side * (half_width + 0.5), 0.05))
            parts.append(tube([top, peg], [0.012, 0.012], "Rope", sides=4, cap=False))
            parts.append(tube([peg + Vector((0, 0, -0.1)), peg + Vector((0, 0, 0.18))], [0.035, 0.025], "Wood", sides=5))
    return parts


# ---------------------------------------------------------------------------------------------- the town kit
BOX_FACES = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (2, 3, 7, 6), (3, 0, 4, 7), (1, 2, 6, 5)]


def hexa(corners, slot, smooth=False):
    """A six-sided solid from eight corners: bottom ring (-x-y, +x-y, +x+y, -x+y), then the top ring."""
    return Part([Vector(c) for c in corners], BOX_FACES, slot, smooth)


def box(center, size, slot, yaw=0.0, tilt=0.0):
    """An axis box (sx, sy, sz) around center, turned by yaw about Z then tilt about X (degrees)."""
    cx, cy, cz = center
    sx, sy, sz = (s * 0.5 for s in size)
    corners = []
    for z in (-sz, sz):
        for x, y in ((-sx, -sy), (sx, -sy), (sx, sy), (-sx, sy)):
            v = Vector((x, y, z))
            if tilt:
                t = math.radians(tilt)
                v = Vector((v.x, v.y * math.cos(t) - v.z * math.sin(t), v.y * math.sin(t) + v.z * math.cos(t)))
            if yaw:
                a = math.radians(yaw)
                v = Vector((v.x * math.cos(a) - v.y * math.sin(a), v.x * math.sin(a) + v.y * math.cos(a), v.z))
            corners.append(v + Vector((cx, cy, cz)))
    return hexa(corners, slot)


def gable_roof(length, depth, eave_z, ridge_z, thick, slot, ridge_slot, overhang=0.5, end_overhang=0.4):
    """A pitched roof along X: two thick slabs meeting at the ridge, with a beam along the top."""
    half_l = length * 0.5 + end_overhang
    half_d = depth * 0.5 + overhang
    slope = (ridge_z - eave_z) / half_d
    drop = eave_z - overhang * slope
    parts = []
    for side in (1, -1):
        def p(x, y, z):
            return (x, side * y, z)
        lower = [p(-half_l, half_d, drop), p(half_l, half_d, drop), p(half_l, 0.0, ridge_z), p(-half_l, 0.0, ridge_z)]
        upper = [(x, y, z + thick) for x, y, z in lower]
        corners = lower + upper
        if side > 0:
            # the ring must run counter-clockwise seen from above, as BOX_FACES expects
            corners = [corners[i] for i in (1, 0, 3, 2, 5, 4, 7, 6)]
        parts.append(hexa(corners, slot))
    parts.append(box((0, 0, ridge_z + thick + 0.05), (length + end_overhang * 2 + 0.2, 0.32, 0.22), ridge_slot))
    return parts


def gable_walls(length, depth, wall_top, ridge_z, slot):
    """The triangles under the roof at both ends."""
    parts = []
    for side in (1, -1):
        x = side * length * 0.5
        a, b, c = Vector((x, -depth * 0.5, wall_top)), Vector((x, depth * 0.5, wall_top)), Vector((x, 0, ridge_z))
        parts.append(Part([a, b, c], [(0, 1, 2)] if side > 0 else [(2, 1, 0)], slot))
    return parts


def log_lines(length, depth, z0, z1, slot, step=0.42):
    parts = []
    z = z0
    while z < z1:
        for side in (1, -1):
            parts.append(box((0, side * (depth * 0.5 + 0.03), z), (length + 0.08, 0.07, 0.08), slot))
            parts.append(box((side * (length * 0.5 + 0.03), 0, z), (0.07, depth + 0.08, 0.08), slot))
        z += step
    return parts


def longhouse(seed, length=7.0, depth=5.4, wall_h=2.4, rise=2.4, door_side=1, hall=False):
    """A Norse longhouse: stone footing, log walls, corner posts, a turf or thatch roof with crossed gable boards, a
    plank door with a frame, a warm window and a smoke hole; the hall adds dragon heads, shields and double doors."""
    rnd = random.Random(seed)
    base_h = 0.4 if not hall else 0.6
    wall_top = base_h + wall_h
    ridge = wall_top + rise
    parts = [box((0, 0, base_h * 0.5), (length + 0.3, depth + 0.3, base_h), "Stone")]
    parts.append(box((0, 0, base_h + wall_h * 0.5), (length, depth, wall_h), "Wall"))
    parts += log_lines(length, depth, base_h + 0.3, wall_top - 0.1, "WallDark")
    for sx in (1, -1):
        for sy in (1, -1):
            parts.append(box((sx * length * 0.5, sy * depth * 0.5, base_h + wall_h * 0.5 + 0.1), (0.34, 0.34, wall_h + 0.2), "WallDark"))
    parts += gable_walls(length, depth, wall_top, ridge - 0.1, "Wall")
    parts += gable_roof(length, depth, wall_top, ridge, 0.32 if not hall else 0.45, "Roof", "RoofDark",
                        overhang=0.6 if not hall else 0.9, end_overhang=0.45 if not hall else 0.7)
    # crossed gable boards rising past the ridge
    for sx in (1, -1):
        x = sx * (length * 0.5 + (0.5 if not hall else 0.75))
        for sy in (1, -1):
            parts.append(box((x, sy * 0.45, ridge + 0.35), (0.14, 0.22, 2.0 if not hall else 3.0), "WallDark", tilt=sy * 38))
        if hall:
            # a carved dragon head on each gable
            neck = [Vector((x, 0, ridge + 0.9)), Vector((x + sx * 0.5, 0, ridge + 1.9)), Vector((x + sx * 1.2, 0, ridge + 2.5))]
            parts.append(tube(neck, [0.28, 0.24, 0.2], "WallDark", sides=8))
            head = neck[-1] + Vector((sx * 0.35, 0, 0.05))
            parts.append(blob(head, 0.42, seed + int(sx * 3), "WallDark", subdiv=1, bumps=0.1, squash=(1.5, 0.8, 0.8)))
            parts.append(blob(head + Vector((sx * 0.2, 0.25, 0.15)), 0.08, seed, "Gold", subdiv=1, bumps=0.0))
            parts.append(blob(head + Vector((sx * 0.2, -0.25, 0.15)), 0.08, seed, "Gold", subdiv=1, bumps=0.0))
    # the door (and its frame) on the long wall
    y = door_side * (depth * 0.5 + 0.06)
    door_w = 1.2 if not hall else 2.6
    door_h = 2.0 if not hall else 3.0
    parts.append(box((0, y, base_h + door_h * 0.5), (door_w, 0.1, door_h), "Door"))
    parts.append(box((0, y + door_side * 0.04, base_h + door_h + 0.1), (door_w + 0.5, 0.16, 0.22), "WallDark"))
    for sx in (1, -1):
        parts.append(box((sx * (door_w * 0.5 + 0.12), y + door_side * 0.04, base_h + door_h * 0.5), (0.2, 0.16, door_h + 0.1), "WallDark"))
    if hall:
        parts.append(box((0, y + door_side * 0.07, base_h + door_h * 0.5), (0.08, 0.06, door_h), "WallDark"))
        parts.append(box((0, door_side * (depth * 0.5 + 0.7), base_h * 0.5), (door_w + 1.6, 1.2, base_h), "Stone"))
        for i, sx in enumerate((-3.6, -1.9, 1.9, 3.6, -5.6, 5.6)):
            disc = [Vector((sx, y + door_side * 0.02, base_h + 1.9)) + Vector((math.cos(a) * 0.55, 0, math.sin(a) * 0.55)) for a in
                    [2 * math.pi * k / 10 for k in range(10)]]
            center = Vector((sx, y + door_side * 0.08, base_h + 1.9))
            verts = disc + [center]
            faces = [((k + 1) % 10, k, 10) if door_side > 0 else (k, (k + 1) % 10, 10) for k in range(10)]
            parts.append(Part(verts, faces, "ShieldA" if i % 2 == 0 else "ShieldB"))
            parts.append(blob(center + Vector((0, door_side * 0.03, 0)), 0.12, seed + i, "Gold", subdiv=1, bumps=0.0, squash=(1, 0.5, 1)))
    else:
        wx = rnd.choice((-2.2, 2.2))
        parts.append(box((wx, y, base_h + 1.4), (0.8, 0.08, 0.6), "Window"))
        parts.append(box((wx, y + door_side * 0.03, base_h + 1.4), (0.95, 0.06, 0.08), "WallDark"))
    # smoke hole on the ridge
    parts.append(box((length * 0.15, 0, ridge + 0.55), (0.7, 0.7, 0.4), "RoofDark"))
    return parts


def stall(seed):
    """A market stall: a plank counter, four posts, a striped awning and goods (crates, a barrel, sacks, apples)."""
    rnd = random.Random(seed)
    parts = [box((0, 0, 0.5), (2.8, 1.2, 1.0), "Wood"), box((0, -0.05, 1.03), (3.0, 1.35, 0.1), "WoodDark")]
    for sx in (1.35, -1.35):
        for sy, h in ((0.55, 2.5), (-0.55, 2.2)):
            parts.append(box((sx, sy, h * 0.5), (0.12, 0.12, h), "WoodDark"))
    stripes = 6
    for i in range(stripes):
        x0 = -1.6 + 3.2 * i / stripes
        x1 = x0 + 3.2 / stripes
        lower = [(x0, -0.95, 2.12), (x1, -0.95, 2.12), (x1, 0.75, 2.55), (x0, 0.75, 2.55)]
        upper = [(x, y, z + 0.05) for x, y, z in lower]
        parts.append(hexa(lower + upper, "Awning" if i % 2 == 0 else "AwningStripe"))
    parts.append(tube([Vector((-0.9, 0.1, 1.08)), Vector((-0.9, 0.1, 1.5))], [0.22, 0.2], "Wood", sides=10))
    parts.append(box((0.2, 0.15, 1.28), (0.6, 0.45, 0.4), "Crate"))
    parts.append(box((0.85, 0.2, 1.22), (0.45, 0.4, 0.3), "Crate"))
    parts.append(blob(Vector((-0.4, -0.25, 1.2)), 0.22, seed, "Sack", subdiv=1, bumps=0.15, squash=(1, 1, 1.2)))
    for k in range(5):
        parts.append(blob(Vector((0.85 + rnd.uniform(-0.15, 0.15), 0.2 + rnd.uniform(-0.12, 0.12), 1.42)), 0.07, seed + k, "Goods", subdiv=1,
                          bumps=0.0))
    return parts


def well(seed):
    """A village well: a ring of stone blocks, dark water, two posts with a little roof, a crank and a bucket."""
    parts = []
    blocks = 12
    for k in range(blocks):
        a = 2 * math.pi * k / blocks
        parts.append(box((math.cos(a) * 0.95, math.sin(a) * 0.95, 0.42), (0.55, 0.32, 0.84), "Stone" if k % 2 else "StoneDark",
                         yaw=math.degrees(a) + 90))
    water = [Vector((math.cos(2 * math.pi * k / 16) * 0.8, math.sin(2 * math.pi * k / 16) * 0.8, 0.7)) for k in range(16)] + [Vector((0, 0, 0.7))]
    parts.append(Part(water, [(k, (k + 1) % 16, 16) for k in range(16)], "Water"))
    for sx in (1, -1):
        parts.append(box((sx * 0.95, 0, 1.4), (0.14, 0.14, 1.7), "WoodDark"))
    parts += [p for p in gable_roof(1.9, 1.3, 2.2, 2.75, 0.08, "Roof", "RoofDark", overhang=0.2, end_overhang=0.15)]
    parts.append(tube([Vector((-0.95, 0, 1.9)), Vector((0.95, 0, 1.9))], [0.07, 0.07], "Wood", sides=8))
    parts.append(tube([Vector((0.2, 0, 1.85)), Vector((0.2, 0, 1.35))], [0.012, 0.012], "Rope", sides=4, cap=False))
    parts.append(tube([Vector((0.2, 0, 1.15)), Vector((0.2, 0, 1.38))], [0.13, 0.15], "Wood", sides=10))
    return parts


def statue(seed):
    """The Allfather as a wanderer, carved in stone: broad hat, beard, cloak and spear, a raven on each shoulder, one eye
    and the runes on his plinth glowing. Faces +Y."""
    parts = [box((0, 0, 0.3), (2.4, 2.4, 0.6), "StatueDark"), box((0, 0, 0.82), (1.9, 1.9, 0.45), "Statue")]
    parts.append(box((0, 0.96, 0.82), (1.2, 0.04, 0.16), "Runes"))
    robe = [Vector((0, 0, 1.0)), Vector((0, 0, 1.7)), Vector((0, 0, 2.5)), Vector((0, 0, 3.2))]
    parts.append(tube(robe, [0.82, 0.66, 0.5, 0.42], "Statue", sides=12, cap=False))
    parts.append(blob(Vector((0, -0.2, 2.4)), 0.9, seed, "StatueDark", subdiv=2, bumps=0.08, squash=(0.85, 0.45, 1.25)))  # cloak
    parts.append(blob(Vector((0, 0, 3.25)), 0.58, seed + 1, "Statue", subdiv=2, bumps=0.05, squash=(1.35, 0.75, 0.55)))  # shoulders
    head = Vector((0, 0.04, 3.78))
    parts.append(blob(head, 0.3, seed + 2, "Statue", subdiv=2, bumps=0.03, squash=(0.9, 1.0, 1.1)))
    parts.append(blob(head + Vector((0, 0.18, -0.32)), 0.24, seed + 3, "StatueDark", subdiv=2, bumps=0.12, squash=(0.9, 0.6, 1.4)))  # beard
    brim = [Vector((math.cos(2 * math.pi * k / 16) * 0.62, math.sin(2 * math.pi * k / 16) * 0.62, 0)) + head + Vector((0, 0, 0.2)) for k in range(16)]
    parts.append(Part(brim + [head + Vector((0, 0, 0.24))], [((k + 1) % 16, k, 16) for k in range(16)] + [(k, (k + 1) % 16, 16) for k in range(16)],
                      "StatueDark"))
    parts.append(tube([head + Vector((0, 0, 0.2)), head + Vector((0, 0, 0.45)), head + Vector((0.05, -0.05, 0.62))], [0.3, 0.24, 0.06], "StatueDark",
                      sides=10))
    parts.append(blob(head + Vector((0.11, 0.27, 0.06)), 0.045, seed, "Runes", subdiv=1, bumps=0.0))  # the one eye
    # the spear arm and the spear
    parts.append(tube([Vector((0.5, 0.05, 3.2)), Vector((0.7, 0.25, 2.75)), Vector((0.75, 0.3, 2.45))], [0.16, 0.13, 0.12], "Statue", sides=8))
    parts.append(tube([Vector((0.78, 0.32, 1.05)), Vector((0.78, 0.32, 5.0))], [0.06, 0.05], "StatueDark", sides=6))
    parts.append(tube([Vector((0.78, 0.32, 4.95)), Vector((0.78, 0.32, 5.35)), Vector((0.78, 0.32, 5.6))], [0.11, 0.08, 0.01], "Statue", sides=4))
    # Huginn and Muninn
    for sx in (1, -1):
        bird = Vector((sx * 0.55, -0.05, 3.55))
        parts.append(blob(bird, 0.16, seed + 5 + sx, "StatueDark", subdiv=1, bumps=0.05, squash=(0.8, 1.3, 0.9)))
        parts.append(blob(bird + Vector((0, 0.18, 0.12)), 0.1, seed + 7 + sx, "StatueDark", subdiv=1, bumps=0.0))
        parts.append(tube([bird + Vector((0, 0.26, 0.12)), bird + Vector((0, 0.38, 0.1))], [0.035, 0.005], "Statue", sides=4))
    return parts


MODELS = {
    "tree_round_0": lambda: tree_round(11), "tree_round_1": lambda: tree_round(23), "tree_round_2": lambda: tree_round(37),
    "tree_pine_0": lambda: tree_pine(5), "tree_pine_1": lambda: tree_pine(19),
    "tree_birch_0": lambda: tree_birch(7), "tree_birch_1": lambda: tree_birch(29),
    "tree_dead_0": lambda: tree_dead(13), "tree_dead_1": lambda: tree_dead(41),
    "bush_0": lambda: bush(3), "bush_1": lambda: bush(8),
    "rock_0": lambda: rock(2), "rock_1": lambda: rock(9), "rock_2": lambda: rock(16),
    "stump_0": lambda: stump(4),
    "flowers_0": lambda: flowers(6), "fern_0": lambda: fern(10),
    "tent_0": lambda: tent(3),
    "house_0": lambda: longhouse(21), "house_1": lambda: longhouse(34, length=6.4, depth=5.0, wall_h=2.2, rise=2.2),
    "meadhall_0": lambda: longhouse(8, length=16.0, depth=11.0, wall_h=3.6, rise=4.4, door_side=-1, hall=True),
    "stall_0": lambda: stall(5), "well_0": lambda: well(2), "statue_0": lambda: statue(4),
}


# ---------------------------------------------------------------------------------------------- Blender plumbing
def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for item in list(block):
            block.remove(item)


def hex_rgba(hexstr):
    h = hexstr.lstrip("#")
    srgb = [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]
    return (lin[0], lin[1], lin[2], 1.0)


def slot_material(slot):
    mat = bpy.data.materials.get(slot)
    if mat is None:
        mat = bpy.data.materials.new(slot)
        mat.diffuse_color = hex_rgba(PREVIEW.get(slot, "#888888"))
    return mat


def build(name, parts):
    verts, faces, slots, smooth = [], [], [], []
    order = []
    for part in parts:
        if part.slot not in order:
            order.append(part.slot)
        base = len(verts)
        verts += [tuple(v) for v in part.verts]
        for f in part.faces:
            faces.append(tuple(base + i for i in f))
            slots.append(order.index(part.slot))
            smooth.append(part.smooth)
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    for slot in order:
        mesh.materials.append(slot_material(slot))
    for poly, s, sm in zip(mesh.polygons, slots, smooth):
        poly.material_index = s
        poly.use_smooth = sm
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def export(obj, path):
    for o in list(bpy.context.scene.objects):
        if o is not None:
            o.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"}, bake_anim=False, axis_forward="-Z", axis_up="Y",
                             apply_scale_options="FBX_SCALE_ALL", mesh_smooth_type="OFF", use_mesh_modifiers=True, use_custom_props=False,
                             path_mode="AUTO", embed_textures=False)


def render_preview(objects):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1800
    scene.render.resolution_y = 900
    world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes.get("Background").inputs["Color"].default_value = (0.62, 0.7, 0.8, 1.0)
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
    sun.data.energy = 3.5
    sun.rotation_euler = (math.radians(50), 0, math.radians(-35))
    scene.collection.objects.link(sun)
    ground = bpy.data.meshes.new("Ground")
    ground.from_pydata([(-40, -40, 0), (40, -40, 0), (40, 40, 0), (-40, 40, 0)], [], [(0, 1, 2, 3)])
    gobj = bpy.data.objects.new("Ground", ground)
    ground.materials.append(slot_material("Moss"))
    scene.collection.objects.link(gobj)
    cols = 6
    for i, obj in enumerate(objects):
        obj.location = ((i % cols - (cols - 1) / 2) * 4.2, (i // cols) * 5.0, 0)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.location = (0, -22, 15)
    cam.rotation_euler = (math.radians(58), 0, 0)
    cam.data.lens = 32
    scene.render.filepath = os.path.join(PREVIEW_DIR, "environment_kit.png")
    bpy.ops.render.render(write_still=True)
    print("RH preview", scene.render.filepath)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    clear_scene()
    built = []
    for name, make in MODELS.items():
        if ONLY and name not in ONLY:
            continue
        obj = build(name, make())
        export(obj, os.path.join(OUT_DIR, name + ".fbx"))
        built.append(obj)
        print("RH built %s: %d verts" % (name, len(obj.data.vertices)))
    if RENDER:
        render_preview(built)


main()
