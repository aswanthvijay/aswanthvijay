"""Phase 8.2 / 8.5: XileRetro's monsters and their cards, as Runeheir monsters and Soul Cards.

Levels stretch from Ragnarok's 1–99 onto Runeheir's 1–255. Each stat keeps the monster's place relative to Ragnarok
monsters of its level (a tough Lv 40 stays tough), measured against Runeheir's own curve at the new level, so imported
monsters sit beside the hand-made bestiary. Extremes (MVPs with 50x the HP of their level) are softened, not erased.
"""

import collections
import glob
import hashlib
import math
import os
import re

import creatures_norse
import monster_skills
import names_norse
import ro_parse
import script_translate as st

RACES = {"Formless": "Formless", "Undead": "Undead", "Brute": "Beast", "Plant": "Plant", "Insect": "Insect", "Fish": "Fish",
         "Demon": "Demon", "Demihuman": "DemiHuman", "DemiHuman": "DemiHuman", "Angel": "Angel", "Dragon": "Dragon"}
ELEMENTS = {"Neutral": "Neutral", "Water": "Water", "Earth": "Earth", "Fire": "Fire", "Wind": "Wind", "Poison": "Poison",
            "Holy": "Holy", "Dark": "Shadow", "Ghost": "Ghost", "Undead": "Undead"}
ELEMENT_ADJ = {"Neutral": "Wild", "Water": "Frost", "Earth": "Stone", "Fire": "Flame", "Wind": "Storm", "Poison": "Venom",
               "Holy": "Holy", "Shadow": "Shadow", "Ghost": "Spectral", "Undead": "Grave"}
ELEMENT_COLORS = {"Neutral": "#9C8B6E", "Water": "#4F8FC0", "Earth": "#8B6B3E", "Fire": "#C8502F", "Wind": "#5BB59B",
                  "Poison": "#7D4C9A", "Holy": "#E8D9A0", "Shadow": "#3B3350", "Ghost": "#A9B8C8", "Undead": "#5E6B5A"}
AGGRESSIVE_AI = {"04", "05", "08", "09", "10", "12", "13", "19", "20", "21", "26", "27"}
ASSIST_AI = {"03", "07", "13"}
STILL_AI = {"06", "10"}

FLYER = r"fly|bat|bird|hornet|wasp|butterfly|moth|chonchon|harpy|gryphon|owl|condor|eagle|raven|dragon ?fly|bee\b|vitata|creamy|gnat|mosquito|anopheles|wing|hill wind|raggler|crag|toucan|wisp|plasma|breeze"
WRAITH = r"ghost|wraith|whisper|banshee|spirit|specter|soul|myst|mist|shade|phantom|wight|haunt|lude|quve|odium|despero|maero|dolor|thanatos|dauði"
HUMANOID = r"knight|skeleton|zombie|draugr|orc|troll|goblin|svartling|kobold|soldier|archer|priest|gothi|sorcer|assassin|bandit|outlaw|raider|thug|samurai|nightraider|shinobi|ninja|warrior|general|lord|king|chief|witch|lady|maiden|druid|bishop|necromancer|hermit|seyren|eremes|howard|margaretha|cecil|kathryne|einherjar|deadeye|archmage|forgelord|shadow walker|mummy|haugb|jester|joker|pasana|zerom|marduk|isis|naga|merman|marmennill|mermaid|sohee|dancer|zealot|valkyrie|skogul|frus|skeggiold|randgris|alice|maid|aliza|doll|cookie|gingerbread|elder|baba|hag|huldra|mavka|chepet|wanderer|dullahan|\brider\b|sigvaldi|eirik|hrafnkell|margrét|sæunn|katla|egna|vigdís|ármóðr|erna|kári|laufey|gangster|mobster|vampire|bathory|loli"
GOLEM = r"golem|stone|rock\b|megalith|obsidian|mineral|\bore\b|titan|giant|bradium|rimesteel|clock|cannon|dvergr|construct|engine|stove|apocalypse|automaton|colossus|mimic|chest|box|crystal|totem|gajomart"
SERPENT = r"snake|serpent|boa|anaconda|side ?winder|naga|drake|dragon|wyrm|lindworm|worm|eel|hydra|fáfnir|níðhöggr|sea serpent|strouf|centipede"
BLOB = r"slim|poring|plant|mushroom|spore|toadstool|egg|flora|rafflesia|mandragora|tangleweed|sunflower|puffball|stem|flytrap|sundew|pitcher|butterwort|tree|willow|cactus|muka|jelly|plankton|anemone|polyp|shell|starfish|porridge|rice|pupa|puff|dryad|parasite|grove|marina|sea mine"
# Phase 8 bodies, checked before the older ones they overlap (slimes and plants were Blobs; insects, birds and fish
# were four-legged beasts or serpents).
SLIME = r"slim|poring|\bdrops\b|marin\b|jelly|ooze|angeling|deviling|ghostring|metaling|mastering|blob"
PLANT = r"plant|flora|rafflesia|mandragora|tangleweed|sunflower|flytrap|sundew|pitcher|butterwort|tree|willow|cactus|muka|dryad|grove|geographer|flower|blossom|rose\b|lily|vine|sprout|thorn"
INSECT = r"argiope|argos|beetle|\bant\b|\bants\b|spider|tarantula|scorpion|mantis|louse|cricket|crab|lobster|crayfish|shrimp|roach|thief bug|\bbug\b|vadon|pincher|stag\b|horn\b"
BIRD = r"peco|chick|picky|condor|ostrich|\bhen\b|rooster|cockatrice|goose|duck|cluck|fowl"
FISH = r"fish|piranha|shark|swordfish|tuna|carp|marlin|\bphen\b|pike|salmon|trout|angler|marse|squid|octopus|gill"

# Details the body builders draw from a creature's name (MonsterDefinition.Variant).
VARIANTS = [
    (r"spider|tarantula|arachn|argiope|argos", "spider"), (r"scorpion", "scorpion"), (r"crab|lobster|crayfish|shrimp|vadon|pincher", "crab"),
    (r"mantis", "mantis"), (r"beetle|horn\b|stag\b", "beetle"),
    (r"boar|\bpig|swine|savage|tusk|hog\b", "tusks"), (r"goat|\box\b|bull|\bram\b|yak|bison|mammoth|horned|antler|elk|deer|stag", "horns"),
    (r"hare|rabbit|lunatic|bunny|eclipse", "long_ears"),
    (r"wolf|\bdog|hound|fox|\bcat\b|tiger|leopard|jaguar|lion|bear|coyote|lynx|badger|smokie|raccoon", "ears"),
    (r"turtle|tortoise", "shell"), (r"frog|toad|lizard|gecko|salamander|newt|roda", "low"),
    (r"flytrap|rafflesia|mandragora|man eater|geographer|pitcher|sundew", "jaws"),
    (r"sunflower|flower|blossom|\brose\b|lily|tulip", "flower"), (r"tree|willow|dryad|treant|grove", "tree"), (r"cactus|muka", "cactus"),
    (r"angeling|angel", "angel"), (r"deviling|devil", "devil"), (r"ghostring|ghost", "ghost"),
    (r"peco|condor|ostrich", "tall"), (r"shark", "shark"), (r"swordfish|marlin", "sword"),
]

QUADRUPED = r"wolf|boar|bear|hound|dog|tiger|leopard|cat|fox|hare|rabbit|lunatic|rat|mole|hedgehog|badger|goat|ox|bull|strider|peco|chick|picky|beetle|ant\b|ant |spider|scorpion|mantis|louse|cricket|frog|toad|lizard|turtle|crab|seal|otter|armadillo|steed|mare|horse|jaguar|lion|mammoth|beast|grub|caterpillar|sandworm|gryphon"

LOOK_WEAPONS = [(r"archer|shooter|sniper|deadeye|sæunn|cecil", "Bow"), (r"priest|gothi|sorcer|archmage|witch|hag|druid|necromancer|katla|kathryne|elder|seer|marduk|hermit", "Staff"),
                (r"knight|general|einherjar|sigvaldi|seyren|warlord|chief|king|jarl|captain", "TwoHandSword"),
                (r"assassin|eirik|eremes|nightraider|shinobi|thief|outlaw|bandit|raider", "Katar"), (r"troll|orc|berserker", "TwoHandAxe"),
                (r"musket|firelock", "ThunderRod"), (r"forgelord|howard|smith|miner|worker", "Mace"), (r"soldier|guard|warrior|samurai|specter|draugr|skeleton", "OneHandSword")]


# ---------------------------------------------------------------------------------------------- curves
class Curve:
    """Piecewise-linear interpolation of log(value) over level, flat-extrapolated by the end slopes."""

    def __init__(self, points):
        pts = sorted((lv, math.log(max(1.0, v))) for lv, v in points if v > 0)
        merged = collections.OrderedDict()
        for lv, v in pts:
            merged.setdefault(lv, []).append(v)
        self.xs = list(merged)
        self.ys = [sorted(vs)[len(vs) // 2] for vs in merged.values()]

    def at(self, level):
        xs, ys = self.xs, self.ys
        if len(xs) == 1:
            return math.exp(ys[0])
        if level <= xs[0]:
            slope = (ys[1] - ys[0]) / max(1, xs[1] - xs[0])
            return math.exp(ys[0] + slope * (level - xs[0]))
        if level >= xs[-1]:
            slope = (ys[-1] - ys[-2]) / max(1, xs[-1] - xs[-2])
            return math.exp(ys[-1] + slope * (level - xs[-1]))
        for i in range(1, len(xs)):
            if level <= xs[i]:
                t = (level - xs[i - 1]) / max(1e-9, xs[i] - xs[i - 1])
                return math.exp(ys[i - 1] + t * (ys[i] - ys[i - 1]))
        return math.exp(ys[-1])


def smoothed(points, window=6):
    """RO reference: the median value in a moving window of levels (so one odd monster doesn't bend the curve)."""
    by_level = collections.defaultdict(list)
    for lv, v in points:
        if v > 0:
            by_level[lv].append(v)
    out = []
    levels = sorted(by_level)
    for lv in levels:
        near = [v for l2 in levels if abs(l2 - lv) <= window for v in by_level[l2]]
        out.append((lv, sorted(near)[len(near) // 2]))
    return out


def soften(ratio, exponent=0.6):
    """Keep a monster's toughness relative to its level, but squash extremes so they stay fightable."""
    if ratio <= 0:
        return 1.0
    if ratio > 2.0:
        return 2.0 * (ratio / 2.0) ** exponent
    if ratio < 0.4:
        return 0.4 * (ratio / 0.4) ** exponent
    return ratio


# Runeheir's own bosses against a normal monster of their level: HP, ATK, EXP and evasion multiples.
BOSS_MULTIPLES = {
    "Mvp": {"hp": 12.0, "atk": 1.45, "bexp": 25.0, "jexp": 25.0, "hit": 1.3, "flee": 1.3, "def": 1.5, "mdef": 1.3},
    "MiniBoss": {"hp": 8.0, "atk": 1.3, "bexp": 15.0, "jexp": 15.0, "hit": 1.2, "flee": 1.2, "def": 1.3, "mdef": 1.2},
}


def runeheir_level(ro_level):
    return max(1, min(255, round(1 + (ro_level - 1) * 254 / 98)))


def runeheir_reference(project):
    """Runeheir's hand-made normal monsters: (level, hp, atk, def, mdef, hit, flee, base exp, job exp)."""
    rows = []
    for path in glob.glob(os.path.join(project, "Assets", "_Runeheir", "Scripts", "Core", "Monsters", "MonsterCatalog.*.cs")):
        text = open(path, encoding="utf-8").read()
        for block in re.split(r"Register\(new MonsterDefinition", text)[1:]:
            block = block.split("});", 1)[0]
            if "Rank =" in block or "Immortal" in block or "SummonOnly" in block:
                continue

            def num(name, default=0):
                m = re.search(r"\b" + name + r" = (\d+)", block)
                return int(m.group(1)) if m else default

            if num("Level"):
                rows.append((num("Level"), num("MaxHp"), (num("AtkMin") + num("AtkMax")) / 2, num("Def"), num("Mdef"),
                             num("Hit"), num("Flee"), num("BaseExp"), num("JobExp")))
    return rows


# ---------------------------------------------------------------------------------------------- spawns
def read_spawns(npc_root):
    spawns = collections.defaultdict(list)  # ro map -> [(mob id, count, boss, delay ms)]
    for path in glob.glob(os.path.join(npc_root, "pre-re", "mobs", "*", "*.txt")):
        for line in open(path, encoding="utf-8", errors="replace"):
            if line.startswith("//"):
                continue
            parts = line.rstrip("\n").split("\t")
            if len(parts) >= 4 and parts[1] in ("monster", "boss_monster"):
                ids = parts[3].split(",")
                try:
                    delay = int(ids[2]) if len(ids) > 2 and ids[2].strip() else 0
                    spawns[parts[0].split(",")[0]].append((int(ids[0]), int(ids[1]), parts[1] == "boss_monster", delay))
                except ValueError:
                    pass
    return spawns


def read_summons(server_db):
    """Monsters that other monsters call (NPC_SUMMONSLAVE / NPC_CALLSLAVE...) from mob_skill_db.txt."""
    path = os.path.join(server_db, "mob_skill_db.txt")
    slaves = collections.defaultdict(set)
    if not os.path.exists(path):
        return slaves
    for line in open(path, encoding="utf-8", errors="replace"):
        if line.startswith("//"):
            continue
        parts = line.split(",")
        if len(parts) > 12 and "SUMMON" in parts[1].upper():
            try:
                owner = int(parts[0])
            except ValueError:
                continue
            for value in parts[12:17]:
                value = value.strip()
                if value.isdigit() and int(value) > 1000:
                    slaves[owner].add(int(value))
    return slaves


# ---------------------------------------------------------------------------------------------- monsters
def pick(options, key):
    return options[hashlib.md5(str(key).encode()).digest()[0] % len(options)]


# Creatures whose names mislead the patterns (Ragnarok's Moonlight Flower is a fox-woman, not a flower).
SHAPE_OVERRIDES = {"moonlight flower": "Humanoid"}


def shape_of(name, race):
    n = name.lower()
    for key, shape in SHAPE_OVERRIDES.items():
        if key in n:
            return shape
    for pattern, shape in ((SLIME, "Slime"), (PLANT, "Plant"), (BLOB, "Blob"), (WRAITH, "Wraith"), (FLYER, "Flyer"), (SERPENT, "Serpent"),
                           (GOLEM, "Golem"), (INSECT, "Insect"), (HUMANOID, "Humanoid"), (BIRD, "Bird"), (FISH, "Fish"),
                           (QUADRUPED, "Quadruped")):
        if re.search(pattern, n):
            return shape
    return {"Plant": "Plant", "Formless": "Golem", "Undead": "Humanoid", "Beast": "Quadruped", "Insect": "Insect", "Fish": "Fish",
            "Demon": "Biped", "DemiHuman": "Humanoid", "Angel": "Flyer", "Dragon": "Serpent"}.get(race, "Biped")


def variant_of(name):
    n = name.lower()
    return next((variant for pattern, variant in VARIANTS if re.search(pattern, n)), None)


def color_of(element, ro_id):
    base = ELEMENT_COLORS[element]
    r, g, b = (int(base[i:i + 2], 16) for i in (1, 3, 5))
    jitter = hashlib.md5(str(ro_id).encode()).digest()
    r, g, b = (max(0, min(255, c + (jitter[i] % 41) - 20)) for i, c in enumerate((r, g, b)))
    return f"#{r:02X}{g:02X}{b:02X}"


class Monster:
    def __init__(self, mob):
        self.mob = mob
        self.ro_id = int(mob["Id"])
        self.ro_name = str(mob["Name"])
        self.name = creatures_norse.creature_name(self.ro_name)
        self.race = RACES.get(str(mob.get("Race")), "Formless")
        self.element = ELEMENTS.get(str(mob.get("Element")), "Neutral")
        self.size = str(mob.get("Size") or "Medium")
        self.level = int(mob.get("Level") or 1)
        self.mvp = bool(mob.get("MvpExp"))
        self.boss = str(mob.get("Class")) == "Boss" or self.mvp

    @property
    def id(self):
        return f"xrm{self.ro_id}"


def disambiguate(monsters, taken):
    by_name = collections.defaultdict(list)
    for m in monsters:
        by_name[m.name].append(m)
    for name, group in by_name.items():
        if len(group) == 1 and name not in taken:
            continue
        elements = {m.element for m in group}
        for m in group:
            if name in taken or len(elements) == len(group):
                m.name = f"{ELEMENT_ADJ[m.element]} {name}" if not name.startswith(ELEMENT_ADJ[m.element]) else name
    seen = collections.Counter()
    used = set(taken)
    for m in sorted(monsters, key=lambda m: (m.level, m.ro_id)):
        base = m.name
        while m.name in used:
            seen[base] += 1
            m.name = f"{base} {['', 'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII', 'IX', 'X'][min(seen[base], 9)]}".strip()
            if seen[base] >= 9:
                m.name = f"{base} {seen[base] + 1}"
        used.add(m.name)


def build_monster(m, ctx):
    mob = m.mob
    ref_ro, ref_rh = ctx["ro_curves"], ctx["rh_curves"]
    L, L2 = m.level, runeheir_level(m.level)

    rank = "Mvp" if m.mvp else "MiniBoss" if m.boss else None

    def scaled(stat, ro_value, floor=1):
        ratio = ro_value / max(1e-9, ref_ro[stat].at(L)) if ro_value > 0 else 0.0
        if ratio <= 0:
            return 0
        if rank:
            # Bosses: Runeheir's boss multiple of a normal monster, nudged by how strong it was in Ragnarok.
            nudge = max(0.7, min(1.4, (ratio / (40.0 if stat in ("hp", "bexp", "jexp") else 4.0)) ** 0.15))
            return max(floor, round(ref_rh[stat].at(L2) * BOSS_MULTIPLES[rank][stat] * nudge))
        # Normal monsters stay within a quarter and three times their level's curve: tougher than that is a boss's job.
        return max(floor, round(ref_rh[stat].at(L2) * min(3.0, max(0.25, soften(ratio, 0.45 if stat == "atk" else 0.6)))))

    hp = scaled("hp", float(mob.get("Hp") or 1))
    atk = scaled("atk", (float(mob.get("Attack") or 1) + float(mob.get("Attack2") or mob.get("Attack") or 1)) / 2)
    defense = min(300, scaled("def", float(mob.get("Defense") or 0), 0))
    mdef = min(150, scaled("mdef", float(mob.get("MagicDefense") or 0), 0))
    hit = max(5, scaled("hit", L + float(mob.get("Dex") or 1)))
    flee = max(1, scaled("flee", L + float(mob.get("Agi") or 1)))
    base_exp = scaled("bexp", float(mob.get("BaseExp") or 0), 0)
    job_exp = scaled("jexp", float(mob.get("JobExp") or 0), 0)
    walk = float(mob.get("WalkSpeed") or 200)
    speed = max(0.8, min(6.0, 3.0 * 200.0 / max(50.0, walk)))
    reach = int(mob.get("AttackRange") or 1)
    attack_range = 0.9 if reach <= 2 else min(12.0, reach * 0.9)
    interval = max(0.6, min(3.0, float(mob.get("AttackDelay") or 1400) / 1000.0))
    ai = f"{int(mob.get('Ai') or 6):02d}" if str(mob.get("Ai") or "").isdigit() else str(mob.get("Ai") or "06")
    modes = mob.get("Modes") or {}
    aggressive = ai in AGGRESSIVE_AI or bool(modes.get("Aggressive"))
    passive = ai == "06" and not aggressive
    still = ai in STILL_AI or modes.get("CanMove") is False
    shape = shape_of(m.name + " " + m.ro_name, m.race)
    scale = {"Small": 0.7, "Medium": 1.0, "Large": 1.45}.get(m.size, 1.0) * (1.35 if m.mvp else 1.15 if m.boss else 1.0)

    fields = [f'Id = "{m.id}", RoId = {m.ro_id}, Name = {st.cs_string(m.name)}, Level = {L2}, MaxHp = {hp}, AtkMin = {round(atk * 0.85)}, AtkMax = {round(atk * 1.15)}']
    if m.mvp:
        fields[0] = fields[0].replace("Level =", "Rank = MonsterRank.Mvp, Level =")
    elif m.boss:
        fields[0] = fields[0].replace("Level =", "Rank = MonsterRank.MiniBoss, Level =")
    fields.append(f"Def = {defense}, Mdef = {mdef}, Hit = {hit}, Flee = {flee}")
    fields.append(f"Element = Element.{m.element}, Race = Race.{m.race}, Size = Size.{m.size}")
    fields.append(f"BaseExp = {base_exp}, JobExp = {job_exp}")
    extra = [f"MoveSpeed = {st._float(round(speed, 2))}", f"AttackInterval = {st._float(round(interval, 2))}"]
    if attack_range > 0.9:
        extra.append(f"AttackRange = {st._float(round(attack_range, 1))}")
    if aggressive:
        extra.append("Aggressive = true")
    if ai in ASSIST_AI:
        extra.append("Assist = true")
    if passive:
        extra.append("Passive = true")
    if still:
        extra.append("Stationary = true")
    if m.ro_id not in ctx["spawned"]:
        extra.append("SummonOnly = true")  # only ever called by another monster
    fields.append(", ".join(extra))

    skills = monster_skills.convert(m.ro_id, ctx["mob_skills"].get(m.ro_id, []), ctx["monster_ids"], m.boss)
    phases = []
    if rank:
        if not aggressive:
            fields.append("Aggressive = true")
        extra_skills, phases = monster_skills.boss_extras(m.ro_id, m.element, skills, ctx["helpers"].get(m.ro_id), rank)
        skills += extra_skills
    if skills:
        fields.append("Skills =\n                {\n" + "".join(f"                    {s},\n" for s in skills) + "                }")
    if phases:
        fields.append("Phases = { " + ", ".join(phases) + " }")
    if m.mvp:
        fields.append(f"MvpExp = {base_exp // 3}")

    drops = []
    for d in mob.get("Drops") or []:
        target = ctx["resolve_aegis"](str(d.get("Item")))
        if target:
            drops.append(f'new DropEntry("{target}", {st._float(max(0.01, int(d.get("Rate") or 1) / 100))})')
    if drops:
        fields.append("Drops = { " + ", ".join(drops) + " }")
    mvp_drops = []
    for d in mob.get("MvpDrops") or []:
        target = ctx["resolve_aegis"](str(d.get("Item")))
        if target:
            mvp_drops.append(f'new DropEntry("{target}", {st._float(max(0.01, int(d.get("Rate") or 1) / 100))})')
    if mvp_drops and m.mvp:
        fields.append("MvpDrops = { " + ", ".join(mvp_drops) + " }")

    fields.append(f'Shape = MonsterShape.{shape}, ColorHex = "{color_of(m.element, m.ro_id)}", Scale = {st._float(round(scale, 2))}')
    variant = variant_of(m.name + " " + m.ro_name)
    if variant and shape in ("Quadruped", "Insect", "Plant", "Slime", "Bird", "Fish"):
        fields.append(f'Variant = "{variant}"')
    if shape == "Humanoid":
        weapon = next((w for pattern, w in LOOK_WEAPONS if re.search(pattern, (m.name + " " + m.ro_name).lower())), "OneHandSword")
        fields.append(f'LookWeapon = WeaponType.{weapon}, SkinHex = "{pick(["#B8C4C9", "#C9B8A8", "#8FA08A", "#A8A0B8", "#D8C3A5"], m.ro_id)}"')
    return fields


# ---------------------------------------------------------------------------------------------- cards
CARD_TARGETS = {"Right_Hand": "Weapon", "Both_Hand": "Weapon", "Armor": "Armor", "Left_Hand": "Shield", "Garment": "Garment",
                "Shoes": "Footgear", "Head_Top": "Headgear", "Head_Mid": "Headgear", "Head_Low": "Headgear",
                "Right_Accessory": "Accessory", "Left_Accessory": "Accessory", "Both_Accessory": "Accessory"}


def card_name(ro_name):
    base = re.sub(r"\s+Card$", "", ro_name.strip())
    return creatures_norse.creature_name(base) + " Card"


def build_card(ro_id, entry, ctx):
    locations = list((entry.get("Locations") or {}).keys())
    target = next((CARD_TARGETS[l] for l in locations if l in CARD_TARGETS), "Weapon")
    result = st.translate(entry.get("Script"), False)
    namer = st.BuffNamer(ro_id, ctx["card_names"][ro_id])
    effect = st.cs_effect(result["base"], buffs=namer) or "new EquipEffect()"
    rules = st.describe(result) or "A faint echo of its owner."
    rank = ctx["card_rank"].get(ro_id, "Normal")
    price = {"Mvp": 2000000, "MiniBoss": 400000}.get(rank, 20000)
    color = "#F1C40F" if rank == "Mvp" else "#E67E22" if rank == "MiniBoss" else "#BB8FCE"
    label = {"Weapon": "Weapon", "Armor": "Armor", "Shield": "Shield", "Garment": "Garment", "Footgear": "Footgear",
             "Headgear": "Headgear", "Accessory": "Accessory"}[target]
    return [f'Id = "xr{ro_id}", RoId = {ro_id}, Name = {st.cs_string(ctx["card_names"][ro_id])}, Kind = ItemKind.Card',
            f"Description = {st.cs_string(f'[{label}] {rules}')}",
            f"CardTarget = EquipKind.{target}, Weight = 1, Price = {price}",
            f'IconLabel = "CRD", IconColorHex = "{color}"', f"Effect = {effect}"]
