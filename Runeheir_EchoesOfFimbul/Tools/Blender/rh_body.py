"""The shared Runeheir body: skeleton (Unity Humanoid bone names), torso and limb profiles, skin, the anime head and hair.

Proportions follow the concept art (about six heads tall): a 1.75 m man, a 1.69 m woman. The head is the same mesh
for both, built around the Head joint, so hair styles fit everyone.
"""

import math

from mathutils import Vector

from rh_geo import V, clamp01, lerp, loft, merge, ring, sgnpow, sheet, smooth, strap, tube

FRONT = Vector((0.0, -1.0, 0.0))
UP = Vector((0.0, 0.0, 1.0))

# Male joints (head, tail); the right side mirrors the left. Parents follow PARENTS.
_MALE = {
    "Hips": ((0.0, 0.0, 0.95), (0.0, 0.0, 1.02)),
    "Spine": ((0.0, 0.0, 1.02), (0.0, 0.0, 1.16)),
    "Chest": ((0.0, 0.0, 1.16), (0.0, 0.0, 1.38)),
    "Neck": ((0.0, 0.012, 1.40), (0.0, 0.012, 1.48)),
    "Head": ((0.0, 0.012, 1.48), (0.0, 0.012, 1.73)),
    "LeftShoulder": ((0.03, 0.0, 1.36), (0.165, 0.005, 1.385)),
    "LeftUpperArm": ((0.185, 0.01, 1.375), (0.24, 0.015, 1.09)),
    "LeftLowerArm": ((0.24, 0.015, 1.09), (0.272, -0.02, 0.835)),
    "LeftHand": ((0.272, -0.02, 0.835), (0.282, -0.03, 0.725)),
    "LeftUpperLeg": ((0.092, 0.0, 0.915), (0.1, -0.005, 0.5)),
    "LeftLowerLeg": ((0.1, -0.005, 0.5), (0.106, 0.015, 0.085)),
    "LeftFoot": ((0.106, 0.015, 0.085), (0.112, -0.08, 0.025)),
    "LeftToes": ((0.112, -0.08, 0.025), (0.112, -0.14, 0.02)),
}

PARENTS = {
    "Hips": None, "Spine": "Hips", "Chest": "Spine", "Neck": "Chest", "Head": "Neck",
    "LeftShoulder": "Chest", "LeftUpperArm": "LeftShoulder", "LeftLowerArm": "LeftUpperArm", "LeftHand": "LeftLowerArm",
    "LeftUpperLeg": "Hips", "LeftLowerLeg": "LeftUpperLeg", "LeftFoot": "LeftLowerLeg", "LeftToes": "LeftFoot",
}

HEAD_LEN = 0.25  # Head joint to tail; the skull reaches 0.275 above the joint
FEMALE_HEAD_Z = 1.445  # 1.72 m tall


def mirror_name(name):
    return name.replace("Left", "Right") if name.startswith("Left") else name


class Proportions:
    """Joint positions, torso and limb profiles for one body type."""

    def __init__(self, female):
        self.female = female
        self.joints = {}
        zs = 0.955 if female else 1.0
        for name, (h, t) in _MALE.items():
            if female:
                fx = 0.9 if ("Shoulder" in name or "Arm" in name or "Hand" in name) else (1.05 if ("Leg" in name or "Foot" in name or "Toes" in name) else 1.0)
                h = (h[0] * fx, h[1], h[2] * zs)
                t = (t[0] * fx, t[1], t[2] * zs)
            if female and name in ("Neck", "Head"):
                # the same skull on a smaller body: keep a slender neck rather than scaling the head down
                if name == "Neck":
                    t = (t[0], t[1], FEMALE_HEAD_Z)
                else:
                    h = (h[0], h[1], FEMALE_HEAD_Z)
            if name == "Head":  # same skull for everyone
                t = (h[0], h[1], h[2] + HEAD_LEN)
            self.joints[name] = (V(h), V(t))
            if name.startswith("Left"):
                self.joints[mirror_name(name)] = (V((-h[0], h[1], h[2])), V((-t[0], t[1], t[2])))
        self.zs = zs
        self.head_origin = self.joints["Head"][0].copy()
        # Torso profile: (z, half-width, front, back). Superellipse exponent TORSO_E.
        if female:
            self.torso = [
                (0.83, 0.150, 0.090, 0.105), (0.88, 0.166, 0.095, 0.118), (0.945, 0.168, 0.096, 0.120),
                (1.01, 0.144, 0.086, 0.102), (1.065, 0.122, 0.080, 0.084), (1.125, 0.134, 0.090, 0.084),
                (1.185, 0.150, 0.118, 0.088), (1.24, 0.156, 0.120, 0.090), (1.285, 0.152, 0.098, 0.088),
                (1.32, 0.142, 0.080, 0.082), (1.35, 0.110, 0.064, 0.068), (1.375, 0.062, 0.048, 0.050),
                (1.395, 0.040, 0.038, 0.040),
            ]
            self.arm = [0.041, 0.036, 0.034, 0.030, 0.025]       # shoulder, mid upper, elbow, mid fore, wrist
            self.leg = [0.088, 0.072, 0.054, 0.056, 0.038]       # hip, mid thigh, knee, calf, ankle
            self.neck_r = 0.035
        else:
            self.torso = [
                (0.84, 0.148, 0.092, 0.098), (0.90, 0.162, 0.098, 0.112), (0.965, 0.156, 0.094, 0.108),
                (1.03, 0.138, 0.088, 0.094), (1.09, 0.142, 0.092, 0.092), (1.16, 0.158, 0.104, 0.095),
                (1.23, 0.178, 0.114, 0.100), (1.29, 0.188, 0.112, 0.100), (1.34, 0.178, 0.096, 0.094),
                (1.375, 0.140, 0.075, 0.078), (1.40, 0.090, 0.058, 0.062), (1.42, 0.052, 0.046, 0.048),
            ]
            self.arm = [0.049, 0.044, 0.039, 0.038, 0.029]
            self.leg = [0.090, 0.074, 0.056, 0.060, 0.040]
            self.neck_r = 0.042

    TORSO_E = 2.5

    def j(self, name, end=0):
        return self.joints[name][end].copy()

    def at(self, name, t):
        h, tl = self.joints[name]
        return h.lerp(tl, t)

    def profile(self, z):
        p = self.torso
        if z <= p[0][0]:
            return p[0][1:]
        for a, b in zip(p, p[1:]):
            if z <= b[0]:
                t = (z - a[0]) / (b[0] - a[0])
                return tuple(lerp(a[k], b[k], t) for k in range(1, 4))
        return p[-1][1:]

    @property
    def neck_base(self):
        return self.torso[-1][0]

    def surface(self, a, z, add=0.0):
        """Point and outward normal on the torso at angle a (0 = front, +90 deg = the character's left) and height z."""
        rx, rf, rb = self.profile(z)
        e = self.TORSO_E
        sx, cy = math.sin(a), math.cos(a)
        x = sgnpow(sx, 2.0 / e) * (rx + add)
        f = sgnpow(cy, 2.0 / e) * ((rf if cy >= 0 else rb) + add)
        p = V((x, -f, z))
        nrm = V((sx / max(rx, 1e-3), -cy / max(rf if cy >= 0 else rb, 1e-3), 0.0)).normalized()
        return p, nrm

    # ------------------------------------------------------------------ weights
    def torso_weights(self, p):
        zs = self.zs
        cuts = [(-9.0, "Hips"), (0.995 * zs, "Spine"), (1.15 * zs, "Chest"), (self.neck_base + 0.004, "Neck")]
        return zcut_weights(p.z, cuts, 0.035)

    def chain_weights(self, p, chain, blend=0.05):
        best = None
        for idx, name in enumerate(chain):
            h, t = self.joints[name]
            d = t - h
            L2 = max(d.length_squared, 1e-9)
            tt = clamp01((p - h).dot(d) / L2)
            dist = (p - (h + d * tt)).length
            if best is None or dist < best[0] - 1e-6:
                best = (dist, idx, tt, math.sqrt(L2))
        _, idx, tt, L = best
        w = {chain[idx]: 1.0}
        if idx > 0 and tt * L < blend:
            k = 0.5 * (1.0 - tt * L / blend)
            w = {chain[idx]: 1.0 - k, chain[idx - 1]: k}
        elif idx < len(chain) - 1 and (1.0 - tt) * L < blend:
            k = 0.5 * (1.0 - (1.0 - tt) * L / blend)
            w = {chain[idx]: 1.0 - k, chain[idx + 1]: k}
        return w

    def arm_chain(self, side):
        s = "Left" if side > 0 else "Right"
        return ["Chest", s + "Shoulder", s + "UpperArm", s + "LowerArm", s + "Hand"]

    def leg_chain(self, side):
        s = "Left" if side > 0 else "Right"
        return ["Hips", s + "UpperLeg", s + "LowerLeg", s + "Foot", s + "Toes"]

    # ------------------------------------------------------------------ limb paths
    def arm_path(self, side, t0=0.0, t1=1.0, count=9):
        """Shoulder → wrist, t in 0..1 over the two arm bones (0.5 = elbow)."""
        s = "Left" if side > 0 else "Right"
        a, b = self.joints[s + "UpperArm"]
        c = self.joints[s + "LowerArm"][1]
        pts = []
        for k in range(count):
            t = lerp(t0, t1, k / (count - 1))
            pts.append(a.lerp(b, t * 2.0) if t <= 0.5 else b.lerp(c, (t - 0.5) * 2.0))
        return pts

    def arm_radius(self, t):
        r = self.arm
        keys = [0.0, 0.25, 0.5, 0.75, 1.0]
        for k in range(4):
            if t <= keys[k + 1]:
                return lerp(r[k], r[k + 1], (t - keys[k]) / 0.25)
        return r[-1]

    def leg_path(self, side, t0=0.0, t1=1.0, count=9):
        """Hip → ankle, 0.5 = knee."""
        s = "Left" if side > 0 else "Right"
        a, b = self.joints[s + "UpperLeg"]
        c = self.joints[s + "LowerLeg"][1]
        pts = []
        for k in range(count):
            t = lerp(t0, t1, k / (count - 1))
            pts.append(a.lerp(b, t * 2.0) if t <= 0.5 else b.lerp(c, (t - 0.5) * 2.0))
        return pts

    def leg_radius(self, t):
        r = self.leg
        # hip, mid thigh, knee, calf (just below the knee, bulging back), ankle
        keys = [0.0, 0.25, 0.5, 0.62, 1.0]
        for k in range(4):
            if t <= keys[k + 1]:
                return lerp(r[k], r[k + 1], (t - keys[k]) / (keys[k + 1] - keys[k]))
        return r[-1]


def zcut_weights(z, cuts, blend):
    """cuts: [(z_start, bone)...] ascending. Linear blend of +-blend around each start."""
    w = {}
    for i, (z0, bone) in enumerate(cuts):
        lo = z0
        hi = cuts[i + 1][0] if i + 1 < len(cuts) else 99.0
        a = 1.0 if i == 0 else smooth((z - (lo - blend)) / (2 * blend))
        b = 1.0 if i + 1 == len(cuts) else 1.0 - smooth((z - (hi - blend)) / (2 * blend))
        val = min(a, b)
        if val > 1e-4:
            w[bone] = val
    total = sum(w.values()) or 1.0
    return {k: v / total for k, v in w.items()}


def rigid(bone):
    return lambda p: {bone: 1.0}


def mix(**weights):
    return lambda p: dict(weights)


# ---------------------------------------------------------------------------------------------- body parts
def torso_shell(P, z0, z1, add, rings=10, n=24, ridges=None, e=None, add_fn=None, open_top=False):
    """A garment or armour shell that follows the torso. ridges: [(z, extra)], add_fn(z) -> extra thickness."""
    e = e or P.TORSO_E
    out = []
    for k in range(rings + 1):
        z = lerp(z0, z1, k / rings)
        rx, rf, rb = P.profile(z)
        extra = add + (add_fn(z) if add_fn else 0.0)
        if ridges:
            for zr, amt in ridges:
                extra += amt * max(0.0, 1.0 - abs(z - zr) / 0.012)
        out.append(ring(V((0.0, 0.0, z)), V((1.0, 0.0, 0.0)), FRONT, rx + extra, rf + extra, rb + extra, n, e))
    return loft(out, True, not open_top)


def limb_shell(path, radius_fn, add, n=12, e=2.0, flare=None, t0=0.0, t1=1.0, ridges=None):
    out_profile = []
    count = len(path)
    for k in range(count):
        t = lerp(t0, t1, k / (count - 1))
        r = radius_fn(t) + add
        if flare:
            r += flare(k / (count - 1))
        if ridges:
            for tr, amt in ridges:
                r += amt * max(0.0, 1.0 - abs(t - tr) / 0.03)
        out_profile.append(r)
    return tube(path, out_profile, n=n, e=e)


def neck(P):
    h = P.j("Neck")
    base = V((0.0, h.y, P.neck_base - 0.05))
    top = P.head_origin + V((0.0, 0.0, 0.05))
    return tube([base, h.lerp(top, 0.5), top], [P.neck_r * 1.05, P.neck_r, P.neck_r * 0.95], n=12)


def hand(P, side, fist=True):
    s = "Left" if side > 0 else "Right"
    h0, h1 = P.joints[s + "Hand"]
    axis = h1 - h0
    k = 0.9 if P.female else 1.0
    palm = tube([h0 - axis * 0.05, h0 + axis * 0.3, h0 + axis * 0.75, h0 + axis * 1.0],
                [(0.020 * k, 0.030 * k, 0.026 * k), (0.024 * k, 0.040 * k, 0.034 * k), (0.024 * k, 0.040 * k, 0.034 * k),
                 (0.016 * k, 0.026 * k, 0.020 * k)], n=10, e=3.0)
    tb = h0 + axis * 0.25 + V((-side * 0.012, -0.034 * k, 0.0))
    thumb = tube([tb, tb + axis * 0.35 + V((-side * 0.006, -0.012, 0.0))], [0.011 * k, 0.008 * k], n=6)
    return merge(palm, thumb)


def legs_bare(P, side, add=0.0):
    return limb_shell(P.leg_path(side, 0.0, 1.0, 11), P.leg_radius, add, n=12)


def arms_bare(P, side, add=0.0, t0=0.0, t1=1.0):
    return limb_shell(P.arm_path(side, t0, t1, 9), P.arm_radius, add, n=12, t0=t0, t1=t1)


# ---------------------------------------------------------------------------------------------- head and face
# Rings of the skull around the Head joint: (z, half-width, front, back, centre y)
_HEAD = [
    (-0.016, 0.010, 0.010, 0.008, -0.064),
    (0.000, 0.030, 0.020, 0.020, -0.052),
    (0.028, 0.056, 0.042, 0.044, -0.032),
    (0.062, 0.078, 0.070, 0.070, -0.012),
    (0.100, 0.091, 0.090, 0.094, -0.002),
    (0.145, 0.098, 0.096, 0.108, 0.004),
    (0.190, 0.096, 0.092, 0.110, 0.008),
    (0.232, 0.083, 0.076, 0.098, 0.010),
    (0.258, 0.058, 0.050, 0.070, 0.012),
    (0.274, 0.024, 0.020, 0.028, 0.012),
]
HEAD_E = 2.2
EYE_Z = 0.098


def head_ring(z):
    p = _HEAD
    if z <= p[0][0]:
        return p[0][1:]
    for a, b in zip(p, p[1:]):
        if z <= b[0]:
            t = (z - a[0]) / (b[0] - a[0])
            return tuple(lerp(a[k], b[k], t) for k in range(1, 5))
    return p[-1][1:]


def face_point(x, z, out=0.0):
    """Point on the front of the face (head-local), pushed `out` along the surface normal."""
    rx, rf, rb, cy = head_ring(z)
    u = min(abs(x) / rx, 0.999)
    y = cy - rf * (1.0 - u ** HEAD_E) ** (1.0 / HEAD_E)
    # normal of the superellipse (x/rx)^e + (y/rf)^e = 1
    nx = math.copysign(u ** (HEAD_E - 1) / rx, x)
    ny = -((cy - y) / rf) ** (HEAD_E - 1) / rf
    # tilt with the profile (forehead leans back, chin forward)
    rx2, rf2, _, cy2 = head_ring(z + 0.01)
    dz = ((cy2 - rf2) - (cy - rf)) / 0.01
    nrm = V((nx, ny, -dz * abs(ny) * 0.6)).normalized()
    return V((x, y, z)) + nrm * out, nrm


def skull():
    rings = [ring(V((0.0, cy, z)), V((1.0, 0.0, 0.0)), FRONT, rx, rf, rb, 20, HEAD_E) for z, rx, rf, rb, cy in _HEAD]
    return loft(rings, True, True)


def face_sheet(cx, cz, half_w, half_h, out, thick=0.0012, nu=6, nv=6, shape=None, tilt=0.0):
    """A feature drawn on the face: an ellipse (or shape(v) -> (left, right) half-widths) at (cx, cz)."""
    def place(u, v):
        vv = -0.97 + 1.94 * v
        if shape:
            left, right = shape(vv)
        else:
            left = right = math.sqrt(max(0.0, 1.0 - vv * vv))
        x = cx + (-left + (left + right) * u) * half_w
        z = cz + vv * half_h + (x - cx) * tilt
        return face_point(x, z, out)

    return sheet(nu, nv, place, thick)


def face_strap(points_xz, width, out, thick=0.0015):
    pts, nrms = [], []
    for x, z in points_xz:
        p, n = face_point(x, z, out)
        pts.append(p)
        nrms.append(n)
    return strap(pts, width, thick, nrms)


def face_features(female):
    """Anime face, head-local. Returns {slot: (verts, faces)}."""
    parts = {"EyeWhite": [], "Eye": [], "Lash": [], "Highlight": [], "Brow": [], "Mouth": [], "Skin": []}
    ex = 0.041
    for side in (1, -1):
        cx = side * ex
        # sclera: almond, flatter at the top
        parts["EyeWhite"].append(face_sheet(cx, EYE_Z, 0.020, 0.015 if not female else 0.017, 0.0016, nu=8, nv=6))
        # iris: tall oval set a little toward the nose
        parts["Eye"].append(face_sheet(cx - side * 0.002, EYE_Z - 0.001, 0.0125, 0.0175 if not female else 0.019, 0.0030))
        parts["Lash"].append(face_sheet(cx - side * 0.002, EYE_Z - 0.002, 0.006, 0.009, 0.0042))  # pupil
        parts["Highlight"].append(face_sheet(cx + side * 0.004, EYE_Z + 0.007, 0.0042, 0.0042, 0.0056, nu=4, nv=4))
        # upper lash line: a thick arc with an outward flick
        top = EYE_Z + (0.016 if not female else 0.018)
        arc = [(cx - side * 0.022, EYE_Z + 0.004), (cx - side * 0.012, top), (cx + side * 0.004, top + 0.002),
               (cx + side * 0.018, top - 0.002), (cx + side * 0.026, EYE_Z + (0.008 if female else 0.004))]
        parts["Lash"].append(face_strap(arc, lambda v: 0.0045 if not female else 0.0055, 0.0050))
        # brow
        by = EYE_Z + (0.040 if not female else 0.043)
        brow = [(cx - side * 0.020, by - (0.001 if female else 0.003)), (cx, by + 0.003), (cx + side * 0.022, by - 0.002)]
        parts["Brow"].append(face_strap(brow, lambda v: (0.0055 if not female else 0.0035) * (1.0 - 0.5 * v), 0.0030))
    # mouth: a short line, a hint of a smile
    parts["Mouth"].append(face_strap([(-0.012, 0.030), (0.0, 0.0285), (0.012, 0.030)], 0.0028, 0.0025))
    # nose: a small wedge
    np_, nn = face_point(0.0, 0.058, 0.0)
    parts["Skin"].append(tube([np_ - nn * 0.004 + V((0, 0, 0.012)), np_ + nn * 0.006, np_ + nn * 0.004 - V((0, 0, 0.006))],
                              [0.006, 0.006, 0.002], n=6))
    # ears
    for side in (1, -1):
        rx, rf, rb, cy = head_ring(0.085)
        base = V((side * (rx - 0.006), cy + 0.012, 0.085))
        parts["Skin"].append(tube([base, base + V((side * 0.012, 0.004, 0.0))], [(0.012, 0.022, 0.018), (0.008, 0.018, 0.014)], n=8,
                                  fwd_hint=(0.0, 0.0, 1.0)))
    return {k: merge(*v) for k, v in parts.items() if v}


