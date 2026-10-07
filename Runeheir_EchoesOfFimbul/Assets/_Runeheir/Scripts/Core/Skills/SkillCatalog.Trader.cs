using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    /// <summary>
    /// Phase 7, Ragnarok's Merchant line under Norse names: Trader (Merchant) → Runesmith (Blacksmith) → Forgelord
    /// (Mastersmith), Trader → Brewmaster (Alchemist) → Lifeweaver (Biochemist).
    /// </summary>
    public static partial class SkillCatalog
    {
        private static void RegisterTraderLine()
        {
            // ---------------------------------------------------------------- Trader (Merchant)
            Register(new SkillDefinition
            {
                Id = "strong_back", Name = "Strong Back", Job = JobId.Trader, MaxLevel = 10, Passive = true,
                Description = "Ragnarok's Enlarge Weight Limit: +200 weight capacity per level.",
                IconLabel = "EWL", IconColorHex = "#A0642D",
                PassivePerLevel = new StatModifiers { WeightCapacity = 200 },
            });
            Register(new SkillDefinition
            {
                Id = "haggle", Name = "Haggle", Job = JobId.Trader, MaxLevel = 10, Passive = true, Requires = Req("strong_back", 3),
                Description = "Ragnarok's Discount: NPC shops sell to you 2.4% cheaper per level (24% at Lv 10).",
                IconLabel = "DSC", IconColorHex = "#B9770E",
                PassivePerLevel = new StatModifiers { BuyDiscountPercent = 2.4f },
            });
            Register(new SkillDefinition
            {
                Id = "silver_tongue", Name = "Silver Tongue", Job = JobId.Trader, MaxLevel = 10, Passive = true, Requires = Req("haggle", 3),
                Description = "Ragnarok's Overcharge: NPC shops pay you 2.4% more per level (24% at Lv 10).",
                IconLabel = "OVC", IconColorHex = "#D4AC0D",
                PassivePerLevel = new StatModifiers { SellBonusPercent = 2.4f },
            });
            Register(new SkillDefinition
            {
                Id = "gold_strike", Name = "Gold-Strike", Job = JobId.Trader, MaxLevel = 10,
                Description = "Ragnarok's Mammonite: a blow backed by coin. Costs 100 zeny per level.",
                IconLabel = "MAM", IconColorHex = "#F1C40F",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Swing,
                Range = 1.4f, Power = L(150f, 50f), SpCost = 5f, ZenyCost = L(100f, 100f), PoiseMultiplier = 1.4f,
            });
            Register(new SkillDefinition
            {
                Id = "cart_charge", Name = "Cart Charge", Job = JobId.Trader, MaxLevel = 1, Requires = Req("strong_back", 5),
                Description = "Ragnarok's Cart Revolution: ram your target with the Pushcart, hitting everyone beside it. Hits harder the more you carry.",
                IconLabel = "CRV", IconColorHex = "#784212",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Area = SkillArea.AroundTarget, Motion = SkillMotion.Swing,
                RequiresPushcart = true, WeightPowerPerThousand = 10f,
                Range = 1.5f, Radius = 1.8f, Power = 150f, Knockback = 2f, SpCost = 12f, AfterCastDelay = 0.5f, PoiseMultiplier = 1.5f,
            });
            Register(new SkillDefinition
            {
                Id = "market_shout", Name = "Market Shout", Job = JobId.Trader, MaxLevel = 1,
                Description = "Ragnarok's Crazy Uproar: a bellow fit for the market square, +4 STR and +30 ATK.",
                IconLabel = "CRU", IconColorHex = "#CA6F1E",
                Target = SkillTarget.Self, BuffId = SkillBuffs.MarketShout, Motion = SkillMotion.Buff, SpCost = 8f,
            });

            // ---------------------------------------------------------------- Runesmith (Blacksmith)
            Register(new SkillDefinition
            {
                Id = "weaponry_research", Name = "Weaponry Research", Job = JobId.Runesmith, MaxLevel = 10, Passive = true,
                Description = "Knowing steel from the inside: +2 ATK and +2 HIT per level, and better forging.",
                IconLabel = "WRS", IconColorHex = "#6E2C00",
                PassivePerLevel = new StatModifiers { Atk = 2, Hit = 2 },
            });
            Register(new SkillDefinition
            {
                Id = "rune_forging", Name = "Rune Forging", Job = JobId.Runesmith, MaxLevel = 3, Requires = Req("weaponry_research", 1),
                Description = "Ragnarok's Smith skills: forge weapons from ore and runes at a forge you carry in your head. Each level unlocks a weapon tier.",
                IconLabel = "FRG", IconColorHex = "#BA4A00",
                Target = SkillTarget.Self, Special = SkillSpecial.Craft, Craft = CraftKind.Forge, Motion = SkillMotion.Cast, SpCost = 5f,
            });
            Register(new SkillDefinition
            {
                Id = "skin_tempering", Name = "Forge-Tempered Skin", Job = JobId.Runesmith, MaxLevel = 5, Passive = true,
                Description = "Ragnarok's Skin Tempering: years at the anvil, +4 DEF and +1% Max HP per level.",
                IconLabel = "SKT", IconColorHex = "#A04000",
                PassivePerLevel = new StatModifiers { Def = 4, MaxHpPercent = 1f },
            });
            Register(new SkillDefinition
            {
                Id = "dwarfs_fervor", Name = "Dwarf's Fervor", Job = JobId.Runesmith, MaxLevel = 5, Requires = Req("weaponry_research", 3),
                Description = "Ragnarok's Adrenaline Rush: the rhythm of the dwarven forges, +10% ASPD.",
                IconLabel = "ADR", IconColorHex = "#BA4A00", Weapons = WeaponMask.Axes | WeaponMask.Mace,
                Target = SkillTarget.Self, BuffId = SkillBuffs.DwarfsFervor, Motion = SkillMotion.Buff,
                BuffDuration = L(30f, 30f), SpCost = L(17f, 3f),
            });
            Register(new SkillDefinition
            {
                Id = "true_edge", Name = "True Edge", Job = JobId.Runesmith, MaxLevel = 5, Requires = Req("weaponry_research", 2),
                Description = "Ragnarok's Weapon Perfection: every swing lands true, +4% physical damage.",
                IconLabel = "WPF", IconColorHex = "#839192",
                Target = SkillTarget.Self, BuffId = SkillBuffs.TrueEdge, Motion = SkillMotion.Buff,
                BuffDuration = L(10f, 10f), SpCost = L(18f, -2f),
            });
            Register(new SkillDefinition
            {
                Id = "hammer_rhythm", Name = "Hammer Rhythm", Job = JobId.Runesmith, MaxLevel = 5, Requires = Req("skin_tempering", 1),
                Description = "Ragnarok's Power-Thrust: strike like a hammer on hot iron, +5% physical damage per level.",
                IconLabel = "PTH", IconColorHex = "#A04000",
                Target = SkillTarget.Self, BuffId = SkillBuffs.HammerRhythm, Motion = SkillMotion.Buff,
                BuffDuration = L(20f, 20f), SpCost = L(18f, -2f),
            });
            Register(new SkillDefinition
            {
                Id = "full_swing", Name = "Full Swing", Job = JobId.Runesmith, MaxLevel = 5, Requires = Req("hammer_rhythm", 2, "true_edge", 3),
                Description = "Ragnarok's Maximize Power: nothing held back, +10% critical damage and +2 CRIT per level.",
                IconLabel = "MXP", IconColorHex = "#6E2C00",
                Target = SkillTarget.Self, BuffId = SkillBuffs.FullSwing, Motion = SkillMotion.Buff, BuffDuration = 30f, SpCost = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "hammerfall", Name = "Hammerfall", Job = JobId.Runesmith, MaxLevel = 5,
                Description = "Ragnarok's Hammer Fall: strike the ground and stun everything around the spot.",
                IconLabel = "HFL", IconColorHex = "#7E5109", Weapons = WeaponMask.Axes | WeaponMask.Mace,
                Target = SkillTarget.Ground, Area = SkillArea.AtGround, Motion = SkillMotion.Swing,
                Range = 2f, Radius = 2.5f, SpCost = 10f, AfterCastDelay = 0.5f, PoiseMultiplier = 2f,
                Status = StatusEffect.Stun, StatusChance = L(30f, 10f), StatusDuration = 3f,
            });

            // ---------------------------------------------------------------- Forgelord (Mastersmith)
            Register(new SkillDefinition
            {
                Id = "cart_termination", Name = "Brokkr's Cart Crush", Job = JobId.Forgelord, MaxLevel = 10, Requires = Req("cart_charge", 1, "hammer_rhythm", 3),
                Description = "Ragnarok's Cart Termination: drive the loaded cart through your foe. Paid in zeny, stronger with weight. Can stun.",
                IconLabel = "CTM", IconColorHex = "#4D2600",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Leap,
                RequiresPushcart = true, WeightPowerPerThousand = 25f, ZenyCost = L(600f, 100f),
                Range = 1.5f, Power = L(300f, 60f), SpCost = 15f, AfterCastDelay = 0.8f, PoiseMultiplier = 2.5f,
                Status = StatusEffect.Stun, StatusChance = L(5f, 2f), StatusDuration = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "molten_edge", Name = "Molten Edge", Job = JobId.Forgelord, MaxLevel = 10, Requires = Req("full_swing", 3),
                Description = "Ragnarok's Meltdown: steel at forge heat cuts through armour, ignoring 10% of DEF and more per level.",
                IconLabel = "MLT", IconColorHex = "#E74C3C",
                Target = SkillTarget.Self, BuffId = SkillBuffs.MoltenEdge, Motion = SkillMotion.Buff,
                BuffDuration = L(15f, 5f), SpCost = L(20f, 4f),
            });
            Register(new SkillDefinition
            {
                Id = "wheel_rush", Name = "Wheel Rush", Job = JobId.Forgelord, MaxLevel = 1, Requires = Req("cart_charge", 1),
                Description = "Ragnarok's Cart Boost: the cart at full tilt, +25% movement speed.",
                IconLabel = "CBT", IconColorHex = "#AF601A",
                Target = SkillTarget.Self, BuffId = SkillBuffs.WheelRush, Motion = SkillMotion.Buff, RequiresPushcart = true, SpCost = 20f,
            });
            Register(new SkillDefinition
            {
                Id = "overthrust", Name = "Overthrust", Job = JobId.Forgelord, MaxLevel = 5, Requires = Req("hammer_rhythm", 5),
                Description = "Ragnarok's Maximum Over Thrust: every blow at full dwarven strength, +20% physical damage per level.",
                IconLabel = "MOT", IconColorHex = "#4D2600",
                Target = SkillTarget.Self, BuffId = SkillBuffs.Overthrust, Motion = SkillMotion.Buff, BuffDuration = 30f, SpCost = L(15f, 5f),
                ZenyCost = 3000f,
            });

            // ---------------------------------------------------------------- Brewmaster (Alchemist)
            Register(new SkillDefinition
            {
                Id = "axe_mastery", Name = "Axe Mastery", Job = JobId.Brewmaster, MaxLevel = 10, Passive = true,
                Description = "+3 ATK per level with axes.",
                IconLabel = "AXM", IconColorHex = "#117A65",
                PassivePerLevel = new StatModifiers { Atk = 3 }, PassiveWeapons = WeaponMask.Axes,
            });
            Register(new SkillDefinition
            {
                Id = "potion_research", Name = "Potion Research", Job = JobId.Brewmaster, MaxLevel = 10, Passive = true,
                Description = "Ragnarok's Potion Research: better brews, and +2 HP and SP regen per level.",
                IconLabel = "PRS", IconColorHex = "#1ABC9C",
                PassivePerLevel = new StatModifiers { HpRegenFlat = 2, SpRegenFlat = 1 },
            });
            Register(new SkillDefinition
            {
                Id = "brewing", Name = "Brewing", Job = JobId.Brewmaster, MaxLevel = 10, Requires = Req("potion_research", 5),
                Description = "Ragnarok's Pharmacy: brew tonics, sap and bombs from herbs. Higher levels brew more surely.",
                IconLabel = "PHA", IconColorHex = "#0E6655",
                Target = SkillTarget.Self, Special = SkillSpecial.Craft, Craft = CraftKind.Brew, Motion = SkillMotion.Cast, SpCost = 5f,
            });
            Register(new SkillDefinition
            {
                Id = "acid_flask", Name = "Acid Flask", Job = JobId.Brewmaster, MaxLevel = 5, Requires = Req("brewing", 5),
                Description = "Ragnarok's Acid Terror: a flask of acid that eats through armour (ignores DEF). Can cause bleeding.",
                IconLabel = "ACT", IconColorHex = "#58D68D",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true, IgnoreDefense = true,
                UseWeaponElement = false, Range = 9f, Power = L(140f, 40f), CastTime = 1f, SpCost = 15f,
                Status = StatusEffect.Bleeding, StatusChance = L(5f, 3f), StatusDuration = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "firebomb", Name = "Firebomb", Job = JobId.Brewmaster, MaxLevel = 5, Requires = Req("brewing", 4),
                Description = "Ragnarok's Bomb (Demonstration): a fire bottle that burns the ground for a while.",
                IconLabel = "DMS", IconColorHex = "#E67E22",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot,
                Element = Element.Fire, UseWeaponElement = false, NeverMiss = true,
                Range = 9f, Radius = 1.5f, Power = L(60f, 20f), ZoneDuration = L(10f, 2f), ZoneTick = 0.8f, CastTime = 1f, SpCost = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "mandrake_patch", Name = "Mandrake Patch", Job = JobId.Brewmaster, MaxLevel = 5, Requires = Req("brewing", 6),
                Description = "Ragnarok's Summon Flora: plant a shrieking mandrake that lashes out at foes that come near.",
                IconLabel = "SFL", IconColorHex = "#229954",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, Damage = SkillDamage.Physical, Motion = SkillMotion.Cast,
                Element = Element.Earth, UseWeaponElement = false,
                Range = 3f, Radius = 2.5f, Power = L(80f, 20f), ZoneDuration = L(30f, 10f), ZoneTick = 1.5f, CastTime = 2f, SpCost = 20f,
            });
            Register(new SkillDefinition
            {
                Id = "potion_toss", Name = "Potion Toss", Job = JobId.Brewmaster, MaxLevel = 5, Requires = Req("potion_research", 3),
                Description = "Ragnarok's Aid Potion: throw a healing draught to yourself or an ally.",
                IconLabel = "PPT", IconColorHex = "#E74C3C",
                Target = SkillTarget.Friend, Special = SkillSpecial.Heal, Motion = SkillMotion.Shoot,
                Range = 9f, FlatHeal = L(300f, 250f), SpCost = 1f, AfterCastDelay = 0.5f,
            });
            Register(new SkillDefinition
            {
                Id = "hallowed_coating", Name = "Hallowed Coating", Job = JobId.Brewmaster, MaxLevel = 5, Requires = Req("brewing", 2),
                Description = "Ragnarok's Chemical Protection: a hardening varnish, +8% DEF and MDEF per level for you or an ally.",
                IconLabel = "CPC", IconColorHex = "#48C9B0",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.HallowedCoating, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = L(120f, 120f), CastTime = 1f, SpCost = 20f,
            });

            // ---------------------------------------------------------------- Lifeweaver (Biochemist)
            Register(new SkillDefinition
            {
                Id = "acid_bloom", Name = "Acid Bloom", Job = JobId.Lifeweaver, MaxLevel = 10, Requires = Req("acid_flask", 5, "firebomb", 5),
                Description = "Ragnarok's Acid Demonstration: acid and fire together, two hits that ignore DEF.",
                IconLabel = "ACD", IconColorHex = "#28B463",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true, IgnoreDefense = true,
                UseWeaponElement = false, Range = 9f, Power = L(150f, 40f), Hits = 2, HitInterval = 0.2f, CastTime = 1f, SpCost = 30f,
            });
            Register(new SkillDefinition
            {
                Id = "full_coating", Name = "Full Coating", Job = JobId.Lifeweaver, MaxLevel = 5, Requires = Req("hallowed_coating", 5),
                Description = "Ragnarok's Full Chemical Protection: armour, shield, weapon and helm all varnished at once.",
                IconLabel = "FCP", IconColorHex = "#1ABC9C",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.FullCoating, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = L(120f, 120f), CastTime = 2f, SpCost = 40f,
            });
            Register(new SkillDefinition
            {
                Id = "golden_apple_grove", Name = "Golden Apple Grove", Job = JobId.Lifeweaver, MaxLevel = 5, Requires = Req("mandrake_patch", 3),
                Description = "Iðunn's own orchard springs up: allies standing in it heal every second.",
                IconLabel = "GAG", IconColorHex = "#F4D03F",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, Motion = SkillMotion.Cast, ZoneAffectsAllies = true,
                Range = 9f, Radius = 3f, ZoneDuration = L(8f, 2f), ZoneTick = 1f, FlatHeal = L(150f, 100f), CastTime = 2f, SpCost = L(40f, 5f), Cooldown = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "bountiful_toss", Name = "Bountiful Toss", Job = JobId.Lifeweaver, MaxLevel = 10, Requires = Req("potion_toss", 5),
                Description = "Ragnarok's Slim Potion Pitcher: lighter, stronger draughts thrown farther.",
                IconLabel = "SPP", IconColorHex = "#F1948A",
                Target = SkillTarget.Friend, Special = SkillSpecial.Heal, Motion = SkillMotion.Shoot,
                Range = 12f, FlatHeal = L(800f, 300f), SpCost = 30f, AfterCastDelay = 1f,
            });
        }
    }
}
