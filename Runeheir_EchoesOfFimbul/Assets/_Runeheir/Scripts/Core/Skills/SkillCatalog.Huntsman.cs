using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    /// <summary>
    /// Phase 7, Ragnarok's Archer line under Norse names: Huntsman (Archer) → Ranger (Hunter) → Deadeye (Sniper),
    /// Huntsman → Skald (Bard) → Thul (Minstrel), Huntsman → Seidkona (Dancer) → Völva (Gypsy). Double Strafe, Arrow Shower,
    /// Owl's Eye and Vulture's Eye live with the Ranger's in SkillCatalog.Scout. Songs and dances are performances: an aura
    /// that walks with the player and gives allies (or foes) its effect.
    /// </summary>
    public static partial class SkillCatalog
    {
        private const float PerformanceRadius = 4.5f;
        private const float PerformanceSeconds = 60f;

        private static void RegisterHuntsmanLine()
        {
            // ---------------------------------------------------------------- Huntsman
            Register(new SkillDefinition
            {
                Id = "hawk_focus", Name = "Hawk Focus", Job = JobId.Huntsman, MaxLevel = 10, Requires = Req("vultures_eye", 1),
                Description = "Ragnarok's Improve Concentration: a hawk's stillness, +2 AGI and DEX per level.",
                IconLabel = "HFC", IconColorHex = "#6E8B3D",
                Target = SkillTarget.Self, BuffId = SkillBuffs.HawkFocus, Motion = SkillMotion.Buff,
                BuffDuration = L(60f, 20f), SpCost = L(25f, 5f),
            });
            Register(new SkillDefinition
            {
                Id = "gust_arrow", Name = "Gust Arrow", Job = JobId.Huntsman, MaxLevel = 1, Requires = Req("double_strafe", 3),
                Description = "Ragnarok's Arrow Repel: a heavy shot that throws the target far back.",
                IconLabel = "GUS", IconColorHex = "#82E0AA", Weapons = WeaponMask.Bow,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true,
                Range = 9f, Power = 150f, Knockback = 3f, CastTime = 1.5f, SpCost = 15f, PoiseMultiplier = 2f,
            });

            // ---------------------------------------------------------------- Ranger (Hunter): traps
            Register(new SkillDefinition
            {
                Id = "raven_training", Name = "Raven Training", Job = JobId.Ranger, MaxLevel = 10, Passive = true, Requires = Req("raven_strike", 1),
                Description = "Ragnarok's Steel Crow: a better-trained raven and steadier hands. +3 ATK per level with bows.",
                IconLabel = "STC", IconColorHex = "#1C2833",
                PassivePerLevel = new StatModifiers { Atk = 3 }, PassiveWeapons = WeaponMask.Bow,
            });
            Register(new SkillDefinition
            {
                Id = "rune_mine", Name = "Rune Mine", Job = JobId.Ranger, MaxLevel = 5, Requires = Req("ankle_snare", 1),
                Description = "Ragnarok's Land Mine: an earth rune that bursts under the first foe to step on it. Can stun.",
                IconLabel = "LMN", IconColorHex = "#A04000",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, ZoneTrap = true, Damage = SkillDamage.Physical, Motion = SkillMotion.Cast,
                Element = Element.Earth, UseWeaponElement = false, NeverMiss = true,
                Range = 3f, Radius = 1.2f, Power = L(150f, 50f), ZoneDuration = L(120f, 30f), SpCost = 10f, AfterCastDelay = 0.5f,
                Status = StatusEffect.Stun, StatusChance = L(30f, 5f), StatusDuration = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "muspel_mine", Name = "Muspel Mine", Job = JobId.Ranger, MaxLevel = 5, Requires = Req("rune_mine", 1),
                Description = "Ragnarok's Blast Mine: a fire rune that explodes over a wide area.",
                IconLabel = "BMN", IconColorHex = "#E74C3C",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, ZoneTrap = true, Damage = SkillDamage.Physical, Motion = SkillMotion.Cast,
                Element = Element.Fire, UseWeaponElement = false, NeverMiss = true,
                Range = 3f, Radius = 2.5f, Power = L(200f, 60f), ZoneDuration = L(60f, 15f), SpCost = 10f, AfterCastDelay = 0.5f,
            });
            Register(new SkillDefinition
            {
                Id = "rime_trap", Name = "Rime Trap", Job = JobId.Ranger, MaxLevel = 5, Requires = Req("rune_mine", 1),
                Description = "Ragnarok's Freezing Trap: Niflheim's cold clamps shut, freezing whatever it catches.",
                IconLabel = "FZT", IconColorHex = "#85C1E9",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, ZoneTrap = true, Damage = SkillDamage.Physical, Motion = SkillMotion.Cast,
                Element = Element.Water, UseWeaponElement = false, NeverMiss = true,
                Range = 3f, Radius = 1.5f, Power = L(100f, 30f), ZoneDuration = L(90f, 30f), SpCost = 10f, AfterCastDelay = 0.5f,
                Status = StatusEffect.Freeze, StatusChance = L(50f, 10f), StatusDuration = 3f,
            });
            Register(new SkillDefinition
            {
                Id = "thorn_of_sleep", Name = "Thorn of Sleep", Job = JobId.Ranger, MaxLevel = 5, Requires = Req("ankle_snare", 1),
                Description = "Ragnarok's Sandman: a sleep-thorn trap, the same that felled Brynhildr. Puts foes around it to sleep.",
                IconLabel = "SND", IconColorHex = "#BB8FCE",
                Target = SkillTarget.Ground, Special = SkillSpecial.Zone, ZoneTrap = true, Motion = SkillMotion.Cast,
                Range = 3f, Radius = 2.5f, ZoneDuration = L(90f, 30f), SpCost = 12f, AfterCastDelay = 0.5f,
                Status = StatusEffect.Sleep, StatusChance = L(50f, 10f), StatusDuration = 6f,
            });

            // ---------------------------------------------------------------- Skald (Bard): songs
            Register(new SkillDefinition
            {
                Id = "bragis_lessons", Name = "Bragi's Lessons", Job = JobId.Skald, MaxLevel = 10, Passive = true,
                Description = "Ragnarok's Music Lessons: +3 ATK and +0.5% ASPD per level with instruments.",
                IconLabel = "MSL", IconColorHex = "#A04000",
                PassivePerLevel = new StatModifiers { Atk = 3, AspdPercent = 0.5f }, PassiveWeapons = WeaponMask.Instrument,
            });
            Register(new SkillDefinition
            {
                Id = "lay_strike", Name = "Lay Strike", Job = JobId.Skald, MaxLevel = 5, Requires = Req("bragis_lessons", 1),
                Description = "Ragnarok's Melody Strike: a struck chord that flies like an arrow.",
                IconLabel = "MSK", IconColorHex = "#DC7633", Weapons = WeaponMask.Instrument | WeaponMask.Bow,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true,
                Range = 7f, Power = L(160f, 40f), CastTime = 1.5f, SpCost = L(6f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "dissonance", Name = "Dissonance", Job = JobId.Skald, MaxLevel = 5, Requires = Req("bragis_lessons", 1),
                Description = "A grating performance: every 3 seconds, foes around you take damage. Moves with you.",
                IconLabel = "DSN", IconColorHex = "#922B21", Weapons = WeaponMask.Instrument,
                Target = SkillTarget.Self, Special = SkillSpecial.Performance, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast,
                UseWeaponElement = false, NeverMiss = true,
                Radius = PerformanceRadius, Power = L(30f, 10f), ZoneDuration = 30f, ZoneTick = 3f, SpCost = L(18f, 3f), AfterCastDelay = 1f,
            });
            Song("whistle_of_heimdall", "Whistle of Heimdall", "Ragnarok's A Whistle: Heimdall's sharp ear for every ally around you, +3 FLEE per level.",
                "WHS", "#5DADE2", SkillBuffs.WhistleOfHeimdall);
            Song("sunset_lay", "Sunset Lay", "Ragnarok's Assassin Cross of Sunset: a quick, bright air, +2% ASPD per level for allies around you.",
                "ACS", "#E67E22", SkillBuffs.SunsetLay);
            Song("bragis_verse", "Bragi's Verse", "Ragnarok's A Poem of Bragi: allies around you cast 3% faster and wait 2% less per level.",
                "POB", "#AF7AC5", SkillBuffs.BragisVerse);
            Song("iduns_apple", "Iðunn's Apple", "Ragnarok's The Apple of Idun: the goddess's fruit, +1% Max HP and +4 HP regen per level for allies.",
                "AOI", "#C0392B", SkillBuffs.IdunsApple);

            // ---------------------------------------------------------------- Seidkona (Dancer): dances
            Register(new SkillDefinition
            {
                Id = "seidr_lessons", Name = "Seiðr Lessons", Job = JobId.Seidkona, MaxLevel = 10, Passive = true,
                Description = "Ragnarok's Dancing Lessons: +3 ATK and +0.5 CRIT per level with whips.",
                IconLabel = "DNL", IconColorHex = "#8E44AD",
                PassivePerLevel = new StatModifiers { Atk = 3, Crit = 0.5f }, PassiveWeapons = WeaponMask.Whip,
            });
            Register(new SkillDefinition
            {
                Id = "slinging_lash", Name = "Slinging Lash", Job = JobId.Seidkona, MaxLevel = 5, Requires = Req("seidr_lessons", 1),
                Description = "Ragnarok's Slinging Arrow: the whip's tip cracks out at range.",
                IconLabel = "SLA", IconColorHex = "#AF7AC5", Weapons = WeaponMask.Whip | WeaponMask.Bow,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true,
                Range = 7f, Power = L(160f, 40f), CastTime = 1.5f, SpCost = L(6f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "heids_shriek", Name = "Heiðr's Shriek", Job = JobId.Seidkona, MaxLevel = 5, Requires = Req("seidr_lessons", 1),
                Description = "Ragnarok's Scream: a seeress's cry that can stun everyone around you.",
                IconLabel = "SCR", IconColorHex = "#7D3C98",
                Target = SkillTarget.Self, Area = SkillArea.AroundSelf, Motion = SkillMotion.Cast,
                Radius = 5f, SpCost = L(12f, 3f), Cooldown = 3f, AfterCastDelay = 1f,
                Status = StatusEffect.Stun, StatusChance = L(10f, 5f), StatusDuration = 3f,
            });
            Dance("spinning_hum", "Spinning Hum", "Ragnarok's Humming: allies around you gain +4 HIT per level.", "HUM", "#F1948A", buffId: SkillBuffs.Humming);
            Dance("forgetful_dance", "Forgetful Dance", "Ragnarok's Please Don't Forget Me: foes around you attack and move slower.",
                "PDF", "#7D3C98", debuffId: SkillBuffs.ForgetfulDance);
            Dance("freyjas_kiss", "Freyja's Kiss", "Ragnarok's Fortune's Kiss: the goddess's favour, +1 CRIT per level for allies around you.",
                "FTK", "#EC7063", buffId: SkillBuffs.FreyjasKiss);
            Dance("seidr_service", "Seiðr Service", "Ragnarok's Service for You: allies around you gain Max SP and SP regen.",
                "SVC", "#85C1E9", buffId: SkillBuffs.SeidrService);

            // ---------------------------------------------------------------- Thul (Minstrel)
            Register(new SkillDefinition
            {
                Id = "bragis_volley", Name = "Bragi's Volley", Job = JobId.Thul, MaxLevel = 10, Requires = Req("lay_strike", 3),
                Description = "Ragnarok's Arrow Vulcan: nine notes in a flurry, each a blow.",
                IconLabel = "AVC", IconColorHex = "#BA4A00", Weapons = WeaponMask.Instrument | WeaponMask.Bow,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot,
                Range = 7f, Power = L(100f, 10f), Hits = 9, HitInterval = 0.08f, CastTime = L(2f, 0.1f), SpCost = L(12f, 2f), Cooldown = 2f,
            });
            Song("hermodrs_ward", "Hermóðr's Ward", "Ragnarok's Wand of Hermode: the messenger's ward, +5% MDEF and 4% of spells reflected per level.",
                "WOH", "#76D7C4", SkillBuffs.HermodrsWard, JobId.Thul, Req("whistle_of_heimdall", 5));
            Song("ballad_of_valhalla", "Ballad of Valhalla", "A war-song from the hall of the slain: +3% physical damage per level for allies around you.",
                "BOV", "#D4AC0D", SkillBuffs.BalladOfValhalla, JobId.Thul, Req("sunset_lay", 5));
            Register(new SkillDefinition
            {
                Id = "gjallarhorn_blast", Name = "Gjallarhorn Blast", Job = JobId.Thul, MaxLevel = 5, Requires = Req("dissonance", 5),
                Description = "A blast on Heimdall's horn: everyone around you is struck, and can be stunned.",
                IconLabel = "GJH", IconColorHex = "#F0B27A",
                Target = SkillTarget.Self, Damage = SkillDamage.Magic, Area = SkillArea.AroundSelf, Motion = SkillMotion.Cast,
                UseWeaponElement = false, Radius = 5f, Power = L(200f, 40f), CastTime = 1.5f, SpCost = L(40f, 5f), Cooldown = 5f,
                Status = StatusEffect.Stun, StatusChance = L(15f, 5f), StatusDuration = 2f,
            });

            // ---------------------------------------------------------------- Völva (Gypsy)
            Register(new SkillDefinition
            {
                Id = "serpent_volley", Name = "Serpent Volley", Job = JobId.Volva, MaxLevel = 10, Requires = Req("slinging_lash", 3),
                Description = "Ragnarok's Arrow Vulcan, by the lash: nine strikes as fast as a striking serpent.",
                IconLabel = "AVC", IconColorHex = "#6C3483", Weapons = WeaponMask.Whip | WeaponMask.Bow,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot,
                Range = 7f, Power = L(100f, 10f), Hits = 9, HitInterval = 0.08f, CastTime = L(2f, 0.1f), SpCost = L(12f, 2f), Cooldown = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "skulds_verdict", Name = "Skuld's Verdict", Job = JobId.Volva, MaxLevel = 5, Requires = Req("heids_shriek", 3),
                Description = "Ragnarok's Tarot Card of Fate: the Norn of what shall be passes judgement. Can curse.",
                IconLabel = "TCF", IconColorHex = "#4A235A",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, UseWeaponElement = false,
                Range = 9f, Power = L(120f, 30f), CastTime = 1f, SpCost = 40f, Cooldown = 3f,
                Status = StatusEffect.Curse, StatusChance = L(20f, 5f), StatusDuration = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "seidr_charm", Name = "Seiðr Charm", Job = JobId.Volva, MaxLevel = 5, Requires = Req("forgetful_dance", 3),
                Description = "Ragnarok's Wink of Charm: a glance that dulls a foe's aim and footwork.",
                IconLabel = "WNK", IconColorHex = "#BB8FCE",
                Target = SkillTarget.Enemy, Motion = SkillMotion.Cast,
                Range = 9f, DebuffId = SkillBuffs.SeidrCharm, DebuffDuration = 10f, DebuffChance = L(50f, 10f), SpCost = 15f, Cooldown = 2f,
            });
            Dance("friggs_kiss", "Frigg's Kiss", "Odin's queen blesses the circle: +3% magic damage and +2 SP regen per level for allies around you.",
                "FKS", "#F5B7B1", buffId: SkillBuffs.FriggsKiss, job: JobId.Volva, requires: Req("seidr_service", 5));
        }

        /// <summary>A Skald (or Thul) song: allies around the performer gain <paramref name="buffId"/>.</summary>
        private static void Song(string id, string name, string description, string icon, string color, string buffId,
            JobId job = JobId.Skald, SkillRequirement[] requires = null)
        {
            Register(new SkillDefinition
            {
                Id = id, Name = name, Job = job, MaxLevel = 10, Requires = requires ?? Req("dissonance", 3),
                Description = description + " Moves with you.",
                IconLabel = icon, IconColorHex = color, Weapons = WeaponMask.Instrument,
                Target = SkillTarget.Self, Special = SkillSpecial.Performance, Motion = SkillMotion.Cast,
                Radius = PerformanceRadius, ZoneDuration = PerformanceSeconds, ZoneTick = 1f,
                BuffId = buffId, BuffDuration = SkillBuffs.PerformanceLinger, SpCost = L(30f, 5f), AfterCastDelay = 1f,
            });
        }

        /// <summary>A Seidkona (or Völva) dance: allies gain <paramref name="buffId"/>, or foes suffer <paramref name="debuffId"/>.</summary>
        private static void Dance(string id, string name, string description, string icon, string color, string buffId = null, string debuffId = null,
            JobId job = JobId.Seidkona, SkillRequirement[] requires = null)
        {
            Register(new SkillDefinition
            {
                Id = id, Name = name, Job = job, MaxLevel = 10, Requires = requires ?? Req("seidr_lessons", 3),
                Description = description + " Moves with you.",
                IconLabel = icon, IconColorHex = color, Weapons = WeaponMask.Whip,
                Target = SkillTarget.Self, Special = SkillSpecial.Performance, Motion = SkillMotion.Cast,
                Radius = PerformanceRadius, ZoneDuration = PerformanceSeconds, ZoneTick = 1f,
                BuffId = buffId, BuffDuration = SkillBuffs.PerformanceLinger,
                DebuffId = debuffId, DebuffDuration = SkillBuffs.PerformanceLinger,
                SpCost = L(30f, 5f), AfterCastDelay = 1f,
            });
        }
    }
}
