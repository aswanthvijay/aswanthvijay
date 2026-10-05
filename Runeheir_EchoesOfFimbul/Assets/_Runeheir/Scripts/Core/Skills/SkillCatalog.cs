using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Jobs;

namespace Runeheir.Skills
{
    public enum SkillTarget
    {
        /// <summary>Fires on yourself immediately.</summary>
        Self = 0,

        /// <summary>Click a hostile target.</summary>
        Enemy = 1,

        /// <summary>Click yourself or a friendly target (Heal).</summary>
        Friend = 2,

        /// <summary>Click a spot on the ground.</summary>
        Ground = 3,
    }

    public enum SkillEffect
    {
        PhysicalStrike = 0,
        MagicStrike = 1,
        PhysicalAreaAroundSelf = 2,
        MagicAreaAtGround = 3,
        Heal = 4,
        Buff = 5,
        Dash = 6,
        FistOfOdin = 7,
    }

    /// <summary>
    /// Data for one skill. Phase 2 keeps skills as code-defined data (easy to diff/test);
    /// Phase 3 can move them to ScriptableObjects without changing the runtime caster.
    /// </summary>
    public sealed class SkillDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public string IconLabel;
        public string IconColorHex = "#5D6D7E";

        /// <summary>The job that learns it; every later job in that branch inherits it.</summary>
        public JobId Job;

        public SkillTarget Target;
        public SkillEffect Effect;

        /// <summary>Edge-to-edge cast range in meters (ignored for Self).</summary>
        public float Range = 1.2f;

        public int SpCost;

        /// <summary>Base variable cast time in seconds, scaled by DEX (150 DEX = instant).</summary>
        public float CastTime;

        /// <summary>Global delay after the cast before any other skill.</summary>
        public float AfterCastDelay = 0.3f;

        /// <summary>Per-skill cooldown in seconds.</summary>
        public float Cooldown;

        /// <summary>Damage percent per hit (300 = 300% ATK/MATK).</summary>
        public int Power = 100;

        public int Hits = 1;

        /// <summary>Seconds between hits/waves (0 = all at once).</summary>
        public float HitInterval;

        public float Radius;
        public Element Element = Element.Neutral;

        /// <summary>Use the weapon's element instead of <see cref="Element"/>.</summary>
        public bool UseWeaponElement = true;

        public string BuffId;
        public StatusEffect Status;
        public float StatusChance;
        public float StatusDuration;
        public float Knockback;

        /// <summary>Heal skill level used in the Ragnarok heal formula.</summary>
        public int HealLevel;

        /// <summary>Flat heal (First Aid).</summary>
        public int FlatHeal;

        public bool IsMagic => Effect == SkillEffect.MagicStrike || Effect == SkillEffect.MagicAreaAtGround;
    }

    /// <summary>Phase 2 skill set: one starter skill per first job plus the GDD signature skills.</summary>
    public static class SkillCatalog
    {
        private static readonly Dictionary<string, SkillDefinition> ById = new Dictionary<string, SkillDefinition>();
        private static readonly List<SkillDefinition> Ordered = new List<SkillDefinition>();

        static SkillCatalog()
        {
            // ---- Initiate
            Register(new SkillDefinition
            {
                Id = "first_aid", Name = "First Aid", Job = JobId.Initiate,
                Description = "Bandage your wounds for a small heal.",
                IconLabel = "FA", IconColorHex = "#27AE60",
                Target = SkillTarget.Self, Effect = SkillEffect.Heal,
                SpCost = 3, FlatHeal = 25, AfterCastDelay = 0.5f,
            });

            // ---- First jobs
            Register(new SkillDefinition
            {
                Id = "bash", Name = "Bash", Job = JobId.Warrior,
                Description = "A heavy blow dealing 300% ATK.",
                IconLabel = "BSH", IconColorHex = "#A93226",
                Target = SkillTarget.Enemy, Effect = SkillEffect.PhysicalStrike,
                Range = 1.4f, SpCost = 8, Power = 300, AfterCastDelay = 0.3f,
            });
            Register(new SkillDefinition
            {
                Id = "twin_fang", Name = "Twin Fang", Job = JobId.Scout,
                Description = "Two quick stabs of 150% ATK each.",
                IconLabel = "TF", IconColorHex = "#1E8449",
                Target = SkillTarget.Enemy, Effect = SkillEffect.PhysicalStrike,
                Range = 1.2f, SpCost = 10, Power = 150, Hits = 2, HitInterval = 0.12f, AfterCastDelay = 0.3f,
            });
            Register(new SkillDefinition
            {
                Id = "muspel_bolt", Name = "Muspel Bolt", Job = JobId.Mystic,
                Description = "Three bolts of Muspelheim fire, 150% MATK each.",
                IconLabel = "MB", IconColorHex = "#E67E22",
                Target = SkillTarget.Enemy, Effect = SkillEffect.MagicStrike,
                Range = 9f, SpCost = 18, CastTime = 1.8f, Power = 150, Hits = 3, HitInterval = 0.15f,
                Element = Element.Fire, UseWeaponElement = false, AfterCastDelay = 0.8f,
            });
            Register(new SkillDefinition
            {
                Id = "eirs_blessing", Name = "Eir's Blessing", Job = JobId.Devotee,
                Description = "Heal yourself or an ally (Heal Lv 10 formula).",
                IconLabel = "HL", IconColorHex = "#58D68D",
                Target = SkillTarget.Friend, Effect = SkillEffect.Heal,
                Range = 9f, SpCost = 40, HealLevel = 10, AfterCastDelay = 1f,
            });

            // ---- Einherjar (GDD signature)
            Register(new SkillDefinition
            {
                Id = "vortex_cleave", Name = "Vortex Cleave", Job = JobId.Einherjar,
                Description = "360° greatsword sweep: two hits of 400% ATK. Launched enemies crash into others nearby (200% to both).",
                IconLabel = "VC", IconColorHex = "#CB4335",
                Target = SkillTarget.Self, Effect = SkillEffect.PhysicalAreaAroundSelf,
                Radius = 3f, SpCost = 20, CastTime = 0.6f, Power = 400, Hits = 2, HitInterval = 0.15f,
                Knockback = 2.5f, AfterCastDelay = 0.6f,
            });
            Register(new SkillDefinition
            {
                Id = "two_hand_surge", Name = "Two-Hand Surge", Job = JobId.Einherjar,
                Description = "+7 ASPD and attack recovery canceling for 60s.",
                IconLabel = "THS", IconColorHex = "#D35400",
                Target = SkillTarget.Self, Effect = SkillEffect.Buff, BuffId = BuffCatalog.TwoHandSurge,
                SpCost = 14, AfterCastDelay = 0.3f,
            });
            Register(new SkillDefinition
            {
                Id = "rage_of_thor", Name = "Rage of Thor", Job = JobId.Einherjar,
                Description = "Max HP x3, ASPD 195, hyper-armor; items locked for 30s.",
                IconLabel = "ROT", IconColorHex = "#C0392B",
                Target = SkillTarget.Self, Effect = SkillEffect.Buff, BuffId = BuffCatalog.RageOfThor,
                SpCost = 60, Cooldown = 120f, AfterCastDelay = 0.5f,
            });

            // ---- Shadow Walker
            Register(new SkillDefinition
            {
                Id = "phantom_barrage", Name = "Phantom Barrage", Job = JobId.ShadowWalker,
                Description = "Rapid 8-hit strike (110% ATK each) with a guaranteed stun.",
                IconLabel = "PB", IconColorHex = "#5B2C6F",
                Target = SkillTarget.Enemy, Effect = SkillEffect.PhysicalStrike,
                Range = 1.2f, SpCost = 25, Power = 110, Hits = 8, HitInterval = 0.06f,
                Status = StatusEffect.Stun, StatusChance = 100f, StatusDuration = 2f, AfterCastDelay = 1.2f,
            });
            Register(new SkillDefinition
            {
                Id = "miasma_weapon", Name = "Miasma Weapon", Job = JobId.ShadowWalker,
                Description = "Poison your blades: physical damage x4 for 40s.",
                IconLabel = "MIA", IconColorHex = "#6C3483",
                Target = SkillTarget.Self, Effect = SkillEffect.Buff, BuffId = BuffCatalog.MiasmaWeapon,
                SpCost = 60, Cooldown = 60f, AfterCastDelay = 0.5f,
            });

            // ---- Archmage
            Register(new SkillDefinition
            {
                Id = "glacial_tempest", Name = "Glacial Tempest", Job = JobId.Archmage,
                Description = "Blizzard: 5 waves of 200% MATK, 35% chance to freeze per wave.",
                IconLabel = "GT", IconColorHex = "#5DADE2",
                Target = SkillTarget.Ground, Effect = SkillEffect.MagicAreaAtGround,
                Range = 10f, Radius = 4.5f, SpCost = 78, CastTime = 6f, Power = 200, Hits = 5, HitInterval = 0.5f,
                Element = Element.Water, UseWeaponElement = false,
                Status = StatusEffect.Freeze, StatusChance = 35f, StatusDuration = 6f, AfterCastDelay = 1.5f,
            });
            Register(new SkillDefinition
            {
                Id = "runic_aegis", Name = "Runic Aegis", Job = JobId.Archmage,
                Description = "Runic dome that blocks the next 10 physical melee strikes.",
                IconLabel = "AEG", IconColorHex = "#2E86C1",
                Target = SkillTarget.Friend, Effect = SkillEffect.Buff, BuffId = BuffCatalog.RunicAegis,
                Range = 9f, SpCost = 35, CastTime = 1.5f, AfterCastDelay = 0.5f,
            });

            // ---- Champion
            Register(new SkillDefinition
            {
                Id = "fist_of_odin", Name = "Fist of Odin", Job = JobId.Champion,
                Description = "Drains ALL SP into one lethal punch: ATK x (8 + SP/10) + 1750.",
                IconLabel = "FOO", IconColorHex = "#F39C12",
                Target = SkillTarget.Enemy, Effect = SkillEffect.FistOfOdin,
                Range = 1.2f, SpCost = 10, CastTime = 1f, Cooldown = 5f, AfterCastDelay = 2f,
            });
            Register(new SkillDefinition
            {
                Id = "aether_snap", Name = "Aether Snap", Job = JobId.Champion,
                Description = "Instant-transmission dash up to 8m.",
                IconLabel = "AS", IconColorHex = "#48C9B0",
                Target = SkillTarget.Ground, Effect = SkillEffect.Dash,
                Range = 8f, SpCost = 12, Cooldown = 1.5f, AfterCastDelay = 0.2f,
            });
        }

        public static IReadOnlyList<SkillDefinition> All => Ordered;

        public static SkillDefinition Get(string id)
        {
            return id != null && ById.TryGetValue(id, out var skill) ? skill : null;
        }

        /// <summary>Skills a job can use: its own plus every ancestor's (Einherjar also has Bash).</summary>
        public static List<SkillDefinition> ForJob(JobId job)
        {
            var result = new List<SkillDefinition>();
            foreach (var skill in Ordered)
            {
                if (JobDatabase.IsSelfOrAncestor(skill.Job, job))
                {
                    result.Add(skill);
                }
            }

            return result;
        }

        public static bool CanUse(JobId job, string skillId)
        {
            var skill = Get(skillId);
            return skill != null && JobDatabase.IsSelfOrAncestor(skill.Job, job);
        }

        private static void Register(SkillDefinition skill)
        {
            ById[skill.Id] = skill;
            Ordered.Add(skill);
        }
    }
}
