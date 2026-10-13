"""rAthena item scripts -> Runeheir equipment effects.

A small interpreter for the item-script subset rAthena's databases use (bonus/bonus2..5, skill, autobonus, if/else,
.@variables, getrefine(), readparam(), max/min...). Scripts are run once per refine level (+0..+MAX_REFINE); comparing
the results splits every bonus into a base value, a per-refine step (linear formulas) and refine thresholds (anything
else), which map onto ItemDefinition.Effect / PerRefine / RefineBonuses. Conditions that can't be known when the item is
built (a stat check, the wearer's job, a learned skill) become an EquipCondition when Runeheir has one (a base stat
threshold) and a tooltip note otherwise. Bonuses Runeheir has no system for (auto-spells, drop rates...) become notes.
"""

import collections
import re

import skills_norse

# Ragnarok's refine range: scripts are only ever written for +0..+10. Past +10 an item keeps its +10 bonuses
# (Runeheir's own refine ATK/DEF keeps growing to +20), so formulas like (refine-3)^2 aren't extrapolated.
MAX_REFINE = 10

# ---------------------------------------------------------------------------------------------- enums
ELEMENTS = {
    "Ele_Neutral": "Neutral", "Ele_Water": "Water", "Ele_Earth": "Earth", "Ele_Fire": "Fire", "Ele_Wind": "Wind",
    "Ele_Poison": "Poison", "Ele_Holy": "Holy", "Ele_Dark": "Shadow", "Ele_Ghost": "Ghost", "Ele_Undead": "Undead",
}
RACES = {
    "RC_Formless": "Formless", "RC_Undead": "Undead", "RC_Brute": "Beast", "RC_Plant": "Plant", "RC_Insect": "Insect",
    "RC_Fish": "Fish", "RC_Demon": "Demon", "RC_DemiHuman": "DemiHuman", "RC_Angel": "Angel", "RC_Dragon": "Dragon",
}
RACE_LABEL = {"DemiHuman": "Demi-Humans", "Formless": "Formless", "Undead": "Undead", "Beast": "Beasts", "Plant": "Plants",
              "Insect": "Insects", "Fish": "Fish", "Demon": "Demons", "Angel": "Angels", "Dragon": "Dragons"}
# Renewal's player races duplicate Demi-Human in these scripts; Runeheir's players are Demi-Human.
PLAYER_RACES = {"RC_Player_Human", "RC_Player_Doram", "RC_Player"}
SIZES = {"Size_Small": "Small", "Size_Medium": "Medium", "Size_Large": "Large"}
STATUSES = {
    "Eff_Stun": "Stun", "Eff_Freeze": "Freeze", "Eff_Stone": "StoneCurse", "Eff_Sleep": "Sleep", "Eff_Poison": "Poison",
    "Eff_DPoison": "Poison", "Eff_Bleeding": "Bleeding", "Eff_Silence": "Silence", "Eff_Blind": "Blind", "Eff_Curse": "Curse",
}
STATUS_LABEL = {"StoneCurse": "Stone Curse"}
STATUS_DURATION = {"Stun": 3, "Freeze": 5, "StoneCurse": 5, "Sleep": 6, "Poison": 10, "Bleeding": 10, "Silence": 8,
                   "Blind": 10, "Curse": 10}
STATS = {"bstr": "Str", "bagi": "Agi", "bvit": "Vit", "bint": "Int", "bdex": "Dex", "bluk": "Luk"}
RO_JOBS = {
    "Job_Novice": "Initiate", "Job_Swordman": "Warrior", "Job_Mage": "Mystic", "Job_Archer": "Huntsman", "Job_Acolyte": "Devotee",
    "Job_Merchant": "Trader", "Job_Thief": "Scout", "Job_Knight": "Berserker", "Job_Priest": "Gothi", "Job_Wizard": "Sorcerer",
    "Job_Blacksmith": "Runesmith", "Job_Hunter": "Ranger", "Job_Assassin": "Assassin", "Job_Crusader": "Guardian", "Job_Monk": "Monk",
    "Job_Sage": "Sage", "Job_Rogue": "Outlaw", "Job_Alchemist": "Brewmaster", "Job_Bard": "Skald", "Job_Dancer": "Seidkona",
    "Job_Taekwon": "Glíma Fighter", "Job_Star_Gladiator": "Sól Guardian", "Job_Soul_Linker": "Fylgja Caller", "Job_Gunslinger": "Thunderer",
    "Job_Ninja": "Nightraider", "Job_SuperNovice": "Wanderer", "Job_Super_Novice": "Wanderer", "Job_Lord_Knight": "Einherjar",
    "Job_High_Priest": "High Gothi", "Job_High_Wizard": "Archmage", "Job_Whitesmith": "Forgelord", "Job_Sniper": "Deadeye",
    "Job_Assassin_Cross": "Shadow Walker", "Job_Paladin": "Valkyrie", "Job_Champion": "Champion", "Job_Professor": "Chronomancer",
    "Job_Stalker": "Vargr", "Job_Creator": "Lifeweaver", "Job_Clown": "Thul", "Job_Gypsy": "Völva",
}

# Skill names for notes; Phase 8.6 (the skills audit) maps them to Runeheir's skills. Filled from skill_db by the importer.
SKILL_NAMES = {}


# Runeheir skill id -> name, filled by the importer from the skill catalog.
RUNEHEIR_SKILL_NAMES = {}


def skill_label(aegis):
    """A Ragnarok skill's name as Runeheir players know it: the Runeheir skill, a Norse label, or Ragnarok's English name."""
    aegis = str(aegis).strip('"')
    mapped = skills_norse.runeheir_skill(aegis)
    if mapped and mapped in RUNEHEIR_SKILL_NAMES:
        return RUNEHEIR_SKILL_NAMES[mapped]
    if aegis in skills_norse.LABELS:
        return skills_norse.LABELS[aegis]
    if aegis in SKILL_NAMES:
        return SKILL_NAMES[aegis]
    return aegis.split("_", 1)[-1].replace("_", " ").title()


# ---------------------------------------------------------------------------------------------- tokens and parsing
_TOKEN = re.compile(r"""
    (?P<ws>\s+|//[^\n]*|/\*.*?\*/)
  | (?P<str>"(?:[^"\\]|\\.)*")
  | (?P<num>0x[0-9A-Fa-f]+|\d+)
  | (?P<id>(?:\.@|\.|@|\$@|\$|'|\#\#|\#)?[A-Za-z_][A-Za-z0-9_]*\$?)
  | (?P<op>==|!=|>=|<=|&&|\|\||<<|>>|\+=|-=|\*=|/=|\+\+|--|[-+*/%<>!~&|^?:;,(){}=\[\]])
""", re.S | re.X)


class ScriptError(Exception):
    pass


def tokenize(text):
    out = []
    pos = 0
    while pos < len(text):
        m = _TOKEN.match(text, pos)
        if not m:
            raise ScriptError(f"bad character {text[pos]!r}")
        pos = m.end()
        kind = m.lastgroup
        if kind == "ws":
            continue
        value = m.group(kind)
        if kind == "num":
            value = int(value, 16) if value.lower().startswith("0x") else int(value)
        out.append((kind, value))
    return out


class _Parser:
    def __init__(self, tokens):
        self.t = tokens
        self.i = 0

    def peek(self, k=0):
        return self.t[self.i + k] if self.i + k < len(self.t) else (None, None)

    def take(self, value=None):
        tok = self.peek()
        if value is not None and tok[1] != value:
            raise ScriptError(f"expected {value!r}, got {tok[1]!r}")
        self.i += 1
        return tok

    def at(self, value):
        return self.peek()[1] == value and self.peek()[0] in ("op", "id")

    # statements
    def block(self):
        stmts = []
        while self.peek()[0] is not None and not self.at("}"):
            stmts.append(self.statement())
        return ("block", stmts)

    def statement(self):
        kind, value = self.peek()
        if self.at("{"):
            self.take("{")
            body = self.block()
            self.take("}")
            return body
        if self.at(";"):
            self.take()
            return ("block", [])
        if kind == "id" and value == "if":
            self.take()
            self.take("(")
            cond = self.expr()
            self.take(")")
            then = self.statement()
            other = None
            if self.peek()[1] == "else":
                self.take()
                other = self.statement()
            return ("if", cond, then, other)
        if kind == "id" and value in ("for", "while", "switch"):
            start = self.i
            self.take()
            self._skip_parens()
            self.statement()
            return ("unsupported", value)
        if kind == "id" and value in ("end", "return", "break"):
            self.take()
            if not self.at(";"):
                self.expr()
            self.take(";")
            return ("block", [])
        if kind == "id" and value == "set":
            self.take()
            name = self.take()[1]
            self.take(",")
            e = self.expr()
            self.take(";")
            return ("assign", name, "=", e)
        if kind == "id" and self.peek(1)[1] in ("=", "+=", "-=", "*=", "/="):
            name = self.take()[1]
            op = self.take()[1]
            e = self.expr()
            self.take(";")
            return ("assign", name, op, e)
        if kind == "id" and self.peek(1)[1] in ("++", "--"):
            name = self.take()[1]
            op = self.take()[1]
            self.take(";")
            return ("assign", name, "+=" if op == "++" else "-=", ("num", 1))
        if kind == "id":
            name = self.take()[1]
            args = []
            if self.at("("):
                # call-style command: name(args);
                self.take("(")
                if not self.at(")"):
                    args.append(self.expr())
                    while self.at(","):
                        self.take()
                        args.append(self.expr())
                self.take(")")
            elif not self.at(";"):
                args.append(self.expr())
                while self.at(","):
                    self.take()
                    args.append(self.expr())
            self.take(";")
            return ("cmd", name, args)
        raise ScriptError(f"unexpected {value!r}")

    def _skip_parens(self):
        depth = 0
        while True:
            tok = self.take()
            if tok[1] == "(":
                depth += 1
            elif tok[1] == ")":
                depth -= 1
                if depth == 0:
                    return
            if tok[0] is None:
                raise ScriptError("unbalanced parentheses")

    # expressions (C precedence)
    _LEVELS = [["||"], ["&&"], ["|"], ["^"], ["&"], ["==", "!="], ["<", ">", "<=", ">="], ["<<", ">>"], ["+", "-"], ["*", "/", "%"]]

    def expr(self):
        cond = self.binary(0)
        if self.at("?"):
            self.take()
            a = self.expr()
            self.take(":")
            b = self.expr()
            return ("tern", cond, a, b)
        return cond

    def binary(self, level):
        if level == len(self._LEVELS):
            return self.unary()
        left = self.binary(level + 1)
        while self.peek()[0] == "op" and self.peek()[1] in self._LEVELS[level]:
            op = self.take()[1]
            right = self.binary(level + 1)
            left = ("bin", op, left, right)
        return left

    def unary(self):
        if self.peek()[0] == "op" and self.peek()[1] in ("-", "!", "~", "+"):
            op = self.take()[1]
            return ("un", op, self.unary())
        return self.primary()

    def primary(self):
        kind, value = self.take()
        if kind == "num":
            return ("num", value)
        if kind == "str":
            return ("str", value[1:-1])
        if kind == "op" and value == "(":
            e = self.expr()
            self.take(")")
            return e
        if kind == "id":
            if self.at("("):
                self.take("(")
                args = []
                if not self.at(")"):
                    args.append(self.expr())
                    while self.at(","):
                        self.take()
                        args.append(self.expr())
                self.take(")")
                return ("call", value, args)
            if self.at("["):
                self.take("[")
                self.expr()
                self.take("]")
            return ("id", value)
        raise ScriptError(f"unexpected {value!r} in expression")


def parse(text):
    p = _Parser(tokenize(text or ""))
    tree = p.block()
    if p.peek()[0] is not None:
        raise ScriptError(f"unexpected {p.peek()[1]!r}")
    return tree


# ---------------------------------------------------------------------------------------------- evaluation
class Sym:
    """A value the importer can't know (it depends on the wearer), carried as English text. `stat` marks readparam()."""

    def __init__(self, text, stat=None, threshold=None):
        self.text = text
        self.stat = stat
        self.threshold = threshold

    def __repr__(self):
        return self.text


class Rand(Sym):
    """rand(a, b): a random value in [low, high], kept as a range (heals, zeny pouches). Unknown to gear scripts."""

    def __init__(self, low, high):
        super().__init__(f"{min(low, high)}~{max(low, high)}")
        self.low = min(low, high)
        self.high = max(low, high)


def _text(v):
    if isinstance(v, Sym):
        return v.text
    return str(v)


def _c_div(a, b):
    if b == 0:
        return 0
    q = abs(a) // abs(b)
    return q if (a >= 0) == (b >= 0) else -q


_STAT_PARAM = {"bStr": "Str", "bAgi": "Agi", "bVit": "Vit", "bInt": "Int", "bDex": "Dex", "bLuk": "Luk"}


def evaluate(e, env):
    kind = e[0]
    if kind == "num":
        return e[1]
    if kind == "str":
        return e[1]
    if kind == "id":
        name = e[1]
        if name.startswith((".@", "@", ".", "$")):
            return env.get(name, 0)
        if name in ("BaseLevel", "JobLevel"):
            return Sym("Base Level" if name == "BaseLevel" else "Job Level")
        if name in ("Class", "BaseJob", "BaseClass", "Upper", "Sex"):
            return Sym({"Sex": "gender"}.get(name, "job"))
        if name in ("true", "false"):
            return 1 if name == "true" else 0
        return name  # a constant: RC_Brute, Ele_Fire, bStr, Job_Sage...
    if kind == "call":
        name, args = e[1], e[2]
        if name == "getrefine":
            return env["refine"]
        values = [evaluate(a, env) for a in args]
        if name == "rand" and values and all(isinstance(v, int) for v in values):
            return Rand(values[0], values[1]) if len(values) > 1 else Rand(0, values[0] - 1)
        if name in ("max", "min") and values and all(isinstance(v, int) for v in values):
            return max(values) if name == "max" else min(values)
        if name in ("max", "min"):
            known = [v for v in values if isinstance(v, int)]
            if not known:
                return Sym(f"{name}({', '.join(map(_text, values))})")
            return Sym(f"{max(known)}+" if name == "max" else f"up to {min(known)}")
        if name == "readparam" and values and values[0] in _STAT_PARAM:
            return Sym(_STAT_PARAM[values[0]].upper(), stat=_STAT_PARAM[values[0]])
        if name == "getskilllv" and values:
            return Sym(f"{skill_label(values[0])} level")
        if name == "eaclass":
            return Sym("job")
        if name == "pow" and len(values) == 2 and all(isinstance(v, int) for v in values):
            return values[0] ** values[1]
        return Sym(f"{name}()")
    if kind == "un":
        v = evaluate(e[2], env)
        if isinstance(v, int):
            return {"-": -v, "+": v, "!": int(not v), "~": ~v}[e[1]]
        if isinstance(v, Sym):
            return Sym(("not " if e[1] == "!" else e[1]) + v.text)
        return v
    if kind == "tern":
        c = evaluate(e[1], env)
        if isinstance(c, int):
            return evaluate(e[2] if c else e[3], env)
        return Sym(f"({_text(evaluate(e[2], env))} if {_text(c)} else {_text(evaluate(e[3], env))})")
    if kind == "bin":
        op = e[1]
        a = evaluate(e[2], env)
        if op in ("&&", "||"):
            if isinstance(a, int):
                if (op == "&&" and not a) or (op == "||" and a):
                    return int(bool(a))
                b = evaluate(e[3], env)
                return int(bool(b)) if isinstance(b, int) else b
            b = evaluate(e[3], env)
            if isinstance(b, int):
                if (op == "&&" and not b) or (op == "||" and b):
                    return int(bool(b))
                return a
            return Sym(f"{_text(a)} {'and' if op == '&&' else 'or'} {_text(b)}")
        b = evaluate(e[3], env)
        if isinstance(a, Rand) and isinstance(b, int) and op in ("+", "-", "*", "/"):
            f = {"+": lambda x: x + b, "-": lambda x: x - b, "*": lambda x: x * b, "/": lambda x: _c_div(x, b)}[op]
            return Rand(f(a.low), f(a.high))
        if isinstance(b, Rand) and isinstance(a, int) and op in ("+", "*"):
            f = {"+": lambda x: a + x, "*": lambda x: a * x}[op]
            return Rand(f(b.low), f(b.high))
        if isinstance(a, int) and isinstance(b, int):
            return {
                "+": lambda: a + b, "-": lambda: a - b, "*": lambda: a * b, "/": lambda: _c_div(a, b),
                "%": lambda: a - b * _c_div(a, b) if b else 0, "<<": lambda: a << b, ">>": lambda: a >> b,
                "&": lambda: a & b, "|": lambda: a | b, "^": lambda: a ^ b,
                "==": lambda: int(a == b), "!=": lambda: int(a != b), "<": lambda: int(a < b), ">": lambda: int(a > b),
                "<=": lambda: int(a <= b), ">=": lambda: int(a >= b),
            }[op]()
        if isinstance(a, str) and isinstance(b, str) and op == "|":
            return f"{a}|{b}"
        if isinstance(a, str) and isinstance(b, str) and op in ("==", "!="):
            return int((a == b) == (op == "=="))
        # comparisons against the wearer
        if isinstance(a, Sym) and isinstance(b, int) and op in (">", ">="):
            need = b + 1 if op == ">" else b
            if a.stat:
                return Sym(f"base {a.text} {need}+", stat=a.stat, threshold=need)
            return Sym(f"{a.text} {need}+")
        if isinstance(a, Sym) and isinstance(b, int) and op == "==":
            return Sym(f"{a.text} {b}")
        if isinstance(a, Sym) and isinstance(b, str) and b in RO_JOBS and op in ("==", "!="):
            return Sym(f"{'as' if op == '==' else 'not as'} a {RO_JOBS[b]}")
        return Sym(f"{_text(a)} {op} {_text(b)}")
    raise ScriptError(f"bad node {kind}")


def run(tree, refine):
    """Executes a script at one refine level. Returns [(guard tuple of Sym, command, [evaluated args])]."""
    out = []
    env = {"refine": refine}

    def execute(node, guard):
        kind = node[0]
        if kind == "block":
            for s in node[1]:
                execute(s, guard)
        elif kind == "if":
            c = evaluate(node[1], env)
            if isinstance(c, Sym):
                execute(node[2], guard + (c,))
                if node[3] is not None:
                    execute(node[3], guard + (Sym(f"not {c.text}"),))
            elif c:
                execute(node[2], guard)
            elif node[3] is not None:
                execute(node[3], guard)
        elif kind == "assign":
            v = evaluate(node[3], env)
            name, op = node[1], node[2]
            old = env.get(name, 0)
            if op == "=":
                env[name] = v
            elif isinstance(old, int) and isinstance(v, int):
                env[name] = {"+=": old + v, "-=": old - v, "*=": old * v, "/=": _c_div(old, v)}[op]
            else:
                env[name] = Sym(f"{_text(old)} {op[0]} {_text(v)}")
        elif kind == "cmd":
            out.append((guard, node[1], [evaluate(a, env) for a in node[2]]))
        elif kind == "unsupported":
            out.append((guard, "unsupported", [node[1]]))

    execute(tree, ())
    return out


# ---------------------------------------------------------------------------------------------- effects
class Fx:
    """One EquipEffect under construction, with its English description."""

    INT_FIELDS = {"Atk", "Matk", "Def", "Mdef", "Hit", "Flee", "MaxHp", "MaxSp", "HpRegenFlat", "SpRegenFlat", "SpDrainOnHit",
                  "WeightCapacity"}

    def __init__(self):
        self.mods = {}
        self.stats = {}
        self.all_stats = 0
        self.flags = set()
        self.calls = {}  # (method, arg) -> value, in insertion order
        self.unbreakable = False
        self.armor_element = None
        self.weapon_element = None
        self.immune = []
        self.procs = []
        self.notes = []
        self.desc = []
        self.condition = None  # (stat, minimum, Fx)
        self.autocasts = []  # (trigger, skill id, level, chance %, trigger skill id)
        self.autobuffs = []  # (trigger, Fx, seconds, chance %, trigger skill id, description)
        self.grants = {}
        self.skill_bonus = {}

    def empty(self):
        return not (self.mods or self.stats or self.all_stats or self.flags or self.calls or self.unbreakable or self.armor_element
                    or self.weapon_element or self.immune or self.procs or self.notes or self.condition or self.autocasts
                    or self.autobuffs or self.grants or self.skill_bonus)

    def mod(self, field, value):
        self.mods[field] = self.mods.get(field, 0) + value

    def call(self, method, arg, value):
        key = (method, arg)
        self.calls[key] = self.calls.get(key, 0) + value


def _num(v):
    return f"{v:+g}" if isinstance(v, (int, float)) else f"+({_text(v)})"


def _pct(v):
    return f"{v:+g}%" if isinstance(v, (int, float)) else f"+({_text(v)})%"


def _div(v, d):
    """v / d for display; a wearer-dependent value is shown as its formula."""
    return f"{v / d:g}" if isinstance(v, (int, float)) else f"({_text(v)})/{d}"


def _less_more(v, what):
    """Ragnarok's bSub* bonuses: positive = take less damage."""
    return f"takes {abs(v):g}% {'less' if v > 0 else 'more'} {what}"


def _race_targets(arg):
    if arg == "RC_All":
        return list(RACES.values())
    return [RACES[arg]] if arg in RACES else []


def _element_targets(arg):
    if arg == "Ele_All":
        return list(ELEMENTS.values())
    return [ELEMENTS[arg]] if arg in ELEMENTS else []


def _size_targets(arg):
    if arg == "Size_All":
        return list(SIZES.values())
    return [SIZES[arg]] if arg in SIZES else []


def _race_label(arg):
    return "all races" if arg == "RC_All" else RACE_LABEL.get(RACES.get(arg, ""), arg)


def _element_label(arg):
    return "every element" if arg == "Ele_All" else ELEMENTS.get(arg, arg)


def _size_label(arg):
    return "every size" if arg == "Size_All" else SIZES.get(arg, arg).lower()


# Simple StatModifiers bonuses: name -> (field, label, percent?, scale).
SIMPLE = {
    "bmaxhp": ("MaxHp", "Max HP", False, 1), "bmaxhprate": ("MaxHpPercent", "Max HP", True, 1),
    "bmaxsp": ("MaxSp", "Max SP", False, 1), "bmaxsprate": ("MaxSpPercent", "Max SP", True, 1),
    "bbaseatk": ("Atk", "ATK", False, 1), "batk": ("Atk", "ATK", False, 1), "batkrate": ("PhysicalDamagePercent", "physical damage", True, 1),
    "bmatk": ("Matk", "MATK", False, 1), "bmatkrate": ("MagicDamagePercent", "MATK", True, 1),
    # Runeheir's armor DEF runs on renewal's curve, so Ragnarok's pre-renewal DEF is worth ten times as much.
    "bdef": ("Def", "DEF", False, 10), "bdefrate": ("DefPercent", "DEF", True, 1), "bdef2rate": ("DefPercent", "DEF", True, 1),
    "bmdef": ("Mdef", "MDEF", False, 1), "bmdefrate": ("MdefPercent", "MDEF", True, 1), "bmdef2rate": ("MdefPercent", "MDEF", True, 1),
    "bhit": ("Hit", "HIT", False, 1), "bhitrate": ("HitPercent", "HIT", True, 1),
    "bflee": ("Flee", "FLEE", False, 1), "bfleerate": ("FleePercent", "FLEE", True, 1),
    "bcritical": ("Crit", "CRIT", False, 1), "bcritatkrate": ("CritDamagePercent", "critical damage", True, 1),
    "baspdrate": ("AspdPercent", "ASPD", True, 1), "baspd": ("AspdFlat", "ASPD", False, 1),
    "bcastrate": ("CastTimePercent", "cast time", True, 1), "bdelayrate": ("CooldownPercent", "skill cooldowns", True, 1),
    "bspeedrate": ("MoveSpeedPercent", "movement speed", True, 1), "bspeedaddrate": ("MoveSpeedPercent", "movement speed", True, 1),
    "bhprecovrate": ("HpRegenPercent", "HP regeneration", True, 1), "bsprecovrate": ("SpRegenPercent", "SP regeneration", True, 1),
    "busesprate": ("SpCostPercent", "SP cost of skills", True, 1), "bhealpower": ("HealPowerPercent", "healing power", True, 1),
    "blongatkrate": ("LongRangePercent", "ranged physical damage", True, 1),
    "bmagicdamagereturn": ("ReflectMagicPercent", "magic damage reflected", True, 1),
    "bshortweapondamagereturn": ("ReflectMeleePercent", "melee damage reflected", True, 1),
    "bspdrainvalue": ("SpDrainOnHit", "SP gained per hit", False, 1),
    "bflee2": ("PerfectDodge", "Perfect Dodge", False, 1),
}
# Unbreakable gear: Runeheir only ever breaks weapons, so the armor versions are always true.
UNBREAKABLE = {"bunbreakableweapon", "bunbreakablearmor", "bunbreakablehelm", "bunbreakableshield", "bunbreakableshoes",
               "bunbreakablegarment"}
NOTE_FLAGS = {
    "bnoknockback": "can't be knocked back", "bsplashrange": "splash damage around the target", "bnogemstone": "skills need no gemstones",
    "bintravision": "sees hidden enemies", "bnowalkdelay": "no flinch when hit", "brestartfullrecover": "revives with full HP and SP",
    "bnosizefix": "no size penalty", "bnoregen": "stops natural regeneration", "bclasschange": "may transform the target",
    "bperfecthitrate": "perfect hit chance", "bflee2": "Perfect Dodge", "bhpgainvalue": "HP gained per kill",
    "bspgainvalue": "SP gained per kill", "bmagichpgainvalue": "HP gained per magic kill", "bmagicspgainvalue": "SP gained per magic kill",
    "bbreakweaponrate": "chance to break the target's weapon (x0.01%)", "bbreakarmorrate": "chance to break the target's armor (x0.01%)",
    "bhealpower2": "healing received", "blongatkdef": "less damage from ranged attacks", "bcriticallong": "ranged CRIT",
    "bnomagicdamage": "less magic damage taken", "bnocastcancel": None, "bdefratioatkclass": "damage grows with the target's DEF",
    "bignoredefclass": None, "bunbreakable": None, "bmatkrate": None,
}


# Some scripts write the enum's number instead of its name (bSubSize,1 = Size_Medium).
_BY_NUMBER = {
    "race": list(RACES), "ele": list(ELEMENTS), "size": list(SIZES),
}
_ARG_KIND = {"baddrace": "race", "bsubrace": "race", "bignoredefracerate": "race", "bignoremdefracerate": "race",
             "bcriticaladdrace": "race", "bmagicaddrace": "race", "bignoredefrace": "race", "bignoremdefrace": "race",
             "baddele": "ele", "bsubele": "ele", "bmagicaddele": "ele", "batkele": "ele", "bdefele": "ele",
             "baddsize": "size", "bsubsize": "size"}


def _named_args(name, args):
    kind = _ARG_KIND.get(name.lower())
    if not kind or not args or not isinstance(args[0], int):
        return args
    names = _BY_NUMBER[kind]
    return [names[args[0]] if 0 <= args[0] < len(names) else str(args[0])] + list(args[1:])


def apply_bonus(fx, cmd, name, args, value, ctx):
    """Adds one bonus (value already a number) to fx. Unknown or unsupported bonuses become notes."""
    n = name.lower()
    args = _named_args(name, args)
    if cmd == "bonus":
        if n in STATS:
            fx.stats[STATS[n]] = fx.stats.get(STATS[n], 0) + value
            fx.desc.append(f"{STATS[n].upper()} {_num(value)}")
            return
        if n == "ballstats":
            fx.all_stats += value
            fx.desc.append(f"All stats {_num(value)}")
            return
        if n in SIMPLE:
            field, label, percent, scale = SIMPLE[n]
            fx.mod(field, value * scale)
            if field == "CastTimePercent":
                fx.desc.append(f"{abs(value):g}% {'faster' if value < 0 else 'slower'} casting")
            elif field == "CooldownPercent":
                fx.desc.append(f"{abs(value):g}% {'shorter' if value < 0 else 'longer'} skill cooldowns")
            elif field == "SpCostPercent":
                fx.desc.append(f"skills cost {abs(value):g}% {'less' if value < 0 else 'more'} SP")
            elif field in ("ReflectMagicPercent", "ReflectMeleePercent"):
                fx.desc.append(f"reflects {value:g}% of {'magic' if field == 'ReflectMagicPercent' else 'melee'} damage taken")
            elif field == "SpDrainOnHit":
                fx.desc.append(f"gain {value:g} SP per hit")
            else:
                fx.desc.append(f"{label} {_pct(value) if percent else _num(value * scale)}")
            return
        if n in UNBREAKABLE:
            if n == "bunbreakableweapon":
                fx.unbreakable = True
                fx.desc.append("can't break")
            return
        if n == "bnocastcancel":
            fx.flags.add("UninterruptibleCasting")
            fx.desc.append("casting can't be interrupted")
            return
        if n == "batkele" and args and args[0] in ELEMENTS:
            fx.weapon_element = ELEMENTS[args[0]]
            fx.desc.append(f"{fx.weapon_element} element attacks")
            return
        if n == "bdefele" and args and args[0] in ELEMENTS:
            fx.armor_element = ELEMENTS[args[0]]
            fx.desc.append(f"{fx.armor_element} element armor")
            return
        if n == "bignoredefrace" and args:
            for race in _race_targets(args[0]):
                fx.call("IgnoreDef", f"Race.{race}", 100)
            fx.desc.append(f"ignores the DEF of {_race_label(args[0])}")
            return
        if n == "bignoremdefrace" and args:
            for race in _race_targets(args[0]):
                fx.call("IgnoreMdef", f"Race.{race}", 100)
            fx.desc.append(f"ignores the MDEF of {_race_label(args[0])}")
            return
        if n == "bignoredefclass" and args and args[0] == "Class_All":
            fx.mod("DefBypassPercent", 100)
            fx.desc.append("ignores DEF")
            return
        if n in NOTE_FLAGS and NOTE_FLAGS[n]:
            fx.notes.append(NOTE_FLAGS[n] + (f" {_num(value)}" if value not in (0, 1) else ""))
            return
    if cmd == "bonus2" and args:
        a = args[0]
        if a in PLAYER_RACES:
            return
        if n == "baddrace":
            for race in _race_targets(a):
                fx.call("Vs", f"Race.{race}", value)
            fx.desc.append(f"{_pct(value)} damage vs {_race_label(a)}")
            return
        if n == "bsubrace":
            for race in _race_targets(a):
                fx.call("From", f"Race.{race}", -value)
            fx.desc.append(_less_more(value, f"damage from {_race_label(a)}"))
            return
        if n == "baddele":
            for el in _element_targets(a):
                fx.call("Vs", f"Element.{el}", value)
            fx.desc.append(f"{_pct(value)} damage vs {_element_label(a)} enemies")
            return
        if n == "bsubele":
            for el in _element_targets(a):
                fx.call("From", f"Element.{el}", -value)
            fx.desc.append(_less_more(value, f"{_element_label(a)} damage"))
            return
        if n == "baddsize":
            for size in _size_targets(a):
                fx.call("Vs", f"Size.{size}", value)
            fx.desc.append(f"{_pct(value)} damage vs {_size_label(a)} enemies")
            return
        if n == "bsubsize":
            for size in _size_targets(a):
                fx.call("From", f"Size.{size}", -value)
            fx.desc.append(_less_more(value, f"damage from {_size_label(a)} enemies"))
            return
        if n == "baddclass" and a in ("Class_Boss", "Class_Normal", "Class_All"):
            if a in ("Class_Boss", "Class_All"):
                fx.call("VsBosses", None, value)
            if a in ("Class_Normal", "Class_All"):
                fx.call("VsNormals", None, value)
            fx.desc.append(f"{_pct(value)} damage vs {'bosses' if a == 'Class_Boss' else 'normal monsters' if a == 'Class_Normal' else 'every enemy'}")
            return
        if n == "bsubclass" and a == "Class_All":
            fx.mod("DamageTakenPercent", -value)
            fx.desc.append(_less_more(value, "damage"))
            return
        if n == "bignoredefracerate":
            for race in _race_targets(a):
                fx.call("IgnoreDef", f"Race.{race}", value)
            fx.desc.append(f"ignores {value:g}% of the DEF of {_race_label(a)}")
            return
        if n == "bignoremdefracerate":
            for race in _race_targets(a):
                fx.call("IgnoreMdef", f"Race.{race}", value)
            fx.desc.append(f"ignores {value:g}% of the MDEF of {_race_label(a)}")
            return
        if n == "bignoremdefclassrate" and a == "Class_All":
            fx.mod("MdefBypassPercent", value)
            fx.desc.append(f"ignores {value:g}% of MDEF")
            return
        if n == "bcriticaladdrace":
            for race in _race_targets(a):
                fx.call("CritVs", f"Race.{race}", value)
            fx.desc.append(f"CRIT {_num(value)} vs {_race_label(a)}")
            return
        if n == "bmagicaddrace":
            for race in _race_targets(a):
                fx.call("MagicVs", f"Race.{race}", value)
            fx.desc.append(f"{_pct(value)} magic damage vs {_race_label(a)}")
            return
        if n == "bmagicaddele":
            for el in _element_targets(a):
                fx.call("MagicVs", f"Element.{el}", value)
            fx.desc.append(f"{_pct(value)} magic damage vs {_element_label(a)} enemies")
            return
        if n == "breseff" and a in STATUSES:
            status = STATUSES[a]
            if value >= 10000:
                fx.immune.append(status)
                fx.desc.append(f"immune to {STATUS_LABEL.get(status, status)}")
            else:
                fx.call("Resist", f"StatusEffect.{status}", value / 100)
                fx.desc.append(f"{value / 100:+g}% resistance to {STATUS_LABEL.get(status, status)}")
            return
        if n == "baddeff" and a in STATUSES:
            status = STATUSES[a]
            fx.procs.append((status, value / 100))
            fx.desc.append(f"{value / 100:g}% chance to inflict {STATUS_LABEL.get(status, status)} on hit")
            return
        if n in ("bhpdrainrate", "bspdrainrate") and len(args) >= 1 and isinstance(args[0], int):
            # rate in 0.1%, percent of damage: Runeheir steals the average every hit.
            average = args[0] * value / 1000
            field = "LifeStealPercent" if n == "bhpdrainrate" else "SpStealPercent"
            fx.mod(field, average)
            fx.desc.append(f"{average:g}% of physical damage dealt restored as {'HP' if n == 'bhpdrainrate' else 'SP'}")
            return
    if n.startswith("bautospell") and cmd in ("bonus3", "bonus4", "bonus5") and args and isinstance(value, (int, float)):
        onskill = n.endswith("onskill")
        cast_arg = args[1] if onskill and len(args) > 1 else args[0]
        level_arg = args[2] if onskill and len(args) > 2 else (args[1] if not onskill and len(args) > 1 else 1)
        skill = skills_norse.runeheir_skill(cast_arg)
        trigger_skill = skills_norse.runeheir_skill(args[0]) if onskill else None
        if skill and (trigger_skill or not onskill):
            trigger = "OnSkill" if onskill else "WhenHit" if "whenhit" in n else "OnAttack"
            level = level_arg if isinstance(level_arg, int) else 0
            fx.autocasts.append((trigger, skill, level, value / 10, trigger_skill))
            fx.desc.append(describe_inactive(cmd, name, args, value))
            return
    if cmd == "skill" and args:
        skill = skills_norse.runeheir_skill(args[0])
        if skill and isinstance(value, int):
            fx.grants[skill] = max(value, fx.grants.get(skill, 0))
            fx.desc.append(f"lets you use Lv {value} {skill_label(args[0])}")
            return
    if cmd == "bonus2" and n == "bskillatk" and args:
        skill = skills_norse.runeheir_skill(args[0])
        if skill:
            fx.skill_bonus[skill] = fx.skill_bonus.get(skill, 0) + value
            fx.desc.append(f"{skill_label(args[0])} damage {_pct(value)}")
            return
    if cmd in ("autobonus", "autobonus2", "autobonus3") and len(args) >= 3 and isinstance(args[1], int) and isinstance(args[2], int):
        inner = translate(str(args[0]).strip("{} "), False)["base"]
        trigger_skill = skills_norse.runeheir_skill(args[3]) if cmd == "autobonus3" and len(args) > 3 else None
        if (inner.mods or inner.stats or inner.all_stats) and not (inner.calls or inner.procs or inner.autocasts)                 and (cmd != "autobonus3" or trigger_skill):
            trigger = "WhenHit" if cmd == "autobonus2" else "OnSkill" if cmd == "autobonus3" else "OnAttack"
            text = describe_inactive(cmd, name, args, value)
            fx.autobuffs.append((trigger, inner, args[2] / 1000, args[1] / 10, trigger_skill, text))
            fx.desc.append(text)
            return
    if cmd == "bonus3" and n == "baddeff" and args and args[0] in STATUSES and "ATF_SELF" not in str(args[1:]):
        status = STATUSES[args[0]]
        fx.procs.append((status, value / 100))
        fx.desc.append(f"{value / 100:g}% chance to inflict {STATUS_LABEL.get(status, status)} on hit")
        return
    if cmd == "bonus3" and n == "bsubele" and args:
        for el in _element_targets(args[0]):
            fx.call("From", f"Element.{el}", -value)
        fx.desc.append(_less_more(value, f"{_element_label(args[0])} damage"))
        return
    fx.notes.append(describe_inactive(cmd, name, args, value))


def describe_inactive(cmd, name, args, value):
    """English for a bonus Runeheir has no system for yet."""
    n = name.lower()
    a = [str(x).strip('"') if not isinstance(x, Sym) else x.text for x in args]
    if n.startswith("bautospell"):
        when = "when hit" if "whenhit" in n else "on skill" if "onskill" in n else "on attack"
        if n.endswith("onskill") and len(a) >= 3:
            return f"chance to cast Lv {a[2]} {skill_label(a[1])} when using {skill_label(a[0])}"
        level = a[1] if len(a) > 1 else "?"
        chance = f"{_div(value, 10)}% chance" if isinstance(value, (int, float)) else "chance"
        return f"{chance} to cast Lv {level} {skill_label(a[0])} {when}"
    if n == "bskillatk" and a:
        return f"{skill_label(a[0])} damage {_pct(value)}"
    if n in ("baddmonsterdropitem", "baddmonsteriddropitem", "baddmonsterdropitemgroup"):
        return "extra item drops from monsters"
    if n == "bexpaddrace" and a:
        return f"{_pct(value)} EXP from {_race_label(a[0])}"
    if n == "baddeffwhenhit" and a:
        return f"{_div(value, 100)}% chance to inflict {STATUSES.get(a[0], a[0])} on attackers"
    if n == "baddeff" and a:
        return f"{_div(value, 100)}% chance to inflict {a[0].replace('Eff_', '')} on hit"
    if n == "bcomarace" and a:
        return f"{_div(value, 100)}% chance of a crippling blow vs {_race_label(a[0])}"
    if n in ("baddrace2", "bsubrace2") and a:
        return f"{_pct(value)} {'damage vs' if n == 'baddrace2' else 'less damage from'} {a[0].replace('RC2_', '')}"
    if n in ("bhpregenrate", "bspregenrate"):
        return f"regenerates {'HP' if 'hp' in n else 'SP'} over time"
    if n in ("bhplossrate", "bsplossrate", "bspvanishrate"):
        return f"drains {'HP' if 'hp' in n else 'SP'} over time"
    if n == "bspgainrace" and a:
        return f"gain SP when killing {_race_label(a[0])}"
    if n in ("badddamageclass", "badddefmonster"):
        return f"{_pct(value)} {'damage vs' if n == 'badddamageclass' else 'less damage from'} a particular monster"
    if n in ("bcastrate", "bskillheal", "bskillcooldown", "bskillusesp", "bskillusesprate", "bskilldelay") and a:
        what = {"bcastrate": "cast time", "bskillheal": "healing", "bskillcooldown": "cooldown", "bskillusesp": "SP cost",
                "bskillusesprate": "SP cost", "bskilldelay": "after-cast delay"}[n]
        return f"{skill_label(a[0])} {what} {_pct(value) if n != 'bskillusesp' else _num(value)}"
    if n == "baddeff2" and a:
        return f"{_div(value, 100)}% chance to suffer {STATUSES.get(a[0], a[0]).replace('StoneCurse', 'Stone Curse')} yourself when attacking"
    if n == "baddeff" and len(a) > 1 and "ATF_SELF" in str(a[1]):
        return f"{_div(value, 100)}% chance to suffer {STATUSES.get(a[0], a[0])} yourself when attacking"
    if n in NOTE_FLAGS and NOTE_FLAGS[n]:
        return NOTE_FLAGS[n]
    if cmd == "skill" and a:
        return f"lets you use Lv {_text(value)} {skill_label(a[0])}"
    if cmd.startswith("autobonus") and args:
        inner = describe(translate(str(args[0]).strip("{} "), False))
        rate = f"{args[1] / 10:g}% chance" if len(args) > 1 and isinstance(args[1], int) else "a chance"
        seconds = f" for {args[2] / 1000:g}s" if len(args) > 2 and isinstance(args[2], int) else ""
        when = "when hit" if cmd == "autobonus2" else "on skill use" if cmd == "autobonus3" else "on attack"
        return f"{rate} {when}: {inner.rstrip('.') or 'a bonus'}{seconds}"
    if cmd == "unsupported":
        return "a scripted effect"
    if cmd in ("sc_start", "sc_start2", "sc_start4", "itemheal", "heal", "percentheal", "specialeffect", "specialeffect2"):
        return "a scripted effect"
    shown = ", ".join(a)
    return f"{name}{' ' + shown if shown else ''} {_text(value)}".strip()


# ---------------------------------------------------------------------------------------------- translation
def _value_position(cmd, name, args):
    """Index into args (after the bonus name) of the numeric value, or None for flag bonuses."""
    n = name.lower()
    if cmd == "bonus3" and n in ("baddeff", "bsubele", "baddeffwhenhit"):
        return 1
    if cmd == "bonus3" and n == "baddeffonskill":
        return 2
    if n.startswith("bautospell"):
        # sk,lv,rate[,flags] / trigger,sk,lv,rate[,flags]
        pos = 3 if n.endswith("onskill") else 2
        return pos if len(args) > pos else None
    if cmd == "skill":
        return 1 if len(args) > 1 else None
    if not args:
        return None
    last = args[-1]
    return len(args) - 1 if isinstance(last, (int, Sym)) else None


def _records(tree, refines):
    """{key: [value at each refine]}, keys in first-seen order. key = (guard texts, guard stat, cmd, name, other args)."""
    table = {}
    for r in refines:
        for guard, cmd, args in run(tree, r):
            if cmd.startswith("bonus") and args:
                name, rest = str(args[0]), args[1:]
            elif cmd == "skill":
                name, rest = "skill", args
            elif cmd.startswith("autobonus"):
                name, rest = cmd, args
            else:
                name, rest = cmd, args[:1]
            pos = _value_position(cmd, name, rest) if cmd.startswith("bonus") or cmd == "skill" else None
            value = rest[pos] if pos is not None else 1
            others = tuple(_text(x) for i, x in enumerate(rest) if i != pos)
            stat_guard = None
            if len(guard) == 1 and guard[0].stat and guard[0].threshold:
                stat_guard = (guard[0].stat, guard[0].threshold)
            key = (tuple(g.text for g in guard), stat_guard, cmd, name, others)
            row = table.setdefault(key, {"values": [0] * len(refines), "args": rest, "pos": pos})
            idx = refines.index(r)
            if isinstance(value, Sym) or isinstance(row["values"][idx], Sym):
                row["values"][idx] = value if isinstance(value, Sym) else row["values"][idx]
            else:
                row["values"][idx] += value
    return table


def translate(script, refinable, ctx=None):
    """
    Translates an rAthena script into {"base": Fx, "per_refine": Fx or None, "thresholds": [(refine, Fx)], "errors": [...]}.
    """
    result = {"base": Fx(), "per_refine": None, "thresholds": [], "refine_desc": [], "errors": []}
    if not script or not script.strip():
        return result
    try:
        tree = parse(script)
    except ScriptError as e:
        result["errors"].append(str(e))
        result["base"].notes.append("a scripted effect")
        return result

    refines = list(range(0, MAX_REFINE + 1)) if refinable else [0]
    table = _records(tree, refines)
    base = result["base"]
    per = Fx()
    steps = {}
    condition = None  # ((stat, minimum), Fx): Runeheir has one base-stat condition per effect

    for key, row in table.items():
        guard_texts, stat_guard, cmd, name, _ = key
        values = row["values"]
        args = [a for i, a in enumerate(row["args"]) if i != row["pos"]]
        prefix = f"{' and '.join(guard_texts)}: " if guard_texts else ""
        syms = [v for v in values if isinstance(v, Sym)]
        if syms:
            base.notes.append(prefix + _sym_bonus_label(cmd, name, args, syms[0]))
            continue
        if not guard_texts:
            _split(base, per, steps, cmd, name, args, values, refines, result["refine_desc"])
            continue
        first = next((i for i, v in enumerate(values) if v), None)
        if first is None:
            continue
        steady = all(v == values[first] for v in values[first:])
        if stat_guard and first == 0 and steady and (condition is None or condition[0] == stat_guard):
            if condition is None:
                condition = (stat_guard, Fx())
            apply_bonus(condition[1], cmd, name, args, values[0], ctx)
            continue
        # A condition Runeheir can't check yet: describe the bonus in a note.
        probe = Fx()
        apply_bonus(probe, cmd, name, args, values[first], ctx)
        text = ", ".join(probe.desc + probe.notes)
        if text:
            base.notes.append(f"{prefix}{text}{f' (from +{refines[first]})' if first else ''}")

    if condition:
        (stat, minimum), fx = condition
        base.condition = (stat, minimum, fx)
        base.notes.extend(f"with base {stat.upper()} {minimum}+: {n}" for n in fx.notes)
        fx.notes = []
    # Inactive bonuses are listed once, on the base effect (the tooltip shows Effect.Inactive).
    base.notes.extend(f"per refine: {n}" for n in per.notes)
    per.notes = []
    for refine, fx in sorted(steps.items()):
        base.notes.extend(f"at +{refine}: {n}" for n in fx.notes)
        fx.notes = []
    if not per.empty():
        result["per_refine"] = per
    result["thresholds"] = [(r, fx) for r, fx in sorted(steps.items()) if not fx.empty()]
    return result


def _sym_bonus_label(cmd, name, args, sym):
    n = name.lower()
    if cmd == "bonus" and n in STATS:
        return f"{STATS[n].upper()} + {sym.text}"
    if cmd == "bonus" and n in SIMPLE:
        return f"{SIMPLE[n][1]} + {sym.text}"
    return describe_inactive(cmd, name, args, sym)


def _split(target, per, steps, cmd, name, args, values, refines, refine_desc):
    """Base value -> target; a step repeated on most refine levels -> per; everything else -> thresholds."""
    if values[0]:
        apply_bonus(target, cmd, name, args, values[0], None)
    deltas = [values[i] - values[i - 1] for i in range(1, len(values))]
    if not any(deltas):
        return
    flag = cmd.startswith("bonus") and _is_flag(cmd, name)
    slope = 0
    if not flag:
        common = collections.Counter(d for d in deltas if d).most_common(1)
        if common and common[0][1] * 2 > len(deltas):
            slope = common[0][0]
            apply_bonus(per, cmd, name, args, slope, None)
    levels, totals, running = [], [], 0
    for i, d in enumerate(deltas):
        rest = d - slope
        if not rest or (flag and rest < 0):
            continue
        running += rest
        apply_bonus(steps.setdefault(refines[i + 1], Fx()), cmd, name, args, rest, None)
        levels.append(refines[i + 1])
        totals.append(running)
    if levels:
        refine_desc.append((levels, [_phrase(cmd, name, args, t) for t in totals]))


def _phrase(cmd, name, args, value):
    probe = Fx()
    apply_bonus(probe, cmd, name, args, value, None)
    return ", ".join(probe.desc)


def _is_flag(cmd, name):
    n = name.lower()
    return n in UNBREAKABLE or n in ("bnocastcancel", "batkele", "bdefele", "bignoredefrace", "bignoremdefrace", "bignoredefclass")


# ---------------------------------------------------------------------------------------------- C#
def _float(v):
    s = f"{float(v):g}"
    if "e" in s:
        s = f"{float(v):.6f}".rstrip("0").rstrip(".")
    return s + "f"


def cs_modifiers(fx):
    inits = []
    for field, value in fx.mods.items():
        if not value:
            continue
        inits.append(f"{field} = {int(round(value)) if field in Fx.INT_FIELDS else _float(value)}")
    for flag in sorted(fx.flags):
        inits.append(f"{flag} = true")
    chain = ""
    for stat, value in fx.stats.items():
        if value:
            chain += f".SetStat(StatType.{stat}, {value})"
    if fx.all_stats:
        chain += f".AddAllStats({fx.all_stats})"
    if not inits and not chain:
        return None
    return "new StatModifiers" + (" { " + ", ".join(inits) + " }" if inits else "()") + chain


class BuffNamer:
    """Ids and names for the timed buffs an item's autobonuses become (xrb<item>_<n>)."""

    def __init__(self, ro_id, item_name):
        self.ro_id = ro_id
        self.item_name = item_name
        self.count = 0

    def next(self):
        self.count += 1
        return f"xrb{self.ro_id}_{self.count}", self.item_name


def cs_effect(fx, indent="                ", buffs=None):
    """C# expression for an EquipEffect, or None when empty."""
    if fx is None or fx.empty():
        return None
    inits = []
    mods = cs_modifiers(fx)
    if mods:
        inits.append(f"Modifiers = {mods}")
    if fx.unbreakable:
        inits.append("Unbreakable = true")
    if fx.armor_element:
        inits.append(f"ArmorElement = Element.{fx.armor_element}")
    if fx.weapon_element:
        inits.append(f"WeaponElement = Element.{fx.weapon_element}")
    if fx.condition:
        stat, minimum, sub = fx.condition
        inner = cs_effect(sub, indent, buffs) or "new EquipEffect()"
        inits.append(f"Condition = new EquipCondition {{ Stat = StatType.{stat}, MinBaseValue = {minimum}, Bonus = {inner} }}")
    text = "new EquipEffect" + (" { " + ", ".join(inits) + " }" if inits else "()")
    for (method, arg), value in fx.calls.items():
        if not value:
            continue
        text += f".{method}({arg + ', ' if arg else ''}{_float(value)})"
    if fx.immune:
        text += ".Immune(" + ", ".join(f"StatusEffect.{s}" for s in dict.fromkeys(fx.immune)) + ")"
    for status, chance in fx.procs:
        text += (f".Proc(new OnHitEffect {{ Status = StatusEffect.{status}, ChancePercent = {_float(chance)}, "
                 f"Duration = {_float(STATUS_DURATION.get(status, 5))} }})")
    for trigger, skill, level, chance, trigger_skill in fx.autocasts:
        if trigger == "OnSkill":
            text += f'.AutoCastOnSkill("{trigger_skill}", "{skill}", {level}, {_float(chance)})'
        else:
            text += f'.AutoCast{trigger}("{skill}", {level}, {_float(chance)})'
    for trigger, inner, seconds, chance, trigger_skill, description in fx.autobuffs:
        buff_id, item_name = buffs.next() if buffs else (f"xrb_anon_{abs(hash(description)) % 100000}", "Surge")
        mods = cs_modifiers(inner) or "StatModifiers.Empty()"
        text += (f".AutoBuff(AutoCastTrigger.{trigger}, XileBuff(\"{buff_id}\", {cs_string(item_name)}, "
                 f"{cs_string(sentence(inner.desc))}, {_float(seconds)}, {mods}), {_float(chance)}"
                 + (f', "{trigger_skill}")' if trigger_skill else ")"))
    for skill, level in fx.grants.items():
        text += f'.Grant("{skill}", {level})'
    for skill, percent in fx.skill_bonus.items():
        text += f'.SkillBonus("{skill}", {_float(percent)})'
    for note in dict.fromkeys(fx.notes):
        text += f".Note({cs_string(note)})"
    return text


def cs_string(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


# ---------------------------------------------------------------------------------------------- English
def sentence(parts):
    parts = [p for p in parts if p]
    if not parts:
        return ""
    text = ", ".join(parts)
    return text[0].upper() + text[1:] + "."


_NUMBER = re.compile(r"([+-]?\d+(?:\.\d+)?)")


def _merge(levels, phrases):
    """"At +6/+7/+8: +9/+16/+25% damage vs Demi-Humans" when the phrases differ only in their numbers."""
    pieces = [_NUMBER.split(p) for p in phrases]
    if len(pieces[0]) > 1 and all(p[::2] == pieces[0][::2] for p in pieces):
        merged = list(pieces[0])
        for k in range(1, len(merged), 2):
            column = [p[k] for p in pieces]
            merged[k] = column[0] if len(set(column)) == 1 else "/".join(column)
        return f"At {'/'.join(f'+{level}' for level in levels)}: " + sentence(["".join(merged)])
    return " ".join(f"At +{level}: {sentence([p])}" for level, p in zip(levels, phrases))


def describe(result):
    """The English rules text of a translated script (notes excluded: the tooltip lists them)."""
    out = []
    base = result["base"]
    out.append(sentence(base.desc))
    if base.condition:
        stat, minimum, fx = base.condition
        out.append(f"With base {stat.upper()} {minimum}+: " + sentence(fx.desc))
    per = result["per_refine"]
    if per and per.desc:
        out.append("Per refine: " + sentence(per.desc))
    singles = {}
    lines = []
    for levels, phrases in result.get("refine_desc", []):
        phrases = [p for p in phrases]
        if not all(phrases):
            continue
        if len(levels) == 1:
            singles.setdefault(levels[0], []).extend(phrases)
        else:
            lines.append((levels[0], _merge(levels, phrases)))
    lines.extend((level, f"At +{level}: " + sentence(phrases)) for level, phrases in singles.items())
    out.extend(text for _, text in sorted(lines, key=lambda line: line[0]))
    return " ".join(p for p in out if p)
