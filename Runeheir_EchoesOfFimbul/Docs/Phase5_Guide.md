# RUNEHEIR — Phase 5 Guide: World Maps & Monster AI

Phase 5 opens up Midgard. The single test field becomes **14 maps**: the capital Vigrid Haven, four leveling fields from Lv 1 to 255, the four floors of the Catacombs of Helheim, the three Sunken Fjord Caverns, the Hall of Branches and Lyngvi, where Fenrir lies bound. **35 monsters** live there, one for every Soul Card. They now fight with **skills**: cast bars, circles on the ground you can step out of, pack calls and summons. **Four mini-bosses** and **three MVPs** guard the deepest places, on respawn timers.

| GDD item | Where it lives |
|---|---|
| §2: the 14 maps and how they link | `Scripts/Core/World/MapCatalog.cs`, `MapDefinition.cs` |
| Map generation (ground, rooms, props, spawns) | `Scripts/Core/World/MapLayoutGenerator.cs`, `MapLayout.cs` (engine-free, deterministic per seed) |
| Drawing a map in Unity, its light and its NavMesh | `Scripts/Runtime/World/WorldBuilder.cs`, `PropFactory.cs` |
| Portals, travel, save points | `Runtime/World/WarpPortal.cs`, `WorldTravel.cs`; `Runtime/Field/FieldBootstrap.cs` |
| Minimap and map banner | `Runtime/UI/Hud/MinimapView.cs`, `MapBanner.cs` |
| §6: the full bestiary | `Scripts/Core/Monsters/MonsterCatalog.Fields.cs`, `.Dungeons.cs`, `.Bosses.cs` |
| Monster skills, boss phases, monster buffs | `Core/Monsters/MonsterSkills.cs`, `MonsterBuffs.cs`; AI in `Runtime/Combat/Monster.cs` |
| MVP rewards, boss timers, tombstones | `Core/Monsters/MvpRules.cs`, `Core/World/BossTracker.cs`; `Runtime/World/BossSpawner.cs` |
| Weapon break and Brokk's repairs | `Core/Items/WeaponBreak.cs`; `Runtime/Player/PlayerCharacter.cs` |

---

## 1. Try it in 5 minutes

1. Download the branch again and open the project in Unity 6.3.
2. Run **Runeheir ▸ Setup ▸ Build Prototype Scenes**. **This step is required:** it creates the new `RH_World` scene, adds it to Build Settings, and removes the old `RH_Field_WhisperwoodPlains` scene.
3. Press **Play** in `RH_Login`.
   - **New characters** start in **Vigrid Haven**.
   - **Phase 2–4 characters** stay where they were saved, on the Whisperwood Plains.
4. In Vigrid, walk around the plaza and talk to the NPCs. The **south gate** (rose portal) leads to the plains, and the **west door** leads to the Hall of Branches.
5. A fast tour (press **Enter**, then type):
   - `@blvl 255`, `@allstats 200`, `@job berserker`, `@allskills`
   - `@warp lyngvi`: Fenrir waits at the top of the island. Hit him and watch his phases at 70% and 35% HP.
   - `@bosses`: every boss and its timer. `@bosstime 0.01` makes MVPs come back in about 36 seconds; `@bossrespawn` brings every boss back now.
   - `@warp helheim_b1`, then take the stairs down. Each floor of the Catacombs is a different set of rooms.
   - `@breakweapon`, then talk to Brokk in Vigrid to repair it.

Played directly, `RH_World` starts a temporary character in Vigrid. To start somewhere else, change **Map Id** on the scene's `WorldBootstrap` object (for example `jotun_steppe`).

## 2. How the world is built

- **One scene for every map.** `RH_World` holds only a camera, a sun and the `WorldBootstrap`. When it loads, it builds the map the character is on:
  1. `MapLayoutGenerator` turns the map's data into a grid of cells, rooms, walls and props. The same seed always gives the same map.
  2. `WorldBuilder` draws it. Ground cells are colored by type (grass, path, paving, snow, ice). Fjord inlets and the sea around Lyngvi are water. Rooms in the dungeons and the arena are cut out of dark rock with walls.
  3. `PropFactory` places the scenery: trees, rocks, runestones, the mead hall, braziers, crystals, wrecks, giants' skulls.
  4. The NavMesh is baked from the ground and the scenery. Solid props are carved out, so you walk around them.
  5. Portals, NPCs, monster spawns and boss lairs go in last.
- **Changing map** saves the character and reloads `RH_World` for the new map. Buffs end when you change maps.
- **Themes:** each map has its own light, fog and colors.
  - Vigrid has golden afternoon light; the Whispering Woods are autumn birch; the fjord is grey and cold.
  - The Jotun Steppe and Lyngvi are snow under pale skies.
  - The Catacombs are lit by blue spirit flames, and the Caverns by glowing ice crystals.
- **Placeholder art:** everything is still built from primitives until the Blender pass.

## 3. Getting around

- **Portals:** walk into a swirling ring to travel. Its label names where it leads and the level range there.
  - Roads between maps glow **rose**; dungeon stairs glow **blue**.
  - You arrive a few steps inside the matching portal on the other side.
- **Map banner:** the map's name fades in at the top of the screen when you arrive.
- **Minimap** (top-right): the whole map, turned with the camera so that up on the minimap is up on screen.
  - Markers: portals (rose), NPCs (gold), the save point (blue), living bosses (red) and you (white, pointing where you face).
  - The map name and your coordinates are shown above it.
  - **Ctrl+Tab** switches between small, large and hidden.
- **Norn Couriers** stand at every field camp and in Vigrid. They offer:
  - **Open storage:** the same account storage as Phase 4.
  - **Save my return point here:** you revive here when you die, and Raven Feathers bring you here.
  - **Teleport:** to Vigrid, the four fields, or the first floor of either dungeon, for zeny (table below). You arrive at that map's save point.
- **Death:** **Return to Save Point** revives you at your saved map's save point, even from another map. Logging out while dead also puts you back there.
- **Raven Feather:** takes you to your save point on your saved map.
- **Wind Rune Shard:** a random teleport on the current map. It doesn't work in Vigrid or the Hall of Branches.
- **Branches:** they can't be cracked inside Vigrid Haven. Use the Hall of Branches (Ylva sells them there), or any field.

## 4. Vigrid Haven and the Hall of Branches

| NPC | Where | What they do |
|---|---|---|
| **Ásta** [Trading Post] | South-west of the plaza | Supplies and starter gear; buys anything |
| **Hrafn** [Vigrid Armory] | South-east of the plaza | Claymores, chainmail, the mystic robe, shields, headgear, accessories |
| **Brokk** [Grand Dwarven Forge] | By the forge hut, east | **Repairs broken weapons** (listed first when you need it), refining, Runic Fuller, card extraction, ores and runes |
| **Verdandi's Courier** [Norn Courier] | South road | Storage, save point, teleports |
| **Sigrun** [Guild Hall] | West, by the guild hall | **Job change** (opens the job window) |
| **Ylva** [Branch Warden] | Hall of Branches | Dead Branches, potions, Sowilo runes |

The **Hall of Branches** is a walled arena under the mead hall. Crack Dead Branches (any normal monster) and Blood Branches (any mini-boss or MVP, with full MVP rewards) there.

## 5. Monster AI

Every monster now thinks four times a second:

1. **Wander** near its spawn.
2. **Aggro:** aggressive kinds attack you on sight; the others only fight back.
3. **Call for help:** pack monsters (wolves, boars, outlaws, draugr, berserkers, naga scouts) bring nearby packmates of the same kind when one is hit. A boss's summons always defend it.
4. **Fight:** chase and auto-attack. Ranged monsters (Forest Outlaws and Banshees) shoot from range.
5. **Use skills:** a monster checks its skills in priority order and uses the first one that is ready, makes sense right now, and passes its chance roll. There is a short global cooldown between skills.
6. **Leash:** past its range, a monster walks home, heals to full and resets its fight (a boss also resets its phases).

**Skill kinds:**

| Kind | What it does | How to play against it |
|---|---|---|
| Strike | A heavy blow (bleeds, stuns, roots, sometimes breaks weapons) | Ones with a cast bar miss if you step out of reach before they land ("Dodged!") |
| Bolt | A spell or shot that flies at you | Can't be dodged once released. Hagalaz Rebound reflects the spells |
| Area | A blast on a circle shown on the ground while the cast bar fills (at your feet, or around the monster) | Step out of the circle |
| Leap | Jumps onto the spot you stood on when the cast started, then blasts the landing circle | Move away |
| Buff / Heal | Hardens, enrages, howls for the pack, heals itself and its allies | Kill it fast, or stagger the cast |
| Summon | Calls its minions; never while they're still alive | Minions vanish when their master dies |
| Teleport | Blinks behind you, or away when it's hurt | — |

- **Cast bars:** they appear over a monster's head, red and labeled with the skill.
- **Interrupting:** a stagger (poise break), stun, freeze, sleep, stone curse or silence cancels the cast ("Interrupted!").
- **Status immunities:**
  - Bosses ignore stun, freeze, stone curse, sleep, root, silence and curse. They can still be poisoned, bled, blinded, frostbitten and staggered, but their poise is 3× (mini-boss) or 5× (MVP) larger.
  - Undead-element monsters can't be frozen or petrified, and Water-element monsters can't be frozen.

## 6. Bosses and MVPs

- **Lairs:** each mini-boss and MVP has a lair on its map. It's there unless it was killed and its timer hasn't run out:
  - mini-bosses return about **2 hours** later;
  - MVPs return about **1 hour** later;
  - either way ± 10 minutes, so nobody can camp the exact second.
- **Tombstones:** while a boss is away, a tombstone in its lair shows who slew it and at what time.
- **Phases:** bosses change phase as their HP falls.
  - Mini-bosses have one phase; MVPs have two (70% and 35%).
  - Each phase brings a lasting buff (Enraged, then Unbound) and a shout in chat, and unlocks new skills.
  - Fenrir calls Sköll and Hati, Hel's Vanguard raises Frozen Revenants, and Jormungandr's Brood spawns Abyssal Leeches.
- **MVP rewards:** the player who dealt the most damage is the **MVP**.
  - They get the bonus MVP EXP and one roll down the MVP drop list. The first entry that hits goes straight into their bag.
  - The godly accessories come from here: Megingjard from Fenrir, Brisingamen from Hel's Vanguard, Mjolnir from Jormungandr's Brood.
  - Normal drops go to whoever landed the killing blow, and EXP is shared by damage, as for any monster.
- **Boss timers live in memory for the play session.** Phase 6 moves them to the map server. They reset when you restart the game.

## 7. Weapon break and repair

- **Which skills break weapons:** Axe Rend, Sunder Strike, Bone Crusher, Execution, Crushing Fist, Hel's Judgment and Devouring Bite each have a 2–5% chance per hit to **break your weapon**.
- **What breaking does:** the weapon stays in your hands but does nothing. You fight bare-handed until it's repaired.
- **Repairs:** Brokk repairs every broken weapon you carry or wear for **1,000 × weapon level² zeny** (+10% per refine level).
- **Prevention:** the **Ancient Golem Card** makes your weapon unbreakable.
- **Restrictions:** broken weapons can't be refined until they're repaired.

## 8. What changed from Phase 4

- **The Whisperwood Plains were re-tuned to cover Lv 1–60.** The six starter monsters now run Lv 3 to Lv 48 (Field Beetle 7, Toxic Spore 13, Forest Imp 22, Horned Grazer 34, Wood Sprite 48), with HP, ATK and EXP to match. Before, they stopped at Lv 24.
- **New characters start in Vigrid Haven** instead of the plains.
- **Every item can now be obtained.** All 87 equipment pieces are sold in a shop or dropped by a monster, and a Core test checks it.
- **Blood Branch** summons a mini-boss or MVP instead of the old Lv 70+ test monsters. **Dead Branch** summons any normal monster.
- **The buff icons** moved left to make room for the minimap.

## 9. Maps, monsters, bosses and drops

These tables are generated from the game data.

### The 14 maps

| Map | Kind | Levels | Monsters | Boss | Exits |
|---|---|---|---|---|---|
| **Vigrid Haven** | Town | Safe zone |  |  | Whisperwood Plains, Hall of Branches |
| **Hall of Branches** | Arena | Safe zone |  |  | Vigrid Haven |
| **Whisperwood Plains** | Field | Lv 1–60 | Rune Spore, Field Beetle, Toxic Spore, Forest Imp, Horned Grazer, Wood Sprite |  | Vigrid Haven, Whispering Woods |
| **Whispering Woods** | Field | Lv 61–120 | Wild Boar, Dire Wolf, Forest Outlaw | Elder Direwolf (mini-boss, 2 h) | Whisperwood Plains, Howling Fjord, Catacombs of Helheim |
| **Howling Fjord** | Field | Lv 121–180 | Fjord Harpy, Frost Wolf, Runic Berserker, Sea Drake | Draugr Warlord (mini-boss, 2 h) | Whispering Woods, Jotun Steppe, Sunken Fjord Caverns |
| **Jotun Steppe** | Field | Lv 181–255 | Ice Golem, Snow Harpy, Jotun Brawler, Frost Wyrm | Ancient Golem (mini-boss, 2 h) | Howling Fjord, Lyngvi |
| **Lyngvi, Isle of the Bound Wolf** | Lair | Lv 240–255 | Frost Wyrm, Snow Harpy | Fenrir, the Bound Wolf (MVP, 1 h) | Jotun Steppe |
| **Catacombs of Helheim B1** | Dungeon | Lv 90–110 | Crypt Bat, Draugr Footman |  | Whispering Woods, Catacombs B2 |
| **Catacombs of Helheim B2** | Dungeon | Lv 120–170 | Ghoul, Crypt Wraith, Crypt Bat |  | Catacombs B1, Catacombs B3 |
| **Catacombs of Helheim B3** | Dungeon | Lv 175–215 | Banshee, Corrupted Einherjar, Crypt Wraith |  | Catacombs B2, Catacombs B4 |
| **Catacombs of Helheim B4** | Dungeon | Lv 220–255 | Frozen Revenant, Hel's Executioner | Hel's Vanguard (MVP, 1 h) | Catacombs B3 |
| **Sunken Fjord Caverns 1** | Dungeon | Lv 130–175 | Cave Crawler, Naga Scout, Sea Drake |  | Howling Fjord, Caverns 2 |
| **Sunken Fjord Caverns 2** | Dungeon | Lv 185–235 | Ice Golem, Abyssal Leech, Naga Scout | Naga Queen (mini-boss, 2 h) | Caverns 1, Caverns 3 |
| **Sunken Fjord Caverns 3** | Dungeon | Lv 235–255 | Abyssal Leech, Frost Wyrm, Ice Golem | Jormungandr's Brood (MVP, 1 h) | Caverns 2 |

### Norn Courier teleports

| Destination | Zeny |
|---|---|
| Vigrid Haven | 600 |
| Whisperwood Plains | 500 |
| Whispering Woods | 1,500 |
| Howling Fjord | 3,000 |
| Jotun Steppe | 5,000 |
| Catacombs of Helheim B1 | 2,500 |
| Sunken Fjord Caverns 1 | 4,000 |

### Bestiary

| Monster | Lv | Element · Race · Size | HP | ATK | DEF / MDEF | EXP (base / job) | Behaviour | Skills | Lives in |
|---|---|---|---|---|---|---|---|---|---|
| **Rune Spore** | 3 | Earth · Plant · Small | 70 | 8–12 | 0 / 0 | 15 / 12 | retaliates | — | Whisperwood Plains |
| **Field Beetle** | 7 | Earth · Insect · Small | 175 | 18–22 | 12 / 2 | 40 / 32 | retaliates | **Harden** (Harden, below 50% HP) | Whisperwood Plains |
| **Toxic Spore** | 13 | Poison · Plant · Small | 375 | 33–40 | 3 / 0 | 95 / 75 | retaliates | **Spore Burst** (around itself r2.5 80%, Poison, Poison 30%, cast 1s) | Whisperwood Plains |
| **Forest Imp** | 22 | Shadow · Demon · Small | 800 | 65–80 | 5 / 5 | 225 / 180 | aggressive | **Blink** (blinks away, below 30% HP); **Imp Bolt** (spell 120%, 7 m, Shadow, cast 0.8s) | Whisperwood Plains |
| **Horned Grazer** | 34 | Earth · Beast · Medium | 2,140 | 120–145 | 10 / 4 | 600 / 480 | retaliates | **Horn Charge** (leap 3–9 m, r1.5 150%, Stun 20%, knockback, cast 0.6s) | Whisperwood Plains |
| **Wood Sprite** | 48 | Wind · Plant · Small | 3,510 | 195–240 | 10 / 20 | 1,360 / 1,090 | aggressive | **Sap Mending** (heal 15%, cast 1s, below 50% HP); **Gale Leaf** (spell 130%, 7 m, Wind, cast 1s) | Whisperwood Plains |
| **Wild Boar** | 64 | Earth · Beast · Medium | 8,600 | 320–395 | 29 / 8 | 3,190 / 2,550 | retaliates, pack | **Frenzy** (Frenzied, below 40% HP); **Tusk Charge** (leap 3–10 m, r1.5 160%, knockback, cast 0.6s) | Whispering Woods |
| **Dire Wolf** | 70 | Earth · Beast · Medium | 9,000 | 380–460 | 35 / 15 | 4,200 / 3,300 | aggressive, pack | **Pack Howl** (Pack Howl (pack)); **Rending Bite** (strike 180%, Bleeding 30%) | Whispering Woods |
| **Forest Outlaw** | 86 | Neutral · DemiHuman · Medium | 14,300 | 530–650 | 43 / 12 | 6,100 / 4,870 | aggressive, pack, ranged 7 m | **Blinding Shot** (shot 150%, 9 m, Blind 40%, cast 0.6s); **Arrow Volley** (at target r3 140%, cast 1.2s) | Whispering Woods |
| **Crypt Bat** | 94 | Shadow · Beast · Small | 13,900 | 610–750 | 40 / 20 | 7,200 / 5,700 | aggressive | **Blood Drain** (strike 140%, drains 50%) | Catacombs of Helheim B1, Catacombs of Helheim B2 |
| **Draugr Footman** | 104 | Undead · Undead · Medium | 22,700 | 730–890 | 79 / 20 | 8,800 / 7,000 | aggressive, pack | **Shield Wall** (Shield Wall, below 50% HP); **Shield Bash** (strike 130%, Stun 25%) | Catacombs of Helheim B1 |
| **Fjord Harpy** | 124 | Wind · Demon · Medium | 36,300 | 1,030–1,260 | 68 / 30 | 13,400 / 10,700 | aggressive | **Gust** (around itself r3.5 120%, Wind, knockback, cast 0.8s); **Dive** (leap 4–10 m, r1.5 170%, cast 0.5s) | Howling Fjord |
| **Ghoul** | 128 | Undead · Undead · Medium | 58,200 | 1,090–1,340 | 71 / 25 | 14,500 / 11,600 | aggressive | **Feast** (heal 10%, cast 1.2s, below 50% HP); **Rotting Claw** (strike 150%, Poison 40%) | Catacombs of Helheim B2 |
| **Cave Crawler** | 136 | Earth · Insect · Small | 54,600 | 1,230–1,510 | 154 / 20 | 16,800 / 13,400 | aggressive | **Burrow** (blinks away, below 30% HP); **Petrifying Spit** (spell 120%, 7 m, Earth, StoneCurse 25%, cast 1s) | Sunken Fjord Caverns 1 |
| **Frost Wolf** | 140 | Water · Beast · Medium | 60,000 | 1,300–1,600 | 80 / 40 | 18,000 / 14,000 | aggressive, pack | **Pack Howl** (Pack Howl (pack)); **Frost Bite** (strike 180%, Water, Freeze 15%) | Howling Fjord |
| **Runic Berserker** | 152 | Fire · DemiHuman · Medium | 79,200 | 1,820–2,220 | 71 / 20 | 21,700 / 17,400 | aggressive, pack | **Berserk** (Berserk, below 50% HP); **Leaping Cleave** (leap 4–9 m, r2.5 200%, cast 0.8s); **Axe Rend** (strike 220%, breaks weapons 2%) | Howling Fjord |
| **Crypt Wraith** | 154 | Ghost · Demon · Medium | 74,500 | 1,630–1,990 | 60 / 40 | 22,400 / 17,900 | aggressive | **Phase Shift** (blinks behind you); **Soul Drain** (spell 150%, 7 m, Ghost, drains 30%, cast 1.2s) | Catacombs of Helheim B2, Catacombs of Helheim B3 |
| **Naga Scout** | 158 | Water · Fish · Medium | 90,200 | 1,730–2,110 | 93 / 35 | 23,700 / 19,000 | aggressive, pack | **Coil** (strike 150%, Root 30%); **Venom Javelin** (shot 160%, 9 m, Poison, Poison 40%, cast 0.8s) | Sunken Fjord Caverns 1, Sunken Fjord Caverns 2 |
| **Sea Drake** | 166 | Water · Dragon · Large | 139,000 | 1,940–2,370 | 99 / 45 | 26,600 / 21,300 | aggressive | **Tail Sweep** (around itself r3 160%, knockback, cast 0.6s); **Tidal Breath** (at target r3.5 180%, Water, cast 1.5s) | Howling Fjord, Sunken Fjord Caverns 1 |
| **Banshee** | 182 | Shadow · Undead · Medium | 132,000 | 2,400–2,940 | 80 / 60 | 32,900 / 26,300 | aggressive, ranged 6 m | **Wail** (around itself r6 130%, Shadow, Silence 40%, cast 1.5s); **Shadow Bolt** (spell 180%, 8 m, Shadow, cast 1.2s) | Catacombs of Helheim B3 |
| **Ice Golem** | 196 | Water · Formless · Large | 282,000 | 3,160–3,860 | 188 / 50 | 39,800 / 31,800 | retaliates | **Ice Armor** (Ice Armor, below 60% HP); **Glacial Slam** (around itself r3.5 200%, Water, Freeze 25%, cast 1.4s) | Jotun Steppe, Sunken Fjord Caverns 2, Sunken Fjord Caverns 3 |
| **Corrupted Einherjar** | 198 | Shadow · DemiHuman · Medium | 211,000 | 2,950–3,600 | 127 / 50 | 40,900 / 32,700 | aggressive | **Fallen Valor** (War Cry, below 50% HP); **Valkyrie's Fall** (leap 4–10 m, r3 200%, cast 0.9s); **Sunder Strike** (strike 230%, breaks weapons 3%) | Catacombs of Helheim B3 |
| **Snow Harpy** | 222 | Water · Demon · Medium | 309,000 | 3,880–4,750 | 152 / 60 | 54,800 / 43,800 | aggressive | **Blizzard Gale** (at target r4 170%, Water, Freeze 20%, cast 1.6s); **Dive** (leap 4–10 m, r1.5 180%, cast 0.5s); **Frost Feather** (spell 150%, 8 m, Water, cast 0.8s) | Jotun Steppe, Lyngvi, Isle of the Bound Wolf |
| **Abyssal Leech** | 226 | Poison · Fish · Small | 334,000 | 4,050–4,960 | 156 / 70 | 57,400 / 45,900 | aggressive | **Leech Bite** (strike 160%, drains 60%); **Ichor Spray** (at target r3 150%, Poison, Poison 50%, cast 1s) | Sunken Fjord Caverns 2, Sunken Fjord Caverns 3 |
| **Frozen Revenant** | 228 | Undead · Undead · Medium | 385,000 | 4,140–5,100 | 158 / 50 | 58,700 / 46,900 | aggressive | **Frozen Grasp** (at target r3 150%, Water, Root 50%, cast 1.2s); **Grave Frost** (spell 200%, 8 m, Water, Freeze 25%, cast 1.3s) | Catacombs of Helheim B4 |
| **Jotun Brawler** | 230 | Water · DemiHuman · Large | 400,000 | 4,200–5,200 | 160 / 60 | 60,000 / 50,000 | aggressive | **Giant's Rage** (Berserk, below 40% HP); **Earthshaker** (around itself r4 220%, knockback, cast 1.6s); **Bone Crusher** (strike 250%, Stun 20%, breaks weapons 3%) | Jotun Steppe |
| **Hel's Executioner** | 240 | Shadow · Undead · Large | 473,000 | 4,680–5,700 | 168 / 60 | 67,600 / 54,000 | aggressive | **Bloodlust** (Berserk, below 40% HP); **Hel's Chains** (shot 150%, 8 m, Root 60%, cast 0.8s); **Execution** (strike 300%, breaks weapons 3%, Mortal Stagger, cast 1.2s) | Catacombs of Helheim B4 |
| **Frost Wyrm** | 244 | Water · Dragon · Large | 605,000 | 4,860–5,900 | 171 / 90 | 70,700 / 56,600 | aggressive | **Glacial Hide** (Ice Armor, below 50% HP); **Wing Buffet** (around itself r4 160%, knockback, cast 0.8s); **Frost Breath** (at target r4.5 220%, Water, Frostbite 50%, cast 1.8s) | Jotun Steppe, Lyngvi, Isle of the Bound Wolf, Sunken Fjord Caverns 3 |

### Mini-bosses and MVPs

#### Elder Direwolf (mini-boss, Lv 115)

- **Lair:** Whispering Woods, back 2 h (± 10 min) after it dies.
- **Stats:** Earth · Beast · Large, 450,000 HP, ATK 1,340–1,630, DEF 80, MDEF 30. 224,000 / 180,000 EXP.
- **Phases:** at 50% HP: Enraged ("The Elder Direwolf's eyes burn red!")
- **Skills:** **Alpha Howl** (summon 3× Dire Wolf, cast 1.5s); **Pack Howl** (Pack Howl (pack)); **Savage Pounce** (leap 3–12 m, r2.5 220%, Stun 30%, cast 0.8s); **Rending Bite** (strike 200%, Bleeding 50%).
- **Drops:** Elder Wolf Mane 60%, Honey Mead 50%, Dead Branch 10%, Wolf Hood 30%, Wolfskin Mantle 20%, Fang Mask 20%, Swift Boots 3%, Valkyrie Winged Helm 2%, Raven Longbow 0.5%, Elder Direwolf Card 0.5%

#### Draugr Warlord (mini-boss, Lv 178)

- **Lair:** Howling Fjord, back 2 h (± 10 min) after it dies.
- **Stats:** Undead · Undead · Large, 1,350,000 HP, ATK 3,190–3,900, DEF 140, MDEF 60. 620,000 / 500,000 EXP.
- **Phases:** at 40% HP: Enraged ("The Warlord's crest blazes with grave-light!")
- **Skills:** **Raise the Fallen** (summon 3× Draugr Footman, cast 1.5s); **Shield Wall** (Shield Wall, below 50% HP); **Grave Roar** (around itself r7 100%, Shadow, Curse 40%, cast 1s); **Warlord's Cleave** (around itself r4 240%, knockback, cast 1.4s).
- **Drops:** Warlord's Seal 60%, Honey Mead 50%, Starmetal 30%, Mirror Shield 10%, Grand Horned Viking Crest 5%, Runic Full Plate 5%, Beard of Odin 5%, Einherjar Greatsword 2%, Valkyrian Shield 1%, Draugr Warlord Card 0.5%

#### Naga Queen (mini-boss, Lv 232)

- **Lair:** Sunken Fjord Caverns 2, back 2 h (± 10 min) after it dies.
- **Stats:** Water · Fish · Large, 2,500,000 HP, ATK 5,600–6,860, DEF 200, MDEF 120. 1,230,000 / 980,000 EXP.
- **Phases:** at 50% HP: Enraged ("The Naga Queen's song turns to a scream!")
- **Skills:** **Serpent's Blessing** (heal 8% (allies), cast 2s, below 50% HP); **Royal Guard** (summon 3× Naga Scout, cast 1.5s); **Tidal Wave** (around itself r6 220%, Water, knockback, cast 2s); **Siren Song** (at target r4 120%, Water, Sleep 40%, cast 1.5s); **Hydro Lance** (spell 260%, 10 m, Water, cast 1.2s).
- **Drops:** Naga Pearl 60%, Honey Mead 50%, Skystone 30%, Mirror Shield 10%, Archmage Wizard Hat 5%, Valkyrian Lance 3%, Norn Staff 3%, Demon Nether Wings 1%, Naga Queen Card 0.5%

#### Ancient Golem (mini-boss, Lv 250)

- **Lair:** Jotun Steppe, back 2 h (± 10 min) after it dies.
- **Stats:** Earth · Formless · Large, 3,300,000 HP, ATK 6,700–8,190, DEF 300, MDEF 80. 1,500,000 / 1,200,000 EXP.
- **Phases:** at 60% HP: Enraged ("Runes flare across the Ancient Golem's body!") at 25% HP: Unbound ("The Ancient Golem's core cracks open!")
- **Skills:** **Stone Skin** (Stone Skin, below 50% HP); **Rock Guardians** (summon 2× Ice Golem, cast 1.5s, phase 1+); **Seismic Slam** (around itself r5 260%, Stun 30%, cast 2s); **Boulder Toss** (at target r3.5 220%, cast 1.6s); **Crushing Fist** (strike 300%, breaks weapons 5%, cast 0.8s).
- **Drops:** Golem Core 60%, Starmetal 50%, Skystone 50%, Frost Crown 10%, Fist of Odin Knuckles 3%, Muspel Flamberge 3%, Valkyrian Armor 2%, Helm of Awe 1%, Ancient Golem Card 0.5%

#### Fenrir, the Bound Wolf (MVP, Lv 255)

- **Lair:** Lyngvi, Isle of the Bound Wolf, back 1 h (± 10 min) after it dies.
- **Stats:** Shadow · Beast · Large, 8,000,000 HP, ATK 7,200–8,800, DEF 260, MDEF 120. 1,800,000 / 1,500,000 EXP, MVP bonus 600,000 base EXP.
- **Phases:** at 70% HP: Enraged ("Gleipnir groans... the chains are slipping!") at 35% HP: Unbound ("GLEIPNIR SNAPS! Fenrir is unbound!")
- **Skills:** **Call Sköll** (summon 1× Sköll, Sun-Chaser, cast 1.5s, phase 1+); **Call Hati** (summon 1× Hati, Moon-Hunter, cast 1.5s, phase 1+); **Ragnarök Howl** (around itself r8 200%, Shadow, Stun 30%, cast 2s, phase 1+); **Lunar Step** (blinks behind you, phase 2+); **Moon-Eater Breath** (at target r5 240%, Shadow, cast 1.8s); **Gleipnir Lunge** (leap 3–14 m, r3 260%, knockback, cast 0.9s); **Devouring Bite** (strike 350%, Bleeding 50%, breaks weapons 2%).
- **MVP reward (first that hits):** Gleipnir Thread 50% → Valkyrian Manteau 30% → Megingjard 5%
- **Drops:** Starmetal 60%, Skystone 60%, Tiwaz Runestone 50%, Valkyrian Feather Wings 5%, Fimbul Frost Wings 5%, Valkyrian Boots 5%, Raven Longbow 5%, Shadow Katar 5%, Fenrir Card 0.01%

#### Hel's Vanguard (MVP, Lv 255)

- **Lair:** Catacombs of Helheim B4, back 1 h (± 10 min) after it dies.
- **Stats:** Ghost · Undead · Large, 7,000,000 HP, ATK 6,800–8,300, DEF 340, MDEF 160. 1,700,000 / 1,400,000 EXP, MVP bonus 550,000 base EXP.
- **Phases:** at 70% HP: Enraged ("The Vanguard raises Hel's banner. The dead answer!") at 35% HP: Unbound ("Niflheim's gate yawns open!")
- **Skills:** **Death's Embrace** (heal 5%, cast 2.5s, below 35% HP, phase 2+); **Raise the Dead** (summon 2× Frozen Revenant, cast 1.5s, phase 1+); **Spectral Blink** (blinks behind you, phase 1+); **Niflheim Gate** (around itself r7 260%, Shadow, Curse 40%, cast 2.2s); **Soul Chains** (shot 200%, 10 m, Root 60%, cast 0.8s); **Hel's Judgment** (strike 320%, breaks weapons 3%, cast 1s).
- **MVP reward (first that hits):** Hel's Soulfire 50% → Helm of Awe 10% → Brisingamen 5%
- **Drops:** Starmetal 60%, Skystone 60%, Valkyrian Armor 10%, Valkyrian Shield 10%, Demon Nether Wings 5%, Templar Mace 5%, Einherjar Greatsword 5%, Muspel Flamberge 5%, Hel's Vanguard Card 0.01%

#### Jormungandr's Brood (MVP, Lv 255)

- **Lair:** Sunken Fjord Caverns 3, back 1 h (± 10 min) after it dies.
- **Stats:** Water · Dragon · Large, 9,000,000 HP, ATK 7,000–8,600, DEF 300, MDEF 180. 1,900,000 / 1,600,000 EXP, MVP bonus 650,000 base EXP.
- **Phases:** at 70% HP: Enraged ("The waters boil as the Brood thrashes!") at 35% HP: Unbound ("The World Serpent's blood awakens!")
- **Skills:** **Shed Skin** (heal 6%, cast 2s, below 35% HP, phase 2+); **Abyssal Spawn** (summon 3× Abyssal Leech, cast 1.5s, phase 1+); **Submerge** (blinks away, below 70% HP, phase 1+); **Frost-Venom Storm** (around itself r9 240%, Water, Frostbite 70%, cast 2.6s, phase 2+); **Tidal Crush** (at target r5 280%, Water, knockback, cast 2s); **Venom Spit** (spell 220%, 11 m, Poison, Poison 60%, cast 1s); **Coil Constrict** (strike 300%, Root 50%).
- **MVP reward (first that hits):** World Serpent Scale 50% → Valkyrian Lance 10% → Mjolnir 5%
- **Drops:** Starmetal 60%, Skystone 60%, Dragon Flame Wings 5%, Fimbul Frost Wings 5%, Norn Staff 5%, Yggdrasil Staff 5%, Fist of Odin Knuckles 5%, Valkyrian Boots 5%, Jormungandr's Brood Card 0.01%

Summoned only: **Sköll, Sun-Chaser** (Lv 240, 600,000 HP, **Sun-Chaser Pounce** (leap 3–10 m, r2 200%, Fire, cast 0.6s)); **Hati, Moon-Hunter** (Lv 240, 600,000 HP, **Moon Bite** (strike 220%, Shadow, Blind 40%)).

### Monster drops

| Monster | Loot and supplies | Equipment | Card |
|---|---|---|---|
| Rune Spore | Lingonberry Tonic 40%, Spore Cap 55% | Sandals 1%, Flower Crown 0.3% | 1% |
| Field Beetle | Lingonberry Tonic 30%, Raven Feather 5%, Beetle Shell 55%, Bog Iron 8% | Cotton Tunic 1.5%, Slotted Dark Sunglasses 0.1% | 1% |
| Toxic Spore | Aether Sap Vial 10%, Toxic Gland 50%, Spore Cap 20% | Bandana 1%, Rune Monocle 0.2% | 1% |
| Forest Imp | Wind Rune Shard 15%, Tiwaz Runestone 2%, Imp Horn 45%, Dead Branch 3% | Clip Ring 0.5%, Fang Mask 0.3%, Raven Hood 0.3% | 0.75% |
| Horned Grazer | Honey Mead 8%, Grazer Hide 50%, Bog Iron 10% | Leather Jerkin 1%, Fur Cap 1%, Antler Crown 0.2%, Fur Boots 0.3% | 0.75% |
| Wood Sprite | Uruz Runestone 3%, Aether Sap Vial 12%, Sprite Leaf 45%, Dwarven Steel 4% | Runed Oak Wand 1%, Rune Tome Staff 0.3%, Rune Circlet 0.2%, Rune Muffler 0.3%, Archmage Wizard Hat 0.02% | 0.5% |
| Wild Boar | Boar Tusk 50%, Honey Mead 10%, Bog Iron 12%, Dwarven Steel 5% | Iron Mouthguard 0.3%, Wolfskin Mantle 0.2%, Fur Boots 0.4% | 0.75% |
| Dire Wolf | Tiwaz Runestone 5%, Honey Mead 20%, Wolf Pelt 50%, Dwarven Steel 8%, Skystone 2% | Wolf Hood 0.8%, Wolf Claws 0.5%, Wolfskin Mantle 0.4%, Fang Mask 0.2% | 0.5% |
| Forest Outlaw | Stolen Coin Pouch 40%, Honey Mead 10%, Tiwaz Runestone 3% | Yew Longbow 0.3%, Runed Blindfold 0.2%, Jamadhar 0.15%, Eyepatch 0.5% | 0.75% |
| Crypt Bat | Bat Wing 50%, Aether Sap Vial 15%, Raven Feather 5% | Slotted Dark Sunglasses 0.3%, Fang Mask 0.3% | 0.75% |
| Draugr Footman | Draugr Bone 50%, Honey Mead 10%, Dwarven Steel 6% | Iron Helm 0.4%, Round Viking Shield 0.4%, Chainmail 0.3%, Mirror Shield 0.08% | 0.75% |
| Fjord Harpy | Harpy Feather 50%, Honey Mead 12%, Skystone 2% | Rune Muffler 0.3%, Swift Boots 0.1% | 0.75% |
| Ghoul | Rotten Bandage 50%, Honey Mead 12%, Dwarven Steel 8% | Saint's Robe 0.3%, Holy Mace 0.05% | 0.75% |
| Cave Crawler | Crawler Carapace 50%, Honey Mead 12%, Starmetal 2% | Frost Goggles 0.2%, Iron Knuckles 0.2% | 0.75% |
| Frost Wolf | Sowilo Runestone 5%, Honey Mead 30%, Frost Fang 40%, Starmetal 3%, Skystone 4%, Glyph of Isa 1% | Frost Stiletto 0.4%, Frost Goggles 0.3%, Frost Crown 0.03% | 0.5% |
| Runic Berserker | Berserker Braid 50%, Honey Mead 15%, Uruz Runestone 4% | Flamberge 0.15%, Beard of Odin 0.08%, Grand Horned Viking Crest 0.03%, Runic Full Plate 0.03% | 0.5% |
| Crypt Wraith | Wraith Shroud 50%, Aether Sap Vial 20% | Runed Staff 0.3%, Archmage Wizard Hat 0.05%, Demon Nether Wings 0.01% | 0.5% |
| Naga Scout | Naga Scale 50%, Honey Mead 15%, Starmetal 3% | Rune Lance 0.2%, Mirror Shield 0.1%, Ward Amulet 0.3% | 0.5% |
| Sea Drake | Drake Scale 50%, Honey Mead 15%, Starmetal 3% | Rune Lance 0.2%, Dragon Flame Wings 0.01% | 0.5% |
| Banshee | Spectral Lace 50%, Aether Sap Vial 25% | Rune Circlet 0.4%, Archmage Wizard Hat 0.08%, Norn Staff 0.03% | 0.5% |
| Ice Golem | Ice Core 50%, Honey Mead 15%, Skystone 4%, Starmetal 4%, Isa Runestone 3% | Frost Crown 0.08%, Runic Full Plate 0.08%, Fimbul Frost Wings 0.02% | 0.5% |
| Corrupted Einherjar | Tarnished Valknut 50%, Honey Mead 15%, Skystone 4% | Einherjar Greatsword 0.04%, Valkyrie Winged Helm 0.08%, Holy Mace 0.15%, Templar Mace 0.02% | 0.5% |
| Snow Harpy | Snow Plume 50%, Honey Mead 20%, Skystone 5% | Swift Boots 0.15%, Raven Longbow 0.02%, Valkyrian Feather Wings 0.02% | 0.5% |
| Abyssal Leech | Leech Ichor 50%, Aether Sap Vial 25%, Skystone 5% | Shadow Katar 0.03%, Demon Nether Wings 0.02% | 0.5% |
| Frozen Revenant | Grave Frost 50%, Honey Mead 20%, Starmetal 5% | Frost Crown 0.08%, Templar Mace 0.03% | 0.5% |
| Jotun Brawler | Uruz Runestone 10%, Honey Mead 40%, Jotun Tooth 40%, Starmetal 6%, Skystone 6%, Glyph of Thurisaz 2%, Blood Branch 0.5% | Plated Greaves 0.5%, Fist of Odin Knuckles 0.02%, Muspel Flamberge 0.03% | 0.5% |
| Hel's Executioner | Hel's Brand 50%, Honey Mead 20%, Starmetal 6% | Muspel Flamberge 0.04%, Einherjar Greatsword 0.04%, Helm of Awe 0.01% | 0.5% |
| Frost Wyrm | Wyrm Heart 40%, Honey Mead 20%, Starmetal 6%, Skystone 6%, Isa Runestone 3% | Fimbul Frost Wings 0.03%, Yggdrasil Staff 0.03%, Dragon Flame Wings 0.02% | 0.5% |

## 10. Save data

- **New data on each character:**
  - `SaveMapId`: the map you revive on. It defaults to Vigrid Haven.
  - `Broken` on weapons (`ItemStack`).
- **Old saves:** they load unchanged. A character saved on `whisperwood_plains` stays there, and revives in Vigrid until it saves with a courier.
- **Boss timers** are not saved (see §6).

## 11. New GM commands

| Command | What it does |
|---|---|
| `@maps` | List every map id and its level range |
| `@warp <map> [portal]` | Travel to a map (its save point, or beside one of its portals); `@go` works too |
| `@where` | Map, coordinates and save map |
| `@bosses` | Every boss: up, or how long until it returns and who slew it |
| `@bossrespawn` | Clear every boss timer |
| `@bosstime <scale>` | Multiply respawn times (`0.01` = MVPs in about 36 s; `1` = GDD) |
| `@breakweapon` | Break your worn weapon (to test repairs) |

## 12. Tests

- **Core (no Unity needed):** `cd Tools/CoreTests && dotnet test Tests` runs **146 tests** (25 new).
  - **World:**
    - there are 14 maps, all in `RH_World`;
    - every portal leads to a partner portal that leads back;
    - every map can be reached from Vigrid;
    - spawns and bosses use real monsters at sensible levels;
    - every card monster lives on some map;
    - layouts are deterministic;
    - from each save point you can reach every portal, arrival, NPC, boss and spawn;
    - scenery never blocks a key point;
    - dungeon stairs and bosses are deep inside;
    - Vigrid's services and rules are in place;
    - old saves migrate.
  - **Bestiary:**
    - there are 35 card monsters, 4 mini-bosses and 3 MVPs, with one card each;
    - stats climb with level;
    - every skill, phase, summon, buff and MVP drop is valid;
    - every item can be bought or looted;
    - status immunities work;
    - phase thresholds, skill conditions and priority picks behave as described;
    - MVP selection and MVP drops work;
    - boss timers fall in their windows;
    - weapon break, repair and the Ancient Golem Card behave as described.
- **Editor tests:**
  - the wizard builds `RH_Login` and `RH_World`;
  - **every one of the 14 maps** builds and bakes, and a NavMesh path exists from its save point to every portal, NPC and boss.
- **PlayMode tests (`WorldSmokeTests`):**
  - **Vigrid:** the player lands on the NavMesh, every NPC and portal is placed, the minimap builds, branches are refused, and every NPC menu opens.
  - **Lyngvi:** Fenrir's phase 1 buff, Sköll's summon, the breath's telegraph and its stagger interrupt, the kill's MVP reward and tombstone, the summons vanishing, and Fenrir returning when his timer is cleared.
  - **The Hall of Branches:**
    - a Jotun Brawler's Earthshaker telegraph hurts you if you stay in it;
    - a broken weapon does nothing until it's repaired;
    - Dead Branches work.

## 13. Design choices to confirm

1. **MVP HP is tuned for a strong solo character or a small party:** Fenrir has 8M, Hel's Vanguard 7M and Jormungandr's Brood 9M. Phase 6 parties could double it.
2. **Boss timers reset on restart** (in memory) until the Phase 6 server keeps them.
3. **Tier-1 rebalance:** the plains monsters span Lv 3–48 so that the plains cover their GDD band (Lv 1–60).
4. **Lyngvi's guards** are Frost Wyrms and Snow Harpies (the Lv 240–255 band). The Frost Wolves first placed there were too weak.
5. **Weapon break** affects weapons only. Armor break could come later.
6. **Mini-boss cards drop at 0.5%** (the GDD's 0.5–1% band) and **MVP cards at 0.01%**, before the server card rate.

## What's next

**Phase 6 — Mirror Multiplayer Networking, Vending & Alpha Release:**
- a map server that owns monsters, boss timers and drops;
- parties with Even Share EXP;
- player street vending in Vigrid;
- PvP rules (Abyssal Leech's crit SP burn and the Demi-Human cards against players);
- the alpha build.
