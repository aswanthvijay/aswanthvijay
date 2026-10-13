"""Phase 8.4: XileRetro's costumes (its client-only "[C]" items) as Runeheir costumes.

XileRetro has about 2,300 costumes, mostly sprite art Runeheir can't draw. Runeheir draws costumes from a few dozen look
templates (ears, halos, wings, auras, wigs, capes...) in the costume's color, so every costume is sorted into a
template by what its name says it is, colored by the color its name gives, and placed by the slot its description
gives. Costumes that would look the same in Runeheir become one item. Names are new (Norse); nothing of XileRO's text is
kept.
"""

import collections
import hashlib
import re

import script_translate as st

# (pattern, template); the first match wins. Patterns run on the lowercased name.
TEMPLATES = [
    (r"\baura\b|\baurora\b", "aura"),
    (r"wing", "wings"),
    (r"\btail\b|\btails\b", "tail"),
    (r"\bcape\b|\bcloak\b|\bmanteau\b|\bmantle\b", "cape"),
    (r"\bhair\b|\bwig\b|ponytail|twin ?tail|braid|\blocks\b|\bbob\b|\bpigtail", "wig"),
    (r"bunny|rabbit|\bhare\b", "ears_bunny"),
    (r"\bfox\b|kitsune", "ears_fox"),
    (r"\bcat\b|\bneko\b|kitty|\bears?\b", "ears_cat"),
    (r"\bhalo\b|angel ring", "halo"),
    (r"flower|\brose\b|sakura|blossom|\blily\b|tulip|daisy|petal", "flower"),
    (r"ribbon|\bbow\b", "ribbon"),
    (r"top ?hat|silk hat|magician", "tophat"),
    (r"\bhorns?\b|antler", "horns"),
    (r"headphone|headset|earphone|ear ?muff", "headphones"),
    (r"crown|tiara|diadem", "crown"),
    (r"circlet", "circlet"),
    (r"witch|wizard|mage hat|sorcer", "wizard_hat"),
    (r"\bhood\b", "hood"),
    (r"helm", "helm"),
    (r"glasses|goggle|shades|spectacle|visor", "glasses"),
    (r"eye ?patch", "eyepatch"),
    (r"monocle", "monocle"),
    (r"scarf|muffler", "scarf"),
    (r"\bpipe\b|cigar", "pipe"),
    (r"beard|mustache|moustache", "beard"),
    (r"mask", "mask"),
    (r"\bcap\b|beret|beanie|bandana|\bhat\b", "cap"),
]
DEFAULT_BY_SLOT = {"CostumeUpper": "cap", "CostumeMid": "glasses", "CostumeLower": "aura", "CostumeGarment": "cape"}

WIG_STYLES = [(r"spik", 1), (r"long|semi long|straight", 2), (r"ponytail|pony", 3), (r"twin|pigtail", 4), (r"\bbun\b|odango", 5),
              (r"mohawk", 6), (r"braid|viking", 7), (r"short|bob", 0)]
HAIR_STYLE_NAMES = ["Short", "Spiky", "Long", "Ponytail", "Twin-Tail", "Bun", "Mohawk", "Braided"]

COLORS = [
    ("rainbow", "Rainbow", "#E67EE6"), ("crimson", "Crimson", "#B0203A"), ("scarlet", "Scarlet", "#D6312E"), ("red", "Red", "#C8352E"),
    ("pink", "Pink", "#F08DB5"), ("magenta", "Magenta", "#C2389A"), ("purple", "Purple", "#8A4FB8"), ("violet", "Violet", "#9B6BD6"),
    ("lavender", "Lavender", "#B9A2E6"), ("navy", "Navy", "#26386E"), ("blue", "Blue", "#3C6FD0"), ("azure", "Azure", "#4FA3E8"),
    ("cyan", "Cyan", "#3FC4D8"), ("teal", "Teal", "#2A9D8F"), ("aqua", "Aqua", "#5ED3D0"), ("mint", "Mint", "#8FE0B5"),
    ("green", "Green", "#3E9E4A"), ("emerald", "Emerald", "#20A060"), ("lime", "Lime", "#9AD43A"), ("yellow", "Yellow", "#E8C93A"),
    ("gold", "Gold", "#D9A932"), ("orange", "Orange", "#E8832E"), ("amber", "Amber", "#D99A28"), ("brown", "Brown", "#7A5233"),
    ("silver", "Silver", "#C0C6CF"), ("grey", "Grey", "#8C9299"), ("gray", "Grey", "#8C9299"), ("white", "White", "#EEF1F5"),
    ("snow", "Snow", "#F4F7FB"), ("black", "Black", "#2A2A30"), ("dark", "Shadow", "#3A3445"), ("light", "Bright", "#F3EBC8"),
    ("ice", "Frost", "#A9D8F2"), ("frost", "Frost", "#A9D8F2"), ("fire", "Ember", "#E8622E"), ("flame", "Ember", "#E8622E"),
    ("blood", "Blood", "#8E1B23"), ("heart", "Rose", "#E05A82"), ("love", "Rose", "#E05A82"), ("star", "Star", "#F2D35B"),
    ("moon", "Moon", "#C7D2F0"), ("sun", "Sun", "#F2B33B"), ("night", "Night", "#2E3560"), ("sky", "Sky", "#7FB8EA"),
]
FALLBACK_COLORS = [("Amber", "#D99A28"), ("Fjord", "#3C6FD0"), ("Moss", "#3E9E4A"), ("Rose", "#E05A82"), ("Ash", "#8C9299"),
                   ("Heather", "#9B6BD6"), ("Ember", "#E8622E"), ("Frost", "#A9D8F2")]

NOUNS = {
    "aura": "Rune Aura", "aura_rise": "Ember Aura", "wings": "Valkyrie Wings", "wings_bat": "Dökkálfar Wings",
    "wings_fairy": "Ljósálfar Wings", "tail": "Fylgja Tail", "cape": "Jarl's Cloak", "ears_bunny": "Hare Ears",
    "ears_cat": "Cat Ears of Freyja", "ears_fox": "Fox Ears", "halo": "Halo of Baldr", "flower": "Blossom of Iðunn",
    "ribbon": "Hair Ribbon", "tophat": "Skald's Top Hat", "horns": "Jötunn Horns", "headphones": "Ear-Cups of Heimdall",
    "crown": "Jarl's Crown", "circlet": "Seiðkona's Circlet", "wizard_hat": "Völva's Hat", "hood": "Wanderer's Hood",
    "helm": "Parade Helm", "glasses": "Glass Lenses", "eyepatch": "Eyepatch", "monocle": "Monocle", "mask_eyes": "Masquerade Mask",
    "head_wings": "Head-Wings of Hermóðr", "scarf": "Wind Scarf", "pipe": "Skald's Pipe", "beard": "False Beard", "mask": "Face Veil",
    "muffler": "Muffler", "cap": "Town Cap",
}
POSITIONS = {"upper": "CostumeUpper", "top": "CostumeUpper", "middle": "CostumeMid", "mid": "CostumeMid", "lower": "CostumeLower",
             "low": "CostumeLower", "garment": "CostumeGarment"}

PRICE = 25000


def _clean(line):
    return re.sub(r"\^[0-9a-fA-F]{6}", "", str(line)).strip()


def is_costume(entry):
    name = str(entry.get("name") or "")
    lines = [_clean(l) for l in entry.get("description") or []]
    kind = next((l.split(":", 1)[1].strip().lower() for l in lines if re.match(r"^type\s*:", l, re.I)), "")
    return name.startswith("[C]") or kind.startswith("costume") or bool(entry.get("costume"))


def slots_of(entry):
    lines = [_clean(l) for l in entry.get("description") or []]
    kind = next((l.split(":", 1)[1].strip().lower() for l in lines if re.match(r"^type\s*:", l, re.I)), "")
    position = next((l.split(":", 1)[1].strip().lower() for l in lines if re.match(r"^position\s*:", l, re.I)), "")
    if "garment" in kind or "garment" in position:
        return ["CostumeGarment"]
    found = []
    for word in re.split(r"[^a-z]+", position):
        slot = POSITIONS.get(word)
        if slot and slot not in found:
            found.append(slot)
    if "mixed" in position:
        found = ["CostumeUpper", "CostumeMid"]
    return found or ["CostumeUpper"]


def template_of(name, slots):
    low = name.lower()
    template = next((t for pattern, t in TEMPLATES if re.search(pattern, low)), None)
    first = slots[0]
    if template == "wings":
        if re.search(r"\bbat\b|demon|devil|vampir|dark|deviling|gargoyle|succubus", low):
            template = "wings_bat"
        elif re.search(r"fairy|butterfly|pixie|dragonfly|moth|sprite", low):
            template = "wings_fairy"
        if first == "CostumeMid":
            template = "head_wings"
    elif template == "aura" and re.search(r"flame|fire|spirit|soul|smoke|bubble|sparkle|snow|petal", low):
        template = "aura_rise"
    elif template == "wig" and first != "CostumeMid":
        template = None  # a "hair" ornament worn on top: a ribbon or cap
    elif template == "mask" and first == "CostumeMid":
        template = "mask_eyes"
    elif template == "scarf" and first == "CostumeGarment":
        template = "muffler"
    if template is None:
        template = DEFAULT_BY_SLOT.get(first, "cap")
    if template == "wig":
        style = next((s for pattern, s in WIG_STYLES if re.search(pattern, low)), int(hashlib.md5(low.encode()).hexdigest(), 16) % 8)
        template = f"wig:{style}"
    return template


def color_of(name):
    low = name.lower()
    for word, label, hexcolor in COLORS:
        if re.search(r"\b" + word, low):
            return label, hexcolor
    label, hexcolor = FALLBACK_COLORS[int(hashlib.md5(low.encode()).hexdigest(), 16) % len(FALLBACK_COLORS)]
    return label, hexcolor


def display_name(template, color):
    if template.startswith("wig:"):
        return f"{color} {HAIR_STYLE_NAMES[int(template[4:]) % len(HAIR_STYLE_NAMES)]} Wig"
    return f"{color} {NOUNS.get(template, 'Ornament')}"


class Costume:
    def __init__(self, ro_id, slots, template, color_label, color_hex):
        self.ro_id = ro_id
        self.id = f"xr{ro_id}"
        self.slots = slots
        self.template = template
        self.color_label = color_label
        self.color_hex = color_hex
        self.name = display_name(template, color_label)
        self.variants = 1


def collect(client, server_ids, taken_names):
    """One Costume per distinct look (template, color, slots), named and ready to emit; ids of the merged duplicates too."""
    groups = collections.OrderedDict()
    merged = {}
    for ro_id in sorted(client):
        if ro_id in server_ids:
            continue
        entry = client[ro_id]
        if not is_costume(entry):
            continue
        name = re.sub(r"^\[[^\]]+\]\s*", "", str(entry.get("name") or ""))
        slots = slots_of(entry)
        template = template_of(name, slots)
        label, hexcolor = color_of(name)
        key = (template, label, tuple(slots))
        if key in groups:
            groups[key].variants += 1
            merged[ro_id] = groups[key].id
            continue
        groups[key] = Costume(ro_id, slots, template, label, hexcolor)
    costumes = list(groups.values())
    # Same template and color in different slots: say where it's worn.
    by_name = collections.defaultdict(list)
    for c in costumes:
        by_name[c.name].append(c)
    for name, same in by_name.items():
        if len(same) > 1 or name in taken_names:
            for c in same:
                where = {"CostumeUpper": "Crown", "CostumeMid": "Brow", "CostumeLower": "Chin", "CostumeGarment": "Back"}[c.slots[0]]
                c.name = f"{name} ({where})"
    seen = collections.Counter()
    for c in costumes:
        seen[c.name] += 1
        if seen[c.name] > 1:
            c.name = f"{c.name} {seen[c.name]}"
    return costumes, merged


def build(c):
    slots = " | ".join(f"EquipSlot.{s}" for s in c.slots)
    where = {"CostumeUpper": "on top of the head", "CostumeMid": "at the brow", "CostumeLower": "low on the face or at the feet",
             "CostumeGarment": "on the back"}[c.slots[0]]
    desc = (f"A costume, worn {where} over whatever gear is there. It changes how you look, never your stats. "
            f"Wear it from the bag; the costume row of the equipment window (Q) holds it.")
    label = "".join(w[0] for w in c.name.split()[:3] if w[0].isalpha()).upper()[:3] or "C"
    return [f'Id = "{c.id}", RoId = {c.ro_id}, Name = {st.cs_string(c.name)}',
            f"Description = {st.cs_string(desc)}",
            f"Kind = ItemKind.Equipment, Slots = {slots}, Refinable = false",
            f'LookAs = "{c.template}", ViewColorHex = "{c.color_hex}"',
            f"Weight = 0, Price = {PRICE}, Category = ItemCategory.Costume",
            f'IconLabel = {st.cs_string(label)}, IconColorHex = "{c.color_hex}"']
