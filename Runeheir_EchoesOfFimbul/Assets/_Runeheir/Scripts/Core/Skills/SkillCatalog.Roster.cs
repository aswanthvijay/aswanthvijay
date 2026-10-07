using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Skills
{
    /// <summary>
    /// Phase 7: the Ragnarok skills the GDD lines were missing (Feign Death, Hiding, Counter Attack, Aura Blade, Fire Ball,
    /// Kyrie Eleison, Lex Aeterna...), and the Outlaw (Rogue) → Vargr (Stalker) line, all under Norse names.
    /// </summary>
    public static partial class SkillCatalog
    {
        private static void RegisterRosterAdditions()
        {
            // ---------------------------------------------------------------- Initiate
            Register(new SkillDefinition
            {
                Id = "feign_death", Name = "Feign Death", Job = JobId.Initiate, MaxLevel = 1, Requires = Req(SkillBook.BasicTrainingId, 7),
                Description = "Ragnarok's Play Dead: drop and lie still until monsters lose track of you. You crawl slowly; attacking ends it.",
                IconLabel = "FGN", IconColorHex = "#7F8C8D",
                Target = SkillTarget.Self, BuffId = SkillBuffs.FeignDeath, Motion = SkillMotion.Buff, SpCost = 5f, Cooldown = 10f,
            });

            // ---------------------------------------------------------------- Scout (Thief)
            Register(new SkillDefinition
            {
                Id = "hide_in_shadows", Name = "Hide in Shadows", Job = JobId.Scout, MaxLevel = 10, Requires = Req("pilfer", 5),
                Description = "Ragnarok's Hiding: melt into the shadows. Monsters can't see you, but you can barely move.",
                IconLabel = "HID", IconColorHex = "#2C3E50",
                Target = SkillTarget.Self, BuffId = SkillBuffs.HideInShadows, Motion = SkillMotion.Buff,
                BuffDuration = L(30f, 30f), SpCost = 10f, Cooldown = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "purge_venom", Name = "Purge Venom", Job = JobId.Scout, MaxLevel = 1, Requires = Req("envenom", 3),
                Description = "Ragnarok's Detoxify: draw the poison, and every other ill, out of yourself or an ally.",
                IconLabel = "DTX", IconColorHex = "#52BE80",
                Target = SkillTarget.Friend, Special = SkillSpecial.Cleanse, Motion = SkillMotion.Cast, Range = 9f, SpCost = 10f,
            });

            // ---------------------------------------------------------------- Outlaw (Rogue)
            Register(new SkillDefinition
            {
                Id = "outlaws_blade", Name = "Outlaw's Blade", Job = JobId.Outlaw, MaxLevel = 10, Passive = true,
                Description = "Ragnarok's Sword Mastery for Rogues: +4 ATK per level with daggers and swords.",
                IconLabel = "OBL", IconColorHex = "#5B4636",
                PassivePerLevel = new StatModifiers { Atk = 4 }, PassiveWeapons = WeaponMask.Dagger | WeaponMask.OneHandSword,
            });
            Register(new SkillDefinition
            {
                Id = "light_fingers", Name = "Light Fingers", Job = JobId.Outlaw, MaxLevel = 10, Passive = true, Requires = Req("pilfer", 1),
                Description = "Ragnarok's Snatcher: your basic attacks sometimes Pilfer by themselves (1% + 1% per level).",
                IconLabel = "SNT", IconColorHex = "#7E5109",
                Proc = new ProcDefinition { SkillIds = new[] { "pilfer" }, Chance = L(2f, 1f), ProcLevel = 10f, OnlyLearned = true },
            });
            Register(new SkillDefinition
            {
                Id = "knife_in_the_back", Name = "Knife in the Back", Job = JobId.Outlaw, MaxLevel = 10, Requires = Req("light_fingers", 4),
                Description = "Ragnarok's Back Stab: a strike that never misses.",
                IconLabel = "BST", IconColorHex = "#641E16",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Thrust, NeverMiss = true,
                Range = 1.2f, Power = L(340f, 40f), SpCost = 16f, AfterCastDelay = 0.5f, PoiseMultiplier = 1.5f,
            });
            Register(new SkillDefinition
            {
                Id = "ambush_raid", Name = "Ambush Raid", Job = JobId.Outlaw, MaxLevel = 5, Requires = Req("knife_in_the_back", 2, "hide_in_shadows", 1),
                Description = "Ragnarok's Raid: burst out on everyone around you. Can stun.",
                IconLabel = "RAD", IconColorHex = "#943126",
                Target = SkillTarget.Self, Damage = SkillDamage.Physical, Area = SkillArea.AroundSelf, Motion = SkillMotion.Spin,
                Radius = 2.5f, Power = L(140f, 40f), SpCost = 20f, AfterCastDelay = 0.5f,
                Status = StatusEffect.Stun, StatusChance = L(13f, 3f), StatusDuration = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "cut_purse", Name = "Cut Purse", Job = JobId.Outlaw, MaxLevel = 10, Requires = Req("light_fingers", 4),
                Description = "Ragnarok's Steal Coin: lift a handful of zeny from a monster (once each, DEX and LUK against its level).",
                IconLabel = "STC", IconColorHex = "#F1C40F",
                Target = SkillTarget.Enemy, Special = SkillSpecial.StealCoin, Motion = SkillMotion.Swing, Range = 1.5f, SpCost = 15f,
            });
            Divest("divest_helm", "Divest Helm", "Ragnarok's Strip Helm: knock the helm off, weakening its MDEF and aim.", "DSH", "#4A235A",
                SkillBuffs.Unhelmed, Req("cut_purse", 2));
            Divest("divest_shield", "Divest Shield", "Ragnarok's Strip Shield: tear the shield away, weakening its DEF.", "DSS", "#7E5109",
                SkillBuffs.Shieldless, Req("divest_helm", 2));
            Divest("divest_armor", "Divest Armour", "Ragnarok's Strip Armor: rip the armour off, weakening its DEF and speed.", "DSA", "#6E2C00",
                SkillBuffs.Unarmored, Req("divest_shield", 2));
            Divest("divest_weapon", "Divest Weapon", "Ragnarok's Strip Weapon: wrench the weapon away, weakening its blows.", "DSW", "#7B241C",
                SkillBuffs.Disarmed, Req("divest_armor", 2));
            Register(new SkillDefinition
            {
                Id = "lokis_mimicry", Name = "Loki's Mimicry", Job = JobId.Outlaw, MaxLevel = 10, Passive = true, Requires = Req("cut_purse", 1),
                Description = "Ragnarok's Plagiarism: the last skill a nearby ally uses (or that hits you) is copied, usable up to this level. +1% ASPD per level.",
                IconLabel = "PLG", IconColorHex = "#8E44AD",
                PassivePerLevel = new StatModifiers { AspdPercent = 1f },
            });

            // ---------------------------------------------------------------- Vargr (Stalker)
            Register(new SkillDefinition
            {
                Id = "wolf_strip", Name = "Wolf-Strip", Job = JobId.Vargr, MaxLevel = 5, Requires = Req("divest_weapon", 5),
                Description = "Ragnarok's Full Strip: weapon, shield, armour and helm all at once.",
                IconLabel = "FST", IconColorHex = "#3B2F2F",
                Target = SkillTarget.Enemy, Motion = SkillMotion.Swing, Range = 1.5f,
                DebuffId = SkillBuffs.WolfStripped, DebuffDuration = L(15f, 5f), DebuffChance = L(20f, 5f), SpCost = 22f, Cooldown = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "wolfs_prowl", Name = "Wolf's Prowl", Job = JobId.Vargr, MaxLevel = 5, Requires = Req("hide_in_shadows", 5),
                Description = "Ragnarok's Chase Walk: hidden, yet moving at full stride. Breaking it lends +10 ATK per level.",
                IconLabel = "CWK", IconColorHex = "#5D4037",
                Target = SkillTarget.Self, BuffId = SkillBuffs.WolfsProwl, Motion = SkillMotion.Buff, BuffDuration = L(20f, 10f), SpCost = 10f, Cooldown = 3f,
            });
            Register(new SkillDefinition
            {
                Id = "preserve", Name = "Preserve", Job = JobId.Vargr, MaxLevel = 1, Requires = Req("lokis_mimicry", 10),
                Description = "Keep the skill Loki's Mimicry holds: nothing new is copied for 10 minutes.",
                IconLabel = "PRS", IconColorHex = "#8E44AD",
                Target = SkillTarget.Self, BuffId = SkillBuffs.PreserveCopy, Motion = SkillMotion.Buff, SpCost = 30f,
            });
            Register(new SkillDefinition
            {
                Id = "turn_the_blade", Name = "Turn the Blade", Job = JobId.Vargr, MaxLevel = 5, Requires = Req("outlaws_blade", 5),
                Description = "Ragnarok's Reject Sword: blades glance off you, some turned back on their wielders.",
                IconLabel = "RJS", IconColorHex = "#566573",
                Target = SkillTarget.Self, BuffId = SkillBuffs.TurnTheBlade, Motion = SkillMotion.Buff, BuffDuration = 30f, SpCost = L(10f, 5f),
            });

            // ---------------------------------------------------------------- Assassin and Shadow Walker
            Register(new SkillDefinition
            {
                Id = "vipers_burst", Name = "Viper's Burst", Job = JobId.Assassin, MaxLevel = 10, Requires = Req("venom_dust", 5),
                Description = "Ragnarok's Venom Splasher: poison bursts out of the wound. Always poisons.",
                IconLabel = "VSP", IconColorHex = "#7D3C98",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Thrust,
                Element = Element.Poison, UseWeaponElement = false,
                Range = 1.2f, Power = L(300f, 60f), CastTime = 1f, SpCost = L(12f, 3f), Cooldown = L(7.5f, -0.5f), PoiseMultiplier = 1.5f,
                Status = StatusEffect.Poison, StatusChance = 100f, StatusDuration = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "night_squall", Name = "Night Squall", Job = JobId.ShadowWalker, MaxLevel = 10, Requires = Req("lacerate", 3),
                Description = "Ragnarok's Meteor Assault: a storm of blows around you. Can stun.",
                IconLabel = "MAS", IconColorHex = "#1B2631",
                Target = SkillTarget.Self, Damage = SkillDamage.Physical, Area = SkillArea.AroundSelf, Motion = SkillMotion.Spin,
                Radius = 3f, Power = L(140f, 40f), CastTime = 0.5f, SpCost = L(10f, 4f), Cooldown = 0.5f,
                Status = StatusEffect.Stun, StatusChance = L(5f, 5f), StatusDuration = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "soul_breaker", Name = "Soul Breaker", Job = JobId.ShadowWalker, MaxLevel = 10, Requires = Req("phantom_barrage", 1),
                Description = "Ragnarok's Soul Destroyer: a thrown blade that strikes body and spirit from range.",
                IconLabel = "SDS", IconColorHex = "#4A235A",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Shoot, Projectile = true, NeverMiss = true,
                Range = 9f, Power = L(220f, 60f), CastTime = 0.5f, SpCost = L(20f, 5f), Cooldown = 1f,
            });

            // ---------------------------------------------------------------- Berserker (Knight), Einherjar (Lord Knight)
            Register(new SkillDefinition
            {
                Id = "riposte", Name = "Riposte", Job = JobId.Berserker, MaxLevel = 5, Requires = Req("battle_frenzy", 1),
                Description = "Ragnarok's Counter Attack: read the next blow; your next attack is a sure critical.",
                IconLabel = "CNT", IconColorHex = "#B03A2E",
                Target = SkillTarget.Self, BuffId = SkillBuffs.Riposte, Motion = SkillMotion.Buff,
                BuffDuration = L(2f, 1f), BuffCharges = 1f, SpCost = 3f, Cooldown = 3f,
            });
            Register(new SkillDefinition
            {
                Id = "valhallas_edge", Name = "Valhalla's Edge", Job = JobId.Einherjar, MaxLevel = 5, Requires = Req("reckless_edge", 5),
                Description = "Ragnarok's Aura Blade: the blade burns with Valhalla's light, +20 ATK per level.",
                IconLabel = "AUB", IconColorHex = "#E59866",
                Target = SkillTarget.Self, BuffId = SkillBuffs.ValhallasEdge, Motion = SkillMotion.Buff, BuffDuration = L(40f, 20f), SpCost = L(18f, 8f),
            });
            Register(new SkillDefinition
            {
                Id = "parry", Name = "Parry", Job = JobId.Einherjar, MaxLevel = 10, Requires = Req("battle_frenzy", 3),
                Description = "Ragnarok's Parrying: turn blows aside with the greatsword, +5% block chance per level.",
                IconLabel = "PAR", IconColorHex = "#99A3A4", Weapons = WeaponMask.TwoHandSword,
                Target = SkillTarget.Self, BuffId = SkillBuffs.Parry, Motion = SkillMotion.Buff, BuffDuration = L(15f, 5f), SpCost = 50f,
            });
            Register(new SkillDefinition
            {
                Id = "berserkergang", Name = "Berserkergang", Job = JobId.Einherjar, MaxLevel = 1, Requires = Req("valhallas_edge", 1),
                Description = "Ragnarok's Frenzy, in the berserkers' own word: double Max HP, +30% ASPD, nothing staggers you. No items.",
                IconLabel = "BSK", IconColorHex = "#922B21",
                Target = SkillTarget.Self, BuffId = SkillBuffs.Berserkergang, Motion = SkillMotion.Buff, SpCost = 100f, Cooldown = 60f,
            });
            Register(new SkillDefinition
            {
                Id = "gungnir_thrust", Name = "Gungnir Thrust", Job = JobId.Einherjar, MaxLevel = 5, Requires = Req("cleaving_strike", 5),
                Description = "Ragnarok's Spiral Pierce, after Odin's spear: five spiralling thrusts that never miss.",
                IconLabel = "SPP", IconColorHex = "#C0392B", Weapons = WeaponMask.Spear,
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Thrust, NeverMiss = true,
                Range = 4f, Power = L(80f, 20f), Hits = 5, HitInterval = 0.1f, CastTime = 0.6f, SpCost = L(18f, 2f), Cooldown = 1f, PoiseMultiplier = 1.2f,
            });

            // ---------------------------------------------------------------- Guardian (Crusader), Valkyrie (Paladin)
            Register(new SkillDefinition
            {
                Id = "shield_bash", Name = "Shield Bash", Job = JobId.Guardian, MaxLevel = 5, Requires = Req("guardians_oath", 3),
                Description = "Ragnarok's Shield Charge: ram a foe with your shield. Knocks back and can stun.",
                IconLabel = "SHC", IconColorHex = "#717D7E",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Thrust,
                Range = 1.4f, Power = L(120f, 20f), Knockback = 1f, SpCost = 10f, PoiseMultiplier = 2f,
                Status = StatusEffect.Stun, StatusChance = L(20f, 5f), StatusDuration = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "spear_of_light", Name = "Spear of Light", Job = JobId.Valkyrie, MaxLevel = 5, Requires = Req("sacred_cross", 5),
                Description = "Ragnarok's Pressure (Gloria Domini): a holy lance of light that ignores armour and never misses.",
                IconLabel = "PRE", IconColorHex = "#F9E79F",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Cast, NeverMiss = true, IgnoreDefense = true,
                Element = Element.Holy, UseWeaponElement = false,
                Range = 9f, Power = L(200f, 50f), CastTime = 2f, SpCost = L(30f, 5f), Cooldown = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "shield_barrage", Name = "Shield Barrage", Job = JobId.Valkyrie, MaxLevel = 5, Requires = Req("shield_bash", 3),
                Description = "Ragnarok's Shield Chain: five shield blows thrown in a chain.",
                IconLabel = "SHN", IconColorHex = "#AEB6BF",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Physical, Motion = SkillMotion.Thrust,
                Range = 3f, Power = L(60f, 15f), Hits = 5, HitInterval = 0.1f, CastTime = 1f, SpCost = L(20f, 3f), PoiseMultiplier = 1.2f,
            });

            // ---------------------------------------------------------------- Mystic (Mage): the bolts (Muspel Bolt, Frost Spike, Thunder Rune) are in SkillCatalog.Mystic
            Register(new SkillDefinition
            {
                Id = "surtrs_orb", Name = "Surtr's Orb", Job = JobId.Mystic, MaxLevel = 10, Requires = Req("muspel_bolt", 4),
                Description = "Ragnarok's Fire Ball: a ball of the fire giant's flame that bursts over everyone near the target.",
                IconLabel = "FBL", IconColorHex = "#DC7633",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Area = SkillArea.AroundTarget, Motion = SkillMotion.Cast, Projectile = true,
                Element = Element.Fire, UseWeaponElement = false,
                Range = 9f, Radius = 2.5f, Power = L(110f, 10f), CastTime = 1.5f, SpCost = 25f, AfterCastDelay = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "troll_stone", Name = "Troll-Stone", Job = JobId.Mystic, MaxLevel = 10,
                Description = "Ragnarok's Stone Curse: the dawn-curse that turns trolls to stone.",
                IconLabel = "STN", IconColorHex = "#7F8C8D",
                Target = SkillTarget.Enemy, Motion = SkillMotion.Cast, Range = 3f, SpCost = L(25f, -1f),
                Status = StatusEffect.StoneCurse, StatusChance = L(24f, 4f), StatusDuration = 10f,
            });
            Register(new SkillDefinition
            {
                Id = "seidr_coat", Name = "Seiðr Coat", Job = JobId.Mystic, MaxLevel = 5, Requires = Req("mana_focus", 3),
                Description = "Ragnarok's Energy Coat: a coat of seidr softens every blow, 10% less damage taken, +2% per level.",
                IconLabel = "ENC", IconColorHex = "#5DADE2",
                Target = SkillTarget.Self, BuffId = SkillBuffs.SeidrCoat, Motion = SkillMotion.Buff, BuffDuration = 300f, SpCost = 30f,
            });

            // ---------------------------------------------------------------- Devotee (Acolyte)
            Register(new SkillDefinition
            {
                Id = "heavy_limbs", Name = "Heavy Limbs", Job = JobId.Devotee, MaxLevel = 10, Requires = Req("swift_wind", 1),
                Description = "Ragnarok's Decrease AGI: a foe's limbs turn to lead, slowing its steps and blows.",
                IconLabel = "DAG", IconColorHex = "#5B2C6F",
                Target = SkillTarget.Enemy, Motion = SkillMotion.Cast, Range = 9f, SpCost = L(15f, 2f),
                DebuffId = SkillBuffs.HeavyLimbs, DebuffDuration = L(20f, 2f), DebuffChance = L(42f, 2f),
            });

            // ---------------------------------------------------------------- Gothi (Priest)
            Register(new SkillDefinition
            {
                Id = "return_from_hel", Name = "Return from Hel", Job = JobId.Gothi, MaxLevel = 4, Requires = Req("purify", 1),
                Description = "Ragnarok's Resurrection: call a fallen ally back from Hel's road, with 10% of their HP and more per level.",
                IconLabel = "RES", IconColorHex = "#FCF3CF",
                Target = SkillTarget.Friend, Special = SkillSpecial.Resurrect, Motion = SkillMotion.Cast,
                Range = 9f, CastTime = L(6f, -1f), SpCost = 60f, AfterCastDelay = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "galdr_of_eir", Name = "Galdr of Eir", Job = JobId.Gothi, MaxLevel = 5, Requires = Req("eirs_blessing", 3),
                Description = "Ragnarok's Magnificat: a healer's chant, +6 SP regen per level.",
                IconLabel = "MAG", IconColorHex = "#AED6F1",
                Target = SkillTarget.Self, BuffId = SkillBuffs.GaldrOfEir, Motion = SkillMotion.Cast, BuffDuration = L(30f, 15f), CastTime = 4f, SpCost = 40f,
            });
            Register(new SkillDefinition
            {
                Id = "glory_of_baldr", Name = "Glory of Baldr", Job = JobId.Gothi, MaxLevel = 5, Requires = Req("galdr_of_eir", 3),
                Description = "Ragnarok's Gloria: the shining god's favour, +10 LUK.",
                IconLabel = "GLO", IconColorHex = "#F9E79F",
                Target = SkillTarget.Self, BuffId = SkillBuffs.GloryOfBaldr, Motion = SkillMotion.Cast, BuffDuration = L(10f, 5f), SpCost = 20f,
            });
            Register(new SkillDefinition
            {
                Id = "svalinns_shield", Name = "Svalinn's Shield", Job = JobId.Gothi, MaxLevel = 10, Requires = Req("divine_shelter", 3),
                Description = "Ragnarok's Kyrie Eleison: the shield that stands before the sun absorbs the next 3,000 damage to you or an ally.",
                IconLabel = "KYR", IconColorHex = "#F7DC6F",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.SvalinnsShield, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = L(30f, 10f), CastTime = 2f, SpCost = L(20f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "tyrs_hand", Name = "Tyr's Hand", Job = JobId.Gothi, MaxLevel = 5,
                Description = "Ragnarok's Impositio Manus: Tyr lays his hand on you or an ally, +5 ATK per level.",
                IconLabel = "IMP", IconColorHex = "#D35400",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.TyrsHand, Motion = SkillMotion.Cast, Range = 9f, BuffDuration = 60f, SpCost = L(13f, 3f),
            });
            Register(new SkillDefinition
            {
                Id = "galdr_of_haste", Name = "Galdr of Haste", Job = JobId.Gothi, MaxLevel = 3, Requires = Req("tyrs_hand", 2),
                Description = "Ragnarok's Suffragium: you or an ally cast 15% faster per level.",
                IconLabel = "SUF", IconColorHex = "#85C1E9",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.GaldrOfHaste, Motion = SkillMotion.Cast, Range = 9f, BuffDuration = 20f, SpCost = 8f,
            });
            Register(new SkillDefinition
            {
                Id = "silencing_rune", Name = "Silencing Rune", Job = JobId.Gothi, MaxLevel = 10, Requires = Req("holy_light", 1),
                Description = "Ragnarok's Lex Divina: a rune of silence. The target can't use skills.",
                IconLabel = "LXD", IconColorHex = "#BB8FCE",
                Target = SkillTarget.Enemy, Motion = SkillMotion.Cast, Range = 9f, SpCost = L(20f, -1f),
                Status = StatusEffect.Silence, StatusChance = L(60f, 4f), StatusDuration = L(10f, 2f),
            });
            Register(new SkillDefinition
            {
                Id = "norns_doom", Name = "Norns' Doom", Job = JobId.Gothi, MaxLevel = 1, Requires = Req("silencing_rune", 5),
                Description = "Ragnarok's Lex Aeterna: the Norns mark a foe; for a few seconds it takes 50% more damage.",
                IconLabel = "LXA", IconColorHex = "#922B21",
                Target = SkillTarget.Enemy, Motion = SkillMotion.Cast, Range = 9f, SpCost = 10f,
                DebuffId = SkillBuffs.NornsDoom, DebuffDuration = 6f,
            });
            Register(new SkillDefinition
            {
                Id = "banishing_rite", Name = "Banishing Rite", Job = JobId.Gothi, MaxLevel = 10, Requires = Req("holy_light", 3),
                Description = "Ragnarok's Turn Undead: drive the dead back to Hel. Four times the damage against the Undead and Demons.",
                IconLabel = "TUN", IconColorHex = "#FEF9E7",
                Target = SkillTarget.Enemy, Damage = SkillDamage.Magic, Motion = SkillMotion.Cast, Element = Element.Holy, UseWeaponElement = false,
                BonusVsUndeadPercent = 300f, Range = 9f, Power = L(110f, 20f), CastTime = 1f, SpCost = 20f, AfterCastDelay = 1f,
            });
            Register(new SkillDefinition
            {
                Id = "rampart_of_runar", Name = "Rampart of Rúnar", Job = JobId.Gothi, MaxLevel = 10, Requires = Req("divine_shelter", 3),
                Description = "Ragnarok's Safety Wall: a rune-wall around you or an ally blocks the next melee hits (one per level).",
                IconLabel = "SFW", IconColorHex = "#D7BDE2",
                Target = SkillTarget.Friend, BuffId = SkillBuffs.RampartOfRunar, Motion = SkillMotion.Cast,
                Range = 9f, BuffDuration = L(5f, 5f), BuffCharges = L(1f, 1f), CastTime = L(4f, -0.3f), SpCost = L(30f, 3f),
            });

            // ---------------------------------------------------------------- High Gothi (High Priest)
            Register(new SkillDefinition
            {
                Id = "hof_meditation", Name = "Hof Meditation", Job = JobId.HighGothi, MaxLevel = 10, Passive = true, Requires = Req("galdr_of_eir", 3),
                Description = "Ragnarok's Meditatio: +1% Max SP and +3 SP regen per level.",
                IconLabel = "MED", IconColorHex = "#D6EAF8",
                PassivePerLevel = new StatModifiers { MaxSpPercent = 1f, SpRegenFlat = 3 },
            });
            Register(new SkillDefinition
            {
                Id = "light_of_baldr", Name = "Light of Baldr", Job = JobId.HighGothi, MaxLevel = 10, Requires = Req("banishing_rite", 3),
                Description = "Ragnarok's Judex: crosses of Baldr's light fall on a spot, three times.",
                IconLabel = "JDX", IconColorHex = "#FCF3CF",
                Target = SkillTarget.Ground, Damage = SkillDamage.Magic, Area = SkillArea.AtGround, Motion = SkillMotion.Cast,
                Element = Element.Holy, UseWeaponElement = false, BonusVsUndeadPercent = 50f,
                Range = 9f, Radius = 2.5f, Power = L(160f, 40f), Hits = 3, HitInterval = 0.25f, CastTime = L(1.5f, -0.1f), SpCost = L(30f, 4f),
            });
            Register(new SkillDefinition
            {
                Id = "hof_sanctum", Name = "Hof Sanctum", Job = JobId.HighGothi, MaxLevel = 5, Requires = Req("glory_of_baldr", 2),
                Description = "Ragnarok's Basilica: consecrate yourself as a sanctum, taking half damage (and 5% less per level).",
                IconLabel = "BAS", IconColorHex = "#FAD7A0",
                Target = SkillTarget.Self, BuffId = SkillBuffs.HofSanctum, Motion = SkillMotion.Cast,
                BuffDuration = L(8f, 2f), CastTime = 5f, SpCost = L(80f, 10f), Cooldown = 30f,
            });
        }

        /// <summary>An Outlaw strip: a chance to land a debuff that weakens one part of the foe.</summary>
        private static void Divest(string id, string name, string description, string icon, string color, string debuffId, SkillRequirement[] requires)
        {
            Register(new SkillDefinition
            {
                Id = id, Name = name, Job = JobId.Outlaw, MaxLevel = 5, Requires = requires,
                Description = description, IconLabel = icon, IconColorHex = color,
                Target = SkillTarget.Enemy, Motion = SkillMotion.Swing, Range = 1.5f,
                DebuffId = debuffId, DebuffDuration = L(15f, 5f), DebuffChance = L(15f, 5f), SpCost = L(12f, 2f),
            });
        }
    }
}
