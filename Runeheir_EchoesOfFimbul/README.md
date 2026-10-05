# RUNEHEIR: Echoes of Fimbul

2.5D isometric Norse action MMORPG (Unity 6 URP · C# · Mirror later), inspired by the high-rate XileRO / Ragnarok Online loop.

This folder holds the **Phase 2 — Core Mechanics & Isometric Prototype** code. Step-by-step setup: **[Docs/Phase2_SetupGuide.md](Docs/Phase2_SetupGuide.md)**.

## What's playable
- **Login → Realm → Character Select (9 slots, 3D preview) → Character Create** (offline accounts, PBKDF2-hashed passwords)
- **2.5D isometric camera:** Pitch −45°, Yaw 45°, smooth follow, wheel zoom, right-drag rotate
- **NavMesh click-to-move**, hold-to-walk, and **click-to-attack** auto-attack loop
- **Stat engine:** STR..LUK up to 255, **Base Lv 255 / Job Lv 120**, GDD §4 formulas, **150 DEX instant cast**, **ASPD 150–197 → attack animation 1.0x–3.0x**
- **F1–F10 hotkeys** (4 pages via F12) that hold any skill or item, set by drag and drop
- GDD signature skills (Vortex Cleave, Two-Hand Surge, Rage of Thor, Phantom Barrage, Miasma Weapon, Glacial Tempest, Runic Aegis, Fist of Odin, Aether Snap), combat runestones, job change, Whisperwood Plains monsters, training dummies with a DPS meter, RO-style `@commands`

## Quick start
1. Unity Hub → New **Universal 3D** project (Unity 6 LTS).
2. Copy `Assets/_Runeheir` into the project's `Assets/`.
3. Menu **Runeheir ▸ Setup ▸ Build Prototype Scenes** → open `RH_Login` → **Play**.

## Layout
```
Assets/_Runeheir/
  Scripts/Core/      Pure C# rules (no UnityEngine): stats, ASPD, EXP, jobs, damage, skills, items, hotkeys, accounts
  Scripts/Runtime/   Unity layer: camera rig, NavMesh movement, combat, skills, HUD, login/char select
  Scripts/Editor/    "Runeheir" menu: scene generator + debug tools
  Tests/EditMode/    NUnit tests for Core (Unity Test Runner)
Docs/Phase2_SetupGuide.md
```
