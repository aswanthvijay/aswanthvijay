# RUNEHEIR — Phase 4 Guide: Loot, Inventory & 4-Slot Card Compounding

Phase 4 turns the Phase 3 combat sandbox into a looter. Every character now wears real gear on a 10-slot paperdoll. Monsters drop loot, ores, rare gear and their own Soul Card. Three town NPCs by the save point buy and sell, refine, carve runes and store your items.

| GDD item | Where it lives |
|---|---|
| §5: 87 base items and the 10-slot paperdoll | `Scripts/Core/Items/ItemCatalog.Weapons.cs`, `ItemCatalog.Gear.cs`, `Equipment.cs`; window `UI/Hud/EquipmentWindow.cs` (**Q**) |
| §6: 35 Soul Cards and 4-slot compounding | `Scripts/Core/Items/ItemCatalog.Cards.cs`, `CardRules.cs`; window `UI/Hud/CardCompoundWindow.cs` |
| §7: Runestones, Runic Fuller etching, Preservation and Extraction | `Scripts/Core/Combat/Buffs.cs`, `Items/RunewordRules.cs`, `Items/RefineRules.cs`; window `UI/Hud/ForgeWindow.cs` |
| Card and gear effects in combat | `Items/EquipEffect.cs`, `EquipmentStats` in `Items/Equipment.cs`, `Combat/DamageCalculator.cs`, `Runtime/Player/PlayerCharacter.cs`, `Runtime/Combat/CombatEntity.cs` |
| Loot tables, Dead and Blood Branches | `Scripts/Core/Monsters/MonsterCatalog.cs`, `Runtime/Combat/Monster.cs` |
| Shops, selling, account storage | `Scripts/Core/Items/Trade.cs`, `Core/Accounts/AccountStore.cs`; windows `ShopWindow.cs`, `StorageWindow.cs` |
| Town NPCs | `Runtime/Field/NpcActor.cs` (spawned next to the save point) |

---

## 1. Try it in 2 minutes

1. Download the branch again, open the project in Unity 6.3, run **Runeheir ▸ Setup ▸ Build Prototype Scenes**, then press **Play** in `RH_Login`.
2. Enter the field with any character. Phase 2 and Phase 3 characters load fine: they now really hold their job's weapon, and nothing else changes.
3. Press **Q** for the **Equipment** window and **E** for the **Items** window (tabs: Items · Gear · Etc). New characters carry a Cotton Tunic and Sandals: double-click them to wear them.
4. Walk to the three NPCs standing near the save point (click one to walk over and talk):
   - **Ásta [Trading Post]:** buy potions, Dead Branches and starter gear; sell anything.
   - **Brokk [Dwarven Forge]:** refine, carve runewords, extract cards, buy ores and runes.
   - **Verdandi's Courier [Norn Storage]:** storage shared by every character on your account.
5. A fast test run (press **Enter**, then type):
   - `@zeny 5000000`
   - `@job berserker`, then `@blvl 120`
   - `@item iron_claymore`, `@item sea_drake_card`, `@item starmetal 10`, `@item dwarven_steel 10`, `@item rune_of_preservation 3`
   - Double-click the Iron Claymore to wear it. Double-click the Sea Drake Card, choose the claymore, then confirm.
   - Talk to Brokk, choose **Refine equipment**, pick the claymore and hammer it past +6.
   - `@monster jotun_brawler`: your +20% vs Large shows in the damage numbers.

---

## 2. The paperdoll

There are ten positions: Upper, Mid and Lower Headgear, Armor, Weapon, Shield, Garment/Wings, Footgear, and Accessory 1 and 2.

- **Wear:** double-click (or right-click) a piece in the inventory, or put it on an F-key and press the key. **Take off:** double-click the piece in the Equipment window.
- **Clashes:**
  - Two-handed weapons (greatswords, spears, bows, katars, two-handed staves) also fill the Shield slot. Equipping a shield takes the two-handed weapon off, and the reverse.
  - Full helms (the Iron Helm) cover Upper and Mid.
  - A third accessory replaces the first.
  - Anything taken off goes back to your bag. If the bag can't hold it, the swap is refused, so nothing is ever lost.
- **Requirements** (shown in red in tooltips):
  - Base Level.
  - Job weapons: Initiates use daggers, one-handed swords, staves and maces. Each job keeps its own list (see the **Who** column below).
  - Job tier: Valkyrian gear and the tier-4 weapons are for Ascended jobs.
  - Job line: the Archmage Wizard Hat is Mystic line only.
- **Job changes hand you the job's weapon.** The new job's starter weapon goes into your bag. It is equipped straight away if your hands are empty, if your old weapon is just an earlier job's plain starter weapon, or if the new job can't use it. A weapon you refined, carded or etched stays in your hands. Gear the new job can't use goes back to the bag.
- **Gear shows on your character:** the weapon, upper, mid and lower headgear, shields, capes, scarves and the four animated wings, which beat slowly while you walk. The character-select preview shows it too.
- **The Equipment window** also sums up your gear:
  - weapon element and armor element;
  - active Runeword and status immunities;
  - crit damage, life steal, reflect, DEF and MDEF bypass, and cooldown changes.

## 3. Inventory

- **Tabs:** Items (consumables), Gear (equipment), Etc (cards, ores, glyphs, loot). There are 35 slots per page; use the arrows to turn pages. The bag holds 200 entries.
- **Stacks and pieces:** equipment never stacks. Every piece is its own item with its own refine, cards and glyphs, so two Iron Claymores are two entries. A green **+7** badge shows refine, and **◆** shows each compounded card.
- **Double-click:** uses a consumable, wears gear, or (for a Soul Card) opens the compound window. While the storage is open, it stores the item instead.
- **Weight:** worn gear counts toward your carry weight. Loot you can't carry, or that doesn't fit in a full bag, stays on the ground (chat tells you).

## 4. Town services

All three NPCs stand a few meters from the save point. Click one to walk over. Walking more than 7 m away, or dying, closes their windows.

### Ásta's Trading Post
- **Buy:** double-click an item; the amount box sets how many you get.
- **Sell:** double-click; everything in your bag sells for **half its price**. Every gear sale asks for confirmation first (and warns when refine, cards or glyphs would go with it). Worn gear isn't for sale.

### Brokk's Dwarven Forge
- **Refine equipment, Runic Fuller, Extract Soul Cards** (sections 5–7), and a supply shop for ores, Runes of Preservation and Extraction, the six glyphs, and the Thurisaz, Isa and Hagalaz runestones.
- **Worn gear can be worked on directly.** Brokk takes the piece off, works it, and puts it back on if it survived.

### Norn Storage (Verdandi's Courier)
- **Shared storage:** 300 entries, shared by **every character on your account**.
- **Moving items:**
  - With the storage open, double-click items in your bag to store them, and double-click stored items to take them out.
  - The amount box limits a stack. Leave it empty to move the whole stack.
- **Saving:** every move saves your character and the storage **in one write**. If the write fails (disk full, file locked), the move is undone on screen, so an item can never be duplicated or lost.

## 5. Refining (+0 to +20)

| | Weapon Lv 1 | Weapon Lv 2 | Weapon Lv 3 | Weapon Lv 4 | Armor, shield, garment, footgear, upper headgear |
|---|---|---|---|---|---|
| Safe up to (always succeeds) | +7 | +6 | +5 | +4 | +4 |
| Ore per attempt | Bog Iron | Dwarven Steel | Starmetal | Starmetal | Skystone |
| Zeny per attempt | 50 | 200 | 5,000 | 20,000 | 2,000 |
| Bonus per + | +2 ATK | +3 ATK | +5 ATK | +7 ATK | +3 DEF |

- **Past the safe limit:** the first step is 60%, then 40, 40, 20, 20, 15, 15, 12, 12, 10, 10, 8, 8, 6, 6, 5, 5%.
- **A failure shatters the piece, and every card in it, for good.** The forge asks before a risky attempt.
- **Rune of Preservation** (GDD §7): tick the box in the forge. A failure then drops the piece by 1 (for example +9 to +8) instead of shattering it. The rune is only used up on attempts that can fail.
- **Can't be refined:** accessories, mid and lower headgear, and the God Relics.

## 6. Soul Cards (4-slot compounding)

- **Where cards come from:** every Phase 4 field monster drops its own card at the GDD rate of **0.5–1%**. The server's ×5 item rate doesn't apply to cards; `RuneheirSettings ▸ cardDropRate` can raise it. Card drops are announced in chat. **Cards can never be stolen with Pilfer** (Ragnarok rule).
- **Sockets:** weapons have 1–4 sockets (lower-tier weapons have more). Some armor, shields, garments, footgear, headgear and accessories have one.
- **Compounding:** a card only fits its family (shown in brackets in its description). Double-click the card, pick a piece (worn pieces are listed too), and confirm. It's permanent…
- **Extracting:** …unless you buy a **Rune of Extraction** (150,000 z). It pulls every card back into your bag and keeps the piece and its refine.
- **What's live in combat:** every card effect in the table below works in Phase 4:
  - race, size and element damage;
  - damage taken by attacker race or element;
  - armor element changes (Frost Wolf Water, Hel's Vanguard Ghost, which cuts Neutral damage to 25%);
  - status immunities and extra resistances;
  - life and SP leech;
  - melee reflect;
  - DEF and MDEF bypass;
  - crit damage;
  - shorter cooldowns;
  - uninterruptible casting;
  - the base-STR condition on Jotun Brawler;
  - the on-hit procs:
    - Toxic Spore poison;
    - Jormungandr's Brood frostbite;
    - Hel's Executioner Mortal Stagger;
    - Fenrir's Berserk Wolf Form: CRIT doubled and no stagger for 10 s.
  - Ancient Golem's "unbreakable" is stored and shown, but nothing breaks weapons until Phase 5's monster skills.

## 7. Runes (GDD §7)

### Runic Fuller (Brokk ▸ Runic Fuller)
- **Grooves:** every weapon has 2 grooves. Pick a groove, then a glyph, for 10,000 z per carving. Carving over a glyph destroys the old one (the forge asks first).
- **Runewords:** two matching glyphs form a Runeword (order doesn't matter):
  - **Blade of Dawn** (Sowilo + Tiwaz): Holy weapon, +15% vs Undead and Demons, +5 CRIT.
  - **Glacial Shroud** (Isa + Hagalaz): Water weapon, 5% chance to Freeze for 3 s, −10% Fire damage taken.
  - **Giant's Cleave** (Thurisaz + Uruz): +20% vs Large, +50% poise damage, +5 STR.
- **Getting glyphs:** Brokk sells all six for 8,000 z each. Frost Wolves drop Isa and Jotun Brawlers drop Thurisaz.

### Combat runestones
All six GDD runestones work. Phase 4 adds the missing three to Brokk's shop:
- **Thurisaz:** triples poise damage for 30 s.
- **Isa:** a 5,000-HP glacial shield for 30 s. Melee attackers who hit it are frozen for 3 s, even on the blow that breaks it. Fully soaked hits show "Absorbed".
- **Hagalaz:** rebounds 25% of spell damage to the caster for 30 s. It applies to any spell that hits you; monsters start casting in Phase 5.

## 8. Branches and loot

- **Dead Branch** (Ásta sells it, Forest Imps drop it): summons a random field monster below Lv 70 that hunts you at once.
- **Blood Branch** (Jotun Brawler drop): summons one from the strongest tier (Lv 70+). Phase 5 swaps in the real MVPs.
- **Monster drops:** besides consumables and their card, every monster drops its own loot item (sell it to Ásta), refine ores and a rare piece of gear (full table below).

---

## 9. All items, cards, shops and drops

These tables are generated from the catalogs. **Who** lists the jobs that can use a weapon ("Monk+" means Monk and the jobs after it); "tier 2+" and "Ascended" are extra job-tier requirements.

### Weapons (28)

| Weapon | Type | Wpn Lv | ATK | Sockets | Base Lv | Who | Notes |
|---|---|---|---|---|---|---|---|
| Rusty Seax | Dagger | 1 | 17 | 3 | 1 | Initiate, Warrior, Scout, Mystic | Every Initiate's first blade. |
| Seax | Dagger | 2 | 64 | 2 | 1 | Initiate, Warrior, Scout, Mystic | A sharp single-edged knife. |
| Frost Stiletto | Dagger | 3 | 105 | 1 | 55 | Initiate, Warrior, Scout, Mystic | Rimed with Niflheim frost. Water element. |
| Iron Claymore | Greatsword (2H) | 2 | 100 | 3 | 1 | Warrior | A plain but honest greatsword. |
| Steel Claymore | Greatsword (2H) | 3 | 150 | 2 | 40 | Warrior | Dwarf-forged steel, balanced for wide swings. |
| Flamberge | Greatsword (2H) | 3 | 160 | 2 | 1 | Warrior; tier 2+ | A wavy blade that tears as it cuts. |
| Muspel Flamberge | Greatsword (2H) | 4 | 215 | 1 | 120 | Warrior | Forged in Muspelheim's fire. Fire element. |
| Einherjar Greatsword | Greatsword (2H) | 4 | 230 | 1 | 1 | Warrior; Ascended | Carried out of Valhalla by the chosen dead. |
| Iron Spear | Spear (2H) | 1 | 60 | 3 | 1 | Warrior, Paladin+ | A long iron-tipped spear. |
| Rune Lance | Spear (2H) | 3 | 150 | 2 | 1 | Warrior, Paladin+; tier 2+ | Runes run the length of its shaft. |
| Valkyrian Lance | Spear (2H) | 4 | 220 | 1 | 1 | Warrior, Paladin+; Ascended | A Valkyrie's spear. +3 VIT, +5% Max HP. |
| Iron Mace | Mace | 2 | 75 | 2 | 1 | Initiate, Warrior, Devotee | A flanged iron mace. |
| Holy Mace | Mace | 3 | 130 | 1 | 1 | Initiate, Warrior, Devotee; tier 2+ | Blessed in Vigrid Haven. Holy element, +10% vs Undead. |
| Templar Mace | Mace | 4 | 200 | 1 | 1 | Initiate, Warrior, Devotee; Ascended | Holy element, +15% vs Undead and Demons. |
| Oak Wand | Staff | 1 | 25 | 3 | 1 | Initiate, Mystic, Devotee | A runed oak twig. +15 MATK, +1 INT. |
| Runed Oak Wand | Staff | 2 | 40 | 2 | 24 | Initiate, Mystic, Devotee | +40 MATK, +2 INT. |
| Runed Staff | Staff (2H) | 2 | 45 | 2 | 1 | Initiate, Mystic, Devotee; tier 2+ | +60 MATK, +3 INT. |
| Rune Tome Staff | Staff (2H) | 2 | 50 | 2 | 1 | Initiate, Mystic, Devotee; tier 2+ | A staff bound with a book of runes. +55 MATK, +5% Max SP. |
| Yggdrasil Staff | Staff (2H) | 4 | 80 | 1 | 1 | Initiate, Mystic, Devotee; Ascended | A living branch of the World Tree. +160 MATK, +5 INT. |
| Norn Staff | Staff (2H) | 4 | 85 | 1 | 1 | Initiate, Mystic, Devotee; Ascended | Cut from the Norns' well-side ash. +140 MATK, 5% faster casting. |
| Hunter's Bow | Bow (2H) | 1 | 40 | 3 | 1 | Scout | A short hunting bow. |
| Yew Longbow | Bow (2H) | 3 | 110 | 2 | 1 | Scout; tier 2+ | A tall yew bow. +2 DEX. |
| Raven Longbow | Bow (2H) | 4 | 180 | 1 | 1 | Scout; Ascended | Fletched with raven feathers. +4 DEX, +10 HIT. |
| Wolf Claws | Knuckles | 2 | 70 | 2 | 20 | Monk+ | Iron claws strapped over the knuckles. |
| Iron Knuckles | Knuckles | 3 | 110 | 2 | 1 | Monk+; tier 2+ | Heavy studded knuckles. |
| Fist of Odin Knuckles | Knuckles | 4 | 180 | 1 | 1 | Monk+; Ascended | Gold-banded knuckles. +3 STR. |
| Jamadhar | Katar (2H) | 3 | 120 | 2 | 1 | Assassin+; tier 2+ | Punch-blades for close killing. +5 CRIT. |
| Shadow Katar | Katar (2H) | 4 | 190 | 1 | 1 | Assassin+; Ascended | Drinks the light around it. Shadow element, +10 CRIT. |

### Headgear (24)

| Item | Slot | DEF | MDEF | Sockets | Base Lv | Who | Effect |
|---|---|---|---|---|---|---|---|
| Valkyrie Winged Helm | Upper | 18 | 5 | 1 | 60 | All | Silver wings sweep back from the temples. +2 AGI, +1 VIT. |
| Grand Horned Viking Crest | Upper | 25 | 0 | 1 | 70 | All | Two great horns. +3 STR, +3% Max HP. |
| Archmage Wizard Hat | Upper | 8 | 15 | 1 | 50 | Mystic line | Mystic line only. +3 INT, +3% magic damage. |
| Feathered Beret | Upper | 6 | 0 | 1 | 30 | All | A jaunty beret. +2 DEX, +5 HIT. |
| Iron Helm | Upper+Mid | 22 | 0 | 1 | 40 | All | A full nasal helm covering upper and mid head. |
| Wolf Hood | Upper | 10 | 0 | 0 | 25 | All | A wolf's head worn as a hood. +2 AGI, +3 FLEE. |
| Raven Hood | Upper | 8 | 2 | 0 | 20 | All | Black feathers. +1 INT, +1 DEX. |
| Antler Crown | Upper | 12 | 0 | 1 | 40 | All | Shed antlers bound in gold. +2 VIT, +200 Max HP. |
| Fur Cap | Upper | 7 | 0 | 0 | 1 | All | Warm against Fimbulwinter. Take 5% less Water damage. |
| Bandana | Upper | 3 | 0 | 0 | 1 | All | A strip of red cloth. |
| Rune Circlet | Upper | 6 | 6 | 1 | 30 | All | A thin band of runes. +2 INT. |
| Flower Crown | Upper | 2 | 3 | 0 | 1 | All | Meadow flowers. +2 LUK. |
| Frost Crown | Upper | 15 | 10 | 1 | 90 | All | Ice that never melts. Take 15% less Water damage. |
| Helm of Awe | Upper+Mid | 35 | 8 | 1 | 150 | All | Ægishjálmur. +2 all stats, +30% poise. |
| Slotted Dark Sunglasses | Mid | 1 | 0 | 1 | 10 | All | Immune to Blind. |
| Runed Blindfold | Mid | 2 | 5 | 0 | 30 | All | See with the inner eye. +2 INT, 3% faster casting. |
| Eyepatch | Mid | 1 | 0 | 0 | 1 | All | +3 HIT, +2 CRIT. |
| Rune Monocle | Mid | 1 | 0 | 0 | 20 | All | +2 DEX. |
| Frost Goggles | Mid | 3 | 0 | 0 | 40 | All | Take 5% less Water damage; +20% resistance to Freeze. |
| Viking Pipe | Lower | 1 | 0 | 0 | 1 | All | +1 VIT, +2 SP regen. |
| Fang Mask | Lower | 3 | 0 | 0 | 20 | All | A wolf's jaw. +1 STR, +5 ATK. |
| Beard of Odin | Lower | 2 | 3 | 0 | 60 | All | Wisdom grows on the chin. +2 INT, +5% Max SP. |
| Iron Mouthguard | Lower | 4 | 0 | 0 | 30 | All | +1 VIT, +20% resistance to Poison. |
| Braided Beard | Lower | 2 | 0 | 0 | 10 | All | +1 STR, +1 VIT. |

### Armor (7)

| Item | Slot | DEF | MDEF | Sockets | Base Lv | Who | Effect |
|---|---|---|---|---|---|---|---|
| Runic Full Plate | Armor | 120 | 0 | 1 | 70 | All | Plate etched with warding runes. +5% Max HP. |
| Valkyrian Armor | Armor | 150 | 15 | 1 | 99 | Ascended | Ascended only. +5% Max HP and Max SP. |
| Cotton Tunic | Armor | 10 | 0 | 1 | 1 | All | Simple homespun cloth. |
| Leather Jerkin | Armor | 25 | 0 | 1 | 15 | All | Boiled leather. |
| Chainmail | Armor | 60 | 0 | 1 | 40 | All | Riveted iron rings. |
| Mystic Robe | Armor | 30 | 15 | 1 | 30 | All | Woven with runic thread. +2 INT. |
| Saint's Robe | Armor | 45 | 10 | 1 | 45 | Devotee line | Devotee line only. Take 10% less Shadow damage. |

### Shields (5)

| Item | Slot | DEF | MDEF | Sockets | Base Lv | Who | Effect |
|---|---|---|---|---|---|---|---|
| Round Viking Shield | Shield | 30 | 0 | 1 | 20 | All | Painted linden boards. +1 VIT. |
| Valkyrian Shield | Shield | 80 | 10 | 1 | 99 | Ascended | Ascended only. Take 15% less Fire, Water, Shadow and Undead damage. |
| Buckler | Shield | 15 | 0 | 1 | 1 | All | A small round shield. |
| Kite Shield | Shield | 50 | 0 | 1 | 50 | All | A tall teardrop shield. |
| Mirror Shield | Shield | 40 | 15 | 1 | 60 | All | Polished silver. Reflects 5% of magic damage. |

### Garments and wings (9)

| Item | Slot | DEF | MDEF | Sockets | Base Lv | Who | Effect |
|---|---|---|---|---|---|---|---|
| Valkyrian Feather Wings | Garment | 18 | 5 | 1 | 90 | All | White wings that beat softly. +3 AGI, +5% movement speed. |
| Demon Nether Wings | Garment | 18 | 5 | 1 | 90 | All | Leathery wings of Niflheim. +3 STR, take 10% less Holy damage. |
| Fimbul Frost Wings | Garment | 18 | 8 | 1 | 90 | All | Wings of the endless winter. +3 INT, take 10% less Water damage. |
| Dragon Flame Wings | Garment | 18 | 5 | 1 | 90 | All | Ember-edged wings. +3 VIT, take 10% less Fire damage. |
| Wolfskin Mantle | Garment | 15 | 0 | 1 | 30 | All | Take 5% less Neutral damage. |
| Valkyrian Manteau | Garment | 35 | 5 | 1 | 99 | Ascended | Ascended only. +10 FLEE, take 10% less Neutral damage. |
| Traveler's Cloak | Garment | 8 | 0 | 1 | 1 | All | Road-worn wool. |
| Bear Pelt | Garment | 20 | 0 | 1 | 40 | All | +1 VIT. |
| Rune Muffler | Garment | 12 | 5 | 1 | 25 | All | Take 3% less Neutral damage. |

### Footgear (6)

| Item | Slot | DEF | MDEF | Sockets | Base Lv | Who | Effect |
|---|---|---|---|---|---|---|---|
| Plated Greaves | Footgear | 30 | 0 | 1 | 60 | All | +5% Max HP. |
| Valkyrian Boots | Footgear | 40 | 0 | 1 | 99 | Ascended | Ascended only. +10% movement speed, +5% Max HP. |
| Sandals | Footgear | 6 | 0 | 1 | 1 | All | Leather straps. |
| Leather Boots | Footgear | 12 | 0 | 1 | 15 | All | Sturdy boots. |
| Fur Boots | Footgear | 22 | 0 | 1 | 40 | All | +3% Max HP. |
| Swift Boots | Footgear | 18 | 0 | 1 | 70 | All | +1 AGI, +5% movement speed. |

### Accessories and God Relics (8)

| Item | Slot | DEF | MDEF | Sockets | Base Lv | Who | Effect |
|---|---|---|---|---|---|---|---|
| Clip Ring | Accessory | 0 | 0 | 1 | 1 | All | A plain ring with a socket. |
| Rune Ring | Accessory | 0 | 0 | 0 | 20 | All | +2 INT. |
| Wolf Tooth Necklace | Accessory | 0 | 0 | 0 | 20 | All | +2 STR. |
| Raven Brooch | Accessory | 0 | 0 | 0 | 20 | All | +2 AGI. |
| Ward Amulet | Accessory | 0 | 0 | 0 | 20 | All | +2 VIT. |
| Megingjard | Accessory | 0 | 0 | 0 | 150 | All | Thor's belt of strength. +40 STR; +1 ATK per 2 Base Levels. |
| Brisingamen | Accessory | 0 | 0 | 0 | 150 | All | Freyja's necklace. +10 all stats, +20% Max SP. |
| Mjolnir | Accessory | 0 | 0 | 0 | 150 | All | Thor's hammer as a talisman. +50 DEX; ASPD permanently at the 197 maximum. |

### Soul Cards (35)

| Card | Goes into | Effect | Drops from (Phase 4) |
|---|---|---|---|
| Rune Spore Card | Headgear | +100 Max HP, +5 HP regen. | Rune Spore 1% |
| Toxic Spore Card | Weapon | 5% chance on melee hit to Poison the target. +5 ATK. | Toxic Spore 1% |
| Forest Imp Card | Accessory | +3 AGI, +2 FLEE. | Forest Imp 0.75% |
| Horned Grazer Card | Shield | Take 30% less damage from Beasts. | Horned Grazer 0.75% |
| Field Beetle Card | Armor | +10% Max HP, +5 DEF. | Field Beetle 1% |
| Wood Sprite Card | Weapon | +20% physical damage against Water monsters. | Wood Sprite 0.5% |
| Wild Boar Card | Footgear | +10% movement speed, +3 STR. | Phase 5 monster |
| Dire Wolf Card | Weapon | +20% critical damage, +5 CRIT. | Dire Wolf 0.5% |
| Forest Outlaw Card | Weapon | +20% physical damage against Demi-Humans and players. | Phase 5 monster |
| Draugr Footman Card | Shield | Take 30% less damage from Demi-Humans and players. | Phase 5 monster |
| Crypt Bat Card | Weapon | Physical attacks heal you for 5% of the damage dealt. | Phase 5 monster |
| Cave Crawler Card | Armor | Your armor becomes Earth element. +10% resistance to Stone Curse. | Phase 5 monster |
| Ghoul Card | Weapon | +20% physical damage against Undead. | Phase 5 monster |
| Fjord Harpy Card | Garment | +20 FLEE, +3 AGI. | Phase 5 monster |
| Frost Wolf Card | Armor | Your armor becomes Water element. Immune to Freeze. | Frost Wolf 0.5% |
| Sea Drake Card | Weapon | +20% physical damage against Large monsters. | Phase 5 monster |
| Runic Berserker Card | Weapon | +15% physical damage against Medium monsters. | Phase 5 monster |
| Ice Golem Card | Shield | Immune to Freeze. +5 DEF. | Phase 5 monster |
| Crypt Wraith Card | Garment | Take 20% less Neutral damage. | Phase 5 monster |
| Banshee Card | Headgear | Immune to Silence. +3 INT. | Phase 5 monster |
| Corrupted Einherjar Card | Weapon | Physical attacks restore SP equal to 3% of the damage dealt. | Phase 5 monster |
| Naga Scout Card | Accessory | Your casting can't be interrupted, but casts take 15% longer. | Phase 5 monster |
| Jotun Brawler Card | Armor | +10 STR. With base STR 180+: +20 ATK and +5% physical damage. | Jotun Brawler 0.5% |
| Snow Harpy Card | Footgear | +10% Max HP and Max SP. | Phase 5 monster |
| Frost Wyrm Card | Weapon | +15% magic damage; spells ignore 10% of the target's MDEF. | Phase 5 monster |
| Abyssal Leech Card | Accessory | Each hit drains up to 5 SP from the target into you. (PvP: crits burn 10% SP, Phase 6.) | Phase 5 monster |
| Frozen Revenant Card | Armor | Your armor becomes Undead element. Immune to Freeze and Stone Curse. | Phase 5 monster |
| Hel's Executioner Card | Weapon | 5% chance on melee hit of a Mortal Stagger (breaks poise outright). | Phase 5 monster |
| Elder Direwolf Card | Headgear | +5 all stats, +10 FLEE. | Phase 5 monster |
| Draugr Warlord Card | Shield | Reflects 10% of physical melee damage back at the attacker. +10 DEF. | Phase 5 monster |
| Ancient Golem Card | Weapon | Your weapon becomes unbreakable. +25 ATK. | Phase 5 monster |
| Naga Queen Card | Accessory | Skill cooldowns 10% shorter. +5 INT. | Phase 5 monster |
| Fenrir Card | Garment | +10 ASPD, +50 FLEE. 5% chance on melee hit to enter Berserk Wolf Form for 10s (CRIT doubled, no stagger). | Phase 5 monster |
| Hel's Vanguard Card | Armor | Your armor becomes Ghost element: Neutral attacks deal only 25%. | Phase 5 monster |
| Jormungandr's Brood Card | Weapon | Attacks ignore 40% of DEF and MDEF. Melee hits inflict Frostbite for 5s (half speed and ASPD; unblockable). | Phase 5 monster |

### Shops

- **Ásta's Trading Post:** Lingonberry Tonic (50 z), Honey Mead (450 z), Aether Sap Vial (300 z), Raven Feather (300 z), Wind Rune Shard (60 z), Dead Branch (5,000 z), Cotton Tunic (500 z), Leather Jerkin (4,000 z), Buckler (2,000 z), Traveler's Cloak (800 z), Sandals (400 z), Leather Boots (3,500 z), Bandana (400 z), Fur Cap (1,200 z), Clip Ring (3,000 z), Hunter's Bow (1,500 z), Iron Spear (1,800 z), Oak Wand (500 z), Rusty Seax (100 z), Seax (2,400 z), Iron Mace (3,500 z)
- **Brokk's Dwarven Forge:** Bog Iron (50 z), Dwarven Steel (200 z), Starmetal (5,000 z), Skystone (5,000 z), Rune of Preservation (50,000 z), Rune of Extraction (150,000 z), Glyph of Sowilo (8,000 z), Glyph of Tiwaz (8,000 z), Glyph of Isa (8,000 z), Glyph of Hagalaz (8,000 z), Glyph of Thurisaz (8,000 z), Glyph of Uruz (8,000 z), Thurisaz Runestone (3,000 z), Isa Runestone (4,000 z), Hagalaz Runestone (3,000 z)

### Monster drops

| Monster | Lv | Drops (base rate, before the server's x5 item rate; cards stay x1) |
|---|---|---|
| Rune Spore | 3 | Lingonberry Tonic 40%, Spore Cap 55%, Sandals 1%, Flower Crown 0.3%, Rune Spore Card 1% |
| Field Beetle | 6 | Lingonberry Tonic 30%, Raven Feather 5%, Beetle Shell 55%, Bog Iron 8%, Cotton Tunic 1.5%, Field Beetle Card 1% |
| Toxic Spore | 10 | Aether Sap Vial 10%, Toxic Gland 50%, Spore Cap 20%, Bandana 1%, Toxic Spore Card 1% |
| Forest Imp | 14 | Wind Rune Shard 15%, Tiwaz Runestone 2%, Imp Horn 45%, Dead Branch 3%, Clip Ring 0.5%, Forest Imp Card 0.75% |
| Horned Grazer | 18 | Honey Mead 8%, Grazer Hide 50%, Bog Iron 10%, Leather Jerkin 1%, Fur Cap 1%, Horned Grazer Card 0.75% |
| Wood Sprite | 24 | Uruz Runestone 3%, Aether Sap Vial 12%, Sprite Leaf 45%, Dwarven Steel 4%, Runed Oak Wand 1%, Wood Sprite Card 0.5% |
| Dire Wolf | 70 | Tiwaz Runestone 5%, Honey Mead 20%, Wolf Pelt 50%, Dwarven Steel 8%, Skystone 2%, Wolf Hood 0.8%, Wolf Claws 0.5%, Dire Wolf Card 0.5% |
| Frost Wolf | 140 | Sowilo Runestone 5%, Honey Mead 30%, Frost Fang 40%, Starmetal 3%, Skystone 4%, Frost Stiletto 0.4%, Glyph of Isa 1%, Frost Wolf Card 0.5% |
| Jotun Brawler | 230 | Uruz Runestone 10%, Honey Mead 40%, Jotun Tooth 40%, Starmetal 6%, Skystone 6%, Glyph of Thurisaz 2%, Plated Greaves 0.5%, Blood Branch 0.5%, Jotun Brawler Card 0.5% |

---

## 10. Save data

- **New data on each character** (`CharacterRecord`):
  - `Equipment` (10 positions);
  - `EquipmentDataVersion`;
  - per item: `Refine`, `Cards[]` and `Glyphs[]` (`ItemStack`).
- **New data on each account:** `AccountRecord.Storage`. Everything still saves to `accounts.json` in `Application.persistentDataPath`.
- **Old saves:** Phase 2/3 characters (version 0) get their job's starter weapon equipped on first load.
- **Repairs on load:**
  - A hand-edited save with stacked equipment ("Amount": 3) is split into separate pieces.
  - Gear in the wrong slot, or gear the character can't wear, goes back to the bag.
  - Overlapping pieces (a shield under a greatsword) are resolved.
- **Saving:** refines, carving, compounding, buying and selling save at once. Storage moves save together with the character.

## 11. New GM commands

| Command | What it does |
|---|---|
| `@zeny <amount>` | Add (or remove, with a negative number) zeny |
| `@items [weapons\|gear\|cards\|consumables\|etc\|text]` | List item ids by category or name |
| `@item <id> [amount]` | Equipment comes as separate pieces |
| `@refine <0-20>` | Set the refine of your worn weapon |
| `@monster <id>` | Spawn monsters to test cards against (`@monsters` lists them) |

## 12. Tests

- **Core (no Unity needed):** `cd Tools/CoreTests && dotnet test Tests` runs **121 tests** (27 new). They cover:
  - catalog counts (87 items, 28 weapons, 24 headgears, 4 wings, 35 cards) and that every job can wield its starter weapon;
  - slot clash rules and wear rules;
  - stats from gear, refine DEF, conditional cards, relics, armor element and immunity;
  - refine safe steps, shattering and Preservation;
  - compounding and extraction;
  - runewords;
  - buying, selling and storage, including the atomic account save rolling back on a failed write;
  - save migration;
  - defender cards and crit damage;
  - the Isa shield;
  - job-change weapon gifts;
  - valid drop tables, no-card steals, and Branch picks;
  - gear never lost to a full bag, per-status resist gear, and storage amount checks.
- **PlayMode:** `Gear_Cards_Npcs_Branches_AndStorage` runs in a real scene. It checks that:
  - the NPCs spawn;
  - wearing the tunic raises DEF;
  - a card compounds into worn armor;
  - Jormungandr's Brood frostbites a dummy;
  - talking to every NPC works;
  - a Dead Branch summons a monster;
  - storage keeps a piece's refine.

## 13. Design choices to confirm

1. **Card rate ×1.** The GDD gives card rates of 0.5–1% while the server uses a ×5 item rate. I kept cards at the GDD numbers. Change `cardDropRate` if you want cards to follow the item rate.
2. **Selling pays half price** (Ragnarok standard). There is no discount or overcharge skill yet.
3. **Weapon refine safe limits** follow Ragnarok (+7/+6/+5/+4). Armor is safe to +4, which matches "past +4" in GDD §7.
4. **Blood Branch** summons the Lv 70+ test monsters until Phase 5 adds the GDD world bosses.
5. **Abyssal Leech against monsters:** monsters have no SP pool, so the card restores its full 5 SP per hit against them. Against players (Phase 6) it drains theirs.
6. **Job-change weapon gift:** the new job's weapon replaces a plain starter weapon automatically. Refined, carded or etched weapons stay equipped and the gift goes to the bag.

## What's next

**Phase 5 — World Maps & Monster AI Pipeline:**
- Fields 2–4 with their monsters, which make the remaining Soul Cards drop;
- monster skills (weapon break makes Ancient Golem matter, and spells make Hagalaz matter);
- mini-bosses on 2-hour timers;
- the three MVP world bosses (Fenrir, Hel's Vanguard, Jormungandr's Brood) at 0.01% card rates.
