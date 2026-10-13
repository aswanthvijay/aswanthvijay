"""Phase 8.3: XileRetro's usable items, loot, ammunition, cash and pet items, as Runeheir items.

Usable scripts (potions, food, boxes, scrolls, wings) run through the same interpreter as gear scripts, with a few more
commands: itemheal, percentheal, heal, sc_start (timed buffs), sc_end (cures), getitem / rentitem (boxes),
getgroupitem (random boxes, from rAthena's item groups), set Zeny (pouches), itemskill (scrolls) and monster (branches).
Anything Runeheir can't do yet leaves the item marked NotUsableYet: using it does nothing and it isn't consumed.
"""

import hashlib
import os
import re

import mercenaries
import names_norse
import ro_parse
import script_translate as st
import skills_norse

# Ragnarok status changes -> (StatModifiers field or stat, how val1 maps). "stat:Str" = SetStat.
SC_BUFFS = {
    "SC_STRFOOD": [("stat:Str", "v")], "SC_AGIFOOD": [("stat:Agi", "v")], "SC_VITFOOD": [("stat:Vit", "v")],
    "SC_INTFOOD": [("stat:Int", "v")], "SC_DEXFOOD": [("stat:Dex", "v")], "SC_LUKFOOD": [("stat:Luk", "v")],
    "SC_FOOD_STR_CASH": [("stat:Str", "v")], "SC_FOOD_AGI_CASH": [("stat:Agi", "v")], "SC_FOOD_VIT_CASH": [("stat:Vit", "v")],
    "SC_FOOD_INT_CASH": [("stat:Int", "v")], "SC_FOOD_DEX_CASH": [("stat:Dex", "v")], "SC_FOOD_LUK_CASH": [("stat:Luk", "v")],
    "SC_SAVAGE_STEAK": [("stat:Str", "v")], "SC_COCKTAIL_WARG_BLOOD": [("stat:Int", "v")], "SC_MINOR_BBQ": [("stat:Vit", "v")],
    "SC_SIROMA_ICE_TEA": [("stat:Dex", "v")], "SC_DROCERA_HERB_STEAMED": [("stat:Agi", "v")], "SC_PUTTI_TAILS_NOODLES": [("stat:Luk", "v")],
    "SC_HITFOOD": [("Hit", "v")], "SC_FLEEFOOD": [("Flee", "v")], "SC_CRIFOOD": [("Crit", "v")], "SC_BATKFOOD": [("Atk", "v")],
    "SC_WATKFOOD": [("Atk", "v")], "SC_MATKFOOD": [("Matk", "v")], "SC_ATKPOTION": [("Atk", "v")], "SC_MATKPOTION": [("Matk", "v")],
    "SC_FULL_SWING_K": [("Atk", "v")], "SC_MANA_PLUS": [("Matk", "v")],
    "SC_ASPDPOTION0": [("AspdPercent", 10)], "SC_ASPDPOTION1": [("AspdPercent", 15)], "SC_ASPDPOTION2": [("AspdPercent", 20)],
    "SC_ASPDPOTION3": [("AspdPercent", 25)], "SC_EXTRACT_SALAMINE_JUICE": [("AspdPercent", "v")],
    "SC_SPEEDUP0": [("MoveSpeedPercent", 25)], "SC_SPEEDUP1": [("MoveSpeedPercent", 50)], "SC_SLOWDOWN": [("MoveSpeedPercent", -25)],
    "SC_BLESSING": [("stat:Str", "v"), ("stat:Int", "v"), ("stat:Dex", "v")],
    "SC_INCREASEAGI": [("stat:Agi", "v+2"), ("MoveSpeedPercent", 25)], "SC_INCLUK": [("stat:Luk", "v")],
    "SC_INCCRI": [("Crit", "v")], "SC_DEF_RATE": [("DefPercent", "v")], "SC_MDEF_RATE": [("MdefPercent", "v")],
    "SC_EXPBOOST": [("ExpPercent", "v")], "SC_ITEMBOOST": [("DropPercent", "v")], "SC_JEXPBOOST": [("ExpPercent", "v")],
    "SC_EXTRACT_WHITE_POTION_Z": [("HpRegenPercent", "v")], "SC_VITATA_500": [("SpRegenPercent", "v")],
}
FIELD_LABELS = {"Hit": "HIT", "Flee": "FLEE", "Crit": "CRIT", "Atk": "ATK", "Matk": "MATK", "AspdPercent": "ASPD",
                "MoveSpeedPercent": "movement speed", "DefPercent": "DEF", "MdefPercent": "MDEF", "ExpPercent": "EXP gained",
                "DropPercent": "item drop chance", "HpRegenPercent": "HP regeneration", "SpRegenPercent": "SP regeneration"}
PERCENT_FIELDS = {"AspdPercent", "MoveSpeedPercent", "DefPercent", "MdefPercent", "ExpPercent", "DropPercent", "HpRegenPercent",
                  "SpRegenPercent"}
SC_CURES = {"SC_Poison": "Poison", "SC_DPoison": "Poison", "SC_Silence": "Silence", "SC_Blind": "Blind", "SC_Curse": "Curse",
            "SC_Stone": "StoneCurse", "SC_Freeze": "Freeze", "SC_Sleep": "Sleep", "SC_Stun": "Stun", "SC_Bleeding": "Bleeding"}

# Ragnarok items Runeheir already has: the potions, wings, branches and refine ores it was modelled on.
STAND_INS = {501: "lingonberry_tonic", 504: "honey_mead", 505: "aether_sap", 601: "wind_rune_shard", 602: "raven_feather",
             604: "dead_branch", 12103: "blood_branch", 1010: "bog_iron", 1011: "dwarven_steel", 984: "starmetal", 985: "skystone"}

AMMO_FOR = {"Arrow": "Bow", "Bullet": "ThunderRod", "Grenade": "ThunderRod", "Cannonball": "ThunderRod", "Shuriken": "Huuma",
            "Kunai": "Huuma"}

ETC_FLAVOR = [
    (r"Feather|Plume", "Light as breath and still holding the wind."),
    (r"Fang|Tooth|Claw|Talon|Horn|Tusk|Nail", "Taken from something that wanted to keep it."),
    (r"Skin|Hide|Pelt|Fur|Leather|Scale", "Tough enough to stitch into armor."),
    (r"Bone|Skull", "It rattles when you shake the bag."),
    (r"Ore|Iron|Steel|Metal|Coal|Stone|Rock|Crystal|Gem|Jewel|Diamond|Ruby|Emerald|Sapphire|Opal|Topaz|Garnet|Pearl",
     "A smith or a jeweller would want this."),
    (r"Herb|Leaf|Flower|Root|Seed|Mushroom|Petal|Stem|Moss|Grass|Fruit|Berry|Apple|Carrot", "Cut from the fields and the forest edge."),
    (r"Cloth|Silk|Thread|Rag|Ribbon|Lace|Wool", "Good cloth is hard to come by in the north."),
    (r"Potion|Bottle|Flask|Water|Wine|Juice|Milk|Honey", "Something to drink or brew with."),
    (r"Card Album|Album", "A book of empty pockets."),
    (r"Key|Map|Letter|Document|Scroll|Book|Note|Certificate|Permit|Ticket|Coupon|Token|Badge|Medal|Seal",
     "Someone, somewhere, is waiting for this."),
]


def stable_pick(options, key):
    return options[hashlib.md5(str(key).encode()).digest()[0] % len(options)]


def etc_flavor(name, ro_id):
    for pattern, line in ETC_FLAVOR:
        if re.search(pattern, name, re.I):
            return line
    return stable_pick(["Worth keeping, or worth selling.", "Loot from the road.", "Merchants will take it off your hands.",
                        "A trophy of the hunt."], ro_id)


# ---------------------------------------------------------------------------------------------- usable scripts
class Usable:
    def __init__(self):
        self.hp = None          # (min, max)
        self.sp = None
        self.hp_percent = 0
        self.sp_percent = 0
        self.cures = []
        self.gives = []         # (ro id or aegis, amount)
        self.group = None       # IG_ constant
        self.zeny = None        # (min, max)
        self.skill = None       # (runeheir id, level)
        self.special = None     # ItemSpecialEffect name
        self.mercenary = None   # Ragnarok mercenary id (mercenary_create)
        self.buff = {}          # field -> value
        self.buff_seconds = 0
        self.notes = []

    def active(self):
        return bool(self.hp or self.sp or self.hp_percent or self.sp_percent or self.cures or self.gives or self.group
                    or self.zeny or self.skill or self.special or self.buff or self.mercenary)


def _range(v):
    if isinstance(v, int):
        return (v, v)
    if isinstance(v, st.Rand):
        return (v.low, v.high)
    return None


def translate_usable(script, aegis_ids):
    u = Usable()
    if not script or not script.strip():
        return u
    try:
        tree = st.parse(script)
    except st.ScriptError:
        u.notes.append("a scripted effect")
        return u
    for guard, cmd, args in st.run(tree, 0):
        if guard and any("job" in g.text or " as a " in g.text for g in guard):
            continue  # class-specific branches: the general one applies
        c = cmd.lower()
        if c == "itemheal" and args:
            u.hp = _range(args[0]) if _range(args[0]) and _range(args[0])[1] > 0 else u.hp
            if len(args) > 1 and _range(args[1]) and _range(args[1])[1] > 0:
                u.sp = _range(args[1])
        elif c == "percentheal" and args:
            u.hp_percent = args[0] if isinstance(args[0], int) and args[0] > 0 else u.hp_percent
            if len(args) > 1 and isinstance(args[1], int) and args[1] > 0:
                u.sp_percent = args[1]
        elif c == "heal" and args:
            if _range(args[0]) and _range(args[0])[1] > 0:
                u.hp = _range(args[0])
        elif c in ("sc_start", "sc_start2", "sc_start4") and len(args) >= 2:
            sc = str(args[0])
            mapping = SC_BUFFS.get(sc)
            val1 = args[2] if len(args) > 2 and isinstance(args[2], int) else 0
            if mapping and isinstance(args[1], int):
                for field, how in mapping:
                    value = how if isinstance(how, int) else val1 + (2 if how == "v+2" else 0)
                    u.buff[field] = u.buff.get(field, 0) + value
                u.buff_seconds = max(u.buff_seconds, args[1] / 1000)
            else:
                u.notes.append(f"a {sc.replace('SC_', '').replace('_', ' ').lower()} effect")
        elif c == "sc_end" and args:
            if str(args[0]) in SC_CURES:
                u.cures.append(SC_CURES[str(args[0])])
        elif c in ("getitem", "rentitem") and args:
            ro = args[0] if isinstance(args[0], int) else aegis_ids.get(str(args[0]))
            amount = args[1] if c == "getitem" and len(args) > 1 and isinstance(args[1], int) else 1
            if ro:
                u.gives.append((ro, amount))
        elif c == "getgroupitem" and args:
            u.group = str(args[0])
        elif c == "set" or (c == "zeny"):
            pass
        elif c == "itemskill" and args:
            aegis = str(args[0])
            level = args[1] if len(args) > 1 and isinstance(args[1], int) else 1
            if aegis == "AL_TELEPORT":
                u.special = "ReturnToSavePoint" if level >= 2 else "RandomTeleport"
            elif skills_norse.runeheir_skill(aegis):
                u.skill = (skills_norse.runeheir_skill(aegis), level)
            else:
                u.notes.append(f"casts {st.skill_label(aegis)}")
        elif c == "monster":
            u.special = "SummonMonster"
        elif c == "mercenary_create" and args and isinstance(args[0], int):
            u.mercenary = args[0]
        elif c in ("specialeffect", "specialeffect2", "skilleffect", "end", "close"):
            pass
        else:
            u.notes.append(f"a {cmd} effect")
    # set Zeny,Zeny+rand(a,b): the interpreter stores it as an assignment, so read it from the text.
    m = re.search(r"Zeny\s*[,=]\s*Zeny\s*\+\s*rand\((\d+)\s*,\s*(\d+)\)", script)
    if m:
        u.zeny = (int(m.group(1)), int(m.group(2)))
    return u


# ---------------------------------------------------------------------------------------------- item groups
def read_groups():
    path = os.path.join(ro_parse.SERVER_DB, "item_group_db.yml")
    groups = {}
    if not os.path.exists(path):
        return groups
    for entry in ro_parse.read_yaml(path):
        name = str(entry.get("Group", ""))
        items = []
        for sub in entry.get("SubGroups") or []:
            for row in sub.get("List") or []:
                if row.get("Item"):
                    items.append((str(row["Item"]), int(row.get("Rate") or 1), int(row.get("Amount") or 1)))
        groups[re.sub(r"[^a-z0-9]", "", name.lower())] = items
    return groups


def group_key(constant):
    return re.sub(r"[^a-z0-9]", "", constant.lower().replace("ig_", "", 1))


# ---------------------------------------------------------------------------------------------- C#
def cs(s):
    return st.cs_string(s)


def _fmt_range(r):
    return f"{r[0]:,}" if r[0] == r[1] else f"{r[0]:,}~{r[1]:,}"


def buff_cs(buff):
    inits, chain = [], ""
    for field, value in buff.items():
        if field.startswith("stat:"):
            chain += f".SetStat(StatType.{field[5:]}, {value})"
        elif field in st.Fx.INT_FIELDS:
            inits.append(f"{field} = {value}")
        else:
            inits.append(f"{field} = {st._float(value)}")
    return "new StatModifiers" + (" { " + ", ".join(inits) + " }" if inits else "()") + chain


def buff_text(buff, seconds):
    parts = []
    for field, value in buff.items():
        if field.startswith("stat:"):
            parts.append(f"{field[5:].upper()} {value:+d}")
        elif field in PERCENT_FIELDS:
            parts.append(f"{FIELD_LABELS[field]} {value:+g}%")
        else:
            parts.append(f"{FIELD_LABELS.get(field, field)} {value:+g}")
    minutes = seconds / 60
    span = f"{minutes:g} min" if seconds >= 60 else f"{seconds:g} s"
    return ", ".join(parts) + f" for {span}"


# ---------------------------------------------------------------------------------------------- items
KIND_OF = {"Healing": "usable", "Usable": "usable", "Delayconsume": "usable", "Cash": "usable", "Etc": "etc", "Ammo": "ammo",
           "Petegg": "egg", "Petarmor": "petarmor"}
ICON_COLORS = {"usable": "#C0392B", "etc": "#A9927D", "ammo": "#B2BABB", "egg": "#F5CBA7", "petarmor": "#D7BDE2"}


class Good:
    def __init__(self, ro_id, entry):
        self.ro_id = ro_id
        self.entry = entry
        self.ro_name = entry["Name"]
        self.name = names_norse.norse_name(entry["Name"])
        self.kind = KIND_OF[entry["Type"]]
        self.alias = STAND_INS.get(ro_id)

    @property
    def id(self):
        return f"xr{self.ro_id}"


def initials(name):
    words = [w for w in re.findall(r"[A-Za-zÀ-ÿ]+", name) if w[0].isupper()] or [name]
    return "".join(w[0] for w in words[:3]).upper() if len(words) > 1 else name[:2].upper()


# Loot that does something in Runeheir.
SPECIAL_ETC = {
    7142: "A living seed of Iðunn's lore. A Brewmaster weaves a homunculus from it (Weave Life). Brewed from a Seed of Life, "
          "Morning Dew of Yggdrasil, a Glass Tube and a Medicine Bowl.",
    7140: "A seed that still remembers being alive. One of the four parts of an Embryo.",
    7141: "Dew from the World Tree's leaves at dawn. One of the four parts of an Embryo.",
    7143: "A thin glass vessel, for brewing. One of the four parts of an Embryo.",
    7134: "A brewer's mixing bowl. One of the four parts of an Embryo.",
}


def build_good(good, ctx):
    """C# field initializers for one imported good. ctx: resolve(ro id) -> Runeheir id or None, aegis_ids, groups."""
    e = good.entry
    weight = int(round((e.get("Weight") or 0) / 10))
    price = int(e.get("Buy") or (e.get("Sell") or 0) * 2)
    category = {"Cash": "CashShop", "Petegg": "PetEgg", "Petarmor": "PetArmor", "Ammo": "Ammo"}.get(e["Type"], "Standard")
    fields = [f'Id = "{good.id}", RoId = {good.ro_id}, Name = {cs(good.name)}']
    desc = []
    if good.ro_id in ctx.get("pet_eggs", ()):
        good.kind = "egg"  # a few eggs (the Leaf Cat's ball) are filed as loot

    if good.kind == "ammo":
        result = st.translate(e.get("Script"), False)
        base = result["base"]
        element = base.weapon_element
        base.weapon_element = None
        base.desc = [d for d in base.desc if not d.endswith("element attacks")]
        ammo_for = AMMO_FOR.get(e.get("SubType"))
        if ammo_for is None:
            return None  # throwing daggers and potions: no Runeheir weapon shoots them
        fields.append(f"Kind = ItemKind.Ammo, Slots = EquipSlot.Ammo, AmmoFor = WeaponType.{ammo_for}, Atk = {int(e.get('Attack') or 0)}")
        if element:
            fields.append(f"Element = Element.{element}")
        effect = st.cs_effect(base, buffs=st.BuffNamer(good.ro_id, good.name))
        if effect:
            fields.append(f"Effect = {effect}")
        what = {"Bow": "Arrows for a bow", "ThunderRod": "Shot for a thunder-rod", "Huuma": "Throwing iron for a nightraider"}[ammo_for]
        desc.append(f"{what}. Wear the stack in the ammunition slot; each shot spends one.")
        rules = st.describe(result)
        if rules:
            desc.append(rules)
    elif good.kind == "petarmor":
        fields.append("Kind = ItemKind.Consumable, Special = ItemSpecialEffect.PetArmor")
        wearers = ctx.get("pet_armors", {}).get(good.ro_id, [])
        who = ", ".join(sorted(set(wearers))) if wearers else "its pet"
        desc.append(f"An accessory for a pet ({who}). Use it while that pet is out to dress it up.")
    elif good.kind == "etc":
        fields.append("Kind = ItemKind.Etc")
        desc.append(SPECIAL_ETC.get(good.ro_id) or etc_flavor(good.name, good.ro_id))
    else:
        u = translate_usable(e.get("Script"), ctx["aegis_ids"])
        fields.append("Kind = ItemKind.Consumable")
        if u.hp:
            fields.append(f"HealHpMin = {u.hp[0]}, HealHpMax = {u.hp[1]}")
            desc.append(f"Restores {_fmt_range(u.hp)} HP.")
        if u.sp:
            fields.append(f"HealSpMin = {u.sp[0]}, HealSpMax = {u.sp[1]}")
            desc.append(f"Restores {_fmt_range(u.sp)} SP.")
        if u.hp_percent or u.sp_percent:
            if u.hp_percent:
                fields.append(f"HealHpPercent = {st._float(u.hp_percent)}")
            if u.sp_percent:
                fields.append(f"HealSpPercent = {st._float(u.sp_percent)}")
            both = [f"{u.hp_percent}% of Max HP" if u.hp_percent else None, f"{u.sp_percent}% of Max SP" if u.sp_percent else None]
            desc.append("Restores " + " and ".join(b for b in both if b) + ".")
        if u.cures:
            cures = list(dict.fromkeys(u.cures))
            fields.append("Cures = new[] { " + ", ".join(f"StatusEffect.{c}" for c in cures) + " }")
            desc.append("Cures " + ", ".join(st.STATUS_LABEL.get(c, c) for c in cures) + ".")
        if u.buff and u.buff_seconds > 0:
            buff_id = f"xrb{good.ro_id}_1"
            text = buff_text(u.buff, u.buff_seconds)
            fields.append(f"BuffId = XileBuff(\"{buff_id}\", {cs(good.name)}, {cs(text[0].upper() + text[1:] + '.')}, "
                          f"{st._float(u.buff_seconds)}, {buff_cs(u.buff)})")
            desc.append(text[0].upper() + text[1:] + ".")
        gives = []
        for ro, amount in u.gives:
            target = ctx["resolve"](ro)
            if target:
                gives.append((target, amount))
        if gives:
            fields.append("GivesItems = new[] { " + ", ".join(f'new ItemGrant("{t}", {a})' for t, a in gives) + " }")
            desc.append("Opening it gives " + ", ".join(f"{ctx['name_of'](t)}{f' x{a}' if a > 1 else ''}" for t, a in gives) + ".")
        if u.group:
            rows = ctx["groups"].get(group_key(u.group), [])
            entries = []
            for aegis, rate, amount in rows:
                target = ctx["resolve"](ctx["aegis_ids"].get(aegis))
                if target:
                    entries.append((target, amount, rate))
            if entries:
                fields.append("RandomGives = new[] { " + ", ".join(f'new ItemGrant("{t}", {a}, {r})' for t, a, r in entries) + " }")
                desc.append(f"Holds one of {len(entries):,} possible treasures.")
        if u.zeny:
            fields.append(f"GivesZenyMin = {u.zeny[0]}, GivesZenyMax = {u.zeny[1]}")
            desc.append(f"Holds {_fmt_range(u.zeny)} zeny.")
        custom_gives = e.get("CustomGives") or []
        if custom_gives:
            # Phase 8.4: a XileRetro mystery box, holding one of its kind's pieces at random.
            fields.append("RandomGives = new[] { " + ", ".join(f'new ItemGrant("{t}", 1, 1)' for t in custom_gives) + " }")
            desc.append(f"A chest from a slain boss. Open it for one of {len(custom_gives):,} treasures, at random.")
        if u.skill:
            fields.append(f'CastSkillId = "{u.skill[0]}", CastSkillLevel = {u.skill[1]}')
            desc.append(f"Casts Lv {u.skill[1]} {st.RUNEHEIR_SKILL_NAMES.get(u.skill[0], u.skill[0])} for free.")
        if u.special:
            fields.append(f"Special = ItemSpecialEffect.{u.special}")
            desc.append({"RandomTeleport": "Carries you to a random spot on this map.",
                         "ReturnToSavePoint": "Carries you back to your save point.",
                         "SummonMonster": "Breaking it summons a random monster."}[u.special])
        if u.mercenary:
            merc = ctx.get("mercenaries", {}).get(u.mercenary)
            if merc:
                good.mercenary_id = merc.ro_id
                fields.append(f'Special = ItemSpecialEffect.HireMercenary, MercenaryId = "{merc.id}"')
                desc.append(mercenaries.hired_by_scroll(merc, good.name))
                if merc.price:
                    price = merc.price
            else:
                u.mercenary = None
        if good.kind == "egg":
            fields.append("Special = ItemSpecialEffect.HatchPet")
            desc.insert(0, "A pet's egg. Use it to hatch your pet (J opens the pet window).")
        elif good.ro_id in ctx.get("tame_items", {}):
            fields.append("Special = ItemSpecialEffect.TamePet")
            desc.append(f"Use it while fighting a {ctx['tame_items'][good.ro_id]} to try to tame it.")
        elif not u.active() and not custom_gives:
            fields.append("NotUsableYet = true")
            desc.append("Not usable in Runeheir yet.")

    fields.insert(1, f"Description = {cs(' '.join(desc) or etc_flavor(good.name, good.ro_id))}")
    fields.append(f"Weight = {weight}, Price = {price}")
    if category != "Standard":
        fields.append(f"Category = ItemCategory.{category}")
    fields.append(f'IconLabel = {cs(initials(good.name))}, IconColorHex = "{ICON_COLORS[good.kind]}"')
    return fields
