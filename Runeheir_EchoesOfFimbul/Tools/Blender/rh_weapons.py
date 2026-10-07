"""Weapon models, one per WeaponType (Core/Combat/Weapons.cs), in a Norse style sized for the 1.75 m body.

Conventions (exported without the characters' 180-degree turn): the hand's grip is at the origin, the weapon points along
+Z (its tip), +X is its edge (the side that leads a cut, an axe's bit, the bow's back facing the target) and Y is the flat.
Unity reads that as tip +Y, edge +X; RiggedBody lines those up with the hand.

Slots (painted from the weapon palette in outfits.json): Steel, SteelDark, Wood, WoodDark, Leather, Gold, Gem (self-lit),
String, Paper, Cover.
"""

import math

from mathutils import Vector

from rh_geo import V, hash01, lerp, loft, merge, ring, sheet, smooth, tube

PALETTE = {
    "Steel": "#C3CAD3", "SteelDark": "#5D6672", "Wood": "#8A5A34", "WoodDark": "#4E3220", "Leather": "#5B3A26",
    "Gold": "#D2A443", "Gem": "#6FD3FF", "String": "#EEE6CF", "Paper": "#EDE3C8", "Cover": "#6B2A22",
}

# Unity WeaponType names, in enum order.
WEAPON_TYPES = ["Unarmed", "Dagger", "OneHandSword", "TwoHandSword", "Spear", "Mace", "Staff", "Bow", "Knuckle", "Katar", "Axe",
                "TwoHandAxe", "Instrument", "Whip", "Book", "ThunderRod", "Huuma", "CatStaff"]

FWD = (0.0, -1.0, 0.0)  # the flat's normal, for tubes along Z


def rod(z0, z1, r0, r1=None, n=8, x=0.0, y=0.0):
    r1 = r0 if r1 is None else r1
    return tube([V((x, y, z0)), V((x, y, z1))], [r0, r1], n=n, fwd_hint=FWD)


def bar(p0, p1, r0, r1=None, n=6, hint=(0.0, 0.0, 1.0)):
    r1 = r0 if r1 is None else r1
    p0, p1 = V(p0), V(p1)
    d = (p1 - p0).normalized()
    h = V(hint)
    if abs(d.dot(h.normalized())) > 0.95:
        h = V((0.0, 1.0, 0.0))
    return tube([p0, p1], [r0, r1], n=n, fwd_hint=tuple(h))


def ball(c, r, n=10, squash=(1.0, 1.0, 1.0)):
    c = V(c)
    sx, sy, sz = squash
    rings = []
    for k in range(1, 6):
        a = math.pi * k / 6
        rings.append(ring(c + V((0.0, 0.0, -math.cos(a) * r * sz)), V((1.0, 0.0, 0.0)), V((0.0, -1.0, 0.0)), math.sin(a) * r * sx,
                          math.sin(a) * r * sy, math.sin(a) * r * sy, n))
    return loft(rings, True, True)


def blade(z0, length, width, thick, tip=0.18, shape="straight", n=4, edge_bias=0.0):
    """A blade along +Z: diamond section (thick at the spine line, sharp at the edges). shape: straight, seax, leaf."""
    pts, prof = [], []
    segs = 10
    for k in range(segs + 1):
        t = k / segs
        z = z0 + length * t
        if shape == "seax":
            w = width * (1.0 + 0.25 * smooth(t / 0.7)) if t < 0.7 else width * 1.25 * max(0.0, 1.0 - (t - 0.7) / 0.3) ** 0.8
        elif shape == "leaf":
            w = width * max(0.0, math.sin(math.pi * min(1.0, 0.15 + 0.85 * t))) ** 0.7
        else:
            w = width * (1.0 - 0.15 * t) if t < 1.0 - tip else width * 0.85 * (1.0 - (t - (1.0 - tip)) / tip)
        w = max(w, 0.002)
        pts.append(V((edge_bias * w, 0.0, z)))
        prof.append((w * 0.5, thick * 0.5, thick * 0.5))
    return tube(pts, prof, n=n, fwd_hint=FWD)


def crossguard(z, half, r, curve=0.0):
    pts = [V((x, 0.0, z - curve * (x / half) ** 2)) for x in (-half, -half * 0.5, 0.0, half * 0.5, half)]
    prof = [r * 0.7, r, r * 1.15, r, r * 0.7]
    return tube(pts, prof, n=6, fwd_hint=(0.0, 0.0, 1.0))


def plate(points_xz, thick, y=0.0):
    """A flat shape in the XZ plane (convex outline), extruded along Y."""
    n = len(points_xz)
    front = [V((x, y - thick / 2, z)) for x, z in points_xz]
    back = [V((x, y + thick / 2, z)) for x, z in points_xz]
    verts = front + back
    faces = [list(range(n)), [n + i for i in reversed(range(n))]]
    for i in range(n):
        j = (i + 1) % n
        faces.append([i, j, n + j, n + i])
    return verts, faces


# ---------------------------------------------------------------------------------------------- the weapons
def w_dagger(W):
    W.add(blade(0.06, 0.30, 0.042, 0.010, shape="seax", edge_bias=0.15), "Steel")
    W.add(crossguard(0.055, 0.035, 0.009), "Gold")
    W.add(rod(-0.06, 0.055, 0.015, 0.014), "Leather")
    W.add(ball((0.0, 0.0, -0.07), 0.018, squash=(1.2, 0.8, 0.8)), "Gold")


def w_one_hand_sword(W):
    W.add(blade(0.06, 0.80, 0.055, 0.011, tip=0.16), "Steel")
    W.add(blade(0.08, 0.62, 0.016, 0.0125, tip=0.1), "SteelDark")  # the fuller
    W.add(crossguard(0.055, 0.075, 0.011, curve=0.0), "SteelDark")
    W.add(rod(-0.06, 0.055, 0.016, 0.015), "Leather")
    W.add(ball((0.0, 0.0, -0.075), 0.026, squash=(1.5, 0.7, 0.75)), "Gold")


def w_two_hand_sword(W):
    W.add(blade(0.13, 1.05, 0.085, 0.014, tip=0.14), "Steel")
    W.add(blade(0.16, 0.82, 0.022, 0.0155, tip=0.08), "SteelDark")
    W.add(crossguard(0.125, 0.16, 0.014, curve=0.03), "SteelDark")
    W.add(rod(-0.14, 0.125, 0.018, 0.017), "Leather")
    for z in (-0.08, 0.0, 0.08):
        W.add(rod(z - 0.006, z + 0.006, 0.0195), "Gold")
    W.add(ball((0.0, 0.0, -0.16), 0.032, squash=(1.3, 0.8, 0.9)), "Gold")


def w_spear(W):
    W.add(rod(-0.6, 1.25, 0.018, 0.016), "Wood")
    W.add(rod(1.2, 1.29, 0.022, 0.018), "SteelDark")  # socket
    W.add(blade(1.27, 0.34, 0.075, 0.012, shape="leaf"), "Steel")
    for side in (1, -1):  # the wings of a krokspjot
        W.add(bar((0.0, 0.0, 1.28), (side * 0.07, 0.0, 1.31), 0.008, 0.003), "SteelDark")
    W.add(rod(-0.64, -0.6, 0.02, 0.012), "SteelDark")
    W.add(rod(-0.06, 0.12, 0.0205), "Leather")


def bearded_head(z, height, reach, beard, thick):
    """An axe bit toward +X: straight top, the beard sweeping down."""
    outline = [(0.0, z + height * 0.15), (reach * 0.55, z + height * 0.22), (reach, z + height * 0.35), (reach * 1.04, z - height * 0.2),
               (reach * 0.92, z - height * 0.55 - beard), (reach * 0.7, z - height * 0.4 - beard * 0.8), (reach * 0.35, z - height * 0.12),
               (0.0, z - height * 0.15)]
    return plate(outline, thick)


def w_axe(W):
    W.add(rod(-0.12, 0.62, 0.017, 0.015), "Wood")
    W.add(bearded_head(0.54, 0.16, 0.15, 0.05, 0.016), "Steel")
    W.add(rod(0.5, 0.63, 0.022), "SteelDark")
    W.add(rod(-0.06, 0.1, 0.019), "Leather")


def w_two_hand_axe(W):
    W.add(rod(-0.3, 1.2, 0.022, 0.019), "Wood")
    W.add(bearded_head(1.08, 0.26, 0.24, 0.1, 0.02), "Steel")
    W.add(bar((0.0, 0.0, 1.06), (-0.07, 0.0, 1.06), 0.02, 0.012), "SteelDark")  # back spike
    W.add(rod(1.02, 1.21, 0.027), "SteelDark")
    W.add(rod(-0.2, 0.2, 0.024), "Leather")
    W.add(rod(-0.33, -0.3, 0.025, 0.015), "SteelDark")


def w_mace(W):
    W.add(rod(-0.1, 0.48, 0.017, 0.016), "WoodDark")
    W.add(rod(-0.06, 0.12, 0.019), "Leather")
    W.add(ball((0.0, 0.0, 0.53), 0.055, squash=(1.0, 1.0, 1.2)), "SteelDark")
    for k in range(6):
        a = 2 * math.pi * k / 6
        d = V((math.cos(a), math.sin(a), 0.0))
        W.add(plate([(0.0, 0.47), (0.0, 0.59), (0.075, 0.555), (0.075, 0.5)], 0.012), "Steel") if False else None
        W.add(tube([V((0.0, 0.0, 0.53)) + d * 0.03, V((0.0, 0.0, 0.53)) + d * 0.085],
                   [(0.004, 0.06, 0.06), (0.004, 0.035, 0.035)], n=4, fwd_hint=(0.0, 0.0, 1.0)), "Steel")
    W.add(ball((0.0, 0.0, 0.6), 0.02), "Steel")
    W.add(ball((0.0, 0.0, -0.11), 0.022), "SteelDark")


def gnarled(z0, z1, r0, r1, wobble=0.012, n=8, seed=1):
    pts, prof = [], []
    segs = 12
    for k in range(segs + 1):
        t = k / segs
        z = lerp(z0, z1, t)
        pts.append(V((wobble * math.sin(t * 7.0 + seed), wobble * math.cos(t * 5.0 + seed * 2), z)))
        prof.append(lerp(r0, r1, t) * (1.0 + 0.15 * math.sin(t * 19.0 + seed)))
    return tube(pts, prof, n=n, fwd_hint=FWD)


def w_staff(W):
    W.add(gnarled(-0.55, 1.0, 0.017, 0.024), "Wood")
    # roots curling around the crystal
    for k in range(4):
        a = 2 * math.pi * k / 4 + 0.4
        d = V((math.cos(a), math.sin(a), 0.0))
        W.add(tube([V((0.0, 0.0, 0.98)) + d * 0.015, V((0.0, 0.0, 1.06)) + d * 0.05, V((0.0, 0.0, 1.15)) + d * 0.035,
                    V((0.0, 0.0, 1.2)) + d * 0.005], [0.011, 0.009, 0.007, 0.003], n=6, fwd_hint=(0.0, 0.0, 1.0)), "WoodDark")
    W.add(tube([V((0.0, 0.0, 1.03)), V((0.0, 0.0, 1.08)), V((0.0, 0.0, 1.13)), V((0.0, 0.0, 1.18))],
               [0.012, 0.034, 0.03, 0.004], n=6, fwd_hint=FWD), "Gem")
    W.add(rod(-0.05, 0.1, 0.027), "Leather")
    W.add(rod(0.9, 0.94, 0.028), "Gold")


def w_cat_staff(W):
    W.add(gnarled(-0.4, 0.8, 0.016, 0.02, wobble=0.006, seed=3), "Wood")
    head = V((0.0, 0.0, 0.9))
    W.add(ball(head, 0.07, squash=(1.0, 0.85, 0.9)), "Paper")  # a round cat face in pale wood
    for side in (1, -1):
        W.add(tube([head + V((side * 0.04, 0.0, 0.035)), head + V((side * 0.055, 0.0, 0.1))], [(0.026, 0.01, 0.01), (0.002, 0.004, 0.004)],
                   n=4, fwd_hint=FWD), "Paper")
        W.add(ball(head + V((side * 0.025, -0.055, 0.01)), 0.009), "WoodDark")  # eyes
    W.add(ball(head + V((0.0, -0.058, -0.012)), 0.007), "Cover")  # nose
    W.add(ball(head + V((0.0, -0.02, -0.075)), 0.018), "Gold")  # bell
    W.add(rod(-0.05, 0.1, 0.022), "Leather")


def w_bow(W, S, A):
    """A longbow (W), its string (S, swapped for a drawn string in the game) and a nocked arrow (A, along +X from the string)."""
    half = 0.7
    tips = []
    limb_pts, limb_prof = [], []
    segs = 14
    for k in range(segs + 1):
        t = -1.0 + 2.0 * k / segs
        z = half * t
        back = -0.13 * t * t  # limbs bend toward the archer (-X)
        flick = 0.035 * max(0.0, abs(t) - 0.82) / 0.18  # recurved tips flick to the front
        limb_pts.append(V((back + flick, 0.0, z)))
        w = 0.03 * (1.0 - 0.5 * abs(t)) + 0.006
        limb_prof.append((w * 1.0, w * 0.6, w * 0.6))
    W.add(tube(limb_pts, limb_prof, n=6, fwd_hint=(1.0, 0.0, 0.0)), "Wood")
    tips = [limb_pts[0] + V((-0.004, 0.0, 0.0)), limb_pts[-1] + V((-0.004, 0.0, 0.0))]
    W.add(rod(-0.07, 0.07, 0.021), "Leather")
    for z in (-0.6, 0.6):
        tz = z / half
        W.add(rod(z - 0.015, z + 0.015, 0.016, x=-0.13 * tz * tz), "Gold")  # wraps on the limb
    S.add(bar(tips[0], tips[1], 0.0035, n=4), "String")
    # the arrow lies on the bow's left with its nock on the string
    nock_x = tips[0].x
    A.add(bar((nock_x, 0.018, 0.0), (nock_x + 0.72, 0.018, 0.0), 0.004, n=5), "Wood")
    A.add(tube([V((nock_x + 0.70, 0.018, 0.0)), V((nock_x + 0.76, 0.018, 0.0))], [(0.012, 0.004, 0.004), (0.001, 0.001, 0.001)], n=4,
               fwd_hint=(0.0, 0.0, 1.0)), "Steel")
    for k in range(3):
        a = 2 * math.pi * k / 3
        off = V((0.0, math.cos(a), math.sin(a))) * 0.012
        A.add(bar(V((nock_x + 0.02, 0.018, 0.0)) + off * 0.3, V((nock_x + 0.1, 0.018, 0.0)) + off, 0.003, 0.0015, n=3), "Paper")


def w_knuckle(W):
    """An iron knuckle-band across the fist (the fist's knuckles face +Z here)."""
    W.add(tube([V((-0.045, 0.0, 0.02)), V((0.0, 0.0, 0.03)), V((0.045, 0.0, 0.02))], [(0.012, 0.02, 0.02)] * 3, n=6,
               fwd_hint=(0.0, 0.0, 1.0)), "SteelDark")
    for x in (-0.03, -0.01, 0.01, 0.03):
        W.add(bar((x, 0.0, 0.04), (x, 0.0, 0.065), 0.008, 0.001, n=5), "Steel")


def w_katar(W):
    """Punch dagger: the blade carries on along the forearm (+Z), a two-bar grip across the fist."""
    W.add(blade(0.05, 0.38, 0.075, 0.013, tip=0.45), "Steel")
    for x in (-0.045, 0.045):
        W.add(bar((x, 0.0, 0.05), (x, 0.0, -0.13), 0.009, n=5), "SteelDark")
    W.add(bar((-0.045, 0.0, 0.05), (0.045, 0.0, 0.05), 0.012, n=6), "Gold")
    W.add(bar((-0.045, 0.0, 0.0), (0.045, 0.0, 0.0), 0.011, n=6), "Leather")


def w_instrument(W):
    """A lyre held at its foot: soundbox, two curving arms, a yoke, strings."""
    W.add(tube([V((0.0, 0.0, -0.02)), V((0.0, 0.0, 0.08)), V((0.0, 0.0, 0.16))], [(0.08, 0.025, 0.025), (0.1, 0.03, 0.03),
                                                                                   (0.09, 0.025, 0.025)], n=8, e=3.0, fwd_hint=FWD), "Wood")
    for side in (1, -1):
        W.add(tube([V((side * 0.07, 0.0, 0.15)), V((side * 0.1, 0.0, 0.26)), V((side * 0.085, 0.0, 0.36)), V((side * 0.105, 0.0, 0.42))],
                   [0.014, 0.013, 0.012, 0.016], n=6, fwd_hint=FWD), "WoodDark")
    W.add(bar((-0.11, 0.0, 0.4), (0.11, 0.0, 0.4), 0.012, n=6, hint=(0.0, 1.0, 0.0)), "Gold")
    for k in range(5):
        x = -0.04 + 0.02 * k
        W.add(bar((x, -0.032, 0.12), (x, -0.012, 0.395), 0.0018, n=3), "String")


def w_whip(W):
    W.add(rod(-0.05, 0.16, 0.017, 0.014), "Leather")
    W.add(ball((0.0, 0.0, -0.06), 0.02), "Gold")
    pts, prof = [], []
    for k in range(18):
        t = k / 17
        a = t * 5.0
        pts.append(V((0.12 * math.sin(a) * t, -0.05 * t, 0.16 + 0.5 * t - 0.25 * t * t)))
        prof.append(0.011 * (1.0 - 0.8 * t))
    W.add(tube(pts, prof, n=6, fwd_hint=(1.0, 0.0, 0.0)), "Leather")


def w_book(W):
    """A rune tome held by its spine (along Z), opening toward +X."""
    W.add(plate([(0.0, -0.15), (0.21, -0.15), (0.21, 0.15), (0.0, 0.15)], 0.07), "Paper")
    for y in (-0.038, 0.038):
        W.add(plate([(-0.008, -0.158), (0.22, -0.158), (0.22, 0.158), (-0.008, 0.158)], 0.008, y=y), "Cover")
    W.add(rod(-0.158, 0.158, 0.039, n=8, x=-0.006), "Cover")
    for x, z in ((0.205, -0.145), (0.205, 0.145)):
        for y in (-0.044, 0.044):
            W.add(ball((x, y, z), 0.012, squash=(1.0, 0.4, 1.0)), "Gold")
    W.add(plate([(0.09, -0.04), (0.13, 0.0), (0.09, 0.04), (0.05, 0.0)], 0.004, y=-0.044), "Gem")  # the rune on the cover


def w_thunder_rod(W):
    """A rune-rod held at the grip: barrel forward (+Z), stock behind."""
    W.add(rod(0.04, 0.82, 0.02, 0.018), "SteelDark")
    for z in (0.2, 0.45, 0.7):
        W.add(rod(z - 0.012, z + 0.012, 0.024), "Gold")
    W.add(tube([V((0.0, 0.0, 0.79)), V((0.0, 0.0, 0.84)), V((0.0, 0.0, 0.87))], [0.012, 0.026, 0.004], n=6, fwd_hint=FWD), "Gem")
    W.add(tube([V((0.0, 0.0, 0.1)), V((0.0, 0.0, -0.05)), V((0.0, 0.0, -0.32))], [(0.03, 0.025, 0.025), (0.026, 0.024, 0.024),
                                                                                 (0.032, 0.05, 0.05)], n=8, e=3.0, fwd_hint=(1.0, 0.0, 0.0)),
          "Wood")
    W.add(bar((0.0, 0.0, 0.0), (-0.07, 0.0, -0.03), 0.017, 0.015, n=6), "Leather")


def w_huuma(W):
    """A great four-bladed shuriken held through its hub; it spins in the XZ plane."""
    W.add(tube([V((0.0, -0.012, 0.0)), V((0.0, 0.012, 0.0))], [0.05, 0.05], n=12, fwd_hint=(0.0, 0.0, 1.0)), "SteelDark")
    for k in range(4):
        a = math.pi * 0.5 * k + 0.35
        d = V((math.cos(a), 0.0, math.sin(a)))
        side = V((-math.sin(a), 0.0, math.cos(a)))
        outline = [d * 0.04 + side * 0.035, d * 0.17 + side * 0.06, d * 0.33 - side * 0.02, d * 0.12 - side * 0.04]
        W.add(plate([(p.x, p.z) for p in outline], 0.01), "Steel")
    W.add(tube([V((0.0, -0.016, 0.0)), V((0.0, 0.016, 0.0))], [0.024, 0.024], n=10, fwd_hint=(0.0, 0.0, 1.0)), "Leather")


WEAPON_BUILDERS = {
    "Dagger": w_dagger, "OneHandSword": w_one_hand_sword, "TwoHandSword": w_two_hand_sword, "Spear": w_spear, "Mace": w_mace,
    "Staff": w_staff, "Knuckle": w_knuckle, "Katar": w_katar, "Axe": w_axe, "TwoHandAxe": w_two_hand_axe, "Instrument": w_instrument,
    "Whip": w_whip, "Book": w_book, "ThunderRod": w_thunder_rod, "Huuma": w_huuma, "CatStaff": w_cat_staff,
}
