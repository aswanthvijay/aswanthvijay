"""Geometry helpers for the Runeheir character builder (Blender 5.2, bpy + mathutils only).

Conventions: Blender space, Z up, the character faces -Y (front), its left side is +X.
Every helper returns (verts, faces) as plain lists; rh_build.Builder adds them to a mesh with a
material slot and bone weights. Winding doesn't matter: the builder recalculates normals per island.
"""

import math

from mathutils import Vector


def V(*xyz):
    if len(xyz) == 1:
        return Vector(xyz[0])
    return Vector(xyz)


def lerp(a, b, t):
    return a + (b - a) * t


def clamp01(t):
    return max(0.0, min(1.0, t))


def smooth(t):
    t = clamp01(t)
    return t * t * (3.0 - 2.0 * t)


def sgnpow(value, power):
    return math.copysign(abs(value) ** power, value)


def hash01(*keys):
    """Deterministic 0..1 noise from integers/floats (art must rebuild identically)."""
    h = 2166136261
    for k in keys:
        for ch in repr(round(k, 5) if isinstance(k, float) else k):
            h = ((h ^ ord(ch)) * 16777619) & 0xFFFFFFFF
    return (h % 10007) / 10007.0


def ring(center, side, fwd, rx, rf, rb, n, e=2.0, phase=0.0, radial=None):
    """Superellipse ring around `center`: half-width rx along `side`, rf toward `fwd`, rb away from it.
    a=0 is the front. `radial(i, a)` may return a multiplier per vertex (folds, fur jags)."""
    pts = []
    for i in range(n):
        a = 2.0 * math.pi * i / n + phase
        sx, cy = math.sin(a), math.cos(a)
        x = sgnpow(sx, 2.0 / e) * rx
        f = sgnpow(cy, 2.0 / e) * (rf if cy >= 0.0 else rb)
        k = radial(i, a) if radial else 1.0
        pts.append(center + side * (x * k) + fwd * (f * k))
    return pts


def loft(rings, cap0=True, cap1=True):
    n = len(rings[0])
    verts = [v.copy() for r in rings for v in r]
    faces = []
    for k in range(len(rings) - 1):
        for i in range(n):
            a = k * n + i
            b = k * n + (i + 1) % n
            c = (k + 1) * n + (i + 1) % n
            d = (k + 1) * n + i
            faces.append([a, b, c, d])
    if cap0:
        faces.append(list(reversed(range(n))))
    if cap1:
        last = (len(rings) - 1) * n
        faces.append([last + i for i in range(n)])
    return verts, faces


def frames(path, fwd_hint):
    """Tangent, side and front vectors along a polyline (front stays as close to fwd_hint as it can).
    fwd_hint may be one vector or a list with one per point."""
    out = []
    count = len(path)
    hints = fwd_hint if isinstance(fwd_hint, list) else None
    for i in range(count):
        if hints:
            fwd_hint = Vector(hints[i])
        if i == 0:
            t = path[1] - path[0]
        elif i == count - 1:
            t = path[-1] - path[-2]
        else:
            t = (path[i + 1] - path[i]).normalized() + (path[i] - path[i - 1]).normalized()
        t = t.normalized()
        f = fwd_hint - t * fwd_hint.dot(t)
        if f.length < 1e-6:
            f = Vector((0.0, 0.0, 1.0)) - t * t.z
        f = f.normalized()
        s = t.cross(f).normalized()
        out.append((t, s, f))
    return out


def tube(path, profile, n=12, e=2.0, fwd_hint=(0.0, -1.0, 0.0), cap0=True, cap1=True, radial=None):
    """Loft rings along a polyline. profile[i] = r or (rx, rf, rb) for path point i."""
    path = [Vector(p) for p in path]
    fr = frames(path, fwd_hint if isinstance(fwd_hint, list) else Vector(fwd_hint))
    rings = []
    for i, (p, (t, s, f)) in enumerate(zip(path, fr)):
        pr = profile[i]
        if not isinstance(pr, (tuple, list)):
            pr = (pr, pr, pr)
        rx, rf, rb = pr
        rings.append(ring(p, s, f, rx, rf, rb, n, e, radial=(lambda j, a, i=i: radial(i, j, a)) if radial else None))
    return loft(rings, cap0, cap1)


def resample(points, count):
    """`count` evenly spaced points along a polyline."""
    pts = [Vector(p) for p in points]
    lengths = [0.0]
    for a, b in zip(pts, pts[1:]):
        lengths.append(lengths[-1] + (b - a).length)
    total = lengths[-1]
    out = []
    for k in range(count):
        d = total * k / (count - 1)
        j = 1
        while j < len(lengths) - 1 and lengths[j] < d:
            j += 1
        seg = lengths[j] - lengths[j - 1]
        t = 0.0 if seg <= 0 else (d - lengths[j - 1]) / seg
        out.append(pts[j - 1].lerp(pts[j], t))
    return out


def sheet(nu, nv, place, thick):
    """A closed slab: place(u, v) -> (point, normal) for u, v in 0..1; thickness grows both ways along the normal."""
    top, bottom = [], []
    for j in range(nv + 1):
        for i in range(nu + 1):
            p, nrm = place(i / nu, j / nv)
            nrm = Vector(nrm).normalized()
            top.append(Vector(p) + nrm * (thick * 0.5))
            bottom.append(Vector(p) - nrm * (thick * 0.5))
    verts = top + bottom
    off = len(top)
    w = nu + 1
    faces = []
    for j in range(nv):
        for i in range(nu):
            a = j * w + i
            faces.append([a, a + 1, a + 1 + w, a + w])
            faces.append([off + a, off + a + w, off + a + 1 + w, off + a + 1])
    for j in range(nv):  # sides u = 0 and u = 1
        a, b = j * w, (j + 1) * w
        faces.append([a, b, off + b, off + a])
        a, b = j * w + nu, (j + 1) * w + nu
        faces.append([a, off + a, off + b, b])
    for i in range(nu):  # ends v = 0 and v = 1
        a = i
        faces.append([a, off + a, off + a + 1, a + 1])
        a = nv * w + i
        faces.append([a, a + 1, off + a + 1, off + a])
    return verts, faces


def strap(points, width, thick, normals, segments=None):
    """A flat band along `points`, lying on the surface whose outward normals are given per point."""
    pts = [Vector(p) for p in points]
    nrms = [Vector(n).normalized() for n in normals]
    count = len(pts)

    def place(u, v):
        x = v * (count - 1)
        j = min(int(x), count - 2)
        t = x - j
        p = pts[j].lerp(pts[j + 1], t)
        nrm = nrms[j].lerp(nrms[j + 1], t).normalized()
        tangent = (pts[j + 1] - pts[j]).normalized()
        across = nrm.cross(tangent).normalized()
        w = width(v) if callable(width) else width
        return p + across * ((u - 0.5) * w), nrm

    return sheet(1, segments or (count - 1), place, thick)


def dome(center, axis, fwd, r, theta_max, thick, n=14, rings=5, squash=(1.0, 1.0, 1.0), lip=0.0):
    """A closed spherical-cap shell (pauldron lames, knee cops). axis = cap direction, squash = (side, front, axis)."""
    axis = Vector(axis).normalized()
    fwd = (Vector(fwd) - axis * Vector(fwd).dot(axis)).normalized()
    side = axis.cross(fwd).normalized()
    sx, sf, sa = squash
    outer, inner = [], []
    for k in range(rings + 1):
        th = theta_max * (0.08 + 0.92 * k / rings)
        rr = r * math.sin(th)
        h = r * math.cos(th)
        ring_pts = []
        ring_in = []
        for i in range(n):
            a = 2.0 * math.pi * i / n
            d = side * (math.sin(a) * sx) + fwd * (math.cos(a) * sf)
            ring_pts.append(center + d * rr + axis * (h * sa))
            ring_in.append(center + d * max(rr - thick, 0.0) * 0.98 + axis * ((h - thick) * sa))
        outer.append(ring_pts)
        inner.append(ring_in)
    if lip:
        last = outer[-1]
        outer.append([center + (p - center) * (1.0 + lip) - axis * (thick * 0.6) for p in last])
    rings_all = outer + list(reversed(inner))
    return loft(rings_all, True, True)


def cone(base, tip, r, n=6):
    base, tip = Vector(base), Vector(tip)
    mid = base.lerp(tip, 0.55)
    return tube([base, mid, tip], [r, r * 0.55, r * 0.04], n=n, fwd_hint=(0.0, 0.0, 1.0) if abs((tip - base).normalized().z) < 0.9 else (0.0, -1.0, 0.0))


def merge(*parts):
    verts, faces = [], []
    for v, f in parts:
        base = len(verts)
        verts.extend(v)
        faces.extend([[base + i for i in face] for face in f])
    return verts, faces


def transform(part, fn):
    v, f = part
    return [fn(Vector(p)) for p in v], f
