"""Anime hair styles, built around the Head joint (head-local coordinates, the same skull for everyone).

A thin cap hugs the skull and stops at the hairline; locks that follow the skull's surface hide the cap's edge and give
the chunky, layered anime look. Style order matches AvatarLook.HairStyleNames in the game.
"""

import math

from mathutils import Vector

from rh_body import HEAD_E, head_ring
from rh_geo import V, clamp01, hash01, lerp, loft, merge, sgnpow, smooth, tube

HAIR_STYLES = ["Short", "Spiky", "Long", "Ponytail", "Twin Tails", "Bun", "Mohawk", "Viking Braids"]

CROWN = 0.276


def hairline(theta, front=0.178, temple=0.14, side=0.118, behind=0.07, back=0.005):
    """Height of the hairline around the head (theta 0 = front, pi = back)."""
    a = abs(math.atan2(math.sin(theta), math.cos(theta)))
    keys = [(0.0, front), (0.85, temple), (1.5, side), (2.1, behind), (math.pi, back)]
    for (a0, z0), (a1, z1) in zip(keys, keys[1:]):
        if a <= a1:
            return lerp(z0, z1, smooth((a - a0) / (a1 - a0)))
    return back


def skull_at(theta, z, out):
    """A point on the skull at angle theta and height z, pushed out by `out`, and the outward normal there."""
    zc = max(-0.015, min(z, 0.271))
    rx, rf, rb, cy = head_ring(zc)
    s, c = math.sin(theta), math.cos(theta)
    x = sgnpow(s, 2.0 / HEAD_E) * (rx + out)
    y = cy - sgnpow(c, 2.0 / HEAD_E) * ((rf if c >= 0 else rb) + out)
    up = smooth((z - 0.19) / 0.09)
    nrm = (V((s, -c, 0.0)) * (1.0 - 0.85 * up) + V((0.0, 0.0, 1.0)) * up).normalized()
    p = V((x, y, z))
    if z > 0.271:  # over the crown: lift straight up
        p = p + V((0.0, 0.0, z - 0.271))
    return p, nrm


def cap(line=None, out=0.006, n=26, rings=9):
    line = line or hairline
    outer, inner = [], []
    for k in range(rings + 1):
        f = k / rings
        o_ring, i_ring = [], []
        for i in range(n):
            th = 2.0 * math.pi * i / n
            z = lerp(CROWN + 0.006, line(th), f ** 0.9)
            p, nrm = skull_at(th, z, out * (1.0 + 0.5 * (1.0 - f)))
            if k == 0:
                p = V((p.x * 0.25, p.y * 0.25 + 0.012 * 0.75, p.z))
            o_ring.append(p)
            q, _ = skull_at(th, z, 0.0015)
            if k == 0:
                q = V((q.x * 0.2, q.y * 0.2 + 0.012 * 0.8, q.z - 0.004))
            i_ring.append(q)
        outer.append(o_ring)
        inner.append(i_ring)
    return loft(outer + list(reversed(inner)), True, True)


def flow_lock(theta, z0, z1, width, out0=0.009, out1=0.016, ext=0.0, ext_dir=None, swirl=0.0, flick=0.0, segs=6, thick=0.38,
              taper=1.5, root_w=0.55):
    """A lock that follows the skull from z0 down to z1 (swirling by `swirl` radians), then hangs `ext` further."""
    pts, hints = [], []
    for k in range(segs + 1):
        t = k / segs
        p, nrm = skull_at(theta + swirl * smooth(t), lerp(z0, z1, t), lerp(out0, out1, t))
        pts.append(p)
        hints.append(nrm)
    if ext > 0.0:
        d = (pts[-1] - pts[-2]).normalized()
        if ext_dir is not None:
            d = (d * 0.4 + V(ext_dir).normalized()).normalized()
        last, n_last = pts[-1], hints[-1]
        steps = 4
        for k in range(1, steps + 1):
            t = k / steps
            dd = (d + n_last * (flick * t)).normalized()
            last = last + dd * (ext / steps)
            pts.append(last.copy())
            hints.append(n_last)
    count = len(pts)
    prof = []
    for k in range(count):
        t = k / (count - 1)
        w = width * (root_w + (1.0 - root_w) * smooth(min(1.0, t * 3.0))) * (1.0 - 0.94 * t ** taper)
        prof.append((w, w * thick, w * thick))
    return tube(pts, prof, n=6, fwd_hint=hints)


def ring_of_locks(count, a0, a1, z0, z1_fn, width, **kw):
    out = []
    for i in range(count):
        t = (i + 0.5) / count
        th = lerp(a0, a1, t)
        j = hash01(i, a0, a1)
        out.append(flow_lock(th, z0 - 0.01 * j, z1_fn(th) - 0.012 * j, width * (0.9 + 0.2 * j), **kw))
    return merge(*out)


def fringe(count=6, spread=0.8, tip_z=0.145, width=0.046, sweep=0.0, ext=0.025, parted=False, out1=0.015):
    locks = []
    for i in range(count):
        t = -1.0 + 2.0 * (i + 0.5) / count
        th = t * spread
        sw = sweep if not parted else (0.18 * math.copysign(1.0, t) * abs(t) ** 0.5)
        j = hash01(i, 31)
        locks.append(flow_lock(th - sw * 0.6, 0.262 - 0.012 * abs(t), tip_z + 0.012 * abs(t) - 0.01 * j, width * (0.9 + 0.2 * j),
                               out0=0.01, out1=out1, swirl=sw, ext=ext * (0.8 + 0.4 * j), ext_dir=(0.0, -0.3, -1.0), segs=6))
    return merge(*locks)


def side_locks(length=0.06, z1=0.075, width=0.04, spread=0.0):
    out = []
    for side in (1, -1):
        th = side * (1.2 + spread)
        out.append(flow_lock(th, 0.235, z1, width, out0=0.012, out1=0.016, ext=length, ext_dir=(side * 0.1, -0.15, -1.0)))
    return merge(*out)


def braid(start, end, r, beads=10, n=6):
    start, end = V(start), V(end)
    axis = end - start
    side_v = axis.normalized().cross(V((0.0, 1.0, 0.0))).normalized()
    parts = []
    for k in range(beads):
        t = (k + 0.5) / beads
        c = start.lerp(end, t) + side_v * (r * 0.28 * (1 if k % 2 else -1))
        rr = r * (1.0 - 0.4 * t)
        step = axis * (0.62 / beads)
        parts.append(tube([c - step, c, c + step], [rr * 0.45, rr, rr * 0.45], n=n))
    tip = end + axis.normalized() * 0.03
    parts.append(tube([end - axis.normalized() * 0.005, tip], [r * 0.45, r * 0.08], n=n))
    return merge(*parts)


def tie(center, direction, r=0.024):
    c, d = V(center), V(direction).normalized()
    return tube([c - d * 0.012, c, c + d * 0.012], [r * 0.9, r, r * 0.9], n=10)


def hair_style(index):
    style = HAIR_STYLES[index]
    back_line = lambda th: hairline(th) - 0.02
    if style == "Short":
        return merge(cap(),
                     ring_of_locks(12, 0.75, 2 * math.pi - 0.75, CROWN, back_line, 0.055, ext=0.025, ext_dir=(0, 0, -1), out1=0.014),
                     fringe(6, 0.8, 0.148, 0.046, sweep=0.12))
    if style == "Spiky":
        spikes = []
        for i in range(6):
            th = math.pi + (-0.9 + 1.8 * i / 5)
            spikes.append(flow_lock(th, CROWN, 0.20, 0.06, out1=0.02, ext=0.075 + 0.02 * hash01(i, 5), ext_dir=(math.sin(th) * 0.4, 0.7, 0.55),
                                    flick=0.9))
        return merge(cap(),
                     ring_of_locks(12, 0.8, 2 * math.pi - 0.8, CROWN, back_line, 0.058, ext=0.05, ext_dir=(0, 0.2, -1), flick=0.9,
                                   out1=0.016),
                     merge(*spikes),
                     fringe(7, 0.85, 0.135, 0.05, sweep=0.28, ext=0.04),
                     side_locks(0.05, 0.08, 0.042))
    if style == "Long":
        return merge(cap(),
                     ring_of_locks(13, 1.25, 2 * math.pi - 1.25, CROWN, back_line, 0.07, ext=0.3, ext_dir=(0, 0.12, -1), out1=0.018,
                                   thick=0.3),
                     fringe(6, 0.8, 0.14, 0.05, parted=True, ext=0.02),
                     side_locks(0.24, 0.08, 0.05, spread=0.05))
    if style == "Ponytail":
        tie_p = V((0.0, 0.122, 0.175))
        tail = [flow_lock(math.pi, 0.2, 0.17, 0.075, out0=0.02, out1=0.03, ext=0.32, ext_dir=(0.0, 0.25, -1.0), thick=0.55, root_w=0.9)]
        return merge(cap(),
                     ring_of_locks(10, 1.4, 2 * math.pi - 1.4, CROWN, lambda th: max(0.13, hairline(th) - 0.01), 0.06, out1=0.013),
                     merge(*tail), tie(tie_p, (0.0, 0.6, -0.8)),
                     fringe(6, 0.8, 0.145, 0.046, sweep=-0.1),
                     side_locks(0.11, 0.08, 0.04))
    if style == "Twin Tails":
        tails = []
        for side in (1, -1):
            root = V((side * 0.088, 0.055, 0.205))
            th = side * 1.95
            tails.append(flow_lock(th, 0.235, 0.19, 0.07, out0=0.02, out1=0.03, ext=0.3, ext_dir=(side * 0.35, 0.1, -1.0), thick=0.55,
                                   root_w=0.9))
            tails.append(tie(root, (side, 0.0, 0.0)))
        return merge(cap(),
                     ring_of_locks(10, 1.3, 2 * math.pi - 1.3, CROWN, back_line, 0.055, ext=0.03, ext_dir=(0, 0, -1), out1=0.013),
                     merge(*tails), fringe(6, 0.8, 0.145, 0.046, parted=True), side_locks(0.09, 0.08, 0.04))
    if style == "Bun":
        c = V((0.0, 0.105, 0.245))
        bun = tube([c + V((0.0, -0.03, 0.0)), c + V((0.0, 0.0, 0.0)), c + V((0.0, 0.03, 0.0))], [0.035, 0.052, 0.03], n=12)
        return merge(cap(),
                     ring_of_locks(10, 1.3, 2 * math.pi - 1.3, CROWN, lambda th: max(0.12, hairline(th)), 0.058, out1=0.013),
                     bun, fringe(6, 0.8, 0.145, 0.046, sweep=0.1), side_locks(0.1, 0.08, 0.038))
    if style == "Mohawk":
        crest_line = lambda th: 0.2
        locks = []
        for i in range(8):
            t = i / 7
            th = math.pi * t
            z = lerp(0.235, 0.17, t)
            p = (0.0, lerp(-0.07, 0.105, t), lerp(0.262, 0.205, t * t))
            locks.append(flow_lock(math.pi * t, CROWN - 0.005, lerp(0.25, 0.2, t), 0.06, out0=0.006, out1=0.012, ext=0.07 - 0.02 * t,
                                   ext_dir=(0.0, 0.5 + t, 0.9 - t), flick=0.6, thick=0.45))
        strip = cap(lambda th: 0.245 if math.cos(th) > -0.2 else 0.2, out=0.004)
        return merge(strip, merge(*locks))
    # Viking Braids: swept back, two braids in front of the ears
    braids = [braid(V((s * 0.094, -0.015, 0.075)), V((s * 0.1, -0.04, -0.13)), 0.022) for s in (1, -1)]
    swept = []
    for i in range(7):
        th = -0.75 + 1.5 * i / 6
        swept.append(flow_lock(th, 0.2, CROWN, 0.05, out0=0.012, out1=0.01, segs=5))
    return merge(cap(lambda th: hairline(th, front=0.19)),
                 ring_of_locks(12, 0.7, 2 * math.pi - 0.7, CROWN, back_line, 0.06, ext=0.07, ext_dir=(0, 0.1, -1), out1=0.015),
                 merge(*swept), merge(*braids))


def lock(root, direction, length, width, curl=0.0, droop=0.0, twist_side=None, n=6, segs=5, flat=0.55):
    """A free-standing tapered lock (also used for feathers)."""
    d = V(direction).normalized()
    side = twist_side if twist_side is not None else d.cross(V((0.0, 0.0, 1.0)))
    if side.length < 1e-4:
        side = V((1.0, 0.0, 0.0))
    side = side.normalized()
    pts = []
    for k in range(segs + 1):
        t = k / segs
        pts.append(V(root) + d * (length * t) + V((0.0, 0.0, -droop)) * (t * t) + side * (curl * t * t))
    prof = [(width * (1.0 - 0.92 * (k / segs) ** 1.3), width * flat * (1.0 - 0.9 * (k / segs)), width * flat * (1.0 - 0.9 * (k / segs)))
            for k in range(segs + 1)]
    return tube(pts, prof, n=n, e=2.0, fwd_hint=tuple(side.cross(d)))
