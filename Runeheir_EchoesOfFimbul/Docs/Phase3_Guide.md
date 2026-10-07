# RUNEHEIR — Phase 3 Guide: Combat, Skills & Animation Layer

Phase 3 turns the Phase 2 prototype's 14 sample skills into a full Ragnarok-style skill system for all 21 jobs, adds status effects and a poise/stagger layer to combat, and gets the animation pipeline ready for real character models.

| GDD Phase 3 item | Where it lives |
|---|---|
| Skill trees with skill points | `Scripts/Core/Skills/SkillBook.cs`, `UI/Hud/SkillWindow.cs` (Alt+S) |
| Full skill lists for all 21 jobs (105 skills) | `Scripts/Core/Skills/SkillCatalog.*.cs`, buffs in `SkillBuffs.cs` |
| Status effects | `Scripts/Core/Combat/StatusEffects.cs` |
| Poise and stagger | `Scripts/Core/Combat/Poise.cs` |
| Skill effects (areas, projectiles, dashes, zones…) | `Scripts/Runtime/Player/SkillEffects.cs`, `GroundZone.cs`, `Visuals/ProjectileFx.cs` |
| Animation layer | `Visuals/CharacterAnimationBridge.cs`, `Editor/RuneheirAnimatorBuilder.cs` |
| Model and rates that survive scene rebuilds | `Field/RuneheirSettings.cs` (Runeheir ▸ Setup ▸ Create Settings Asset) |

---

## 1. Try it in 2 minutes

1. Pull the latest code, open the project in Unity 6.3, then run **Runeheir ▸ Setup ▸ Build Prototype Scenes** and press **Play**.
2. Log in and enter the field with any character. Phase 2 characters load fine: they keep their skill points, and **First Aid** is added for free.
3. Press **Alt+S** to open the **Skill Tree**. Each tab is one job in your line. Click **+** to spend a skill point. The first time you learn an active skill, it goes into a free F-key slot.
4. To test quickly, press **Enter** and type:
   - `@job einherjar` → `@blvl 200` → `@allskills`
   - Then fight a Training Dummy with **Vortex Cleave**, **Two-Hand Surge** and **Rage of Thor**.
5. Other lines to try:
   - `@job shadow walker` → `@allskills`: Shadow Veil, then attack (guaranteed critical backstab), then Phantom Barrage.
   - `@job archmage` → `@allskills`: Glacial Tempest, Muspel Wall, Frost Nova.
   - `@job champion` → `@allskills`: Spirit Call ×5, then Spirit Barrage or Fist of Odin.

---

## 2. Skill trees and skill points

- **One skill point per Job Level.** Unspent points carry over when you change job.
- **Initiates:** Job 10 gives exactly 9 points. **Basic Training Lv 9** is required for your first job change, like Ragnarok's Basic Skill.
- A skill can be learned when:
  - it belongs to your job line (your own job or an earlier one),
  - you have a point,
  - it isn't at max level,
  - and its prerequisites are met.
- Locked skills show their requirement in red (for example "Requires Bash Lv 5"). Hover any icon to see the numbers at your level and at the next level.
- **Passives** (23 of them) feed the stat engine directly: Sword Mastery's ATK, Owl's Eye's DEX, Free Cast's walk speed while casting, and so on. Weapon-specific passives apply only with that weapon.
- **Procs:**
  - Keen Edge (Scout, daggers): a chance to strike again.
  - Storm Fists (Monk): a three-hit combo.
  - Auto Rune (Sage): casts a learned bolt for free.
  - Procs trigger on basic-attack hits, at most one per hit.
- **Spirit Spheres (Monk/Champion):** Spirit Call adds one sphere per cast, up to its level (max 5). Each sphere gives +3 ATK. Occult Strike, Thunder Palm, Diamond Skin and Spirit Barrage spend them.
- **Stealth:**
  - Shadow Cloak and Shadow Veil hide you: monsters lose track of you and can't target you.
  - Attacking or using a skill reveals you (recasting a stealth skill doesn't). Cloak and Veil end together.
  - The first attack out of **Shadow Veil** is a guaranteed critical.
- **Save data:** learned levels are stored per character (`CharacterRecord.Skills`). Older saves are migrated automatically, and bad entries are repaired instead of crashing.
- **Signature skills:** the GDD signature skills keep their Phase 2 numbers at max level. Examples: Vortex Cleave 2×400%, Two-Hand Surge +7 ASPD, Rage of Thor 30 s, Phantom Barrage 8×110% with a guaranteed stun, Miasma Weapon ×4, Glacial Tempest 5×200% with 35% freeze, Runic Aegis 10 blocks, Fist of Odin +1750, Aether Snap 8 m.

**How many points?** A character at max Job level in every tier has earned 9 + 49 + 69 + 119 = 246 points. Each full job line (Initiate to Ascended) has 120–164 skill levels, so in this high-rate style an endgame character can max their whole line. While leveling, you choose.

---

## 3. All skills

Generated from `SkillCatalog` (numbers per level are in the in-game tooltips). "Needs" = prerequisite skill levels, required weapon, or Spirit Spheres.

### Initiate

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Basic Training** | 9 | Passive | Survival basics: +1 HIT, +1 FLEE and +1 HP regen per level. | – |
| **First Aid** | 1 | Self | Bandage your wounds: heals 25 + Base Level HP. | – |

### Warrior (after Initiate)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Bash** | 10 | Enemy | A heavy blow. From Lv 6 it can stun. | – |
| **Magnum Break** | 10 | Self | A fiery shockwave around you that knocks enemies back. | Bash 5 |
| **Provoke** | 10 | Enemy | Enrage an enemy: it hits harder but its DEF drops, and it comes after you. | – |
| **Endure** | 10 | Self | Grit your teeth: you can't be staggered, and MDEF rises. | Provoke 5 |
| **Sword Mastery** | 10 | Passive | +4 ATK per level with swords and greatswords. | – |
| **Iron Constitution** | 10 | Passive | +3 HP regen and +0.5% Max HP per level. | – |

### Berserker (after Warrior)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Battle Frenzy** | 10 | Self | Greatsword fury: +3% ASPD and +2 HIT per level. | Sword Mastery 1, Greatsword |
| **Cleaving Strike** | 10 | Enemy | A wide cut that also hits everyone next to your target. | Bash 5 |
| **War Cry** | 5 | Self | A terrifying roar: nearby enemies lose DEF and turn on you. | Provoke 3 |
| **Blood Rush** | 5 | Enemy | Charge at an enemy and slam into it. Can stun. | Cleaving Strike 3 |
| **Feral Resilience** | 10 | Passive | +2% Max HP and +5% poise per level. | – |
| **Reckless Edge** | 10 | Passive | +1% physical damage and +1 CRIT per level with greatswords. | – |

### Einherjar (after Berserker)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Vortex Cleave** | 10 | Self | 360° greatsword sweep: two hits that launch enemies. Each launched enemy that lands on others crashes into them (half power, at most once per enemy per cast). | Bash 10, Magnum Break 3 |
| **Two-Hand Surge** | 10 | Self | +0.7 ASPD per level (+7 at Lv 10) and attack recovery canceling. | Battle Frenzy 5, Greatsword |
| **Rage of Thor** | 5 | Self | Max HP x3, ASPD 195, hyper-armor (no flinch, knockback or stagger); items locked. Lasts 10 s + 5 s per level. | Two-Hand Surge 5 |
| **Valhallan Might** | 10 | Passive | +2% physical damage per level. | – |

### Guardian (after Warrior)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Spear Mastery** | 10 | Passive | +4 ATK per level with spears. | – |
| **Spear Stab** | 10 | Enemy | Thrust through every enemy in a line and push them back. | Spear Mastery 3, Spear |
| **Brandish Spear** | 10 | Enemy | Sweep the spear around your target, hitting everything near it. | Spear Stab 3, Spear |
| **Guardian's Oath** | 10 | Self | Raise your guard: +3% chance per level to block physical melee hits, but walk 10% slower. | – |
| **Holdfast** | 5 | Self | Plant your feet: take 4% less damage per level and can't be staggered, but walk 30% slower. | Guardian's Oath 5 |
| **Stalwart** | 10 | Passive | +1% Max HP and +5% poise per level. | – |

### Valkyrie (after Guardian)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Valkyrie's Descent** | 10 | Ground | Leap to a spot and crash down, hitting and knocking back everyone around you. | Brandish Spear 5 |
| **Spiral Lance** | 10 | Enemy | Five spinning lance strikes on one enemy. | Spear Stab 5, Spear |
| **Freyja's Shield** | 10 | Ally/self | Shield yourself or an ally: less damage taken and more poise. | Guardian's Oath 5 |
| **Valkyrie Grace** | 10 | Passive | +1% Max HP, +5% poise and +1% movement speed per level. | – |

### Scout (after Initiate)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Twin Fang** | 10 | Enemy | Two quick stabs. | – |
| **Envenom** | 10 | Enemy | A poisoned strike that can poison the target (Poison: lose HP over time, -25% DEF). | – |
| **Dust Kick** | 5 | Enemy | Kick dirt in an enemy's eyes. Can blind (Blind: -25% HIT and FLEE). | – |
| **Pilfer** | 10 | Enemy | Try to steal one of a monster's drops (once per monster). Better with DEX and against weaker monsters. | – |
| **Evasion Drills** | 10 | Passive | +3 FLEE per level. | – |
| **Keen Edge** | 10 | Passive | With daggers, each basic hit has a 5% chance per level to strike again. | – |

### Assassin (after Scout)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Katar Mastery** | 10 | Passive | +3 ATK and +1 CRIT per level with katars. | – |
| **Shadow Cloak** | 10 | Self | Vanish: monsters lose track of you and can't target you. You move slowly (less so at higher levels). Attacking or using a skill reveals you. | Evasion Drills 3 |
| **Venom Dust** | 10 | Ground | Leave a cloud of venom on the ground that poisons enemies standing in it. | Envenom 5 |
| **Underfang** | 5 | Enemy | Strike from the shadows at everything around your target. Out of Shadow Veil it is a critical ambush. | Shadow Cloak 2, Katar |
| **Lacerate** | 10 | Enemy | Two tearing cuts that can cause Bleeding (HP loss over time, no natural regen). | Katar Mastery 3, Dagger / Katar |

### Shadow Walker (after Assassin)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Phantom Barrage** | 10 | Enemy | Rapid 8-hit strike with a guaranteed stun. | Katar Mastery 4, Katar |
| **Miasma Weapon** | 5 | Self | Poison your blades for 40 s: physical damage x2 at Lv 1, +50% per level (x4 at Lv 5). | Venom Dust 3 |
| **Shadow Veil** | 10 | Self | Conceal yourself in darkness. Your first attack out of the veil is a guaranteed critical backstab. | Shadow Cloak 5 |
| **Shadow Fang** | 10 | Enemy | Hurl a blade of shadow at a distant enemy. Never misses. | Phantom Barrage 3 |
| **Lethal Precision** | 10 | Passive | +1 CRIT per level with katars and daggers. | – |

### Ranger (after Scout)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Double Strafe** | 10 | Enemy | Loose two arrows at once. | Bow |
| **Arrow Shower** | 10 | Ground | Rain arrows on an area, pushing enemies back. | Double Strafe 5, Bow |
| **Ankle Snare** | 5 | Ground | Set a trap. The first enemy to step on it is snared in place (it can still attack). | – |
| **Raven Strike** | 5 | Enemy | Huginn dives at the target: one hit per level that ignores FLEE. | Double Strafe 3 |
| **Owl's Eye** | 10 | Passive | +1 DEX per level. | – |
| **Vulture's Eye** | 10 | Passive | +1 HIT and +0.25 m bow range per level. | – |

### Deadeye (after Ranger)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Sharp Shot** | 10 | Enemy | A piercing arrow that hits every enemy in a line and can critically hit. | Double Strafe 5, Vulture's Eye 5, Bow |
| **Raven Assault** | 5 | Enemy | Huginn and Muninn strike together: one huge hit that never misses. | Raven Strike 5 |
| **True Sight** | 10 | Self | +5 all stats, +3 HIT and +1 CRIT per level. | – |
| **Gale Step** | 10 | Ally/self | Wind at your heels (or an ally's): +2% movement speed and +1 FLEE per level. | – |

### Mystic (after Initiate)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Muspel Bolt** | 10 | Enemy | Bolts of Muspelheim fire: one bolt (100% MATK) per level. | – |
| **Frost Spike** | 10 | Enemy | Bolts of Niflheim ice: one bolt (100% MATK) per level. | – |
| **Thunder Rune** | 10 | Enemy | Bolts of Thor's lightning: one bolt (100% MATK) per level. | – |
| **Soul Lance** | 10 | Enemy | Ghostly lances: one more every two levels. Ghost element, quick to cast. | – |
| **Frost Lock** | 10 | Enemy | Encase an enemy in ice (Frozen: can't act; blunt weapons deal triple damage to it). | Frost Spike 1 |
| **Mana Focus** | 10 | Passive | +3 SP regen per level. | – |

### Sorcerer (after Mystic)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Muspel Rain** | 10 | Ground | Burning meteors fall on an area, wave after wave. Can stun. | Muspel Bolt 5, Thunder Rune 1 |
| **Storm Lance** | 10 | Enemy | A ball of lightning that hits many times and blasts the target back. | Thunder Rune 1 |
| **Frost Nova** | 10 | Self | A ring of frost around you that can freeze everything it touches. | Frost Lock 3 |
| **Muspel Wall** | 10 | Ground | A wall of fire on the ground that burns and repels enemies that touch it. | Muspel Bolt 4 |
| **Earthen Spikes** | 5 | Ground | Stone spikes erupt in an area, one wave per level. | – |
| **Arcane Mind** | 10 | Passive | +1% magic damage and 1% faster casting per level. | – |

### Archmage (after Sorcerer)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Glacial Tempest** | 10 | Ground | Multi-wave blizzard (5 waves). Each wave can freeze enemies brittle: frozen foes take triple damage from blunt weapons. | Frost Nova 3, Storm Lance 1 |
| **Runic Aegis** | 10 | Ally/self | A runic dome on you or an ally that blocks one physical melee strike per level (10 at Lv 10). | Earthen Spikes 3 |
| **Rune Amplify** | 10 | Self | Your next damaging spell deals +5% damage per level. | Arcane Mind 5 |
| **Heaven's Wrath** | 10 | Ground | Thunder from the sky strikes an area in four waves. Can blind. | Storm Lance 5, Earthen Spikes 3 |

### Sage (after Mystic)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Rune Study** | 10 | Passive | +2 MATK and +1% Max SP per level. | – |
| **Free Cast** | 10 | Passive | Walk while casting at 7.5% of your speed per level (75% at Lv 10). | – |
| **Auto Rune** | 10 | Self | While active, your basic hits can cast a learned bolt (Muspel Bolt, Frost Spike or Thunder Rune) for free. | Rune Study 3 |
| **Bog of Niflheim** | 5 | Ground | Turn the ground into a freezing bog: enemies in it lose AGI and DEX (monsters: FLEE and HIT) and move at half speed. | – |
| **Mystic Volcano** | 5 | Ground | Allies standing in the area gain ATK and magic damage. | Rune Study 5 |

### Chronomancer (after Sage)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Stasis Field** | 5 | Ground | Stop time in an area: enemies there can be frozen in place (stunned). | Bog of Niflheim 3 |
| **Haste Rune** | 10 | Ally/self | Quicken yourself or an ally: +2% ASPD and 2% faster casting per level. | Free Cast 5 |
| **Slow Time** | 5 | Ground | An area where time drags: enemies in it move and attack slower. | Stasis Field 1 |
| **Chrono Lance** | 10 | Enemy | Lances from a moment ago and a moment ahead strike at once. Ghost element. | Soul Lance 5 |
| **Temporal Mend** | 5 | Ground | Rewind wounds: allies in the area heal every second. | Haste Rune 3 |

### Devotee (after Initiate)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Eir's Blessing** | 10 | Ally/self | Heal yourself or an ally (Ragnarok Heal formula at this level). | – |
| **Divine Shelter** | 10 | Passive | Take 1% less damage per level. | – |
| **Odin's Blessing** | 10 | Ally/self | +1 STR, INT and DEX per level for you or an ally. | Divine Shelter 5 |
| **Swift Wind** | 10 | Ally/self | +3 AGI (+1 per level) and +25% movement speed for you or an ally. | Eir's Blessing 3 |
| **Holy Light** | 5 | Enemy | A beam of holy light. +50% damage to Undead and Demons. | – |
| **Purify** | 1 | Ally/self | Cure yourself or an ally of every negative status and debuff. | Eir's Blessing 2 |

### Paladin (after Devotee)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Faith** | 10 | Passive | +200 Max HP per level. | – |
| **Sacred Cross** | 10 | Enemy | Two holy cross-shaped strikes. Can blind. | Faith 5 |
| **Radiant Cross** | 10 | Self | Sacrifice 20% of your HP: a great holy cross erupts around you, three times. +50% vs Undead and Demons. | Sacred Cross 6 |
| **Valor Aura** | 10 | Self | +5 ATK and 1% less damage taken per level. | Faith 3 |
| **Judgment** | 5 | Enemy | Bring the mace down on your target and everyone beside it. Often stuns. | Sacred Cross 3, Mace |

### Templar (after Paladin)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Sanctuary** | 10 | Ground | Consecrate the ground: allies standing in it heal every second. | Eir's Blessing 3 |
| **Holy Judgment** | 10 | Ground | Waves of holy fire purge an area. Double damage to Undead and Demons. | Holy Light 3 |
| **Divine Bulwark** | 5 | Ally/self | You or an ally take 10% less damage per level (half at Lv 5). | Divine Shelter 5 |
| **Hammer of Tyr** | 5 | Ground | Smash the ground: damage and a strong chance to stun everything in the area. | Judgment 3, Mace |

### Monk (after Devotee)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Iron Fist** | 10 | Passive | +3 ATK per level with knuckles or bare hands. | – |
| **Storm Fists** | 10 | Passive | Basic hits can burst into a three-hit combo (higher levels: stronger combo, slightly lower chance). | – |
| **Spirit Call** | 5 | Self | Summon one Spirit Sphere (+3 ATK each), up to one per level. Spheres fuel Occult Strike, Spirit Barrage and Thunder Palm. | – |
| **Occult Strike** | 5 | Enemy | A palm strike through armour: ignores DEF and never misses. Uses 1 Spirit Sphere. | Iron Fist 5, 1 sphere(s) |
| **Spirit Barrage** | 5 | Enemy | Hurl your Spirit Spheres: one hit per sphere, up to the skill level. | Spirit Call 3 |
| **Diamond Skin** | 5 | Self | Harden your body: huge DEF (+100 per level) and MDEF, but -25% movement speed and ASPD. Uses 3 Spirit Spheres. | Occult Strike 3, 3 sphere(s) |

### Champion (after Monk)

| Skill | Max Lv | Type | What it does | Needs |
|---|---|---|---|---|
| **Fist of Odin** | 5 | Enemy | Drains ALL SP into one lethal, unmissable punch: ATK x (8 + SP/10) plus a flat bonus (+1750 at Lv 5). | Occult Strike 3 |
| **Aether Snap** | 5 | Ground | Instant-transmission dash (8 m at Lv 5). | Spirit Call 2 |
| **Thunder Palm** | 10 | Enemy | A four-strike palm combo. Uses 1 Spirit Sphere. | Storm Fists 5, Unarmed / Knuckles, 1 sphere(s) |
| **Zen Breath** | 5 | Self | Meditate: +10 SP regen and +2% Max SP per level. | Spirit Call 5 |


---

## 4. Status effects

Skills roll the status against the target's resistance:
- **chance** × (1 − resist%)
- **duration** × (1 − resist%/2)
- **resist%** = stat / 2, capped at 80%.

Monsters use Level/2 as a stand-in for their stats until Phase 5. Sowilo's ward blocks every status.

| Status | Effect | Resisted by |
|---|---|---|
| Stun | Can't move, attack or use skills | VIT |
| Frozen | Can't act; blunt weapons deal ×3 (GDD Glacial Tempest combo); doesn't break when hit | INT/2 + MDEF |
| Stone Curse | Can't act, DEF −50%; breaks when hit | INT/2 + MDEF |
| Sleep | Can't act; breaks when hit | INT |
| Poison | −1.5% max HP per second, DEF −25%, no natural regen | VIT |
| Bleeding | −2% max HP every 2 s, no natural regen | VIT |
| Silence | Can't use skills | VIT |
| Blind | −25% HIT and FLEE | INT |
| Frostbite | Half movement speed and ASPD (GDD Jormungandr card, unblockable) | — |
| Curse | LUK 0, −25% physical damage, −10% speed | LUK |
| Snared | Can't walk (can still attack and cast) | AGI |
| Staggered | Poise broken: 0.7 s can't act | — (see §5) |

Damage over time never kills: it stops at 1 HP. Incapacitating statuses and Silence cancel a cast even through Rage of Thor's no-flinch. Statuses show in the buff tray with a red frame, and over-time damage shows as small purple numbers. **Purify** and the **Sowilo** runestone remove all statuses and debuffs.

**Phantom Barrage's stun is guaranteed**, as the GDD says: it ignores resistance (only Sowilo blocks it).

---

## 5. Poise and stagger

GDD §4 VIT gives "stagger / poise resilience", and §7 Thurisaz "triples poise stagger damage".

- **Every hit chips poise.**
  - Basic attacks chip by weapon: greatsword 22, mace 18, spear 16, knuckles 14, sword 12, katar 10, dagger/staff/unarmed 8, bow 6.
  - Spells chip 10 per hit.
  - Skills scale this with their own poise multiplier (Fist of Odin ×5, Blood Rush ×2…).
  - Critical hits chip ×1.5.
- **Poise pools:**
  - Players: 40 + VIT×0.6 + Base Lv×0.2.
  - Monsters: Small 30, Medium 60, Large 120, + Level/2.
  - Feral Resilience, Stalwart, Valkyrie Grace and Freyja's Shield raise the pool.
- **Breaking:** at 0 poise the target is **Staggered** for 0.7 s. That cancels its swing or cast. Its poise then refills, and it can't be staggered again for 2 s (no stun-lock).
- **Recovery:** poise refills completely after 2.5 s without taking poise damage.
- **Never staggered:** Rage of Thor (hyper-armor), Endure and Holdfast.
- The `PoiseDamagePercent` modifier is ready for the Thurisaz runestone (Phase 4: +200%).

---

## 6. How skills work (for designers)

A skill is one `SkillDefinition`. Numbers are `LevelValue`s:
- `L(130, 30)` is 130 at Lv 1, +30 per level.
- `T(8, 8, 15)` is an explicit table.
- A plain number is the same at every level.

The runtime picks the behaviour from three fields:

| Field | Values |
|---|---|
| `Damage` | None · Physical · Magic |
| `Area` | Single · AroundSelf · AroundTarget (splash) · AtGround · Line (pierces, 0.7 m wide) |
| `Special` | Heal · Dash (charge to an enemy, or leap to a point and strike there) · FistOfOdin · Steal · Cleanse · Provoke · Zone (ground effect that repeats every `ZoneTick`; `ZoneTrap` fires once) · SpiritRelease |

Other knobs:
- `Projectile`: the damage lands when the bolt arrives. Bows' basic attacks fly as arrows too.
- `CanCrit`, `NeverMiss`, `IgnoreDefense`, `FlatDamage`, `HpCostPercent`, `SphereCost`.
- `Status` with `StatusChance` and `StatusDuration`.
- `BuffId` for you or an ally, with `BuffDuration` and `BuffCharges`; `DebuffId` for enemies that are hit.
- `Knockback`, `ChainImpacts` (Vortex Cleave), `PoiseMultiplier`, `BonusVsUndeadPercent`, `Weapons`, `Requires`.
- Passives use `PassivePerLevel` (a `StatModifiers` added per level), with `PassiveWeapons` and an optional `Proc`.

**Adding a skill:** add it to the right `SkillCatalog.<Line>.cs` file. If it applies a buff, add the buff to `SkillBuffs.cs`. Then run `dotnet test Tools/CoreTests/Tests`. The catalog tests check that:
- every prerequisite exists and is in the same job line,
- every buff, debuff and proc exists,
- the numbers are valid at every level,
- and every job can learn its whole tree.

---

## 7. Animation layer

`CharacterAnimationBridge` is the only thing gameplay talks to. It drives a real `Animator` when the model has one, and the procedural placeholder otherwise. The placeholder now plays:
- spins, thrusts, punches, leaps, shots, casts and buff poses for skills,
- a stagger rock-back,
- a dark see-through silhouette while you are hidden.

### Plug in a real model
1. **Runeheir ▸ Animation ▸ Create Character Animator Controller.** This builds `Assets/_Runeheir/Generated/Animation/RuneheirCharacter.controller` with every parameter and state the bridge uses:

   | Parameter | Type | Drives |
   |---|---|---|
   | `Speed` | float | Locomotion blend: Idle at 0, Run at 4 m/s |
   | `AttackSpeed` | float | ASPD play rate 1.0–3.0 on Attack and the melee skill states |
   | `Attack` | trigger | Basic attack (the bridge also restarts the **Attack** state directly at high ASPD) |
   | `Casting` | bool | Cast loop |
   | `Hit` | trigger | Flinch |
   | `Stagger` | trigger | Poise break |
   | `Dead` | bool | Death and revive |
   | `Skill` + `SkillMotion` | trigger + int | One state per motion: Swing, Thrust, Spin, Cast, Shoot, Punch, Leap, Buff |

2. Replace the placeholder clips in `Generated/Animation/Clips/` (`RH_Idle`, `RH_Run`, `RH_Attack`, `RH_Skill_Spin`…) with your Blender or Mixamo clips:
   - drag them onto the states, or
   - create an **Animator Override Controller** per character on top of this base.
   - Rebuilding the controller keeps its asset ID, so references stay linked, and it never overwrites clip files that already exist.
3. Author attack clips at **0.6 s with the hit on the 50% frame** (`StatFormulas.ImpactFrameFraction`), so ASPD scaling lines up with the damage.
4. **Runeheir ▸ Setup ▸ Create Settings Asset**, then set:
   - **Player Visual Prefab** to your model,
   - **Player Animator Controller** to the generated controller.

   The asset lives in `Resources/RuneheirSettings.asset`, so scene rebuilds, builds and CI never overwrite it (unlike inspector edits in the generated scenes). Root motion is turned off on the model: the NavMesh moves the character.
5. **Runeheir ▸ Animation ▸ Check Selected Animator** lists any parameter your own controller is missing.

### Server rates
The same settings asset has **Override Rates** (Base/Job/Drop). Use it instead of editing `FieldBootstrap` in the generated scene.

---

## 8. New GM commands

| Command | Effect |
|---|---|
| `@skills` | Your line's skills with levels and ids, plus unspent points |
| `@allskills` | Learn every skill of your line at max level (points unchanged) |
| `@learn bash 5` · `@learn VortexCleave` | Set one skill's level (id, or name without spaces) |
| `@skillpoint 50` | Set unspent skill points |
| `@skillreset` | Refund every learned level (First Aid stays) |
| `@status poison 10` · `@statuses` | Put a status on yourself for testing |
| `@cleanse` | Remove your statuses and debuffs |

---

## 9. Tests

| Where | Count | Covers |
|---|---|---|
| `Tests/EditMode` (also `dotnet test Tools/CoreTests/Tests`) | 93 | Everything from Phase 2, plus: catalog integrity, every job can learn its full tree, learning rules, reset, migration, passives and procs, level values, buff levels and stacks, stealth, steal, statuses (flags, DoT, breaking, resistance), poise |
| `Tests/Editor` | 3 | Scene generator, toon shader, **Animator Controller builder** (every parameter, ASPD-scaled attack, one state per skill motion, rebuild in place) |
| `Tests/PlayMode` | 3 | The Phase 2 smoke tests, plus: unlearned skills are refused, Two-Hand Surge Lv 10 gives +7 ASPD, Sword Mastery feeds ATK, Silence blocks skills, Purify clears it |

---

## 10. Design choices to confirm

- **Skill lists for the jobs without GDD signatures** (Valkyrie, Deadeye, Chronomancer, Templar, and the first/second jobs) are my Ragnarok-inspired picks with Norse names. Every number is one `LevelValue` in `SkillCatalog.*.cs`.
- **Basic Training Lv 9 before the first job change** (Ragnarok rule). Delete the check in `CharacterProgression.TryChangeJob` if you don't want it.
- **Freeze doesn't break on damage**, so the GDD's "frozen foes take 300% blunt damage" combo works. Stone and Sleep do break.
- **Damage over time never kills** (Ragnarok poison rule).
- **Monster resistance** uses Level/2 as a stand-in for real monster stats until Phase 5.

## What's next
- Real character models and clips through the generated controller (Art track).
- Skill VFX art: today's rings, streaks, arcs and projectiles are clear placeholders.
- Phase 4 (done): equipment, refining, cards and the Thurisaz, Isa and Hagalaz runestones. See [Phase4_Guide.md](Phase4_Guide.md).
- Phase 5: monster skills and MVPs using the same skill engine.
- Optional: choose a skill's level on each hotkey (Ragnarok's level-select drag).
