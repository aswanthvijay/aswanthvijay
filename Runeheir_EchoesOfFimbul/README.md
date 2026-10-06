# RUNEHEIR: Echoes of Fimbul

2.5D isometric Norse action MMORPG (Unity 6.3 LTS · URP 17.3 · C# · Mirror later), inspired by the high-rate XileRO / Ragnarok Online loop.

[![Unity CI](https://github.com/aswanthvijay/aswanthvijay/actions/workflows/unity-ci.yml/badge.svg?branch=claude/runeheir-core-mechanics-piezc9)](https://github.com/aswanthvijay/aswanthvijay/actions/workflows/unity-ci.yml)

This folder is the Unity project. Guides: **[Phase 2 — Core Mechanics & Isometric Prototype](Docs/Phase2_SetupGuide.md)** (setup, controls, stat engine) and **[Phase 3 — Combat, Skills & Animation](Docs/Phase3_Guide.md)** (skill trees, all skills, statuses, poise, animator).

## What's playable
- **Login → Realm → Character Select (9 slots, 3D preview) → Character Create** (offline accounts, PBKDF2-hashed passwords)
- **2.5D isometric camera:** Pitch −45°, Yaw 45°, smooth follow, wheel zoom, right-drag rotate
- **NavMesh click-to-move**, hold-to-walk, and **click-to-attack** auto-attack loop
- **Stat engine:** STR..LUK up to 255, **Base Lv 255 / Job Lv 120**, GDD §4 formulas, **150 DEX instant cast**, **ASPD 150–197 → attack animation 1.0x–3.0x**
- **F1–F10 hotkeys** (4 pages via F12) that hold any skill or item, set by drag and drop
- **Cel-shaded toon shader** (`Runeheir/Toon`, GDD Step 5): hard light/shadow band, cool shadow tint, anime specular, rim light, constant-width ink outlines
- **Skill trees for all 21 jobs:** 105 skills learned with skill points (Alt+S), including every GDD signature skill (Vortex Cleave, Two-Hand Surge, Rage of Thor, Phantom Barrage, Miasma Weapon, Shadow Veil, Glacial Tempest, Runic Aegis, Fist of Odin, Aether Snap)
- **Combat layer:** 12 status effects with stat resistance, poise and stagger, projectiles, ground zones and traps, stealth, procs
- **Animation layer:** one-click Animator Controller for real models (ASPD-scaled attack, skill motions, stagger), plus a settings asset for your model and rates
- Combat runestones, job change, Whisperwood Plains monsters, training dummies with a DPS meter, RO-style `@commands`

## Quick start
1. Unity Hub → **Add project from disk** → this `Runeheir_EchoesOfFimbul/` folder → open with Unity **6000.3.x** (6.3 LTS).
2. Menu **Runeheir ▸ Setup ▸ Build Prototype Scenes** (opens `RH_Login`) → **Play**.
3. Register an ID → **Vigrid Haven** → Create a character → **Start**.

## Layout
```
Assets/_Runeheir/
  Scripts/Core/      Pure C# rules (no UnityEngine): stats, ASPD, EXP, jobs, damage, skills, items, hotkeys, accounts
  Scripts/Runtime/   Unity layer: camera rig, NavMesh movement, combat, skills, HUD, login/char select
  Scripts/Editor/    "Runeheir" menu: scene generator, animator builder, settings asset, player builds, debug tools
  Resources/         RuneheirToon.shader (URP cel shader + ink outline)
  Tests/EditMode/    93 Core rule tests
  Tests/Editor/      Scene generator, shader import and animator builder tests
  Tests/PlayMode/    Smoke tests: walk, fight, cast, hotkeys, death/respawn, login screens
Assets/Settings/     URP assets (from the Unity 6.3 Universal 3D template)
Packages/, ProjectSettings/
Tools/CoreTests/     Run the Core tests without Unity: dotnet test Tools/CoreTests/Tests
Docs/Phase2_SetupGuide.md, Docs/Phase3_Guide.md
```

## CI
`.github/workflows/unity-ci.yml` runs the Core tests on every push. Add the `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` repository secrets to also run every Unity test in real Unity 6.3 and build Windows and Linux players ([guide §11](Docs/Phase2_SetupGuide.md#11-builds-and-ci)).
