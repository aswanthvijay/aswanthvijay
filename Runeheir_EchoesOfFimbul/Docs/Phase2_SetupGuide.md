# RUNEHEIR: Echoes of Fimbul — Phase 2 Setup Guide
### Core Mechanics & 2.5D Isometric Prototype (Unity 6 URP)

This guide takes you from an empty Unity project to a playable prototype loop:

**Login → Realm select → Character select / create → Whisperwood Plains → click-to-move, click-to-attack, F1–F10 skills & items, level up to Base 255 / Job 120.**

| GDD Phase 2 step | Where it lives |
|---|---|
| Step 1 — Unity URP project `Runeheir_EchoesOfFimbul` | §1 below |
| Step 2 — 2.5D isometric camera (Pitch −45°, Yaw 45°) | `Scripts/Runtime/Cameras/IsometricCameraRig.cs` |
| Step 3 — NavMesh click-to-move controller | `Scripts/Runtime/Player/ClickToMoveController.cs` + `Movement/NavMotor.cs` |
| Step 4 — Stat engine (STR..LUK, Base 255 / Job 120, ASPD, 150 DEX) | `Scripts/Core/Stats/*`, `Scripts/Core/Characters/*` |
| ASPD → animation speed | `Scripts/Runtime/Visuals/AspdAnimationScaler.cs` + `CharacterAnimationBridge.cs` |
| XileRO-style click-to-attack | `Scripts/Runtime/Combat/AutoAttacker.cs` |
| F1–F10 assignable skills / items | `Scripts/Runtime/Player/HotkeyController.cs`, `UI/Hud/HotkeyBarView.cs` |
| Login, realm & character select / create | `Scripts/Runtime/FrontEnd/*` |
| Step 5 — Cel-shaded toon material | *Not in this drop (see "What's next")* |

---

## 0. Requirements

| | |
|---|---|
| Unity | **Unity 6 LTS** (6000.0 or newer) via Unity Hub |
| Template | **Universal 3D** (URP) |
| Packages | **Input System** (included in Unity 6 templates), **Unity UI (uGUI)** (included), **Test Framework** (included). Optional: **AI Navigation** for editor-baked NavMeshes. |
| Hardware | The ASUS TUF F15 (i5-11400H / RTX 3060 6 GB) is plenty. The prototype uses primitives only. |

No other assets are needed. All UI, icons and placeholder characters are generated in code.

---

## 1. Create the project and add the code

### Option A — fastest (copy the code into a new project)
1. Unity Hub → **New project** → **Universal 3D** → name it `Runeheir_EchoesOfFimbul` → **Create**.
2. Close Unity. Copy the folder **`Runeheir_EchoesOfFimbul/Assets/_Runeheir`** from this repo into your new project's `Assets/` folder.
3. Re-open the project. Unity imports the scripts and compiles four assemblies:
   `Runeheir.Core` (pure C# rules), `Runeheir.Runtime`, `Runeheir.Editor`, `Runeheir.Tests.EditMode`.

### Option B — keep the whole Unity project in this repo (recommended for the team)
1. Create the URP project in a temporary folder exactly like Option A (step 1).
2. Close Unity and move the generated **`Assets/`** content (Scenes, Settings, etc.), **`Packages/`** and **`ProjectSettings/`** into this repo's `Runeheir_EchoesOfFimbul/` folder. Keep the existing `Assets/_Runeheir`.
3. Unity Hub → **Add** → **Add project from disk** → pick `Runeheir_EchoesOfFimbul/`.
4. Commit `Assets/`, `Packages/`, `ProjectSettings/` and the generated **`.meta`** files. The `.gitignore` in that folder already excludes `Library/`, `Temp/`, `Logs/`, `UserSettings/` and IDE files.

> **Input backend:** Unity 6 templates use the new Input System ("Active Input Handling = Input System Package"). The code supports that and the legacy Input Manager, chosen at compile time in `Controls/GameInput.cs`. Either setting works.

---

## 2. One-click prototype scenes

Menu bar → **Runeheir ▸ Setup ▸ Build Prototype Scenes**

This generates two scenes and adds them to Build Settings / Build Profiles:

| Scene | Contents |
|---|---|
| `Assets/_Runeheir/Scenes/RH_Login.unity` | Main Camera, Sun, `FrontEndController` (builds the login UI at runtime) |
| `Assets/_Runeheir/Scenes/RH_Field_WhisperwoodPlains.unity` | Isometric camera rig, 140×140 m meadow with paths, birch & pine trees, rocks, glowing runestones, two windmills, a campfire save point, `RuntimeNavMeshBaker`, `FieldBootstrap`, 6 monster spawners and 3 training dummies |

Then:
- **Full flow:** open `RH_Login` → **Play** → type an ID and password → **Register** (logs you in) → **Vigrid Haven** → pick an empty slot → **Create** → **Start**.
- **Quick test:** open `RH_Field_WhisperwoodPlains` → **Play**. You get a temporary character "Wanderer" that is not saved.

### Controls (XileRO-style)

| Input | Action |
|---|---|
| Left-click ground | Walk there (hold to keep steering toward the cursor) |
| Left-click a monster | Walk into range and auto-attack until it dies |
| F1–F10 | Use the skill or item in that hotkey slot · **F12** switches between 4 hotkey pages |
| Skill target cursor | Left-click a target or the ground · right-click / Esc cancels |
| Mouse wheel | Zoom · **right-drag** rotates the camera |
| **A** / **S** / **E** (or Alt+A/S/E) | Status / Skills / Items windows |
| Enter | Chat (lines starting with `@` are test commands) |
| Esc | Cancel target cursor → close top window → game menu (Character Select / Exit) |

---

## 3. Step 2 — 2.5D isometric camera rig

**Script:** `Runeheir.Cameras.IsometricCameraRig` (put it on the Main Camera).

| Inspector field | Default | Notes |
|---|---|---|
| Pitch | **−45** | GDD convention (negative = looking down). Internally `Quaternion.Euler(-pitch, yaw, 0)`, so −45 becomes Unity's +45° X tilt. |
| Yaw | **45** | Classic isometric diagonal. Right-drag rotates it at runtime. |
| Distance / Min / Max | 18 / 9 / 32 | Mouse-wheel zoom, smoothed. |
| Follow Smooth Time | 0.1 s | `SmoothDamp` follow. Set 0 for a locked camera. |
| Orthographic | off | Off = perspective with a 30° FOV (the HD-XileRO look). On = true orthographic isometric. |

Manual setup in your own scene:
1. Select **Main Camera** → **Add Component → Isometric Camera Rig**.
2. Drag your player into **Target** (or call `rig.SetTarget(player.transform)` from code; `FieldBootstrap` does this for you).
3. Right-click the component header → **Snap To Target** to preview the framing in edit mode.

`rig.Shake(strength, seconds)` is used by Fist of Odin's "screen-distorting" punch.

---

## 4. Step 3 — NavMesh click-to-move

**Scripts:**
- `Movement/NavMotor` wraps `NavMeshAgent`: it snaps clicks onto the NavMesh, turns instantly (no slow agent rotation), locks movement while casting or stunned, and handles knockback and warps.
- `Player/ClickToMoveController` handles mouse input: picking ground vs. monsters (with a 28 px "near miss" radius for small targets), hold-to-walk, and skill target cursors.

### NavMesh: two options
- **Prototype (zero setup):** `RuntimeNavMeshBaker` on the environment root bakes every collider under it when the scene loads. Objects with a `NavBlocker` component (tree trunks, rocks, runestones) are carved out instead of becoming walkable islands.
- **Hand-built maps (Phase 5):** Package Manager → install **AI Navigation** → add **NavMeshSurface** to your level root → **Bake**. Keep or remove the `RuntimeNavMeshBaker`; it does nothing when an editor-baked NavMesh already exists.

### Your own player model
`FieldBootstrap ▸ Player Visual Prefab` accepts any model (FBX from Blender). `EntityFactory.CreatePlayer` adds the rest: `NavMeshAgent`, `NavMotor`, `PlayerCharacter`, `AutoAttacker`, `SkillCaster`, `AspdAnimationScaler`, `ClickToMoveController`, `HotkeyController`. Leave it empty to use the procedural placeholder avatar (job-colored outfit, 8 hair styles, 9 hair colors, weapon by job).

---

## 5. Step 4 — Stat engine (Base 255 / Job 120)

Everything numeric is in **`Scripts/Core`**, a pure C# assembly with no UnityEngine reference (`noEngineReferences: true`). The same rules can run on a Mirror server in Phase 6, and they are unit-tested.

### GDD §4 formulas → code (`StatFormulas.cs`)

| Stat | Formula (GDD) | Function |
|---|---|---|
| STR | ATK bonus = STR + (STR/10)² · weight 2000 + STR×30 | `StatusAtk`, `WeightCapacity` |
| AGI | ASPD 150–197 → anim play rate 1.0x–3.0x · FLEE = BaseLv + AGI | `Aspd`, `AttackPlayRate`, `AttackInterval`, `Flee` |
| VIT | Max HP = BaseLv×120 + VIT×250 | `MaxHp` |
| INT | Max SP = BaseLv×25 + INT×50 · MATK INT+(INT/7)² ~ INT+(INT/5)² | `MaxSp`, `MatkMin/Max` |
| DEX | Cast multiplier = 1 − DEX/150 → **150 DEX = instant cast** · HIT = BaseLv + DEX | `CastTimeMultiplier`, `Hit` |
| LUK | Crit % = LUK×0.35 + 1 · crits deal 140% and ignore DEF | `CritChance`, `CriticalDamageMultiplier` |

Caps: Base 255, Job 120 (Ascended tier), each stat 1–255. Everyone starts at 1 in every stat with **48 status points**. Each level grants `3 + floor((L−1)/5)` points and raising a stat costs `floor((x−1)/10) + 2` (the classic curve; it gives exactly 1,273 points at Base 99, like RO). At Base 255 you have **7,185 points**: one stat at 255 (3,608) plus another at 200 (2,279) with change to spare, but never two maxed stats.

### ASPD → animation speed (how it's computed)

```
ASPD        = clamp(weaponBase + sqrt(AGI×9.9987 + DEX×0.1922) + flat bonuses, 150, 197)
              (Rage of Thor / Mjolnir override it to 195 / 197)
t           = (ASPD − 150) / 47                      // 0 at 150, 1 at 197
PlayRate    = 1 + 2t                                 // GDD: 1.0x → 3.0x
Swing       = 0.6 s / PlayRate                       // the animation itself
Recovery    = 0.4 s × (1 − t)   (0 with Two-Hand Surge "recovery cancel")
Interval    = Swing + Recovery                       // time between hits
```

| ASPD | Anim | Interval | Hits/s |
|---|---|---|---|
| 150 | 1.00x | 1.000 s | 1.00 |
| 160 | 1.43x | 0.736 s | 1.36 |
| 170 | 1.85x | 0.554 s | 1.81 |
| 180 | 2.28x | 0.408 s | 2.45 |
| 190 | 2.70x | 0.282 s | 3.55 |
| 195 | 2.91x | 0.223 s | 4.49 |
| 197 | 3.00x | 0.200 s | 5.00 |

With a two-handed sword (base 150) and 50 DEX: AGI 90 gives ASPD 180, AGI 150 gives 189, AGI 230 reaches the 197 cap. Two-Hand Surge (+7) reaches the cap at AGI ~180.
Menu **Runeheir ▸ Debug ▸ Log ASPD Table** prints this table; the `@aspd` chat command prints your live breakdown.

### Wiring ASPD to a real Animator (when Blender models arrive)
`CharacterAnimationBridge` drives your Animator if it finds one, and the placeholder otherwise.
1. Animator parameters: `Speed` (float), `AttackSpeed` (float), `Attack` (trigger), `Casting` (bool), `Dead` (bool), `Hit` (trigger).
2. Select the **Attack** state → **Speed = 1**, **Multiplier → ☑ Parameter → `AttackSpeed`**.
3. `AspdAnimationScaler` writes ASPD's play rate (1.0–3.0) into `AttackSpeed` whenever stats change, so only the attack clip speeds up, not walking or idle.
4. Leave **Restart Attack State Each Swing** on, so very high ASPD restarts the clip instead of queueing triggers. Author attack clips at 0.6 s with the hit on the 50% frame to match `StatFormulas.ImpactFrameFraction`.

The inspector shows a live readout (ASPD, play rate, swing, interval, hits/s) while playing.

### EXP curve (`ExperienceTable.cs`) and rates
Base EXP to next = `20 × L^2.65` (Lv 99→100 ≈ 3.9 M, Lv 254→255 ≈ 47 M, 3.3 B total). Job EXP = `15 × tierMultiplier × L^2.5`.
Server rates (XileRO-style high rate) are on **FieldBootstrap**: Base ×50, Job ×50, Drop ×5 by default.

---

## 6. XileRO click-to-attack

`AutoAttacker` (shared by players and monsters) runs this loop:

1. **Chase:** re-path every 0.2 s until the target is inside weapon reach (edge to edge: dagger 0.9 m, two-hand sword 1.3 m, spear 2.2 m, bow 9 m).
2. **Swing:** stop, face the target, play the attack at the current ASPD play rate.
3. **Impact:** damage lands at 50% of the swing via `DamageCalculator` (HIT vs FLEE, crit, size table, 10-element table, hard/soft DEF, card bonuses).
4. **Repeat** every `AttackInterval` until the target dies or you click the ground (continuous by default, like `/noctrl`).

Monsters (`Combat/Monster.cs`) wander, aggro (aggressive types) or retaliate (passive types), leash home, and on death give EXP split by damage share and auto-loot their drops. The three **Training Dummies** next to spawn never die and post your DPS and **hits/s** to chat, which is the quickest way to verify ASPD.

Floating numbers: white = your damage, red = damage you take, yellow `1,234!` = critical, `Miss`, green `+heal`.

---

## 7. F1–F10 skills and items

- Open **Skills (S)** or **Items (E)** and **drag an icon onto any F1–F10 slot**. Drag between slots to swap; drag off the bar or right-click to clear; click a slot to use it. **Bind** puts a skill in the first free slot.
- Every slot can hold a skill **or** an item. **F12** cycles 4 pages (40 shortcuts). Keys are remappable on `HotkeyController ▸ Slot Keys`.
- Slots show item counts, cooldown sweeps and after-cast delay. The layout is saved per character.

Skill flow (`SkillCaster`): key → target cursor (or **quick-cast** if you're already hovering a valid target) → walk into range → cast bar (`castTime × (1 − DEX/150)`) → SP spent → effect → after-cast delay + cooldown. Taking damage interrupts casting unless you are uninterruptible (Rage of Thor).

Prototype skills (`Core/Skills/SkillCatalog.cs`, inherited down the job tree):

| Job | Skills |
|---|---|
| Initiate | First Aid |
| Warrior → … → **Einherjar** | Bash · **Vortex Cleave** (360° AoE + knockback) · **Two-Hand Surge** (+7 ASPD, recovery cancel) · **Rage of Thor** (HP ×3, ASPD 195, items locked) |
| Scout → … → **Shadow Walker** | Twin Fang · **Phantom Barrage** (8 hits + stun) · **Miasma Weapon** (damage ×4, 40 s) |
| Mystic → … → **Archmage** | Muspel Bolt · **Glacial Tempest** (5-wave blizzard, freeze; frozen targets take ×3 blunt damage) · **Runic Aegis** (blocks 10 melee hits) |
| Devotee → … → **Champion** | Eir's Blessing (heal) · **Fist of Odin** (drains all SP) · **Aether Snap** (dash) |

Items: Lingonberry Tonic, Honey Mead, Aether Sap Vial, **Uruz / Tiwaz / Sowilo runestones** (GDD §7), Raven Feather (return to save point), Wind Rune Shard (random teleport).

**Job change:** reach Job Lv 10 as an Initiate (40 / 70 for later tiers) → **Skills (S) → Job Change**.

**Adding a skill:** add a `SkillDefinition` to `SkillCatalog` (pick a `SkillTarget` and `SkillEffect`) and give it a `Job`. It shows up in that job's skill window and can be dragged onto hotkeys. New effect types go in `Player/SkillEffects.cs`.

---

## 8. Login, realm select and character select

| Screen | XileRO-style behaviour |
|---|---|
| Login | ID + password, **Save ID**, Register, Exit. Enter = login, Tab = switch field. |
| Realm | **Vigrid Haven** (offline, this PC) and **Asgard** (greyed out until Mirror in Phase 6). |
| Character select | 9 slots (3×3); stat sheet (Base/Job Lv, EXP %, map, STR..LUK); rotating 3D preview; Start / Create / Delete / Logout. Arrows move, Enter starts, Del deletes (you must type the character name). |
| Character create | Name (4–23 letters/numbers, unique), gender, 8 hair styles, 9 hair colors, live preview. You start as an Initiate in Whisperwood Plains. |

**Storage:** `LocalAccountService` keeps accounts in `Application.persistentDataPath/runeheir_local_accounts.json`. Passwords are **PBKDF2-SHA256 hashed with a per-account salt**, never stored as plain text. Characters autosave every 60 s, on level-up, on job change, on returning to character select and on quit.
Menu **Runeheir ▸ Debug ▸ Reveal / Delete Local Account Database** to inspect or reset it.

**Going online (Phase 6):** the screens only talk to `IAccountService`. Implement it over Mirror or HTTP, assign `GameSession.Instance.Accounts = new YourService()` before the login screen opens, and the UI stays the same. `AccountStore` (in Core) holds the login/char-server rules and can run on the server unchanged.

---

## 9. Testing

**In-game test commands** (editor and development builds only). Press Enter and type:

| Command | Effect |
|---|---|
| `@help` | List commands |
| `@blvl 255` / `@jlvl 120` | Set Base / Job level (status points recalculated) |
| `@job einherjar` · `@jobs` | Change job (any name: `shadow walker`, `archmage`, …) |
| `@allstats 150` · `@agi 230` · `@dex 150` | Set stats |
| `@reset` | Stat reset (refunds all points) |
| `@aspd` | Print the ASPD → animation breakdown |
| `@item honey_mead 50` · `@items` | Spawn items |
| `@monster dire_wolf 3` · `@monsters` | Spawn monsters next to you |
| `@heal` · `@save` · `@where` | Utilities |

Suggested ASPD check: `@job einherjar` → `@blvl 255` → `@agi 150` → hit a Training Dummy and read the hits/s in chat → cast **Two-Hand Surge** → hit again (higher) → **Rage of Thor** (locked at 195).

**Unit tests:** Window → General → **Test Runner** → **EditMode** → Run All. There are 56 tests covering formulas, ASPD, stat points, EXP and level caps, job changes, damage, buffs, hotkeys, inventory and accounts.

---

## 10. Troubleshooting

| Symptom | Fix |
|---|---|
| `Scene 'RH_…' is not in Build Settings` | Run **Runeheir ▸ Setup ▸ Build Prototype Scenes**, or add both scenes in File ▸ Build Profiles. |
| Clicks do nothing / no NavMesh error in Console | The field needs `RuntimeNavMeshBaker` on the environment root, or a baked NavMeshSurface. |
| `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class…` | A scene contains an old `StandaloneInputModule`. Delete that EventSystem; the game creates the correct one. |
| Assembly error mentioning `Unity.InputSystem` | Install **Input System** via Package Manager (it is preinstalled in Unity 6 templates). |
| Pink objects | The project isn't using URP. Create it from the **Universal 3D** template, or assign a URP asset in Graphics settings. |
| UI text missing | Unity versions before 2022.2 use `Arial.ttf`; `UITheme.Font` falls back to an OS font automatically. |
| Alt+key opens an editor menu | Use the plain keys **A / S / E** or the Basic Info buttons. |

---

## Design assumptions to confirm
These numbers weren't fixed by the GDD, so I picked Ragnarok-style defaults. Each one is a single constant you can change:
- **Job level caps per tier:** Initiate 10, 1st 50, 2nd 70, Ascended 120 (`JobDatabase.MaxJobLevelByTier`).
- **ASPD:** stats-based renewal-style curve, swing + recovery split 0.6 / 0.4 s (`StatFormulas`).
- **"Frozen foes take 300% bonus blunt damage"** is read as ×3 total (`FrozenBluntDamageMultiplier`; set it to 4 for +300%).
- **EXP curve and server rates** (`ExperienceTable`, `FieldBootstrap`).
- **Status point curve** (classic RO).

## What's next
- **Phase 2 Step 5:** cel-shaded toon material with ink outlines (URP Shader Graph + inverted-hull or Renderer Feature outline). Swap the shader in `RuntimeMaterials` / the generated materials.
- **Phase 3:** skill trees with skill points, real animations through `CharacterAnimationBridge`, more skills as ScriptableObjects.
- **Phase 4:** the 10-slot equipment paperdoll, +10/+20 refining and 4-socket soul cards (`DamageBonuses` and `StatModifiers` are ready to receive card effects).
- **Phase 6:** Mirror server authority (move `DamageCalculator` / `AccountStore` calls server-side) and a networked `IAccountService`.
