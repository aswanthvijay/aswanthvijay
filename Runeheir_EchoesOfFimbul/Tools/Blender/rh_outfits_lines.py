"""The other job lines' outfits (Art Pass 2). Importing this module registers them with rh_outfits.

Every job but the Initiate and the Wanderer (Ragnarok's Novice and Super Novice, who keep the travelling clothes) now has
an outfit. A second job and its transcendent job share a model; the cloth that carries the job's own color ($outfit) tells
them apart, so the Sorcerer's robe is midnight blue and the Archmage's a deeper one.

  mystic     Mystic, Freyja's Kin   apprentice's knee robe, hood down, satchel and a book of runes
  sorcerer   Sorcerer, Archmage     ankle robe open over dark breeches, gold-trimmed mantle, glowing runes, long cape
  sage       Sage, Chronomancer,    scholar's coat with a valknut tabard, a book at the hip, one leather pauldron
             Fylgja Caller
  devotee    Devotee                novice's white robe, the stole in the job color, a rope belt, a sun-cross
  gothi      Gothi, High Gothi      white vestments, a tabard in the job color with the valknut, gold mantle and cape
  monk       Monk, Champion, Glíma  sleeveless gi, bare arms with wrist wraps, a wide sash with long tails, prayer beads
             Fighter, Sól Guardian
  huntsman   Huntsman, Ranger,      leather jerkin, hood down, a quiver on the back, an archer's bracer
             Deadeye
  skald      Skald, Thul            minstrel's tunic with gold trim, puffed sleeves, a short cape and the harp strap
  seidkona   Seidkona, Völva        split skirt to the ankle, a bodice, bare arms with gold bangles, a fur mantle
  trader     Trader                 vest and rolled sleeves, a leather apron, coin pouches
  smith      Runesmith, Forgelord   heavy leather apron with forge runes, bare forearms, great gloves
  brewer     Brewmaster, Lifeweaver long coat open at the front, a high collar, a belt of glowing vials, a bandolier
  scout      Scout                  light leather vest, a scarf, arm wraps, a dagger harness
  assassin   Assassin, Shadow       close dark wraps, a high collar to the chin, long sash tails, spiked arm guards
             Walker, Nightraider
  outlaw     Outlaw, Vargr,         a long coat open at the front, a wolf-pelt collar, a bandolier
             Thunderer
"""

import math

import rh_outfits as O
from rh_body import FRONT, zcut_weights
from rh_geo import V, lerp, loft, ring, sheet, smooth, tube

# ---------------------------------------------------------------------------------------------- palettes
_BASE = {"Under": "#D9CDB4", "Leather": "#7A5534", "LeatherDark": "#4A3222", "Metal": "#98A0AA", "MetalDark": "#5A636E",
         "Gold": "#C29A48", "Fur": "#CFC4B2", "Glow": "#7FD0FF"}


def _pal(**slots):
    pal = {"Cloth": "$outfit", "ClothDark": "$outfitDark", **_BASE}
    pal.update(slots)
    return pal


LINE_PALETTES = {
    "mystic": _pal(Under="#D8CFC0", Glow="#8FD8FF"),
    "sorcerer": _pal(Under="#2A2833", Gold="#D2A54A", Glow="#7FD6FF", Fur="#3A3446"),
    "sage": _pal(Under="#E3DCCB", Gold="#C9A04A", Glow="#B8A8FF"),
    "devotee": _pal(Cloth="#E8E2D4", ClothDark="$outfit", Under="#CFC6B4", Leather="#B39668", Glow="#FFE9A6"),
    "gothi": _pal(Cloth="#EEE9DD", ClothDark="$outfit", Under="#D8D0BF", Gold="#D8B04E", Glow="#FFE9A6"),
    "monk": _pal(Under="#E2D6BE", Leather="#6B4A2E", LeatherDark="#3A2618", Glow="#FFB45A"),
    "huntsman": _pal(Leather="#7C5A36", LeatherDark="#4A3420", Fur="#E4DDCF", Glow="#B6F07A"),
    "skald": _pal(Under="#E9DFC8", Gold="#D6A947", Glow="#FFD27A"),
    "seidkona": _pal(Under="#E8DCCD", Gold="#D9B24C", Fur="#BFB6A8", Glow="#E6A6FF"),
    "trader": _pal(Under="#E3D8C0", Leather="#8A6238", LeatherDark="#4E3622", Gold="#CFA64C", Glow="#FFD27A"),
    "smith": _pal(Under="#CDBEA2", Leather="#6A4A30", LeatherDark="#3A281A", Metal="#8E979F", Glow="#FF7A2E"),
    "brewer": _pal(Under="#E2DBCA", Leather="#6E5236", LeatherDark="#3E2E20", Glow="#6CFFB0"),
    "scout": _pal(Under="#CFC7B4", Leather="#6E5638", LeatherDark="#3C2E1F", Glow="#9CFFB0"),
    "assassin": _pal(Under="#26262C", Leather="#3A302A", LeatherDark="#211B18", Metal="#8C939C", MetalDark="#3F454D",
                     Gold="#A88A48", Glow="#FF5A5A"),
    "outlaw": _pal(Under="#D2C6AE", Leather="#5E4430", LeatherDark="#33251A", Fur="#6E6660", Glow="#FFB45A"),
}

LINE_JOBS = {
    "mystic": ["Mystic", "FreyjasKin"],
    "sorcerer": ["Sorcerer", "Archmage"],
    "sage": ["Sage", "Chronomancer", "FylgjaCaller"],
    "devotee": ["Devotee"],
    "gothi": ["Gothi", "HighGothi"],
    "monk": ["Monk", "Champion", "GlimaFighter", "SolGuardian"],
    "huntsman": ["Huntsman", "Ranger", "Deadeye"],
    "skald": ["Skald", "Thul"],
    "seidkona": ["Seidkona", "Volva"],
    "trader": ["Trader"],
    "smith": ["Runesmith", "Forgelord"],
    "brewer": ["Brewmaster", "Lifeweaver"],
    "scout": ["Scout"],
    "assassin": ["Assassin", "ShadowWalker", "Nightraider"],
    "outlaw": ["Outlaw", "Vargr", "Thunderer"],
}

# The job color each outfit shows in the Blender previews (the game paints $outfit per job).
PREVIEW_JOB = {
    "mystic": "#3B4F8C", "sorcerer": "#2C3E7A", "sage": "#4A6FA5", "devotee": "#C9A227", "gothi": "#D4AC0D",
    "monk": "#B9770E", "huntsman": "#5E7D3A", "skald": "#8C5A2B", "seidkona": "#8E44AD", "trader": "#A0642D",
    "smith": "#6E2C00", "brewer": "#117A65", "scout": "#3E6B48", "assassin": "#1E3D2F", "outlaw": "#5B4636",
}


# ---------------------------------------------------------------------------------------------- new pieces
def robe(B, P, slot, z_bot, flare, trim=None, panels=10, add=0.022, open_front=False, z_top=None, leg_share=0.55, gap=0.012):
    """A long skirt of panels from the hips (z_bot in body heights). open_front leaves a gap down the front."""
    z_top = 0.97 * P.zs if z_top is None else z_top
    span = 2.0 * math.pi / panels
    only = (lambda a: math.cos(a) < 0.88) if open_front else None
    O.skirt(B, P, slot, z_top, z_bot * P.zs, add, flare, panels=panels, gap=gap, trim=trim, leg_share=leg_share, only=only,
            start=0.0 if open_front else -span * 0.5)


def bare_arms(B, P, t1=0.95):
    O.sleeves(B, P, "Skin", add=0.0, t0=0.0, t1=t1)


def sash_tails(B, P, slot, z, side, length, slope=0.08, width=0.05, count=2, add=0.05, back=False, angle=None):
    """The hanging ends of a knotted sash at the hip (side 1 = the character's left), leaning out over what's below.
    angle: where around the hip, as a fraction of a half turn from the front (default 0.3, or 0.7 at the back)."""
    a_c = side * math.pi * (angle if angle is not None else 0.7 if back else 0.3)
    leg = O.side_name(side) + "UpperLeg"
    for k in range(count):
        a = a_c + side * 0.17 * k
        p0, n0 = P.surface(a, z, add + 0.004 * k)
        n0 = V((n0.x, n0.y, 0.0)).normalized()
        tang = V((n0.y, -n0.x, 0.0))
        L = length * (1.0 - 0.2 * k)

        def place(u, v, p0=p0, n0=n0, tang=tang, L=L):
            return p0 + tang * ((u - 0.5) * width) + n0 * (slope * L * v) + V((0.0, 0.0, -L * v)), n0

        def weigh(p, leg=leg):
            f = smooth((z - p.z) / max(length, 1e-3)) * 0.6
            return {"Hips": 1.0 - f, leg: f}

        B.add(sheet(2, 7, place, 0.006), slot, weigh)


def cowl(B, P, slot):
    """A hood worn down: a soft roll of cloth around the neck, deeper at the back."""
    O.fur_collar(B, P, slot, rx=0.175, depth=(0.105, 0.15), drop=0.05, jag=0.003, back_drop=0.1, height=0.05, n=32)


def mantle(B, P, slot, rx=0.25, drop=0.15, back_drop=0.06):
    """A capelet over the shoulders."""
    O.fur_collar(B, P, slot, rx=rx, depth=(0.15, 0.165), drop=drop, jag=0.004, back_drop=back_drop, height=0.03, n=34)


def high_collar(B, P, slot, h=0.075, front=0.012):
    """A stiff collar up to the jaw (scarves, alchemists' coats, assassins' wraps)."""
    nb = P.neck_base
    rings_ = []
    for k, (zz, extra) in enumerate(((nb - 0.035, 0.034), (nb + h * 0.45, 0.014), (nb + h, 0.02))):
        r = P.neck_r + extra + 0.008
        rings_.append(ring(V((0.0, 0.012 - front * (k / 2.0), zz)), V((1.0, 0.0, 0.0)), FRONT, r + 0.006, r + front * (k / 2.0), r, 20, 2.0))
    B.add(loft(rings_, True, True), slot, lambda p: zcut_weights(p.z, [(-9, "Chest"), (nb + 0.03, "Neck")], 0.02))


def box_at(B, P, slot, a, z, add, size, weigh=None):
    """A small box (book, pouch) against the body at angle a, height z."""
    w, d, h = size
    p, n = P.surface(a, z, add)
    n = V((n.x, n.y, 0.0)).normalized()
    c = p + n * (d * 0.5)
    B.add(tube([c - V((0.0, 0.0, h * 0.5)), c + V((0.0, 0.0, h * 0.5))], [(w * 0.5, d * 0.5, d * 0.5)] * 2, n=12, e=5.0,
               fwd_hint=(n.x, n.y, 0.0)), slot, weigh or P.torso_weights)
    return c, n


def book(B, P, cover, clasp, a, z, add):
    c, n = box_at(B, P, cover, a, z, add, (0.075, 0.032, 0.1))
    B.add(tube([c + n * 0.012 - V((0.0, 0.0, 0.006)), c + n * 0.012 + V((0.0, 0.0, 0.006))], [(0.04, 0.008, 0.008)] * 2, n=8, e=4.0,
               fwd_hint=(n.x, n.y, 0.0)), clasp, P.torso_weights)


def pouches(B, P, slot, z, angles, add):
    for a in angles:
        box_at(B, P, slot, a, z - 0.04, add, (0.06, 0.035, 0.065))


def vials(B, P, glass, cap, z, xs, add):
    """Little glowing flasks hanging from the belt."""
    for x in xs:
        p, n = O.front_point(P, x, z, add)
        base = p + n * 0.014
        B.add(tube([base + V((0.0, 0.0, -0.05)), base + V((0.0, 0.0, -0.03)), base + V((0.0, 0.0, -0.008))], [0.006, 0.014, 0.007], n=10,
                   fwd_hint=(0.0, -1.0, 0.0)), glass, P.torso_weights)
        B.add(tube([base + V((0.0, 0.0, -0.01)), base + V((0.0, 0.0, 0.006))], [0.0075, 0.0075], n=8, fwd_hint=(0.0, -1.0, 0.0)), cap,
              P.torso_weights)


def quiver(B, P, slot, trim, fletch, shafts):
    """A quiver across the back, mouth over the right shoulder (the drawing hand's)."""
    bottom = P.surface(math.pi - 0.55, 0.99 * P.zs, 0.07)[0]
    top = P.surface(math.pi + 0.6, 1.37 * P.zs, 0.085)[0]
    axis = (top - bottom).normalized()
    B.add(tube([bottom - axis * 0.02, bottom, top], [0.03, 0.044, 0.05], n=12, fwd_hint=(0.0, 1.0, 0.0)), slot, P.torso_weights)
    for t in (0.12, 0.85):
        c = bottom.lerp(top, t)
        B.add(tube([c - axis * 0.012, c + axis * 0.012], [0.05, 0.051], n=12, fwd_hint=(0.0, 1.0, 0.0)), trim, P.torso_weights)
    side = axis.cross(V((0.0, 1.0, 0.0))).normalized()
    for k in range(5):
        off = side * (0.022 * math.cos(k * 1.3)) + V((0.0, 0.018 * math.sin(k * 1.3), 0.0))
        s0 = top + off
        s1 = s0 + axis * (0.05 + 0.012 * (k % 2))
        B.add(tube([s0 - axis * 0.03, s1], [0.0045, 0.0045], n=6, fwd_hint=(0.0, 1.0, 0.0)), shafts, P.torso_weights)
        B.add(tube([s1 - axis * 0.035, s1 - axis * 0.01, s1 + axis * 0.004], [(0.003, 0.014, 0.014), (0.003, 0.016, 0.016), (0.003, 0.004, 0.004)],
                   n=6, fwd_hint=(side.x, side.y, side.z)), fletch, P.torso_weights)


def beads(B, P, slot, big_slot, count=15):
    """A loop of prayer beads from the neck down onto the chest (the back of the loop hides under the collar)."""
    nb = P.neck_base
    for i in range(count):
        u = i / (count - 1.0)
        a = lerp(-1.25, 1.25, u)
        dip = 1.0 - abs(2.0 * u - 1.0)
        z = nb - 0.025 - 0.15 * dip ** 0.8
        x = math.sin(a) * lerp(0.075, 0.05, dip)
        p, n = O.front_point(P, x, z, 0.022)
        r = 0.017 if i == count // 2 else 0.011
        B.add(tube([p - V((0.0, 0.0, r)), p, p + V((0.0, 0.0, r))], [r * 0.25, r, r * 0.25], n=8, fwd_hint=(0.0, -1.0, 0.0)),
              big_slot if i == 0 else slot, P.torso_weights)


def bangles(B, P, slot, ts=(0.8, 0.86)):
    for t in ts:
        O.bracers(B, P, slot, t0=t, t1=t + 0.025, add=0.012, cuff=0.0)


def apron(B, P, slot, z_top, z_bot, w_top, w_bot, add, strap_slot, trim=None):
    O.tabard(B, P, slot, z_top, z_bot, w_top, w_bot, add, trim=trim, back=False, flare=0.035)
    for side in (1, -1):
        O.front_strap(B, P, strap_slot, [(side * w_top * 0.85, z_top - 0.01), (side * 0.06, P.neck_base - 0.015)], 0.018, add + 0.004)


def stole(B, P, slot, z_bot, add, trim=None):
    """A priest's stole: two bands from the neck down the front."""
    for side in (1, -1):
        pts = [(side * 0.055, P.neck_base - 0.01), (side * 0.06, 1.2 * P.zs), (side * 0.058, z_bot)]
        O.front_strap(B, P, slot, pts, 0.042, add)
        if trim:
            O.front_strap(B, P, trim, [(side * 0.058, z_bot + 0.012), (side * 0.058, z_bot)], 0.046, add + 0.002)


# ---------------------------------------------------------------------------------------------- outfits
def outfit_mystic(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Under", add=0.005, z0=0.86 * P.zs)
    O.shirt(B, P, "Cloth", add=0.016, z0=0.92 * P.zs, z1=P.neck_base - 0.01)
    robe(B, P, "Cloth", 0.5, 0.06, trim="ClothDark", panels=8)
    cowl(B, P, "ClothDark")
    O.sleeves(B, P, "Cloth", add=0.012, t0=0.0, t1=0.86, cuff=0.022)
    O.belt(B, P, "ClothDark", 0.99 * P.zs, h=0.05, add=0.032)
    sash_tails(B, P, "ClothDark", 0.98 * P.zs, -1, 0.26, slope=0.16, width=0.045)
    O.harness(B, P, "Leather", add=0.03)
    book(B, P, "LeatherDark", "Gold", math.pi * 0.52, 0.96 * P.zs, 0.06)
    O.trousers(B, P, "Under", add=0.008, t1=0.85)
    O.boots(B, P, "Leather", top_t=0.8, add=0.018, sole_slot="LeatherDark", cuff_slot="LeatherDark")
    O.hands(B, P, "Skin")


def outfit_sorcerer(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Under", add=0.006, z0=0.86 * P.zs)
    O.shirt(B, P, "Cloth", add=0.017, z0=0.94 * P.zs, z1=P.neck_base - 0.012)
    robe(B, P, "Cloth", 0.13, 0.1, trim="Gold", panels=12, open_front=True)
    O.rune_line(B, P, "Glow", [(0.0, 1.02 * P.zs), (0.0, 1.14 * P.zs), (0.02, 1.19 * P.zs), (0.0, 1.24 * P.zs), (-0.02, 1.29 * P.zs)],
                0.03, width=0.008)
    mantle(B, P, "ClothDark", rx=0.24, drop=0.13)
    high_collar(B, P, "ClothDark", h=0.06, front=0.0)
    O.sleeves(B, P, "Cloth", add=0.012, t0=0.0, t1=0.88, cuff=0.034)
    bangles(B, P, "Gold", ts=(0.84,))
    O.belt(B, P, "LeatherDark", 0.99 * P.zs, h=0.05, add=0.034, buckle_slot="Gold", buckle="round")
    O.trousers(B, P, "Under", add=0.01, t1=0.9)
    O.boots(B, P, "LeatherDark", top_t=0.84, add=0.018, sole_slot="LeatherDark", cuff_slot="Gold")
    O.hands(B, P, "Skin")
    O.cape(C, P, "ClothDark", "Cloth", z_bot=0.2 * P.zs, w_top=2.4, w_bot=1.6, r_top=0.22, r_bot=0.34, back_bot=0.1, folds=6)


def outfit_sage(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Under", add=0.006, z0=0.86 * P.zs)
    O.shirt(B, P, "Cloth", add=0.016, z0=0.92 * P.zs, z1=P.neck_base - 0.012)
    robe(B, P, "Cloth", 0.4, 0.07, trim="ClothDark", panels=10)
    O.tabard(B, P, "Under", 1.3 * P.zs, 0.46 * P.zs, 0.07, 0.085, 0.04, trim="Gold", point=0.03)
    O.emblem(B, P, "valknut", "Gold", 1.2 * P.zs, 0.05, size=0.04)
    O.gorget(B, P, "ClothDark", add=0.012, h=0.035)
    O.pauldron(B, P, -1, "Leather", r=0.09, lames=2, trim="Gold", theta=1.1, lift=0.018)
    O.sleeves(B, P, "Cloth", add=0.012, t0=0.0, t1=0.62)
    O.bracers(B, P, "Leather", t0=0.6, t1=0.92, add=0.014, cuff=0.01, trim="Gold")
    O.belt(B, P, "LeatherDark", 0.99 * P.zs, h=0.045, add=0.05, buckle_slot="Gold", buckle="square")
    book(B, P, "LeatherDark", "Gold", math.pi * 0.55, 0.95 * P.zs, 0.075)
    pouches(B, P, "Leather", 0.99 * P.zs, (-math.pi * 0.55,), 0.06)
    O.trousers(B, P, "ClothDark", add=0.009, t1=0.9)
    O.boots(B, P, "Leather", top_t=0.82, add=0.02, sole_slot="LeatherDark", cuff_slot="LeatherDark")
    O.hands(B, P, "Skin")


def outfit_devotee(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Under", add=0.005, z0=0.86 * P.zs)
    O.shirt(B, P, "Cloth", add=0.016, z0=0.92 * P.zs, z1=P.neck_base - 0.01)
    robe(B, P, "Cloth", 0.15, 0.09, panels=10)
    cowl(B, P, "Cloth")
    stole(B, P, "ClothDark", 0.8 * P.zs, 0.03, trim="Gold")
    O.emblem(B, P, "suncross", "Gold", 1.26 * P.zs, 0.036, size=0.03)
    O.sleeves(B, P, "Cloth", add=0.012, t0=0.0, t1=0.88, cuff=0.03)
    O.belt(B, P, "Leather", 0.99 * P.zs, h=0.025, add=0.036)
    sash_tails(B, P, "Leather", 0.98 * P.zs, 1, 0.32, slope=0.2, width=0.018, add=0.04)
    O.trousers(B, P, "Under", add=0.008, t1=0.9)
    O.boots(B, P, "Leather", top_t=0.9, add=0.014, sole_slot="LeatherDark")
    O.hands(B, P, "Skin")


def outfit_gothi(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Under", add=0.005, z0=0.86 * P.zs)
    O.shirt(B, P, "Cloth", add=0.016, z0=0.92 * P.zs, z1=P.neck_base - 0.01)
    robe(B, P, "Cloth", 0.13, 0.1, trim="Gold", panels=12)
    O.tabard(B, P, "ClothDark", 1.3 * P.zs, 0.3 * P.zs, 0.07 if not P.female else 0.062, 0.09, 0.034, trim="Gold", flare=0.08, point=0.05)
    O.emblem(B, P, "valknut", "Gold", 1.2 * P.zs, 0.046, size=0.042)
    mantle(B, P, "Cloth", rx=0.24, drop=0.12, back_drop=0.04)
    O.gorget(B, P, "Gold", add=0.016, h=0.03)
    O.sleeves(B, P, "Cloth", add=0.012, t0=0.0, t1=0.88, cuff=0.034)
    bangles(B, P, "Gold", ts=(0.85,))
    O.belt(B, P, "Gold", 0.99 * P.zs, h=0.03, add=0.05)
    O.trousers(B, P, "Under", add=0.008, t1=0.9)
    O.boots(B, P, "Leather", top_t=0.9, add=0.014, sole_slot="LeatherDark")
    O.hands(B, P, "Skin")
    O.cape(C, P, "Cloth", "ClothDark", z_bot=0.24 * P.zs, w_top=2.3, w_bot=1.5, r_top=0.22, r_bot=0.33, back_bot=0.09, folds=5)


def outfit_monk(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Cloth", add=0.012, z0=0.9 * P.zs, z1=P.neck_base - 0.008)
    bare_arms(B, P)
    O.bracers(B, P, "Under", t0=0.7, t1=0.95, add=0.008, cuff=0.0, ridges=[(0.74 + 0.04 * k, 0.003) for k in range(5)])
    O.hands(B, P, "Skin")
    O.belt(B, P, "ClothDark", 0.98 * P.zs, h=0.085, add=0.03)
    sash_tails(B, P, "ClothDark", 0.97 * P.zs, -1, 0.4, slope=0.1, width=0.075, angle=0.14)
    beads(B, P, "LeatherDark", "Gold")
    O.trousers(B, P, "Under", add=0.022, t1=0.86, ridges=[(0.2, 0.004), (0.35, 0.004), (0.5, 0.003)])
    O.leg_wraps(B, P, "Under", "Leather", t0=0.62, t1=0.94, add=0.016)
    O.boots(B, P, "Leather", top_t=0.92, add=0.012, sole_slot="LeatherDark")


def outfit_huntsman(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Under", add=0.005, z0=0.86 * P.zs)
    O.shirt(B, P, "Cloth", add=0.014, z0=0.9 * P.zs, z1=P.neck_base - 0.01)
    O.cuirass(B, P, "Leather", 1.0 * P.zs, P.neck_base - 0.05, 0.026, trim="LeatherDark",
              ridges=[(z * P.zs, 0.003) for z in (1.08, 1.16, 1.24)])
    O.skirt(B, P, "Leather", 0.96 * P.zs, (0.66 if P.female else 0.7) * P.zs, 0.034, 0.03, panels=6, gap=0.03, start=math.pi / 6,
            trim="LeatherDark")
    cowl(B, P, "Cloth")
    quiver(B, P, "LeatherDark", "Gold", "Fur", "Leather")
    O.harness(B, P, "LeatherDark", metal_slot="Metal", add=0.034)
    O.sleeves(B, P, "Cloth", add=0.011, t0=0.0, t1=0.6)
    O.bracers(B, P, "LeatherDark", t0=0.6, t1=0.94, add=0.014, cuff=0.012, trim="Leather")
    O.hands(B, P, "LeatherDark")
    O.belt(B, P, "LeatherDark", 0.99 * P.zs, h=0.045, add=0.042, buckle_slot="Metal", buckle="square")
    pouches(B, P, "Leather", 0.99 * P.zs, (math.pi * 0.42,), 0.05)
    O.trousers(B, P, "ClothDark", add=0.008, t1=0.84)
    O.leg_wraps(B, P, "Under", "Leather", t0=0.58, t1=0.86, add=0.014)
    O.boots(B, P, "Leather", top_t=0.82, add=0.02, cuff_slot="Fur", fur=True, sole_slot="LeatherDark")


def outfit_skald(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Under", add=0.005, z0=0.86 * P.zs)
    O.shirt(B, P, "Cloth", add=0.016, z0=0.92 * P.zs, z1=P.neck_base - 0.01)
    O.skirt(B, P, "Cloth", 0.97 * P.zs, (0.58 if P.female else 0.62) * P.zs, 0.024, 0.05, panels=8, gap=0.012, trim="Gold")
    O.gorget(B, P, "Gold", add=0.012, h=0.025)
    O.front_strap(B, P, "Gold", [(-0.12, 1.3 * P.zs), (0.0, 1.22 * P.zs), (0.12, 1.3 * P.zs)], 0.012, 0.02)
    O.sleeves(B, P, "Cloth", add=0.02, t0=0.0, t1=0.45, ridges=[(0.12, 0.006), (0.24, 0.006), (0.36, 0.006)])
    O.sleeves(B, P, "Under", add=0.008, t0=0.44, t1=0.9, cuff=0.012)
    O.belt(B, P, "Leather", 0.99 * P.zs, h=0.045, add=0.034, buckle_slot="Gold", buckle="round")
    sash_tails(B, P, "ClothDark", 0.98 * P.zs, 1, 0.3, slope=0.18, width=0.05)
    O.harness(B, P, "LeatherDark", metal_slot="Gold", add=0.03)
    O.trousers(B, P, "ClothDark", add=0.008, t1=0.84)
    O.boots(B, P, "Leather", top_t=0.66, add=0.02, cuff_slot="LeatherDark", sole_slot="LeatherDark")
    O.hands(B, P, "Skin")
    O.cape(C, P, "ClothDark", "Cloth", z_bot=0.62 * P.zs, w_top=2.0, w_bot=1.4, r_top=0.2, r_bot=0.28, back_bot=0.05, folds=4)


def outfit_seidkona(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Under", add=0.004, z0=0.9 * P.zs)
    O.cuirass(B, P, "ClothDark", 1.04 * P.zs, (1.32 if P.female else 1.34) * P.zs, 0.012, trim="Gold")
    robe(B, P, "Cloth", 0.12, 0.08, trim="Gold", panels=8, gap=0.09, leg_share=0.75)
    mantle(B, P, "Fur", rx=0.21, drop=0.08, back_drop=0.08)
    bare_arms(B, P)
    bangles(B, P, "Gold", ts=(0.72, 0.8, 0.86))
    O.hands(B, P, "Skin")
    O.belt(B, P, "Gold", 1.0 * P.zs, h=0.03, add=0.03)
    O.belt(B, P, "ClothDark", 0.965 * P.zs, h=0.045, add=0.03)
    sash_tails(B, P, "Cloth", 0.96 * P.zs, -1, 0.5, slope=0.18, width=0.05, count=2)
    O.trousers(B, P, "Under", add=0.006, t1=0.92)
    O.boots(B, P, "Leather", top_t=0.9, add=0.012, sole_slot="LeatherDark", cuff_slot="Gold")


def outfit_trader(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Under", add=0.006, z0=0.86 * P.zs)
    O.cuirass(B, P, "Cloth", 0.98 * P.zs, P.neck_base - 0.04, 0.02, trim="ClothDark")
    O.skirt(B, P, "Cloth", 0.97 * P.zs, (0.7 if P.female else 0.74) * P.zs, 0.026, 0.03, panels=6, gap=0.03, start=math.pi / 6)
    apron(B, P, "Leather", 1.12 * P.zs, 0.56 * P.zs, 0.085, 0.1, 0.05, "LeatherDark", trim="LeatherDark")
    high_collar(B, P, "ClothDark", h=0.035, front=0.0)
    O.sleeves(B, P, "Under", add=0.008, t0=0.0, t1=0.68)
    O.bracers(B, P, "Under", t0=0.66, t1=0.72, add=0.016, cuff=0.0)
    bare_arms(B, P)
    O.hands(B, P, "Skin")
    O.belt(B, P, "LeatherDark", 0.99 * P.zs, h=0.045, add=0.06, buckle_slot="Gold", buckle="square")
    pouches(B, P, "LeatherDark", 0.99 * P.zs, (math.pi * 0.45, -math.pi * 0.45, math.pi * 0.62), 0.07)
    O.trousers(B, P, "ClothDark", add=0.009, t1=0.86)
    O.boots(B, P, "Leather", top_t=0.8, add=0.02, cuff_slot="LeatherDark", sole_slot="LeatherDark")


def outfit_smith(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Cloth", add=0.01, z0=0.88 * P.zs)
    apron(B, P, "LeatherDark", 1.3 * P.zs, 0.5 * P.zs, 0.11, 0.12, 0.04, "LeatherDark", trim="Leather")
    O.rune_line(B, P, "Glow", [(-0.03, 1.08 * P.zs), (0.0, 1.15 * P.zs), (0.03, 1.08 * P.zs)], 0.09, width=0.008)
    O.rune_line(B, P, "Glow", [(0.0, 1.15 * P.zs), (0.0, 1.0 * P.zs)], 0.09, width=0.008)
    O.sleeves(B, P, "Cloth", add=0.01, t0=0.0, t1=0.36)
    bare_arms(B, P)
    O.bracers(B, P, "Metal", t0=0.66, t1=0.9, add=0.016, cuff=0.014, trim="MetalDark")
    O.hands(B, P, "LeatherDark", cuff_slot="Leather", cuff=0.028)
    O.pauldron(B, P, 1, "Leather", r=0.1, lames=2, trim="LeatherDark", theta=1.15, lift=0.016)
    O.belt(B, P, "Leather", 0.98 * P.zs, h=0.06, add=0.06, buckle_slot="Metal", buckle="square")
    pouches(B, P, "Leather", 0.98 * P.zs, (-math.pi * 0.5,), 0.075)
    O.trousers(B, P, "ClothDark", add=0.01, t1=0.86)
    O.boots(B, P, "LeatherDark", top_t=0.8, add=0.022, sole_slot="LeatherDark", toe_cap="MetalDark")


def outfit_brewer(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Under", add=0.006, z0=0.86 * P.zs)
    O.shirt(B, P, "Cloth", add=0.016, z0=0.93 * P.zs, z1=P.neck_base - 0.012)
    robe(B, P, "Cloth", 0.42, 0.07, trim="ClothDark", panels=10, open_front=True)
    high_collar(B, P, "ClothDark", h=0.07, front=0.006)
    O.harness(B, P, "LeatherDark", metal_slot="Metal", add=0.03)
    O.sleeves(B, P, "Cloth", add=0.012, t0=0.0, t1=0.8, cuff=0.018)
    O.hands(B, P, "LeatherDark", cuff_slot="LeatherDark", cuff=0.02)
    O.belt(B, P, "Leather", 0.99 * P.zs, h=0.045, add=0.04, buckle_slot="Metal", buckle="square")
    vials(B, P, "Glow", "Metal", 0.97 * P.zs, (-0.1, -0.065, 0.065, 0.1), 0.045)
    pouches(B, P, "Leather", 0.99 * P.zs, (math.pi * 0.5,), 0.05)
    O.trousers(B, P, "Under", add=0.009, t1=0.86)
    O.boots(B, P, "LeatherDark", top_t=0.72, add=0.02, cuff_slot="Leather", sole_slot="LeatherDark")


def outfit_scout(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Cloth", add=0.01, z0=0.88 * P.zs)
    O.cuirass(B, P, "Leather", 1.02 * P.zs, (1.3 if P.female else 1.32) * P.zs, 0.022, trim="LeatherDark")
    high_collar(B, P, "ClothDark", h=0.055, front=0.01)
    O.sleeves(B, P, "Cloth", add=0.01, t0=0.0, t1=0.55)
    O.bracers(B, P, "Under", t0=0.55, t1=0.92, add=0.01, cuff=0.0, ridges=[(0.6 + 0.05 * k, 0.003) for k in range(7)])
    O.hands(B, P, "LeatherDark")
    O.harness(B, P, "LeatherDark", metal_slot="Metal", add=0.03)
    O.belt(B, P, "LeatherDark", 0.99 * P.zs, h=0.04, add=0.03, buckle_slot="Metal", buckle="square")
    pouches(B, P, "Leather", 0.99 * P.zs, (math.pi * 0.5, -math.pi * 0.62), 0.04)
    sash_tails(B, P, "ClothDark", 0.98 * P.zs, -1, 0.22, slope=0.12, width=0.04, count=1)
    O.trousers(B, P, "ClothDark", add=0.008, t1=0.86)
    O.leg_wraps(B, P, "Under", "LeatherDark", t0=0.56, t1=0.86, add=0.012)
    O.boots(B, P, "Leather", top_t=0.84, add=0.016, sole_slot="LeatherDark")


def outfit_assassin(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "Cloth", add=0.01, z0=0.88 * P.zs)
    O.cuirass(B, P, "Under", 1.02 * P.zs, (1.28 if P.female else 1.3) * P.zs, 0.018,
              ridges=[(z * P.zs, 0.003) for z in (1.08, 1.14, 1.2)])
    high_collar(B, P, "ClothDark", h=0.09, front=0.02)
    O.pauldron(B, P, 1, "LeatherDark", r=0.085, lames=2, trim="MetalDark", theta=1.05, lift=0.02)
    O.pauldron(B, P, -1, "LeatherDark", r=0.085, lames=2, trim="MetalDark", theta=1.05, lift=0.02)
    O.sleeves(B, P, "Cloth", add=0.01, t0=0.0, t1=0.6)
    O.bracers(B, P, "MetalDark", t0=0.58, t1=0.94, add=0.016, cuff=0.016, trim="Metal")
    O.hands(B, P, "LeatherDark", spikes_slot="Metal")
    O.belt(B, P, "ClothDark", 0.98 * P.zs, h=0.06, add=0.026)
    sash_tails(B, P, "ClothDark", 0.98 * P.zs, 1, 0.55, slope=0.1, width=0.055, back=True)
    sash_tails(B, P, "ClothDark", 0.98 * P.zs, -1, 0.5, slope=0.1, width=0.055, back=True, count=1)
    O.trousers(B, P, "Cloth", add=0.008, t1=0.86)
    O.leg_wraps(B, P, "ClothDark", "LeatherDark", t0=0.5, t1=0.9, add=0.012)
    O.boots(B, P, "LeatherDark", top_t=0.88, add=0.012, sole_slot="LeatherDark")


def outfit_outlaw(B, C, P):
    O.base_head(B, P)
    O.shirt(B, P, "ClothDark", add=0.006, z0=0.86 * P.zs)
    O.shirt(B, P, "Cloth", add=0.018, z0=0.93 * P.zs, z1=P.neck_base - 0.014)
    robe(B, P, "Cloth", 0.4, 0.08, trim="ClothDark", panels=10, open_front=True)
    O.fur_collar(B, P, "Fur", rx=0.2, depth=(0.13, 0.15), drop=0.07, jag=0.026, back_drop=0.08, height=0.03)
    O.harness(B, P, "LeatherDark", metal_slot="Metal", add=0.036)
    O.sleeves(B, P, "Cloth", add=0.013, t0=0.0, t1=0.84, cuff=0.016)
    O.hands(B, P, "LeatherDark")
    O.belt(B, P, "LeatherDark", 0.99 * P.zs, h=0.05, add=0.044, buckle_slot="Metal", buckle="round")
    O.belt(B, P, "Leather", 0.94 * P.zs, h=0.03, add=0.05)
    pouches(B, P, "Leather", 0.99 * P.zs, (math.pi * 0.5, -math.pi * 0.5), 0.05)
    O.trousers(B, P, "ClothDark", add=0.009, t1=0.86)
    O.boots(B, P, "Leather", top_t=0.64, add=0.022, cuff_slot="LeatherDark", sole_slot="LeatherDark")


LINE_OUTFITS = {
    "mystic": outfit_mystic,
    "sorcerer": outfit_sorcerer,
    "sage": outfit_sage,
    "devotee": outfit_devotee,
    "gothi": outfit_gothi,
    "monk": outfit_monk,
    "huntsman": outfit_huntsman,
    "skald": outfit_skald,
    "seidkona": outfit_seidkona,
    "trader": outfit_trader,
    "smith": outfit_smith,
    "brewer": outfit_brewer,
    "scout": outfit_scout,
    "assassin": outfit_assassin,
    "outlaw": outfit_outlaw,
}

# A worn cloak is cut to clear each outfit (long robes need a wider hem).
LINE_GARMENT_FIT = {
    "mystic": dict(r_top=0.21, r_bot=0.33, w_top=2.3, w_bot=1.45, back_bot=0.07, z_bot=0.4),
    "sorcerer": dict(r_top=0.25, r_bot=0.36, w_top=2.4, w_bot=1.6, back_bot=0.1, z_bot=0.32, hug=0.9),
    "sage": dict(r_top=0.215, r_bot=0.34, w_top=2.3, w_bot=1.5, back_bot=0.08, z_bot=0.4),
    "devotee": dict(r_top=0.215, r_bot=0.35, w_top=2.3, w_bot=1.5, back_bot=0.09, z_bot=0.4),
    "gothi": dict(r_top=0.25, r_bot=0.36, w_top=2.4, w_bot=1.6, back_bot=0.1, z_bot=0.32, hug=0.9),
    "monk": dict(r_top=0.2, r_bot=0.3, w_top=2.3, w_bot=1.4, back_bot=0.06, z_bot=0.4),
    "huntsman": dict(r_top=0.24, r_bot=0.33, w_top=2.3, w_bot=1.45, back_bot=0.08, z_bot=0.4, hug=0.9),
    "skald": dict(r_top=0.21, r_bot=0.31, w_top=2.3, w_bot=1.45, back_bot=0.07, z_bot=0.4),
    "seidkona": dict(r_top=0.225, r_bot=0.34, w_top=2.3, w_bot=1.5, back_bot=0.08, z_bot=0.4, hug=0.9),
    "trader": dict(r_top=0.21, r_bot=0.31, w_top=2.3, w_bot=1.4, back_bot=0.07, z_bot=0.4),
    "smith": dict(r_top=0.215, r_bot=0.31, w_top=2.3, w_bot=1.45, back_bot=0.07, z_bot=0.4),
    "brewer": dict(r_top=0.215, r_bot=0.34, w_top=2.3, w_bot=1.5, back_bot=0.08, z_bot=0.4),
    "scout": dict(r_top=0.21, r_bot=0.3, w_top=2.3, w_bot=1.4, back_bot=0.06, z_bot=0.4),
    "assassin": dict(r_top=0.21, r_bot=0.31, w_top=2.3, w_bot=1.4, back_bot=0.07, z_bot=0.4),
    "outlaw": dict(r_top=0.225, r_bot=0.34, w_top=2.3, w_bot=1.5, back_bot=0.08, z_bot=0.4, hug=0.9),
}

O.PALETTES.update(LINE_PALETTES)
O.OUTFIT_JOBS.update(LINE_JOBS)
O.OUTFITS.update(LINE_OUTFITS)
O.GARMENT_FIT.update(LINE_GARMENT_FIT)
O.PREVIEW_JOB.update(PREVIEW_JOB)
