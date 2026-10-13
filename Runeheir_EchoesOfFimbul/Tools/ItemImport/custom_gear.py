"""Phase 8.4: XileRetro's own gear and cards (its client-only items) for Runeheir.

XileRetro's custom weapons, armor and cards exist only in its client: their stats are written out in English in the item
description ("STR +15, VIT +10", "Max HP +7%", "Reduces damage taken from Fire property enemies by 20%"). This reads
those lines back into rAthena bonus script, so the custom pieces go through the same importer as everything else (job
lists, refines, the Norse renaming, tooltips). Lines it can't read are dropped; the description shown in Runeheir is
written from the bonuses, never copied.

Custom gear gets new names (tier + Norse piece + epithet): XileRetro's own names are often other games' and shows'.
"""

import hashlib
import re

# ---------------------------------------------------------------------------------------------- what a piece is
WEAPON_TYPES = {
    "dagger": "Dagger", "one-handed sword": "1hSword", "sword": "1hSword", "two-handed sword": "2hSword", "spear": "1hSpear",
    "one-handed spear": "1hSpear", "two-handed spear": "2hSpear", "axe": "1hAxe", "one-handed axe": "1hAxe", "two-handed axe": "2hAxe",
    "mace": "Mace", "staff": "Staff", "one-handed staff": "Staff", "two-handed staff": "2hStaff", "rod": "Staff", "bow": "Bow",
    "knuckle": "Knuckle", "claw": "Knuckle", "guitar": "Musical", "instrument": "Musical", "musical instrument": "Musical",
    "whip": "Whip", "book": "Book", "katar": "Katar", "revolver": "Revolver", "pistol": "Revolver", "rifle": "Rifle",
    "gatling gun": "Gatling", "shotgun": "Shotgun", "grenade launcher": "Grenade", "huuma": "Huuma", "huuma shuriken": "Huuma",
}
TWO_HANDED = {"2hSword", "2hSpear", "2hAxe", "2hStaff", "Bow", "Knuckle", "Musical", "Whip", "Katar", "Rifle", "Gatling", "Shotgun",
              "Grenade", "Huuma"}
ARMOR_TYPES = {"armor": "Armor", "garment": "Garment", "shoes": "Shoes", "boots": "Shoes", "footgear": "Shoes", "shield": "Left_Hand",
               "accessory": "Both_Accessory", "headgear": "Head", "head gear": "Head"}
HEAD_POSITIONS = {"upper": "Head_Top", "top": "Head_Top", "middle": "Head_Mid", "mid": "Head_Mid", "lower": "Head_Low", "low": "Head_Low"}

NOUNS = {
    "Dagger": "Seax", "1hSword": "Sword", "2hSword": "Greatsword", "1hSpear": "Spear", "2hSpear": "Halberd", "1hAxe": "Axe",
    "2hAxe": "Great Axe", "Mace": "Mace", "Staff": "Staff", "2hStaff": "Great Staff", "Bow": "Bow", "Knuckle": "Claws", "Musical": "Lyre",
    "Whip": "Lash", "Book": "Tome", "Katar": "Katar", "Revolver": "Thunder-Pistol", "Rifle": "Thunder-Rifle", "Gatling": "Thunder-Gatling",
    "Shotgun": "Thunder-Scattergun", "Grenade": "Thunder-Mortar", "Huuma": "Huuma",
    "Armor": "Mail", "Garment": "Mantle", "Shoes": "Boots", "Left_Hand": "Shield", "Head_Top": "Helm", "Head_Mid": "Visor", "Head_Low": "Mask",
}
TIERS = [(r"^\[g\]|godly", "Godly", 2000000), (r"^\[u\]|\buber\b", "Uber", 1000000), (r"elite|a-tier", "Elite", 500000),
         (r"^\[br\]|battle royal", "Arena", 300000), (r"^\[e\]|event", "Festival", 300000), (r"^\[s\]|sealed", "Sealed", 1000000)]
EPITHETS = [
    "of the Storm", "of Ymir", "of Surtr's Ember", "of the Ninth Realm", "of Fenrir", "of Jörmungandr", "of Sleipnir", "of the Einherjar",
    "of Muspel", "of Niflheim", "of the Norns", "of Mímir", "of Heimdall", "of Týr", "of Víðarr", "of Skaði", "of Ullr", "of the Wild Hunt",
    "of Bifröst", "of Yggdrasil", "of the Aurora", "of Hel's Gate", "of the Frost Giants", "of Valhalla", "of Fólkvangr", "of the Rimeheart",
    "of the Ash-Tree", "of the Raven", "of the Wolf-Age", "of Ragnarök", "of Andvari", "of the Dwarven Deep", "of Brokkr", "of Sindri",
    "of Freyr", "of Njörðr", "of the Sea-King", "of Huginn", "of Muninn", "of the Long Winter",
]

# Requirement names as XileRetro writes them -> rAthena job keys.
JOBS = {
    "novice": "Novice", "swordsman": "Swordman", "swordman": "Swordman", "mage": "Mage", "magician": "Mage", "archer": "Archer",
    "acolyte": "Acolyte", "merchant": "Merchant", "thief": "Thief", "knight": "Knight", "priest": "Priest", "wizard": "Wizard",
    "blacksmith": "Blacksmith", "hunter": "Hunter", "assassin": "Assassin", "crusader": "Crusader", "monk": "Monk", "sage": "Sage",
    "rogue": "Rogue", "alchemist": "Alchemist", "bard": "BardDancer", "dancer": "BardDancer", "gunslinger": "Gunslinger",
    "ninja": "Ninja", "taekwon": "Taekwon", "star gladiator": "StarGladiator", "soul linker": "SoulLinker",
}

# ---------------------------------------------------------------------------------------------- what it does
STATS = {"str": "bStr", "agi": "bAgi", "vit": "bVit", "int": "bInt", "dex": "bDex", "luk": "bLuk"}
FLAT = [
    (r"^(?:max\s*hp|mhp|hp)$", "bMaxHP", "bMaxHPrate"), (r"^(?:max\s*sp|msp|sp)$", "bMaxSP", "bMaxSPrate"),
    (r"^atk$|^attack$", "bBaseAtk", "bAtkRate"), (r"^matk$|^magic attack$", "bMatk", "bMatkRate"),
    (r"^hit$", "bHit", None), (r"^flee$", "bFlee", None), (r"^(?:crit|critical|critical rate|cri)$", "bCritical", None),
    (r"^perfect dodge$|^lucky dodge$", "bFlee2", None), (r"^def$", "bDef", "bDefRate"), (r"^mdef$", "bMdef", "bMdefRate"),
    (r"^aspd$", "bAspd", "bAspdRate"), (r"^(?:movement|movement speed|move speed)$", None, "bSpeedRate"),
    (r"^(?:hp recovery|hp regen)$", None, "bHPrecovRate"), (r"^(?:sp recovery|sp regen)$", None, "bSPrecovRate"),
    (r"^(?:heal|heal effectiveness|healing)$", None, "bHealPower"),
]
ELEMENTS = {"neutral": "Ele_Neutral", "water": "Ele_Water", "earth": "Ele_Earth", "fire": "Ele_Fire", "wind": "Ele_Wind", "poison": "Ele_Poison",
            "holy": "Ele_Holy", "shadow": "Ele_Dark", "dark": "Ele_Dark", "ghost": "Ele_Ghost", "undead": "Ele_Undead", "all": "Ele_All"}
RACES = {"formless": "RC_Formless", "undead": "RC_Undead", "brute": "RC_Brute", "beast": "RC_Brute", "plant": "RC_Plant", "insect": "RC_Insect",
         "fish": "RC_Fish", "demon": "RC_Demon", "demi-human": "RC_DemiHuman", "demihuman": "RC_DemiHuman", "demi human": "RC_DemiHuman",
         "human": "RC_DemiHuman", "angel": "RC_Angel", "dragon": "RC_Dragon", "all": "RC_All", "boss": "RC_Boss"}
SIZES = {"small": "Size_Small", "medium": "Size_Medium", "large": "Size_Large", "all": "Size_All"}


def _clean(line):
    return re.sub(r"\^[0-9a-fA-F]{6}", "", str(line)).strip()


def _number(text):
    m = re.search(r"([+-]?\s*\d+)", text)
    return int(m.group(1).replace(" ", "")) if m else None


def parse_bonuses(lines):
    """rAthena bonus lines for the description's effect lines; skips combos ([+ Item]) and job-only ([Job]) sections."""
    out = []
    skipping = False
    for raw in lines:
        line = _clean(raw)
        low = line.lower()
        if not line or line.startswith("___"):
            skipping = False
            continue
        if re.match(r"^\[\+", line) or (re.match(r"^\[(?:[a-z ]+)\]$", low) and not re.search(r"tier|gear|card|retro", low)) \
                or low.startswith("if ") or "is equipped" in low:
            skipping = True  # a set bonus, a job-only section or a combo condition
            continue
        if skipping or re.match(r"^\[", line) or re.match(r"^(type|attack|defense|weight|weapon level|level|position|requirement|"
                                                          r"compound on|slot|class|jobs?)\s*:", low):
            continue
        out += _parse_line(low)
    return out


def _parse_line(low):
    found = []
    # "+17 across all stats" / "All Stats +5"
    m = re.search(r"([+-]\s*\d+)\s+(?:across|to)\s+all stats", low)
    if m:
        return [f"bonus bAllStats,{int(m.group(1).replace(' ', ''))};"]
    # Resist / damage by element, race, size.
    m = re.search(r"(?:resistance|reduces? damage|damage taken).*?(?:from|against|to)\s+(?:the\s+)?(?:all\s+)?([a-z]+)\s+(?:property|element)[^%]*?(\d+)%", low) \
        or re.search(r"(\d+)%\s+resistance against (?:all )?([a-z]+)\s+property", low)
    if m:
        a, b = m.groups()
        element, value = (a, b) if not a.isdigit() else (b, a)
        if element in ELEMENTS:
            found.append(f"bonus2 bSubEle,{ELEMENTS[element]},{int(value)};")
    m = re.search(r"(?:increases?|add|inflict|deal).*?damage.*?(?:on|against|to)\s+([a-z]+)\s+(?:property|element)[^%]*?(\d+)%", low)
    if m and m.group(1) in ELEMENTS:
        found.append(f"bonus2 bAddEle,{ELEMENTS[m.group(1)]},{int(m.group(2))};")
    m = re.search(r"(?:reduces? damage|damage taken|resistance).*?(?:from|against)\s+([a-z -]+?)\s+(?:race|monsters?|enemies|targets)[^%]*?(\d+)%", low)
    if m and m.group(1).strip() in RACES:
        found.append(f"bonus2 bSubRace,{RACES[m.group(1).strip()]},{int(m.group(2))};")
    m = re.search(r"(?:increases?|add|inflict|deal).*?damage.*?(?:on|against|upon|to)\s+(?:all\s+)?([a-z -]+?)\s+(?:race|monsters?|enemies|targets)[^%]*?(\d+)%", low)
    if m:
        target = m.group(1).strip()
        if target in ("all", ""):
            found.append(f"bonus2 bAddRace,RC_All,{int(m.group(2))};")
        elif target in RACES:
            found.append(f"bonus2 bAddRace,{RACES[target]},{int(m.group(2))};")
        elif target in SIZES:
            found.append(f"bonus2 bAddSize,{SIZES[target]},{int(m.group(2))};")
    m = re.search(r"(?:reduces? damage|damage taken).*?long[- ]range[^%]*?(\d+)%", low)
    if m:
        found.append(f"bonus bLongAtkDef,{int(m.group(1))};")
    m = re.search(r"long[- ]range (?:physical )?(?:attack|damage)[^%]*?\+?\s*(\d+)%", low)
    if m and "reduc" not in low and "taken" not in low:
        found.append(f"bonus bLongAtkRate,{int(m.group(1))};")
    m = re.search(r"crit(?:ical)?(?: rate)?\s*\+\s*(\d+)\s+(?:on|against)\s+([a-z -]+?)\s+(?:enemies|monsters|race|targets)", low)
    if m and m.group(2).strip() in RACES:
        found.append(f"bonus2 bCriticalAddRace,{RACES[m.group(2).strip()]},{int(m.group(1))};")
    m = re.search(r"(?:reduces?|decreases?)\s+(?:variable\s+)?cast(?:ing)? time by (\d+)%|cast(?:ing)? time\s*-\s*(\d+)%", low)
    if m:
        found.append(f"bonus bVariableCastrate,-{int(m.group(1) or m.group(2))};")
    m = re.search(r"(?:sp consumption|sp cost)[^%]*?-\s*(\d+)%|reduces? sp (?:consumption|cost) by (\d+)%", low)
    if m:
        found.append(f"bonus bUseSPrate,-{int(m.group(1) or m.group(2))};")
    if re.search(r"indestructible|unbreakable", low):
        found.append("bonus bUnbreakableWeapon;")
    if found:
        return found

    # "STR +15, VIT +10, LUK +5" / "DEX,VIT, LUK +5" / "Max HP +7%, MATK +5%"
    for chunk in re.split(r",|;|\band\b", low):
        chunk = chunk.strip().rstrip("!.")
        m = re.match(r"^all\s*stats?\s*([+-]\s*\d+)$", chunk)
        if m:
            found.append(f"bonus bAllStats,{int(m.group(1).replace(' ', ''))};")
            continue
        m = re.match(r"^([a-z][a-z ,]*?)\s*([+-]\s*\d+)\s*(%)?$", chunk)
        if not m:
            continue
        names, value, percent = m.group(1).strip(), int(m.group(2).replace(" ", "")), bool(m.group(3))
        for name in [n.strip() for n in names.split(",") if n.strip()]:
            if name in STATS and not percent:
                found.append(f"bonus {STATS[name]},{value};")
                continue
            for pattern, flat, rate in FLAT:
                if re.match(pattern, name):
                    bonus = rate if percent else flat
                    if bonus:
                        found.append(f"bonus {bonus},{value};")
                    break
    # "DEX,VIT, LUK +5": names listed with commas before one value.
    m = re.match(r"^((?:str|agi|vit|int|dex|luk)(?:\s*,\s*(?:str|agi|vit|int|dex|luk))+)\s*([+-]\s*\d+)$", low)
    if m:
        value = int(m.group(2).replace(" ", ""))
        found = [f"bonus {STATS[n.strip()]},{value};" for n in m.group(1).split(",")]
    return found


# ---------------------------------------------------------------------------------------------- the pieces
def _fields(lines):
    out = {}
    for raw in lines:
        line = _clean(raw)
        m = re.match(r"^([A-Za-z ]+?)\s*:\s*(.+)$", line)
        if m:
            out.setdefault(m.group(1).strip().lower(), m.group(2).strip())
    return out


def _requirement(lines):
    cleaned = [_clean(l) for l in lines]
    for i, line in enumerate(cleaned):
        if re.match(r"^requirement\s*:", line, re.I):
            rest = line.split(":", 1)[1].strip() or (cleaned[i + 1] if i + 1 < len(cleaned) else "")
            return rest
    return ""


def tier_of(name, lines):
    text = (name + " " + " ".join(_clean(l) for l in lines[:2])).lower()
    for pattern, tier, price in TIERS:
        if re.search(pattern, text):
            return tier, price
    return "Hirð", 100000


def piece(ro_id, info, taken):
    """A pseudo rAthena item_db entry for a client-only weapon or armor piece, or None when it isn't one."""
    lines = info.get("description") or []
    fields = _fields(lines)
    kind = (fields.get("type") or "").lower()
    name = re.sub(r"^\[[^\]]+\]\s*", "", str(info.get("name") or ""))
    entry = {"Id": ro_id, "AegisName": f"XR_CUSTOM_{ro_id}", "Slots": int(info.get("slots") or 0), "Refineable": True}
    weight = _number(fields.get("weight", "") or "") or 0
    entry["Weight"] = max(0, weight) * 10
    level = _number(fields.get("level", "") or fields.get("required level", "") or "")
    if level:
        entry["EquipLevelMin"] = max(1, min(255, level))
    if kind in WEAPON_TYPES:
        sub = WEAPON_TYPES[kind]
        entry.update({"Type": "Weapon", "SubType": sub, "Attack": _number(fields.get("attack", "") or "") or 0,
                      "WeaponLevel": max(1, min(4, _number(fields.get("weapon level", "") or "") or 3)),
                      "Locations": {"Both_Hand": True} if sub in TWO_HANDED else {"Right_Hand": True}})
        noun = NOUNS[sub]
    elif kind in ARMOR_TYPES:
        location = ARMOR_TYPES[kind]
        if location == "Head":
            position = (fields.get("position") or "upper").lower()
            locs = [HEAD_POSITIONS[w] for w in re.split(r"[^a-z]+", position) if w in HEAD_POSITIONS] or ["Head_Top"]
            entry["Locations"] = {loc: True for loc in dict.fromkeys(locs)}
            noun = NOUNS[locs[0]]
        else:
            entry["Locations"] = {location: True}
            noun = NOUNS.get(location)
            if location == "Both_Accessory":
                low = name.lower()
                noun = "Ring" if "ring" in low else "Amulet" if re.search(r"necklace|pendant|amulet|choker", low) else \
                    "Earring" if "earring" in low else "Brooch" if "brooch" in low else "Charm"
        entry.update({"Type": "Armor", "Defense": _number(fields.get("defense", "") or "") or 0})
    else:
        return None
    tier, price = tier_of(str(info.get("name") or ""), lines)
    epithet = EPITHETS[int(hashlib.md5(str(ro_id).encode()).hexdigest(), 16) % len(EPITHETS)]
    display = f"{tier} {noun} {epithet}"
    n = 2
    while display in taken:
        display = f"{tier} {noun} {epithet} {['', '', 'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII'][n] if n < 9 else n}"
        n += 1
    taken.add(display)
    entry["Name"] = display
    entry["Buy"] = price
    jobs = _requirement(lines).lower()
    if jobs and "all" not in jobs and "none" not in jobs:
        keys = {JOBS[j] for j in JOBS if re.search(r"\b" + j + r"\b", jobs)}
        if keys:
            entry["Jobs"] = {k: True for k in sorted(keys)}
    entry["Script"] = "\n".join(parse_bonuses(lines))
    return entry


CARD_ADJECTIVES = ["Wandering", "Fallen", "Forsworn", "Bound", "Grim", "Pale", "Ashen", "Howling", "Silent", "Hollow", "Rimebound", "Sunken"]
CARD_NOUNS = ["Einherjar", "Draugr", "Huldra", "Mara", "Vættr", "Jötunn", "Seiðkona", "Berserker", "Skald", "Valkyrie", "Nisse", "Fylgja"]


def collect(client, server, spawned_names, creature_name, is_costume):
    """
    Pseudo item_db entries for XileRetro's client-only gear, cards and pet eggs (costumes are costumes.py's), keyed by id.
    spawned_names: Ragnarok monster name (lowercase) -> AegisName, for the monsters Runeheir imports (eggs need theirs).
    """
    out = {}
    taken = set()
    card_names = set()
    for ro_id in sorted(client):
        if ro_id in server or is_costume(client[ro_id]):
            continue
        info = client[ro_id]
        kind = (_fields(info.get("description") or []).get("type") or "").lower()
        if kind == "monster egg":
            base = re.sub(r"\s*\((?:m|f)\)\s*$", "", re.sub(r"^\[[^\]]+\]\s*", "", str(info.get("name") or "")), flags=re.I)
            base = re.sub(r"\s+egg$", "", base.strip(), flags=re.I)
            mob = spawned_names.get(base.lower())
            if mob:
                out[ro_id] = {"Id": ro_id, "AegisName": f"XR_CUSTOM_{ro_id}", "Name": f"{creature_name(base)} Egg", "Type": "Petegg",
                              "Weight": 0, "Buy": 20, "CustomPetMob": mob}
            continue
        entry = piece(ro_id, info, taken)
        if entry is None and kind == "card":
            entry = card(ro_id, info)
            if entry:
                raw = entry["Name"]
                suffix = re.search(r"\s*(\(\+\d+\))\s*$", raw)
                base = re.sub(r"\s*\(\+\d+\)\s*$", "", raw)
                base = re.sub(r"\s+card$", "", base, flags=re.I).strip()
                entry["CustomCardOf"] = base.lower()
                norse = creature_name(base)
                if norse == base and base.lower() not in spawned_names:
                    h = int(hashlib.md5(raw.encode()).hexdigest(), 16)
                    norse = f"{CARD_ADJECTIVES[h % len(CARD_ADJECTIVES)]} {CARD_NOUNS[(h // 13) % len(CARD_NOUNS)]}"
                name = f"{norse} Card" + (f" {suffix.group(1)}" if suffix else "")
                n = 2
                while name in card_names:
                    name = f"{norse} Card {['', '', 'II', 'III', 'IV', 'V', 'VI'][n] if n < 7 else n}" + (f" {suffix.group(1)}" if suffix else "")
                    n += 1
                card_names.add(name)
                entry["Name"] = name
                entry["CustomCardName"] = name
        if entry:
            out[ro_id] = entry
    return out


# XileRetro's mystery boxes -> (pattern on name + description, Runeheir chest, what it holds, who drops it, rate per 10,000)
BOXES = [
    (r"godly loot box|godly s-tier", "Godly Hoard Chest", "godly", "mvp", 100),
    (r"elite loot box|elite a-tier", "Elite Hoard Chest", "elite", "mvp", 200),
    (r"uber loot box|uber b-tier|uber accessory box", "Uber Hoard Chest", "uber", "mini", 100),
    (r"costume", "Tailor's Mystery Box", "costume", "boss", 200),
    (r"event card|card grab", "Seiðr Card Pouch", "card", "mini", 50),
    (r"seal box", "Sealed Rune Box", "sealed", "mvp", 50),
]


def boxes(client, server, custom, costume_ids, is_costume):
    """
    Pseudo usable entries for the XileRetro mystery boxes Runeheir can fill: one chest per kind (the other boxes of that
    kind alias to it), each holding a random piece of its kind. Returns (entries, aliases).
    """
    gear = {"godly": [], "elite": [], "uber": [], "sealed": [], "card": []}
    for ro_id, entry in custom.items():
        first = entry["Name"].split()[0].lower()
        if entry["Type"] in ("Weapon", "Armor") and first in gear:
            gear[first].append(f"xr{ro_id}")
        elif entry["Type"] == "Card":
            gear["sealed" if "(+" in entry["Name"] else "card"].append(f"xr{ro_id}")
    holds = dict(gear, costume=list(costume_ids))
    out, aliases, made = {}, {}, {}
    for ro_id in sorted(client):
        info = client[ro_id]
        if ro_id in server or ro_id in custom or is_costume(info):
            continue
        name = str(info.get("name") or "")
        text = (name + " " + " ".join(_clean(l) for l in (info.get("description") or [])[:3])).lower()
        if not re.search(r"box|bag|chest|crate", name.lower()):
            continue
        for pattern, chest, kind, dropper, rate in BOXES:
            if re.search(pattern, text) and holds.get(kind):
                if chest in made:
                    aliases[ro_id] = f"xr{made[chest]}"
                else:
                    made[chest] = ro_id
                    out[ro_id] = {"Id": ro_id, "AegisName": f"XR_CUSTOM_{ro_id}", "Name": chest, "Type": "Usable", "Weight": 10,
                                  "Buy": 0, "CustomGives": holds[kind], "CustomDrop": (dropper, rate)}
                break
    return out, aliases


def assign_drops(monsters, custom):
    """
    Where XileRetro's custom pieces come from in Runeheir: eggs and cards from their own monster when it's in the world,
    the strongest gear and the rest of the cards from MVPs near the piece's level, the rest of the gear from mini-bosses.
    Adds Ragnarok-style drop rows (rate per 10,000) to the monsters' mob entries before they're built.
    """
    by_name = {m.ro_name.lower(): m for m in monsters}
    mvps = sorted((m for m in monsters if m.mvp), key=lambda m: m.level)
    minis = sorted((m for m in monsters if m.boss and not m.mvp), key=lambda m: m.level)

    def near(pool, level, key):
        if not pool:
            return None
        ranked = sorted(pool, key=lambda m: (abs(m.level - level), m.ro_id))
        return ranked[int(hashlib.md5(str(key).encode()).hexdigest(), 16) % min(4, len(ranked))]

    placed = 0
    for ro_id, entry in sorted(custom.items()):
        aegis = entry["AegisName"]
        if entry.get("CustomDrop"):
            dropper, rate = entry["CustomDrop"]
            for m in monsters:
                if (dropper == "mvp" and m.mvp) or (dropper == "mini" and m.boss and not m.mvp) or (dropper == "boss" and m.boss):
                    m.mob.setdefault("MvpDrops" if dropper == "mvp" else "Drops", []).append({"Item": aegis, "Rate": rate})
            placed += 1
            continue
        if entry["Type"] == "Petegg":
            owner = next((m for m in monsters if str(m.mob.get("AegisName")) == entry["CustomPetMob"]), None)
            if owner:
                owner.mob.setdefault("Drops", []).append({"Item": aegis, "Rate": 10})
                placed += 1
            continue
        if entry["Type"] == "Card":
            owner = by_name.get(entry.get("CustomCardOf", ""))
            if owner:
                owner.mob.setdefault("Drops", []).append({"Item": aegis, "Rate": 1})
            else:
                owner = near(mvps, 60, ro_id)
                if owner:
                    owner.mob.setdefault("MvpDrops", []).append({"Item": aegis, "Rate": 50})
            placed += bool(owner)
            continue
        level = int(entry.get("EquipLevelMin") or 60)
        ro_level = max(1, round(1 + (min(255, level) - 1) * 98 / 254))
        strong = entry["Name"].split()[0] in ("Godly", "Uber", "Elite", "Sealed")
        owner = near(mvps if strong else minis or mvps, ro_level, ro_id)
        if owner:
            if strong:
                owner.mob.setdefault("MvpDrops", []).append({"Item": aegis, "Rate": 100})
            else:
                owner.mob.setdefault("Drops", []).append({"Item": aegis, "Rate": 50})
            placed += 1
    return placed


def card(ro_id, info):
    """A pseudo item_db entry for a client-only card: its target and bonuses; None when it has no readable bonus."""
    lines = info.get("description") or []
    fields = _fields(lines)
    target = (fields.get("compound on") or "").lower()
    locations = {"weapon": "Right_Hand", "armor": "Armor", "shield": "Left_Hand", "garment": "Garment", "shoes": "Shoes",
                 "footgear": "Shoes", "accessory": "Both_Accessory", "headgear": "Head_Top"}
    location = next((v for k, v in locations.items() if k in target), None)
    script = "\n".join(parse_bonuses(lines))
    if not location or not script:
        return None
    name = re.sub(r"^\[[^\]]+\]\s*", "", str(info.get("name") or ""))
    return {"Id": ro_id, "AegisName": f"XR_CUSTOM_{ro_id}", "Name": name, "Type": "Card", "Locations": {location: True},
            "Script": script, "Weight": 10, "Buy": 20}
