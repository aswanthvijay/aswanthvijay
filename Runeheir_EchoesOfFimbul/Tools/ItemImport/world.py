"""Phase 8.5: XileRetro's world as Runeheir maps.

Every Ragnarok field and dungeon that has monsters becomes a generated Runeheir map under a Norse name, with its
spawns (thinned to Runeheir's density), its boss spawns and portals from Ragnarok's warps; Ragnarok's towns become
towns with a Norn Courier and the town shops. Prontera is Vigrid Haven: its gate opens on the imported plains.
Maps that the warps leave unreachable are joined to the nearest town by a road (Ragnarok's ships and airships).
"""

import collections
import glob
import hashlib
import os
import re
import struct

import names_norse
import script_translate as st

# Ragnarok map prefix -> (Norse place, field word or dungeon word, theme, kind)
PREFIXES = [
    (r"^prt_fild", "Vigrid", "Plains", "Meadow", "Field"), (r"^prt_sewb", "Vigrid", "Sewers B", "IceCavern", "Dungeon"),
    (r"^prt_maze", "Vigrid", "Maze Wood", "BirchForest", "Field"), (r"^gef_fild", "Galdrheim", "Moors", "BirchForest", "Field"),
    (r"^gef_dun", "Galdrheim", "Tower Depths", "Crypt", "Dungeon"), (r"^gefenia", "Galdrheim", "Underworld", "Crypt", "Dungeon"),
    (r"^pay_fild", "Ýdalir", "Woods", "BirchForest", "Field"), (r"^pay_dun", "Ýdalir", "Caves", "IceCavern", "Dungeon"),
    (r"^moc_fild", "Sandmark", "Desert", "Desert", "Field"), (r"^moc_pryd", "Sandmark", "Pyramid", "SandTomb", "Dungeon"),
    (r"^in_sphinx", "Sandmark", "Riddle-Lion Tomb", "SandTomb", "Dungeon"), (r"^iz_dun", "Skjaldvík", "Sea Halls", "IceCavern", "Dungeon"),
    (r"^orcsdun", "Troll", "Warrens", "IceCavern", "Dungeon"), (r"^anthell", "Ant", "Hell", "IceCavern", "Dungeon"),
    (r"^mjolnir_", "Mjölnir", "Fells", "Tundra", "Field"), (r"^mjo_dun", "Mjölnir", "Mines", "IceCavern", "Dungeon"),
    (r"^c_tower", "Tímaborg", "Clocktower", "Crypt", "Dungeon"), (r"^alde_dun", "Tímaborg", "Clock Depths B", "Crypt", "Dungeon"),
    (r"^gl_chyard", "Glasisvellir", "Churchyard", "Crypt", "Dungeon"), (r"^gl_church", "Glasisvellir", "Chapel", "Crypt", "Dungeon"),
    (r"^gl_knt", "Glasisvellir", "Knights' Hall", "Crypt", "Dungeon"), (r"^gl_prison", "Glasisvellir", "Prison", "Crypt", "Dungeon"),
    (r"^gl_sew", "Glasisvellir", "Sewers", "Crypt", "Dungeon"), (r"^gl_step", "Glasisvellir", "Stairs", "Crypt", "Dungeon"),
    (r"^gl_dun", "Glasisvellir", "Underground", "Crypt", "Dungeon"), (r"^gl_cas", "Glasisvellir", "Castle", "Crypt", "Dungeon"),
    (r"^gl_in", "Glasisvellir", "Inner Halls", "Crypt", "Dungeon"), (r"^glast_", "Glasisvellir", "Gates", "Crypt", "Dungeon"),
    (r"^treasure", "Sunken", "Longship", "Crypt", "Dungeon"), (r"^tur_dun", "Shell-Jarl's", "Isle", "Coast", "Dungeon"),
    (r"^xmas_dun", "Jólheim", "Toy Halls", "Crypt", "Dungeon"), (r"^xmas_fild", "Jólheim", "Snowfield", "Tundra", "Field"),
    (r"^um_fild", "Járnviðr", "Forest", "Jungle", "Field"), (r"^um_dun", "Járnviðr", "Root Halls", "IceCavern", "Dungeon"),
    (r"^gld_dun", "Guild", "Vault", "Crypt", "Dungeon"), (r"^beach_dun", "Sólvík", "Sea Caves", "IceCavern", "Dungeon"),
    (r"^cmd_fild", "Sólvík", "Coast", "Coast", "Field"), (r"^mag_dun", "Muspel", "Vents", "Volcano", "Dungeon"),
    (r"^ama_fild", "Austrey", "Fields", "Meadow", "Field"), (r"^ama_dun", "Austrey", "Shrine", "Crypt", "Dungeon"),
    (r"^gon_fild", "Fjallheim", "Slopes", "Meadow", "Field"), (r"^gon_dun", "Fjallheim", "Tower", "Crypt", "Dungeon"),
    (r"^lou_fild", "Drekaborg", "Fields", "Meadow", "Field"), (r"^lou_dun", "Drekaborg", "Tomb", "Crypt", "Dungeon"),
    (r"^ayo_fild", "Suðrey", "Jungle", "Jungle", "Field"), (r"^ayo_dun", "Suðrey", "Ruins", "SandTomb", "Dungeon"),
    (r"^ein_fild", "Smiðjuborg", "Wastes", "Tundra", "Field"), (r"^ein_dun", "Smiðjuborg", "Mines", "IceCavern", "Dungeon"),
    (r"^lhz_fild", "Ljósvellir", "Fields", "Meadow", "Field"), (r"^lhz_dun", "Ljósvellir", "Rune Lab", "Crypt", "Dungeon"),
    (r"^yuno_fild", "Himinbjörg", "Highlands", "Meadow", "Field"), (r"^juperos_", "Dvergr", "Ruins", "Crypt", "Dungeon"),
    (r"^jupe_core", "Dvergr", "Core", "Crypt", "Dungeon"), (r"^kh_kiehl", "Kjölr", "Workshop", "Crypt", "Dungeon"),
    (r"^kh_dun", "Kjölr", "Manor", "Crypt", "Dungeon"), (r"^hu_fild", "Hólmr", "Meadows", "Meadow", "Field"),
    (r"^ra_fild", "Hofland", "Fields", "Meadow", "Field"), (r"^ra_san", "Hof", "Sanctum", "Crypt", "Dungeon"),
    (r"^ice_dun", "Rime", "Caverns", "IceCavern", "Dungeon"), (r"^ve_fild", "Eldgjá", "Canyon", "Desert", "Field"),
    (r"^thor_v", "Thor's", "Volcano", "Volcano", "Dungeon"), (r"^odin_tem", "Odin's", "Temple", "Crypt", "Dungeon"),
    (r"^abyss_", "Fáfnir's", "Lake Caverns", "IceCavern", "Dungeon"), (r"^tha_t", "Hel's", "Tower", "Crypt", "Dungeon"),
    (r"^abbey", "Cursed", "Abbey", "Crypt", "Dungeon"), (r"^nameless_n", "Nameless", "Isle", "Isle", "Field"),
    (r"^mosk_fild", "Garðaríki", "Woods", "BirchForest", "Field"), (r"^mosk_dun", "Garðaríki", "Deep Woods", "BirchForest", "Dungeon"),
    (r"^bra_fild", "Vínland", "Jungle", "Jungle", "Field"), (r"^bra_dun", "Vínland", "Falls Cave", "IceCavern", "Dungeon"),
    (r"^nif_fild", "Niflheim", "Fields", "Tundra", "Field"), (r"^niflheim$", "Niflheim", "Village", "Tundra", "Field"), (r"^nyd_dun", "Níðhöggr's", "Nest", "IceCavern", "Dungeon"),
    (r"^yggdrasil", "Yggdrasil's", "Roots", "BirchForest", "Field"), (r"^spl_fild", "Laufás", "Fields", "Jungle", "Field"),
    (r"^man_fild", "Myrkheim", "Fields", "BirchForest", "Field"), (r"^arug_dun", "Siege", "Proving Grounds", "Crypt", "Dungeon"),
    (r"^schg_dun", "Siege", "Proving Halls", "Crypt", "Dungeon"),
]
TOWNS = {
    "prontera": "Vigrid Haven", "geffen": "Galdrheim", "payon": "Ýdalir", "morocc": "Sandmark", "alberta": "Hafnir",
    "izlude": "Skjaldvík", "aldebaran": "Tímaborg", "yuno": "Himinbjörg", "comodo": "Sólvík", "umbala": "Járnviðr",
    "xmas": "Jólheim", "amatsu": "Austrey", "gonryun": "Fjallheim", "louyang": "Drekaborg", "ayothaya": "Suðrey",
    "einbroch": "Smiðjuborg", "einbech": "Smiðjuvík", "lighthalzen": "Ljósvellir", "hugel": "Hólmr", "rachel": "Hofstaðr",
    "veins": "Eldgjá", "moscovia": "Garðaríki", "brasilis": "Vínland", "manuk": "Myrkheim",
    "splendide": "Laufás", "mid_camp": "Midgard Camp",
}
ROMAN = ["", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII",
         "XIX", "XX", "XXI", "XXII", "XXIII"]


def read_map_sizes(path):
    sizes = {}
    data = open(path, "rb").read()
    off = 8
    while off + 20 <= len(data):
        name = data[off:off + 12].split(b"\0")[0].decode("latin-1")
        xs, ys, length = struct.unpack("<hhi", data[off + 12:off + 20])
        sizes[name] = (xs, ys)
        off += 20 + length
    return sizes


def read_warps(npc_root):
    warps = []
    for path in glob.glob(os.path.join(npc_root, "warps", "**", "*.txt"), recursive=True) + \
            glob.glob(os.path.join(npc_root, "pre-re", "warps", "**", "*.txt"), recursive=True):
        for line in open(path, encoding="utf-8", errors="replace"):
            if line.startswith("//"):
                continue
            parts = line.rstrip("\n").split("\t")
            if len(parts) >= 4 and parts[1] == "warp":
                src = parts[0].split(",")
                dst = parts[3].split(",")
                try:
                    warps.append((src[0], int(src[1]), int(src[2]), dst[2], int(dst[3]), int(dst[4])))
                except (ValueError, IndexError):
                    pass
    return warps


def describe_map(ro_map):
    """(runeheir id, name, theme, kind) for a Ragnarok map, or None when it isn't a field, dungeon or town."""
    if ro_map in TOWNS:
        if ro_map == "prontera":
            return ("vigrid_haven", "Vigrid Haven", "Town", "Town")
        return (f"xr_{ro_map}", TOWNS[ro_map], "Town", "Town")
    for pattern, place, word, theme, kind in PREFIXES:
        if re.match(pattern, ro_map):
            m = re.search(r"(\d+)$", ro_map)
            n = int(m.group(1)) if m else 0
            label = f"{place} {word}"
            if m:
                label = f"{label}{n}" if word.endswith(" B") else f"{label} {ROMAN[n] if 0 < n < len(ROMAN) else n}" if n else f"{label} 0"
            if label.endswith(" 0"):
                label = f"{place} {word}" + (" Entrance" if kind == "Dungeon" else " Gate")
            if kind == "Dungeon" and theme not in ("Crypt", "IceCavern", "Volcano", "SandTomb"):
                kind = "Field"  # open-air "dungeons" (deep woods, an isle) are laid out and camped like fields
            return (f"xr_{ro_map}", label.replace("  ", " "), theme, kind)
    return None


def seed_of(text):
    return int(hashlib.md5(text.encode()).hexdigest()[:6], 16)


class WorldMap:
    def __init__(self, ro_map, desc, cells):
        self.ro_map = ro_map
        self.id, self.name, self.theme, self.kind = desc
        self.cells = cells or (300, 300)
        # Imported towns are roomier than Vigrid so gates on the palisade stay clear of the plaza's fixed buildings.
        self.size = max(100.0, min(200.0, max(self.cells) * 0.45)) if self.kind != "Town" else 160.0
        self.spawns = []      # (monster id, count)
        self.bosses = []      # (monster id, minutes)
        self.portals = []     # dicts
        self.levels = []

    def local(self, x, y):
        """Ragnarok cell -> Runeheir local metres (inside the walkable square)."""
        half = self.size / 2 - 7.0
        lx = (x / max(1, self.cells[0]) - 0.5) * self.size
        lz = (y / max(1, self.cells[1]) - 0.5) * self.size
        return (round(max(-half, min(half, lx)), 1), round(max(-half, min(half, lz)), 1))


def build_world(spawns, monsters_by_ro, npc_root, map_cache):
    sizes = read_map_sizes(map_cache)
    warps = read_warps(npc_root)
    maps = {}
    for ro_map in sorted(set(spawns) | set(TOWNS)):
        desc = describe_map(ro_map)
        if desc is None or (ro_map in TOWNS and ro_map not in sizes):
            continue
        maps[ro_map] = WorldMap(ro_map, desc, sizes.get(ro_map))

    # Spawns: Runeheir keeps fewer, tougher packs than Ragnarok's crowds.
    for ro_map, rows in spawns.items():
        wm = maps.get(ro_map)
        if wm is None:
            continue
        normal_total = sum(c for mob, c, boss, _ in rows if not boss and mob in monsters_by_ro and not monsters_by_ro[mob].boss)
        factor = min(1.0, 60.0 / max(1, normal_total)) if normal_total > 60 else 0.6
        merged = collections.OrderedDict()
        for mob, count, boss, delay in rows:
            m = monsters_by_ro.get(mob)
            if m is None:
                continue
            if m.boss:
                if m.id not in [b for b, _ in wm.bosses]:
                    wm.bosses.append((m.id, max(30.0, min(240.0, delay / 60000.0 if delay else 60.0))))
            else:
                merged[m.id] = merged.get(m.id, 0) + count
            wm.levels.append(m)
        for mid, count in merged.items():
            wm.spawns.append((mid, max(1, min(12, round(count * factor)))))

    # Portals from warps between maps we kept. Warps A->B and B->A are paired one to one, nearest first; a one-way
    # warp gets a return portal where it lands, so every portal leads somewhere that leads back.
    pairs = collections.defaultdict(list)
    for w in warps:
        if w[0] in maps and w[3] in maps and w[0] != w[3]:
            key = tuple(sorted((w[0], w[3])))
            if not any(o[0] == w[0] and abs(o[1] - w[1]) <= 4 and abs(o[2] - w[2]) <= 4 and o[3] == w[3] for o in pairs[key]):
                pairs[key].append(w)
    counters = collections.Counter()

    def new_id(src, dst):
        counters[(src, dst)] += 1
        n = counters[(src, dst)]
        return f"to_{maps[dst].id}" + (f"_{n}" if n > 1 else "")

    for (m1, m2), ws in sorted(pairs.items()):
        forward = [w for w in ws if w[0] == m1]
        backward = [w for w in ws if w[0] == m2]
        links = []  # (pos in m1, pos in m2)
        remaining = list(backward)
        for w in forward:
            if remaining:
                best = min(remaining, key=lambda b: (b[1] - w[4]) ** 2 + (b[2] - w[5]) ** 2)
                remaining.remove(best)
                links.append(((w[1], w[2]), (best[1], best[2])))
            else:
                links.append(((w[1], w[2]), (w[4], w[5])))
        for b in remaining:
            links.append(((b[4], b[5]), (b[1], b[2])))
        for (x1, y1), (x2, y2) in links:
            id1, id2 = new_id(m1, m2), new_id(m2, m1)
            p1x, p1z = maps[m1].local(x1, y1)
            p2x, p2z = maps[m2].local(x2, y2)
            maps[m1].portals.append({"Id": id1, "Label": maps[m2].name, "X": p1x, "Z": p1z, "TargetMap": maps[m2].id, "TargetPortal": id2,
                                     "Ro": (x1, y1)})
            maps[m2].portals.append({"Id": id2, "Label": maps[m1].name, "X": p2x, "Z": p2z, "TargetMap": maps[m1].id, "TargetPortal": id1,
                                     "Ro": (x2, y2)})
    return maps


def settle_spawns(maps):
    """Towns are safe: their Ragnarok spawns move to the town's nearest field. Maps left with no normal monsters (a
    lone MVP's floor) borrow the packs of their toughest neighbour."""
    by_id = {wm.id: wm for wm in maps.values()}
    for wm in maps.values():
        if wm.kind != "Town" or not (wm.spawns or wm.bosses):
            continue
        fields = [by_id[p["TargetMap"]] for p in wm.portals if p["TargetMap"] in by_id and by_id[p["TargetMap"]].kind == "Field"]
        if fields:
            host = fields[0]
            host.spawns.extend(s for s in wm.spawns if s[0] not in [h for h, _ in host.spawns])
            host.bosses.extend(b for b in wm.bosses if b[0] not in [h for h, _ in host.bosses])
            host.levels.extend(wm.levels)
        wm.spawns, wm.bosses, wm.levels = [], [], []
    for wm in maps.values():
        if wm.kind == "Town" or wm.spawns:
            continue
        neighbours = [by_id[p["TargetMap"]] for p in wm.portals if p["TargetMap"] in by_id and by_id[p["TargetMap"]].spawns]
        if neighbours:
            source = max(neighbours, key=lambda n: max((m.level for m in n.levels), default=0))
            wm.spawns = list(source.spawns)
            wm.levels.extend(m for m in source.levels if not m.boss)


def connect(maps, start="prontera"):
    """Joins every map to the town network: unreachable groups get a road from the nearest town (ships, airships)."""
    graph = collections.defaultdict(set)
    by_id = {wm.id: wm for wm in maps.values()}
    for wm in maps.values():
        for p in wm.portals:
            graph[wm.id].add(p["TargetMap"])
            graph[p["TargetMap"]].add(wm.id)
    towns = [wm for wm in maps.values() if wm.kind == "Town"]
    reached = set()
    frontier = [maps[start].id] if start in maps else []
    while frontier:
        node = frontier.pop()
        if node in reached:
            continue
        reached.add(node)
        frontier.extend(graph[node] - reached)
    added = 0
    for wm in sorted(maps.values(), key=lambda w: w.id):
        if wm.id in reached:
            continue
        group, todo = set(), [wm.id]
        while todo:
            node = todo.pop()
            if node not in group:
                group.add(node)
                todo.extend(graph[node] - group)
        # Ties go to the lowest map id: a set's order changes from run to run (Python salts its string hashes).
        entry = min((by_id[g] for g in sorted(group)),
                    key=lambda w: (w.kind != "Town", w.kind == "Dungeon", min((m.level for m in w.levels), default=0), w.id))
        level = min((m.level for m in entry.levels), default=1)
        open_towns = [t for t in towns if t.id in reached] or [maps[start]]
        town = min(open_towns, key=lambda t: (abs(level - town_level(t, maps)), t.id))
        tid, eid = f"road_{entry.id}", f"road_{town.id}"
        town.portals.append({"Id": tid, "Label": entry.name, "X": 0.0, "Z": 0.0, "TargetMap": entry.id, "TargetPortal": eid, "Road": True})
        entry.portals.append({"Id": eid, "Label": town.name, "X": 0.0, "Z": 0.0, "TargetMap": town.id, "TargetPortal": tid, "Road": True})
        reached |= group
        added += 1
    return added


def place_town_portals(wm):
    """Town gates go round the palisade in Ragnarok's direction from the town centre, at least 25 degrees apart."""
    import math
    radius = wm.size / 2 - 8
    spacing = min(25.0, 360.0 / max(1, len(wm.portals)) * 0.9)
    def free(a):
        return not any(abs((a % 360 - t + 180) % 360 - 180) < spacing for t in taken)

    taken = []
    for p in wm.portals:
        if "Ro" in p:
            angle = math.degrees(math.atan2(p["Ro"][1] - wm.cells[1] / 2, p["Ro"][0] - wm.cells[0] / 2))
        else:
            angle = 35.0 + 47.0 * len(taken)
        for step in range(120):
            candidate = angle + (step // 2 + 1) * 3.0 * (1 if step % 2 == 0 else -1) if step else angle
            if free(candidate):
                angle = candidate
                break
        taken.append(angle)
        p["X"], p["Z"] = round(math.cos(math.radians(angle)) * radius, 1), round(math.sin(math.radians(angle)) * radius, 1)


def town_level(town, maps):
    near = [m.level for wm in maps.values() for p in wm.portals if p["TargetMap"] == town.id for m in wm.levels]
    return sorted(near)[len(near) // 2] if near else 50


def level_band(wm):
    # Passive plants and mushrooms (Ragnarok AI 06) sit on every map; they don't set its level.
    fighters = [m for m in wm.levels if not m.boss and str(m.mob.get("Ai") or "") not in ("6", "06")]
    levels = sorted(st_level(m) for m in fighters) or sorted(st_level(m) for m in wm.levels if not m.boss) or [1]
    return levels[0], levels[-1]


def st_level(m):
    return max(1, min(255, round(1 + (m.level - 1) * 254 / 98)))
