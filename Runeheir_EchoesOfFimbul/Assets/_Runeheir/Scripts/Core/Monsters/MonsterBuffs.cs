using System;
using Runeheir.Combat;
using Runeheir.Stats;

namespace Runeheir.Monsters
{
    /// <summary>Buffs monsters cast on themselves and their allies, and the lasting buffs of boss phases.</summary>
    public static class MonsterBuffs
    {
        public const string Harden = "mon_harden";
        public const string PackHowl = "mon_pack_howl";
        public const string Frenzied = "mon_frenzied";
        public const string ShieldWall = "mon_shield_wall";
        public const string Berserk = "mon_berserk";
        public const string IceArmor = "mon_ice_armor";
        public const string StoneSkin = "mon_stone_skin";
        public const string WarCry = "mon_war_cry";

        /// <summary>Boss phase 1: angrier.</summary>
        public const string Enraged = "mon_enraged";

        /// <summary>Boss phase 2: the gloves come off.</summary>
        public const string Unbound = "mon_unbound";

        /// <summary>Phase buffs last the rest of the fight (they're removed when the boss resets).</summary>
        public const float PhaseDuration = 3600f;

        internal static void RegisterAll(Action<BuffDefinition> register)
        {
            register(new BuffDefinition
            {
                Id = Harden, Name = "Harden", Description = "DEF +60%.", IconLabel = "HRD", IconColorHex = "#7E8F5A", Duration = 15f,
                Modifiers = new StatModifiers { DefPercent = 60f },
            });
            register(new BuffDefinition
            {
                Id = PackHowl, Name = "Pack Howl", Description = "Physical damage +20%, movement speed +25%.", IconLabel = "HWL", IconColorHex = "#5D6D7E",
                Duration = 20f, Modifiers = new StatModifiers { PhysicalDamagePercent = 20f, MoveSpeedPercent = 25f },
            });
            register(new BuffDefinition
            {
                Id = Frenzied, Name = "Frenzied", Description = "ASPD +30%, movement speed +40%.", IconLabel = "FRZ", IconColorHex = "#CA6F1E", Duration = 20f,
                Modifiers = new StatModifiers { AspdPercent = 30f, MoveSpeedPercent = 40f },
            });
            register(new BuffDefinition
            {
                Id = ShieldWall, Name = "Shield Wall", Description = "Takes 35% less damage.", IconLabel = "WAL", IconColorHex = "#566573", Duration = 15f,
                Modifiers = new StatModifiers { DamageTakenPercent = -35f },
            });
            register(new BuffDefinition
            {
                Id = Berserk, Name = "Berserk", Description = "Physical damage +40%, ASPD +30%, DEF -30%.", IconLabel = "BSK", IconColorHex = "#B03A2E",
                Duration = 25f, Modifiers = new StatModifiers { PhysicalDamagePercent = 40f, AspdPercent = 30f, DefPercent = -30f },
            });
            register(new BuffDefinition
            {
                Id = IceArmor, Name = "Ice Armor", Description = "DEF +100%, MDEF +50%.", IconLabel = "ICE", IconColorHex = "#85C1E9", Duration = 20f,
                Modifiers = new StatModifiers { DefPercent = 100f, MdefPercent = 50f },
            });
            register(new BuffDefinition
            {
                Id = StoneSkin, Name = "Stone Skin", Description = "Takes 50% less damage.", IconLabel = "STN", IconColorHex = "#909497", Duration = 15f,
                Modifiers = new StatModifiers { DamageTakenPercent = -50f },
            });
            register(new BuffDefinition
            {
                Id = WarCry, Name = "War Cry", Description = "Physical damage +30%, HIT +20%.", IconLabel = "CRY", IconColorHex = "#A93226", Duration = 30f,
                Modifiers = new StatModifiers { PhysicalDamagePercent = 30f, HitPercent = 20f },
            });
            register(new BuffDefinition
            {
                Id = Enraged, Name = "Enraged", Description = "Damage +25%, ASPD +20%, movement speed +15%.", IconLabel = "ENR", IconColorHex = "#E74C3C",
                Duration = PhaseDuration,
                Modifiers = new StatModifiers { PhysicalDamagePercent = 25f, MagicDamagePercent = 25f, AspdPercent = 20f, MoveSpeedPercent = 15f },
            });
            register(new BuffDefinition
            {
                Id = Unbound, Name = "Unbound", Description = "Damage +50%, ASPD +40%, movement speed +30%.", IconLabel = "UNB", IconColorHex = "#8E44AD",
                Duration = PhaseDuration,
                Modifiers = new StatModifiers { PhysicalDamagePercent = 50f, MagicDamagePercent = 50f, AspdPercent = 40f, MoveSpeedPercent = 30f },
            });
        }
    }
}
