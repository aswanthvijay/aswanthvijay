using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Monsters;

namespace Runeheir.Homunculi
{
    /// <summary>Ragnarok's four homunculi, as the living companions a Brewmaster weaves from an Embryo.</summary>
    public enum HomunculusKind
    {
        /// <summary>Ragnarok's Lif: a gentle healer.</summary>
        Lif = 0,

        /// <summary>Ragnarok's Amistr: a bear-spirit that holds the line.</summary>
        Bjarki = 1,

        /// <summary>Ragnarok's Filir: a swift raptor.</summary>
        Valfugl = 2,

        /// <summary>Ragnarok's Vanilmirth: a wild spirit of seiðr.</summary>
        Gandr = 3,
    }

    /// <summary>What a homunculus is: its look, its build against a monster of its level, and the skills it learns.</summary>
    public sealed class HomunculusDefinition
    {
        public HomunculusKind Kind;
        public string Name;
        public string Title;
        public string Description;

        /// <summary>Multiples of a normal monster of its level.</summary>
        public float HpFactor = 1f;

        public float AtkFactor = 1f;
        public float DefFactor = 1f;

        public float MoveSpeed = 4.5f;
        public float AttackRange = 0.9f;
        public float AttackInterval = 1.2f;
        public bool MagicAttacks;

        public MonsterShape Shape;
        public string ColorHex;
        public float Scale = 0.75f;
        public Element Element = Element.Neutral;
        public Race Race = Race.Formless;

        /// <summary>Skills, each from a level on.</summary>
        public List<(int Level, MonsterSkill Skill)> Skills = new List<(int, MonsterSkill)>();
    }

    /// <summary>One character's homunculus (one at a time, kept for life).</summary>
    [Serializable]
    public sealed class HomunculusState
    {
        public HomunculusKind Kind;
        public string Name;
        public int Level = 1;
        public long Exp;

        /// <summary>HP when last seen (0 = full).</summary>
        public int Hp;

        /// <summary>Called out (it follows its master between maps).</summary>
        public bool Out;

        /// <summary>Seconds of play before it can be called again after falling (0 = ready).</summary>
        public float RestSeconds;

        /// <summary>Saves made before homunculi existed load an empty state: no name means no homunculus.</summary>
        public bool Exists => !string.IsNullOrEmpty(Name);
    }

    /// <summary>Weaving, calling, resting, growing and reviving homunculi (Ragnarok's Call Homunculus, Vaporize, Resurrect).</summary>
    public static class HomunculusRules
    {
        public const int MaxLevel = 255;
        public const string EmbryoItemId = "xr7142";

        /// <summary>A fallen homunculus rests this long before it can be called again (a Lifeweaver can revive it at once).</summary>
        public const float RestAfterFalling = 120f;

        /// <summary>Its share of its master's base EXP (it needs a tenth of a character's EXP per level, so it keeps pace).</summary>
        public const float ExpShare = 0.1f;

        private static readonly Dictionary<HomunculusKind, HomunculusDefinition> Definitions = new Dictionary<HomunculusKind, HomunculusDefinition>
        {
            [HomunculusKind.Lif] = new HomunculusDefinition
            {
                Kind = HomunculusKind.Lif, Name = "Líf", Title = "the Healer",
                Description = "Named for the woman who outlives Ragnarök: frail in a fight, but she mends her master's wounds.",
                HpFactor = 0.9f, AtkFactor = 0.6f, Shape = MonsterShape.Biped, ColorHex = "#F5B7B1", Race = Race.DemiHuman, Element = Element.Holy,
                Skills =
                {
                    (1, new MonsterSkill { Id = "hom_healing_hands", Name = "Healing Hands", Kind = MonsterSkillKind.Heal, HealPercent = 12f, BelowHpPercent = 70f,
                        Radius = 12f, CastTime = 0.8f, Cooldown = 8f, ChancePercent = 90f }),
                    (15, new MonsterSkill { Id = "hom_urgent_escape", Name = "Urgent Escape", Kind = MonsterSkillKind.Buff, BuffId = MonsterBuffs.Frenzied,
                        BelowHpPercent = 50f, Cooldown = 40f, ChancePercent = 60f }),
                    (40, new MonsterSkill { Id = "hom_mental_charge", Name = "Mental Charge", Kind = MonsterSkillKind.Bolt, Range = 9f, Percent = 220f,
                        Magical = true, Element = Element.Holy, CastTime = 1f, Cooldown = 9f, ChancePercent = 40f }),
                },
            },
            [HomunculusKind.Bjarki] = new HomunculusDefinition
            {
                Kind = HomunculusKind.Bjarki, Name = "Bjarki", Title = "the Bear-Guard",
                Description = "A bear-spirit like Bödvar Bjarki's: slow, tough, and between its master and harm.",
                HpFactor = 1.6f, AtkFactor = 0.9f, DefFactor = 1.5f, MoveSpeed = 4f, Shape = MonsterShape.Quadruped, ColorHex = "#7E5109",
                Scale = 1f, Race = Race.Beast, Element = Element.Earth,
                Skills =
                {
                    (1, new MonsterSkill { Id = "hom_maul", Name = "Maul", Kind = MonsterSkillKind.Strike, Percent = 200f, Cooldown = 6f, ChancePercent = 45f }),
                    (15, new MonsterSkill { Id = "hom_bulwark", Name = "Bulwark", Kind = MonsterSkillKind.Buff, BuffId = MonsterBuffs.ShieldWall,
                        BelowHpPercent = 80f, Cooldown = 30f, ChancePercent = 60f }),
                    (40, new MonsterSkill { Id = "hom_blood_lust", Name = "Blood Lust", Kind = MonsterSkillKind.Buff, BuffId = MonsterBuffs.Enraged,
                        Cooldown = 60f, ChancePercent = 50f }),
                },
            },
            [HomunculusKind.Valfugl] = new HomunculusDefinition
            {
                Kind = HomunculusKind.Valfugl, Name = "Valfugl", Title = "the Swift",
                Description = "A falcon of the slain-field: quick wings, quicker talons.",
                HpFactor = 0.8f, AtkFactor = 1.25f, MoveSpeed = 5.5f, AttackInterval = 0.9f, Shape = MonsterShape.Flyer, ColorHex = "#5D6D7E",
                Scale = 0.8f, Race = Race.Beast, Element = Element.Wind,
                Skills =
                {
                    (1, new MonsterSkill { Id = "hom_moonlight", Name = "Moonlight", Kind = MonsterSkillKind.Strike, Percent = 240f, Cooldown = 5f, ChancePercent = 45f }),
                    (15, new MonsterSkill { Id = "hom_flitting", Name = "Flitting", Kind = MonsterSkillKind.Buff, BuffId = MonsterBuffs.Frenzied,
                        Cooldown = 40f, ChancePercent = 60f }),
                    (40, new MonsterSkill { Id = "hom_sbr44", Name = "Diving Talon", Kind = MonsterSkillKind.Strike, Percent = 420f, CastTime = 0.6f,
                        Cooldown = 20f, ChancePercent = 35f }),
                },
            },
            [HomunculusKind.Gandr] = new HomunculusDefinition
            {
                Kind = HomunculusKind.Gandr, Name = "Gandr", Title = "the Wild Spirit",
                Description = "A gandr, a sorcerer's spirit: fickle, but its bolts land hard.",
                HpFactor = 0.85f, AtkFactor = 1.1f, AttackRange = 6f, MagicAttacks = true, Shape = MonsterShape.Blob, ColorHex = "#8E44AD",
                Scale = 0.75f, Race = Race.Formless, Element = Element.Ghost,
                Skills =
                {
                    (1, new MonsterSkill { Id = "hom_caprice_fire", Name = "Caprice", Kind = MonsterSkillKind.Bolt, Range = 9f, Percent = 200f, Magical = true,
                        Element = Element.Fire, CastTime = 0.6f, Cooldown = 6f, ChancePercent = 35f }),
                    (1, new MonsterSkill { Id = "hom_caprice_frost", Name = "Caprice", Kind = MonsterSkillKind.Bolt, Range = 9f, Percent = 200f, Magical = true,
                        Element = Element.Water, CastTime = 0.6f, Cooldown = 6f, ChancePercent = 35f }),
                    (15, new MonsterSkill { Id = "hom_chaotic_blessings", Name = "Chaotic Blessings", Kind = MonsterSkillKind.Heal, HealPercent = 10f,
                        BelowHpPercent = 60f, Radius = 12f, Cooldown = 15f, ChancePercent = 50f }),
                    (40, new MonsterSkill { Id = "hom_seidr_storm", Name = "Seiðr Storm", Kind = MonsterSkillKind.Area, Range = 9f, Radius = 3f, Percent = 260f,
                        Magical = true, Element = Element.Wind, CastTime = 1.2f, Cooldown = 18f, ChancePercent = 40f }),
                },
            },
        };

        public static HomunculusDefinition Get(HomunculusKind kind) => Definitions[kind];

        public static IEnumerable<HomunculusDefinition> All => Definitions.Values;

        /// <summary>Weaves a new homunculus from an Embryo (the kind is the Norns' choice, from <paramref name="roll"/>).</summary>
        public static HomunculusState Weave(CharacterRecord record, int roll)
        {
            var kind = (HomunculusKind)(Math.Abs(roll) % 4);
            record.Homunculus = new HomunculusState { Kind = kind, Name = Get(kind).Name, Level = 1, Out = true };
            return record.Homunculus;
        }

        public static HomunculusState Of(CharacterRecord record)
        {
            return record?.Homunculus != null && record.Homunculus.Exists ? record.Homunculus : null;
        }

        /// <summary>EXP to the next level: a tenth of a character's, so with its share it grows about as fast as its master.</summary>
        public static long ExpToNext(int level)
        {
            return Math.Max(10L, (long)Math.Round(ExperienceTable.BaseExpToNext(Math.Min(level, MaxLevel - 1)) * ExpShare));
        }

        /// <summary>Adds EXP; returns the levels gained.</summary>
        public static int GainExp(HomunculusState state, long amount)
        {
            if (state == null || amount <= 0 || state.Level >= MaxLevel)
            {
                return 0;
            }

            int gained = 0;
            state.Exp += amount;
            while (state.Level < MaxLevel && state.Exp >= ExpToNext(state.Level))
            {
                state.Exp -= ExpToNext(state.Level);
                state.Level++;
                gained++;
            }

            if (state.Level >= MaxLevel)
            {
                state.Exp = 0;
            }

            return gained;
        }

        /// <summary>
        /// The homunculus as a monster that fights for its master: a normal monster of its level (the median of the bestiary
        /// near that level) times its kind's build, with the skills it has learned.
        /// </summary>
        public static MonsterDefinition Build(HomunculusState state)
        {
            var kind = Get(state.Kind);
            var reference = ReferenceAt(state.Level);
            var monster = new MonsterDefinition
            {
                Id = "homunculus_" + state.Kind.ToString().ToLowerInvariant(),
                Name = $"{state.Name} (Lv {state.Level})",
                Level = state.Level,
                MaxHp = Math.Max(30, (int)(reference.Hp * kind.HpFactor)),
                AtkMin = Math.Max(5, (int)(reference.Atk * kind.AtkFactor * 0.85f)),
                AtkMax = Math.Max(6, (int)(reference.Atk * kind.AtkFactor * 1.15f)),
                Def = (int)(reference.Def * kind.DefFactor),
                Mdef = reference.Mdef,
                Hit = reference.Hit + state.Level / 2,
                Flee = reference.Flee,
                Element = kind.Element,
                AttackElement = kind.Element,
                Race = kind.Race,
                Size = Size.Small,
                MoveSpeed = kind.MoveSpeed,
                AttackRange = kind.AttackRange,
                AttackInterval = kind.AttackInterval,
                SummonOnly = true,
                Shape = kind.Shape,
                ColorHex = kind.ColorHex,
                Scale = kind.Scale,
            };
            foreach (var (level, skill) in kind.Skills)
            {
                if (state.Level >= level)
                {
                    monster.Skills.Add(skill);
                }
            }

            return monster;
        }

        private struct Reference
        {
            public int Hp;
            public int Atk;
            public int Def;
            public int Mdef;
            public int Hit;
            public int Flee;
        }

        private static readonly Dictionary<int, Reference> References = new Dictionary<int, Reference>();

        /// <summary>The bestiary's median normal monster within a few levels of <paramref name="level"/>.</summary>
        private static Reference ReferenceAt(int level)
        {
            int bucket = Math.Max(1, Math.Min(MaxLevel, level));
            if (References.TryGetValue(bucket, out var cached))
            {
                return cached;
            }

            var near = new List<MonsterDefinition>();
            for (int window = 4; near.Count < 5 && window <= 64; window *= 2)
            {
                near.Clear();
                foreach (var monster in MonsterCatalog.All)
                {
                    if (monster.Rank == MonsterRank.Normal && !monster.Immortal && !monster.Passive && monster.MaxHp > 1
                        && Math.Abs(monster.Level - bucket) <= window)
                    {
                        near.Add(monster);
                    }
                }
            }

            int Median(Func<MonsterDefinition, int> pick)
            {
                if (near.Count == 0)
                {
                    return 0;
                }

                var values = near.ConvertAll(m => pick(m));
                values.Sort();
                return values[values.Count / 2];
            }

            var reference = new Reference
            {
                Hp = Math.Max(40, Median(m => m.MaxHp)),
                Atk = Math.Max(8, Median(m => (m.AtkMin + m.AtkMax) / 2)),
                Def = Median(m => m.Def),
                Mdef = Median(m => m.Mdef),
                Hit = Math.Max(5, Median(m => m.Hit)),
                Flee = Math.Max(1, Median(m => m.Flee)),
            };
            References[bucket] = reference;
            return reference;
        }
    }
}
