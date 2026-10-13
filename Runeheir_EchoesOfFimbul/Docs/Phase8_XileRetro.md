# Phase 8: the XileRetro Database, Adapted to Norse Myth

Phase 8 brings everything XileRetro (the Ragnarok Online server played on XileRO) has into Runeheir: its items, cards,
monsters, maps, pets, mercenaries and costumes. Everything is renamed and rewritten for the Nine Worlds. It was done in
steps:

| Step | What | Status |
|---|---|---|
| 8.1 | **Equipment:** every weapon, armor, shield, garment, footgear, headgear and accessory | Done |
| 8.2 | **Cards**, named for their Norse monsters | Done |
| 8.3 | **Goods:** potions, food, boxes, scrolls, loot, ammunition, pet eggs and accessories | Done |
| 8.4 | **XileRetro's own items:** costumes (with costume slots), its custom gear, cards, pets and chests | Done |
| 8.5 | **The bestiary and the world:** 589 monsters with drops and skills, 322 maps, warps and spawns | Done |
| 8.6 | **Pets and mercenaries**, plus procedural music and sound | Done |
| 8.7 | Skills audit against Ragnarok's pre-renewal skill trees | Partly: ~200 Ragnarok skills map onto Runeheir's (auto-casts, scrolls, mercenary skills) |
| 8.8 | **Ragnarok's class companions:** duets, homunculi, mounts and the Ranger's raven | Done |
| 8.9 | **The War of the Realms** (Ragnarok's War of Emperium): guild sieges for five halls | Done |

The numbers below are what the importer produced last time it ran.

| Imported | Count |
|---|---|
| Weapons | 730 (635 from Ragnarok, 95 of XileRetro's own) |
| Armor, shields, garments, footgear, headgear, accessories | 1,174 (1,055 + 119) |
| Usable items (potions, food, boxes, scrolls, mercenary contracts, chests) | 1,140 |
| Loot and crafting goods | 1,286 |
| Ammunition | 46 |
| Pet eggs and pet accessories | 80 and 23 |
| Cards | 595 (519 + 76) |
| Costumes | 626 looks, made from XileRetro's 2,473 costumes |
| Monsters | 589 (37 MVPs, 71 mini-bosses) |
| Maps | 322 (25 towns), with 974 portals; 342 with Runeheir's own 20 (14, plus Iðavöllr and its five halls) |
| Pets | 75 (52 from Ragnarok, 23 of XileRetro's own) |
| Mercenaries | 41 (the Hirð's 30 fighters and 11 monster contracts) |

---

## 1. What changed in the game

### Gear (8.1)

- **1,904 pieces of equipment** joined the 116 Runeheir already had: every Ragnarok weapon class, from the Knife to Gungnir
  and the thunder-rods, and every kind of armor.
- **Two merchants in Vigrid Haven,** each with a stall west and east of the plaza:
  - **Eyvind's Weapon Smithy** sells what Ragnarok's town weapon dealers sell (84 weapons).
  - **Ragna's Armoury** sells what its armor dealers sell (38 pieces).
  The rest drops from the bestiary. GM commands also spawn anything: `@item xr1137`, just the Ragnarok id (`@item 1137`), or
  search with `@items <text>`.
- **Ragnarok's job rules:** each item keeps its Ragnarok job list, translated to Runeheir's jobs (§3). Transcendent-only
  ("Upper") gear needs a reborn character, and a few dresses need a particular gender.
- **Refine bonuses:** Ragnarok's "+1 INT per refine" and "at +9: ASPD +10%" scripts work (§4).
- **New bonus types** in the combat and stat engine:
  - damage against bosses or normal monsters
  - crit chance against a race
  - DEF or MDEF ignored against a race
  - magic damage against a race or element
  - damage taken from small, medium or large attackers
  - resistance to several statuses at once
  - HP/SP regeneration %, healing power, the SP cost of skills, ranged physical damage
  - Perfect Dodge, and bonus EXP and drop rates
  - skills that auto-cast when you attack, when you're hit or when you use a skill, and skills the gear grants
  - per-skill damage bonuses ("Sonic Blow +20%")
- **An ammunition slot:** arrows, shot and shuriken are worn as a stack. They add their ATK and element when they fit the
  weapon, and each shot spends one.
- **Tooltips** list the jobs that can use an item, "reborn only" where it applies, and any Ragnarok bonus Runeheir can't do
  yet, in grey under "Not yet in Runeheir". **85%** of the imported equipment works in full.

### Cards and goods (8.2, 8.3)

- **Cards** compound into sockets as Runeheir's Soul Cards always have. Each is named for its monster's Norse name
  ("Slimling Card"), and drops from that monster.
- **Usable items:** heals, percent heals, SP, cures, timed buffs, boxes that hold set items or a random one from Ragnarok's
  item groups, zeny pouches, and scrolls that cast a skill for free.
- **Butterfly Wings and Fly Wings** are Raven Feathers and Wind Rune Shards: Runeheir's own versions win where they exist.

### Costumes (8.4)

- **Four costume slots** under the equipment window (Q): Costume Upper, Mid, Lower and Garment. A costume shows over the
  gear in that place and changes only how you look: no stats, refines or cards.
- **626 costumes**, made from XileRetro's 2,473. Runeheir draws costumes from about 30 look templates in the costume's
  color: cat, fox and hare ears, halos, blossoms, ribbons, top hats, horns, crowns, circlets, völva's hats, hoods, helms,
  lenses, masquerade masks, head-wings, face veils, scarves, pipes, beards, wigs in 8 styles, valkyrie, fairy and bat wings,
  cloaks, tails, and auras that glow at the feet. XileRetro costumes that would look the same become one item; the others'
  ids point at it.
- **Two tailors in Vigrid Haven:** Gerðr's Hats & Ears (410 head costumes) and Ylfa's Wings & Auras (216). The Tailor's
  Mystery Box drops from bosses and holds any of them.
- **Other players see your costumes,** your wig and your aura online.

### XileRetro's own gear, cards and chests (8.4)

XileRetro's custom items exist only in its client, with their stats written out in English. The importer reads those
lines back into Ragnarok bonus script (§4), so they work like everything else.

- **214 pieces of custom gear** in six tiers: Godly, Uber, Elite, Sealed, Arena and Festival, plus a few untiered pieces.
  Their names are new ("Godly Greatsword of the Rimeheart"), since XileRetro's own names are often other games'.
  The strongest drop from MVPs near their level, the rest from mini-bosses.
- **76 custom cards.** Each drops from the monster it's named for, or from an MVP.
- **Six chests** fold XileRetro's mystery boxes into kinds:

  | Chest | Holds one of | Dropped by |
  |---|---|---|
  | Godly Hoard Chest | the Godly pieces | MVPs (1%) |
  | Elite Hoard Chest | the Elite pieces | MVPs (2%) |
  | Uber Hoard Chest | the Uber pieces | mini-bosses (1%) |
  | Tailor's Mystery Box | the 626 costumes | every boss (2%) |
  | Seiðr Card Pouch | the custom cards | mini-bosses (0.5%) |
  | Sealed Rune Box | the sealed cards | MVPs (0.5%) |

- **23 custom pets**, from XileRetro's monster eggs whose monster lives in Runeheir's world. Their eggs drop from their
  monster (0.1%); they eat Pet Food.
- Not imported: XileRetro's server-event and guild packages, ladder rewards, coins, quest items and streamer boxes (they
  only mean something on XileRO), and eggs of monsters Runeheir doesn't have.

### Monsters and the world (8.5)

- **589 monsters** from the maps Ragnarok spawns them on, with their drops and skills, under Norse names (the Poring is the
  Slimling, Baphomet the Hel-Goat, the Orc Hero the Troll Chieftain, Detardeurus Fáfnir).
- **Rescaled to Runeheir's levels 1–255.** Each stat keeps its ratio to Ragnarok's curve at that level, on Runeheir's own
  curve. Normal monsters stay within a quarter and three times their level's curve. Bosses take Runeheir's boss multiples
  (MVPs: 12× HP, 1.45× ATK, 25× EXP).
- **Monster bodies.** Each monster is drawn from one of 13 bodies, picked from its name and race, with a detail
  (variant) from its name:

  | Body | Who | Variants |
  |---|---|---|
  | Slime | the Poring family (Slimlings) | haloed and winged (Angeling), horned (Deviling), a floating wisp (Ghostring) |
  | Insect | ants, beetles | spiders (Argiope), scorpions, crabs (Vadon), mantises, horned beetles |
  | Plant | flowers and bulbs | flowers with petals, snapping flytraps (Mandragora, Rafflesia), walking trees, cacti |
  | Bird | chicks and fowl | tall striders (the Peco Peco) |
  | Fish | fish, squid | sharks, swordfish |
  | Beast (four legs) | wolves, bears, boars, goats, hares, turtles, frogs | tusks, horns, long ears, pointed ears and tail, a shell, a low body |
  | Humanoid, Golem, Wraith, Serpent, Flyer, Biped, Mushroom | as before | |

  Slimes squash and stretch as they hop, fish swim through the air, and mantises flutter their wings. Runeheir ▸ Art ▸
  Render Map Previews draws one of each (`Builds/Previews/bestiary_1.png`, `_2.png`).
- **Monster skills** fold Ragnarok's NPC_ skills into Runeheir's kinds: elemental and status strikes, bolts, telegraphed
  breaths and storms, self-buffs, heals, teleports and slave summons. Every boss calls for help, has a telegraphed area
  attack, and enrages at 50% (MVPs again at 20%).
- **322 maps:** Ragnarok's fields, dungeons and 25 towns, connected by its warps, all reachable from Vigrid Haven (Prontera).
  Each is generated from its Ragnarok size, spawns and warps into Runeheir's tile layouts.
- **Five new map looks,** each with its own palette, props and music:

  | Theme | Maps | Look |
  |---|---|---|
  | Desert | Sandmark's dunes (Morroc), Eldgjá's canyon (Veins) | Sand, sandstone, cacti, oases ringed by palms, bones, obelisks |
  | Jungle | Suðrey (Ayothaya), Vínland (Brasilis), Járnviðr (Umbala), Laufás (Splendide) | Broadleaf trees, palms, ferns, mushrooms, ruined pillars |
  | Coast | Sólvík's coast (Comodo), the Shell-Jarl's Isle (Turtle Island) | Sand, open sea along two edges, coves, palms, wrecks |
  | Volcano | Muspel's vents (Magma Dungeon), Thor's Volcano | Basalt cavern, lava pools, glowing seams, red haze |
  | Sand Tomb | Sandmark's Pyramid, the Riddle-Lion's Tomb (Sphinx), Suðrey's ruins | Sandstone halls, braziers, sarcophagi, urns |

### Pets, mercenaries and sound (8.6)

- **Pets:** use a taming item while you fight its monster (weaker monsters are easier; it's used either way), then use the
  egg to hatch the pet. J opens the pet window: feed it its food to raise intimacy, put on its accessory, or return it to
  its egg. A starving pet loses intimacy and runs away at zero. Once **Loyal** it grants Ragnarok's pet bonus. Other players
  see your pet and its accessory.
- **Mercenaries:** Thrand's **Hirð Hall** in Vigrid Haven sells the 30 contract scrolls: ten bowmen, ten spearmen and ten
  huscarls, grades 1 to 10. Each grade needs a base level. A contract runs for 30 minutes of play. The mercenary:
  - follows you
  - fights what you fight and whatever attacks you
  - uses its Ragnarok skills, folded into strikes, bolts, area attacks, guards, buffs on itself and you, and mending
  - counts its damage as yours (EXP, drops, MVP)

  It waits while you're down and comes with you through warps. R opens the mercenary window (HP, time left, kills,
  Dismiss). Monster contracts (the Mimic, the Maid Wight, Valkyrie Randgris…) come from rarer scrolls. Online, your
  mercenary's blows reach the realm's monsters as yours.
- **Music and sound,** all synthesized at runtime (nothing recorded):
  - Nordic folk loops for every map type, plus Desert, Volcano, Jungle and Coast moods
  - a drum-and-bass boss track
  - 44 sound effects
  - Music and Sound volume in the Escape menu

### Duets, homunculi, mounts and ravens (8.8)

- **Duets** (Ragnarok's ensembles). A Skald or Thul and a Seiðkona or Völva who both know the same duet, standing within
  3 m of each other, can play it together. Neither can move while it plays, and allies or foes inside the circle feel it.
  Either player starts it and pays its SP. There are eight:

  | Duet | Ragnarok's | Inside the circle |
  |---|---|---|
  | Lullaby of the Norns | Lullaby | Foes may fall asleep |
  | Mead of Plenty | Mr. Kim A Rich Man | Allies earn +20% EXP, +10% per level |
  | Ginnungagap | Eternal Chaos | Foes lose their DEF |
  | Drums of the Shield-Wall | A Drum on the Battlefield | Allies gain ATK and DEF |
  | Ring of Andvari | The Ring of Nibelungen | Allies gain +75 ATK, +25 per level |
  | Loki's Veil | Loki's Veil | Foes are silenced |
  | Mímir's Well | Into the Abyss | Allies' skills cost 30% less SP, 5% more per level |
  | Sigurd's Hide | Invulnerable Siegfried | Allies take 15% less damage, 3% less per level |

  Loki's Mimicry can't copy a duet.
- **Homunculi.** A Brewmaster brews an **Embryo** (Seed of Life, Morning Dew of Yggdrasil, a Glass Tube and a Medicine
  Bowl, all sold at Brokk's forge). **Weave Life** then turns it into one of four living companions, as the Norns choose:

  | Homunculus | Ragnarok's | Role | Skills (learned at levels 1, 15 and 40) |
  |---|---|---|---|
  | Líf, the Healer | Lif | Mends you | Healing Hands, Urgent Escape, Mental Charge |
  | Bjarki, the Bear-Guard | Amistr | Holds the line | Maul, Bulwark, Blood Lust |
  | Valfugl, the Swift | Filir | Fast strikes | Moonlight, Flitting, Diving Talon |
  | Gandr, the Wild Spirit | Vanilmirth | Seiðr | Caprice, Chaotic Blessings, Seiðr Storm |

  A homunculus keeps 10% of the EXP you earn while it's out, and grows to level 255. It's yours for good and is saved with
  you. If it falls, it rests for two minutes, unless a Lifeweaver wakes it with **Breath of Iðunn** (20% HP per level).
  R opens the **Companions** window, with your mercenary and your homunculus (Call / Rest).
- **Mounts.**
  - **Hestr's Stable** in Vigrid Haven sells the **Reins of the Fjord Horse** (150,000 zeny) to anyone. Riding: +25%
    movement speed, but you get down to fight. The reins are never used up.
  - The **Berserker line** rides **Sleipnir's Foal** (Horsemanship) and the **Guardian line** rides **Gullinbursti**, the
    golden boar (Boar Riding). These are Ragnarok's Peco Peco and Grand Peco: +36% speed and +1,000 weight, but −50% ASPD.
    Cavalry Mastery and Boar Mastery win back 10% of that per level. Knights fight from the saddle.
  - Other players see the mount.
- **Odin's ravens** (Ragnarok's falcon). **Huginn** perches on a Ranger's or Deadeye's shoulder. He swoops for Raven Strike
  and Raven Assault. On every auto-attack there's a LUK ÷ 3 % chance he strikes on his own (Ragnarok's auto Blitz Beat).

### The War of the Realms (8.9)

Ragnarok's War of Emperium, between the guilds of a realm (Host, Join or a dedicated server).

- **Where:** the road north of Vigrid Haven leads to **Iðavöllr**, the plain where the gods meet. Five halls stand there:

  | Hall | Lore |
  |---|---|
  | Bilskirnir | Thor's hall of five hundred and forty rooms |
  | Breiðablik | Baldr's hall, where nothing unclean can stand |
  | Glaðsheimr | Odin's bright home, roofed with gold |
  | Sessrúmnir | Freyja's many-seated hall on Fólkvangr |
  | Sökkvabekkr | Sága's hall, under cool waves |

  Each is a long Norse hall: a hearth down the middle, rows of pillars, banners, braziers either side of the Jarlstone's
  dais, and the jarl's high seat at the far end.
- **When:** at the realm's siege hours. By default that's Wednesdays and Saturdays at 20:00 for 60 minutes; `realm.json` sets
  `SiegeDays` ("Wed,Sat", "daily"…), `SiegeStartHour` and `SiegeMinutes` (0 turns the war off). Wars that start late run
  past midnight. The realm announces the start and end, and tells each player on login when the next war is and who
  holds what.
- **How:**
  - Each hall has a **Jarlstone** (Ragnarok's Emperium) with 250,000 HP and heavy DEF.
  - Only a member of a guild that doesn't hold the hall can strike it, and only while the war is on.
  - The guild whose member breaks it takes the hall. The whole realm hears of it, and everyone else in the hall is sent
    home.
  - The stone stands again moments later for the next challenger.
  - The realm checks guild membership itself and never trusts what a client claims.
- **The prize:** three **Castle Hoards** gather in each hall every six hours. Only the holders can open them. They hold
  Elite, Uber and Godly Hoard Chests and Tailor's Mystery Boxes.
- **Records:** the realm saves owners in `realm_sieges.json` next to its guilds. A disbanded guild's halls stand empty.
- **GM:** `@siege start`, `@siege stop` and `@siege auto` (back to the schedule) need GM commands on; `@siege status` is for
  anyone.

### Item icons

Every item now shows a pictogram instead of letters. There are 47 shapes (blades, bows, flasks, scrolls, helms, rings,
cards, eggs, chests, wings…), drawn at runtime from signed-distance shapes and tinted with the item's color. Runeheir ▸ Art
▸ Render Map Previews writes the sheet (`Builds/Previews/icons_sheet.png`) along with map and costume previews.

## 2. Names and descriptions

Ragnarok already borrows a lot from Norse myth (Mjolnir, Sleipnir, Valkyrie, Brynhild), so those names stay, with Old Norse
spellings where they differ. What changes:

| Kind | Ragnarok | Runeheir |
|---|---|---|
| Jobs | Novice, Swordman, Mage, Archer, Acolyte, Merchant, Thief | Initiate, Warrior, Mystic, Huntsman, Devotee, Trader, Scout |
| Legend blades | Excalibur, Masamune, Muramasa, Byeollungum, Tirfing | Hofud, Skofnung, Hviting, Dáinsleif, Tyrfingr |
| Other legends | Gae Bolg, Brionac, Longinus's Spear, Rudra Bow, Krishna | Gunnar's Atgeir, Lævateinn, Spear of the Hanged God, Ullr's Bow, Sumarbrandr |
| Ragnarok's people | Goibne's, Morpheus's, Morrigane's, Hollgrehenn's, Serin's | Völundr's, Mímir's, Hildr's, Eitri's, Sif's |
| Monsters | Poring, Deviruchi, Baphomet, Orc, Munak, Bongun, Succubus, Ifrit | Slimling, Myrkling, Hel-Goat, Troll, Draugmey, Draugsveinn, Mara, Surtr |
| Towns | Prontera, Payon, Morroc, Comodo, Moscovia, Brasilis | Vigrid Haven, Ýdalir, Sandmark, Sólvík, Garðaríki, Vínland |
| Events | Santa, Christmas, Valentine, Anniversary | Yule, Yule, Freyja's Day, Jubilee |
| Firearms | Revolver, Rifle, Shotgun, Gatling Gun | Thunder-Rod, Long Rod, Scatter Rod, Storm Rod |
| Mercenaries | Mina, David, Rodin… | Ylva the Bowman, Leif the Huscarl, Bjorn the Spearman… |

Ordinary English item words (Sword, Ribbon, Bucket Hat) keep their names. Where Ragnarok has several items of one name, the
slotted ones carry Ragnarok's own `[n]` socket count ("Sword [3]"), and true duplicates get a numeral ("Light Eihwaz II").
The full rename list is in [report_equipment.md](../Tools/ItemImport/report_equipment.md).

**Descriptions are new.** Each is a line of Norse flavor plus the item's rules, written out from its effects:
"A seax-length blade, quick in the hand. DEX +3, +10% damage vs Demi-Humans. Per refine: ATK +5." No Ragnarok or XileRO
description text is used.

**Runeheir's own versions win.** When an imported item would share a name with one of Runeheir's own items, the
unslotted one becomes an alias: its id points at Runeheir's item (the Valkyrian set, Megingjard, Brisingamen, Buckler,
Mirror Shield, Sandals, and more).

## 3. Stats and rules

| Ragnarok | Runeheir |
|---|---|
| Weapon ATK, weapon level, MDEF, sockets, required level | the same; levels map 1–99 → 1–255 for monsters |
| Armor DEF | ×10, since Runeheir's DEF curve is renewal's |
| Weight (database units) | ÷10, as Ragnarok's client shows it |
| Buy price | the shop price; merchants pay half |
| Item ids | `xr` + the Ragnarok id (`xr1201` is the Knife), so saves survive renames |
| Monster ids | `xrm` + the Ragnarok id; maps `xr_` + the Ragnarok map (Prontera is `vigrid_haven`) |
| `bAtkEle` / `bDefEle` | the weapon's or armor's element |
| `bonus bStr` … `bonus3 bAutoSpell` | the matching Runeheir bonus (§1) |

**Jobs.** Ragnarok's class list maps onto Runeheir's jobs. A second class covers both its normal and transcendent jobs
unless the item is "Upper" (transcendent) only.

| Ragnarok | Runeheir | Ragnarok | Runeheir |
|---|---|---|---|
| Novice | Initiate | Super Novice | Wanderer |
| Swordman | Warrior | Knight | Berserker, Einherjar |
| Crusader | Guardian, Valkyrie | Mage | Mystic |
| Wizard | Sorcerer, Archmage | Sage | Sage, Chronomancer |
| Archer | Huntsman | Hunter | Ranger, Deadeye |
| Bard / Dancer | Skald, Seidkona, Thul, Völva | Acolyte | Devotee |
| Priest | Gothi, High Gothi | Monk | Monk, Champion |
| Merchant | Trader | Blacksmith | Runesmith, Forgelord |
| Alchemist | Brewmaster, Lifeweaver | Thief | Scout |
| Assassin | Assassin, Shadow Walker | Rogue | Outlaw, Vargr |
| Taekwon | Glíma Fighter | Star Gladiator | Sól Guardian |
| Soul Linker | Fylgja Caller | Gunslinger | Thunderer |
| Ninja | Nightraider | Summoner | Freyja's Kin |

Runeheir's own weapon rules still apply on top. A job can only wield the weapon types its guild teaches.

## 4. Item scripts

Each Ragnarok item carries a small script (`bonus bInt,getrefine()-5; if (getrefine()>8) bonus bInt,5;`). The importer
runs every script once for each refine level from +0 to +10, then compares the results:

- the value at +0 becomes the item's effect
- a step that repeats on most refine levels becomes a per-refine bonus
- every other change becomes a bonus from that refine level on

Ragnarok's scripts stop at +10, so from +11 to +20 an imported item keeps its +10 bonuses (`PerRefineLimit`).
Runeheir's own refine ATK and DEF keep growing.

Conditions on the wearer become an `EquipCondition` when they are a base stat check: the Doom Slayer's "ATK +340 with base
STR 95". Other conditions, such as the wearer's job or a learned skill, become tooltip notes.

**XileRetro's own items** have no script, only English. `custom_gear.py` reads the description's effect lines back into
script:

| Description says | Becomes |
|---|---|
| "STR +15, VIT +10" / "DEX, VIT, LUK +5" / "All Stats +10" / "+17 across all stats" | `bonus bStr,15; …` / `bAllStats` |
| "Max HP +7%", "Max HP +2500", "MATK +5%", "ASPD +10%", "Critical +40" | `bMaxHPrate`, `bMaxHP`, `bMatkRate`, `bAspdRate`, `bCritical` |
| "Reduces damage taken from Fire property enemies by 20%" | `bonus2 bSubEle,Ele_Fire,20` |
| "Increase damage inflicted upon all enemies by 3%" | `bonus2 bAddRace,RC_All,3` |
| "Reduce damage from Long Range attacks by 5%" | `bonus bLongAtkDef,5` |
| "Critical Rate + 10 on Brute enemies" | `bonus2 bCriticalAddRace,RC_Brute,10` |

Combo sections ("[+ Traveler Hat]", "If … is equipped") and job-only sections ("[Bard]") are skipped. Lines it can't read
are dropped.

## 5. Re-running the importer

The importer is plain Python. It runs on the Python bundled with Blender (there is no system Python):

```bash
"C:/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe" -I Tools/ItemImport/import_items.py
```

| File | Role |
|---|---|
| `ro_parse.py` | Reads rAthena's YAML databases and XileRetro's `System/ItemInfo.lub` |
| `names_norse.py`, `creatures_norse.py`, `skills_norse.py` | The rename tables: items, monsters, skills |
| `script_translate.py` | The item-script interpreter and the bonus → Runeheir translation and English |
| `goods.py` | Usable items, loot, ammunition, eggs and pet accessories |
| `bestiary.py`, `monster_skills.py` | Monsters: stat scaling, looks, drops, skills, boss phases |
| `world.py` | Maps, towns, warps, spawns and roads |
| `pets.py`, `mercenaries.py` | Pets (pet_db) and mercenaries (mercenary_db) |
| `costumes.py`, `custom_gear.py` | XileRetro's costumes, and its own gear, cards, eggs and chests |
| `import_items.py` | Runs everything and writes the generated C# and the report |
| `Scripts/Core/*/Generated/*.g.cs` | The generated catalogs (committed; don't edit by hand) |
| `report_equipment.md` | Coverage, aliases, inactive bonuses, every rename |

Paths to the data can be overridden with `RH_RATHENA_DB` (rAthena's `db/pre-re`) and `RH_XILERETRO_SYSTEM` (the client's
`System` folder).

The output is the same on every run. Python salts its string hashes per process (and `-I` ignores `PYTHONHASHSEED`), so
anything the importer picks out of a set breaks ties by id. The roads that join unreachable maps to a town do this, for
example.

## 6. A note on the source data

The stats come from rAthena's pre-renewal database (GPL-3.0), which records Gravity's original Ragnarok values, and from
XileRetro's client descriptions. Runeheir uses its own names, descriptions, art, models and sound, but the numbers
themselves come from those sources. Have them reviewed legally before a commercial release.

## 7. Tests

| Tests | Covers |
|---|---|
| `XileDatabaseTests` | Import size, ids, names, renames, aliases, dealers, job lists, refine formulas, conditions, bonuses, ammo, old saves |
| `BestiaryTests`, `WorldTests` | Monster scaling, drops and skills; every map's layout connects its portals, NPCs and spawns |
| `Phase8VisualTests` (Editor), bodies | Every monster body and variant builds; the Poring is a slime, the Peco Peco a tall bird, Argiope a spider |
| `PetTests` | Pets point at real items and monsters; taming, hatching, feeding, starving, the Loyal bonus, accessories, saves |
| `MercenaryTests` | The Hirð's ten grades of three roles; scrolls, the Hirð Hall, level gates, contracts, saves, mending |
| `CostumeTests` | Costume slots, looks, the tailors, save repair, look-alike aliases, custom gear, cards and pets |
| `CompanionTests` | Duets for both sides, partners, Mimicry; Weave Life, the Embryo recipe, the four homunculi, leveling, saves |
| `MountTests` | Class mounts per line, riding speed and the mastery, saved mounts, the reins at Hestr's Stable |
| `SiegeTests` | The siege schedule (and past midnight), who strikes a Jarlstone or opens a hoard, captures, disbanding, the halls |
| `AudioTests` | Music loops and sound effects |
| `Phase8VisualTests` (Editor) | Every icon shape draws; items get fitting icons; costumes, wigs and auras in the look code |
