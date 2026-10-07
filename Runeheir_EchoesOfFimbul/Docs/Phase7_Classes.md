# Phase 7 Guide: the Full Roster, Rebirth and the Expanded Jobs

Phase 7 brings Ragnarok Online's complete class roster to Runeheir, as played on servers like XileRO, under Norse names: 40 jobs and 282 skills. It adds RO's rebirth loop, the expanded jobs (Super Novice, Taekwon line, Gunslinger, Ninja, Doram) and the mechanics those jobs need: songs and dances, coins, traps, strips, zeny skills, resurrection, crafting and skill copying.

Everything here is in code and covered by tests: 188 Core rules tests, the Unity EditMode suite and the PlayMode smoke tests in CI. The models are still placeholders (Phase 7.2 art comes later), so new weapons and Freyja's Kin are recognisable shapes rather than final art.

---

## 1. The road, step by step

Every human character walks the same road as Ragnarok:

1. **Initiate** (Novice). Job levels 1–10. At **Job Lv 10**, Sigrun in Vigrid Haven (or *Skills → Job Change*) offers the six first jobs and the four expanded paths.
2. **First job.** Warrior, Mystic, Huntsman, Devotee, Trader or Scout. Job levels up to 50. At **Job Lv 40** you choose a second job.
3. **Second job.** For example, Berserker or Guardian for a Warrior. Job levels up to 70. **Base level stops at 99.**
4. **Rebirth.** As a second job at **Base Lv 99 and Job Lv 50**, bring **1,285,000 zeny** to **Urðr at Urðr's Well** (north-east Vigrid Haven, near the forge). You become a **High Initiate**.
5. **High Initiate → High first job.** It must be the same first job as your first life: a reborn Berserker or Guardian can only become a High Warrior.
6. **High first job → transcendent job** at Job Lv 40. Only the transcendent form of your first life's second job is open:
   - Berserker → **Einherjar** (Lord Knight)
   - Guardian → **Valkyrie** (Paladin)
   - Base levels now go to **255**, and transcendent job levels to **120**.

Your example from the brief, in Runeheir names:

> Initiate → Warrior → **Berserker** *or* **Guardian** → *rebirth* → High Initiate → High Warrior → **Einherjar** (if your first choice was Berserker) *or* **Valkyrie** (if it was Guardian).

The job change window only lists the choices you can take next, and greyed-out rows say why ("Requires Job Level 40 as High Warrior", "Your first life was a Guardian: your path is High Warrior, then Valkyrie.").

## 2. What rebirth does

| | Before | After rebirth |
|---|---|---|
| Job | a second job (Berserker...) | **High Initiate** |
| Base / Job level | 99 / 50+ | **1 / 1** |
| Status points | spent | **100** to spend (48 + 52 reborn bonus), stats back to 1 |
| Skills | learned | **cleared, 0 points** (Ragnarok-faithful: you relearn as you level) |
| Gear | worn | **moved to your bag** (your High first job can wear it again) |
| Max HP / SP | normal | **+25%** for the rest of your life |
| Base level cap | 99 | **255** |
| Zeny | | −1,285,000 |

**Skills after rebirth.** Skills are cleared and you start with 0 skill points, not a refund. This is how Ragnarok does it: each job level after rebirth gives a point back, so you relearn your tree while levelling. (An earlier note said the points would be refunded; the RO behaviour was chosen instead.)

**Saves.** Characters saved as a transcendent job before Phase 7 are made reborn automatically. Skills a job can no longer use (because they moved to another job in the new tree) are refunded on load.

## 3. Expanded jobs

These never rebirth. They level to **Base 255** directly, and all but the Glíma Fighter wear the gear marked for transcendent and expanded jobs.

| Job | Ragnarok | How to get it | Notes |
|---|---|---|---|
| **Wanderer** | Super Novice | Initiate Job Lv 10 **and Base Lv 45** | Job levels to 99. Learns the skills of **all six first jobs**; the skill tree shows a tab for each. |
| **Glíma Fighter** | Taekwon | Initiate Job Lv 10 | Bare-handed kicks and stances. Job levels to 50; at Job Lv 40 → Sól Guardian or Fylgja Caller. |
| **Sól Guardian** | Star Gladiator | Glíma Fighter Job Lv 40 | Sun, moon and star warmth. Job levels to 70. |
| **Fylgja Caller** | Soul Linker | Glíma Fighter Job Lv 40 | Spirit links that empower other jobs. Job levels to 70. |
| **Thunderer** | Gunslinger | Initiate Job Lv 10 | Thunder-rods and **Thor's Coins** (up to 10, spent by its strongest shots). |
| **Nightraider** | Ninja | Initiate Job Lv 10 | Huuma shuriken, ninjutsu, shadow steps. |
| **Freyja's Kin** | Doram (Summoner) | **Chosen at character creation** | Cat-folk of Freyja's chariot: a people, not a job change. Smaller model with ears and a tail. Cat staves. |

## 4. NPCs

| NPC | Where | What |
|---|---|---|
| **Sigrun** · Guild Hall | Vigrid Haven, north-west | Job changes. The *Job Change* button in the Skill window opens the same list. |
| **Urðr** · Urðr's Well | Vigrid Haven, north-east | *Ask for a new thread* (rebirth, after a confirmation that lists everything it does) and *What is rebirth?* |
| **Ásta** · Trading Post | Vigrid Haven | Now also sells the Woodcutter's Axe, Spark Rod and Bygul's Staff. |
| **Hrafn** · Vigrid Armory | Vigrid Haven | Now also sells the Bearded Axe, Willow Lyre, Leather Lash, Rune Primer, Thunder Carbine, Iron Huuma and Trjegul's Staff. |
| **Brokk** · Forge supplies | Vigrid Haven | Bog Iron, Dwarven Steel and Starmetal: the Runesmith's forging metals. |

## 5. New weapons

| Type | Used by | In shops | Stronger ones |
|---|---|---|---|
| Axe | Trader line, Wanderer | Woodcutter's Axe, Bearded Axe | Dwarven Axe, Herbwife's Sickle, Iðunn's Golden Sickle (job gifts, drops, forging) |
| Great Axe | Trader line, Warrior line | – | Dane Axe, Eitri's Great Axe |
| Instrument | Skald, Thul | Willow Lyre | Tagelharpa, Bragi's Harp |
| Whip | Seidkona, Völva | Leather Lash | Seidr Lash, Serpent Lash |
| Rune Tome | Sage, Gothi lines | Rune Primer | Eddic Codex, Book of Mímir |
| Thunder-Rod (ranged) | Thunderer | Spark Rod, Thunder Carbine | Storm Rod, Thor's Wrath |
| Huuma Shuriken | Nightraider | Iron Huuma | Frost Huuma, Fenrir's Fang |
| Cat Staff | Freyja's Kin | Bygul's Staff, Trjegul's Staff | Brísingamen Staff |

Each job's gift on job change is listed in its section below. Level-4 weapons are transcendent and expanded gear.

## 6. New mechanics

**Songs and dances** (Skald, Seidkona, Thul, Völva). A ring follows the performer for the song's duration (30–60 s), with a 4.5 m radius. Every few seconds it gives allies inside it the song's buff, which fades a few seconds after they step out, or gives foes inside it the dance's debuff or damage. You move at half speed while performing and can't start a second performance until the first ends.

**Return from Hel** (Gothi, Ragnarok's Resurrection). Target a fallen ally; they stand up where they fell with 10/30/50/80% HP (by skill level), without the trip to the save point. Online, it goes through the realm to the ally's own game.

**Thor's Coins** (Thunderer). *Thor's Coin* flips coins into your pouch, up to 10. The heavy shots spend them, and the error message tells you how many you need.

**Zeny and Pushcart skills.** Gold-Strike (Mammonite) costs zeny per level. Cart skills need a rented Pushcart, and Cart Charge and Brokkr's Cart Crush hit harder the more your cart carries.

**Traps** (Ranger). Most traps spring on the first foe to step in. Area traps (Muspel Mine, Thorn of Sleep) burst over everyone inside.

**Strips and charms** (Outlaw and others) roll their chance and show "Failed" when they miss.

**Cut Purse** (Outlaw). Once per monster: DEX and LUK against its level for a handful of zeny that grows with the monster's level.

**Loki's Mimicry** (Outlaw/Vargr, Ragnarok's Plagiarism). When an ally within 14 m uses a first- or second-job active skill, you copy it. You can use the copy up to your Mimicry level (and never above the level they used). It appears at the top of the Outlaw page of your skill tree, marked *copied*, ready to bind. You hold one copy at a time and it survives logging out. **Preserve** (Vargr) stops a new skill from replacing it. Passives, songs, crafting, resurrection, transcendent and expanded skills, and the thief line's own skills can't be copied.

**Haggle and Silver Tongue** (Trader, Ragnarok's Discount and Overcharge). NPC shops sell to you 2.4% cheaper per Haggle level and pay you 2.4% more per Silver Tongue level, both capping at 24%. The shop window shows the adjusted prices. The caps keep buy-and-resell from turning a profit.

**Reborn bonus.** +25% Max HP and SP, already counted in the HP and SP you see.

## 7. Crafting: Rune Forging and Brewing

Use **Rune Forging** (Runesmith, Forgelord) or **Brewing** (Brewmaster, Lifeweaver) to open the crafting window. Pick a recipe on the left; the right side shows the materials (green when you have enough), your success chance and the buttons. **Every attempt costs the skill's SP, and a failure uses up the materials**, as in Ragnarok. *×5* repeats until you run out of materials or SP.

**Forging.** Rune Forging Lv N makes weapons of weapon level up to N, for daggers, greatswords, spears, maces, knuckles, katars, axes and great axes. Smiths don't make bows, staves, tomes or instruments.

| Weapon level | Metal | Plus |
|---|---|---|
| 1 | 4 Bog Iron | 1 piece of loot (see below) |
| 2 | 2 Dwarven Steel + 4 Bog Iron | 2 pieces |
| 3 | 1 Starmetal + 4 Dwarven Steel | 3 pieces |

The loot depends on the type: Wolf Pelt (daggers, knuckles), Grazer Hide (greatswords, spears), Beetle Shell (maces), Crawler Carapace (katars), Boar Tusk (axes). Chance: 45 + DEX/5 + LUK/10 + Job Lv/5 + 2 per Weaponry Research level, +10 per Rune Forging level above the weapon level, −15 per weapon level above 1 (between 5% and 95%).

**Brewing.**

| Makes | Brewing Lv | From |
|---|---|---|
| 3 Lingonberry Tonic | 1 | 2 Spore Cap |
| 2 Wind Rune Shard | 2 | 1 Sprite Leaf |
| Aether Sap Vial | 3 | Sprite Leaf + Spore Cap |
| Raven Feather | 4 | Harpy Feather + Bat Wing |
| Honey Mead | 5 | Imp Horn + 2 Spore Cap |
| Sowilo Runestone | 7 | Harpy Feather + 2 Sprite Leaf |
| Uruz Runestone | 9 | 2 Boar Tusk + Imp Horn |
| Isa Runestone | 10 | Ice Core + Sprite Leaf |

Chance: 30 + 3 per Brewing level + 2 per Potion Research level + Job Lv × 0.3 + (INT + DEX + LUK)/10, minus the recipe's difficulty (0–40). Every material is sold in a shop or dropped by a monster; a test checks this.

## 8. Testing it in the editor

GM commands (type in chat; offline they work in the editor and development builds, online only if the realm's `realm.json` allows them):

| Command | Does |
|---|---|
| `@jobs` | Lists every job. |
| `@job berserker` | Changes job (Ragnarok names work too: `@job knight`, `@job lordknight`). |
| `@blvl 99` · `@jlvl 50` | Sets levels. |
| `@rebirth` | Meets Urðr's requirements (Base 99, Job 50, the fee) and rebirths: needs a second job. |
| `@allskills` · `@learn <skill> [lv]` | Learns skills without points. |
| `@item bog_iron 20` · `@zeny 2000000` | Materials and money for crafting and rebirth. |

A full-road test: make a character, `@job warrior`, `@job berserker`, `@rebirth`, then talk to Sigrun. Only High Warrior is offered. `@jlvl 40`, open Job Change, and only Einherjar is offered. For Freyja's Kin, choose *Freyja's Kin* on the creation screen.

## 9. Known limits

- **Placeholder art.** New weapons and the songs' rings are still primitive shapes. Art Pass 1 ([Art_Characters.md](Art_Characters.md)) gave every human a rigged body and the Warrior line its outfits; the other jobs wear the common clothes in their job color until their own outfits are made.
- **Balance is untested by players.** Numbers follow Ragnarok's shape (level scaling, chances, caps) but haven't been tuned in play.
- **Ensembles** (Ragnarok's Bard+Dancer duets) and **homunculi / Lifeweaver summons** aren't in this phase.
- **Basic Training** isn't required for the first job change.

---

## 10. The roster

| Runeheir | Ragnarok | Tier | Reached from | Skills |
|---|---|---|---|---|
| Initiate | Novice | Novice | character creation | 3 |
| Warrior | Swordman | First | Initiate | 6 |
| Berserker | Knight | Second | Warrior | 7 |
| Guardian | Crusader | Second | Warrior | 11 |
| Einherjar | Lord Knight | Transcendent | High Warrior, after Berserker | 8 |
| Valkyrie | Paladin | Transcendent | High Warrior, after Guardian | 8 |
| Mystic | Mage | First | Initiate | 9 |
| Sorcerer | Wizard | Second | Mystic | 6 |
| Sage | Sage | Second | Mystic | 5 |
| Archmage | High Wizard | Transcendent | High Mystic, after Sorcerer | 4 |
| Chronomancer | Professor | Transcendent | High Mystic, after Sage | 5 |
| Huntsman | Archer | First | Initiate | 6 |
| Ranger | Hunter | Second | Huntsman | 7 |
| Skald | Bard | Second | Huntsman | 7 |
| Seidkona | Dancer | Second | Huntsman | 7 |
| Deadeye | Sniper | Transcendent | High Huntsman, after Ranger | 4 |
| Thul | Minstrel | Transcendent | High Huntsman, after Skald | 4 |
| Völva | Gypsy | Transcendent | High Huntsman, after Seidkona | 4 |
| Devotee | Acolyte | First | Initiate | 7 |
| Gothi | Priest | Second | Devotee | 12 |
| Monk | Monk | Second | Devotee | 6 |
| High Gothi | High Priest | Transcendent | High Devotee, after Gothi | 4 |
| Champion | Champion | Transcendent | High Devotee, after Monk | 4 |
| Trader | Merchant | First | Initiate | 6 |
| Runesmith | Blacksmith | Second | Trader | 8 |
| Brewmaster | Alchemist | Second | Trader | 8 |
| Forgelord | Mastersmith | Transcendent | High Trader, after Runesmith | 4 |
| Lifeweaver | Biochemist | Transcendent | High Trader, after Brewmaster | 4 |
| Scout | Thief | First | Initiate | 8 |
| Assassin | Assassin | Second | Scout | 6 |
| Outlaw | Rogue | Second | Scout | 10 |
| Shadow Walker | Assassin Cross | Transcendent | High Scout, after Assassin | 7 |
| Vargr | Stalker | Transcendent | High Scout, after Outlaw | 4 |
| Wanderer | Super Novice | Expanded | Initiate | 4 |
| Glíma Fighter | Taekwon | Expanded | Initiate | 8 |
| Sól Guardian | Star Gladiator | Expanded | Glíma Fighter | 9 |
| Fylgja Caller | Soul Linker | Expanded | Glíma Fighter | 8 |
| Thunderer | Gunslinger | Expanded | Initiate | 15 |
| Nightraider | Ninja | Expanded | Initiate | 14 |
| Freyja's Kin | Doram (Summoner) | Expanded | character creation | 13 |

## 11. Every job and its skills

Generated from the game's own job and skill catalogs. *Type* is Attack (physical), Magic, Buff, Self buff, Debuff, Ground (an area that lasts), Trap, Song, Dance, Support, Crafting, Passive or Granted.

### The Initiate

#### Initiate — Ragnarok's Novice

*Everyone begins here. Learn Basic Training, then choose a path at Job Level 10.*

From **character creation** · Job Lv up to 10 · next job at Job Lv 10 · weapons: Unarmed / Dagger / Sword / Mace / Staff / Axe · gift: Rusty Seax

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Basic Training | 9 | Passive | Survival basics: +1 HIT, +1 FLEE and +1 HP regen per level. |
| First Aid | 1 | Granted | Bandage your wounds: heals 25 + Base Level HP. |
| Feign Death | 1 | Self buff | Ragnarok's Play Dead: drop and lie still until monsters lose track of you. You crawl slowly; attacking ends it. |

### The Warrior line

#### Warrior — Ragnarok's Swordman

*Sword and shield of the shield-wall: high HP, Bash and Magnum Break.*

From **Initiate** · Job Lv up to 50 · next job at Job Lv 40 · weapons: Unarmed / Dagger / Sword / Greatsword / Spear / Mace / Axe / Great Axe · gift: Iron Claymore

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Bash | 10 | Attack | A heavy blow. From Lv 6 it can stun. |
| Magnum Break | 10 | Attack | A fiery shockwave around you that knocks enemies back. |
| Provoke | 10 | Debuff | Enrage an enemy: it hits harder but its DEF drops, and it comes after you. |
| Endure | 10 | Self buff | Grit your teeth: you can't be staggered, and MDEF rises. |
| Sword Mastery | 10 | Passive | +4 ATK per level with swords and greatswords. |
| Iron Constitution | 10 | Passive | +3 HP regen and +0.5% Max HP per level. |

#### Berserker — Ragnarok's Knight

*Two-handed fury: frenzied strikes that shake the battlefield.*

From **Warrior** · Job Lv up to 70 · weapons: Unarmed / Dagger / Sword / Greatsword / Spear / Mace / Axe / Great Axe · gift: Flamberge

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Battle Frenzy | 10 | Self buff | Greatsword fury: +3% ASPD and +2 HIT per level. |
| Cleaving Strike | 10 | Attack | A wide cut that also hits everyone next to your target. |
| War Cry | 5 | Debuff | A terrifying roar: nearby enemies lose DEF and turn on you. |
| Blood Rush | 5 | Attack | Charge at an enemy and slam into it. Can stun. |
| Feral Resilience | 10 | Passive | +2% Max HP and +5% poise per level. |
| Reckless Edge | 10 | Passive | +1% physical damage and +1 CRIT per level with greatswords. |
| Riposte | 5 | Self buff | Ragnarok's Counter Attack: read the next blow; your next attack is a sure critical. |

#### Guardian — Ragnarok's Crusader

*Holy shield-bearer: guards allies, smites with the Sacred Cross.*

From **Warrior** · Job Lv up to 70 · weapons: Unarmed / Dagger / Sword / Greatsword / Spear / Mace / Axe / Great Axe · gift: Rune Lance

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Spear Mastery | 10 | Passive | +4 ATK per level with spears. |
| Spear Stab | 10 | Attack | Thrust through every enemy in a line and push them back. |
| Brandish Spear | 10 | Attack | Sweep the spear around your target, hitting everything near it. |
| Guardian's Oath | 10 | Self buff | Raise your guard: +3% chance per level to block physical melee hits, but walk 10% slower. |
| Holdfast | 5 | Self buff | Plant your feet: take 4% less damage per level and can't be staggered, but walk 30% slower. |
| Stalwart | 10 | Passive | +1% Max HP and +5% poise per level. |
| Faith | 10 | Passive | +200 Max HP per level. |
| Sacred Cross | 10 | Attack | Two holy cross-shaped strikes. Can blind. |
| Radiant Cross | 10 | Magic | Sacrifice 20% of your HP: a great holy cross erupts around you, three times. +50% vs Undead and Demons. |
| Judgment | 5 | Attack | Bring your weapon down on your target and everyone beside it. Often stuns. |
| Shield Bash | 5 | Attack | Ragnarok's Shield Charge: ram a foe with your shield. Knocks back and can stun. |

#### Einherjar — Ragnarok's Lord Knight

*Chosen of Valhalla: Vortex Cleave, Rage of Thor, Spiral Pierce.*

From **High Warrior** (reborn after a life as **Berserker**) · Job Lv up to 120 · weapons: Unarmed / Dagger / Sword / Greatsword / Spear / Mace / Axe / Great Axe · gift: Einherjar Greatsword

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Vortex Cleave | 10 | Attack | 360° greatsword sweep: two hits that launch enemies. Each launched enemy that lands on others crashes into them (half power, at most once per enemy per cast). |
| Two-Hand Surge | 10 | Self buff | +0.7 ASPD per level (+7 at Lv 10) and attack recovery canceling. |
| Rage of Thor | 5 | Self buff | Max HP x3, ASPD 195, hyper-armor (no flinch, knockback or stagger); items locked. Lasts 10 s + 5 s per level. |
| Valhallan Might | 10 | Passive | +2% physical damage per level. |
| Valhalla's Edge | 5 | Self buff | Ragnarok's Aura Blade: the blade burns with Valhalla's light, +20 ATK per level. |
| Parry | 10 | Self buff | Ragnarok's Parrying: turn blows aside with the greatsword, +5% block chance per level. |
| Berserkergang | 1 | Self buff | Ragnarok's Frenzy, in the berserkers' own word: double Max HP, +30% ASPD, nothing staggers you. No items. |
| Gungnir Thrust | 5 | Attack | Ragnarok's Spiral Pierce, after Odin's spear: five spiralling thrusts that never miss. |

#### Valkyrie — Ragnarok's Paladin

*Odin's shield-maiden: Valkyrie's Descent, Gloria, Shield Chain.*

From **High Warrior** (reborn after a life as **Guardian**) · Job Lv up to 120 · weapons: Unarmed / Dagger / Sword / Greatsword / Spear / Mace / Axe / Great Axe · gift: Valkyrian Lance

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Valkyrie's Descent | 10 | Attack | Leap to a spot and crash down, hitting and knocking back everyone around you. |
| Spiral Lance | 10 | Attack | Five spinning lance strikes on one enemy. |
| Freyja's Shield | 10 | Buff | Shield yourself or an ally: less damage taken and more poise. |
| Valkyrie Grace | 10 | Passive | +1% Max HP, +5% poise and +1% movement speed per level. |
| Valor Aura | 10 | Self buff | +5 ATK and 1% less damage taken per level. |
| Hammer of Tyr | 5 | Attack | Smash the ground with Tyr's strength: damage and a strong chance to stun everything in the area. |
| Spear of Light | 5 | Attack | Ragnarok's Pressure (Gloria Domini): a holy lance of light that ignores armour and never misses. |
| Shield Barrage | 5 | Attack | Ragnarok's Shield Chain: five shield blows thrown in a chain. |

### The Mystic line

#### Mystic — Ragnarok's Mage

*A student of rune-magic: bolts of fire, frost and lightning.*

From **Initiate** · Job Lv up to 50 · next job at Job Lv 40 · weapons: Unarmed / Dagger / Staff · gift: Oak Wand

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Muspel Bolt | 10 | Magic | Bolts of Muspelheim fire: one bolt (100% MATK) per level. |
| Frost Spike | 10 | Magic | Bolts of Niflheim ice: one bolt (100% MATK) per level. |
| Thunder Rune | 10 | Magic | Bolts of Thor's lightning: one bolt (100% MATK) per level. |
| Soul Lance | 10 | Magic | Ghostly lances: one more every two levels. Ghost element, quick to cast. |
| Frost Lock | 10 | Magic | Encase an enemy in ice (Frozen: can't act; blunt weapons deal triple damage to it). |
| Mana Focus | 10 | Passive | +3 SP regen per level. |
| Surtr's Orb | 10 | Magic | Ragnarok's Fire Ball: a ball of the fire giant's flame that bursts over everyone near the target. |
| Troll-Stone | 10 | Active | Ragnarok's Stone Curse: the dawn-curse that turns trolls to stone. |
| Seiðr Coat | 5 | Self buff | Ragnarok's Energy Coat: a coat of seidr softens every blow, 10% less damage taken, +2% per level. |

#### Sorcerer — Ragnarok's Wizard

*Master of destructive seidr: storms of fire, ice and lightning.*

From **Mystic** · Job Lv up to 70 · weapons: Unarmed / Dagger / Staff · gift: Runed Staff

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Muspel Rain | 10 | Magic | Burning meteors fall on an area, wave after wave. Can stun. |
| Storm Lance | 10 | Magic | A ball of lightning that hits many times and blasts the target back. |
| Frost Nova | 10 | Magic | A ring of frost around you that can freeze everything it touches. |
| Muspel Wall | 10 | Ground | A wall of fire on the ground that burns and repels enemies that touch it. |
| Earthen Spikes | 5 | Magic | Stone spikes erupt in an area, one wave per level. |
| Arcane Mind | 10 | Passive | +1% magic damage and 1% faster casting per level. |

#### Sage — Ragnarok's Sage

*Scholar of runes: free casting, auto-runes, ground magic.*

From **Mystic** · Job Lv up to 70 · weapons: Unarmed / Dagger / Staff / Rune Tome · gift: Rune Tome Staff

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Rune Study | 10 | Passive | +2 MATK and +1% Max SP per level. |
| Free Cast | 10 | Passive | Walk while casting at 7.5% of your speed per level (75% at Lv 10). |
| Auto Rune | 10 | Self buff | While active, your basic hits can cast a learned bolt (Muspel Bolt, Frost Spike or Thunder Rune) for free. |
| Bog of Niflheim | 5 | Ground | Turn the ground into a freezing bog: enemies in it lose AGI and DEX (monsters: FLEE and HIT) and move at half speed. |
| Mystic Volcano | 5 | Ground | Allies standing in the area gain ATK and magic damage. |

#### Archmage — Ragnarok's High Wizard

*Rune-lord of the nine realms: Glacial Tempest, Runic Aegis.*

From **High Mystic** (reborn after a life as **Sorcerer**) · Job Lv up to 120 · weapons: Unarmed / Dagger / Staff · gift: Yggdrasil Staff

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Glacial Tempest | 10 | Magic | Multi-wave blizzard (5 waves). Each wave can freeze enemies brittle: frozen foes take triple damage from blunt weapons. |
| Runic Aegis | 10 | Buff | A runic dome on you or an ally that blocks one physical melee strike per level (10 at Lv 10). |
| Rune Amplify | 10 | Self buff | Your next damaging spell deals +5% damage per level. |
| Heaven's Wrath | 10 | Magic | Thunder from the sky strikes an area in four waves. Can blind. |

#### Chronomancer — Ragnarok's Professor

*Scholar of the Norns' threads: stasis, haste, slowed time.*

From **High Mystic** (reborn after a life as **Sage**) · Job Lv up to 120 · weapons: Unarmed / Dagger / Staff / Rune Tome · gift: Norn Staff

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Stasis Field | 5 | Active | Stop time in an area: enemies there can be frozen in place (stunned). |
| Haste Rune | 10 | Buff | Quicken yourself or an ally: +2% ASPD and 2% faster casting per level. |
| Slow Time | 5 | Ground | An area where time drags: enemies in it move and attack slower. |
| Chrono Lance | 10 | Magic | Lances from a moment ago and a moment ahead strike at once. Ghost element. |
| Temporal Mend | 5 | Ground | Rewind wounds: allies in the area heal every second. |

### The Huntsman line

#### Huntsman — Ragnarok's Archer

*Bow-hunter of the northern woods: Double Strafe, Arrow Shower, a keen eye.*

From **Initiate** · Job Lv up to 50 · next job at Job Lv 40 · weapons: Unarmed / Dagger / Bow · gift: Hunter's Bow

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Double Strafe | 10 | Attack | Loose two arrows at once. |
| Arrow Shower | 10 | Attack | Rain arrows on an area, pushing enemies back. |
| Owl's Eye | 10 | Passive | +1 DEX per level. |
| Vulture's Eye | 10 | Passive | +1 HIT and +0.25 m bow range per level. |
| Hawk Focus | 10 | Self buff | Ragnarok's Improve Concentration: a hawk's stillness, +2 AGI and DEX per level. |
| Gust Arrow | 1 | Attack | Ragnarok's Arrow Repel: a heavy shot that throws the target far back. |

#### Ranger — Ragnarok's Hunter

*Trapper with a hunting raven: snares, mines and Raven Strike.*

From **Huntsman** · Job Lv up to 70 · weapons: Unarmed / Dagger / Bow · gift: Yew Longbow

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Ankle Snare | 5 | Trap | Set a trap. The first enemy to step on it is snared in place (it can still attack). |
| Raven Strike | 5 | Magic | Huginn dives at the target: one hit per level that ignores FLEE. |
| Raven Training | 10 | Passive | Ragnarok's Steel Crow: a better-trained raven and steadier hands. +3 ATK per level with bows. |
| Rune Mine | 5 | Trap | Ragnarok's Land Mine: an earth rune that bursts under the first foe to step on it. Can stun. |
| Muspel Mine | 5 | Trap | Ragnarok's Blast Mine: a fire rune that explodes over everyone near it. |
| Rime Trap | 5 | Trap | Ragnarok's Freezing Trap: Niflheim's cold clamps shut, freezing whatever it catches. |
| Thorn of Sleep | 5 | Trap | Ragnarok's Sandman: a sleep-thorn trap, the same that felled Brynhildr. Puts foes around it to sleep. |

#### Skald — Ragnarok's Bard

*Court poet of the jarls: songs that strengthen the whole warband.*

From **Huntsman** · Job Lv up to 70 · weapons: Unarmed / Dagger / Bow / Instrument · gift: Tagelharpa

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Bragi's Lessons | 10 | Passive | Ragnarok's Music Lessons: +3 ATK and +0.5% ASPD per level with instruments. |
| Lay Strike | 5 | Attack | Ragnarok's Melody Strike: a struck chord that flies like an arrow. |
| Dissonance | 5 | Dance | A grating performance: every 3 seconds, foes around you take damage. Moves with you. |
| Whistle of Heimdall | 10 | Song | Ragnarok's A Whistle: Heimdall's sharp ear for every ally around you, +3 FLEE per level. Moves with you. |
| Sunset Lay | 10 | Song | Ragnarok's Assassin Cross of Sunset: a quick, bright air, +2% ASPD per level for allies around you. Moves with you. |
| Bragi's Verse | 10 | Song | Ragnarok's A Poem of Bragi: allies around you cast 3% faster and wait 2% less per level. Moves with you. |
| Iðunn's Apple | 10 | Song | Ragnarok's The Apple of Idun: the goddess's fruit, +1% Max HP and +4 HP regen per level for allies. Moves with you. |

#### Seidkona — Ragnarok's Dancer

*Seidr-woman whose dances bend fate against her foes.*

From **Huntsman** · Job Lv up to 70 · weapons: Unarmed / Dagger / Bow / Whip · gift: Seidr Lash

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Seiðr Lessons | 10 | Passive | Ragnarok's Dancing Lessons: +3 ATK and +0.5 CRIT per level with whips. |
| Slinging Lash | 5 | Attack | Ragnarok's Slinging Arrow: the whip's tip cracks out at range. |
| Heiðr's Shriek | 5 | Active | Ragnarok's Scream: a seeress's cry that can stun everyone around you. |
| Spinning Hum | 10 | Song | Ragnarok's Humming: allies around you gain +4 HIT per level. Moves with you. |
| Forgetful Dance | 10 | Dance | Ragnarok's Please Don't Forget Me: foes around you attack and move slower. Moves with you. |
| Freyja's Kiss | 10 | Song | Ragnarok's Fortune's Kiss: the goddess's favour, +1 CRIT per level for allies around you. Moves with you. |
| Seiðr Service | 10 | Song | Ragnarok's Service for You: allies around you gain Max SP and SP regen. Moves with you. |

#### Deadeye — Ragnarok's Sniper

*Huginn's eye: Sharp Shot, Raven Assault, True Sight.*

From **High Huntsman** (reborn after a life as **Ranger**) · Job Lv up to 120 · weapons: Unarmed / Dagger / Bow · gift: Raven Longbow

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Sharp Shot | 10 | Attack | A piercing arrow that hits every enemy in a line and can critically hit. |
| Raven Assault | 5 | Attack | Huginn and Muninn strike together: one huge hit that never misses. |
| True Sight | 10 | Self buff | +5 all stats, +3 HIT and +1 CRIT per level. |
| Gale Step | 10 | Buff | Wind at your heels (or an ally's): +2% movement speed and +1 FLEE per level. |

#### Thul — Ragnarok's Minstrel

*Bragi's chanter: songs of the gods and sound that shatters.*

From **High Huntsman** (reborn after a life as **Skald**) · Job Lv up to 120 · weapons: Unarmed / Dagger / Bow / Instrument · gift: Bragi's Harp

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Bragi's Volley | 10 | Attack | Ragnarok's Arrow Vulcan: nine notes in a flurry, each a blow. |
| Hermóðr's Ward | 10 | Song | Ragnarok's Wand of Hermode: the messenger's ward, +5% MDEF and 4% of spells reflected per level. Moves with you. |
| Ballad of Valhalla | 10 | Song | A war-song from the hall of the slain: +3% physical damage per level for allies around you. Moves with you. |
| Gjallarhorn Blast | 5 | Magic | A blast on Heimdall's horn: everyone around you is struck, and can be stunned. |

#### Völva — Ragnarok's Gypsy

*Far-seeing seeress: dances that unravel fate itself.*

From **High Huntsman** (reborn after a life as **Seidkona**) · Job Lv up to 120 · weapons: Unarmed / Dagger / Bow / Whip · gift: Serpent Lash

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Serpent Volley | 10 | Attack | Ragnarok's Arrow Vulcan, by the lash: nine strikes as fast as a striking serpent. |
| Skuld's Verdict | 5 | Magic | Ragnarok's Tarot Card of Fate: the Norn of what shall be passes judgement. Can curse. |
| Seiðr Charm | 5 | Debuff | Ragnarok's Wink of Charm: a glance that dulls a foe's aim and footwork. |
| Frigg's Kiss | 10 | Song | Odin's queen blesses the circle: +3% magic damage and +2 SP regen per level for allies around you. Moves with you. |

### The Devotee line

#### Devotee — Ragnarok's Acolyte

*Servant of the Aesir: heals, blessings and holy light.*

From **Initiate** · Job Lv up to 50 · next job at Job Lv 40 · weapons: Unarmed / Mace / Staff · gift: Iron Mace

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Eir's Blessing | 10 | Support | Heal yourself or an ally (Ragnarok Heal formula at this level). |
| Divine Shelter | 10 | Passive | Take 1% less damage per level. |
| Odin's Blessing | 10 | Buff | +1 STR, INT and DEX per level for you or an ally. |
| Swift Wind | 10 | Buff | +3 AGI (+1 per level) and +25% movement speed for you or an ally. |
| Holy Light | 5 | Magic | A beam of holy light. +50% damage to Undead and Demons. |
| Purify | 1 | Active | Cure yourself or an ally of every negative status and debuff. |
| Heavy Limbs | 10 | Debuff | Ragnarok's Decrease AGI: a foe's limbs turn to lead, slowing its steps and blows. |

#### Gothi — Ragnarok's Priest

*Temple priest: Sanctuary, resurrection, banishing the dead.*

From **Devotee** · Job Lv up to 70 · weapons: Unarmed / Mace / Staff / Rune Tome · gift: Holy Mace

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Sanctuary | 10 | Ground | Consecrate the ground: allies standing in it heal every second. |
| Holy Judgment | 10 | Magic | Waves of holy fire purge an area. Double damage to Undead and Demons. |
| Return from Hel | 4 | Support | Ragnarok's Resurrection: call a fallen ally back from Hel's road, with 10% of their HP and more per level. |
| Galdr of Eir | 5 | Self buff | Ragnarok's Magnificat: a healer's chant, +6 SP regen per level. |
| Glory of Baldr | 5 | Self buff | Ragnarok's Gloria: the shining god's favour, +10 LUK. |
| Svalinn's Shield | 10 | Buff | Ragnarok's Kyrie Eleison: the shield that stands before the sun absorbs the next 3,000 damage to you or an ally. |
| Tyr's Hand | 5 | Buff | Ragnarok's Impositio Manus: Tyr lays his hand on you or an ally, +5 ATK per level. |
| Galdr of Haste | 3 | Buff | Ragnarok's Suffragium: you or an ally cast 15% faster per level. |
| Silencing Rune | 10 | Active | Ragnarok's Lex Divina: a rune of silence. The target can't use skills. |
| Norns' Doom | 1 | Debuff | Ragnarok's Lex Aeterna: the Norns mark a foe; for a few seconds it takes 50% more damage. |
| Banishing Rite | 10 | Magic | Ragnarok's Turn Undead: drive the dead back to Hel. Four times the damage against the Undead and Demons. |
| Rampart of Rúnar | 10 | Buff | Ragnarok's Safety Wall: a rune-wall around you or an ally blocks the next melee hits (one per level). |

#### Monk — Ragnarok's Monk

*Fighting ascetic: spirit spheres, combos, Occult Strike.*

From **Devotee** · Job Lv up to 70 · weapons: Unarmed / Mace / Staff / Knuckles · gift: Iron Knuckles

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Iron Fist | 10 | Passive | +3 ATK per level with knuckles or bare hands. |
| Storm Fists | 10 | Passive | Basic hits can burst into a three-hit combo (higher levels: stronger combo, slightly lower chance). |
| Spirit Call | 5 | Self buff | Summon one Spirit Sphere (+3 ATK each), up to one per level. Spheres fuel Occult Strike, Spirit Barrage and Thunder Palm. |
| Occult Strike | 5 | Attack | A palm strike through armour: ignores DEF and never misses. Uses 1 Spirit Sphere. |
| Spirit Barrage | 5 | Attack | Hurl your Spirit Spheres: one hit per sphere, up to the skill level. |
| Diamond Skin | 5 | Self buff | Harden your body: huge DEF (+100 per level) and MDEF, but -25% movement speed and ASPD. Uses 3 Spirit Spheres. |

#### High Gothi — Ragnarok's High Priest

*High priest of the hof: Meditatio, Assumptio, Holy Judgment.*

From **High Devotee** (reborn after a life as **Gothi**) · Job Lv up to 120 · weapons: Unarmed / Mace / Staff / Rune Tome · gift: Hallowed Mace

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Divine Bulwark | 5 | Buff | You or an ally take 10% less damage per level (half at Lv 5). |
| Hof Meditation | 10 | Passive | Ragnarok's Meditatio: +1% Max SP and +3 SP regen per level. |
| Light of Baldr | 10 | Magic | Ragnarok's Judex: crosses of Baldr's light fall on a spot, three times. |
| Hof Sanctum | 5 | Self buff | Ragnarok's Basilica: consecrate yourself as a sanctum, taking half damage (and 5% less per level). |

#### Champion — Ragnarok's Champion

*Odin's fist: the five-sphere combo ending in Fist of Odin.*

From **High Devotee** (reborn after a life as **Monk**) · Job Lv up to 120 · weapons: Unarmed / Mace / Staff / Knuckles · gift: Fist of Odin Knuckles

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Fist of Odin | 5 | Attack | Drains ALL SP into one lethal, unmissable punch: ATK x (8 + SP/10) plus a flat bonus (+1750 at Lv 5). |
| Aether Snap | 5 | Active | Instant-transmission dash (8 m at Lv 5). |
| Thunder Palm | 10 | Attack | A four-strike palm combo. Uses 1 Spirit Sphere. |
| Zen Breath | 5 | Self buff | Meditate: +10 SP regen and +2% Max SP per level. |

### The Trader line

#### Trader — Ragnarok's Merchant

*A market-town trader: better prices, a cart, and a zeny-backed axe swing.*

From **Initiate** · Job Lv up to 50 · next job at Job Lv 40 · weapons: Unarmed / Dagger / Sword / Mace / Axe / Great Axe · gift: Woodcutter's Axe

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Strong Back | 10 | Passive | Ragnarok's Enlarge Weight Limit: +200 weight capacity per level. |
| Haggle | 10 | Passive | Ragnarok's Discount: NPC shops sell to you 2.4% cheaper per level (24% at Lv 10). |
| Silver Tongue | 10 | Passive | Ragnarok's Overcharge: NPC shops pay you 2.4% more per level (24% at Lv 10). |
| Gold-Strike | 10 | Attack | Ragnarok's Mammonite: a blow backed by coin. Costs 100 zeny per level. |
| Cart Charge | 1 | Attack | Ragnarok's Cart Revolution: ram your target with the Pushcart, hitting everyone beside it. Hits harder the more you carry. |
| Market Shout | 1 | Self buff | Ragnarok's Crazy Uproar: a bellow fit for the market square, +4 STR and +30 ATK. |

#### Runesmith — Ragnarok's Blacksmith

*Dwarf-taught smith: forges runed weapons and hammers foes flat.*

From **Trader** · Job Lv up to 70 · weapons: Unarmed / Dagger / Sword / Mace / Axe / Great Axe · gift: Dwarven Axe

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Weaponry Research | 10 | Passive | Knowing steel from the inside: +2 ATK and +2 HIT per level, and better forging. |
| Rune Forging | 3 | Crafting | Ragnarok's Smith skills: forge weapons from ore and runes at a forge you carry in your head. Each level unlocks a weapon tier. |
| Forge-Tempered Skin | 5 | Passive | Ragnarok's Skin Tempering: years at the anvil, +4 DEF and +1% Max HP per level. |
| Dwarf's Fervor | 5 | Self buff | Ragnarok's Adrenaline Rush: the rhythm of the dwarven forges, +10% ASPD. |
| True Edge | 5 | Self buff | Ragnarok's Weapon Perfection: every swing lands true, +4% physical damage. |
| Hammer Rhythm | 5 | Self buff | Ragnarok's Power-Thrust: strike like a hammer on hot iron, +5% physical damage per level. |
| Full Swing | 5 | Self buff | Ragnarok's Maximize Power: nothing held back, +10% critical damage and +2 CRIT per level. |
| Hammerfall | 5 | Active | Ragnarok's Hammer Fall: strike the ground and stun everything around the spot. |

#### Brewmaster — Ragnarok's Alchemist

*Brewer of potions and bombs, grower of man-eating plants.*

From **Trader** · Job Lv up to 70 · weapons: Unarmed / Dagger / Sword / Mace / Axe / Great Axe · gift: Herbwife's Sickle

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Axe Mastery | 10 | Passive | +3 ATK per level with axes. |
| Potion Research | 10 | Passive | Ragnarok's Potion Research: better brews, and +2 HP and SP regen per level. |
| Brewing | 10 | Crafting | Ragnarok's Pharmacy: brew tonics, sap and bombs from herbs. Higher levels brew more surely. |
| Acid Flask | 5 | Attack | Ragnarok's Acid Terror: a flask of acid that eats through armour (ignores DEF). Can cause bleeding. |
| Firebomb | 5 | Ground | Ragnarok's Bomb (Demonstration): a fire bottle that burns the ground for a while. |
| Mandrake Patch | 5 | Ground | Ragnarok's Summon Flora: plant a shrieking mandrake that lashes out at foes that come near. |
| Potion Toss | 5 | Support | Ragnarok's Aid Potion: throw a healing draught to yourself or an ally. |
| Hallowed Coating | 5 | Buff | Ragnarok's Chemical Protection: a hardening varnish, +8% DEF and MDEF per level for you or an ally. |

#### Forgelord — Ragnarok's Mastersmith

*Heir of Brokkr and Eitri: Cart Termination and masterwork arms.*

From **High Trader** (reborn after a life as **Runesmith**) · Job Lv up to 120 · weapons: Unarmed / Dagger / Sword / Mace / Axe / Great Axe · gift: Eitri's Great Axe

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Brokkr's Cart Crush | 10 | Attack | Ragnarok's Cart Termination: drive the loaded cart through your foe. Paid in zeny, stronger with weight. Can stun. |
| Molten Edge | 10 | Self buff | Ragnarok's Meltdown: steel at forge heat cuts through armour, ignoring 10% of DEF and more per level. |
| Wheel Rush | 1 | Self buff | Ragnarok's Cart Boost: the cart at full tilt, +25% movement speed. |
| Overthrust | 5 | Self buff | Ragnarok's Maximum Over Thrust: every blow at full dwarven strength, +20% physical damage per level. |

#### Lifeweaver — Ragnarok's Biochemist

*Keeper of Iðunn's lore: Acid Demonstration, plant cultivation.*

From **High Trader** (reborn after a life as **Brewmaster**) · Job Lv up to 120 · weapons: Unarmed / Dagger / Sword / Mace / Axe / Great Axe · gift: Iðunn's Golden Sickle

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Acid Bloom | 10 | Attack | Ragnarok's Acid Demonstration: acid and fire together, two hits that ignore DEF. |
| Full Coating | 5 | Buff | Ragnarok's Full Chemical Protection: armour, shield, weapon and helm all varnished at once. |
| Golden Apple Grove | 5 | Ground | Iðunn's own orchard springs up: allies standing in it heal every second. |
| Bountiful Toss | 10 | Support | Ragnarok's Slim Potion Pitcher: lighter, stronger draughts thrown farther. |

### The Scout line

#### Scout — Ragnarok's Thief

*Quick hands and quicker feet: double strikes, poison, stealing.*

From **Initiate** · Job Lv up to 50 · next job at Job Lv 40 · weapons: Unarmed / Dagger / Sword / Bow · gift: Seax

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Twin Fang | 10 | Attack | Two quick stabs. |
| Envenom | 10 | Attack | A poisoned strike that can poison the target (Poison: lose HP over time, -25% DEF). |
| Dust Kick | 5 | Attack | Kick dirt in an enemy's eyes. Can blind (Blind: -25% HIT and FLEE). |
| Pilfer | 10 | Active | Try to steal one of a monster's drops (once per monster). Better with DEX and against weaker monsters. |
| Evasion Drills | 10 | Passive | +3 FLEE per level. |
| Keen Edge | 10 | Passive | With daggers, each basic hit has a 5% chance per level to strike again. |
| Hide in Shadows | 10 | Self buff | Ragnarok's Hiding: melt into the shadows. Monsters can't see you, but you can barely move. |
| Purge Venom | 1 | Active | Ragnarok's Detoxify: draw the poison, and every other ill, out of yourself or an ally. |

#### Assassin — Ragnarok's Assassin

*Katar killer of the shadows: cloaking, poison, Sonic Blow.*

From **Scout** · Job Lv up to 70 · weapons: Unarmed / Dagger / Sword / Katar · gift: Jamadhar

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Katar Mastery | 10 | Passive | +3 ATK and +1 CRIT per level with katars. |
| Shadow Cloak | 10 | Self buff | Vanish: monsters lose track of you and can't target you. You move slowly (less so at higher levels). Attacking or using a skill reveals you. |
| Venom Dust | 10 | Ground | Leave a cloud of venom on the ground that poisons enemies standing in it. |
| Underfang | 5 | Attack | Strike from the shadows at everything around your target. Out of Shadow Veil it is a critical ambush. |
| Lacerate | 10 | Attack | Two tearing cuts that can cause Bleeding (HP loss over time, no natural regen). |
| Viper's Burst | 10 | Attack | Ragnarok's Venom Splasher: poison bursts out of the wound. Always poisons. |

#### Outlaw — Ragnarok's Rogue

*Loki's own: strips foes, copies skills, snatches zeny.*

From **Scout** · Job Lv up to 70 · weapons: Unarmed / Dagger / Sword / Bow · gift: Cutpurse Dagger

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Outlaw's Blade | 10 | Passive | Ragnarok's Sword Mastery for Rogues: +4 ATK per level with daggers and swords. |
| Light Fingers | 10 | Passive | Ragnarok's Snatcher: your basic attacks sometimes Pilfer by themselves (1% + 1% per level). |
| Knife in the Back | 10 | Attack | Ragnarok's Back Stab: a strike that never misses. |
| Ambush Raid | 5 | Attack | Ragnarok's Raid: burst out on everyone around you. Can stun. |
| Cut Purse | 10 | Active | Ragnarok's Steal Coin: lift a handful of zeny from a monster (once each, DEX and LUK against its level). |
| Divest Helm | 5 | Debuff | Ragnarok's Strip Helm: knock the helm off, weakening its MDEF and aim. |
| Divest Shield | 5 | Debuff | Ragnarok's Strip Shield: tear the shield away, weakening its DEF. |
| Divest Armour | 5 | Debuff | Ragnarok's Strip Armor: rip the armour off, weakening its DEF and speed. |
| Divest Weapon | 5 | Debuff | Ragnarok's Strip Weapon: wrench the weapon away, weakening its blows. |
| Loki's Mimicry | 10 | Passive | Ragnarok's Plagiarism: the last first- or second-job skill a nearby ally uses is copied, usable up to this level. +1% ASPD per level. |

#### Shadow Walker — Ragnarok's Assassin Cross

*Death from the dark: Phantom Barrage, Miasma Weapon, Shadow Veil.*

From **High Scout** (reborn after a life as **Assassin**) · Job Lv up to 120 · weapons: Unarmed / Dagger / Sword / Katar · gift: Shadow Katar

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Phantom Barrage | 10 | Attack | Rapid 8-hit strike with a guaranteed stun. |
| Miasma Weapon | 5 | Self buff | Poison your blades for 40 s: physical damage x2 at Lv 1, +50% per level (x4 at Lv 5). |
| Shadow Veil | 10 | Self buff | Conceal yourself in darkness. Your first attack out of the veil is a guaranteed critical backstab. |
| Shadow Fang | 10 | Attack | Hurl a blade of shadow at a distant enemy. Never misses. |
| Lethal Precision | 10 | Passive | +1 CRIT per level with katars and daggers. |
| Night Squall | 10 | Attack | Ragnarok's Meteor Assault: a storm of blows around you. Can stun. |
| Soul Breaker | 10 | Attack | Ragnarok's Soul Destroyer: a thrown blade that strikes body and spirit from range. |

#### Vargr — Ragnarok's Stalker

*The wolf-outlaw: Full Strip, Chase Walk, Preserve.*

From **High Scout** (reborn after a life as **Outlaw**) · Job Lv up to 120 · weapons: Unarmed / Dagger / Sword / Bow · gift: Loki's Sting

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Wolf-Strip | 5 | Debuff | Ragnarok's Full Strip: weapon, shield, armour and helm all at once. |
| Wolf's Prowl | 5 | Self buff | Ragnarok's Chase Walk: hidden, yet moving at full stride. Breaking it lends +10 ATK per level. |
| Preserve | 1 | Self buff | Keep the skill Loki's Mimicry holds: nothing new is copied for 10 minutes. |
| Turn the Blade | 5 | Self buff | Ragnarok's Reject Sword: blades glance off you, some turned back on their wielders. |

### Expanded jobs

#### Wanderer — Ragnarok's Super Novice

*An Initiate who never chose: learns every first job's skills. Needs Base Level 45.*

From **Initiate** · Job Lv up to 99 · weapons: Unarmed / Dagger / Sword / Mace / Staff / Axe · gift: Seax

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Wanderer's Luck | 10 | Passive | The road favours those who never chose one: +1 LUK per level. |
| Many Roads | 10 | Passive | A little of every path: +1% Max HP and Max SP per level. |
| Norns' Favour | 5 | Self buff | Ragnarok's guardian angel of the Super Novice: the Norns watch over you, +10 ATK, +10 MATK and +1 CRIT per level. |
| Last Stand | 1 | Self buff | Ragnarok's Steel Body for the Super Novice: 60% less damage taken for 10 seconds, but slower. |

#### Glíma Fighter — Ragnarok's Taekwon

*Norse wrestler-kicker: stances, kicks and tumbling. Fights bare-handed.*

From **Initiate** · Job Lv up to 50 · next job at Job Lv 40 · weapons: Unarmed · gift: bare hands

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Whirlwind Kick | 7 | Attack | Ragnarok's Tornado Kick: a spinning kick that hits everyone around you. |
| Heel Drop | 7 | Attack | Ragnarok's Axe Kick: the heel comes down like an axe. Can stun. |
| Roundhouse | 7 | Attack | Ragnarok's Roundhouse Kick: a wide kick that knocks the target back. |
| Glíma Throw | 7 | Attack | Ragnarok's Counter Kick, as Norse wrestling: catch the foe and throw it down. Never misses. |
| Sprint | 10 | Self buff | Ragnarok's Running: break into a sprint, +20% movement speed and more per level. |
| Leaping Kick | 7 | Attack | Ragnarok's Flying Side Kick: close the distance in one leap. |
| Tumbling | 5 | Passive | Roll with the blows: +4 FLEE per level. |
| Warrior's Rest | 10 | Passive | Ragnarok's Peaceful and Happy Break: +4 HP and +2 SP regen per level. |

#### Sól Guardian — Ragnarok's Star Gladiator

*Sworn to Sól, Máni and the stars: celestial kicks and auras.*

From **Glíma Fighter** · Job Lv up to 70 · weapons: Unarmed · gift: bare hands

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Sól's Warmth | 5 | Attack | Ragnarok's Warmth of the Sun: Sól's fire burns everyone around you for a few seconds. |
| Máni's Chill | 5 | Attack | Ragnarok's Warmth of the Moon: Máni's cold light sears everyone around you. |
| Glow of the Stars | 5 | Attack | Ragnarok's Warmth of the Stars: starlight burns everyone around you. |
| Sun Kick | 5 | Attack | Ragnarok's Solar Kick: a kick wreathed in Sól's fire. |
| Moon Kick | 5 | Attack | Ragnarok's Lunar Kick: a cold crescent kick around you. |
| Star Kick | 5 | Attack | Ragnarok's Stellar Kick: a falling-star kick. Can stun. |
| Comfort of the Stars | 5 | Self buff | Ragnarok's Comfort of the Stars: +3% ASPD and +3 HIT per level. |
| Sól's Protection | 5 | Self buff | Ragnarok's Solar Protection: +6% DEF per level. |
| Union of Sun, Moon and Stars | 1 | Self buff | Ragnarok's Union (Fusion): become one with Sól, Máni and the stars. +20% damage and ASPD, hyper armour. Costs 10% HP. |

#### Fylgja Caller — Ragnarok's Soul Linker

*Calls the fylgjur, guardian spirits, to bind with allies.*

From **Glíma Fighter** · Job Lv up to 70 · weapons: Unarmed / Dagger / Staff · gift: Fylgja Wand

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Fylgja Strike | 10 | Magic | Ragnarok's Esma: your fylgja strikes once per level. |
| Spirit Push | 7 | Magic | Ragnarok's Estin: a spirit's shove that knocks the target back. |
| Spirit Stun | 7 | Magic | Ragnarok's Estun: a spirit's blow to the head. Can stun. |
| Hamingja | 7 | Buff | Ragnarok's Kaizel, as the Norse luck-spirit: it stands guard over you or an ally, absorbing the next 5,000 damage. |
| Fylgja's Mend | 7 | Buff | Ragnarok's Kaahi: a spirit tends your wounds, +8 HP regen per level for you or an ally. |
| Spirit Dodge | 3 | Buff | Ragnarok's Kaupe: a spirit tugs you out of harm's way, +8 FLEE per level for you or an ally. |
| Spirit Mirror | 7 | Buff | Ragnarok's Kaite: a spirit-mirror reflects 10% of spells per level. |
| Fylgja Bond | 5 | Buff | Ragnarok's Spirit Links: bind a guardian spirit to an ally, +5 to every stat and +5% damage. |

#### Thunderer — Ragnarok's Gunslinger

*Wields Thor's lightning through iron thunder-rods; flips Thor's coins.*

From **Initiate** · Job Lv up to 70 · weapons: Unarmed / Thunder-Rod · gift: Spark Rod

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Thor's Coin | 10 | Self buff | Ragnarok's Flip the Coin: flip one of Thor's coins into your pouch (up to one per level, 10 at most). +2 HIT each. |
| Snake Eye | 10 | Passive | +1 HIT and +0.3 m of reach per level with thunder-rods. |
| Triple Thunder | 1 | Attack | Ragnarok's Triple Action: three cracks in a blink. Spends 1 coin. |
| Thor's Eye | 1 | Attack | Ragnarok's Bull's Eye: a shot straight through the heart. Spends 1 coin. Can stun. |
| Rapid Thunder | 10 | Attack | Ragnarok's Rapid Shower: five shots fanned off the hip. |
| Storm Spin | 10 | Attack | Ragnarok's Desperado: spin and fire everywhere at once. Spends 1 coin. |
| Tracking | 10 | Attack | Take your time and follow the mark: a long-aimed shot that never misses. |
| Disarm | 5 | Debuff | Ragnarok's Disarm: shoot the weapon out of a foe's grip. |
| Rending Shot | 5 | Attack | Ragnarok's Piercing Shot: a round that goes through armour. Can cause bleeding. |
| Spread Shot | 10 | Attack | Ragnarok's Spread Attack: a burst of shot over the target and everyone near it. |
| Dust Blast | 10 | Attack | Ragnarok's Dust: a blast that throws the target back. |
| Thunder Fever | 10 | Self buff | Ragnarok's Gatling Fever: a storm of fire, +20% ASPD and +10 ATK per level, but slow on your feet. Spends 1 coin. |
| Steady Aim | 1 | Self buff | Ragnarok's Increasing Accuracy: +20 HIT, +4 DEX and +4 AGI. Spends 4 coins. |
| Mjölnir's Stillness | 1 | Self buff | Ragnarok's Madness Canceller: plant yourself like Thor's hammer, +100 ATK and +20% ASPD, barely moving. Spends 4 coins. |
| Weather-Eye | 1 | Self buff | Ragnarok's Adjustment: watch the storm and sidestep it, +30 FLEE but -30 HIT. Spends 2 coins. |

#### Nightraider — Ragnarok's Ninja

*Raider from the long night: thrown blades, shadow steps, rune-fire.*

From **Initiate** · Job Lv up to 70 · weapons: Unarmed / Dagger / Huuma Shuriken · gift: Iron Huuma

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Night Training | 10 | Passive | Ragnarok's Ninja Mastery: +3 SP regen and +2 FLEE per level. |
| Throw Shuriken | 10 | Attack | Ragnarok's Throw Shuriken: a quick star of iron. |
| Throw Kunai | 5 | Attack | Ragnarok's Throw Kunai: three knives at once. |
| Huuma Storm | 5 | Attack | Ragnarok's Throw Huuma Shuriken: the great shuriken whirls through the target and everyone near it. |
| Hurl Gold | 10 | Attack | Ragnarok's Throw Zeny: hurl a fistful of gold. It never misses and ignores armour, but it costs zeny. |
| Shield Flip | 5 | Attack | Ragnarok's Flip Tatami, with a Viking round shield: flip it up and slam everyone around you back. |
| Mist Slash | 10 | Attack | Ragnarok's Haze Slasher: cut through the fog around you. |
| Shadow Leap | 5 | Active | Ragnarok's Shadow Leap: step through the shadows to a spot. |
| Shadow Slash | 5 | Attack | Ragnarok's Shadow Slash: a cut from the dark that can strike critically. |
| Shed Skin | 5 | Self buff | Ragnarok's Cicada Skin Shedding: leave a husk behind, blocking the next melee hits (one per level). |
| Mirror Image | 5 | Self buff | Ragnarok's Mirror Image: shadow copies take blows meant for you, +10% block chance per level. |
| Muspel Blossom | 10 | Magic | Ragnarok's Crimson Fire Formation: a flower of fire opens around you. |
| Thunder Jolt | 10 | Magic | Ragnarok's Lightning Jolt: lightning strikes the ground where you point. |
| Hail of Niflheim | 10 | Magic | Ragnarok's Lightning Spear of Ice: one shard of Niflheim's ice per level. |

#### Freyja's Kin — Ragnarok's Doram (Summoner)

*Cat-folk of Freyja's chariot (Bygul and Trjegul's kin). Chosen when a character is made.*

From **character creation** · Job Lv up to 70 · weapons: Unarmed / Cat Staff · gift: Bygul's Staff

| Skill | Max Lv | Type | What it does |
|---|---|---|---|
| Cat's Bite | 5 | Attack | Ragnarok's Bite: teeth, quick and sharp. |
| Bygul's Claws | 5 | Attack | Ragnarok's Scar of Tarou, after Freyja's cat Bygul: raking claws that leave bleeding scars. |
| Root Smash | 5 | Attack | Ragnarok's Lunatic Carrot Beat: a giant root crashes down on the target and everyone near it. Can stun. |
| Pounce | 5 | Attack | Ragnarok's Arclouse Dash: a cat's pounce onto the target. |
| Falcons of Freyja | 5 | Attack | Ragnarok's Picky Peck: falcons from Freyja's feather-cloak dive five times at the target. |
| Catnip Meteor | 5 | Magic | Ragnarok's Catnip Meteor: a shower of giant catnip falls on a spot, three times. |
| Silvervine Spear | 5 | Magic | Ragnarok's Silvervine Stem Spear: a spear of living vine. |
| Gift of Njörðr | 5 | Passive | Ragnarok's Power of Sea, from Freyja's father the sea god: +1% Max HP and SP and +2 HP regen per level. |
| Spirit of Life | 5 | Passive | Ragnarok's Spirit of Life: the life in all things answers you, +2% magic damage per level. |
| Fish Feast | 5 | Buff | Ragnarok's Tuna Party: a feast for you or an ally that absorbs the next 2,500 damage. |
| Purr of Comfort | 5 | Buff | Ragnarok's Purring: a steady purr, +6 HP regen per level for you or an ally. |
| Hiss | 5 | Self buff | Ragnarok's Hiss: fur on end, +10 FLEE per level. |
| Chattering | 5 | Self buff | Ragnarok's Chattering: the hunting chatter of a cat at the window, +15 ATK and MATK per level. |

