"""Norse renaming of XileRetro / Ragnarok item names for Runeheir.

Ragnarok already borrows heavily from Norse myth (Mjolnir, Sleipnir, Valkyrie...), so those names stay, with Old Norse
spellings. What changes: Ragnarok's own places, people, companies and monsters, the Japanese, Korean, Indian and Greek
legend names, real-world events and nations, and the Ragnarok job names (which become Runeheir's jobs). Ordinary
English item words (Sword, Ribbon, Bucket Hat...) stay as they are.

    norse_name("Excalibur")            -> "Hofud"
    norse_name("Orcish Axe")           -> "Trollish Axe"
    norse_name("Santa Poring Hat")     -> "Yule Slimling Hat"

Order: FULL (whole-name overrides) first, then PHRASES (multi-word), then WORDS (single tokens, word-bounded).
"""

import re

# ---------------------------------------------------------------------------------------------- whole names
FULL = {
    # Legendary blades and polearms: Ragnarok's Arthurian, Japanese and Indian legends become Norse ones.
    "Excalibur": "Hofud",
    "Mysteltainn": "Mistilteinn",
    "Tirfing": "Tyrfingr",
    "Balmung": "Gram",
    "Warrior's Balmung": "Warrior's Gram",
    "Byeollungum": "Dáinsleif",
    "Masamune": "Skofnung",
    "Muramasa": "Hviting",
    "Murasame": "Rainmist",
    "Hakujin": "White Seax",
    "Kamaitachi": "Gale Weasel",
    "Jitte": "Parrying Iron",
    "Asura": "Ásmegin Blade",
    "Ashura": "Ásmegin Edge",
    "Krishna": "Sumarbrandr",
    "Chakram": "Ring Blade",
    "Nagan": "Nagelring",
    "Caesar's Sword": "Jarl's Sword",
    "Haedonggum": "Fjord Sword",
    "Tae Goo Lyeon": "Mountain-Cleaver",
    "Sashimi": "Fish-Splitter",
    "Atlas Weapon": "Ymir's Weapon",
    "Krasnaya": "Rauðr",
    "Gae Bolg": "Gunnar's Atgeir",
    "Longinus's Spear": "Spear of the Hanged God",
    "Brionac": "Lævateinn",
    "Zephyrus": "Hræsvelgr's Gale",
    "Gelerdria": "Geirdriful",
    "Brocca": "Brokkr's Pike",
    "Tjungkuletti": "Týr's Pike",
    "Mjolnir": "Thrúðvangr Maul",
    "Hollgrehenn's Hammer": "Eitri's Hammer",
    "Lunakaligo": "Mánamyrkr",
    "Nemesis": "Retribution",
    "Quadrille": "Four-Flanged Mace",
    "Kronos": "Urðr's Staff",
    "Dea Staff": "Dís Staff",
    "Croce Staff": "Rood Staff",
    "Staff Of Bordeaux": "Staff of the Red Vale",
    "La'cryma Stick": "Weeping Wand",
    "Angra Manyu": "Myrkr Codex",
    "Ahura Mazdah": "Ljós Ward",
    "Vecer Axe": "Kvöld Axe",
    "Sabbath": "Seiðr Night",
    "Guillotine": "Headsman's Axe",
    "Berdysz": "Bardiche",
    "Bazerald": "Baldr's Seax",
    "Azoth": "Quicksilver Seax",
    "Sucsamad": "Sorrow Seax",
    "Khukri": "Hooked Seax",
    "Cinquedea": "Broad-Fuller Dagger",
    "Krierg": "Vígr",
    "Weihna": "Jólnir",
    "Gakkung Bow": "Horn Bow",
    "Luna Bow": "Máni Bow",
    "Ixion Wings": "Valravn Wings",
    "Waghnak": "Bear Claws",
    "Horn of Hillslion": "Horn of Hildisvíni",
    "Chemeti Whip": "Lash of Thorns",
    "Gumoongoh": "Langspil",
    "Orleans's Server": "Fulla's Platter",
    "Orleans's Gown": "Fulla's Gown",
    "Orleans's Glove": "Fulla's Glove",
    "Guyak Shield": "Turtle Shield",
    "Valkyrja's Shield": "Valkyrja's Shield",
    "Lucius's Fierce Armor of Volcano": "Múspell's Raging Armor",
    "Saphien's Armor of Ocean": "Ægir's Armor of the Tide",
    "Aebecee's Raging Typhoon Armor": "Kári's Storm Armor",
    "Claytos Cracking Earth Armor": "Jörð's Quaking Armor",
    "Kandura": "Desert Robe",
    "Aumdura's Benefit": "Eir's Blessing",
    "Lord Kaho's Horn": "Logi's Horn",
    "Matyr's Leash": "Garm's Leash",
    "Hyuke's Black Cat Ears": "Freyja's Black Cat Ears",
    "Serin's Gold Ring": "Sif's Gold Ring",
    "Takius's Blindfold": "Höðr's Blindfold",
    "Antonio's Santa Hat": "Yule Elder's Hat",
    "Carmen Miranda's Hat": "Iðunn's Fruit Crown",
    "Poet Natalie's Hat": "Bragi's Poet Hat",
    "Apple of Archer": "Huntsman's Apple",
    "Zorro Masque": "Outlaw's Masque",
    "Roguemaster's Bow": "Outlaw Chief's Bow",
    "Rogue's Treasure": "Outlaw's Hoard",
    "Ph.D Hat": "Scholar's Hat",
    "Ph.D Hat V": "Scholar's Hat V",
    "Bao Bao": "Bear Cub Hat",
    "Sombrero": "Broad Straw Hat",
    "Coppola": "Flat Cap",
    "Shafka": "Fur Earflap Hat",
    "Ayam": "Fowl Cap",
    "Pagdayaw": "Feather Crown of Praise",
    "Selendang": "Silk Shawl",
    "Samambaia": "Fern Crown",
    "Hahoe Mask": "Grinning Masque",
    "Kabuki Mask": "Stage Masque",
    "Sakkat": "Reed Hat",
    "Wanderer's Sakkat": "Wanderer's Reed Hat",
    "Neko Mimi": "Cat Ears",
    "Neko Mimi Kafra": "Hall-Maid Cat Ears",
    "Koneko Hat": "Kitten Hat",
    "Kitsune Mask": "Fox-Spirit Mask",
    "Tanigumi Girl Doll": "Farm Girl Doll",
    "Sakura Milk Tea Hat": "Blossom Tea Hat",
    "Hyegun Hat": "Draugr Sentry Hat",
    "Kwati Hat": "Coati Hat",
    "Alice Doll": "Maid Doll",
    "Muslim Hat M": "Pilgrim's Cap",
    "Indian Fillet": "Feathered Fillet",
    "Indian Headband": "Feather Headband",
    "Chinese Crown": "Eastern Crown",
    "Chinese Handicraft": "Eastern Handicraft",
    "Luoyang NewYear Hat": "Yule Lantern Hat",
    "Ayothaya King's Hat": "Southern King's Hat",
    "Australian Flag Hat": "Southern Jarl's Banner Hat",
    "Brazil National Flag Hat": "Green Jarl's Banner Hat",
    "Bandana Merah Putih": "Red-and-White Bandana",
    "France Holiday Hat": "Midsummer Holiday Hat",
    "Europe Baseball Cap": "Northland Ball Cap",
    "Independence Memorial Hat": "Thing-Day Memorial Hat",
    "Republic Anniversary Ring": "Althing Jubilee Ring",
    "October Fest Cap": "Harvest Feast Cap",
    "Love Valentine's Hat": "Freyja's Day Hat",
    "Valentine Pledge": "Freyja's Pledge",
    "Twin Pompom By JB": "Twin Pompom",
    "RJC Necklace": "Tourney Necklace",
    "RJC Katusa Flower": "Tourney Flower",
    "RWC 2008 Dragon Helm Gold": "Champion's Dragon Helm (Gold)",
    "RWC 2008 Dragon Helm Silver": "Champion's Dragon Helm (Silver)",
    "RWC 2008 Dragon Helm Copper": "Champion's Dragon Helm (Copper)",
    "kRO Drooping Kitty C": "Drooping Kitty (Variant)",
    "2nd Anniversary Hat": "Second Jubilee Hat",
    "3rd Anniversary Celebration Ring": "Third Jubilee Ring",
    "5th Anniversary Coin": "Fifth Jubilee Coin",
    "Ragnarok Limited Edition": "Ragnarök Limited Edition",
    "Eye of Dullahan": "Eye of the Headless Rider",
    "Mask Of Ifrit": "Mask of Surtr",
    "Ears Of Ifrit": "Ears of Surtr",
    "Umbala Spirit": "Ironwood Spirit",
    "Zealotus Mask": "Masque of the Zealot",
    "Drooping Morocc Minion": "Drooping Múspell Minion",
    "Bijofnil Wings": "Bifröst Wings",
    "Hermode Cap": "Hermóðr's Cap",
    "Nydhorgg's Shadow Garb": "Níðhöggr's Shadow Garb",
    "Hatii Claw": "Hati Claw",
    "Freya's Crown": "Freyja's Crown",
    "Ulle's Cap": "Ullr's Cap",
    "Fricca's Circlet": "Frigg's Circlet",
    "Fricco's Shoes": "Frigg's Shoes",
    "Rudra Bow": "Ullr's Bow",
    "Galapago Cap": "Shellback Cap",
    "Gate Keeper-DD": "Gate Keeper DD",
}

# ---------------------------------------------------------------------------------------------- multi-word phrases
PHRASES = [
    ("Super Novice", "Wanderer"),
    ("Main Gauche", "Parrying Seax"),
    ("Main-Gauche", "Parrying Seax"),
    ("Dark Knight", "Black Einherjar"),
    ("Lord Knight", "Einherjar"),
    ("High Priest", "High Gothi"),
    ("Gatling Gun", "Storm Rod"),
    ("Grenade Launcher", "Thunderclap Rod"),
    ("Six Shooter", "Six-Bolt Rod"),
    ("Naght Sieger", "Náttsigr"),
    ("Glast Heim", "Glasisvellir"),
    ("Vesper Core", "Dvergr Core"),
    ("Orc Hero", "Troll Chieftain"),
    ("Orc Lord", "Troll King"),
    ("Goblin Leader", "Svartling Chief"),
    ("Evil Snake Lord", "Serpent King"),
    ("Moonlight Flower", "Moonflower"),
    ("Eden Team", "Mead-Hall Team"),
    ("Eden Revlover", "Mead-Hall Thunder-Rod"),
    ("Magic Bible", "Galdr Edda"),
    ("Book of the Apocalypse", "Book of Ragnarök"),
    ("Mr Smile", "Mr. Smile"),
    ("Pumpkin-Head", "Pumpkin Head"),
    ("Al De Baran", "Tímaborg"),
    ("Mt. Mjolnir", "Mjölnir Fells"),
]

# Ragnarok's towns and lands -> the Nine Worlds (shared with the bestiary and map import).
PLACES = {
    "Prontera": "Vigrid", "Geffen": "Galdrheim", "Payon": "Ýdalir", "Morroc": "Sandmark", "Alberta": "Hafnir",
    "Izlude": "Skjaldvík", "Aldebaran": "Tímaborg", "Juno": "Himinbjörg", "Yuno": "Himinbjörg", "Comodo": "Sólvík",
    "Umbala": "Járnviðr", "Lutie": "Jólheim", "Amatsu": "Austrey", "Kunlun": "Fjallheim", "Gonryun": "Fjallheim",
    "Louyang": "Drekaborg", "Ayothaya": "Suðrey", "Einbroch": "Smiðjuborg", "Einbech": "Smiðjuvík", "Lighthalzen": "Ljósvellir",
    "Hugel": "Hólmr", "Rachel": "Hofstaðr", "Veins": "Eldgjá", "Moscovia": "Garðaríki", "Brasilis": "Vínland",
    "Dewata": "Suðrland", "Malangdo": "Kattey", "Malaya": "Pálmaey", "Jawaii": "Ástarey", "Niflheim": "Niflheim",
    "Sograt": "Sandmark", "Thanatos": "Hel's Tower", "Juperos": "Dvergr Ruins", "Schwarzwald": "Myrkviðr",
    "Arunafeltz": "Hofland", "Rune-Midgard": "Midgard", "Midgard": "Midgard", "Kiel": "Kjölr",
}

# ---------------------------------------------------------------------------------------------- single words
WORDS = {
    # Ragnarok jobs -> Runeheir jobs.
    "Novice": "Initiate",
    "Swordman": "Warrior",
    "Swordsman": "Warrior",
    "Mage": "Mystic",
    "Archer": "Huntsman",
    "Archer's": "Huntsman's",
    "Acolyte": "Devotee",
    "Merchant": "Trader",
    "Thief": "Scout",
    "Wizard": "Sorcerer",
    "Minstrel": "Skald",
    "Ninja": "Nightraider",
    "Shinobi": "Nightraider",
    # Ragnarok's own people, gods and smiths.
    "Goibne's": "Völundr's",
    "Morpheus's": "Mímir's",
    "Morrigane's": "Hildr's",
    "Lucius's": "Múspell's",
    "Diabolus": "Hel-Forged",
    "Kafra": "Hall-Maid",
    "Freya's": "Freyja's",
    "Freya": "Freyja",
    "Lich's": "Draugr-Lord's",
    "Bradium": "Rimesteel",
    "Morocc": "Múspell",
    "Morroc": "Múspell",
    "Ifrit": "Surtr",
    "Defolty": "Dagny",
    "Glaris": "Gunnhild",
    "Sorin": "Solveig",
    "Telling": "Tove",
    "Bennit": "Bjorn",
    # Monsters (shared with the card and bestiary import).
    "Poring": "Slimling",
    "Poporing": "Venom Slimling",
    "Angeling": "Haloed Slimling",
    "Deviling": "Horned Slimling",
    "Deviruchi": "Myrkling",
    "Munak": "Draugmey",
    "Bongun": "Draugsveinn",
    "Baphomet": "Hel-Goat",
    "Orc": "Troll",
    "Orcish": "Trollish",
    "Goblin": "Svartling",
    "Smokie": "Mossbadger",
    "Sphinx": "Riddle-Lion",
    "Spinx": "Riddle-Lion",
    "Anubis": "Garmr",
    "Succubus": "Mara",
    "Incubus": "Alp",
    "Lunatic": "Moon-Hare",
    "Peco": "Strider",
    "Pecopeco": "Strider",
    "Filir": "Fjaðrir",
    "Filir's": "Fjaðrir's",
    "Vanilmirth": "Seiðr Wisp",
    "Lif": "Líf",
    "Bacilium": "Plague Mote",
    "Hatii": "Hati",
    "Nepenthes": "Pitcher-Vine",
    # Cultures, events and the real world.
    "Santa": "Yule",
    "Santa's": "Yule Elder's",
    "Christmas": "Yule",
    "Rudolph": "Reindeer",
    "Rudolph's": "Reindeer",
    "Anniversary": "Jubilee",
    "Valentine's": "Freyja's Day",
    "Valentine": "Freyja's Day",
    "Oriental": "Eastern",
    "Revolver": "Thunder-Rod",
    "Revlover": "Thunder-Rod",
    "Rifle": "Long Rod",
    "Shotgun": "Scatter Rod",
    "Pistol": "Short Rod",
    "Carbine": "Rod",
    "Katana": "Curved Langsax",
    "Tsurugi": "Straight Langsax",
    "Gladius": "Broad Seax",
    "Damascus": "Pattern-Welded Seax",
    "Bible": "Edda",
    "Apocalypse": "Ragnarök",
    "Gaia": "Jörð",
    "Epsilon": "Eihwaz",
    "Ragnarok": "Ragnarök",
    "Mjolnir": "Mjölnir",
    # The legend blades inside longer names (a "Muramasa Box" holds a Hviting).
    "Excalibur": "Hofud", "Mysteltainn": "Mistilteinn", "Tirfing": "Tyrfingr", "Balmung": "Gram", "Byeollungum": "Dáinsleif",
    "Masamune": "Skofnung", "Muramasa": "Hviting", "Murasame": "Rainmist", "Krishna": "Sumarbrandr", "Brionac": "Lævateinn",
    "Longinus": "the Hanged God", "Zephyrus": "Hræsvelgr", "Kronos": "Urðr", "Gungnir": "Gungnir", "Asura": "Ásmegin",
}

# Tokens dropped from names (they marked Ragnarok client variants or regional releases).
DROP = {"kRO", "RWC", "RJC"}

_PHRASE_RES = [(re.compile(r"(?<![\w'])" + re.escape(a) + r"(?![\w'])"), b) for a, b in PHRASES]
_WORD_RE = re.compile(r"[A-Za-z][A-Za-z'\-]*")


def norse_name(name):
    """The Runeheir name of a Ragnarok item name."""
    name = name.strip()
    if name in FULL:
        return FULL[name]
    for regex, replacement in _PHRASE_RES:
        name = regex.sub(replacement, name)

    def word(match):
        token = match.group(0)
        if token in DROP:
            return ""
        if token in PLACES:
            return PLACES[token]
        if token.endswith("'s") and token[:-2] in PLACES:
            return PLACES[token[:-2]] + "'s"
        return WORDS.get(token, token)

    name = _WORD_RE.sub(word, name)
    # Ragnarok's "T " prefix and " C" suffix marked alternate client sprites.
    name = re.sub(r"^T\s+", "", name)
    name = re.sub(r"\s+C$", " (Variant)", name)
    return re.sub(r"\s{2,}", " ", name).strip()
