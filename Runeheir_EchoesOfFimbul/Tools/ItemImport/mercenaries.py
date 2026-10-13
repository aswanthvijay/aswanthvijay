"""Phase 8: Ragnarok's mercenaries (rAthena mercenary_db.yml) as Runeheir's hired swords.

Thirty fighters of the Hird (ten bowmen, ten spearmen and ten huscarls, grades 1-10) and the monster contracts (the
Mimic, the Maid Wight, Valkyrie Randgris...). A hire scroll brings one to fight beside you for 30 minutes. Their stats
scale onto Runeheir's level curve the way the bestiary's do; their Ragnarok skills fold into the monster skill kinds
(strikes, bolts, telegraphed areas, buffs on themselves and their master, and mending).

The hired fighters' own names are new (Norse); only the numbers come from Ragnarok.
"""

import os
import re

import bestiary
import creatures_norse
import ro_parse
import script_translate as st
import skills_norse

BASE_DB = os.path.join(os.path.dirname(ro_parse.SERVER_DB), "mercenary_db.yml")
CONTRACT_SECONDS = 1800

# Role (from the AegisName) -> title, the weapon drawn on the model, outfit color.
ROLES = {"ARCHER": ("Bowman", "Bow", "#5E7F3A"), "LANCER": ("Spearman", "Spear", "#4A6A8C"), "SWORDMAN": ("Huscarl", "OneHandSword", "#8C4A3A")}
HIRED_NAMES = {
    "ARCHER": ["Ylva", "Sigrid", "Hildr", "Thora", "Ragnhild", "Gunnhild", "Astrid", "Solveig", "Brynja", "Valdis"],
    "LANCER": ["Bjorn", "Ulf", "Halvar", "Torstein", "Ketil", "Orm", "Arnulf", "Gorm", "Styrbjorn", "Einar"],
    "SWORDMAN": ["Leif", "Hakon", "Sigvard", "Ragnvald", "Egil", "Asbjorn", "Thorkel", "Harald", "Sveinn", "Grimr"],
}
# What each grade's scroll costs at the Hird Hall.
GRADE_PRICES = [7000, 14000, 21000, 30000, 40000, 60000, 70000, 100000, 140000, 280000]
SPECIAL_NAMES = {"Male Game Master": "Einherjar Champion", "Female Game Master": "Shieldmaiden of Valhalla"}

# Ragnarok mercenary skill -> the player skill it copies (for its Runeheir name).
EQUIVALENT = {
    "MS_BASH": "SM_BASH", "MA_DOUBLE": "AC_DOUBLE", "ML_PIERCE": "KN_PIERCE", "MS_MAGNUM": "SM_MAGNUM", "MA_SHOWER": "AC_SHOWER",
    "ML_BRANDISH": "KN_BRANDISHSPEAR", "MS_BOWLINGBASH": "KN_BOWLINGBASH", "ML_SPIRALPIERCE": "LK_SPIRALPIERCE",
    "MA_CHARGEARROW": "AC_CHARGEARROW", "MA_SHARPSHOOTING": "SN_SHARPSHOOTING", "MA_LANDMINE": "HT_LANDMINE",
    "MA_FREEZINGTRAP": "HT_FREEZINGTRAP", "MA_SANDMAN": "HT_SANDMAN", "MA_SKIDTRAP": "HT_SKIDTRAP", "MER_LEXDIVINA": "PR_LEXDIVINA",
    "MER_DECAGI": "AL_DECAGI", "MER_QUICKEN": "KN_TWOHANDQUICKEN", "MER_AUTOBERSERK": "LK_BERSERK", "MS_BERSERK": "LK_BERSERK",
    "ML_AUTOGUARD": "CR_AUTOGUARD", "ML_DEFENDER": "CR_DEFENDER", "MS_PARRYING": "LK_PARRYING", "MS_REFLECTSHIELD": "CR_REFLECTSHIELD",
    "MER_BLESSING": "AL_BLESSING", "MER_INCAGI": "AL_INCAGI", "MER_MAGNIFICAT": "PR_MAGNIFICAT",
}
FALLBACK_NAMES = {"MER_CRASH": "Crushing Blow", "MER_REGAIN": "Mend", "MER_TENDER": "Mend", "MER_RECUPERATE": "Mend",
                  "MER_MENTALCURE": "Mend", "MER_COMPRESS": "Mend", "MER_BENEDICTION": "Mend"}
MENDS = {"MER_REGAIN", "MER_TENDER", "MER_RECUPERATE", "MER_MENTALCURE", "MER_COMPRESS", "MER_BENEDICTION"}
GUARDS = {"ML_AUTOGUARD", "ML_DEFENDER", "MS_PARRYING", "MS_REFLECTSHIELD"}
TRAPS = {"MA_LANDMINE": ("Earth", "Stun"), "MA_FREEZINGTRAP": ("Water", "Freeze"), "MA_SANDMAN": ("Neutral", "Sleep"),
         "MA_SKIDTRAP": ("Neutral", None)}


def read_mercenaries():
    return ro_parse.read_yaml(BASE_DB) if os.path.exists(BASE_DB) else []


class Mercenary:
    """One contract: its Runeheir id and name, level, the base level a hirer needs, and its role (or None for monsters)."""

    def __init__(self, entry):
        self.entry = entry
        self.ro_id = int(entry["Id"])
        self.id = f"xrmerc{self.ro_id}"
        aegis = str(entry.get("AegisName") or "")
        self.role = next((r for r in ROLES if aegis.startswith(f"MER_{r}")), None)
        self.grade = int(aegis[-2:]) if self.role and aegis[-2:].isdigit() else 0
        ro_name = str(entry.get("Name") or aegis)
        if self.role and 1 <= self.grade <= 10:
            self.name = f"{HIRED_NAMES[self.role][self.grade - 1]} the {ROLES[self.role][0]}"
        else:
            self.name = SPECIAL_NAMES.get(ro_name) or creatures_norse.creature_name(ro_name)
        self.ro_name = ro_name
        self.ro_level = int(entry.get("Level") or 1)
        self.level = bestiary.runeheir_level(self.ro_level)
        # Ragnarok's guild lets you hire a grade a few levels below its own.
        self.min_level = bestiary.runeheir_level(max(1, self.ro_level - 5))
        self.price = GRADE_PRICES[self.grade - 1] if self.role and 1 <= self.grade <= 10 else 0


def scroll_targets():
    """Ragnarok ids some usable item hires (mercenary_create)."""
    path = os.path.join(ro_parse.SERVER_DB, "item_db_usable.yml")
    text = open(path, encoding="utf-8").read() if os.path.exists(path) else ""
    return {int(i) for i in re.findall(r"mercenary_create\s+(\d+)", text)}


def contracts():
    """Every hireable mercenary by Ragnarok id: the Hird's thirty and the monsters a scroll hires (Game Masters renamed)."""
    wanted = scroll_targets()
    return {m.ro_id: m for m in (Mercenary(e) for e in read_mercenaries()) if m.role or m.ro_id in wanted}


# ---------------------------------------------------------------------------------------------- skills
def _skill(fields):
    return "new MonsterSkill { " + ", ".join(fields) + " }"


def _label(aegis):
    equivalent = EQUIVALENT.get(aegis)
    if equivalent and (skills_norse.runeheir_skill(equivalent) or equivalent in skills_norse.LABELS):
        return st.skill_label(equivalent)
    return FALLBACK_NAMES.get(aegis)


def convert_skills(rows):
    """C# MonsterSkill expressions for a mercenary's Ragnarok skills (unknown ones are left out)."""
    out = []
    for row in rows or []:
        a = str(row.get("Name"))
        lv = max(1, int(row.get("MaxLevel") or 1))
        name = _label(a)
        if not name:
            continue
        sid = f'"merc_{a.lower()}"'
        head = [f"Id = {sid}", f"Name = {st.cs_string(name)}"]
        if a in ("MS_BASH", "ML_PIERCE", "MER_CRASH", "ML_SPIRALPIERCE"):
            percent = {"MS_BASH": 100 + 30 * lv, "ML_PIERCE": 100 + 10 * lv, "MER_CRASH": 100 + 10 * lv, "ML_SPIRALPIERCE": 150 + 50 * lv}[a]
            fields = head + ["Kind = MonsterSkillKind.Strike", f"Percent = {st._float(min(500, percent))}", "Cooldown = 6f", "ChancePercent = 40f"]
            if a == "MER_CRASH":
                fields += ["Status = StatusEffect.Stun", f"StatusChance = {st._float(6 * lv)}", "StatusSeconds = 3f"]
            if a == "ML_SPIRALPIERCE":
                fields.append("CastTime = 0.8f")
            out.append(_skill(fields))
        elif a in ("MA_DOUBLE", "MA_CHARGEARROW", "MA_SHARPSHOOTING"):
            percent = {"MA_DOUBLE": 2 * (90 + 10 * lv), "MA_CHARGEARROW": 150, "MA_SHARPSHOOTING": 200 + 50 * lv}[a]
            sharp = a == "MA_SHARPSHOOTING"
            fields = head + ["Kind = MonsterSkillKind.Bolt", "Range = 10f", f"Percent = {st._float(min(500, percent))}", "Magical = false",
                             "Cooldown = 12f" if sharp else "Cooldown = 5f", "ChancePercent = 45f"]
            if a == "MA_CHARGEARROW":
                fields.append("Knockback = 3f")
            if sharp:
                fields.append("CastTime = 1.5f")
            out.append(_skill(fields))
        elif a in ("MS_MAGNUM", "ML_BRANDISH", "MS_BOWLINGBASH"):
            element = "Fire" if a == "MS_MAGNUM" else "Neutral"
            percent = {"MS_MAGNUM": 100 + 20 * lv, "ML_BRANDISH": 100 + 20 * lv, "MS_BOWLINGBASH": 100 + 40 * lv}[a]
            fields = head + ["Kind = MonsterSkillKind.Area", "CenteredOnSelf = true", "Radius = 3f", "CastTime = 0.6f",
                             f"Percent = {st._float(min(500, percent))}", "Magical = false", f"Element = Element.{element}",
                             "Cooldown = 8f", "ChancePercent = 40f"]
            if a == "MS_BOWLINGBASH":
                fields.append("Knockback = 2f")
            out.append(_skill(fields))
        elif a == "MA_SHOWER" or a in TRAPS:
            element, status = TRAPS.get(a, ("Neutral", None))
            fields = head + ["Kind = MonsterSkillKind.Area", "Range = 10f", "Radius = 2.5f", "CastTime = 0.8f",
                             f"Percent = {st._float(min(400, 75 + 25 * lv if a == 'MA_SHOWER' else 100 + 20 * lv))}", "Magical = false",
                             f"Element = Element.{element}", "Cooldown = 10f", "ChancePercent = 35f"]
            if status:
                fields += [f"Status = StatusEffect.{status}", f"StatusChance = {st._float(min(80, 30 + 10 * lv))}", "StatusSeconds = 4f"]
            if a == "MA_SKIDTRAP":
                fields.append("Knockback = 4f")
            out.append(_skill(fields))
        elif a in ("MER_LEXDIVINA", "MER_DECAGI"):
            status = "Silence" if a == "MER_LEXDIVINA" else "Frostbite"
            out.append(_skill(head + ["Kind = MonsterSkillKind.Bolt", "Range = 9f", "Percent = 1f", "Magical = true",
                                      f"Status = StatusEffect.{status}", f"StatusChance = {st._float(min(90, 50 + 10 * lv))}",
                                      "StatusSeconds = 6f", "CastTime = 0.5f", "Cooldown = 15f", "ChancePercent = 30f"]))
        elif a == "MER_QUICKEN":
            out.append(_skill(head + ["Kind = MonsterSkillKind.Buff", "BuffId = MonsterBuffs.Frenzied", "Cooldown = 30f", "ChancePercent = 60f"]))
        elif a in ("MER_AUTOBERSERK", "MS_BERSERK"):
            out.append(_skill(head + ["Kind = MonsterSkillKind.Buff", "BuffId = MonsterBuffs.Berserk", "BelowHpPercent = 25f",
                                      "Cooldown = 60f", "ChancePercent = 100f"]))
        elif a in GUARDS:
            out.append(_skill(head + ["Kind = MonsterSkillKind.Buff", "BuffId = MonsterBuffs.ShieldWall", "BelowHpPercent = 70f",
                                      "Cooldown = 30f", "ChancePercent = 50f"]))
        elif a in ("MER_BLESSING", "MER_INCAGI", "MER_MAGNIFICAT"):
            buff = {"MER_BLESSING": "SkillBuffs.OdinsBlessing", "MER_INCAGI": "SkillBuffs.SwiftWind", "MER_MAGNIFICAT": "SkillBuffs.GaldrOfEir"}[a]
            out.append(_skill(head + ["Kind = MonsterSkillKind.Buff", f"BuffId = {buff}", f"BuffLevel = {min(10, lv)}", "Radius = 12f",
                                      "Cooldown = 40f", "ChancePercent = 60f"]))
        elif a in MENDS:
            out.append(_skill(head + ["Kind = MonsterSkillKind.Heal", f"HealPercent = {st._float(min(20, 5 + 2 * lv))}", "BelowHpPercent = 60f",
                                      "Radius = 12f", "CastTime = 1f", "Cooldown = 12f", "ChancePercent = 70f"]))
    # Mending and guarding first (the AI takes the first usable skill), then the strongest attacks.
    order = {"Heal": 0, "Buff": 1, "Area": 2, "Bolt": 3, "Strike": 4}
    out.sort(key=lambda s: order.get(s.split("MonsterSkillKind.")[1].split(",")[0].split(" ")[0], 5))
    return out


# ---------------------------------------------------------------------------------------------- definitions
def build_mercenary(m, ref_ro, ref_rh):
    """C# MercenaryDefinition initializer: stats on Runeheir's curve (a mercenary is worth about a strong monster of its level)."""
    e = m.entry
    L, L2 = m.ro_level, m.level

    def scaled(stat, ro_value, floor=1, lo=0.5, hi=3.0):
        ratio = ro_value / max(1e-9, ref_ro[stat].at(L)) if ro_value > 0 else 0.0
        if ratio <= 0:
            return 0
        return max(floor, round(ref_rh[stat].at(L2) * min(hi, max(lo, bestiary.soften(ratio)))))

    hp = scaled("hp", float(e.get("Hp") or 1), lo=0.8, hi=1.5)
    atk = scaled("atk", (float(e.get("Attack") or 1) + float(e.get("Attack2") or e.get("Attack") or 1)) / 2, lo=0.9)
    defense = min(300, scaled("def", float(e.get("Defense") or 0) + 1, 0))
    mdef = min(150, scaled("mdef", float(e.get("MagicDefense") or 0) + 1, 0))
    hit = max(5, scaled("hit", L + float(e.get("Dex") or 1), lo=1.0))
    flee = max(1, scaled("flee", L + float(e.get("Agi") or 1)))
    reach = int(e.get("AttackRange") or 1)
    attack_range = 0.9 if reach <= 2 else min(12.0, reach * 0.9)
    interval = max(0.6, min(2.5, float(e.get("AttackDelay") or 1400) / 1000.0))
    walk = float(e.get("WalkSpeed") or 150)
    speed = max(3.5, min(6.0, 3.0 * 200.0 / max(50.0, walk)))
    race = bestiary.RACES.get(str(e.get("Race") or "Formless"), "Formless")
    element = bestiary.ELEMENTS.get(str(e.get("Element") or "Neutral"), "Neutral")
    size = str(e.get("Size") or "Medium")

    fields = [f'Id = "{m.id}", RoId = {m.ro_id}, Name = {st.cs_string(m.name)}, Level = {L2}, MaxHp = {hp}, '
              f'AtkMin = {round(atk * 0.85)}, AtkMax = {round(atk * 1.15)}',
              f"Def = {defense}, Mdef = {mdef}, Hit = {hit}, Flee = {flee}",
              f"Element = Element.{element}, Race = Race.{race}, Size = Size.{size}",
              f"MoveSpeed = {st._float(round(speed, 2))}, AttackInterval = {st._float(round(interval, 2))}, "
              f"AttackRange = {st._float(round(attack_range, 1))}, SummonOnly = true"]
    skills = convert_skills(e.get("Skills"))
    if skills:
        fields.append("Skills =\n                    {\n" + "".join(f"                        {s},\n" for s in skills) + "                    }")
    if m.role:
        title, weapon, color = ROLES[m.role]
        fields.append(f'Shape = MonsterShape.Humanoid, ColorHex = "{color}", Scale = 1f, LookWeapon = WeaponType.{weapon}, '
                      f'SkinHex = "{bestiary.pick(["#F2D4BE", "#E8C0A0", "#D9A980", "#F5DCC8"], m.ro_id)}"')
        if m.role == "SWORDMAN":
            fields.append('LookShield = "round_viking_shield"')
        if m.role == "ARCHER":
            fields.append("LookFemale = true")
    else:
        shape = "Humanoid" if m.ro_name in SPECIAL_NAMES or "Valkyrie" in m.ro_name else bestiary.shape_of(m.name + " " + m.ro_name, race)
        scale = {"Small": 0.7, "Medium": 1.0, "Large": 1.3}.get(size, 1.0)
        fields.append(f'Shape = MonsterShape.{shape}, ColorHex = "{bestiary.color_of(element, m.ro_id)}", Scale = {st._float(scale)}')
        variant = bestiary.variant_of(m.name + " " + m.ro_name)
        if variant and shape in ("Quadruped", "Insect", "Plant", "Slime", "Bird", "Fish"):
            fields.append(f'Variant = "{variant}"')
        if shape == "Humanoid":
            fields.append("LookWeapon = WeaponType.OneHandSword" if "Valkyrie" in m.ro_name or m.ro_name in SPECIAL_NAMES else "LookWeapon = WeaponType.Unarmed")
            if "Valkyrie" in m.ro_name or "Female" in m.ro_name or m.ro_name == "Alice":
                fields.append("LookFemale = true")
    return fields


def hired_by_scroll(m, scroll_name):
    """The text on a hire scroll."""
    who = f"{m.name} (Lv {m.level})"
    return (f"Hires {who} to fight beside you for {CONTRACT_SECONDS // 60} minutes. Needs base level {m.min_level}. "
            f"One mercenary at a time; using another scroll ends the old contract.")
