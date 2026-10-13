"""Ragnarok monster skills (mob_skill_db.txt) -> Runeheir MonsterSkills, plus boss phases.

Runeheir monsters use a small set of skill kinds (strike, bolt, telegraphed area, buff, heal, summon, leap, teleport), so
Ragnarok's hundreds of NPC_ skills fold into them by what they do: elemental and status strikes, bolts, breaths and
storms (telegraphed areas), self-buffs, heals, teleports and slave summons.
"""

import collections
import os

import script_translate as st
import skills_norse

ELEMENT_ATTACKS = {"NPC_FIREATTACK": "Fire", "NPC_WATERATTACK": "Water", "NPC_GROUNDATTACK": "Earth", "NPC_WINDATTACK": "Wind",
                   "NPC_POISONATTACK": "Poison", "NPC_HOLYATTACK": "Holy", "NPC_DARKNESSATTACK": "Shadow",
                   "NPC_TELEKINESISATTACK": "Ghost", "NPC_UNDEADATTACK": "Undead"}
STATUS_ATTACKS = {"NPC_STUNATTACK": "Stun", "NPC_POISON": "Poison", "NPC_SLEEPATTACK": "Sleep", "NPC_SILENCEATTACK": "Silence",
                  "NPC_BLINDATTACK": "Blind", "NPC_CURSEATTACK": "Curse", "NPC_PETRIFYATTACK": "StoneCurse", "NPC_BLEEDING": "Bleeding",
                  "NPC_FREEZEATTACK": "Freeze", "SM_BASH": "Stun", "NPC_CRITICALWOUND": "Bleeding"}
BOLTS = {"MG_FIREBOLT": "Fire", "MG_COLDBOLT": "Water", "MG_LIGHTNINGBOLT": "Wind", "MG_SOULSTRIKE": "Ghost", "MG_NAPALMBEAT": "Ghost",
         "MG_FIREBALL": "Fire", "WZ_JUPITEL": "Wind", "WZ_WATERBALL": "Water", "NPC_DARKSTRIKE": "Shadow", "AL_HOLYLIGHT": "Holy",
         "AC_DOUBLE": "Neutral", "NPC_RANGEATTACK": "Neutral", "HT_BLITZBEAT": "Neutral", "AS_GRIMTOOTH": "Neutral"}
AREAS = {"MG_THUNDERSTORM": ("Wind", 3.5), "WZ_METEOR": ("Fire", 4.5), "WZ_STORMGUST": ("Water", 5.0), "WZ_VERMILION": ("Wind", 5.0),
         "WZ_HEAVENDRIVE": ("Earth", 3.5), "WZ_EARTHSPIKE": ("Earth", 2.5), "WZ_FROSTNOVA": ("Water", 4.0), "MG_FIREWALL": ("Fire", 2.5),
         "NPC_EARTHQUAKE": ("Neutral", 6.0), "NPC_DARKBREATH": ("Shadow", 4.0), "NPC_FIREBREATH": ("Fire", 4.0),
         "NPC_ICEBREATH": ("Water", 4.0), "NPC_THUNDERBREATH": ("Wind", 4.0), "NPC_ACIDBREATH": ("Poison", 4.0),
         "NPC_DARKTHUNDER": ("Shadow", 4.0), "NPC_HELLJUDGEMENT": ("Shadow", 6.0), "NPC_PULSESTRIKE": ("Neutral", 5.0),
         "NPC_DRAGONFEAR": ("Neutral", 6.0), "SM_MAGNUM": ("Fire", 3.0), "AC_SHOWER": ("Neutral", 2.5), "NPC_SPLASHATTACK": ("Neutral", 2.5),
         "NPC_WIDEBLEEDING": ("Neutral", 5.0), "NPC_WIDESTUN": ("Neutral", 5.0), "NPC_WIDESLEEP": ("Neutral", 5.0),
         "NPC_WIDECURSE": ("Shadow", 5.0), "NPC_WIDEFREEZE": ("Water", 5.0), "NPC_WIDESILENCE": ("Neutral", 5.0),
         "NPC_WIDECONFUSE": ("Neutral", 5.0), "NPC_WIDEPOISON": ("Poison", 5.0), "NPC_WIDESTONE": ("Earth", 5.0),
         "NPC_WIDEBLIND": ("Shadow", 5.0), "NPC_WIDESOULDRAIN": ("Ghost", 5.0), "PR_MAGNUS": ("Holy", 4.0), "CR_GRANDCROSS": ("Holy", 4.0),
         "NPC_GRANDDARKNESS": ("Shadow", 5.0), "BS_HAMMERFALL": ("Neutral", 3.0), "KN_BOWLINGBASH": ("Neutral", 2.5),
         "KN_BRANDISHSPEAR": ("Neutral", 3.0), "AS_SPLASHER": ("Poison", 2.5)}
AREA_STATUS = {"NPC_WIDEBLEEDING": "Bleeding", "NPC_WIDESTUN": "Stun", "NPC_WIDESLEEP": "Sleep", "NPC_WIDECURSE": "Curse",
               "NPC_WIDEFREEZE": "Freeze", "NPC_WIDESILENCE": "Silence", "NPC_WIDEPOISON": "Poison", "NPC_WIDESTONE": "StoneCurse",
               "NPC_WIDEBLIND": "Blind", "WZ_STORMGUST": "Freeze", "WZ_FROSTNOVA": "Freeze", "BS_HAMMERFALL": "Stun",
               "NPC_DRAGONFEAR": "Stun", "WZ_METEOR": "Stun"}
BUFFS = {"NPC_POWERUP": "Enraged", "NPC_AGIUP": "Frenzied", "NPC_DEFENDER": "ShieldWall", "NPC_BARRIER": "StoneSkin",
         "NPC_STONESKIN": "StoneSkin", "NPC_HALLUCINATION": None, "KN_TWOHANDQUICKEN": "Frenzied", "LK_BERSERK": "Berserk",
         "MO_EXPLOSIONSPIRITS": "Enraged", "AL_INCAGI": "Frenzied", "NPC_ANTIMAGIC": "IceArmor", "AS_ENCHANTPOISON": "Enraged",
         "SM_ENDURE": "ShieldWall", "NPC_SELFDESTRUCTION": None}
SUMMONS = {"NPC_SUMMONSLAVE", "NPC_CALLSLAVE", "NPC_SUMMONMONSTER"}
HEALS = {"AL_HEAL", "NPC_HEAL", "PR_SANCTUARY"}
TELEPORTS = {"AL_TELEPORT", "NPC_RUN", "MO_BODYRELOCATION", "NPC_INVISIBLE"}
ELEMENT_WORD = {"Neutral": "Wild", "Water": "Frost", "Earth": "Stone", "Fire": "Flame", "Wind": "Storm", "Poison": "Venom",
                "Holy": "Holy", "Shadow": "Shadow", "Ghost": "Soul", "Undead": "Grave"}
STATUS_WORD = {"Stun": "Stunning", "Poison": "Venom", "Sleep": "Sleeping", "Silence": "Silencing", "Blind": "Blinding",
               "Curse": "Cursing", "StoneCurse": "Petrifying", "Bleeding": "Rending", "Freeze": "Freezing"}
AREA_NAMES = {"breath": "Breath", "storm": "Storm", "quake": "Quake"}


def read_mob_skills(server_db):
    path = os.path.join(server_db, "mob_skill_db.txt")
    skills = collections.defaultdict(list)
    if not os.path.exists(path):
        return skills
    for line in open(path, encoding="utf-8", errors="replace"):
        if line.startswith("//") or not line.strip():
            continue
        parts = [p.strip() for p in line.split(",")]
        if len(parts) < 12 or not parts[0].isdigit():
            continue
        aegis = parts[1].split("@", 1)[-1]
        try:
            row = {"aegis": aegis, "state": parts[2], "level": int(parts[4] or 1), "rate": int(parts[5] or 0),
                   "cast": int(parts[6] or 0), "delay": int(parts[7] or 0), "target": parts[9], "cond": parts[10],
                   "cond_value": parts[11], "vals": [p for p in parts[12:17]], "chat": parts[18] if len(parts) > 18 else ""}
        except ValueError:
            continue
        skills[int(parts[0])].append(row)
    return skills


def _skill(fields):
    return "new MonsterSkill { " + ", ".join(fields) + " }"


def _common(row, chance_floor=8.0):
    cooldown = max(5.0, row["delay"] / 1000.0)
    chance = max(chance_floor, min(60.0, row["rate"] / 100.0))
    out = [f"Cooldown = {st._float(round(cooldown, 1))}", f"ChancePercent = {st._float(round(chance, 1))}"]
    if row["cond"] == "myhpltmaxrate" and row["cond_value"].isdigit():
        out.append(f"BelowHpPercent = {st._float(float(row['cond_value']))}")
    return out


def convert(mob_id, rows, monster_ids, boss):
    """C# MonsterSkill expressions for one monster (at most five, the most useful first)."""
    out = []
    used = set()
    for row in sorted(rows, key=lambda r: (r["state"] not in ("attack", "chase", "angry", "berserk"), -r["rate"])):
        a = row["aegis"]
        if a in used:
            continue
        lv = max(1, row["level"])
        cast = st._float(round(min(3.0, row["cast"] / 1000.0), 2))
        sid = f'"xr_{a.lower()}"'
        known = skills_norse.runeheir_skill(a) or a in skills_norse.LABELS
        if a in ELEMENT_ATTACKS:
            el = ELEMENT_ATTACKS[a]
            out.append(_skill([f"Id = {sid}", f'Name = "{ELEMENT_WORD[el]} Strike"', "Kind = MonsterSkillKind.Strike",
                               f"Percent = {st._float(100 + 25 * min(lv, 10))}", f"Element = Element.{el}"] + _common(row)))
        elif a in STATUS_ATTACKS:
            status = STATUS_ATTACKS[a]
            out.append(_skill([f"Id = {sid}", f'Name = "{STATUS_WORD[status]} Strike"', "Kind = MonsterSkillKind.Strike",
                               "Percent = 120f", f"Status = StatusEffect.{status}", f"StatusChance = {st._float(min(60, 20 + 5 * lv))}",
                               "StatusSeconds = 4f"] + _common(row)))
        elif a in BOLTS:
            el = BOLTS[a]
            magic = a.startswith(("MG_", "WZ_", "NPC_DARK", "AL_"))
            out.append(_skill([f"Id = {sid}", f"Name = {st.cs_string(st.skill_label(a) if known else ELEMENT_WORD[el] + ' Bolt')}", "Kind = MonsterSkillKind.Bolt", "Range = 9f",
                               f"CastTime = {cast}", f"Percent = {st._float(min(500, 100 * min(lv, 5)))}",
                               f"Magical = {'true' if magic else 'false'}", f"Element = Element.{el}"] + _common(row)))
        elif a in AREAS:
            el, radius = AREAS[a]
            status = AREA_STATUS.get(a)
            kind = "Breath" if "BREATH" in a else "Quake" if a in ("NPC_EARTHQUAKE", "BS_HAMMERFALL", "WZ_HEAVENDRIVE", "WZ_EARTHSPIKE") else "Storm"
            label = st.skill_label(a) if known else ("Thunderstorm" if el == "Wind" and kind == "Storm" else f"{ELEMENT_WORD[el]} {kind}")
            self_centered = a in ("NPC_EARTHQUAKE", "SM_MAGNUM", "WZ_FROSTNOVA", "NPC_DRAGONFEAR", "NPC_SPLASHATTACK", "BS_HAMMERFALL") or a.startswith("NPC_WIDE")
            fields = [f"Id = {sid}", f"Name = {st.cs_string(label)}", "Kind = MonsterSkillKind.Area", f"Radius = {st._float(radius)}",
                      f"CastTime = {st._float(max(0.8, min(3.0, row['cast'] / 1000.0)))}", f"Percent = {st._float(min(400, 120 + 30 * min(lv, 10)))}",
                      f"Magical = {'true' if not a.startswith(('SM_', 'KN_', 'BS_', 'AC_', 'NPC_SPLASH')) else 'false'}",
                      f"Element = Element.{el}"]
            fields.append("CenteredOnSelf = true" if self_centered else "Range = 10f")
            if status:
                fields += [f"Status = StatusEffect.{status}", "StatusChance = 30f", "StatusSeconds = 4f"]
            out.append(_skill(fields + _common(row)))
        elif a in BUFFS and BUFFS[a]:
            out.append(_skill([f"Id = {sid}", f'Name = "{BUFFS[a]}"', "Kind = MonsterSkillKind.Buff", f"BuffId = MonsterBuffs.{BUFFS[a]}"]
                              + _common(row)))
        elif a in HEALS:
            out.append(_skill([f"Id = {sid}", 'Name = "Mend"', "Kind = MonsterSkillKind.Heal", "HealPercent = 8f",
                               "BelowHpPercent = 60f", "CastTime = 1f"] + [c for c in _common(row) if not c.startswith("BelowHp")]))
        elif a in TELEPORTS:
            out.append(_skill([f"Id = {sid}", 'Name = "Blink"', "Kind = MonsterSkillKind.Teleport", "Range = 8f",
                               "BehindTarget = true"] + _common(row)))
        elif a in SUMMONS:
            slaves = [int(v) for v in row["vals"] if v.isdigit() and int(v) > 1000 and f"xrm{int(v)}" in monster_ids]
            if not slaves:
                continue
            out.append(_skill([f"Id = {sid}", 'Name = "Call Kin"', "Kind = MonsterSkillKind.Summon", f'SummonId = "xrm{slaves[0]}"',
                               f"SummonCount = {min(4, max(2, len(slaves) + 1))}", "CastTime = 1.5f"] + _common(row, 20.0)))
        else:
            continue
        used.add(a)
        if len(out) >= (6 if boss else 4):
            break
    return out


def boss_extras(mob_id, element, skills_cs, slaves, rank):
    """Every boss calls for help and has a telegraphed attack; MVPs have two phases, mini-bosses one."""
    extra = []
    if not any("MonsterSkillKind.Summon" in s for s in skills_cs) and slaves:
        extra.append(_skill(['Id = "xr_call"', 'Name = "Call Kin"', "Kind = MonsterSkillKind.Summon", f'SummonId = "{slaves}"',
                             "SummonCount = 3", "CastTime = 1.5f", "Cooldown = 40f", "ChancePercent = 50f"]))
    if not any("MonsterSkillKind.Area" in s and "Radius" in s for s in skills_cs):
        extra.append(_skill(['Id = "xr_quake"', f'Name = "{ELEMENT_WORD[element]} Quake"', "Kind = MonsterSkillKind.Area",
                             "CenteredOnSelf = true", "Radius = 5f", "CastTime = 1.6f", "Percent = 220f", "Magical = true",
                             f"Element = Element.{element}", "Cooldown = 14f", "ChancePercent = 35f"]))
    phases = ['new BossPhase { BelowHpPercent = 50f, Shout = "Enough!", BuffId = MonsterBuffs.Enraged }']
    if rank == "Mvp":
        phases.append('new BossPhase { BelowHpPercent = 20f, Shout = "You will not leave this place!", BuffId = MonsterBuffs.Unbound }')
    return extra, phases
