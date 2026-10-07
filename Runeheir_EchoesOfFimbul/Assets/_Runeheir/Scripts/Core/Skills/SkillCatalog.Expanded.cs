using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    /// <summary>
    /// Phase 7, Ragnarok's expanded classes under Norse names: Wanderer (Super Novice), Glíma Fighter (Taekwon) → Sól Guardian
    /// (Star Gladiator) / Fylgja Caller (Soul Linker), Thunderer (Gunslinger), Nightraider (Ninja) and Freyja's Kin (Doram).
    /// </summary>
    public static partial class SkillCatalog
    {
        private static void RegisterExpandedJobs()
        {
            RegisterWanderer();
            RegisterGlimaLine();
            RegisterThunderer();
            RegisterNightraider();
            RegisterFreyjasKin();
        }

        // ---------------------------------------------------------------- Wanderer (Super Novice)
        private static void RegisterWanderer()
        {
            Register(new SkillDefinition
            {
                Id = "wanderers_luck", Name = "Wanderer's Luck", Job = JobId.Wanderer, MaxLevel = 10, Passive = true,
                Description = "The road favours those who never chose one: +1 LUK per level.",
                IconLabel = "WLK", IconColorHex = "#B7950B",
                PassivePerLevel = new StatModifiers().SetStat(StatType.Luk, 1),
            });
            Register(new SkillDefinition
            {
                Id = "many_roads", Name = "Many Roads", Job = JobId.Wanderer, MaxLevel = 10, Passive = true,
                Description = "A little of every path: +1% Max HP and Max SP per level.",
                IconLabel = "MRD", IconColorHex = "#D4AC0D",
                PassivePerLevel = new StatModifiers { MaxHpPercent = 1f, MaxSpPercent = 1f },
            });
            Register(new SkillDefinition
            {
                Id = "norns_favor", Name = "Norns' Favour", Job = JobId.Wanderer, MaxLevel = 5, Requires = Req("wanderers_luck", 3),
                Description = "Ragnarok's guardian angel of the Super Novice: the Norns watch over you, +10 ATK, +10 MATK and +1 CRIT per level.",
                IconLabel = "ANG", IconColorHex = "#F4D03F",
                Target = SkillTarget.Self, BuffId = SkillBuffs.NornsFavor, Motion = SkillMotion.Buff, BuffDuration = 60f, SpCost = L(20f, 5f),
            });
            Register(new SkillDefinition
            {
                Id = "last_stand", Name = "Last Stand", Job = JobId.Wanderer, MaxLevel = 1, Requires = Req("many_roads", 5),
                Description = "Ragnarok's Steel Body for the Super Novice: 60% less damage taken for 10 seconds, but slower.",
                IconLabel = "STB", IconColorHex = "#7F8C8D",
                Target = SkillTarget.Self, BuffId = SkillBuffs.LastStand, Motion = SkillMotion.Buff, SpCost = 50f, Cooldown = 120f,
            });
        }

        // ---------------------------------------------------------------- Glíma Fighter (Taekwon), Sól Guardian, Fylgja Caller
        private static void RegisterGlimaLine()
        {
            const WeaponMask bare = WeaponMask.Unarmed;
            Register(new SkillDefinition
            {
                Id = "whirlwind_kick", Name = "Whirlwind Kick", Job = JobId.GlimaFighter, MaxLevel = 7,
                Description = "Ragnarok's Tornado Kick: a spinning kick that hits everyone around you.",
                IconLabel = "TKK", IconColorHex = "#C0392B", Weapons = bare,
                Target = SkillTarget.Self, Damage = SkillDamage.Physical, Area = SkillArea.AroundSelf, Motion = SkillMotion.Spin,
                Radius = 2.5f, Power = L(160f, 20f), SpCost = L(12f, 2f), AfterCastDelay = 0.4f,
            });
            Register(new SkillDefinition
            {
                Id = "heel_drop", Name = "Heel Drop", Job = JobId.GlimaFighter, MaxLevel = 7,
                Description = "Ragnarok's Axe Kick: the heel comes down like an axe. Can stun.",
                IconLabel = "HDK", IconColorHex = "#A93226", Weapons = bare,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Punch,
                Range = 1.3f, Power = L(160f, 20f), SpCost = L(12f, 2f), PoiseMultiplier = 1.5f,
                Status = StatusEffect.Stun, StatusChance = L(10f, 3f), StatusDuration = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "roundhouse", Name = "Roundhouse", Job = JobId.GlimaFighter, MaxLevel = 7,
                Description = "Ragnarok's Roundhouse Kick: a wide kick that knocks the target back.",
                IconLabel = "RHK", IconColorHex = "#E74C3C", Weapons = bare,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Punch,
                Range = 1.3f, Power = L(190f, 30f), Knockback = 2f, SpCost = L(14f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "glima_throw", Name = "Glíma Throw", Job = JobId.GlimaFighter, MaxLevel = 7, Requires = Req("roundhouse", 1, "heel_drop", 1),
                Description = "Ragnarok's Counter Kick, as Norse wrestling: catch the foe and throw it down. Never misses.",
                IconLabel = "CKK", IconColorHex = "#922B21", Weapons = bare,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Punch, NeverMiss = true,
                Range = 1.2f, Power = L(220f, 30f), SpCost = L(16f, 2f), Cooldown = 1f, PoiseMultiplier = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "sprint", Name = "Sprint", Job = JobId.GlimaFighter, MaxLevel = 10,
                Description = "Ragnarok's Running: break into a sprint, +20% movement speed and more per level.",
                IconLabel = "RUN", IconColorHex = "#E74C3C",
                Target = SkillTarget.Self, BuffId = SkillBuffs.Sprint, Motion = SkillMotion.Buff, BuffDuration = L(10f, 2f), SpCost = 10f, Cooldown = 5f,
            });
            Register(new SkillDefinition
            {
                Id = "leaping_kick", Name = "Leaping Kick", Job = JobId.GlimaFighter, MaxLevel = 7, Requires = Req("sprint", 7),
                Description = "Ragnarok's Flying Side Kick: close the distance in one leap.",
                IconLabel = "FSK", IconColorHex = "#CB4335", Weapons = bare,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Special = SkillSpecial.Dash, Motion = SkillMotion.Leap,
                Range = L(4f, 1f), Power = L(130f, 10f), SpCost = 20f, Cooldown = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "tumbling", Name = "Tumbling", Job = JobId.GlimaFighter, MaxLevel = 5, Passive = true,
                Description = "Roll with the blows: +4 FLEE per level.",
                IconLabel = "TMB", IconColorHex = "#F1948A",
                PassivePerLevel = new StatModifiers { Flee = 4 },
            });
            Register(new SkillDefinition
            {
                Id = "warriors_rest", Name = "Warrior's Rest", Job = JobId.GlimaFighter, MaxLevel = 10, Passive = true,
                Description = "Ragnarok's Peaceful and Happy Break: +4 HP and +2 SP regen per level.",
                IconLabel = "PBK", IconColorHex = "#82E0AA",
                PassivePerLevel = new StatModifiers { HpRegenFlat = 4, SpRegenFlat = 2 },
            });

            // ---- Sól Guardian (Star Gladiator): Sól, Máni and the stars
            Warmth("sols_warmth", "Sól's Warmth", "Ragnarok's Warmth of the Sun: Sól's fire burns everyone around you for a few seconds.",
                "WSN", "#F39C12", Element.Fire);
            Warmth("manis_chill", "Máni's Chill", "Ragnarok's Warmth of the Moon: Máni's cold light sears everyone around you.",
                "WMN", "#AED6F1", Element.Water);
            Warmth("stars_glow", "Glow of the Stars", "Ragnarok's Warmth of the Stars: starlight burns everyone around you.",
                "WST", "#F4F6F7", Element.Holy);
            Register(new SkillDefinition
            {
                Id = "sun_kick", Name = "Sun Kick", Job = JobId.SolGuardian, MaxLevel = 5, Requires = Req("heel_drop", 5),
                Description = "Ragnarok's Solar Kick: a kick wreathed in Sól's fire.",
                IconLabel = "SUK", IconColorHex = "#F5B041", Weapons = bare,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Punch, Element = Element.Fire, UseWeaponElement = false,
                Range = 1.3f, Power = L(200f, 40f), SpCost = L(15f, 3f), Cooldown = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "moon_kick", Name = "Moon Kick", Job = JobId.SolGuardian, MaxLevel = 5, Requires = Req("whirlwind_kick", 5),
                Description = "Ragnarok's Lunar Kick: a cold crescent kick around you.",
                IconLabel = "MNK", IconColorHex = "#85C1E9", Weapons = bare,
                Target = SkillTarget.Self, Damage = SkillDamage.Physical, Area = SkillArea.AroundSelf, Motion = SkillMotion.Spin,
                Element = Element.Water, UseWeaponElement = false, Radius = 2.5f, Power = L(150f, 30f), SpCost = L(18f, 3f), Cooldown = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "star_kick", Name = "Star Kick", Job = JobId.SolGuardian, MaxLevel = 5, Requires = Req("roundhouse", 5),
                Description = "Ragnarok's Stellar Kick: a falling-star kick. Can stun.",
                IconLabel = "STK", IconColorHex = "#FDFEFE", Weapons = bare,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Leap,
                Range = 1.3f, Power = L(180f, 30f), SpCost = L(16f, 3f), Cooldown = 1f,
                Status = StatusEffect.Stun, StatusChance = L(20f, 5f), StatusDuration = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "comfort_of_stars", Name = "Comfort of the Stars", Job = JobId.SolGuardian, MaxLevel = 5,
                Description = "Ragnarok's Comfort of the Stars: +3% ASPD and +3 HIT per level.",
                IconLabel = "COS", IconColorHex = "#F4F6F7",
                Target = SkillTarget.Self, BuffId = SkillBuffs.ComfortOfStars, Motion = SkillMotion.Buff, BuffDuration = 60f, SpCost = L(20f, 5f),
            });
            Register(new SkillDefinition
            {
                Id = "sols_protection", Name = "Sól's Protection", Job = JobId.SolGuardian, MaxLevel = 5,
                Description = "Ragnarok's Solar Protection: +6% DEF per level.",
                IconLabel = "SPR", IconColorHex = "#F8C471",
                Target = SkillTarget.Self, BuffId = SkillBuffs.SolsProtection, Motion = SkillMotion.Buff, BuffDuration = 60f, SpCost = L(20f, 5f),
            });
            Register(new SkillDefinition
            {
                Id = "union", Name = "Union of Sun, Moon and Stars", Job = JobId.SolGuardian, MaxLevel = 1,
                Requires = Req("comfort_of_stars", 3, "sols_protection", 3),
                Description = "Ragnarok's Union (Fusion): become one with Sól, Máni and the stars. +20% damage and ASPD, hyper armour. Costs 10% HP.",
                IconLabel = "FUS", IconColorHex = "#F5B041",
                Target = SkillTarget.Self, BuffId = SkillBuffs.Union, Motion = SkillMotion.Buff, HpCostPercent = 10f, SpCost = 100f, Cooldown = 60f,
            });

            // ---- Fylgja Caller (Soul Linker): the fylgjur, guardian spirits
            Register(new SkillDefinition
            {
                Id = "fylgja_strike", Name = "Fylgja Strike", Job = JobId.FylgjaCaller, MaxLevel = 10,
                Description = "Ragnarok's Esma: your fylgja strikes once per level.",
                IconLabel = "ESM", IconColorHex = "#2E86C1",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, Projectile = true, UseWeaponElement = false,
                Range = 9f, Power = 80f, Hits = L(1f, 1f), HitInterval = 0.12f, CastTime = L(0.5f, 0.3f), SpCost = L(10f, 3f), AfterCastDelay = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "spirit_push", Name = "Spirit Push", Job = JobId.FylgjaCaller, MaxLevel = 7,
                Description = "Ragnarok's Estin: a spirit's shove that knocks the target back.",
                IconLabel = "EST", IconColorHex = "#5DADE2",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, UseWeaponElement = false,
                Range = 9f, Power = L(100f, 20f), Knockback = 2f, CastTime = 0.5f, SpCost = 18f,
            });
            Register(new SkillDefinition
            {
                Id = "spirit_stun", Name = "Spirit Stun", Job = JobId.FylgjaCaller, MaxLevel = 7,
                Description = "Ragnarok's Estun: a spirit's blow to the head. Can stun.",
                IconLabel = "ETN", IconColorHex = "#21618C",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, UseWeaponElement = false,
                Range = 9f, Power = L(80f, 10f), CastTime = 0.5f, SpCost = 18f,
                Status = StatusEffect.Stun, StatusChance = L(25f, 5f), StatusDuration = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "hamingja", Name = "Hamingja", Job = JobId.FylgjaCaller, MaxLevel = 7, Requires = Req("fylgjas_mend", 3),
                Description = "Ragnarok's Kaizel, as the Norse luck-spirit: it stands guard over you or an ally, absorbing the next 5,000 damage.",
                IconLabel = "KZL", IconColorHex = "#AED6F1",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.Hamingja, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = L(120f, 30f), CastTime = 3f, SpCost = 120f,
            });
            Register(new SkillDefinition
            {
                Id = "fylgjas_mend", Name = "Fylgja's Mend", Job = JobId.FylgjaCaller, MaxLevel = 7,
                Description = "Ragnarok's Kaahi: a spirit tends your wounds, +8 HP regen per level for you or an ally.",
                IconLabel = "KAH", IconColorHex = "#82E0AA",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.FylgjasMend, Motion = SkillMotion.Cast, Range = 9f, BuffDuration = 120f, SpCost = 30f,
            });
            Register(new SkillDefinition
            {
                Id = "spirit_dodge", Name = "Spirit Dodge", Job = JobId.FylgjaCaller, MaxLevel = 3,
                Description = "Ragnarok's Kaupe: a spirit tugs you out of harm's way, +8 FLEE per level for you or an ally.",
                IconLabel = "KAU", IconColorHex = "#A9CCE3",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.SpiritDodge, Motion = SkillMotion.Cast, Range = 9f, BuffDuration = 60f, SpCost = 20f,
            });
            Register(new SkillDefinition
            {
                Id = "spirit_mirror", Name = "Spirit Mirror", Job = JobId.FylgjaCaller, MaxLevel = 7, Requires = Req("spirit_dodge", 1),
                Description = "Ragnarok's Kaite: a spirit-mirror reflects 10% of spells per level.",
                IconLabel = "KAI", IconColorHex = "#D2B4DE",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.SpiritMirror, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = 60f, CastTime = 2f, SpCost = 70f,
            });
            Register(new SkillDefinition
            {
                Id = "fylgja_bond", Name = "Fylgja Bond", Job = JobId.FylgjaCaller, MaxLevel = 5, Requires = Req("fylgjas_mend", 1),
                Description = "Ragnarok's Spirit Links: bind a guardian spirit to an ally, +5 to every stat and +5% damage.",
                IconLabel = "LNK", IconColorHex = "#5499C7",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.FylgjaBond, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = L(60f, 30f), CastTime = 1f, SpCost = 50f,
            });
        }

        /// <summary>A Sól Guardian warmth: five pulses of an element around you.</summary>
        private static void Warmth(string id, string name, string description, string icon, string color, Element element)
        {
            Register(new SkillDefinition
            {
                Id = id, Name = name, Job = JobId.SolGuardian, MaxLevel = 5, Requires = Req("whirlwind_kick", 3),
                Description = description, IconLabel = icon, IconColorHex = color, Weapons = WeaponMask.Unarmed,
                Target = SkillTarget.Self, Damage = SkillDamage.Physical, Area = SkillArea.AroundSelf, Motion = SkillMotion.Cast,
                Element = element, UseWeaponElement = false, NeverMiss = true,
                Radius = 3f, Power = L(40f, 10f), Hits = 5, HitInterval = 1f, CastTime = 1f, SpCost = L(60f, 10f), Cooldown = 20f,
            });
        }

        // ---------------------------------------------------------------- Thunderer (Gunslinger)
        private static void RegisterThunderer()
        {
            const WeaponMask rod = WeaponMask.ThunderRod;
            Register(new SkillDefinition
            {
                Id = "thors_coin", Name = "Thor's Coin", Job = JobId.Thunderer, MaxLevel = 10,
                Description = "Ragnarok's Flip the Coin: flip one of Thor's coins into your pouch (up to one per level, 10 at most). +2 HIT each.",
                IconLabel = "COI", IconColorHex = "#F7DC6F",
                Target = SkillTarget.Self, BuffId = SkillBuffs.ThorsCoins, Motion = SkillMotion.Buff, CastTime = 0.5f, SpCost = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "snake_eye", Name = "Snake Eye", Job = JobId.Thunderer, MaxLevel = 10, Passive = true,
                Description = "+1 HIT and +0.3 m of reach per level with thunder-rods.",
                IconLabel = "SNE", IconColorHex = "#7F8C8D",
                PassivePerLevel = new StatModifiers { Hit = 1, AttackRange = 0.3f }, PassiveWeapons = rod,
            });
            Register(new SkillDefinition
            {
                Id = "triple_thunder", Name = "Triple Thunder", Job = JobId.Thunderer, MaxLevel = 1, Requires = Req("thors_coin", 1),
                Description = "Ragnarok's Triple Action: three cracks in a blink. Spends 1 coin.",
                IconLabel = "TRA", IconColorHex = "#D4AC0D", Weapons = rod,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true,
                Range = 8f, Power = 150f, Hits = 3, HitInterval = 0.1f, SphereCost = 1, SphereBuffId = SkillBuffs.ThorsCoins, SpCost = 20f, AfterCastDelay = 0.5f,
            });
            Register(new SkillDefinition
            {
                Id = "thors_eye", Name = "Thor's Eye", Job = JobId.Thunderer, MaxLevel = 1, Requires = Req("thors_coin", 5),
                Description = "Ragnarok's Bull's Eye: a shot straight through the heart. Spends 1 coin. Can stun.",
                IconLabel = "BUE", IconColorHex = "#B7950B", Weapons = rod,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true,
                Range = 8f, Power = 500f, CastTime = 0.5f, SphereCost = 1, SphereBuffId = SkillBuffs.ThorsCoins, SpCost = 30f, Cooldown = 1f,
                Status = StatusEffect.Stun, StatusChance = 10f, StatusDuration = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "rapid_thunder", Name = "Rapid Thunder", Job = JobId.Thunderer, MaxLevel = 10,
                Description = "Ragnarok's Rapid Shower: five shots fanned off the hip.",
                IconLabel = "RSH", IconColorHex = "#F39C12", Weapons = rod,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot,
                Range = 8f, Power = L(50f, 10f), Hits = 5, HitInterval = 0.06f, SpCost = 22f, Cooldown = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "storm_spin", Name = "Storm Spin", Job = JobId.Thunderer, MaxLevel = 10, Requires = Req("rapid_thunder", 3),
                Description = "Ragnarok's Desperado: spin and fire everywhere at once. Spends 1 coin.",
                IconLabel = "DSP", IconColorHex = "#E67E22", Weapons = rod,
                Target = SkillTarget.Self, Damage = SkillDamage.Physical, Area = SkillArea.AroundSelf, Motion = SkillMotion.Spin,
                Radius = 3.5f, Power = L(50f, 5f), Hits = 10, HitInterval = 0.1f, SphereCost = 1, SphereBuffId = SkillBuffs.ThorsCoins,
                SpCost = 32f, Cooldown = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "tracking", Name = "Tracking", Job = JobId.Thunderer, MaxLevel = 10, Requires = Req("snake_eye", 3),
                Description = "Take your time and follow the mark: a long-aimed shot that never misses.",
                IconLabel = "TRK", IconColorHex = "#566573", Weapons = rod,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true, NeverMiss = true,
                Range = 12f, Power = L(200f, 100f), CastTime = L(1.2f, 0.2f), SpCost = L(15f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "disarm", Name = "Disarm", Job = JobId.Thunderer, MaxLevel = 5, Requires = Req("tracking", 2),
                Description = "Ragnarok's Disarm: shoot the weapon out of a foe's grip.",
                IconLabel = "DSA", IconColorHex = "#7B241C", Weapons = rod,
                Target = SkillTarget.Enemy, Motion = SkillMotion.Shoot, Range = 8f, CastTime = 2f, SpCost = 15f,
                DebuffId = SkillBuffs.Disarmed, DebuffDuration = L(15f, 5f), DebuffChance = L(40f, 10f),
            });
            Register(new SkillDefinition
            {
                Id = "rending_shot", Name = "Rending Shot", Job = JobId.Thunderer, MaxLevel = 5, Requires = Req("tracking", 5),
                Description = "Ragnarok's Piercing Shot: a round that goes through armour. Can cause bleeding.",
                IconLabel = "PSH", IconColorHex = "#922B21", Weapons = rod,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true, IgnoreDefense = true,
                Range = 8f, Power = L(120f, 20f), CastTime = 1.5f, SpCost = L(11f, 1f),
                Status = StatusEffect.Bleeding, StatusChance = L(3f, 3f), StatusDuration = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "spread_shot", Name = "Spread Shot", Job = JobId.Thunderer, MaxLevel = 10, Requires = Req("rapid_thunder", 2),
                Description = "Ragnarok's Spread Attack: a burst of shot over the target and everyone near it.",
                IconLabel = "SPA", IconColorHex = "#DC7633", Weapons = rod,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Area = SkillArea.AroundTarget, Motion = SkillMotion.Shoot,
                Range = 8f, Radius = L(2f, 0.3f), Power = L(80f, 20f), CastTime = 1f, SpCost = 15f,
            });
            Register(new SkillDefinition
            {
                Id = "dust_blast", Name = "Dust Blast", Job = JobId.Thunderer, MaxLevel = 10,
                Description = "Ragnarok's Dust: a blast that throws the target back.",
                IconLabel = "DST", IconColorHex = "#A6ACAF", Weapons = rod,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot,
                Range = 8f, Power = L(100f, 50f), Knockback = L(1.5f, 0.25f), SpCost = 3f,
            });
            Register(new SkillDefinition
            {
                Id = "thunder_fever", Name = "Thunder Fever", Job = JobId.Thunderer, MaxLevel = 10, Requires = Req("rapid_thunder", 7),
                Description = "Ragnarok's Gatling Fever: a storm of fire, +20% ASPD and +10 ATK per level, but slow on your feet. Spends 1 coin.",
                IconLabel = "GAT", IconColorHex = "#F39C12", Weapons = rod,
                Target = SkillTarget.Self, BuffId = SkillBuffs.ThunderFever, Motion = SkillMotion.Buff, BuffDuration = 30f,
                SphereCost = 1, SphereBuffId = SkillBuffs.ThorsCoins, SpCost = L(30f, 5f), Cooldown = 5f,
            });
            Register(new SkillDefinition
            {
                Id = "steady_aim", Name = "Steady Aim", Job = JobId.Thunderer, MaxLevel = 1, Requires = Req("snake_eye", 5),
                Description = "Ragnarok's Increasing Accuracy: +20 HIT, +4 DEX and +4 AGI. Spends 4 coins.",
                IconLabel = "INC", IconColorHex = "#58D68D",
                Target = SkillTarget.Self, BuffId = SkillBuffs.SteadyAim, Motion = SkillMotion.Buff,
                SphereCost = 4, SphereBuffId = SkillBuffs.ThorsCoins, SpCost = 30f,
            });
            Register(new SkillDefinition
            {
                Id = "mjolnirs_stillness", Name = "Mjölnir's Stillness", Job = JobId.Thunderer, MaxLevel = 1, Requires = Req("steady_aim", 1),
                Description = "Ragnarok's Madness Canceller: plant yourself like Thor's hammer, +100 ATK and +20% ASPD, barely moving. Spends 4 coins.",
                IconLabel = "MAD", IconColorHex = "#D4AC0D", Weapons = rod,
                Target = SkillTarget.Self, BuffId = SkillBuffs.MjolnirsStillness, Motion = SkillMotion.Buff,
                SphereCost = 4, SphereBuffId = SkillBuffs.ThorsCoins, SpCost = 30f, Cooldown = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "weather_eye", Name = "Weather-Eye", Job = JobId.Thunderer, MaxLevel = 1, Requires = Req("thors_coin", 3),
                Description = "Ragnarok's Adjustment: watch the storm and sidestep it, +30 FLEE but -30 HIT. Spends 2 coins.",
                IconLabel = "ADJ", IconColorHex = "#ABB2B9",
                Target = SkillTarget.Self, BuffId = SkillBuffs.WeatherEye, Motion = SkillMotion.Buff,
                SphereCost = 2, SphereBuffId = SkillBuffs.ThorsCoins, SpCost = 15f,
            });
        }

        // ---------------------------------------------------------------- Nightraider (Ninja)
        private static void RegisterNightraider()
        {
            Register(new SkillDefinition
            {
                Id = "night_training", Name = "Night Training", Job = JobId.Nightraider, MaxLevel = 10, Passive = true,
                Description = "Ragnarok's Ninja Mastery: +3 SP regen and +2 FLEE per level.",
                IconLabel = "NMS", IconColorHex = "#212F3D",
                PassivePerLevel = new StatModifiers { SpRegenFlat = 3, Flee = 2 },
            });
            Register(new SkillDefinition
            {
                Id = "throw_shuriken", Name = "Throw Shuriken", Job = JobId.Nightraider, MaxLevel = 10,
                Description = "Ragnarok's Throw Shuriken: a quick star of iron.",
                IconLabel = "TSH", IconColorHex = "#566573",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true,
                Range = 9f, Power = L(110f, 10f), SpCost = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "throw_kunai", Name = "Throw Kunai", Job = JobId.Nightraider, MaxLevel = 5, Requires = Req("throw_shuriken", 5),
                Description = "Ragnarok's Throw Kunai: three knives at once.",
                IconLabel = "TKN", IconColorHex = "#34495E",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true,
                Range = 9f, Power = L(100f, 10f), Hits = 3, HitInterval = 0.08f, SpCost = 3f,
            });
            Register(new SkillDefinition
            {
                Id = "huuma_storm", Name = "Huuma Storm", Job = JobId.Nightraider, MaxLevel = 5, Requires = Req("throw_kunai", 5),
                Description = "Ragnarok's Throw Huuma Shuriken: the great shuriken whirls through the target and everyone near it.",
                IconLabel = "THS", IconColorHex = "#1C2833", Weapons = WeaponMask.Huuma,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Area = SkillArea.AroundTarget, Motion = SkillMotion.Shoot,
                Range = 9f, Radius = 2f, Power = L(150f, 150f), CastTime = L(1f, 0.25f), SpCost = L(15f, 3f), Cooldown = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "hurl_gold", Name = "Hurl Gold", Job = JobId.Nightraider, MaxLevel = 10, Requires = Req("throw_shuriken", 3),
                Description = "Ragnarok's Throw Zeny: hurl a fistful of gold. It never misses and ignores armour, but it costs zeny.",
                IconLabel = "TZN", IconColorHex = "#F1C40F",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, NeverMiss = true, IgnoreDefense = true,
                Range = 9f, Power = L(300f, 100f), ZenyCost = L(500f, 500f), SpCost = 50f, Cooldown = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "shield_flip", Name = "Shield Flip", Job = JobId.Nightraider, MaxLevel = 5,
                Description = "Ragnarok's Flip Tatami, with a Viking round shield: flip it up and slam everyone around you back.",
                IconLabel = "FTM", IconColorHex = "#7E5109",
                Target = SkillTarget.Self, Damage = SkillDamage.Physical, Area = SkillArea.AroundSelf, Motion = SkillMotion.Spin,
                Radius = 2.5f, Power = 120f, Knockback = 2f, SpCost = 15f, Cooldown = 3f,
            });
            Register(new SkillDefinition
            {
                Id = "mist_slash", Name = "Mist Slash", Job = JobId.Nightraider, MaxLevel = 10, Requires = Req("shield_flip", 1),
                Description = "Ragnarok's Haze Slasher: cut through the fog around you.",
                IconLabel = "HZS", IconColorHex = "#AAB7B8",
                Target = SkillTarget.Self, Damage = SkillDamage.Physical, Area = SkillArea.AroundSelf, Motion = SkillMotion.Spin,
                Radius = 2f, Power = L(110f, 10f), SpCost = L(17f, 1f),
            });
            Register(new SkillDefinition
            {
                Id = "shadow_leap", Name = "Shadow Leap", Job = JobId.Nightraider, MaxLevel = 5, Requires = Req("mist_slash", 3),
                Description = "Ragnarok's Shadow Leap: step through the shadows to a spot.",
                IconLabel = "SLP", IconColorHex = "#17202A",
                Target = SkillTarget.Ground, Special = SkillSpecial.Dash, Motion = SkillMotion.Leap, Range = L(4f, 1f), SpCost = 20f, Cooldown = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "shadow_slash", Name = "Shadow Slash", Job = JobId.Nightraider, MaxLevel = 5, Requires = Req("shadow_leap", 1),
                Description = "Ragnarok's Shadow Slash: a cut from the dark that can strike critically.",
                IconLabel = "SSL", IconColorHex = "#1B2631",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Swing, CanCrit = true,
                Range = 1.3f, Power = L(110f, 30f), SpCost = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "shed_skin", Name = "Shed Skin", Job = JobId.Nightraider, MaxLevel = 5,
                Description = "Ragnarok's Cicada Skin Shedding: leave a husk behind, blocking the next melee hits (one per level).",
                IconLabel = "CIC", IconColorHex = "#A3E4D7",
                Target = SkillTarget.Self, BuffId = SkillBuffs.ShedSkin, Motion = SkillMotion.Buff, BuffCharges = L(1f, 1f), SpCost = L(12f, 3f),
            });
            Register(new SkillDefinition
            {
                Id = "mirror_image", Name = "Mirror Image", Job = JobId.Nightraider, MaxLevel = 5, Requires = Req("shed_skin", 1),
                Description = "Ragnarok's Mirror Image: shadow copies take blows meant for you, +10% block chance per level.",
                IconLabel = "MIR", IconColorHex = "#D6EAF8",
                Target = SkillTarget.Self, BuffId = SkillBuffs.MirrorImage, Motion = SkillMotion.Buff, SpCost = L(10f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "muspel_blossom", Name = "Muspel Blossom", Job = JobId.Nightraider, MaxLevel = 10, Requires = Req("night_training", 5),
                Description = "Ragnarok's Crimson Fire Formation: a flower of fire opens around you.",
                IconLabel = "CFB", IconColorHex = "#E74C3C",
                Target = SkillTarget.Self, Damage = SkillDamage.Magic, Area = SkillArea.AroundSelf, Motion = SkillMotion.Cast,
                Element = Element.Fire, UseWeaponElement = false, Radius = 3f, Power = L(100f, 40f), CastTime = 1.5f, SpCost = L(15f, 3f),
            });
            Register(new SkillDefinition
            {
                Id = "thunder_jolt", Name = "Thunder Jolt", Job = JobId.Nightraider, MaxLevel = 10, Requires = Req("night_training", 5),
                Description = "Ragnarok's Lightning Jolt: lightning strikes the ground where you point.",
                IconLabel = "LJT", IconColorHex = "#F7DC6F",
                Target = SkillTarget.Ground, Damage = SkillDamage.Magic, Area = SkillArea.AtGround, Motion = SkillMotion.Cast,
                Element = Element.Wind, UseWeaponElement = false, Range = 9f, Radius = 2.5f, Power = L(160f, 40f), CastTime = 2f, SpCost = 30f,
            });
            Register(new SkillDefinition
            {
                Id = "hail_of_niflheim", Name = "Hail of Niflheim", Job = JobId.Nightraider, MaxLevel = 10, Requires = Req("night_training", 5),
                Description = "Ragnarok's Lightning Spear of Ice: one shard of Niflheim's ice per level.",
                IconLabel = "LSI", IconColorHex = "#85C1E9",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, Projectile = true,
                Element = Element.Water, UseWeaponElement = false,
                Range = 9f, Power = 100f, Hits = L(1f, 1f), HitInterval = 0.12f, CastTime = L(0.7f, 0.3f), SpCost = L(15f, 3f),
            });
        }

        // ---------------------------------------------------------------- Freyja's Kin (Doram)
        private static void RegisterFreyjasKin()
        {
            Register(new SkillDefinition
            {
                Id = "cats_bite", Name = "Cat's Bite", Job = JobId.FreyjasKin, MaxLevel = 5,
                Description = "Ragnarok's Bite: teeth, quick and sharp.",
                IconLabel = "BIT", IconColorHex = "#E59866",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Punch,
                Range = 1.3f, Power = L(120f, 30f), SpCost = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "bygul_claws", Name = "Bygul's Claws", Job = JobId.FreyjasKin, MaxLevel = 5, Requires = Req("cats_bite", 1),
                Description = "Ragnarok's Scar of Tarou, after Freyja's cat Bygul: raking claws that leave bleeding scars.",
                IconLabel = "SOT", IconColorHex = "#CA6F1E",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Swing,
                Range = 1.3f, Power = L(150f, 30f), SpCost = 14f,
                Status = StatusEffect.Bleeding, StatusChance = L(10f, 5f), StatusDuration = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "root_smash", Name = "Root Smash", Job = JobId.FreyjasKin, MaxLevel = 5, Requires = Req("bygul_claws", 1),
                Description = "Ragnarok's Lunatic Carrot Beat: a giant root crashes down on the target and everyone near it. Can stun.",
                IconLabel = "LCB", IconColorHex = "#D35400",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Area = SkillArea.AroundTarget, Motion = SkillMotion.Swing,
                Element = Element.Earth, UseWeaponElement = false,
                Range = 7f, Radius = 2f, Power = L(130f, 30f), CastTime = 1f, SpCost = 30f,
                Status = StatusEffect.Stun, StatusChance = L(5f, 2f), StatusDuration = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "pounce", Name = "Pounce", Job = JobId.FreyjasKin, MaxLevel = 5, Requires = Req("cats_bite", 3),
                Description = "Ragnarok's Arclouse Dash: a cat's pounce onto the target.",
                IconLabel = "ACD", IconColorHex = "#F0B27A",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Special = SkillSpecial.Dash, Motion = SkillMotion.Leap,
                Range = L(4f, 1f), Power = L(120f, 20f), SpCost = 12f, Cooldown = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "falcons_of_freyja", Name = "Falcons of Freyja", Job = JobId.FreyjasKin, MaxLevel = 5,
                Description = "Ragnarok's Picky Peck: falcons from Freyja's feather-cloak dive five times at the target.",
                IconLabel = "PPK", IconColorHex = "#AF7AC5",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Cast, Projectile = true,
                Element = Element.Wind, UseWeaponElement = false,
                Range = 9f, Power = L(60f, 10f), Hits = 5, HitInterval = 0.1f, CastTime = 1f, SpCost = 20f,
            });
            Register(new SkillDefinition
            {
                Id = "catnip_meteor", Name = "Catnip Meteor", Job = JobId.FreyjasKin, MaxLevel = 5, Requires = Req("falcons_of_freyja", 3),
                Description = "Ragnarok's Catnip Meteor: a shower of giant catnip falls on a spot, three times.",
                IconLabel = "CTM", IconColorHex = "#82E0AA",
                Target = SkillTarget.Ground, Damage = SkillDamage.Magic, Area = SkillArea.AtGround, Motion = SkillMotion.Cast,
                Element = Element.Earth, UseWeaponElement = false,
                Range = 9f, Radius = 3f, Power = L(150f, 30f), Hits = 3, HitInterval = 0.3f, CastTime = 2f, SpCost = 70f, Cooldown = 3f,
            });
            Register(new SkillDefinition
            {
                Id = "silvervine_spear", Name = "Silvervine Spear", Job = JobId.FreyjasKin, MaxLevel = 5, Requires = Req("root_smash", 1),
                Description = "Ragnarok's Silvervine Stem Spear: a spear of living vine.",
                IconLabel = "SSS", IconColorHex = "#58D68D",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, Projectile = true,
                Element = Element.Earth, UseWeaponElement = false, Range = 9f, Power = L(200f, 40f), CastTime = 1f, SpCost = 25f,
            });
            Register(new SkillDefinition
            {
                Id = "gift_of_njord", Name = "Gift of Njörðr", Job = JobId.FreyjasKin, MaxLevel = 5, Passive = true,
                Description = "Ragnarok's Power of Sea, from Freyja's father the sea god: +1% Max HP and SP and +2 HP regen per level.",
                IconLabel = "POS", IconColorHex = "#5DADE2",
                PassivePerLevel = new StatModifiers { MaxHpPercent = 1f, MaxSpPercent = 1f, HpRegenFlat = 2 },
            });
            Register(new SkillDefinition
            {
                Id = "spirit_of_life", Name = "Spirit of Life", Job = JobId.FreyjasKin, MaxLevel = 5, Passive = true, Requires = Req("gift_of_njord", 1),
                Description = "Ragnarok's Spirit of Life: the life in all things answers you, +2% magic damage per level.",
                IconLabel = "SOL", IconColorHex = "#ABEBC6",
                PassivePerLevel = new StatModifiers { MagicDamagePercent = 2f },
            });
            Register(new SkillDefinition
            {
                Id = "fish_feast", Name = "Fish Feast", Job = JobId.FreyjasKin, MaxLevel = 5, Requires = Req("gift_of_njord", 1),
                Description = "Ragnarok's Tuna Party: a feast for you or an ally that absorbs the next 2,500 damage.",
                IconLabel = "TUN", IconColorHex = "#5DADE2",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.FishFeast, Motion = SkillMotion.Cast, Range = 9f, BuffDuration = L(30f, 10f), SpCost = 20f,
            });
            Register(new SkillDefinition
            {
                Id = "purr_of_comfort", Name = "Purr of Comfort", Job = JobId.FreyjasKin, MaxLevel = 5,
                Description = "Ragnarok's Purring: a steady purr, +6 HP regen per level for you or an ally.",
                IconLabel = "PUR", IconColorHex = "#F5CBA7",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.PurrOfComfort, Motion = SkillMotion.Cast, Range = 9f, BuffDuration = 60f, SpCost = 15f,
            });
            Register(new SkillDefinition
            {
                Id = "hiss", Name = "Hiss", Job = JobId.FreyjasKin, MaxLevel = 5,
                Description = "Ragnarok's Hiss: fur on end, +10 FLEE per level.",
                IconLabel = "HIS", IconColorHex = "#E59866",
                Target = SkillTarget.Self, BuffId = SkillBuffs.Hiss, Motion = SkillMotion.Buff, BuffDuration = 30f, SpCost = 15f,
            });
            Register(new SkillDefinition
            {
                Id = "chattering", Name = "Chattering", Job = JobId.FreyjasKin, MaxLevel = 5, Requires = Req("hiss", 1),
                Description = "Ragnarok's Chattering: the hunting chatter of a cat at the window, +15 ATK and MATK per level.",
                IconLabel = "CHT", IconColorHex = "#DC7633",
                Target = SkillTarget.Self, BuffId = SkillBuffs.Chattering, Motion = SkillMotion.Buff, BuffDuration = 60f, SpCost = 20f,
            });
        }
    }
}
