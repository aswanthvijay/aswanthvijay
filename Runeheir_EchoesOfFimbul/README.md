# RUNEHEIR: Echoes of Fimbul

2.5D isometric Norse action MMORPG (Unity 6.3 LTS · URP 17.3 · C# · Mirror), inspired by the high-rate XileRO / Ragnarok Online loop.

[![Unity CI](https://github.com/aswanthvijay/aswanthvijay/actions/workflows/unity-ci.yml/badge.svg?branch=claude/runeheir-core-mechanics-piezc9)](https://github.com/aswanthvijay/aswanthvijay/actions/workflows/unity-ci.yml)

This folder is the Unity project. Guides: **[Phase 2 — Core Mechanics & Isometric Prototype](Docs/Phase2_SetupGuide.md)** (setup, controls, stat engine) **[Phase 3 — Combat, Skills & Animation](Docs/Phase3_Guide.md)** (skill trees, all skills, statuses, poise, animator), **[Phase 4 — Loot, Inventory & Card Compounding](Docs/Phase4_Guide.md)** (gear, cards, refining, runewords, shops, storage, every item), **[Phase 5 — World Maps & Monster AI](Docs/Phase5_Guide.md)** (the 14 maps, travel, the bestiary, monster skills, bosses and MVPs) **[Phase 6 — Multiplayer, Vending & the Alpha](Docs/Phase6_Guide.md)** (hosting, joining, dedicated servers, chat, parties, guilds, trades, street stalls), **[Phase 7 — the Full Roster & Rebirth](Docs/Phase7_Classes.md)** (all 40 jobs and 282 skills, rebirth, the expanded jobs, crafting) and **[Art Pass 1 — Characters, Weapons & Actions](Docs/Art_Characters.md)** (the rigged anime body, the Warrior-line outfits, hair, weapon models, Ragnarok-style actions, the Blender pipeline).

## What's playable
- **Realm screen → Login → Character Select (9 slots, 3D preview) → Character Create** (offline accounts, PBKDF2-hashed passwords)
- **2.5D isometric camera:** Pitch −45°, Yaw 45°, smooth follow, wheel zoom, right-drag rotate
- **NavMesh click-to-move**, hold-to-walk, and **click-to-attack** auto-attack loop
- **Stat engine:** STR..LUK up to 255, **Base Lv 255 / Job Lv 120**, GDD §4 formulas, **150 DEX instant cast**, **ASPD 150–197 → attack animation 1.0x–3.0x**
- **F1–F10 hotkeys** (4 pages via F12) that hold any skill or item, set by drag and drop
- **Cel-shaded toon shader** (`Runeheir/Toon`, GDD Step 5): hard light/shadow band, cool shadow tint, anime specular, rim light, constant-width ink outlines
- **Characters (Art Pass 1):** a rigged, anime-proportioned body built in Blender for every human, with eight anime hair styles. The **Warrior line wears its own outfits** (Warrior, Berserker, Guardian, Einherjar, Valkyrie, male and female); other jobs wear the common clothes in their job color for now. **A model for every weapon type**, held in hand (bows in the left), worn cloaks fitted to each outfit
- **Actions after Ragnarok's:** a relaxed stance and a battle stance, an attack combo per weapon class (slashes, two-handed cuts, spear thrusts, staff strikes, a drawn and loosed bow, punches, Glíma kicks, katar stabs), casting poses with a rune circle, slash trails, flinch, stagger and death, all timed by ASPD
- **The full Ragnarok roster (Phase 7):** 40 jobs under Norse names, from Initiate (Novice) through six first jobs, 13 second jobs and 13 transcendent jobs, plus the expanded jobs (Wanderer, Glíma Fighter, Sól Guardian, Fylgja Caller, Thunderer, Nightraider and **Freyja's Kin**, the cat-folk chosen at creation). **282 skills** learned with skill points (Alt+S), including every GDD signature skill
- **Rebirth (Phase 7):** a second job at Base 99 / Job 50 visits **Urðr at Urðr's Well** (1,285,000 zeny) and starts again as a High Initiate with 100 status points and +25% HP/SP, then takes the High first job and the matching transcendent job (Berserker → Einherjar, Guardian → Valkyrie...), Base 255
- **Job mechanics (Phase 7):** songs and dances that follow the performer, Return from Hel, Thor's Coins, traps, strips, Cut Purse, zeny and cart skills, **Loki's Mimicry** skill copying, **Haggle / Silver Tongue** shop prices, and **Rune Forging and Brewing** with a crafting window
- **Combat layer:** 12 status effects with stat resistance, poise and stagger, projectiles, ground zones and traps, stealth, procs
- **Animation layer:** one-click Animator Controller for real models (ASPD-scaled attack, skill motions, stagger), plus a settings asset for your model and rates
- **Loot and gear (Phase 4):** 87 base items on a 10-slot paperdoll (Q), 35 Soul Cards compounded into 1–4 sockets, refining to +20 with safe limits, Rune of Preservation and shattering, Runic Fuller runewords, card extraction, worn gear drawn on the model (headgear, shields, capes, animated wings)
- **The world (Phase 5):** 14 generated maps in one scene: Vigrid Haven, four fields from Lv 1 to 255, the Catacombs of Helheim (B1–B4), the Sunken Fjord Caverns (1–3), the Hall of Branches and Lyngvi. Warp portals, a minimap (Ctrl+Tab), save points, cross-map revival and paid courier teleports
- **Monster AI (Phase 5):** 35 card monsters with skills (strikes, bolts, telegraphed areas, leaps, buffs, heals, summons, blinks), cast bars, stagger interrupts, pack assist; 4 mini-bosses and 3 MVPs with phases, MVP rewards, tombstones and respawn timers; weapon break and repair
- **Multiplayer (Phase 6, Mirror):** Play Offline, **Host** a realm on your PC or **Join** one by address, or run a headless **realm server**. Everyone shares the 14 maps and fights the same realm-run monsters and bosses
- **Social (Phase 6):** map / party (`%`) / guild (`$`) / shout / whisper chat; **parties** of 12 with **Even Share** EXP within a 30-level gap (Z); **guilds** with ranks and a notice (G); **player trades**; the **Merchant Pushcart** (+8,000 weight) and **street stalls** in Vigrid Haven (V)
- **Town services:** Ásta's Trading Post, Hrafn's Armory, Brokk's Dwarven Forge (refine, carve, extract, repair), Norn Couriers (storage, save point, teleport), Sigrun's job change, Urðr's rebirth, the Branch Warden
- Combat runestones, job change (with a job weapon gift), loot tables and cards for every monster, Dead/Blood Branches, training dummies with a DPS meter, RO-style `@commands` (`@warp`, `@bosses`...)

## Quick start
1. Unity Hub → **Add project from disk** → this `Runeheir_EchoesOfFimbul/` folder → open with Unity **6000.3.x** (6.3 LTS).
2. Menu **Runeheir ▸ Setup ▸ Build Prototype Scenes** (opens `RH_Login`) → **Play**.
3. Register an ID → **Vigrid Haven** → Create a character → **Start**. You begin in Vigrid Haven; the south gate leads to the Whisperwood Plains.

## Layout
```
Assets/_Runeheir/
  Scripts/Core/      Pure C# rules (no UnityEngine): stats, ASPD, EXP, jobs, damage, skills, items, equipment, cards, refining, trade, maps and layouts, bestiary, monster skills, boss timers, hotkeys, accounts, parties, guilds, trades, vending, chat, realm settings
  Scripts/Net/       Mirror networking (Runeheir.Net): realm server and client, host/join/dedicated launcher, networked players and monsters, chat, parties, guilds, trades, stalls
  Scripts/Runtime/   Unity layer: world builder, portals, camera rig, NavMesh movement, combat and monster AI, skills, NPCs, HUD (inventory, equipment, shop, forge, storage, minimap), login/char select
  Scripts/Editor/    "Runeheir" menu: scene generator, animator builder, settings asset, player builds, debug tools
  Resources/         RuneheirToon.shader (URP cel shader + ink outline); Characters/ (Blender-built bodies, outfits, hair, palettes)
  Tests/EditMode/    166 Core rule tests
  Tests/Editor/      Scene generator, every map's NavMesh, shader import and animator builder tests
  Tests/PlayMode/    Smoke tests: walk, fight, cast, hotkeys, death/respawn, gear, cards, NPCs, branches, storage, login screens, generated maps, boss fights, a hosted realm (login, monsters, chat, party, stall)
Assets/Mirror/      Mirror networking (MIT), vendored
Assets/Settings/     URP assets (from the Unity 6.3 Universal 3D template)
Packages/, ProjectSettings/
Tools/CoreTests/     Run the Core tests without Unity: dotnet test Tools/CoreTests/Tests
Tools/Blender/       The character builder (Blender 5.2): skeleton, body, hair, outfits → FBX in Resources/Characters
Docs/Phase2_SetupGuide.md … Docs/Phase7_Classes.md, Docs/Art_Characters.md
```

## CI
`.github/workflows/unity-ci.yml` runs the Core tests on every push. Add the `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` repository secrets to also run every Unity test in real Unity 6.3 and build Windows and Linux players ([guide §11](Docs/Phase2_SetupGuide.md#11-builds-and-ci)).
