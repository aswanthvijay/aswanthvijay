using System;
using Runeheir.Combat;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    /// <summary>
    /// Buffs and debuffs applied by Phase 3 skills. Registered into <see cref="BuffCatalog"/> by its static
    /// constructor, so <c>BuffCatalog.Get</c> finds them no matter which catalog is touched first.
    /// Durations here are defaults; most skills override them per level (<c>SkillDefinition.BuffDuration</c>).
    /// </summary>
    public static class SkillBuffs
    {
        public const string Provoked = "provoked";
        public const string Endure = "endure";
        public const string BattleFrenzy = "battle_frenzy";
        public const string Terrified = "terrified";
        public const string GuardiansOath = "guardians_oath";
        public const string Holdfast = "holdfast";
        public const string FreyjasShield = "freyjas_shield";
        public const string ShadowCloak = "shadow_cloak";
        public const string ShadowVeil = "shadow_veil";
        public const string TrueSight = "true_sight";
        public const string GaleStep = "gale_step";
        public const string RuneAmplify = "rune_amplify";
        public const string AutoRune = "auto_rune";
        public const string Bogged = "bogged";
        public const string VolcanoAura = "volcano_aura";
        public const string HasteRune = "haste_rune";
        public const string TimeSlowed = "time_slowed";
        public const string OdinsBlessing = "odins_blessing";
        public const string SwiftWind = "swift_wind";
        public const string ValorAura = "valor_aura";
        public const string DivineBulwark = "divine_bulwark";
        public const string SpiritSpheres = "spirit_spheres";
        public const string DiamondSkin = "diamond_skin";
        public const string ZenBreath = "zen_breath";

        public const int MaxSpiritSpheres = 5;

        internal static void RegisterAll(Action<BuffDefinition> register)
        {
            // ---- Warrior line
            register(new BuffDefinition
            {
                Id = Provoked, Name = "Provoked", IsDebuff = true, IconLabel = "PRV", IconColorHex = "#922B21", Duration = 30f,
                Description = "Hits harder, but DEF is lowered.",
                Modifiers = new StatModifiers { DefPercent = -10f, PhysicalDamagePercent = 5f },
                ModifiersPerLevel = new StatModifiers { DefPercent = -5f, PhysicalDamagePercent = 3f },
            });
            register(new BuffDefinition
            {
                Id = Endure, Name = "Endure", IconLabel = "END", IconColorHex = "#B03A2E", Duration = 10f,
                Description = "Can't be staggered; +1 MDEF per level.",
                Modifiers = new StatModifiers { StaggerImmune = true, Mdef = 1 },
                ModifiersPerLevel = new StatModifiers { Mdef = 1 },
            });
            register(new BuffDefinition
            {
                Id = BattleFrenzy, Name = "Battle Frenzy", IconLabel = "BFZ", IconColorHex = "#D35400", Duration = 30f,
                Description = "+3% ASPD and +2 HIT per level.",
                Modifiers = new StatModifiers { AspdPercent = 3f, Hit = 2 },
                ModifiersPerLevel = new StatModifiers { AspdPercent = 3f, Hit = 2 },
            });
            register(new BuffDefinition
            {
                Id = Terrified, Name = "Terrified", IsDebuff = true, IconLabel = "WCY", IconColorHex = "#943126", Duration = 20f,
                Description = "DEF lowered by a Berserker's war cry.",
                Modifiers = new StatModifiers { DefPercent = -8f },
                ModifiersPerLevel = new StatModifiers { DefPercent = -4f },
            });
            register(new BuffDefinition
            {
                Id = GuardiansOath, Name = "Guardian's Oath", IconLabel = "GOA", IconColorHex = "#AAB7B8", Duration = 300f,
                Description = "+3% block chance per level against physical melee hits; 10% slower.",
                Modifiers = new StatModifiers { BlockChance = 3f, MoveSpeedPercent = -10f },
                ModifiersPerLevel = new StatModifiers { BlockChance = 3f },
            });
            register(new BuffDefinition
            {
                Id = Holdfast, Name = "Holdfast", IconLabel = "HLD", IconColorHex = "#839192", Duration = 15f,
                Description = "4% less damage taken per level, can't be staggered, 30% slower.",
                Modifiers = new StatModifiers { DamageTakenPercent = -4f, StaggerImmune = true, MoveSpeedPercent = -30f },
                ModifiersPerLevel = new StatModifiers { DamageTakenPercent = -4f },
            });
            register(new BuffDefinition
            {
                Id = FreyjasShield, Name = "Freyja's Shield", IconLabel = "FRS", IconColorHex = "#F7DC6F", Duration = 120f,
                Description = "Less damage taken and more poise.",
                Modifiers = new StatModifiers { DamageTakenPercent = -3f, MaxPoisePercent = 5f },
                ModifiersPerLevel = new StatModifiers { DamageTakenPercent = -2f, MaxPoisePercent = 5f },
            });

            // ---- Scout line
            register(new BuffDefinition
            {
                Id = ShadowCloak, Name = "Shadow Cloak", IconLabel = "SCL", IconColorHex = "#212F3D", Duration = 10f,
                Description = "Hidden from monsters. Attacking or using a skill reveals you.",
                Traits = BuffTraits.Stealth,
                Modifiers = new StatModifiers { MoveSpeedPercent = -60f },
                ModifiersPerLevel = new StatModifiers { MoveSpeedPercent = 5f },
            });
            register(new BuffDefinition
            {
                Id = ShadowVeil, Name = "Shadow Veil", IconLabel = "SHV", IconColorHex = "#1B2631", Duration = 15f,
                Description = "Hidden from monsters; the attack that breaks the veil is a guaranteed critical.",
                Traits = BuffTraits.Stealth | BuffTraits.AmbushCritical,
                Modifiers = new StatModifiers { MoveSpeedPercent = -25f },
                ModifiersPerLevel = new StatModifiers { MoveSpeedPercent = 2.5f },
            });
            register(new BuffDefinition
            {
                Id = TrueSight, Name = "True Sight", IconLabel = "TRS", IconColorHex = "#F4D03F", Duration = 60f,
                Description = "+5 all stats, +3 HIT and +1 CRIT per level.",
                Modifiers = new StatModifiers { Hit = 3, Crit = 1f }.AddAllStats(5),
                ModifiersPerLevel = new StatModifiers { Hit = 3, Crit = 1f },
            });
            register(new BuffDefinition
            {
                Id = GaleStep, Name = "Gale Step", IconLabel = "GLS", IconColorHex = "#76D7C4", Duration = 130f,
                Description = "+2% movement speed and +1 FLEE per level.",
                Modifiers = new StatModifiers { MoveSpeedPercent = 2f, Flee = 1 },
                ModifiersPerLevel = new StatModifiers { MoveSpeedPercent = 2f, Flee = 1 },
            });

            // ---- Mystic line
            register(new BuffDefinition
            {
                Id = RuneAmplify, Name = "Rune Amplify", IconLabel = "AMP", IconColorHex = "#A569BD", Duration = 30f,
                Description = "Next damaging spell: +5% damage per level.",
                Traits = BuffTraits.ConsumedBySpell,
                Modifiers = new StatModifiers { MagicDamagePercent = 5f },
                ModifiersPerLevel = new StatModifiers { MagicDamagePercent = 5f },
            });
            register(new BuffDefinition
            {
                Id = AutoRune, Name = "Auto Rune", IconLabel = "ARN", IconColorHex = "#2E4053", Duration = 120f,
                Description = "Basic hits can cast a learned bolt for free.",
                Proc = new ProcDefinition
                {
                    SkillIds = new[] { "muspel_bolt", "frost_spike", "thunder_rune" },
                    Chance = new LevelValue(7f, 1f),
                    ProcLevel = LevelValue.Table(1, 1, 2, 2, 3, 3, 3, 4, 4, 5),
                    OnlyLearned = true,
                },
            });
            register(new BuffDefinition
            {
                Id = Bogged, Name = "Bogged", IsDebuff = true, IconLabel = "BOG", IconColorHex = "#566573", Duration = 1.5f,
                Description = "Stuck in a freezing bog: -5 AGI and DEX per level, half movement speed.",
                Modifiers = new StatModifiers { MoveSpeedPercent = -50f }.SetStat(StatType.Agi, -5).SetStat(StatType.Dex, -5),
                ModifiersPerLevel = new StatModifiers().SetStat(StatType.Agi, -5).SetStat(StatType.Dex, -5),
            });
            register(new BuffDefinition
            {
                Id = VolcanoAura, Name = "Mystic Volcano", IconLabel = "VOL", IconColorHex = "#CB4335", Duration = 2f,
                Description = "+10 ATK per level and +5% magic damage (+2% per level).",
                Modifiers = new StatModifiers { Atk = 10, MagicDamagePercent = 5f },
                ModifiersPerLevel = new StatModifiers { Atk = 10, MagicDamagePercent = 2f },
            });
            register(new BuffDefinition
            {
                Id = HasteRune, Name = "Haste Rune", IconLabel = "HST", IconColorHex = "#F5B041", Duration = 60f,
                Description = "+2% ASPD and 2% faster casting per level.",
                Modifiers = new StatModifiers { AspdPercent = 2f, CastTimePercent = -2f },
                ModifiersPerLevel = new StatModifiers { AspdPercent = 2f, CastTimePercent = -2f },
            });
            register(new BuffDefinition
            {
                Id = TimeSlowed, Name = "Slowed Time", IsDebuff = true, IconLabel = "SLT", IconColorHex = "#7D3C98", Duration = 1.5f,
                Description = "Moves and attacks slower.",
                Modifiers = new StatModifiers { MoveSpeedPercent = -20f, AspdPercent = -10f },
                ModifiersPerLevel = new StatModifiers { MoveSpeedPercent = -6f, AspdPercent = -5f },
            });

            // ---- Devotee line
            register(new BuffDefinition
            {
                Id = OdinsBlessing, Name = "Odin's Blessing", IconLabel = "BLS", IconColorHex = "#F4D03F", Duration = 60f,
                Description = "+1 STR, INT and DEX per level.",
                Modifiers = new StatModifiers().SetStat(StatType.Str, 1).SetStat(StatType.Int, 1).SetStat(StatType.Dex, 1),
                ModifiersPerLevel = new StatModifiers().SetStat(StatType.Str, 1).SetStat(StatType.Int, 1).SetStat(StatType.Dex, 1),
            });
            register(new BuffDefinition
            {
                Id = SwiftWind, Name = "Swift Wind", IconLabel = "INC", IconColorHex = "#76D7C4", Duration = 60f,
                Description = "+3 AGI (+1 per level) and +25% movement speed.",
                Modifiers = new StatModifiers { MoveSpeedPercent = 25f }.SetStat(StatType.Agi, 3),
                ModifiersPerLevel = new StatModifiers().SetStat(StatType.Agi, 1),
            });
            register(new BuffDefinition
            {
                Id = ValorAura, Name = "Valor Aura", IconLabel = "VAL", IconColorHex = "#E59866", Duration = 180f,
                Description = "+5 ATK and 1% less damage taken per level.",
                Modifiers = new StatModifiers { Atk = 5, DamageTakenPercent = -1f },
                ModifiersPerLevel = new StatModifiers { Atk = 5, DamageTakenPercent = -1f },
            });
            register(new BuffDefinition
            {
                Id = DivineBulwark, Name = "Divine Bulwark", IconLabel = "ASM", IconColorHex = "#FAD7A0", Duration = 20f,
                Description = "10% less damage taken per level.",
                Modifiers = new StatModifiers { DamageTakenPercent = -10f },
                ModifiersPerLevel = new StatModifiers { DamageTakenPercent = -10f },
            });
            register(new BuffDefinition
            {
                Id = SpiritSpheres, Name = "Spirit Spheres", IconLabel = "SPH", IconColorHex = "#F8C471", Duration = 600f,
                Description = "+3 ATK per sphere. Spent by Occult Strike, Spirit Barrage, Thunder Palm and Diamond Skin.",
                MaxStacks = MaxSpiritSpheres,
                ModifiersPerStack = new StatModifiers { Atk = 3 },
            });
            register(new BuffDefinition
            {
                Id = DiamondSkin, Name = "Diamond Skin", IconLabel = "DMS", IconColorHex = "#D5D8DC", Duration = 30f,
                Description = "+100 DEF and +10 MDEF per level; -25% movement speed and ASPD.",
                Modifiers = new StatModifiers { Def = 100, Mdef = 10, MoveSpeedPercent = -25f, AspdPercent = -25f },
                ModifiersPerLevel = new StatModifiers { Def = 100, Mdef = 10 },
            });
            register(new BuffDefinition
            {
                Id = ZenBreath, Name = "Zen Breath", IconLabel = "ZEN", IconColorHex = "#A3E4D7", Duration = 60f,
                Description = "+10 SP regen and +2% Max SP per level.",
                Modifiers = new StatModifiers { SpRegenFlat = 10, MaxSpPercent = 2f },
                ModifiersPerLevel = new StatModifiers { SpRegenFlat = 10, MaxSpPercent = 2f },
            });
        }
    }
}
