"""Phase 8: imports the XileRetro item database into Runeheir, under Norse names.

Reads XileRetro's client item table (which items exist) and rAthena's pre-renewal database (their stats and scripts),
and writes Assets/_Runeheir/Scripts/Core/Items/Generated/ItemCatalog.Xile.*.g.cs plus a coverage report. Names go
through names_norse.py, descriptions are written fresh from the translated effects (script_translate.py); Ragnarok's
own description text is never copied.

    "C:/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe" -I Tools/ItemImport/import_items.py

Re-run it after changing the rename tables or the translator; the generated files are committed.
"""

import collections
import glob
import hashlib
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bestiary  # noqa: E402
import world  # noqa: E402
import costumes  # noqa: E402
import custom_gear  # noqa: E402
import goods  # noqa: E402
import mercenaries  # noqa: E402
import pets  # noqa: E402
import creatures_norse  # noqa: E402
import names_norse  # noqa: E402
import ro_parse  # noqa: E402
import script_translate as st  # noqa: E402

PROJECT = os.path.normpath(os.path.join(HERE, "..", ".."))
ITEMS_DIR = os.path.join(PROJECT, "Assets", "_Runeheir", "Scripts", "Core", "Items")
OUT_DIR = os.path.join(ITEMS_DIR, "Generated")
REPORT = os.path.join(HERE, "report_equipment.md")
PER_METHOD = 120

WEAPON_TYPES = {
    "Dagger": "Dagger", "1hSword": "OneHandSword", "2hSword": "TwoHandSword", "1hSpear": "Spear", "2hSpear": "Spear",
    "1hAxe": "Axe", "2hAxe": "TwoHandAxe", "Mace": "Mace", "2hMace": "Mace", "Staff": "Staff", "2hStaff": "Staff",
    "Bow": "Bow", "Knuckle": "Knuckle", "Musical": "Instrument", "Whip": "Whip", "Book": "Book", "Katar": "Katar",
    "Revolver": "ThunderRod", "Rifle": "ThunderRod", "Gatling": "ThunderRod", "Shotgun": "ThunderRod", "Grenade": "ThunderRod",
    "Huuma": "Huuma",
}
LOCATIONS = {
    "Head_Top": "HeadUpper", "Head_Mid": "HeadMid", "Head_Low": "HeadLower", "Armor": "Armor", "Right_Hand": "Weapon",
    "Left_Hand": "Shield", "Both_Hand": "TwoHanded", "Garment": "Garment", "Shoes": "Footgear", "Right_Accessory": "Accessory",
    "Left_Accessory": "Accessory", "Both_Accessory": "Accessory",
}
RO_CLASSES = {
    "Novice": "Novice", "Swordman": "Swordman", "Mage": "Mage", "Archer": "Archer", "Acolyte": "Acolyte", "Merchant": "Merchant",
    "Thief": "Thief", "Knight": "Knight", "Priest": "Priest", "Wizard": "Wizard", "Blacksmith": "Blacksmith", "Hunter": "Hunter",
    "Assassin": "Assassin", "Crusader": "Crusader", "Monk": "Monk", "Sage": "Sage", "Rogue": "Rogue", "Alchemist": "Alchemist",
    "BardDancer": "BardDancer", "Taekwon": "Taekwon", "StarGladiator": "StarGladiator", "SoulLinker": "SoulLinker",
    "Gunslinger": "Gunslinger", "Rebellion": "Gunslinger", "Ninja": "Ninja", "KagerouOboro": "Ninja", "SuperNovice": "SuperNovice",
    "Summoner": "Summoner",
}
ALL_CLASSES = list(dict.fromkeys(RO_CLASSES.values()))

# Imported items whose Norse name is one of Runeheir's own: Runeheir's version stands in (the RO id becomes an alias).
STAND_INS = {"Valkyrian Shoes": "valkyrian_boots", "Valkyrja's Shield": "valkyrian_shield"}
ICONIC = {"Valkyrian Armor", "Valkyrian Manteau", "Megingjard", "Brisingamen"}

ELEMENT_COLORS = {"Fire": "#E74C3C", "Water": "#5DADE2", "Earth": "#A0522D", "Wind": "#48C9B0", "Poison": "#7D3C98",
                  "Holy": "#F7DC6F", "Shadow": "#4A235A", "Ghost": "#AEB6BF", "Undead": "#566573"}
NAME_COLORS = {"Red": "#C0392B", "Blue": "#2E86C1", "Green": "#28B463", "Yellow": "#F4D03F", "Black": "#1C2833", "White": "#F2F3F4",
               "Pink": "#F1948A", "Purple": "#7D3C98", "Gold": "#D4AC0D", "Golden": "#D4AC0D", "Silver": "#BDC3C7", "Brown": "#7E5109",
               "Gray": "#7F8C8D", "Orange": "#E67E22", "Dark": "#283747", "Crimson": "#922B21", "Scarlet": "#CB4335", "Violet": "#8E44AD"}
GEAR_COLORS = {"Armor": "#95A5A6", "Shield": "#A04000", "Garment": "#784212", "Footgear": "#6E2C00", "Accessory": "#BDC3C7",
               "Headgear": "#A9927D"}
VIEW_PALETTE = ["#C0392B", "#2E86C1", "#28B463", "#D4AC0D", "#7D3C98", "#E67E22", "#F2F3F4", "#1C2833", "#A04000", "#48C9B0"]

WEAPON_FLAVOR = {
    "Dagger": ["A short blade for close work.", "Seax-length and quick in the hand.", "Light enough to hide in a boot.",
               "A keen little blade that finds the gaps in mail."],
    "OneHandSword": ["A one-handed sword, balanced for shield work.", "A straight blade of good dwarven steel.",
                     "A sword fit for a jarl's hirdman.", "Pattern-welded steel with a runed fuller."],
    "TwoHandSword": ["A greatsword that wants both hands and a strong back.", "A long blade for wide, sweeping cuts.",
                     "Heavy steel that cleaves shield and bone alike."],
    "Spear": ["An ash-shafted spear with an iron head.", "Long reach and a hungry point.", "A spear in the fashion of Odin's own."],
    "Axe": ["A bearded axe for one hand.", "A woodsman's axe turned to war.", "A light axe that bites deep."],
    "TwoHandAxe": ["A great Dane axe swung with both hands.", "A heavy axe that splits helms.", "Iron enough to fell a jotun's oak."],
    "Mace": ["A flanged mace for crushing mail.", "Heavy iron on a stout haft.", "Made to ring helmets like temple bells."],
    "Staff": ["A staff carved with galdr runes.", "Rune-cut wood that hums with seiðr.", "A staff that steadies a caster's mind."],
    "Bow": ["A bow of yew and horn.", "A hunter's bow, strung tight against the cold.", "A bow for long shots across the snow."],
    "Knuckle": ["Iron for the fists.", "Weighted knuckles for brawlers.", "Wrapped iron that turns a punch into a hammer."],
    "Katar": ["A punch-blade gripped in the fist.", "A katar with a cruel, narrow point."],
    "Instrument": ["An instrument for a skald's songs.", "Its strings carry a saga across the mead-hall."],
    "Whip": ["A lash for a seiðkona's dance.", "A whip that cracks like river ice."],
    "Book": ["A tome bound in sealskin and runes.", "Pages of galdr in a careful hand."],
    "ThunderRod": ["A thunder-rod that throws lightning-driven shot.", "Dwarven brass and storm-glass."],
    "Huuma": ["A great shuriken for a nightraider.", "Broad blades on a single iron ring."],
}
GEAR_FLAVOR = {
    "Armor": ["Body armor for the long winter.", "Well-stitched and well-worn.", "Armor that has seen a few raids."],
    "Shield": ["A shield to stand in the wall.", "Linden boards bound with iron.", "It has turned aside many blows."],
    "Garment": ["A cloak against the north wind.", "Thick cloth over the shoulders.", "A mantle for cold roads."],
    "Footgear": ["Footwear for snow and stone.", "Sturdy soles for long marches.", "Laced tight for the road."],
    "Headgear": ["Worn on the head.", "A fine thing to wear to the Thing.", "It keeps the snow off, mostly."],
    "Accessory": ["A small charm worn close.", "A trinket with a story behind it.", "Worn for luck as much as anything."],
}


# ---------------------------------------------------------------------------------------------- helpers
def stable_pick(options, key):
    digest = hashlib.md5(str(key).encode()).digest()
    return options[digest[0] % len(options)]


def cs(s):
    return st.cs_string(s)


def slots_of(locations):
    flags = []
    for loc in locations:
        flag = LOCATIONS.get(loc)
        if flag and flag not in flags:
            flags.append(flag)
    return flags


def gear_kind(flags):
    if "Weapon" in flags or "TwoHanded" in flags:
        return "Weapon"
    if "Shield" in flags:
        return "Shield"
    if "Armor" in flags:
        return "Armor"
    if "Garment" in flags:
        return "Garment"
    if "Footgear" in flags:
        return "Footgear"
    if any(f.startswith("Head") for f in flags):
        return "Headgear"
    return "Accessory"


def class_mask(entry):
    jobs = entry.get("Jobs")
    allowed = set(ALL_CLASSES) if not jobs or jobs.get("All") else set()
    for name, on in (jobs or {}).items():
        if name == "All":
            continue
        mapped = RO_CLASSES.get(name)
        if not mapped:
            continue
        if on:
            allowed.add(mapped)
        else:
            allowed.discard(mapped)
    return allowed


def jobs_expr(allowed, tier):
    """C# for ItemCatalog.JobsFor(...), or None when every job may use it."""
    if set(ALL_CLASSES) <= allowed and tier == "All":
        return None
    missing = [c for c in ALL_CLASSES if c not in allowed]
    present = [c for c in ALL_CLASSES if c in allowed]
    if len(missing) < len(present):
        mask = "RoClass.All" + (" & ~(" + " | ".join(f"RoClass.{c}" for c in missing) + ")" if missing else "")
    else:
        mask = " | ".join(f"RoClass.{c}" for c in present) or "RoClass.None"
    return f"JobsFor({mask}{'' if tier == 'All' else f', RoTier.{tier}'})"


def tier_of(entry):
    classes = entry.get("Classes")
    if not classes or classes.get("All"):
        return "All", False, False
    keys = {k for k, v in classes.items() if v}
    normal = "Normal" in keys or "Baby" in keys
    upper = "Upper" in keys
    third = bool(keys & {"Third", "Third_Upper", "Third_Baby", "Fourth"})
    if upper and not normal:
        return "Upper", True, False
    if normal and not upper:
        return "Normal", False, False
    if third and not normal and not upper:
        return "All", False, True
    return "All", False, False


def own_items():
    """Names of Runeheir's hand-written items -> ids."""
    own = {}
    for path in glob.glob(os.path.join(ITEMS_DIR, "ItemCatalog*.cs")):
        text = open(path, encoding="utf-8").read()
        for m in re.finditer(r'\b(?:Weapon|Wear|Head|Material|Loot|Card)\(\s*"?([A-Za-z_]+)"?\s*,\s*"([^"]+)"', text):
            own[m.group(2)] = m.group(1)
        for m in re.finditer(r'Id = "?([A-Za-z_]+)"?, Name = "([^"]+)"', text):
            own[m.group(2)] = m.group(1)
    # constants: Weapon(RustySeax, "Rusty Seax") -> resolve the const to its value
    consts = {}
    for path in glob.glob(os.path.join(ITEMS_DIR, "ItemCatalog*.cs")):
        for m in re.finditer(r'public const string (\w+) = "([a-z_]+)"', open(path, encoding="utf-8").read()):
            consts[m.group(1)] = m.group(2)
    return {name: consts.get(i, i) for name, i in own.items()}


def load_runeheir_skill_names():
    skills_dir = os.path.join(PROJECT, "Assets", "_Runeheir", "Scripts", "Core", "Skills")
    for path in glob.glob(os.path.join(skills_dir, "SkillCatalog*.cs")):
        text = open(path, encoding="utf-8").read()
        for m in re.finditer(r'"([a-z_]+)",\s*"([^"]+)"', text):
            st.RUNEHEIR_SKILL_NAMES.setdefault(m.group(1), m.group(2))
        for m in re.finditer(r'Id = "([a-z_]+)", Name = "([^"]+)"', text):
            st.RUNEHEIR_SKILL_NAMES[m.group(1)] = m.group(2)


def load_skill_names():
    load_runeheir_skill_names()
    path = os.path.join(ro_parse.SERVER_DB, "skill_db.yml")
    if not os.path.exists(path):
        return
    for entry in ro_parse.read_yaml(path):
        if entry.get("Name") and entry.get("Description"):
            st.SKILL_NAMES[str(entry["Name"])] = str(entry["Description"])


# ---------------------------------------------------------------------------------------------- items
class Item:
    def __init__(self, ro_id, entry, info):
        self.ro_id = ro_id
        self.entry = entry
        self.info = info
        self.ro_name = entry["Name"]
        self.name = names_norse.norse_name(entry["Name"])
        self.flags = slots_of((entry.get("Locations") or {}).keys())
        self.kind = gear_kind(self.flags)
        self.sockets = int(entry.get("Slots") or 0)
        self.refinable = bool(entry.get("Refineable"))
        self.result = st.translate(entry.get("Script"), self.refinable)
        self.alias = None

    @property
    def id(self):
        return f"xr{self.ro_id}"


def build(item):
    e = item.entry
    r = item.result
    base = r["base"]
    weapon = item.kind == "Weapon"
    fields = [f'Id = "{item.id}", RoId = {item.ro_id}, Name = {cs(item.name)}, Kind = ItemKind.Equipment']

    slots = " | ".join(f"EquipSlot.{f}" for f in item.flags)
    element = None
    if weapon:
        wtype = WEAPON_TYPES.get(e.get("SubType"), "Dagger")
        if base.weapon_element:
            element = base.weapon_element
            base.weapon_element = None
            base.desc = [d for d in base.desc if not d.endswith("element attacks")]
        fields.append(f"Slots = {slots}, WeaponType = WeaponType.{wtype}, WeaponLevel = {int(e.get('WeaponLevel') or 1)}, "
                      f"Atk = {int(e.get('Attack') or 0)}")
        icon = f"WeaponIcon(WeaponType.{wtype})"
        color = ELEMENT_COLORS.get(element, "#B2BABB")
        flavor = stable_pick(WEAPON_FLAVOR[wtype], item.ro_id)
    else:
        mdef = int(base.mods.pop("Mdef", 0))
        base.desc = [d for d in base.desc if not d.startswith("MDEF ")] if mdef else base.desc
        if "Armor" in item.flags and base.armor_element:
            element = base.armor_element
            base.armor_element = None
        defense = int(e.get("Defense") or 0) * 10
        fields.append(f"Slots = {slots}, Def = {defense}" + (f", Mdef = {mdef}" if mdef else ""))
        icon = f"GearIcon({slots})"
        words = re.findall(r"[A-Za-z]+", item.name)
        color = next((NAME_COLORS[w] for w in words if w in NAME_COLORS), None) or GEAR_COLORS[item.kind]
        flavor = stable_pick(GEAR_FLAVOR[item.kind], item.ro_id)
    if element:
        fields.append(f"Element = Element.{element}")

    weight = int(round((e.get("Weight") or 0) / 10))
    price = int(e.get("Buy") or (e.get("Sell") or 0) * 2)
    fields.append(f"Sockets = {item.sockets}, EquipLevel = {max(1, int(e.get('EquipLevelMin') or 1))}, Weight = {weight}, Price = {price}")
    if not item.refinable:
        fields.append("Refinable = false")
    fields.append(f"IconLabel = {icon}, IconColorHex = \"{color}\"")
    if item.kind == "Headgear":
        view = next((NAME_COLORS[w] for w in re.findall(r"[A-Za-z]+", item.name) if w in NAME_COLORS), None)
        fields.append(f"ViewColorHex = \"{view or stable_pick(VIEW_PALETTE, item.ro_id)}\"")

    tier, reborn, third = tier_of(e)
    jobs = jobs_expr(class_mask(e), tier)
    if jobs:
        fields.append(f"AllowedJobs = {jobs}")
    if reborn:
        fields.append("RebornOnly = true")
    if third:
        fields.append("MinTier = 3")
    gender = e.get("Gender")
    if gender in ("Female", "Male") and not weapon:
        fields.append(f"RequiredGender = Gender.{gender}")

    rules = st.describe(r)
    description = f"{flavor} {rules}".strip()
    fields.insert(1, f"Description = {cs(description)}")

    namer = st.BuffNamer(item.ro_id, item.name)
    effect = st.cs_effect(base, buffs=namer)
    if effect:
        fields.append(f"Effect = {effect}")
    per = st.cs_effect(r["per_refine"], buffs=namer)
    if per:
        fields.append(f"PerRefine = {per}, PerRefineLimit = {st.MAX_REFINE}")
    if r["thresholds"]:
        steps = ", ".join(f"AtRefine({level}, {st.cs_effect(fx, buffs=namer)})" for level, fx in r["thresholds"])
        fields.append(f"RefineBonuses = new[] {{ {steps} }}")
    return fields


def emit_register(fields):
    lines = ["            AddXile(new ItemDefinition", "            {"]
    lines += [f"                {f}," for f in fields]
    lines += ["            });"]
    return "\n".join(lines)


HEADER = """// <auto-generated>
// Generated by Tools/ItemImport/import_items.py from the XileRetro item list and rAthena's pre-renewal item database,
// renamed under Norse myth. Don't edit by hand: change the importer's tables and re-run it.
// </auto-generated>
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Items
{
    public static partial class ItemCatalog
    {
"""


def write_group(name, items, builder=None):
    builder = builder or build
    methods = []
    calls = []
    built = [(item, builder(item)) for item in items]
    built = [(item, fields) for item, fields in built if fields]
    items = [item for item, _ in built]
    for n in range(0, len(built), PER_METHOD):
        chunk = built[n:n + PER_METHOD]
        method = f"RegisterXile{name}{n // PER_METHOD}"
        calls.append(f"            {method}();")
        body = "\n\n".join(emit_register(fields) for _, fields in chunk)
        methods.append(f"        private static void {method}()\n        {{\n{body}\n        }}\n")
    text = HEADER
    text += f"        /// <summary>{len(items)} imported {name.lower()} items.</summary>\n"
    text += f"        private static void RegisterXile{name}()\n        {{\n" + "\n".join(calls) + "\n        }\n\n"
    text += "\n".join(methods)
    text += "    }\n}\n"
    path = os.path.join(OUT_DIR, f"ItemCatalog.Xile.{name}.g.cs")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    return items


SHOP_FILES = [os.path.join(os.path.dirname(ro_parse.SERVER_DB), "..", "npc", "merchants", "shops.txt"),
              os.path.join(os.path.dirname(ro_parse.SERVER_DB), "..", "npc", "pre-re", "merchants", "shops.txt")]


def dealer_stock():
    """Ragnarok's town weapon and armor dealers' stock, in first-seen order: {"Weapon": [ro ids], "Armor": [ro ids]}."""
    stock = {"Weapon": [], "Armor": []}
    for path in SHOP_FILES:
        if not os.path.exists(path):
            continue
        for line in open(path, encoding="utf-8", errors="replace"):
            parts = line.rstrip("\n").split("\t")
            if len(parts) < 4 or parts[1] != "shop" or line.startswith("//"):
                continue
            kind = "Weapon" if "Weapon Dealer" in parts[2] else "Armor" if "Armor Dealer" in parts[2] else None
            if not kind:
                continue
            for pair in parts[3].split(",")[1:]:
                ro_id = int(pair.split(":")[0])
                if ro_id not in stock[kind]:
                    stock[kind].append(ro_id)
    return stock


def write_index(groups, aliases, stock):
    text = HEADER
    summaries = {
        "Weapon": "Ragnarok's town weapon dealers' stock, sold by Vigrid Haven's weapon smith.",
        "Armor": "Ragnarok's town armor dealers' stock, sold by Vigrid Haven's armourer.",
        "Mercenary": "The Hird's contract scrolls (bowmen, spearmen, huscarls; grades 1-10), sold by Vigrid Haven's Hird Hall.",
        "CostumeHead": "XileRetro's costumes worn on the head (hats, ears, lenses, masks, wigs), sold by Vigrid Haven's tailor.",
        "CostumeBack": "XileRetro's costumes worn on the back or at the feet (wings, cloaks, tails, auras), sold by Vigrid Haven's tailor.",
    }
    for kind, ids in stock.items():
        text += f"        /// <summary>{summaries.get(kind, kind)}</summary>\n"
        text += f"        public static readonly string[] Xile{kind}DealerStock =\n        {{\n"
        for n in range(0, len(ids), 8):
            text += "            " + " ".join(f'"{i}",' for i in ids[n:n + 8]) + "\n"
        text += "        };\n\n"
    text += "        /// <summary>Registers the whole imported XileRetro database (called last by the static constructor).</summary>\n"
    text += "        private static void RegisterXileDatabase()\n        {\n"
    text += "".join(f"            RegisterXile{name}();\n" for name in groups)
    for ro_id, target in sorted(aliases.items()):
        text += f"            AliasXile({ro_id}, \"{target}\");\n"
    text += "        }\n    }\n}\n"
    with open(os.path.join(OUT_DIR, "ItemCatalog.Xile.g.cs"), "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def disambiguate(items, own, taken_names=()):
    """RO-style "[n]" for slotted duplicates, a roman numeral for exact ones; Runeheir's own names win."""
    by_name = collections.defaultdict(list)
    for item in items:
        by_name[item.name].append(item)
    taken = set(own) | set(taken_names)
    aliases = {i.ro_id: i.alias for i in items if i.alias}
    for name, group in by_name.items():
        clash = len(group) > 1 or name in own or name in taken
        for item in group:
            if item.alias:
                continue
            if name in STAND_INS:
                item.alias = STAND_INS[name]
            elif name in own and (item.sockets == 0 or name in ICONIC):
                item.alias = own[name]
            elif clash and item.sockets:
                item.name = f"{name} [{item.sockets}]"
            if item.alias:
                aliases[item.ro_id] = item.alias
    seen = collections.Counter()
    numerals = ["", " II", " III", " IV", " V", " VI", " VII", " VIII"]
    for item in sorted((i for i in items if not i.alias), key=lambda i: i.ro_id):
        base = item.name
        while item.name in taken:
            seen[base] += 1
            n = seen[base]
            item.name = base + (numerals[n] if n < len(numerals) else f" {n + 1}")
        taken.add(item.name)
    return aliases


MONSTERS_OUT = os.path.join(PROJECT, "Assets", "_Runeheir", "Scripts", "Core", "Monsters", "Generated")
NPC_ROOT = os.path.normpath(os.path.join(ro_parse.SERVER_DB, "..", "..", "npc"))
EXCLUDED_MONSTERS = r"Treasure|Emperium|Barricade|Guardian Stone|Flag|Food (Storage|Depot)|Soccer Ball|Bring it on"


def import_bestiary(server, client, aliases, importable, own, names):
    """Cards (as items) and the monsters that spawn in Ragnarok's fields and dungeons, with their slaves."""
    mobs = {int(m["Id"]): m for m in ro_parse.read_yaml(os.path.join(ro_parse.SERVER_DB, "mob_db.yml"))}
    spawns = bestiary.read_spawns(NPC_ROOT)
    slaves = bestiary.read_summons(ro_parse.SERVER_DB)
    wanted = {mob for rows in spawns.values() for mob, _, _, _ in rows}
    frontier = list(wanted)
    while frontier:
        for slave in slaves.get(frontier.pop(), ()):
            if slave not in wanted:
                wanted.add(slave)
                frontier.append(slave)
    wanted = {i for i in wanted if i in mobs and not re.search(EXCLUDED_MONSTERS, str(mobs[i]["Name"]))}

    aegis_ids = {str(e.get("AegisName")): i for i, e in server.items() if e.get("AegisName")}

    def resolve_aegis(aegis):
        ro_id = aegis_ids.get(aegis)
        if ro_id is None:
            return None
        if ro_id in aliases:
            return aliases[ro_id]
        return f"xr{ro_id}" if ro_id in importable else None

    # Cards: every card in XileRetro's list, named for its (Norse) monster.
    card_ids = sorted(i for i in client if i in server and server[i].get("Type") == "Card")
    card_names = {i: server[i].get("CustomCardName") or bestiary.card_name(str(server[i]["Name"])) for i in card_ids}
    seen = collections.Counter()
    taken = set(own) | set(names.values())
    for i in card_ids:
        base = card_names[i]
        while card_names[i] in taken:
            seen[base] += 1
            card_names[i] = f"{base[:-5]} {['', 'II', 'III', 'IV', 'V', 'VI'][min(seen[base], 5)]} Card" if seen[base] <= 5 else f"{base[:-5]} {seen[base] + 1} Card"
        taken.add(card_names[i])
    card_rank = {}
    for mob in mobs.values():
        for d in mob.get("Drops") or []:
            ro = aegis_ids.get(str(d.get("Item")))
            if ro in card_names:
                card_rank[ro] = "Mvp" if mob.get("MvpExp") else "MiniBoss" if str(mob.get("Class")) == "Boss" else card_rank.get(ro, "Normal")
    card_ctx = {"card_names": card_names, "card_rank": card_rank}

    class Card:
        def __init__(self, ro_id):
            self.ro_id = ro_id
            self.name = card_names[ro_id]

    cards = [Card(i) for i in card_ids]

    # Monsters
    rh = bestiary.runeheir_reference(PROJECT)
    ro_rows = [m for i, m in mobs.items() if i in wanted and not m.get("MvpExp") and str(m.get("Class")) != "Boss"]

    def ro_points(f):
        return bestiary.smoothed([(int(m.get("Level") or 1), f(m)) for m in ro_rows])

    ro_curves = {
        "hp": bestiary.Curve(ro_points(lambda m: float(m.get("Hp") or 1))),
        "atk": bestiary.Curve(ro_points(lambda m: (float(m.get("Attack") or 1) + float(m.get("Attack2") or m.get("Attack") or 1)) / 2)),
        "def": bestiary.Curve(ro_points(lambda m: float(m.get("Defense") or 0) + 1)),
        "mdef": bestiary.Curve(ro_points(lambda m: float(m.get("MagicDefense") or 0) + 1)),
        "hit": bestiary.Curve(ro_points(lambda m: int(m.get("Level") or 1) + float(m.get("Dex") or 1))),
        "flee": bestiary.Curve(ro_points(lambda m: int(m.get("Level") or 1) + float(m.get("Agi") or 1))),
        "bexp": bestiary.Curve(ro_points(lambda m: float(m.get("BaseExp") or 1))),
        "jexp": bestiary.Curve(ro_points(lambda m: float(m.get("JobExp") or 1))),
    }
    rh_curves = {
        "hp": bestiary.Curve([(r[0], r[1]) for r in rh]), "atk": bestiary.Curve([(r[0], r[2]) for r in rh]),
        "def": bestiary.Curve([(r[0], r[3] + 1) for r in rh]), "mdef": bestiary.Curve([(r[0], r[4] + 1) for r in rh]),
        "hit": bestiary.Curve([(r[0], r[5]) for r in rh]), "flee": bestiary.Curve([(r[0], r[6]) for r in rh]),
        "bexp": bestiary.Curve([(r[0], r[7]) for r in rh]), "jexp": bestiary.Curve([(r[0], r[8]) for r in rh]),
    }
    monsters = [bestiary.Monster(mobs[i]) for i in sorted(wanted)]
    own_monster_names = set()
    for path in glob.glob(os.path.join(PROJECT, "Assets", "_Runeheir", "Scripts", "Core", "Monsters", "MonsterCatalog.*.cs")):
        own_monster_names.update(re.findall(r'Id = "[a-z_]+", Name = "([^"]+)"', open(path, encoding="utf-8").read()))
    bestiary.disambiguate(monsters, own_monster_names)
    MONSTER_NAMES.update({m.id: m.name for m in monsters})
    MVP_IDS.update(m.id for m in monsters if m.mvp)
    import monster_skills
    spawned = {mob for rows in spawns.values() for mob, _, _, _ in rows}
    monster_ids = {m.id for m in monsters}
    mob_skills = monster_skills.read_mob_skills(ro_parse.SERVER_DB)
    # Who a boss calls for help: its Ragnarok slaves, else the normal monsters that share its map.
    helpers = {}
    for m in monsters:
        if not m.boss:
            continue
        own_slaves = [s for s in sorted(slaves.get(m.ro_id, ())) if f"xrm{s}" in monster_ids and not mobs[s].get("MvpExp") and str(mobs[s].get("Class")) != "Boss"]
        if own_slaves:
            helpers[m.ro_id] = f"xrm{own_slaves[0]}"
            continue
        for rows in spawns.values():
            if any(mob == m.ro_id for mob, _, _, _ in rows):
                mates = [mob for mob, _, boss, _ in rows if not boss and mob != m.ro_id and f"xrm{mob}" in monster_ids
                         and str(mobs[mob].get("Class")) != "Boss"]
                if mates:
                    helpers[m.ro_id] = f"xrm{mates[0]}"
                    break
        if m.ro_id not in helpers:
            # No kin of its own: the closest-level normal monster of its race (or any race) answers the call.
            normals = [n for n in monsters if not n.boss]
            same = [n for n in normals if n.race == m.race] or normals
            helpers[m.ro_id] = min(same, key=lambda n: abs(n.level - m.level)).id
    ctx = {"ro_curves": ro_curves, "rh_curves": rh_curves, "resolve_aegis": resolve_aegis, "spawned": spawned,
           "monster_ids": {m.id for m in monsters if not m.boss}, "mob_skills": mob_skills, "helpers": helpers}
    placed = custom_gear.assign_drops(monsters, CUSTOM)
    print(f"custom XileRetro pieces: {sum(1 for e in CUSTOM.values() if e['Type'] in ('Weapon', 'Armor'))} gear, "
          f"{sum(1 for e in CUSTOM.values() if e['Type'] == 'Card')} cards, {sum(1 for e in CUSTOM.values() if e['Type'] == 'Petegg')} eggs; "
          f"{placed} given a monster to drop them")
    write_monsters(monsters, ctx)
    write_mercenaries(ro_curves, rh_curves)
    write_pets(aegis_ids, {str(m.mob.get("AegisName")): m.id for m in monsters},
               lambda ro: aliases.get(ro) or (f"xr{ro}" if ro in importable else None))
    print(f"cards {len(cards)}, monsters {len(monsters)} ({sum(1 for m in monsters if m.mvp)} MVPs, "
          f"{sum(1 for m in monsters if m.boss and not m.mvp)} mini-bosses)")
    globals()["LAST_MONSTERS"] = monsters
    globals()["LAST_SPAWNS"] = spawns
    return (cards, lambda c: bestiary.build_card(c.ro_id, server[c.ro_id], card_ctx)), monsters


WORLD_OUT = os.path.join(PROJECT, "Assets", "_Runeheir", "Scripts", "Core", "World", "Generated")
NPC_NAMES = ["Ásgeir", "Bera", "Dagný", "Eyja", "Finnr", "Gunnvör", "Hallr", "Ingunn", "Jórunn", "Ketill", "Líf", "Magni",
             "Njáll", "Oddný", "Ragnhildr", "Sigrún", "Þóra", "Úlfr", "Vígdís", "Yngvarr", "Arnkell", "Bjólfr", "Dísa", "Erlendr"]
WORLD_HEADER = """// <auto-generated>
// Generated by Tools/ItemImport/import_items.py from XileRetro's fields, dungeons and towns (rAthena's spawn and warp
// scripts), renamed under Norse myth. The maps themselves are generated by MapLayoutGenerator. Don't edit by hand.
// </auto-generated>
namespace Runeheir.World
{
    public static partial class MapCatalog
    {
"""


def import_world(monsters):
    monsters_by_ro = {m.ro_id: m for m in monsters}
    spawns = bestiary.read_spawns(NPC_ROOT)
    maps = world.build_world(spawns, monsters_by_ro, NPC_ROOT, os.path.join(ro_parse.SERVER_DB, "map_cache.dat"))

    # Prontera is Vigrid Haven: one road out (Midgard Road, the east gate) into the old south field; every way back
    # from those plains arrives there.
    vigrid = maps.get("prontera")
    if vigrid:
        # Vigrid Haven is hand-made and has room for one more gate: every other way into Prontera is dropped (those
        # maps keep their other roads, and connect() joins any that end up cut off).
        keep = next((p for p in vigrid.portals if p["TargetMap"] == "xr_prt_fild08"), None)
        partner = keep["TargetPortal"] if keep else None
        vigrid.portals = [keep] if keep else []
        if keep:
            keep.update({"Id": "midgard_road", "X": 57.0, "Z": 0.0, "Label": "Midgard Road"})
        for wm in maps.values():
            if wm is vigrid:
                continue
            kept = []
            for p in wm.portals:
                if p["TargetMap"] != "vigrid_haven":
                    kept.append(p)
                elif keep and wm.ro_map == "prt_fild08" and p["Id"] == partner:
                    p["TargetPortal"] = "midgard_road"
                    kept.append(p)
            wm.portals = kept
    roads = world.connect(maps)
    world.settle_spawns(maps)
    write_world(maps)
    print(f"world: {len(maps) - (1 if vigrid else 0)} maps ({sum(1 for w in maps.values() if w.kind == 'Town') - (1 if vigrid else 0)} towns), "
          f"{sum(len(w.portals) for w in maps.values())} portals, {roads} roads added")


def _portal_cs(var, p, kind):
    at_entrance = "" if kind != "Dungeon" else f", AtEntrance = {'true' if p.get('Entrance') else 'false'}"
    target = f', TargetPortal = "{p["TargetPortal"]}"' if p.get("TargetPortal") else ""
    return (f'            {var}.Portals.Add(new MapPortal {{ Id = "{p["Id"]}", Label = {st.cs_string(p["Label"])}, '
            f'X = {st._float(p["X"])}, Z = {st._float(p["Z"])}, TargetMap = "{p["TargetMap"]}"{target}{at_entrance} }});')


def write_world(maps):
    import math
    os.makedirs(WORLD_OUT, exist_ok=True)
    by_id = {wm.id: wm for wm in maps.values()}
    depth = {}
    for wm in maps.values():
        m = re.search(r"(\d+)$", wm.ro_map)
        depth[wm.id] = (0 if wm.kind != "Dungeon" else 1) * 100 + (int(m.group(1)) if m else 0)
    blocks = []
    towns = []
    ordered = [wm for wm in sorted(maps.values(), key=lambda w: (w.kind != "Town", world.level_band(w)[0], w.id)) if wm.id != "vigrid_haven"]
    # Pass 1: settle every map's portals, stairs and camp first (renaming a stairway touches its neighbour too).
    for wm in ordered:
        if wm.kind == "Town":
            wm.spawns, wm.bosses = [], []  # towns are safe, whatever roamed Ragnarok's streets
            world.place_town_portals(wm)
        else:
            # Roads (ships, airships) into a field or dungeon get a spot on a ring inside the edge.
            ring = [p for p in wm.portals if p.get("Road")]
            for i, p in enumerate(ring):
                angle = math.radians(200 + i * 47)
                radius = wm.size / 2 - 10
                p["X"], p["Z"] = round(math.cos(angle) * radius, 1), round(math.sin(angle) * radius, 1)
        for p in wm.portals:
            target = by_id.get(p["TargetMap"])
            p["Entrance"] = target is None or depth.get(target.id, 0) <= depth[wm.id]
        save_x, save_z = (0.0, -12.0) if wm.kind == "Town" else (0.0, 0.0)
        outdoor = wm.theme not in ("Crypt", "IceCavern")
        if wm.kind == "Field" and wm.portals:
            gate = next((p for p in wm.portals if by_id.get(p["TargetMap"]) is not None and by_id[p["TargetMap"]].kind == "Town"), wm.portals[0])
            px, pz = gate["X"], gate["Z"]
            dist = max(1.0, math.hypot(px, pz))
            save_x, save_z = round(px - px / dist * 12, 1), round(pz - pz / dist * 12, 1)
            # The camp must not sit on another way out: walk it in toward the middle until it's clear.
            for _ in range(20):
                if all(math.hypot(q["X"] - save_x, q["Z"] - save_z) >= 12 for q in wm.portals):
                    break
                save_x, save_z = round(save_x * 0.8, 1), round(save_z * 0.8, 1)
        if wm.kind == "Dungeon":
            # Every dungeon's way back toward the surface is its "up" stairs (where you arrive and where you save).
            ups = [q for q in wm.portals if q.get("Entrance")] or wm.portals[:1]
            if ups and not any(q["Id"] == "up" for q in wm.portals):
                up = min(ups, key=lambda q: depth.get(q["TargetMap"], 0))
                up["Entrance"] = True
                partner = by_id.get(up["TargetMap"])
                for q in (partner.portals if partner else []):
                    if q.get("TargetPortal") == up["Id"] and q["TargetMap"] == wm.id:
                        q["TargetPortal"] = "up"
                up["Id"] = "up"
        if wm.kind == "Dungeon" and outdoor and wm.portals:
            # An open-air dungeon is laid out like a field: camp just inside the stairs up.
            gate = next((q for q in wm.portals if q["Id"] == "up"), wm.portals[0])
            dist = max(1.0, math.hypot(gate["X"], gate["Z"]))
            save_x, save_z = round(gate["X"] - gate["X"] / dist * 12, 1), round(gate["Z"] - gate["Z"] / dist * 12, 1)
            for _ in range(20):
                if all(math.hypot(q["X"] - save_x, q["Z"] - save_z) >= 7 for q in wm.portals):
                    break
                save_x, save_z = round(save_x * 0.85, 1), round(save_z * 0.85, 1)
        wm.save = (save_x, save_z)

    # Pass 2: write them out.
    for wm in ordered:
        lo, hi = world.level_band(wm)
        save_x, save_z = wm.save
        names = sorted({by_id_name for by_id_name in (n for n in [m_name(s) for s in wm.spawns])})[:4]
        kind_word = {"Town": "A town", "Field": "Open country", "Dungeon": "A dungeon"}[wm.kind]
        desc = f"{kind_word} of the old world" + (f", home to {', '.join(names)}" if names else "") + "."
        lines = [f'            var map = Add(new MapDefinition',
                 f'            {{',
                 f'                Id = "{wm.id}", Name = {st.cs_string(wm.name)}, Kind = MapKind.{wm.kind}, Theme = MapTheme.{wm.theme},',
                 f'                MinLevel = {lo if wm.kind != "Town" else 0}, MaxLevel = {hi if wm.kind != "Town" else 0}, Size = {st._float(wm.size)}, '
                 f'Seed = {world.seed_of(wm.id) % 100000}, SaveX = {st._float(save_x)}, SaveZ = {st._float(save_z)},',
                 f'                Description = {st.cs_string(desc)},']
        if wm.kind == "Town":
            lines.append("                AllowBranches = false, AllowRandomTeleport = false,")
        lines.append("            });")
        for p in wm.portals:
            lines.append(_portal_cs("map", p, wm.kind))
        for mid, count in wm.spawns:
            lines.append(f'            map.Spawns.Add(new MapSpawn {{ MonsterId = "{mid}", Count = {count}, Radius = 8f }});')
        for mid, minutes in wm.bosses:
            mvp = mid in MVP_IDS
            lines.append(f'            map.Bosses.Add(new BossSpawn {{ MonsterId = "{mid}", RespawnMinutes = {"60f" if mvp else "120f"} }});')
        seed = world.seed_of(wm.id)
        if wm.kind == "Field":
            lines.append(f"            map.Npcs.Add(Courier({st._float(save_x - 6)}, {st._float(save_z)}));")
        elif wm.kind == "Town":
            towns.append(wm)
            n1, n2, n3 = (NPC_NAMES[(seed + k * 7) % len(NPC_NAMES)] for k in range(3))
            lines.append("            map.Npcs.Add(Courier(7f, -34f));")
            for (shop, name, title, x, greet) in (
                    ("GeneralStore", n1, "Trading Post", -14.0, "Tonics, feathers and honest gear for the road."),
                    ("WeaponSmith", n2, "Weapon Smith", -25.0, "Blades and bows. The good steel comes from Vigrid, but this will do."),
                    ("Armourer", n3, "Armourer", 25.0, "Helms, mail and warm cloaks.")):
                lines.append(f'            map.Npcs.Add(new MapNpc {{ Kind = NpcKind.Merchant, ShopId = Items.ShopCatalog.{shop}, Name = {st.cs_string(name)}, '
                             f'Title = "{title}", X = {st._float(x)}, Z = -18f, Greeting = {st.cs_string(greet)}, '
                             f'OutfitHex = "#{seed % 0xFFFFFF:06X}", Gender = {seed % 2} }});')
        blocks.append("\n".join(lines))
    methods, calls = [], []
    for n in range(0, len(blocks), 40):
        method = f"RegisterXileWorld{n // 40}"
        calls.append(f"            {method}();")
        inner ="\n\n".join("            {\n" + b.replace("\n            ", "\n                ") .replace("            var map", "                var map", 1) + "\n            }" for b in blocks[n:n + 40])
        methods.append(f"        private static void {method}()\n        {{\n{inner}\n        }}\n")
    vigrid = maps.get("prontera")
    extra = []
    if vigrid:
        for p in vigrid.portals:
            extra.append(_portal_cs("Get(VigridHaven)", p, "Town").replace("            ", "            ", 1))
    for wm in towns:
        extra.append(f'            Destinations.Add(new TeleportDestination {{ MapId = "{wm.id}", Zeny = 1200 }});')
    text = WORLD_HEADER
    text += f"        /// <summary>{len(blocks)} maps of XileRetro's world (called last by the static constructor).</summary>\n"
    text += "        private static void RegisterXileWorld()\n        {\n" + "\n".join(calls) + "\n" + "\n".join(extra) + "\n        }\n\n"
    text += "\n".join(methods) + "    }\n}\n"
    with open(os.path.join(WORLD_OUT, "MapCatalog.Xile.g.cs"), "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def m_name(spawn):
    return MONSTER_NAMES.get(spawn[0], spawn[0])


MONSTER_NAMES = {}
MVP_IDS = set()


MERCENARIES_OUT = os.path.join(PROJECT, "Assets", "_Runeheir", "Scripts", "Core", "Mercenaries", "Generated")


def write_mercenaries(ro_curves, rh_curves):
    os.makedirs(MERCENARIES_OUT, exist_ok=True)
    rows = sorted(mercenaries.contracts().values(), key=lambda m: (m.role is None, m.role or "", m.grade, m.ro_id))
    text = ("// <auto-generated>\n// Generated by Tools/ItemImport/import_items.py from rAthena's mercenary_db.yml. Don't edit by hand.\n"
            "// </auto-generated>\nusing Runeheir.Combat;\nusing Runeheir.Monsters;\nusing Runeheir.Skills;\n\nnamespace Runeheir.Mercenaries\n{\n"
            "    public static partial class MercenaryCatalog\n    {\n        private static void RegisterXileMercenaries()\n        {\n")
    for m in rows:
        fields = mercenaries.build_mercenary(m, ro_curves, rh_curves)
        role = f'"{mercenaries.ROLES[m.role][0]}"' if m.role else "null"
        text += (f"            Register(new MercenaryDefinition\n            {{\n                Grade = {m.grade}, Role = {role}, MinBaseLevel = {m.min_level},\n"
                 "                Monster = new MonsterDefinition\n                {\n"
                 + "".join(f"                    {f},\n" for f in fields) + "                },\n            });\n")
    text += "        }\n    }\n}\n"
    with open(os.path.join(MERCENARIES_OUT, "MercenaryCatalog.Xile.g.cs"), "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    print(f"mercenaries {len(rows)} ({sum(1 for m in rows if m.role)} of the Hird)")


PETS_OUT = os.path.join(PROJECT, "Assets", "_Runeheir", "Scripts", "Core", "Pets", "Generated")


def write_pets(aegis_ids, monster_ids, resolve):
    os.makedirs(PETS_OUT, exist_ok=True)
    extra = [{"Mob": e["CustomPetMob"], "EggItem": e["AegisName"], "FoodItem": "Pet_Food", "Fullness": 3, "IntimacyFed": 50,
              "CaptureRate": 0} for e in CUSTOM.values() if e["Type"] == "Petegg"]
    rows = pets.build_pets(aegis_ids, monster_ids, resolve, extra)
    text = ("// <auto-generated>\n// Generated by Tools/ItemImport/import_items.py from rAthena's pet_db.yml. Don't edit by hand.\n"
            "// </auto-generated>\nusing Runeheir.Combat;\nusing Runeheir.Items;\nusing Runeheir.Stats;\n\nnamespace Runeheir.Pets\n{\n"
            "    public static partial class PetCatalog\n    {\n        private static void RegisterXilePets()\n        {\n")
    for _, _, fields in rows:
        text += "            Register(new PetDefinition\n            {\n" + "".join(f"                {f},\n" for f in fields) + "            });\n"
    text += "        }\n\n        private static string XileBuff(string id, string name, string description, float seconds, StatModifiers modifiers)\n"
    text += "        {\n            return ItemCatalog.RegisterXileBuff(id, name, description, seconds, modifiers);\n        }\n    }\n}\n"
    with open(os.path.join(PETS_OUT, "PetCatalog.Xile.g.cs"), "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    print(f"pets {len(rows)}")


MONSTER_HEADER = """// <auto-generated>
// Generated by Tools/ItemImport/import_items.py from rAthena's pre-renewal monster database (the monsters that roam
// XileRetro's fields and dungeons), renamed under Norse myth and rescaled to Runeheir's levels. Don't edit by hand.
// </auto-generated>
using Runeheir.Combat;

namespace Runeheir.Monsters
{
    public static partial class MonsterCatalog
    {
"""


def write_monsters(monsters, ctx):
    os.makedirs(MONSTERS_OUT, exist_ok=True)
    methods, calls = [], []
    for n in range(0, len(monsters), PER_METHOD):
        chunk = monsters[n:n + PER_METHOD]
        method = f"RegisterXileMonsters{n // PER_METHOD}"
        calls.append(f"            {method}();")
        blocks = []
        for m in chunk:
            fields = bestiary.build_monster(m, ctx)
            blocks.append("            Register(new MonsterDefinition\n            {\n" + "".join(f"                {f},\n" for f in fields) + "            });")
        methods.append(f"        private static void {method}()\n        {{\n" + "\n\n".join(blocks) + "\n        }\n")
    text = MONSTER_HEADER
    text += f"        /// <summary>{len(monsters)} monsters from XileRetro's world (called last by the static constructor).</summary>\n"
    text += "        private static void RegisterXileMonsters()\n        {\n" + "\n".join(calls) + "\n        }\n\n"
    text += "\n".join(methods) + "    }\n}\n"
    with open(os.path.join(MONSTERS_OUT, "MonsterCatalog.Xile.g.cs"), "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


# Phase 8.4: XileRetro's own gear, cards and pet eggs, read back from their descriptions (see custom_gear.py).
CUSTOM = {}


def main():
    load_skill_names()
    server = ro_parse.read_items()
    client = ro_parse.read_item_info()
    own = own_items()

    mob_rows = ro_parse.read_yaml(os.path.join(ro_parse.SERVER_DB, "mob_db.yml"))
    spawned = {mob for rows in bestiary.read_spawns(NPC_ROOT).values() for mob, _, _, _ in rows}
    spawned_names = {str(m["Name"]).lower(): str(m["AegisName"]) for m in mob_rows if int(m["Id"]) in spawned}
    CUSTOM.update(custom_gear.collect(client, server, spawned_names, creatures_norse.creature_name, costumes.is_costume))
    early_costumes, _ = costumes.collect(client, set(server), set())
    chests, chest_aliases = custom_gear.boxes(client, server, CUSTOM, [c.id for c in early_costumes], costumes.is_costume)
    CUSTOM.update(chests)
    server.update(CUSTOM)

    skipped = collections.Counter()
    weapons, armor, wares = [], [], []
    for ro_id in sorted(client):
        entry = server.get(ro_id)
        if entry is None:
            if costumes.is_costume(client[ro_id]):
                continue  # costumes.py
            skipped["client-only XileRetro extras (coins, event and guild packages, quest items, eggs of absent monsters)"] += 1
            continue
        kind = entry.get("Type")
        if kind in goods.KIND_OF:
            wares.append(goods.Good(ro_id, entry))
            continue
        if kind not in ("Weapon", "Armor"):
            skipped[f"{kind} (later Phase 8 steps)"] += 1
            continue
        locations = list((entry.get("Locations") or {}).keys())
        if any(loc.startswith(("Costume_", "Shadow_")) for loc in locations) or not slots_of(locations):
            skipped["costume / shadow slots (Phase 8.4)"] += 1
            continue
        item = Item(ro_id, entry, client[ro_id])
        (weapons if item.kind == "Weapon" else armor).append(item)

    aliases = disambiguate(weapons + armor, own)
    aliases.update(chest_aliases)
    weapons = [i for i in weapons if not i.alias]
    armor = [i for i in armor if not i.alias]
    for ware in wares:
        ware.sockets = 0
    aliases.update(disambiguate(wares, own, [i.name for i in weapons + armor]))
    wares = [w for w in wares if not w.alias]

    # Box contents may name any XileRetro item: resolve to the imported id, an alias, or nothing.
    aegis_ids = {str(e.get("AegisName")): i for i, e in server.items() if e.get("AegisName")}
    handled = set(goods.KIND_OF) | {"Weapon", "Armor", "Card"}
    importable = {i for i in client if i in server and server[i].get("Type") in handled}
    names = {v: k for k, v in own.items()}
    names.update({i.id: i.name for i in weapons + armor + wares})

    # Phase 8.4: XileRetro's costumes, one item per look Runeheir can draw (the look-alikes alias to it).
    costume_list, costume_merged = costumes.collect(client, set(server), set(names.values()))
    aliases.update(costume_merged)
    importable |= {c.ro_id for c in costume_list}
    names.update({c.id: c.name for c in costume_list})

    def resolve(ro_id):
        if ro_id is None:
            return None
        if ro_id in aliases:
            return aliases[ro_id]
        return f"xr{ro_id}" if ro_id in importable else None

    def name_of(item_id):
        if item_id in names:
            return names[item_id]
        ro_id = int(item_id[2:]) if item_id.startswith("xr") and item_id[2:].isdigit() else None
        return names_norse.norse_name(server[ro_id]["Name"]) if ro_id in server else item_id

    mob_names = {str(m["AegisName"]): str(m["Name"]) for m in ro_parse.read_yaml(os.path.join(ro_parse.SERVER_DB, "mob_db.yml")) if m.get("AegisName")}
    tame_items = {}
    pet_armors = {}
    pet_eggs = set()
    for entry in pets.read_pets():
        if str(entry.get("Mob")) not in mob_names:
            continue
        creature = creatures_norse.creature_name(mob_names[str(entry.get("Mob"))])
        ro = aegis_ids.get(str(entry.get("TameItem")))
        if ro:
            tame_items[ro] = creature
        egg = aegis_ids.get(str(entry.get("EggItem")))
        if egg:
            pet_eggs.add(egg)
        accessory = aegis_ids.get(str(entry.get("EquipItem")))
        if accessory:
            pet_armors.setdefault(accessory, []).append(creature)
    ctx = {"resolve": resolve, "name_of": name_of, "aegis_ids": aegis_ids, "groups": goods.read_groups(), "tame_items": tame_items,
           "pet_armors": pet_armors, "pet_eggs": pet_eggs, "mercenaries": mercenaries.contracts()}

    os.makedirs(OUT_DIR, exist_ok=True)
    write_group("Weapons", weapons)
    write_group("Armor", armor)
    wares = write_group("Goods", wares, builder=lambda w: goods.build_good(w, ctx))
    write_group("Costumes", costume_list, builder=costumes.build)
    imported = {i.ro_id: i for i in weapons + armor}
    kinds = ["Weapon", "Shield", "Armor", "Garment", "Footgear", "Headgear", "Accessory"]

    def shelf(ro_id):
        """Shop order: by kind, weapon type and price; Runeheir's own stand-ins first in their kind."""
        item = imported.get(ro_id)
        if item is None:
            return (0, "", 0)
        return (kinds.index(item.kind), item.entry.get("SubType") or "", int(item.entry.get("Buy") or 0))

    stock = {}
    for kind, ro_ids in dealer_stock().items():
        ordered = sorted((r for r in ro_ids if r in imported or r in aliases), key=shelf)
        stock[kind] = list(dict.fromkeys(imported[r].id if r in imported else aliases[r] for r in ordered))
    hires = {m.ro_id: m for m in ctx["mercenaries"].values() if m.role}
    scrolls = [(hires[w.mercenary_id].role, hires[w.mercenary_id].grade, w.id) for w in wares if getattr(w, "mercenary_id", None) in hires]
    stock["Mercenary"] = [i for _, _, i in sorted(scrolls)]
    back = ("aura", "wings", "tail", "cape", "muffler")
    order = {"CostumeUpper": 0, "CostumeMid": 1, "CostumeLower": 2, "CostumeGarment": 3}
    ordered = sorted(costume_list, key=lambda c: (order[c.slots[0]], c.template, c.name))
    stock["CostumeHead"] = [c.id for c in ordered if not c.template.startswith(back)]
    stock["CostumeBack"] = [c.id for c in ordered if c.template.startswith(back)]
    cards, monsters = import_bestiary(server, client, aliases, importable, own, names)
    import_world(monsters)
    write_group("Cards", cards[0], builder=cards[1])
    write_index(["Weapons", "Armor", "Goods", "Costumes", "Cards"], aliases, stock)
    write_report(weapons + armor, aliases, skipped)
    kinds_count = collections.Counter(w.kind for w in wares)
    print("goods:", dict(kinds_count))
    print(f"costumes {len(costume_list)} looks ({len(costume_merged)} look-alikes merged): "
          f"{len(stock['CostumeHead'])} for the head, {len(stock['CostumeBack'])} for the back")
    print(f"weapons {len(weapons)}, armor {len(armor)}, aliases {len(aliases)}, skipped {sum(skipped.values())}, "
          f"dealer stock {len(stock['Weapon'])} weapons / {len(stock['Armor'])} armor")


def write_report(items, aliases, skipped):
    notes = collections.Counter()
    errors = []
    active = 0
    for item in items:
        base = item.result["base"]
        for note in base.notes:
            notes[re.sub(r"[+-]?\d+(\.\d+)?", "N", re.sub(r"^.*?: ", "", note))] += 1
        if not base.notes:
            active += 1
        errors += [f"{item.ro_id} {item.ro_name}: {e}" for e in item.result["errors"]]
    by_kind = collections.Counter(i.kind for i in items)
    renamed = sum(1 for i in items if i.name.split(" [")[0] != i.ro_name)
    lines = [
        "# XileRetro equipment import (Phase 8.1)",
        "",
        "Generated by `import_items.py`. Re-run the importer to refresh it.",
        "",
        f"- Imported: **{len(items)}** pieces ({', '.join(f'{k} {v}' for k, v in sorted(by_kind.items()))})",
        f"- Every bonus active in Runeheir: **{active}** ({active * 100 // max(1, len(items))}%); the rest list their inactive bonuses in the tooltip",
        f"- Renamed: {renamed}; aliased to Runeheir's own item: {len(aliases)}",
        f"- Script parse errors: {len(errors)}",
        "",
        "## Not imported in this step",
        "",
    ]
    lines += [f"- {reason}: {count}" for reason, count in skipped.most_common()]
    lines += ["", "## Aliases (Ragnarok id -> Runeheir item)", ""]
    lines += [f"- {ro_id} -> `{target}`" for ro_id, target in sorted(aliases.items())]
    lines += ["", "## Inactive bonuses (most common first)", "", "| Count | Bonus |", "|---:|---|"]
    lines += [f"| {count} | {note} |" for note, count in notes.most_common(60)]
    if errors:
        lines += ["", "## Script errors", ""] + [f"- {e}" for e in errors]
    lines += ["", "## Renames", "", "| RO id | Ragnarok | Runeheir |", "|---:|---|---|"]
    lines += [f"| {i.ro_id} | {i.ro_name} | {i.name} |" for i in sorted(items, key=lambda i: i.ro_id) if i.name != i.ro_name]
    with open(REPORT, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main()
