# RUNEHEIR: Echoes of Fimbul — Phase 2 Setup Guide
### Core Mechanics & 2.5D Isometric Prototype (Unity 6.3 LTS · URP 17.3)

This guide takes you from a fresh clone to a playable prototype loop:

**Login → Realm select → Character select / create → Whisperwood Plains → click-to-move, click-to-attack, F1–F10 skills & items, level up to Base 255 / Job 120.**

| GDD Phase 2 step | Where it lives |
|---|---|
| Step 1 — Unity URP project `Runeheir_EchoesOfFimbul` | This folder is the project (§1) |
| Step 2 — 2.5D isometric camera (Pitch −45°, Yaw 45°) | `Scripts/Runtime/Cameras/IsometricCameraRig.cs` |
| Step 3 — NavMesh click-to-move controller | `Scripts/Runtime/Player/ClickToMoveController.cs` + `Movement/NavMotor.cs` |
| Step 4 — Stat engine (STR..LUK, Base 255 / Job 120, ASPD, 150 DEX) | `Scripts/Core/Stats/*`, `Scripts/Core/Characters/*` |
| ASPD → animation speed | `Scripts/Runtime/Visuals/AspdAnimationScaler.cs` + `CharacterAnimationBridge.cs` |
| XileRO-style click-to-attack | `Scripts/Runtime/Combat/AutoAttacker.cs` |
| F1–F10 assignable skills / items | `Scripts/Runtime/Player/HotkeyController.cs`, `UI/Hud/HotkeyBarView.cs` |
| Login, realm & character select / create | `Scripts/Runtime/FrontEnd/*` |
| Step 5 — Cel-shaded toon material with ink outlines | `Resources/RuneheirToon.shader` + `Visuals/RuntimeMaterials.cs` (§10) |

---

## 0. Requirements

| | |
|---|---|
| Unity | **Unity 6.3 LTS**. The project pins `6000.3.25f1` (`ProjectSettings/ProjectVersion.txt`); any 6000.3.x patch opens it. |
| Render pipeline | **URP 17.3**, already configured (PC and Mobile quality assets from the official Universal 3D template) |
| Packages | Pinned in `Packages/manifest.json` at Unity 6.3's own versions: Input System 1.20.0, Unity UI 2.0.0, AI Navigation 2.0.14, Test Framework 1.6.0, Timeline 1.8.13, URP 17.3.0. Unity installs them on first open. |
| Hardware | The ASUS TUF F15 (i5-11400H / RTX 3060 6 GB) is plenty. The prototype uses primitives only. |

No other assets are needed. All UI, icons and placeholder characters are generated in code.

---

## 1. Open the project

`Runeheir_EchoesOfFimbul/` is a complete Unity project (`Assets/`, `Packages/`, `ProjectSettings/`), set up from the official Unity 6.3 **Universal 3D** template.

1. Clone the repo (branch `claude/runeheir-core-mechanics-piezc9`).
2. Unity Hub → **Add** → **Add project from disk** → pick the `Runeheir_EchoesOfFimbul/` folder → open it with Unity **6000.3.x**.
3. The first import takes a few minutes. Unity resolves the packages and compiles six assemblies:
   `Runeheir.Core` (pure C# rules), `Runeheir.Runtime`, `Runeheir.Editor`, and the tests `Runeheir.Tests.EditMode`, `Runeheir.Tests.Editor`, `Runeheir.Tests.PlayMode`.
4. Commit the `.meta` files Unity generates for the scripts. The `.gitignore` in that folder already excludes `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/` and IDE files.

**Adding the code to another project instead:** copy `Assets/_Runeheir` into any Unity 6.3 **Universal 3D** project. The toon shader needs URP 17.1 or newer (Unity 6.1+).

> **Input backend:** the project uses the new Input System (Player Settings ▸ Active Input Handling = **Input System Package**), as Unity 6 templates do. The code also supports the legacy Input Manager, chosen at compile time in `Controls/GameInput.cs`.

---

## 2. One-click prototype scenes

Menu bar → **Runeheir ▸ Setup ▸ Build Prototype Scenes**

This generates two scenes and their materials (`Assets/_Runeheir/Generated/Materials`, toon-shaded under URP) and adds both scenes to Build Profiles. Re-run it any time; it rebuilds them from code. Command-line and CI builds call the same generator (`RuneheirSetupWizard.GenerateScenes()`), so the scenes never need to be committed.

| Scene | Contents |
|---|---|
| `Assets/_Runeheir/Scenes/RH_Login.unity` | Main Camera, Sun, `FrontEndController` (builds the login UI at runtime) |
| `Assets/_Runeheir/Scenes/RH_World.unity` | Isometric camera rig, a sun and the `WorldBootstrap` (`FieldBootstrap` with **Generate Map** on). Since Phase 5 it builds whichever of the 14 maps the character is on at load time: ground, props, the runtime NavMesh, NPCs, portals, monster spawners and boss lairs ([Phase 5 Guide](Phase5_Guide.md)). It replaces the Phase 2–4 hand-built `RH_Field_WhisperwoodPlains` |

Then:
- **Full flow:** open `RH_Login` → **Play** → type an ID and password → **Register** (logs you in) → **Vigrid Haven** → pick an empty slot → **Create** → **Start**.
- **Quick test:** open `RH_World` → **Play**. You get a temporary character "Wanderer" in Vigrid Haven that is not saved (change **Map Id** on `WorldBootstrap` to start on another map).

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
- **Hand-built maps (Phase 5):** add a **NavMeshSurface** (AI Navigation, already installed) to your level root → **Bake**. Keep or remove the `RuntimeNavMeshBaker`; it checks in `Start` (after every NavMeshSurface has registered its data) and does nothing when an editor-baked NavMesh already exists.

### Your own player model
`FieldBootstrap ▸ Player Visual Prefab` accepts any model (FBX from Blender). `EntityFactory.CreatePlayer` adds the rest: `CapsuleCollider`, `NavMeshAgent`, `NavMotor`, `CharacterAnimationBridge`, `AutoAttacker`, `PlayerCharacter`, `SkillCaster`, `AspdAnimationScaler`, `ClickToMoveController`, `HotkeyController`. Your prefab only needs the mesh and an Animator; don't add these components to it. Leave the field empty to use the procedural placeholder avatar (job-colored outfit, 8 hair styles, 9 hair colors, weapon by job).

> **The generated scenes are disposable.** `RH_Login` and `RH_World` are rebuilt from code by *Build Prototype Scenes*, by every *Runeheir ▸ Build ▸ …* build and by CI, so inspector edits made in them are lost. Put your model (and its Animator Controller) in the settings asset instead: **Runeheir ▸ Setup ▸ Create Settings Asset** → `Resources/RuneheirSettings.asset`. It is never regenerated and wins over `FieldBootstrap`. See [Phase 3 Guide §7](Phase3_Guide.md#7-animation-layer).

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

Caps: Base 99 until rebirth, 255 once reborn or on an expanded job (Phase 7, see [Phase7_Classes.md](Phase7_Classes.md)), Job 120 (transcendent jobs), each stat 1–255. Everyone starts at 1 in every stat with **48 status points**. Each level grants `3 + floor((L−1)/5)` points and raising a stat costs `floor((x−1)/10) + 2` (the classic curve; it gives exactly 1,273 points at Base 99, like RO). At Base 255 you have **7,185 points**: one stat at 255 (3,608) plus another at 200 (2,279) with change to spare, but never two maxed stats.

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

With a two-handed sword (base 150) and 50 DEX: AGI 90 gives ASPD 180, AGI 150 gives 189, AGI 220 reaches the 197 cap. Two-Hand Surge (+7) reaches the cap at AGI 160.
Menu **Runeheir ▸ Debug ▸ Log ASPD Table** prints this table; the `@aspd` chat command prints your live breakdown.

### Wiring ASPD to a real Animator (when Blender models arrive)
`CharacterAnimationBridge` drives your Animator if it finds one, and the placeholder otherwise.
1. Animator parameters: `Speed` (float), `AttackSpeed` (float), `Attack` (trigger), `Casting` (bool), `Dead` (bool), `Hit` (trigger), plus `Skill`/`SkillMotion`/`Stagger` since Phase 3. **Runeheir ▸ Animation ▸ Create Character Animator Controller** builds a controller with all of them wired ([Phase 3 Guide §7](Phase3_Guide.md#7-animation-layer)).
2. Select the **Attack** state → **Speed = 1**, **Multiplier → ☑ Parameter → `AttackSpeed`**.
3. `AspdAnimationScaler` writes ASPD's play rate (1.0–3.0) into `AttackSpeed` whenever stats change, so only the attack clip speeds up, not walking or idle.
4. Leave **Restart Attack State Each Swing** on, so very high ASPD restarts the clip instead of queueing triggers. Author attack clips at 0.6 s with the hit on the 50% frame to match `StatFormulas.ImpactFrameFraction`.

The inspector shows a live readout (ASPD, play rate, swing, interval, hits/s) while playing.

### EXP curve (`ExperienceTable.cs`) and rates
Base EXP to next = `20 × L^2.65` (Lv 99→100 ≈ 3.9 M, Lv 254→255 ≈ 47 M, 3.3 B total). Job EXP = `15 × tierMultiplier × L^2.5`.
Server rates (XileRO-style high rate): Base ×50, Job ×50, Drop ×5 by default (`FieldBootstrap`). The field scene is regenerated by every build (see §4), so change them with **Override Rates** in `Resources/RuneheirSettings.asset` (Runeheir ▸ Setup ▸ Create Settings Asset), not in the scene's inspector.

---

## 6. XileRO click-to-attack

`AutoAttacker` (shared by players and monsters) runs this loop:

1. **Chase:** re-path every 0.2 s until the target is inside weapon reach (edge to edge: dagger 0.9 m, two-hand sword 1.3 m, spear 2.2 m, bow 9 m).
2. **Swing:** stop, face the target, play the attack at the current ASPD play rate.
3. **Impact:** damage lands at 50% of the swing via `DamageCalculator` (HIT vs FLEE, crit, size table, 10-element table, hard/soft DEF, card bonuses).
4. **Repeat** every `AttackInterval` until the target dies or you click the ground (continuous by default, like `/noctrl`).

The swing schedule carries over from one swing to the next instead of restarting from the current frame, so the real attack rate matches the table above at any frame rate. A stun or freeze that lands mid-swing cancels the hit.

Monsters (`Combat/Monster.cs`) wander, aggro (aggressive types) or retaliate (passive types), leash home, and on death give EXP split by damage share and auto-loot their drops. The three **Training Dummies** next to spawn never die. After 2.5 s without hits they post your DPS and **hits/s** to chat, measured from the first hit to the last, which is the quickest way to verify ASPD.

Floating numbers: white = your damage, red = damage you take, yellow `1,234!` = critical, `Miss`, green `+heal`.

---

## 7. F1–F10 skills and items

- Open **Skills (S)** or **Items (E)** and **drag an icon onto any F1–F10 slot**. Drag between slots to swap; drag off the bar or right-click to clear; click a slot to use it. **Bind** puts a skill in the first free slot.
- Every slot can hold a skill **or** an item. **F12** cycles 4 pages (40 shortcuts). Keys are set in code: change the `slotKeys` / `nextPageKey` defaults in `Scripts/Runtime/Player/HotkeyController.cs`. The player is built at runtime by `EntityFactory.CreatePlayer`, so there is no inspector instance to edit outside Play mode.
- Slots show item counts, cooldown sweeps and after-cast delay. The layout is saved per character.

Skill flow (`SkillCaster`): key → target cursor (or **quick-cast** if you're already hovering a valid target) → walk into range → cast bar (`castTime × (1 − DEX/150)`) → SP spent → effect → after-cast delay + cooldown. Taking damage interrupts casting unless you are uninterruptible (Rage of Thor); stuns, silence and staggers always do.

Since Phase 3, skills are **learned with skill points in the Skill Tree (Alt+S)** and fire at your learned level, with statuses and poise. The rules and test commands are in **[Phase3_Guide.md](Phase3_Guide.md)**; since Phase 7 there are 282 skills across 40 jobs, all listed in **[Phase7_Classes.md](Phase7_Classes.md)**.

Items: Lingonberry Tonic, Honey Mead, Aether Sap Vial, **Uruz / Tiwaz / Sowilo runestones** (GDD §7), Raven Feather (return to save point), Wind Rune Shard (random teleport).

**Job change:** reach Job Lv 10 as an Initiate (40 / 70 for later tiers) → **Skills (S) → Job Change**. Initiates also need **Basic Training Lv 9** (the 9 points Job 10 gives).

**Adding a skill:** see [Phase 3 Guide §6](Phase3_Guide.md#6-how-skills-work-for-designers).

---

## 8. Login, realm select and character select

| Screen | XileRO-style behaviour |
|---|---|
| Login | ID + password, **Save ID**, Register, Exit. Enter = login, Tab = switch field. |
| Realm | Offline: **Vigrid Haven** (this PC). Online (Phase 6): the realm you hosted or joined, with its message of the day. Hosting and joining happen on the realm screen before login ([Phase 6 guide](Phase6_Guide.md)). |
| Character select | 9 slots (3×3); stat sheet (Base/Job Lv, EXP %, map, STR..LUK); rotating 3D preview; Start / Create / Delete / Logout. Arrows move, Enter starts, Del deletes (you must type the character name). |
| Character create | Name (4–23 characters: letters, numbers and single spaces; unique across all accounts, ignoring case), gender, 8 hair styles, 9 hair colors, live preview. You start as an Initiate in Whisperwood Plains. |

**Storage:** `LocalAccountService` keeps accounts in `Application.persistentDataPath/runeheir_local_accounts.json`. Passwords are **PBKDF2-SHA256 hashed with a per-account salt**, never stored as plain text. Characters autosave every 60 s, on level-up, on job change, on returning to character select and on quit.
Menu **Runeheir ▸ Debug ▸ Reveal / Delete Local Account Database** to inspect or reset it (delete is disabled during Play, because the running game would write the data back).

**Crash safety:** each save writes `.tmp` and then swaps it in, keeping the previous file as `.bak`. A damaged file is kept as `.corrupt-…` and the game recovers from `.tmp` or `.bak`. If the file can't be read (locked by another program, no permission), or another game window changed it, the game won't overwrite it: saves fail with a message in chat (once per error), on the login screen and in the Console, and `@save` only confirms a save that really landed. A failed save is rolled back in the stored data, so character select always shows what is on disk; the character you are playing keeps its unsaved progress until you leave the field.

**Going online (Phase 6, done):** the screens only talk to `IAccountService`; online, `NetworkAccountService` (in `Scripts/Net`) talks to the realm over Mirror, and the realm keeps the accounts with the same `LocalAccountService` rules in `realm_accounts.json`. The screens are ready for slow replies: each one locks its controls while a request is in flight and ignores replies that arrive after you've moved on. Returning to character select waits for the save before reloading the list, and saves send a snapshot of the character. The quit-time save is best-effort: a network service should also save on a timer, as the autosave already does. `AccountStore` (in Core) holds the login/char-server rules and can run on the server unchanged.

---

## 9. Testing

**In-game test commands** (editor and development builds only). Press Enter and type:

| Command | Effect |
|---|---|
| `@help` | List commands |
| `@blvl 255` / `@jlvl 120` | Set Base level (status points recalculated) / Job level (capped at the current job's max: Initiate 10, 1st 50, 2nd 70, transcendent 120, expanded jobs 50–99; skill points adjusted) |
| `@job einherjar` · `@jobs` | Change job (any name: `shadow walker`, `archmage`, …) |
| `@allstats 150` · `@agi 230` · `@dex 150` | Set stats |
| `@reset` | Stat reset (refunds all points) |
| `@aspd` | Print the ASPD → animation breakdown |
| `@item honey_mead 50` · `@items` | Spawn items |
| `@monster dire_wolf 3` · `@monsters` | Spawn monsters next to you |
| `@heal` · `@save` · `@where` | Utilities |
| `@allskills` · `@learn bash 5` · `@skillpoint 50` · `@skillreset` · `@status poison 10` · `@cleanse` | Skills and statuses ([Phase 3 Guide §8](Phase3_Guide.md#8-new-gm-commands)) |
| `@zeny 100000` · `@items cards` · `@refine 10` | Gear and loot ([Phase 4 Guide §11](Phase4_Guide.md#11-new-gm-commands)) |

Suggested ASPD check: `@job einherjar` → `@blvl 255` → `@agi 150` → hit a Training Dummy and read the hits/s in chat → cast **Two-Hand Surge** → hit again (higher) → **Rage of Thor** (locked at 195).

**Automated tests:** Window → General → **Test Runner**.

| Tab | Tests | What they cover |
|---|---|---|
| **EditMode** | 121 Core tests | Formulas, ASPD, stat points, EXP and level caps, job changes, damage, buffs, hotkeys, inventory, accounts; Phase 3 skill trees, skill catalog, statuses and poise; Phase 4 equipment, cards, refining, runewords, trade and storage |
| **EditMode** | 3 editor tests | The scene generator builds playable scenes (spawners, NavMesh baker, wiring); the toon shader imports with no errors on the active graphics API; the Animator Controller builder wires every parameter |
| **PlayMode** | 4 smoke tests | Walk on the NavMesh, auto-attack a dummy, skills must be learned, Two-Hand Surge Lv 10 raises ASPD by 7, passives and Silence, an F2 hotkey uses an item, level-up, death and respawn; a Forest Imp aggroes and dies for EXP; gear, cards and on-hit procs, the town NPCs, a Dead Branch and storage; the login, realm, character select and create screens open, and passwords are never stored in plain text |

Unity fails a test whenever an error or exception is logged, so the PlayMode tests also catch crashes in Update loops (HUD, AI, skills) while they run.

The Core tests also run without Unity: `dotnet test Tools/CoreTests/Tests` (needs the .NET 8 SDK).

---

## 10. Step 5 — Cel-shaded toon shader (`Runeheir/Toon`)

`Assets/_Runeheir/Resources/RuneheirToon.shader` is a hand-written URP shader for the "HD-XileRO" anime look. It sits in a `Resources` folder so every player build includes it.

- **Cel shading:** one hard light/shadow band with a cool shadow tint, crisp received shadows, banded point and spot lights (Forward and Forward+), and ambient from light probes / the skybox.
- **Anime highlights:** a banded specular spot and a rim light along silhouette edges, strongest on the lit side.
- **Ink outline:** an inverted-hull pass with a **constant on-screen width** (pixels at 1080p), so outlines stay crisp at every zoom level.
- **Plays well with URP:** casts and receives shadows (including screen-space shadows), receives SSAO (on in the PC renderer), has depth and depth-normals passes, fog, GPU instancing and SRP Batcher support.
- **Checked offline:** all 5 passes compile with Microsoft DXC against the real URP 17.3 shader library, with each keyword switched on one at a time plus an all-off and an all-on variant (204 compilations for Vulkan, D3D, and Metal via SPIR-V). In Unity, the `ToonShader_ImportsWithoutErrors` test checks the shader on your graphics API.

| Property | Default | Notes |
|---|---|---|
| Base Map / Base Color | white | Your Krita texture × tint |
| Shadow Tint | (0.62, 0.66, 0.82) | Color of the shadow side (cool blue = Fimbulwinter) |
| Light / Shadow Threshold | 0.52 | Where the band splits |
| Band Edge Softness | 0.03 | 0.001 = razor-sharp, 0.5 = soft |
| Received Shadow Strength | 1 | How dark cast shadows get |
| Ambient Strength | 0.6 | Sky/probe fill light |
| Specular Color (A = intensity) / Size | A 0.35 / 0.1 | Set A to 0 for matte cloth |
| Rim Color (A = intensity) / Threshold | A 0.4 / 0.72 | Backlit edge glow |
| Emission | black | HDR; the runestones and campfire use it |
| Outline Color / Width | near-black / 2 px | 0 = no outline |

**Where it's used:** `RuntimeMaterials.Lit(color, outlineWidth)` builds toon materials for the placeholder player, monsters and preview pedestals (2 px outline). The scene generator gives props a 1.5 px outline and the ground none. Without URP, or if the shader is unsupported on the GPU, both fall back to the pipeline's default lit shader automatically.

**Outlines on hard-edged meshes:** the inverted hull pushes vertices along their normals, so meshes with split normals (the placeholder cubes) show small gaps at sharp corners. For Blender characters, export with smooth normals or keep the outline width small on hard-surface props. A smoothed-normal bake for the outline is planned with the Phase 3 models.

---

---

## 11. Builds and CI

**Local builds:** menu **Runeheir ▸ Build ▸ Windows Player** or **Linux Player**. Each regenerates the prototype scenes and builds to `Builds/`. From the command line:

```
Unity -batchmode -quit -projectPath Runeheir_EchoesOfFimbul -executeMethod Runeheir.EditorTools.RuneheirBuild.BuildWindows
```

**GitHub Actions** (`.github/workflows/unity-ci.yml`) runs on every push that touches this folder:

| Job | Needs | What it does |
|---|---|---|
| Core rules (dotnet) | nothing | Builds `Runeheir.Core` at Unity's API level (.NET Standard 2.1, warnings as errors) and runs the Core rule tests |
| Unity 6.3 EditMode + PlayMode tests | Unity license secrets | Opens the project in real Unity 6000.3.25f1 (GameCI) and runs every test above |
| Build StandaloneWindows64 / StandaloneLinux64 | Unity license secrets | Builds both players and uploads them as artifacts |

To turn on the Unity jobs, add these repository secrets (GitHub → **Settings ▸ Secrets and variables ▸ Actions**):

- `UNITY_EMAIL` and `UNITY_PASSWORD`: your Unity ID.
- `UNITY_LICENSE`: the full contents of your `Unity_lic.ulf` (Personal licence; see the GameCI "activation" guide). Pro or Plus users set `UNITY_SERIAL` instead.

Until then those jobs show as skipped, with a notice explaining why.

The test job pins `game-ci/unity-test-runner@v4.3.2`. From v4.4.0, GameCI's runner can't activate Personal `.ulf` licences: the job stops at "TimeStamp validation failed" before any test runs. Keep the pin until GameCI fixes that.

**Realm servers:** **Runeheir ▸ Build ▸ Linux / Windows Realm Server (headless)**, or add `-realmServer` to the command-line build ([Phase 6 guide §4](Phase6_Guide.md#4-dedicated-realm-server)).

---

## 12. Troubleshooting

| Symptom | Fix |
|---|---|
| `Scene 'RH_…' is not in Build Settings` | Run **Runeheir ▸ Setup ▸ Build Prototype Scenes**, or add both scenes in File ▸ Build Profiles. |
| Clicks do nothing / no NavMesh error in Console | The field needs `RuntimeNavMeshBaker` on the environment root, or a baked NavMeshSurface. |
| `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class…` | A scene contains an old `StandaloneInputModule`. Delete that EventSystem; the game creates the correct one. |
| `'BuildTarget' does not contain a definition for 'ReservedCFE'` (inside `com.unity.inputsystem`), then a `TypeLoadException` for `InputActionAsset` | The Input System package is older than Unity 6.3 supports. `Packages/manifest.json` must ask for **`com.unity.inputsystem` 1.20.0** or newer (this repo does since 6 Oct 2026). Close Unity, fix the version, delete `Packages/packages-lock.json` if present, and reopen. |
| No **Runeheir** menu in the menu bar | Scripts didn't compile. Open **Window ▸ General ▸ Console** and fix the first red error. A package error blocks every script in the project. |
| Assembly error mentioning `Unity.InputSystem` | Install **Input System** via Package Manager (it is preinstalled in Unity 6 templates). |
| Pink objects | The project isn't using URP. This project ships with URP assigned; in another project, assign a URP asset in Project Settings ▸ Graphics. |
| Outlines missing | Outline Width is 0 on that material, or the object doesn't use `Runeheir/Toon` (check `RuntimeMaterials.ToonAvailable`). |
| UI text missing | Unity versions before 2022.2 use `Arial.ttf`; `UITheme.Font` falls back to an OS font automatically. |
| Alt+key opens an editor menu | Use the plain keys **A / S / E** or the Basic Info buttons. |

---

## Design assumptions to confirm
These numbers weren't fixed by the GDD, so I picked Ragnarok-style defaults. Each one is a single constant you can change:
- **Job level caps per tier:** Initiate 10, 1st 50, 2nd 70, transcendent 120; expanded jobs set their own (`JobDatabase`).
- **ASPD:** stats-based renewal-style curve, swing + recovery split 0.6 / 0.4 s (`StatFormulas`).
- **"Frozen foes take 300% bonus blunt damage"** is read as ×3 total (`FrozenBluntDamageMultiplier`; set it to 4 for +300%).
- **EXP curve and server rates** (`ExperienceTable`, `FieldBootstrap`).
- **Status point curve** (classic RO).

## What's next
- **Phase 2 Step 5 polish:** smoothed-normal outline bake for imported Blender models; optional Shader Graph port for artists.
- **Phase 3 (done):** skill trees, 105 skills, statuses, poise/stagger and the animator builder. See [Phase3_Guide.md](Phase3_Guide.md).
- **Phase 4 (done):** the 10-slot paperdoll, 87 items, 35 Soul Cards, refining to +20, runewords, shops and storage. See [Phase4_Guide.md](Phase4_Guide.md).
- **Phase 5 (done):** the 14 maps, the bestiary, monster skills, bosses and MVPs. See [Phase5_Guide.md](Phase5_Guide.md).
- **Phase 6 (done):** Mirror multiplayer (host, join, dedicated realm servers), chat, parties with Even Share, guilds, trades, the Pushcart and street stalls. See [Phase6_Guide.md](Phase6_Guide.md).
- **Phase 7 (in code):** the full Ragnarok roster under Norse names (40 jobs, 282 skills), rebirth at Urðr's Well, the expanded jobs and Freyja's Kin, songs, coins, Mimicry, Haggle, Rune Forging and Brewing. Art for the new weapons and Freyja's Kin is still placeholder. See [Phase7_Classes.md](Phase7_Classes.md).
