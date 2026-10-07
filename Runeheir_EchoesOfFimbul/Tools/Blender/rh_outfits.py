"""Outfit pieces and the job outfits.

Every piece adds geometry to a Builder (rh_build.Builder) with a material *slot* name and bone weights. Slot names are
what Unity matches: the game paints each slot with the outfit's palette (Resources/Characters/outfits.json, written from
PALETTES below), the character's skin, hair and eye colors, through the Runeheir/Toon shader.

Design notes for the Warrior line (Phase 7 roster, Ragnarok's Swordman tree):
  Warrior    – red gambeson, leather baldric, one steel pauldron on the shield side, leg wraps, fur-cuffed boots
  Berserker  – bear-pelt mantle, steel lamellar, spiked pauldrons, mail skirt, ember runes on the gauntlets
  Guardian   – smooth plate, white tabard with the sun-cross, rounded layered pauldrons, mail sleeves
  Einherjar  – crimson and gold Valhalla plate, white fur collar, long crimson cape, glowing blue runes (the concept art)
  Valkyrie   – silver plate, feather-winged pauldrons, white skirt, sky-blue cape
"""

import math

from mathutils import Vector

from rh_body import FRONT, rigid, torso_shell, limb_shell, zcut_weights
from rh_geo import V, clamp01, dome, hash01, lerp, loft, merge, ring, sgnpow, sheet, smooth, strap, tube

# ---------------------------------------------------------------------------------------------- palettes
# "$outfit" is the job's own color (JobInfo.ColorHex) and "$outfitDark" a darker shade: the common outfit uses them so
# jobs without their own outfit yet still read as their job.
PALETTES = {
    "common": {"Cloth": "$outfit", "ClothDark": "$outfitDark", "Under": "#D9CDB4", "Leather": "#7A5534", "LeatherDark": "#4A3222",
               "Metal": "#98A0AA", "MetalDark": "#5A636E", "Gold": "#C29A48", "Fur": "#CFC4B2", "Glow": "#7FD0FF"},
    "warrior": {"Cloth": "#9A3B2C", "ClothDark": "#5A241C", "Under": "#DCCFB5", "Leather": "#7E5634", "LeatherDark": "#45301F",
                "Metal": "#9AA3AE", "MetalDark": "#58616C", "Gold": "#C0923F", "Fur": "#C9BCA6", "Glow": "#FF9A4A"},
    "berserker": {"Cloth": "#84261C", "ClothDark": "#47150F", "Under": "#C9B99C", "Leather": "#5C3B25", "LeatherDark": "#33231A",
                  "Metal": "#8A929C", "MetalDark": "#454B54", "Gold": "#A97B35", "Fur": "#6B4E3A", "Glow": "#FF6A2E"},
    "guardian": {"Cloth": "#E4DDCC", "ClothDark": "#3F5872", "Under": "#5D6D7E", "Leather": "#6E4C31", "LeatherDark": "#3D2B1E",
                 "Metal": "#A4ADB8", "MetalDark": "#5F6A77", "Gold": "#D4AE55", "Fur": "#D8D0C2", "Glow": "#FFE6A0"},
    "einherjar": {"Cloth": "#9E2A22", "ClothDark": "#4E1512", "Under": "#2E2A33", "Leather": "#4E3426", "LeatherDark": "#2C1F18",
                  "Metal": "#A9B2BE", "MetalDark": "#515B6A", "Gold": "#D8A43A", "Fur": "#E2DCD0", "Glow": "#64C8FF"},
    "valkyrie": {"Cloth": "#E6E1D6", "ClothDark": "#3C72AE", "Under": "#2F4E73", "Leather": "#8A6A4A", "LeatherDark": "#4D3A2A",
                 "Metal": "#B4BDC9", "MetalDark": "#707C8C", "Gold": "#DDB656", "Fur": "#EDE9E0", "Glow": "#A6E6FF"},
}

# Which jobs wear which outfit (the rest wear "common", tinted with their job color).
OUTFIT_JOBS = {
    "warrior": ["Warrior"],
    "berserker": ["Berserker"],
    "guardian": ["Guardian"],
    "einherjar": ["Einherjar"],
    "valkyrie": ["Valkyrie"],
}

# Slots shared by every model (colored per character in Unity, here only for the previews).
CHARACTER_SLOTS = {"Skin": "#F7D8BF", "Hair": "#E9D9A6", "Brow": "#B79A62", "Eye": "#2E5E8C", "EyeWhite": "#FBFAF6",
                   "Lash": "#231A1C", "Highlight": "#FFFFFF", "Mouth": "#A0524A"}

# ---------------------------------------------------------------------------------------------- helpers


def front_point(P, x, z, add):
    """Point and normal on the front of the torso at height z and sideways offset x."""
    rx, rf, rb = P.profile(z)
    e = P.TORSO_E
    u = min(abs(x) / (rx + add), 0.995)
    y = -(rf + add) * (1.0 - u ** e) ** (1.0 / e)
    nx = math.copysign(u ** (e - 1) / (rx + add), x)
    ny = -((-y) / (rf + add)) ** (e - 1) / (rf + add)
    return V((x, y, z)), V((nx, ny, 0.0)).normalized()


def surface_strap(B, P, slot, pts_az, width, add, thick=0.006, weigh=None):
    pts, nrms = [], []
    for a, z in pts_az:
        p, n = P.surface(a, z, add)
        pts.append(p)
        nrms.append(n)
    B.add(strap(pts, width, thick, nrms), slot, weigh or P.torso_weights)


def front_strap(B, P, slot, pts_xz, width, add, thick=0.006, weigh=None):
    pts, nrms = [], []
    for x, z in pts_xz:
        p, n = front_point(P, x, z, add)
        pts.append(p)
        nrms.append(n)
    B.add(strap(pts, width, thick, nrms), slot, weigh or P.torso_weights)


def circle_xz(cx, cz, r, n=14):
    return [(cx + r * math.sin(2 * math.pi * i / n), cz + r * math.cos(2 * math.pi * i / n)) for i in range(n + 1)]


def side_name(side):
    return "Left" if side > 0 else "Right"


# ---------------------------------------------------------------------------------------------- base pieces
def shirt(B, P, slot, add=0.005, z0=None, z1=None):
    z0 = P.torso[0][0] if z0 is None else z0
    z1 = P.neck_base if z1 is None else z1
    B.add(torso_shell(P, z0, z1, add, rings=14), slot, P.torso_weights)


def sleeves(B, P, slot, add=0.006, t0=0.0, t1=0.5, cuff=0.0, ridges=None):
    for side in (1, -1):
        path = P.arm_path(side, t0, t1, 7)
        flare = (lambda k: cuff * smooth((k - 0.75) / 0.25)) if cuff else None
        B.add(limb_shell(path, P.arm_radius, add, n=12, flare=flare, t0=t0, t1=t1, ridges=ridges), slot,
              lambda p, s=side: P.chain_weights(p, P.arm_chain(s)))


def trousers(B, P, slot, add=0.006, t1=0.97, ridges=None):
    for side in (1, -1):
        path = P.leg_path(side, -0.04, t1, 11)
        B.add(limb_shell(path, P.leg_radius, add, n=14, t0=-0.04, t1=t1, ridges=ridges), slot,
              lambda p, s=side: P.chain_weights(p, P.leg_chain(s)))


def leg_wraps(B, P, slot, band_slot, t0=0.55, t1=0.96, add=0.012):
    """Winingas: wrapped cloth from below the knee to the ankle, with leather bands."""
    for side in (1, -1):
        w = lambda p, s=side: P.chain_weights(p, P.leg_chain(s))
        path = P.leg_path(side, t0, t1, 9)
        rid = [(lerp(t0, t1, k / 7.0), 0.004) for k in range(8)]
        B.add(limb_shell(path, P.leg_radius, add, n=12, t0=t0, t1=t1, ridges=rid), slot, w)
        for tb in (t0 + 0.04, lerp(t0, t1, 0.5), t1 - 0.04):
            path_b = P.leg_path(side, tb - 0.012, tb + 0.012, 2)
            B.add(limb_shell(path_b, P.leg_radius, add + 0.007, n=12, t0=tb - 0.012, t1=tb + 0.012), band_slot, w)


def hands(B, P, slot, cuff_slot=None, cuff=0.0, spikes_slot=None):
    from rh_body import hand
    for side in (1, -1):
        s = side_name(side)
        B.add(hand(P, side), slot, lambda p, s=s: P.chain_weights(p, [s + "LowerArm", s + "Hand"], blend=0.02))
        if cuff_slot:
            path = P.arm_path(side, 0.9, 1.0, 3)
            B.add(limb_shell(path, P.arm_radius, 0.012, n=12, t0=0.9, t1=1.0, flare=lambda k: cuff * k), cuff_slot,
                  lambda p, s=side: P.chain_weights(p, P.arm_chain(s)))
        if spikes_slot:
            h0, h1 = P.joints[s + "Hand"]
            axis = (h1 - h0).normalized()
            for k in range(3):
                base = h0 + axis * (0.035 + 0.018 * k) + V((side * 0.0, -0.036, 0.0))
                B.add(cone_piece(base, base + V((side * 0.004, -0.022, 0.0)), 0.007), spikes_slot, rigid(s + "Hand"))


def cone_piece(base, tip, r, n=6):
    base, tip = V(base), V(tip)
    return tube([base, base.lerp(tip, 0.5), tip], [r, r * 0.55, r * 0.05], n=n,
                fwd_hint=(0.0, 0.0, 1.0) if abs((tip - base).normalized().z) < 0.9 else (0.0, -1.0, 0.0))


def boots(B, P, slot, top_t=0.8, add=0.014, cuff_slot=None, fur=False, sole_slot=None, toe_cap=None, plated=False):
    for side in (1, -1):
        s = side_name(side)
        a0 = P.j(s + "LowerLeg", 1)
        x = a0.x + side * 0.004
        k = 0.92 if P.female else 1.0
        # the foot: rings from heel to toe, boxy
        prof = [(0.055, 0.036, 0.075), (0.025, 0.046, 0.105), (-0.03, 0.050, 0.090), (-0.085, 0.052, 0.066),
                (-0.13, 0.046, 0.050), (-0.158, 0.034, 0.036), (-0.168, 0.018, 0.022)]
        rings_ = []
        for y, w, h in prof:
            y = y * k + a0.y
            c = V((x, y, h * 0.5 * k))
            rings_.append(ring(c, V((1.0, 0.0, 0.0)), V((0.0, 0.0, 1.0)), (w + (0.006 if plated else 0)) * k, h * 0.5 * k, h * 0.5 * k, 12, 3.2))
        foot = loft(rings_, True, True)
        w_foot = lambda p, s=s: P.chain_weights(p, [s + "LowerLeg", s + "Foot", s + "Toes"], blend=0.03)
        B.add(foot, slot, w_foot)
        if sole_slot:
            sole = [ring(V((x, y * k + a0.y, 0.007)), V((1.0, 0.0, 0.0)), V((0.0, 0.0, 1.0)), (w + 0.006) * k, 0.008, 0.008, 12, 3.5)
                    for y, w, h in prof[:-1]]
            B.add(loft(sole, True, True), sole_slot, w_foot)
        # the shaft
        path = P.leg_path(side, top_t, 1.0, 6)
        path[-1] = path[-1] + V((0.0, 0.0, -0.02))
        flare = (lambda kk: 0.012 * smooth((0.25 - kk) / 0.25)) if not fur else None
        B.add(limb_shell(path, P.leg_radius, add, n=14, t0=top_t, t1=1.0, flare=flare), slot,
              lambda p, s=side: P.chain_weights(p, P.leg_chain(s)))
        if fur and cuff_slot:
            fur_cuff(B, P, side, cuff_slot, top_t, add)
        elif cuff_slot:
            path_c = P.leg_path(side, top_t - 0.01, top_t + 0.035, 2)
            B.add(limb_shell(path_c, P.leg_radius, add + 0.01, n=14, t0=top_t - 0.01, t1=top_t + 0.035), cuff_slot,
                  lambda p, s=side: P.chain_weights(p, P.leg_chain(s)))
        if toe_cap:
            tc = [ring(V((x, y * k + a0.y, h * 0.5 * k)), V((1.0, 0.0, 0.0)), V((0.0, 0.0, 1.0)), (w + 0.008) * k, (h * 0.5 + 0.006) * k,
                       (h * 0.5 + 0.006) * k, 12, 3.2) for y, w, h in prof[3:]]
            B.add(loft(tc, True, True), toe_cap, w_foot)


def fur_cuff(B, P, side, slot, t, add):
    path = P.leg_path(side, t - 0.03, t + 0.04, 3)
    r = P.leg_radius(t) + add + 0.016

    def radial(i, j, a):
        return 1.0 + (0.18 * hash01(side, j, 3) if i == 0 else 0.1 * hash01(side, j, i))

    v, f = tube(path, [r * 1.05, r * 1.15, r * 0.95], n=16, radial=radial)
    # jag the top ring
    for j in range(16):
        v[j] = v[j] + V((0.0, 0.0, 0.018 * (hash01(side, j) - 0.3)))
    B.add((v, f), slot, lambda p, s=side: P.chain_weights(p, P.leg_chain(s)))


def belt(B, P, slot, z, h=0.045, add=0.022, buckle_slot=None, buckle="round", e=None):
    rings_ = []
    for zz in (z - h / 2, z - h / 2 + 0.004, z + h / 2 - 0.004, z + h / 2):
        rx, rf, rb = P.profile(zz)
        edge = 0.0 if zz in (z - h / 2, z + h / 2) else 0.004
        rings_.append(ring(V((0.0, 0.0, zz)), V((1.0, 0.0, 0.0)), FRONT, rx + add + edge, rf + add + edge, rb + add + edge, 28, e or P.TORSO_E))
    B.add(loft(rings_, True, True), slot, P.torso_weights)
    if buckle_slot:
        p, n = front_point(P, 0.0, z, add + 0.006)
        if buckle == "round":
            B.add(tube([p - n * 0.006, p + n * 0.004, p + n * 0.008], [(0.032, 0.03, 0.03), (0.034, 0.032, 0.032), (0.024, 0.022, 0.022)], n=16,
                       fwd_hint=(0.0, 0.0, 1.0)), buckle_slot, P.torso_weights)
        else:
            B.add(tube([p - n * 0.006, p + n * 0.008], [(0.034, h * 0.62, h * 0.62), (0.03, h * 0.55, h * 0.55)], n=12, e=4.0,
                       fwd_hint=(0.0, 0.0, 1.0)), buckle_slot, P.torso_weights)


def skirt(B, P, slot, z_top, z_bot, add, flare, panels=8, thick=0.008, gap=0.03, trim=None, start=0.0, leg_share=0.7,
          only=None, tatter=0.0, length_fn=None):
    """Tunic hem / tassets / mail skirt as separate panels; each hangs from the hips and follows its leg at the bottom."""
    rx0, rf0, rb0 = P.profile(z_top)
    span = 2.0 * math.pi / panels
    for k in range(panels):
        a0 = start + k * span + gap * 0.5
        a1 = start + (k + 1) * span - gap * 0.5
        mid = 0.5 * (a0 + a1)
        if only and not only(mid):
            continue
        side = 1 if math.sin(mid) > 1e-6 else -1 if math.sin(mid) < -1e-6 else (1 if math.cos(mid) > 0 else -1)
        bot = z_bot + (length_fn(mid) if length_fn else 0.0)

        def place(u, v, a0=a0, a1=a1, bot=bot, k=k):
            a = lerp(a0, a1, u)
            zb = bot - tatter * hash01(k, round(u, 3))
            z = lerp(z_top, zb, v)
            out = add + flare * v ** 1.2
            e = P.TORSO_E
            sx, cy = math.sin(a), math.cos(a)
            x = sgnpow(sx, 2.0 / e) * (rx0 + out)
            f = sgnpow(cy, 2.0 / e) * ((rf0 if cy >= 0 else rb0) + out)
            nrm = V((sx, -cy, 0.15 * v)).normalized()
            return V((x, -f, z)), nrm

        leg = side_name(side) + "UpperLeg"

        def weigh(p, leg=leg, bot=bot):
            f = smooth((z_top - p.z) / max(z_top - bot, 1e-3)) * leg_share
            return {"Hips": 1.0 - f, leg: f}

        B.add(sheet(3, 5, place, thick), slot, weigh)
        if trim:
            def place_t(u, v, a0=a0, a1=a1, bot=bot):
                a = lerp(a0, a1, u)
                z = lerp(bot + 0.02, bot - 0.002, v)
                out = add + flare * 1.0 + 0.004
                e = P.TORSO_E
                sx, cy = math.sin(a), math.cos(a)
                x = sgnpow(sx, 2.0 / e) * (rx0 + out)
                f = sgnpow(cy, 2.0 / e) * ((rf0 if cy >= 0 else rb0) + out)
                return V((x, -f, z)), V((sx, -cy, 0.0))
            B.add(sheet(3, 1, place_t, thick + 0.004), trim, weigh)


def tabard(B, P, slot, z_top, z_bot, w_top, w_bot, add, trim=None, back=True, flare=0.03, point=0.0):
    """Hanging front (and back) panels, split down the middle so each half follows its leg."""
    for face in ((1, -1) if back else (1,)):  # 1 = front, -1 = back
        for side in (1, -1):
            def place(u, v, face=face, side=side):
                z = lerp(z_top, z_bot, v) - point * (1.0 - u) * v
                w = lerp(w_top, w_bot, v)
                x = side * u * w
                zz = max(z, P.torso[0][0] + 0.06)
                rx, rf, rb = P.profile(zz)
                r_face = rf if face > 0 else rb
                e = P.TORSO_E
                uu = min(abs(x) / (rx + add), 0.99)
                depth = (r_face + add) * (1.0 - uu ** e) ** (1.0 / e) + flare * v
                return V((x, -face * depth, z)), V((0.0, -face, 0.0))

            leg = side_name(side) + "UpperLeg"

            def weigh(p, leg=leg):
                f = smooth((P.torso[1][0] - p.z) / 0.35) * 0.6
                base = P.torso_weights(p) if p.z > P.torso[1][0] else {"Hips": 1.0}
                return {**{k: v * (1.0 - f) for k, v in base.items()}, leg: f}

            B.add(sheet(3, 8, place, 0.008), slot, weigh)
            if trim:
                def place_edge(u, v, face=face, side=side):
                    z = lerp(z_top, z_bot, v) - point * v * 0.0
                    w = lerp(w_top, w_bot, v)
                    x = side * (w - 0.01 + 0.012 * u)
                    zz = max(z, P.torso[0][0] + 0.06)
                    rx, rf, rb = P.profile(zz)
                    r_face = rf if face > 0 else rb
                    e = P.TORSO_E
                    uu = min(abs(x) / (rx + add), 0.99)
                    depth = (r_face + add) * (1.0 - uu ** e) ** (1.0 / e) + flare * v + 0.003
                    return V((x, -face * depth, z)), V((0.0, -face, 0.0))
                B.add(sheet(1, 8, place_edge, 0.010), trim, weigh)
                # hem
                def place_hem(u, v, face=face, side=side):
                    x = side * u * w_bot
                    z = z_bot + 0.015 - 0.017 * v - point * (1.0 - u)
                    rx, rf, rb = P.profile(max(z, P.torso[0][0] + 0.06))
                    r_face = rf if face > 0 else rb
                    e = P.TORSO_E
                    uu = min(abs(x) / (rx + add), 0.99)
                    depth = (r_face + add) * (1.0 - uu ** e) ** (1.0 / e) + flare + 0.003
                    return V((x, -face * depth, z)), V((0.0, -face, 0.0))
                B.add(sheet(3, 1, place_hem, 0.010), trim, weigh)


def pauldron(B, P, side, slot, r=0.105, lames=2, trim=None, spikes=0, spike_slot=None, wing_slot=None, lift=0.012, out=0.01,
             theta=1.25, squash=(1.0, 1.08, 0.82), rune_slot=None, flare_top=0.0):
    s = side_name(side)
    sh = P.j(s + "UpperArm")
    elbow = P.j(s + "LowerArm")
    down = (elbow - sh).normalized()
    if P.female:
        r *= 0.9
    axis = V((side * 0.78, 0.0, 1.0)).normalized()
    weigh = lambda p, s=s: {s + "UpperArm": 0.6, s + "Shoulder": 0.4}
    center0 = sh + V((side * out, 0.0, lift - 0.03))
    for k in range(lames):
        rr = r * (1.0 - 0.05 * k) + (flare_top if k == 0 else 0.0)
        c = center0 + down * (0.038 * k) - axis * (0.006 * k)
        B.add(dome(c, axis, FRONT, rr, theta - 0.08 * k, 0.01, n=16, rings=5, squash=squash, lip=0.04), slot, weigh)
        if trim and k == lames - 1:
            band = rim_band(c, axis, FRONT, rr, theta - 0.08 * k, squash)
            B.add(band, trim, weigh)
    if spikes:
        for i in range(spikes):
            az = (-0.55 + 1.1 * i / max(spikes - 1, 1)) * math.pi * 0.5
            th = 0.55
            d = (axis * math.cos(th) + (V((0.0, -1.0, 0.0)) * math.cos(az) + axis.cross(V((0.0, -1.0, 0.0))).normalized() * math.sin(az))
                 * math.sin(th)).normalized()
            base = center0 + V((d.x * r * squash[0], d.y * r * squash[1], d.z * r * squash[2]))
            B.add(cone_piece(base - d * 0.006, base + d * (0.055 + 0.015 * (i % 2)), 0.016), spike_slot or slot, weigh)
    if wing_slot:
        from rh_hair import lock
        back = V((0.0, 1.0, 0.0))
        root = center0 + axis * (r * 0.45) + back * (r * 0.45)
        for i in range(5):
            # a fan of feathers sweeping up and back from the pauldron, longest on top
            d = V((side * (0.2 + 0.16 * i), 0.75, 1.0 - 0.2 * i)).normalized()
            plane = V((side, 0.0, 0.0)).cross(d).normalized()
            B.add(lock(root + V((side * 0.006 * i, 0.01 * i, -0.022 * i)), d, 0.25 - 0.03 * i, 0.05 - 0.004 * i, curl=0.03 * side,
                       droop=-0.02, flat=0.18, segs=5, twist_side=plane.cross(d)), wing_slot, weigh)
            if trim:
                B.add(lock(root + V((side * 0.006 * i, 0.01 * i, -0.022 * i)) - plane * 0.004, d, 0.2 - 0.026 * i, 0.022, curl=0.03 * side,
                           droop=-0.02, flat=0.3, segs=5, twist_side=plane.cross(d)), trim, weigh)
    if rune_slot:
        p = center0 + axis * (r * 0.83)
        n = axis
        B.add(strap([p - FRONT * 0.03 + n * 0.0, p + FRONT * 0.03], 0.008, 0.004, [n, n]), rune_slot, weigh)


def rim_band(c, axis, fwd, r, theta, squash, width=0.012, thick=0.008, n=16):
    axis = V(axis).normalized()
    fwd = (V(fwd) - axis * V(fwd).dot(axis)).normalized()
    side = axis.cross(fwd).normalized()
    sx, sf, sa = squash
    rings_ = []
    for th, extra in ((theta - width / r, 0.0), (theta - width / r, thick), (theta + 0.02, thick), (theta + 0.02, 0.0)):
        rr = (r + extra) * math.sin(th)
        h = (r + extra) * math.cos(th)
        pts = []
        for i in range(n):
            a = 2.0 * math.pi * i / n
            d = side * (math.sin(a) * sx) + fwd * (math.cos(a) * sf)
            pts.append(c + d * rr + axis * (h * sa))
        rings_.append(pts)
    return loft(rings_ + [rings_[0]], False, False)


def bracers(B, P, slot, t0=0.58, t1=0.94, add=0.012, cuff=0.012, ridges=None, trim=None):
    for side in (1, -1):
        path = P.arm_path(side, t0, t1, 7)
        flare = (lambda k: cuff * smooth((0.3 - k) / 0.3))
        w = lambda p, s=side: P.chain_weights(p, P.arm_chain(s))
        B.add(limb_shell(path, P.arm_radius, add, n=14, t0=t0, t1=t1, flare=flare, ridges=ridges), slot, w)
        if trim:
            for tt in (t0 + 0.01, t1 - 0.01):
                pth = P.arm_path(side, tt - 0.012, tt + 0.012, 2)
                fl = cuff if tt < 0.7 else 0.0
                B.add(limb_shell(pth, P.arm_radius, add + 0.004 + fl * 0.9, n=14, t0=tt - 0.012, t1=tt + 0.012), trim, w)


def couters(B, P, slot):
    for side in (1, -1):
        s = side_name(side)
        el = P.j(s + "LowerArm")
        B.add(dome(el + V((side * 0.006, 0.026, 0.0)), V((side * 0.3, 1.0, 0.0)), V((0, 0, 1)), 0.038, 1.2, 0.008, n=12, rings=4),
              slot, lambda p, s=s: {s + "UpperArm": 0.5, s + "LowerArm": 0.5})


def greaves(B, P, slot, t0=0.52, t1=0.94, add=0.018, knee_slot=None, span=1.9, trim=None):
    for side in (1, -1):
        s = side_name(side)

        def place(u, v, side=side):
            t = lerp(t0, t1, v)
            path = P.leg_path(side, t, t, 2)
            c = path[0]
            r = P.leg_radius(t) + add
            a = lerp(-span, span, u)
            nrm = V((math.sin(a), -math.cos(a), 0.0))
            return c + nrm * r, nrm

        w = lambda p, s=side: P.chain_weights(p, P.leg_chain(s))
        B.add(sheet(8, 6, place, 0.008), slot, w)
        if trim:
            def place_t(u, v, side=side):
                t = lerp(t0, t0 + 0.025, v)
                c = P.leg_path(side, t, t, 2)[0]
                r = P.leg_radius(t) + add + 0.005
                a = lerp(-span, span, u)
                nrm = V((math.sin(a), -math.cos(a), 0.0))
                return c + nrm * r, nrm
            B.add(sheet(8, 1, place_t, 0.008), trim, w)
        if knee_slot:
            knee = P.j(s + "LowerLeg")
            B.add(dome(knee + V((0.0, -0.035, 0.012)), V((0.0, -1.0, 0.25)), V((0, 0, 1)), 0.05, 1.25, 0.009, n=14, rings=4,
                       squash=(1.0, 1.15, 0.9), lip=0.05), knee_slot,
                  lambda p, s=s: {s + "UpperLeg": 0.5, s + "LowerLeg": 0.5})


def cuisses(B, P, slot, t0=0.08, t1=0.42, add=0.02, span=1.5):
    for side in (1, -1):
        def place(u, v, side=side):
            t = lerp(t0, t1, v)
            c = P.leg_path(side, t, t, 2)[0]
            r = P.leg_radius(t) + add
            a = lerp(-span, span * 0.7, u) * side
            nrm = V((math.sin(a), -math.cos(a), 0.0))
            return c + nrm * r, nrm
        B.add(sheet(8, 5, place, 0.008), slot, lambda p, s=side: P.chain_weights(p, P.leg_chain(s)))


def fur_collar(B, P, slot, rx=0.17, depth=(0.12, 0.125), z_bot=None, drop=0.09, jag=0.025, n=30, back_drop=0.0, height=0.05):
    nb = P.neck_base
    z_bot = nb - drop if z_bot is None else z_bot
    nr = P.neck_r
    if P.female:
        rx *= 0.92
    rings_ = [
        ring(V((0.0, 0.01, nb + height)), V((1.0, 0.0, 0.0)), FRONT, nr + 0.028, nr + 0.024, nr + 0.03, n, 2.0),
        ring(V((0.0, 0.01, nb + height * 0.4)), V((1.0, 0.0, 0.0)), FRONT, rx * 0.72, depth[0] * 0.8, depth[1] * 0.85, n, 2.2),
        ring(V((0.0, 0.01, (nb + z_bot) * 0.5)), V((1.0, 0.0, 0.0)), FRONT, rx * 0.96, depth[0], depth[1], n, 2.4),
    ]
    bot = []
    for i in range(n):
        a = 2 * math.pi * i / n
        back = max(0.0, -math.cos(a))
        k = 1.04 + 0.08 * hash01(i, 11)
        z = z_bot - back_drop * back + (jag if i % 2 else -jag) * (0.6 + 0.6 * hash01(i, 5))
        sx, cy = math.sin(a), math.cos(a)
        x = sgnpow(sx, 2 / 2.4) * rx * k
        f = sgnpow(cy, 2 / 2.4) * (depth[0] if cy >= 0 else depth[1]) * k
        bot.append(V((x, 0.01 - f, z)))
    rings_.append(bot)
    B.add(loft(rings_, True, True), slot, lambda p: zcut_weights(p.z, [(-9, "Chest"), (nb + 0.02, "Neck")], 0.02))


def cape(C, P, slot, lining, z_bot=0.36, w_top=1.75, w_bot=1.25, r_top=0.2, r_bot=0.32, back_bot=0.07, folds=5, tatter=0.0,
         collar_z=None, hug=0.62):
    """A cape hanging from the shoulders. Separate object ('Cape'), hidden in game when a garment is worn."""
    z_top = (P.neck_base - 0.035) if collar_z is None else collar_z
    if P.female:
        r_top *= 0.92

    def make(offset, thick):
        def place(u, v):
            k = int(round(u * 10))
            zb = z_bot - tatter * hash01(k, 21) * smooth(v)
            z = lerp(z_top, zb, v)
            half = lerp(w_top, w_bot, smooth(v)) * 0.5
            phi = lerp(-half, half, u)
            r = lerp(r_top, r_bot, smooth(v)) + 0.012 * v * math.sin(u * folds * 2 * math.pi) - offset
            cy = lerp(0.0, back_bot, smooth(v))
            x = r * math.sin(phi)
            y = cy + r * math.cos(phi) * (hug if v < 0.05 else lerp(hug, 1.0, smooth(v * 3.0)))
            nrm = V((math.sin(phi), math.cos(phi), 0.25 * (1.0 - v))).normalized()
            return V((x, y, z)), nrm
        return sheet(10, 14, place, thick)

    def weigh(p):
        v = clamp01((z_top - p.z) / (z_top - z_bot))
        if v < 0.15:
            return {"Chest": 1.0}
        s = smooth((v - 0.15) / 0.6)
        return {"Chest": 1.0 - 0.6 * s, "Spine": 0.35 * s, "Hips": 0.25 * s}

    C.add(make(0.0, 0.007), slot, weigh)
    C.add(make(0.008, 0.005), lining, weigh)


def harness(B, P, slot, metal_slot=None, add=0.03):
    path = [(0.55, 1.37), (0.25, 1.29), (-0.15, 1.18), (-0.6, 1.07), (-1.1, 1.0), (-1.6, 0.97), (-2.1, 1.0), (-2.6, 1.08),
            (-3.0, 1.17), (2.95, 1.24), (2.65, 1.32), (2.45, 1.37)]
    z = P.zs
    surface_strap(B, P, slot, [(a, zz * z) for a, zz in path], 0.038, add)
    if metal_slot:
        for a, zz in ((0.25, 1.29), (-0.6, 1.07)):
            p, n = P.surface(a, zz * z, add + 0.006)
            B.add(tube([p - n * 0.004, p + n * 0.006], [(0.016, 0.016, 0.016), (0.013, 0.013, 0.013)], n=10, fwd_hint=(0, 0, 1)),
                  metal_slot, P.torso_weights)


def emblem(B, P, kind, slot, z, add, size=0.05):
    if kind == "suncross":
        front_strap(B, P, slot, circle_xz(0.0, z, size, 16), 0.012, add)
        front_strap(B, P, slot, [(0.0, z - size * 1.25), (0.0, z + size * 1.25)], 0.014, add + 0.002)
        front_strap(B, P, slot, [(-size * 1.25, z), (size * 1.25, z)], 0.014, add + 0.002)
    elif kind == "valknut":
        for k in range(3):
            cx = size * 0.42 * math.sin(2 * math.pi * k / 3)
            cz = z + size * 0.42 * math.cos(2 * math.pi * k / 3)
            tri = [(cx + size * 0.62 * math.sin(2 * math.pi * j / 3 + math.pi), cz + size * 0.62 * math.cos(2 * math.pi * j / 3 + math.pi))
                   for j in range(4)]
            front_strap(B, P, slot, tri, 0.008, add + 0.001 * k)
    elif kind == "wings":
        for side in (1, -1):
            front_strap(B, P, slot, [(side * 0.008, z - size * 0.6), (side * size * 0.9, z + size * 0.35), (side * size * 1.5, z + size * 0.75)],
                        lambda v: 0.016 * (1 - 0.6 * v), add)
            front_strap(B, P, slot, [(side * 0.008, z - size * 0.9), (side * size * 0.8, z - size * 0.05), (side * size * 1.25, z + size * 0.2)],
                        lambda v: 0.012 * (1 - 0.6 * v), add)
        front_strap(B, P, slot, [(0.0, z - size * 1.0), (0.0, z + size * 0.4)], 0.014, add + 0.001)


def gorget(B, P, slot, add=0.02, h=0.05):
    nb = P.neck_base
    rings_ = []
    for k, zz in enumerate((nb - 0.03, nb + h * 0.5, nb + h)):
        r = P.neck_r + add + (0.012 if k == 0 else 0.0)
        rings_.append(ring(V((0.0, 0.01, zz)), V((1.0, 0.0, 0.0)), FRONT, r + 0.03 * (1 - k / 2), r + 0.02 * (1 - k / 2), r + 0.03 * (1 - k / 2), 18, 2.0))
    B.add(loft(rings_, True, True), slot, lambda p: zcut_weights(p.z, [(-9, "Chest"), (nb + 0.03, "Neck")], 0.02))


def cuirass(B, P, slot, z0, z1, add, trim=None, ridges=None, lames=0, lame_slot=None, keel=0.0):
    """Plate (or lamellar) over the chest. keel pushes a ridge down the middle of the breastplate."""
    add_fn = (lambda z: 0.0)
    B.add(torso_shell(P, z0, z1, add, rings=14, ridges=ridges, add_fn=add_fn), slot, P.torso_weights)
    if keel:
        front_strap(B, P, slot, [(0.0, z0 + 0.03), (0.0, (z0 + z1) * 0.5), (0.0, z1 - 0.06)], lambda v: 0.05 * (1 - 0.5 * abs(v - 0.5)),
                    add + keel * 0.5, thick=keel)
    if trim:
        for zz in (z0 + 0.008, z1 - 0.02):
            belt(B, P, trim, zz, h=0.014, add=add + 0.004)
    for k in range(lames):
        zz = z0 - 0.03 - k * 0.035
        belt(B, P, lame_slot or slot, zz, h=0.04, add=add - 0.004 + 0.006 * k)


def rune_line(B, P, slot, pts_xz, add, width=0.007):
    front_strap(B, P, slot, pts_xz, width, add, thick=0.004)


# ---------------------------------------------------------------------------------------------- outfits
def base_head(B, P):
    """Skull, face and neck in skin; everyone has them."""
    from rh_body import face_features, neck, skull
    head = P.head_origin
    off = lambda part: ([V(p) + head for p in part[0]], part[1])
    B.add(off(skull()), "Skin", rigid("Head"))
    for slot, part in face_features(P.female).items():
        B.add(off(part), slot, rigid("Head"))
    B.add(neck(P), "Skin", lambda p: P.chain_weights(p, ["Chest", "Neck", "Head"], blend=0.03))


def outfit_common(B, C, P):
    """Initiate's travelling clothes, and every job that has no outfit of its own yet (tinted with the job color)."""
    base_head(B, P)
    shirt(B, P, "Under", add=0.005, z0=0.86 * P.zs)
    shirt(B, P, "Cloth", add=0.016, z0=0.92 * P.zs, z1=P.neck_base - 0.01)
    skirt(B, P, "Cloth", 0.95 * P.zs, (0.62 if P.female else 0.70) * P.zs, 0.018, 0.03, panels=6, gap=0.02, start=math.pi / 6)
    sleeves(B, P, "Cloth", add=0.010, t0=0.0, t1=0.48)
    sleeves(B, P, "Under", add=0.006, t0=0.46, t1=0.9, cuff=0.006)
    belt(B, P, "Leather", 0.98 * P.zs, add=0.028, buckle_slot="Gold", buckle="square")
    trousers(B, P, "ClothDark", add=0.008, t1=0.85)
    leg_wraps(B, P, "Under", "Leather", t0=0.58, t1=0.92, add=0.012)
    boots(B, P, "Leather", top_t=0.86, add=0.016, sole_slot="LeatherDark")
    hands(B, P, "Skin")


def outfit_warrior(B, C, P):
    base_head(B, P)
    shirt(B, P, "Under", add=0.005, z0=0.86 * P.zs)
    # quilted gambeson
    quilt = [(z * P.zs, 0.0035) for z in (1.0, 1.06, 1.12, 1.18, 1.24, 1.30)]
    B.add(torso_shell(P, 0.9 * P.zs, P.neck_base - 0.012, 0.02, rings=22, ridges=quilt), "Cloth", P.torso_weights)
    skirt(B, P, "Cloth", 0.95 * P.zs, (0.60 if P.female else 0.64) * P.zs, 0.024, 0.04, panels=6, gap=0.015, start=math.pi / 6,
          trim="ClothDark")
    gorget(B, P, "ClothDark", add=0.012, h=0.035)
    sleeves(B, P, "Cloth", add=0.012, t0=0.0, t1=0.6, ridges=[(0.15, 0.003), (0.3, 0.003)])
    bracers(B, P, "Leather", t0=0.6, t1=0.93, add=0.014, cuff=0.012, trim="LeatherDark")
    hands(B, P, "LeatherDark")
    belt(B, P, "Leather", 0.985 * P.zs, h=0.06, add=0.034, buckle_slot="Gold", buckle="round")
    harness(B, P, "LeatherDark", metal_slot="Metal", add=0.036)
    # one steel pauldron on the shield (left) side, a leather pad on the sword arm
    pauldron(B, P, 1, "Metal", r=0.11, lames=2, trim="MetalDark")
    pauldron(B, P, -1, "Leather", r=0.085, lames=1, theta=1.1, lift=0.02, out=0.012)
    trousers(B, P, "ClothDark", add=0.008, t1=0.8)
    leg_wraps(B, P, "Under", "Leather", t0=0.56, t1=0.86, add=0.014)
    boots(B, P, "Leather", top_t=0.82, add=0.02, cuff_slot="Fur", fur=True, sole_slot="LeatherDark")


def outfit_berserker(B, C, P):
    base_head(B, P)
    shirt(B, P, "Under", add=0.005, z0=0.86 * P.zs)
    shirt(B, P, "Cloth", add=0.015, z0=0.88 * P.zs)
    # steel lamellar: rows of plates
    rows = [(z * P.zs, 0.006) for z in (1.04, 1.09, 1.14, 1.19, 1.24, 1.29)]
    cuirass(B, P, "Metal", 1.0 * P.zs, P.neck_base - 0.035, 0.034, ridges=rows, trim="MetalDark")
    # mail skirt under a red war-skirt
    skirt(B, P, "MetalDark", 0.97 * P.zs, (0.60 if P.female else 0.62) * P.zs, 0.03, 0.035, panels=8, gap=0.0, leg_share=0.65)
    skirt(B, P, "Cloth", 0.97 * P.zs, (0.66 if P.female else 0.70) * P.zs, 0.042, 0.04, panels=6, gap=0.05, start=0.0,
          tatter=0.05, only=lambda a: math.cos(a) > -0.2)
    belt(B, P, "LeatherDark", 0.99 * P.zs, h=0.07, add=0.05, buckle_slot="Gold", buckle="square")
    # bear pelt over the shoulders
    # bear pelt: open at the front, over the shoulders and down the back
    fur_collar(B, P, "Fur", rx=0.235, depth=(0.15, 0.175), drop=0.06, jag=0.032, back_drop=0.17, height=0.025, n=34)
    pauldron(B, P, 1, "MetalDark", r=0.12, lames=3, trim="Metal", spikes=3, spike_slot="Metal", rune_slot="Glow")
    pauldron(B, P, -1, "MetalDark", r=0.12, lames=3, trim="Metal", spikes=3, spike_slot="Metal", rune_slot="Glow")
    sleeves(B, P, "Cloth", add=0.012, t0=0.0, t1=0.62)
    bracers(B, P, "MetalDark", t0=0.6, t1=0.95, add=0.018, cuff=0.02, trim="Metal")
    for side in (1, -1):
        rune_line_arm(B, P, side, "Glow", 0.68, 0.88, 0.024)
    hands(B, P, "MetalDark", spikes_slot="Metal")
    trousers(B, P, "ClothDark", add=0.01, t1=0.9)
    greaves(B, P, "Metal", t0=0.53, t1=0.9, add=0.022, knee_slot="MetalDark", trim="MetalDark")
    boots(B, P, "LeatherDark", top_t=0.86, add=0.024, cuff_slot="Fur", fur=True, sole_slot="LeatherDark", toe_cap="MetalDark")


def outfit_guardian(B, C, P):
    base_head(B, P)
    shirt(B, P, "Under", add=0.005, z0=0.86 * P.zs)
    # mail
    shirt(B, P, "MetalDark", add=0.016, z0=0.88 * P.zs)
    skirt(B, P, "MetalDark", 0.97 * P.zs, (0.66 if P.female else 0.68) * P.zs, 0.022, 0.03, panels=8, gap=0.0, leg_share=0.65)
    cuirass(B, P, "Metal", 1.02 * P.zs, P.neck_base - 0.03, 0.036, trim="Gold", keel=0.01)
    gorget(B, P, "Metal", add=0.02, h=0.045)
    tabard(B, P, "Cloth", 1.12 * P.zs, (0.50 if P.female else 0.52) * P.zs, 0.085 if not P.female else 0.075, 0.105, 0.05, trim="ClothDark",
           point=0.04)
    emblem(B, P, "suncross", "Gold", 1.23 * P.zs, 0.048, size=0.045)
    belt(B, P, "Leather", 1.0 * P.zs, h=0.05, add=0.058, buckle_slot="Gold", buckle="square")
    for side in (1, -1):
        pauldron(B, P, side, "Metal", r=0.115, lames=3, trim="Gold", squash=(1.0, 1.12, 0.78))
    sleeves(B, P, "MetalDark", add=0.012, t0=0.0, t1=0.62)
    couters(B, P, "Metal")
    bracers(B, P, "Metal", t0=0.6, t1=0.94, add=0.018, cuff=0.01, trim="Gold")
    hands(B, P, "Metal")
    trousers(B, P, "MetalDark", add=0.01, t1=0.9)
    cuisses(B, P, "Metal", t0=0.12, t1=0.42)
    greaves(B, P, "Metal", t0=0.52, t1=0.92, add=0.022, knee_slot="Metal", trim="Gold")
    boots(B, P, "MetalDark", top_t=0.88, add=0.024, sole_slot="LeatherDark", toe_cap="Metal", plated=True)


def outfit_einherjar(B, C, P):
    base_head(B, P)
    shirt(B, P, "Under", add=0.006, z0=0.86 * P.zs)
    skirt(B, P, "Cloth", 0.97 * P.zs, (0.50 if P.female else 0.54) * P.zs, 0.02, 0.05, panels=8, gap=0.02, trim="Gold", leg_share=0.65)
    cuirass(B, P, "Metal", 1.0 * P.zs, P.neck_base - 0.03, 0.036, trim="Gold", keel=0.012, lames=2, lame_slot="Metal")
    rune_line(B, P, "Glow", [(0.0, 1.06 * P.zs), (0.0, 1.16 * P.zs), (0.018, 1.2 * P.zs), (0.0, 1.24 * P.zs), (-0.018, 1.28 * P.zs)],
              0.06, width=0.008)
    # tassets over the crimson skirt
    skirt(B, P, "Metal", 0.95 * P.zs, (0.74 if P.female else 0.76) * P.zs, 0.05, 0.03, panels=6, gap=0.08, start=math.pi / 6,
          trim="Gold", only=lambda a: math.cos(a) > -0.6, leg_share=0.75)
    belt(B, P, "LeatherDark", 0.985 * P.zs, h=0.05, add=0.05, buckle_slot="Gold", buckle="round")
    fur_collar(B, P, "Fur", rx=0.2, depth=(0.13, 0.145), drop=0.085, jag=0.022, back_drop=0.05, height=0.03)
    for side in (1, -1):
        pauldron(B, P, side, "Metal", r=0.125, lames=3, trim="Gold", flare_top=0.01, rune_slot="Glow", squash=(1.0, 1.1, 0.8))
    sleeves(B, P, "Under", add=0.012, t0=0.0, t1=0.62)
    couters(B, P, "Metal")
    bracers(B, P, "Metal", t0=0.6, t1=0.95, add=0.018, cuff=0.016, trim="Gold")
    for side in (1, -1):
        rune_line_arm(B, P, side, "Glow", 0.7, 0.88, 0.03)
    hands(B, P, "MetalDark")
    trousers(B, P, "Under", add=0.01, t1=0.9)
    cuisses(B, P, "Metal", t0=0.12, t1=0.40)
    greaves(B, P, "Metal", t0=0.52, t1=0.92, add=0.022, knee_slot="Gold", trim="Gold")
    boots(B, P, "MetalDark", top_t=0.88, add=0.024, sole_slot="LeatherDark", toe_cap="Metal", plated=True)
    cape(C, P, "Cloth", "ClothDark", z_bot=0.30 * P.zs, w_top=2.6, w_bot=1.5, r_top=0.21, r_bot=0.33, back_bot=0.08, folds=5)


def outfit_valkyrie(B, C, P):
    base_head(B, P)
    shirt(B, P, "Under", add=0.006, z0=0.86 * P.zs)
    skirt(B, P, "Cloth", 0.97 * P.zs, (0.44 if P.female else 0.5) * P.zs, 0.02, 0.06, panels=8, gap=0.012, trim="Gold", leg_share=0.65,
          length_fn=lambda a: 0.06 * (1 - abs(math.cos(a))) if P.female else 0.0)
    cuirass(B, P, "Metal", 1.0 * P.zs, P.neck_base - 0.03, 0.034, trim="Gold", keel=0.01)
    emblem(B, P, "wings", "Gold", 1.22 * P.zs, 0.046, size=0.04)
    skirt(B, P, "Metal", 0.95 * P.zs, (0.78 if P.female else 0.78) * P.zs, 0.045, 0.025, panels=6, gap=0.1, start=math.pi / 6, trim="Gold",
          only=lambda a: math.cos(a) > -0.6, leg_share=0.75)
    belt(B, P, "Leather", 0.985 * P.zs, h=0.045, add=0.048, buckle_slot="Gold", buckle="square")
    gorget(B, P, "Metal", add=0.018, h=0.04)
    for side in (1, -1):
        pauldron(B, P, side, "Metal", r=0.11, lames=2, trim="Gold", wing_slot="Fur", squash=(1.0, 1.1, 0.8))
    sleeves(B, P, "Under", add=0.012, t0=0.0, t1=0.62)
    bracers(B, P, "Metal", t0=0.58, t1=0.95, add=0.016, cuff=0.018, trim="Gold")
    hands(B, P, "Metal")
    trousers(B, P, "Under", add=0.01, t1=0.9)
    greaves(B, P, "Metal", t0=0.5, t1=0.92, add=0.022, knee_slot="Metal", trim="Gold")
    boots(B, P, "MetalDark", top_t=0.86, add=0.024, sole_slot="LeatherDark", toe_cap="Metal", plated=True)
    cape(C, P, "ClothDark", "Cloth", z_bot=0.42 * P.zs, w_top=2.4, w_bot=1.5, r_top=0.2, r_bot=0.3, back_bot=0.06, folds=4)


def rune_line_arm(B, P, side, slot, t0, t1, add):
    path = P.arm_path(side, t0, t1, 4)
    nrms = []
    pts = []
    for p in path:
        n = V((side * 0.35, -1.0, 0.0)).normalized()
        t = (t0 + t1) * 0.5
        pts.append(p + n * (P.arm_radius(t) + add))
        nrms.append(n)
    B.add(strap(pts, 0.007, 0.004, nrms), slot, lambda p, s=side: P.chain_weights(p, P.arm_chain(s)))


OUTFITS = {
    "common": outfit_common,
    "warrior": outfit_warrior,
    "berserker": outfit_berserker,
    "guardian": outfit_guardian,
    "einherjar": outfit_einherjar,
    "valkyrie": outfit_valkyrie,
}


# ---------------------------------------------------------------------------------------------- worn capes
# A cloak from the Garment slot (Traveler's Cloak, Wolfskin Mantle, Valkyrian Manteau...) is drawn with this outfit's own
# fitted cape ("GarmentCape", hidden until worn, painted in the item's color), cut wide enough to clear its armour.
GARMENT_FIT = {
    "common": dict(r_top=0.2, r_bot=0.3, w_top=2.3, w_bot=1.4, back_bot=0.06, z_bot=0.4),
    "warrior": dict(r_top=0.215, r_bot=0.31, w_top=2.2, w_bot=1.4, back_bot=0.07, z_bot=0.4),
    "berserker": dict(r_top=0.25, r_bot=0.34, w_top=2.2, w_bot=1.45, back_bot=0.08, z_bot=0.4, hug=0.95),
    "guardian": dict(r_top=0.225, r_bot=0.33, w_top=2.3, w_bot=1.45, back_bot=0.08, z_bot=0.4),
    "einherjar": dict(r_top=0.215, r_bot=0.33, w_top=2.6, w_bot=1.5, back_bot=0.08, z_bot=0.32),
    "valkyrie": dict(r_top=0.21, r_bot=0.31, w_top=2.4, w_bot=1.5, back_bot=0.07, z_bot=0.4),
}


def garment_cape(G, P, key):
    fit = dict(GARMENT_FIT.get(key, GARMENT_FIT["common"]))
    fit["z_bot"] = fit["z_bot"] * P.zs
    cape(G, P, "Garment", "GarmentDark", folds=5, **fit)

# Preview colors for the worn-cloak slots (the game paints them in the garment item's color).
GARMENT_PREVIEW = {"Garment": "#784212", "GarmentDark": "#4A280B"}
