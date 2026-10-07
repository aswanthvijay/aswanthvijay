using System;
using Runeheir.Combat;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    /// <summary>Phase 7: buffs, songs, dances and debuffs of the full roster.</summary>
    public static partial class SkillBuffs
    {
        // ---- Initiate, Scout, Outlaw, Vargr
        public const string FeignDeath = "feign_death";
        public const string HideInShadows = "hide_in_shadows";
        public const string Disarmed = "disarmed";
        public const string Shieldless = "shieldless";
        public const string Unarmored = "unarmored";
        public const string Unhelmed = "unhelmed";
        public const string WolfStripped = "wolf_stripped";
        public const string WolfsProwl = "wolfs_prowl";
        public const string PreserveCopy = "preserve_copy";
        public const string TurnTheBlade = "turn_the_blade";

        // ---- Huntsman line: songs and dances
        public const string HawkFocus = "hawk_focus";
        public const string Performing = "performing";
        public const string WhistleOfHeimdall = "whistle_of_heimdall";
        public const string SunsetLay = "sunset_lay";
        public const string BragisVerse = "bragis_verse";
        public const string IdunsApple = "iduns_apple";
        public const string Humming = "humming";
        public const string ForgetfulDance = "forgetful_dance";
        public const string FreyjasKiss = "freyjas_kiss";
        public const string SeidrService = "seidr_service";
        public const string HermodrsWard = "hermodrs_ward";
        public const string BalladOfValhalla = "ballad_of_valhalla";
        public const string SeidrCharm = "seidr_charm";
        public const string FriggsKiss = "friggs_kiss";

        // ---- Trader line
        public const string MarketShout = "market_shout";
        public const string DwarfsFervor = "dwarfs_fervor";
        public const string TrueEdge = "true_edge";
        public const string HammerRhythm = "hammer_rhythm";
        public const string FullSwing = "full_swing";
        public const string MoltenEdge = "molten_edge";
        public const string WheelRush = "wheel_rush";
        public const string Overthrust = "overthrust";
        public const string HallowedCoating = "hallowed_coating";
        public const string FullCoating = "full_coating";

        // ---- Devotee line
        public const string HeavyLimbs = "heavy_limbs";
        public const string GaldrOfEir = "galdr_of_eir";
        public const string GloryOfBaldr = "glory_of_baldr";
        public const string SvalinnsShield = "svalinns_shield";
        public const string TyrsHand = "tyrs_hand";
        public const string GaldrOfHaste = "galdr_of_haste";
        public const string NornsDoom = "norns_doom";
        public const string RampartOfRunar = "rampart_of_runar";
        public const string HofSanctum = "hof_sanctum";

        // ---- Warrior and Mystic lines
        public const string Riposte = "riposte";
        public const string ValhallasEdge = "valhallas_edge";
        public const string Parry = "parry";
        public const string Berserkergang = "berserkergang";
        public const string SeidrCoat = "seidr_coat";

        // ---- Expanded jobs
        public const string NornsFavor = "norns_favor";
        public const string LastStand = "last_stand";
        public const string Sprint = "sprint";
        public const string ComfortOfStars = "comfort_of_stars";
        public const string Union = "union";
        public const string SolsProtection = "sols_protection";
        public const string Hamingja = "hamingja";
        public const string FylgjasMend = "fylgjas_mend";
        public const string SpiritDodge = "spirit_dodge";
        public const string SpiritMirror = "spirit_mirror";
        public const string FylgjaBond = "fylgja_bond";
        public const string ThorsCoins = "thors_coins";
        public const string ThunderFever = "thunder_fever";
        public const string SteadyAim = "steady_aim";
        public const string MjolnirsStillness = "mjolnirs_stillness";
        public const string WeatherEye = "weather_eye";
        public const string ShedSkin = "shed_skin";
        public const string MirrorImage = "mirror_image";
        public const string Hiss = "hiss";
        public const string FishFeast = "fish_feast";
        public const string PurrOfComfort = "purr_of_comfort";
        public const string Chattering = "chattering";

        public const int MaxThorsCoins = 10;

        /// <summary>How long a song or dance lingers on someone after they step out of the performance.</summary>
        public const float PerformanceLinger = 3f;

        private static void RegisterRosterBuffs(Action<BuffDefinition> register)
        {
            // ---------------------------------------------------------------- Initiate, Scout, Outlaw, Vargr
            register(Buff(FeignDeath, "Feign Death", "FGN", "#7F8C8D", 10f, "Lie still: monsters lose track of you. Moving is slow; attacking ends it.",
                new StatModifiers { MoveSpeedPercent = -70f }, traits: BuffTraits.Stealth));
            register(Buff(HideInShadows, "Hide in Shadows", "HID", "#2C3E50", 30f, "Hidden in the shadows: monsters can't see you, but you barely move.",
                new StatModifiers { MoveSpeedPercent = -80f }, traits: BuffTraits.Stealth));
            register(Debuff(Disarmed, "Disarmed", "DSW", "#7B241C", 15f, "Weapon stripped: -10% physical damage per level of the strip.",
                new StatModifiers { PhysicalDamagePercent = -10f }, new StatModifiers { PhysicalDamagePercent = -4f }));
            register(Debuff(Shieldless, "Shieldless", "DSS", "#7E5109", 15f, "Shield stripped: -10% DEF, more per level.",
                new StatModifiers { DefPercent = -10f }, new StatModifiers { DefPercent = -4f }));
            register(Debuff(Unarmored, "Unarmored", "DSA", "#6E2C00", 15f, "Armour stripped: -15% DEF and -10% ASPD, more per level.",
                new StatModifiers { DefPercent = -15f, AspdPercent = -10f }, new StatModifiers { DefPercent = -3f }));
            register(Debuff(Unhelmed, "Unhelmed", "DSH", "#4A235A", 15f, "Helm stripped: -15% MDEF and -10 HIT, more per level.",
                new StatModifiers { MdefPercent = -15f, Hit = -10 }, new StatModifiers { MdefPercent = -3f }));
            register(Debuff(WolfStripped, "Wolf-Stripped", "FST", "#3B2F2F", 15f, "Everything torn away: less damage, DEF, MDEF and ASPD.",
                new StatModifiers { PhysicalDamagePercent = -15f, DefPercent = -20f, MdefPercent = -20f, AspdPercent = -10f },
                new StatModifiers { DefPercent = -2f, MdefPercent = -2f }));
            register(Buff(WolfsProwl, "Wolf's Prowl", "CWK", "#5D4037", 60f, "Hidden, yet moving at full stride; +10 ATK per level once it breaks.",
                new StatModifiers { Atk = 10 }, new StatModifiers { Atk = 10 }, traits: BuffTraits.Stealth));
            register(Buff(PreserveCopy, "Preserve", "PRS", "#8E44AD", 600f, "Loki's Mimicry keeps the skill it holds.", StatModifiers.Empty()));
            register(Buff(TurnTheBlade, "Turn the Blade", "RJS", "#566573", 30f, "Turns blades aside: 15% less damage taken and 10% melee reflected, more per level.",
                new StatModifiers { DamageTakenPercent = -15f, ReflectMeleePercent = 10f }, new StatModifiers { DamageTakenPercent = -2f, ReflectMeleePercent = 3f }));

            // ---------------------------------------------------------------- Huntsman line
            register(Buff(HawkFocus, "Hawk Focus", "HFC", "#6E8B3D", 60f, "+2 AGI and DEX per level.",
                new StatModifiers().SetStat(StatType.Agi, 2).SetStat(StatType.Dex, 2),
                new StatModifiers().SetStat(StatType.Agi, 2).SetStat(StatType.Dex, 2)));
            register(Buff(Performing, "Performing", "PRF", "#B7950B", 30f, "Playing or dancing: half speed, and no other performance until this one ends.",
                new StatModifiers { MoveSpeedPercent = -50f }));
            register(Buff(WhistleOfHeimdall, "Whistle of Heimdall", "WHS", "#5DADE2", PerformanceLinger, "A Skald's song: +3 FLEE per level.",
                new StatModifiers { Flee = 3 }, new StatModifiers { Flee = 3 }));
            register(Buff(SunsetLay, "Sunset Lay", "ACS", "#E67E22", PerformanceLinger, "A Skald's song: +2% ASPD per level.",
                new StatModifiers { AspdPercent = 2f }, new StatModifiers { AspdPercent = 2f }));
            register(Buff(BragisVerse, "Bragi's Verse", "POB", "#AF7AC5", PerformanceLinger, "A Skald's song: -3% cast time and -2% cooldowns per level.",
                new StatModifiers { CastTimePercent = -3f, CooldownPercent = -2f }, new StatModifiers { CastTimePercent = -3f, CooldownPercent = -2f }));
            register(Buff(IdunsApple, "Iðunn's Apple", "AOI", "#C0392B", PerformanceLinger, "A Skald's song: +1% Max HP and +4 HP regen per level.",
                new StatModifiers { MaxHpPercent = 1f, HpRegenFlat = 4 }, new StatModifiers { MaxHpPercent = 1f, HpRegenFlat = 4 }));
            register(Buff(Humming, "Spinning Hum", "HUM", "#F1948A", PerformanceLinger, "A Seidkona's dance: +4 HIT per level.",
                new StatModifiers { Hit = 4 }, new StatModifiers { Hit = 4 }));
            register(Debuff(ForgetfulDance, "Forgetful Dance", "PDF", "#7D3C98", PerformanceLinger, "Bewitched by a dance: slower attacks and steps.",
                new StatModifiers { AspdPercent = -6f, MoveSpeedPercent = -10f }, new StatModifiers { AspdPercent = -1f, MoveSpeedPercent = -2f }));
            register(Buff(FreyjasKiss, "Freyja's Kiss", "FTK", "#EC7063", PerformanceLinger, "A Seidkona's dance: +1 CRIT per level.",
                new StatModifiers { Crit = 1f }, new StatModifiers { Crit = 1f }));
            register(Buff(SeidrService, "Seiðr Service", "SVC", "#85C1E9", PerformanceLinger, "A Seidkona's dance: +1% Max SP and +2 SP regen per level.",
                new StatModifiers { MaxSpPercent = 1f, SpRegenFlat = 2 }, new StatModifiers { MaxSpPercent = 1f, SpRegenFlat = 2 }));
            register(Buff(HermodrsWard, "Hermóðr's Ward", "WOH", "#76D7C4", PerformanceLinger, "A Thul's song: +5% MDEF and 4% of spells reflected per level.",
                new StatModifiers { MdefPercent = 5f, ReflectMagicPercent = 4f }, new StatModifiers { MdefPercent = 5f, ReflectMagicPercent = 4f }));
            register(Buff(BalladOfValhalla, "Ballad of Valhalla", "BOV", "#D4AC0D", PerformanceLinger, "A Thul's song: +3% physical damage per level.",
                new StatModifiers { PhysicalDamagePercent = 3f }, new StatModifiers { PhysicalDamagePercent = 3f }));
            register(Debuff(SeidrCharm, "Seiðr Charm", "WNK", "#BB8FCE", 10f, "Charmed: -10 HIT and FLEE per level of the charm.",
                new StatModifiers { Hit = -10, Flee = -10 }, new StatModifiers { Hit = -5, Flee = -5 }));
            register(Buff(FriggsKiss, "Frigg's Kiss", "FKS", "#F5B7B1", PerformanceLinger, "A Völva's dance: +3% magic damage and +2 SP regen per level.",
                new StatModifiers { MagicDamagePercent = 3f, SpRegenFlat = 2 }, new StatModifiers { MagicDamagePercent = 3f, SpRegenFlat = 2 }));

            // ---------------------------------------------------------------- Trader line
            register(Buff(MarketShout, "Market Shout", "CRU", "#CA6F1E", 300f, "A trader's bellow: +4 STR and +30 ATK.",
                new StatModifiers { Atk = 30 }.SetStat(StatType.Str, 4)));
            register(Buff(DwarfsFervor, "Dwarf's Fervor", "ADR", "#BA4A00", 30f, "+10% ASPD, +1% per level.",
                new StatModifiers { AspdPercent = 10f }, new StatModifiers { AspdPercent = 1f }));
            register(Buff(TrueEdge, "True Edge", "WPF", "#839192", 30f, "Every swing finds its mark: +4% physical damage, +1% per level.",
                new StatModifiers { PhysicalDamagePercent = 4f }, new StatModifiers { PhysicalDamagePercent = 1f }));
            register(Buff(HammerRhythm, "Hammer Rhythm", "PTH", "#A04000", 60f, "+5% physical damage per level.",
                new StatModifiers { PhysicalDamagePercent = 5f }, new StatModifiers { PhysicalDamagePercent = 5f }));
            register(Buff(FullSwing, "Full Swing", "MXP", "#6E2C00", 30f, "+10% critical damage and +2 CRIT per level.",
                new StatModifiers { CritDamagePercent = 10f, Crit = 2f }, new StatModifiers { CritDamagePercent = 10f, Crit = 2f }));
            register(Buff(MoltenEdge, "Molten Edge", "MLT", "#E74C3C", 30f, "Glowing steel: ignores 10% of DEF and +5% damage, more per level.",
                new StatModifiers { DefBypassPercent = 10f, PhysicalDamagePercent = 5f }, new StatModifiers { DefBypassPercent = 4f, PhysicalDamagePercent = 2f }));
            register(Buff(WheelRush, "Wheel Rush", "CBT", "#AF601A", 60f, "Cart at full tilt: +25% movement speed.",
                new StatModifiers { MoveSpeedPercent = 25f }));
            register(Buff(Overthrust, "Overthrust", "MOT", "#4D2600", 30f, "+20% physical damage per level.",
                new StatModifiers { PhysicalDamagePercent = 20f }, new StatModifiers { PhysicalDamagePercent = 20f }));
            register(Buff(HallowedCoating, "Hallowed Coating", "CPC", "#48C9B0", 120f, "+8% DEF and MDEF per level.",
                new StatModifiers { DefPercent = 8f, MdefPercent = 8f }, new StatModifiers { DefPercent = 8f, MdefPercent = 8f }));
            register(Buff(FullCoating, "Full Coating", "FCP", "#1ABC9C", 300f, "+15% DEF and MDEF and 5% less damage taken, more per level.",
                new StatModifiers { DefPercent = 15f, MdefPercent = 15f, DamageTakenPercent = -5f },
                new StatModifiers { DefPercent = 5f, MdefPercent = 5f, DamageTakenPercent = -1f }));

            // ---------------------------------------------------------------- Devotee line
            register(Debuff(HeavyLimbs, "Heavy Limbs", "DAG", "#5B2C6F", 20f, "-15% movement speed and -5% ASPD, more per level.",
                new StatModifiers { MoveSpeedPercent = -15f, AspdPercent = -5f }, new StatModifiers { MoveSpeedPercent = -2f, AspdPercent = -1f }));
            register(Buff(GaldrOfEir, "Galdr of Eir", "MAG", "#AED6F1", 60f, "+6 SP regen per level.",
                new StatModifiers { SpRegenFlat = 6 }, new StatModifiers { SpRegenFlat = 6 }));
            register(Buff(GloryOfBaldr, "Glory of Baldr", "GLO", "#F9E79F", 60f, "Baldr's radiance: +10 LUK.",
                new StatModifiers().SetStat(StatType.Luk, 10)));
            register(Buff(SvalinnsShield, "Svalinn's Shield", "KYR", "#F7DC6F", 120f, "The shield before the sun absorbs the next 3,000 damage.",
                StatModifiers.Empty(), absorb: 3000));
            register(Buff(TyrsHand, "Tyr's Hand", "IMP", "#D35400", 60f, "+5 ATK per level.",
                new StatModifiers { Atk = 5 }, new StatModifiers { Atk = 5 }));
            register(Buff(GaldrOfHaste, "Galdr of Haste", "SUF", "#85C1E9", 60f, "-15% cast time per level (one spell's worth of haste for longer).",
                new StatModifiers { CastTimePercent = -15f }, new StatModifiers { CastTimePercent = -15f }));
            register(Debuff(NornsDoom, "Norns' Doom", "LXA", "#922B21", 6f, "Doomed by the Norns: takes 50% more damage.",
                new StatModifiers { DamageTakenPercent = 50f }));
            register(Buff(RampartOfRunar, "Rampart of Rúnar", "SFW", "#D7BDE2", 20f, "A rune-wall blocks the next melee hits.",
                StatModifiers.Empty(), traits: BuffTraits.MeleeBlockCharges, charges: 2));
            register(Buff(HofSanctum, "Hof Sanctum", "BAS", "#FAD7A0", 8f, "Inside the sanctum: 50% less damage taken, +5% per level.",
                new StatModifiers { DamageTakenPercent = -50f }, new StatModifiers { DamageTakenPercent = -5f }));

            // ---------------------------------------------------------------- Warrior and Mystic lines
            register(Buff(Riposte, "Riposte", "CNT", "#B03A2E", 5f, "The next basic attacks are guaranteed criticals.",
                StatModifiers.Empty(), traits: BuffTraits.CriticalCharges, charges: 1));
            register(Buff(ValhallasEdge, "Valhalla's Edge", "AUB", "#E59866", 60f, "Your blade burns with Valhalla's light: +20 ATK per level.",
                new StatModifiers { Atk = 20 }, new StatModifiers { Atk = 20 }));
            register(Buff(Parry, "Parry", "PAR", "#99A3A4", 30f, "+5% block chance per level.",
                new StatModifiers { BlockChance = 5f }, new StatModifiers { BlockChance = 5f }));
            register(Buff(Berserkergang, "Berserkergang", "BSK", "#922B21", 30f, "The berserker's trance: double Max HP, +30% ASPD and no stagger, but no items.",
                new StatModifiers { MaxHpMultiplier = 2f, AspdPercent = 30f, ItemsLocked = true, StaggerImmune = true }));
            register(Buff(SeidrCoat, "Seiðr Coat", "ENC", "#5DADE2", 120f, "A coat of seidr: 10% less damage taken, +2% per level.",
                new StatModifiers { DamageTakenPercent = -10f }, new StatModifiers { DamageTakenPercent = -2f }));

            // ---------------------------------------------------------------- Expanded jobs
            register(Buff(NornsFavor, "Norns' Favor", "ANG", "#F4D03F", 60f, "The Norns watch over a Wanderer: +10 ATK, +10 MATK and +1 CRIT per level.",
                new StatModifiers { Atk = 10, Matk = 10, Crit = 1f }, new StatModifiers { Atk = 10, Matk = 10, Crit = 1f }));
            register(Buff(LastStand, "Last Stand", "STB", "#7F8C8D", 10f, "Steel body: 60% less damage taken, -25% speed.",
                new StatModifiers { DamageTakenPercent = -60f, MoveSpeedPercent = -25f, StaggerImmune = true }));
            register(Buff(Sprint, "Sprint", "RUN", "#E74C3C", 20f, "+20% movement speed, +5% per level.",
                new StatModifiers { MoveSpeedPercent = 20f }, new StatModifiers { MoveSpeedPercent = 5f }));
            register(Buff(ComfortOfStars, "Comfort of the Stars", "COS", "#F4F6F7", 60f, "+3% ASPD and +3 HIT per level.",
                new StatModifiers { AspdPercent = 3f, Hit = 3 }, new StatModifiers { AspdPercent = 3f, Hit = 3 }));
            register(Buff(Union, "Union of Sun, Moon and Stars", "FUS", "#F5B041", 30f, "Sól, Máni and the stars as one: +20% damage and ASPD, hyper armour.",
                new StatModifiers { PhysicalDamagePercent = 20f, AspdPercent = 20f, HyperArmor = true }));
            register(Buff(SolsProtection, "Sól's Protection", "SPR", "#F8C471", 60f, "+6% DEF per level.",
                new StatModifiers { DefPercent = 6f }, new StatModifiers { DefPercent = 6f }));
            register(Buff(Hamingja, "Hamingja", "KZL", "#AED6F1", 300f, "A luck-spirit stands guard: absorbs the next 5,000 damage.",
                StatModifiers.Empty(), absorb: 5000));
            register(Buff(FylgjasMend, "Fylgja's Mend", "KAH", "#82E0AA", 120f, "+8 HP regen per level.",
                new StatModifiers { HpRegenFlat = 8 }, new StatModifiers { HpRegenFlat = 8 }));
            register(Buff(SpiritDodge, "Spirit Dodge", "KAU", "#A9CCE3", 60f, "+8 FLEE per level.",
                new StatModifiers { Flee = 8 }, new StatModifiers { Flee = 8 }));
            register(Buff(SpiritMirror, "Spirit Mirror", "KAI", "#D2B4DE", 60f, "Reflects 10% of spells per level.",
                new StatModifiers { ReflectMagicPercent = 10f }, new StatModifiers { ReflectMagicPercent = 10f }));
            register(Buff(FylgjaBond, "Fylgja Bond", "LNK", "#5499C7", 180f, "A guardian spirit walks beside you: +5 to every stat and +5% damage.",
                new StatModifiers { PhysicalDamagePercent = 5f, MagicDamagePercent = 5f }
                    .SetStat(StatType.Str, 5).SetStat(StatType.Agi, 5).SetStat(StatType.Vit, 5)
                    .SetStat(StatType.Int, 5).SetStat(StatType.Dex, 5).SetStat(StatType.Luk, 5)));
            register(new BuffDefinition
            {
                Id = ThorsCoins, Name = "Thor's Coins", IconLabel = "COI", IconColorHex = "#F7DC6F", Duration = 600f,
                Description = "+2 HIT per coin. Spent by the Thunderer's heavier shots.",
                MaxStacks = MaxThorsCoins,
                ModifiersPerStack = new StatModifiers { Hit = 2 },
            });
            register(Buff(ThunderFever, "Thunder Fever", "GAT", "#F39C12", 30f, "+20% ASPD and +10 ATK per level, -30% movement speed.",
                new StatModifiers { AspdPercent = 20f, Atk = 10, MoveSpeedPercent = -30f }, new StatModifiers { Atk = 10 }));
            register(Buff(SteadyAim, "Steady Aim", "INC", "#58D68D", 60f, "+20 HIT, +4 DEX and +4 AGI.",
                new StatModifiers { Hit = 20 }.SetStat(StatType.Dex, 4).SetStat(StatType.Agi, 4)));
            register(Buff(MjolnirsStillness, "Mjölnir's Stillness", "MAD", "#D4AC0D", 15f, "Planted like a hammer: +100 ATK and +20% ASPD, but you can barely move.",
                new StatModifiers { Atk = 100, AspdPercent = 20f, MoveSpeedPercent = -80f }));
            register(Buff(WeatherEye, "Weather-Eye", "ADJ", "#ABB2B9", 30f, "+30 FLEE, -30 HIT.",
                new StatModifiers { Flee = 30, Hit = -30 }));
            register(Buff(ShedSkin, "Shed Skin", "CIC", "#A3E4D7", 60f, "Leave a husk behind: blocks the next melee hits (one per level, at most 5).",
                StatModifiers.Empty(), traits: BuffTraits.MeleeBlockCharges, charges: 1));
            register(Buff(MirrorImage, "Mirror Image", "MIR", "#D6EAF8", 60f, "Shadow copies take blows meant for you: +10% block chance per level.",
                new StatModifiers { BlockChance = 10f }, new StatModifiers { BlockChance = 10f }));
            register(Buff(Hiss, "Hiss", "HIS", "#E59866", 30f, "Fur on end: +10 FLEE per level.",
                new StatModifiers { Flee = 10 }, new StatModifiers { Flee = 10 }));
            register(Buff(FishFeast, "Fish Feast", "TUN", "#5DADE2", 60f, "A fine meal: absorbs the next 2,500 damage.",
                StatModifiers.Empty(), absorb: 2500));
            register(Buff(PurrOfComfort, "Purr of Comfort", "PUR", "#F5CBA7", 60f, "+6 HP regen per level.",
                new StatModifiers { HpRegenFlat = 6 }, new StatModifiers { HpRegenFlat = 6 }));
            register(Buff(Chattering, "Chattering", "CHT", "#DC7633", 60f, "+15 ATK and MATK per level.",
                new StatModifiers { Atk = 15, Matk = 15 }, new StatModifiers { Atk = 15, Matk = 15 }));
        }

        private static BuffDefinition Buff(string id, string name, string icon, string color, float duration, string description,
            StatModifiers modifiers, StatModifiers perLevel = null, BuffTraits traits = BuffTraits.None, int charges = 0, int absorb = 0)
        {
            return new BuffDefinition
            {
                Id = id, Name = name, IconLabel = icon, IconColorHex = color, Duration = duration, Description = description,
                Modifiers = modifiers ?? StatModifiers.Empty(), ModifiersPerLevel = perLevel, Traits = traits, Charges = charges, AbsorbAmount = absorb,
            };
        }

        private static BuffDefinition Debuff(string id, string name, string icon, string color, float duration, string description,
            StatModifiers modifiers, StatModifiers perLevel = null)
        {
            var buff = Buff(id, name, icon, color, duration, description, modifiers, perLevel);
            buff.IsDebuff = true;
            return buff;
        }
    }
}
