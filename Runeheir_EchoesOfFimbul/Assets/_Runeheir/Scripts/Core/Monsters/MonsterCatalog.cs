using System;
using System.Collections.Generic;
using Runeheir.Combat;

namespace Runeheir.Monsters
{
    /// <summary>
    /// GDD §6 bestiary: the 35 Soul Card monsters (31 field and dungeon monsters, 4 mini-bosses and the 3 MVPs), Fenrir's
    /// summoned wolves and the training dummy. Data lives in MonsterCatalog.Fields.cs, .Dungeons.cs and .Bosses.cs.
    /// </summary>
    public static partial class MonsterCatalog
    {
        public const string TrainingDummy = "training_dummy";

        private static readonly Dictionary<string, MonsterDefinition> ById = new Dictionary<string, MonsterDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<MonsterDefinition> Ordered = new List<MonsterDefinition>();

        static MonsterCatalog()
        {
            Register(new MonsterDefinition
            {
                Id = TrainingDummy, Name = "Training Dummy", Level = 1, MaxHp = 1000000,
                Element = Element.Neutral, Race = Race.Formless, Size = Size.Medium,
                Stationary = true, Passive = true, Immortal = true, RespawnSeconds = 0f,
                Shape = MonsterShape.Dummy, ColorHex = "#A67C52", Scale = 1.1f,
            });

            RegisterPlains();
            RegisterWoodsAndFjord();
            RegisterSteppe();
            RegisterCatacombs();
            RegisterCaverns();
            RegisterMiniBosses();
            RegisterMvps();
        }

        public static IReadOnlyList<MonsterDefinition> All => Ordered;

        public static MonsterDefinition Get(string id)
        {
            return id != null && ById.TryGetValue(id, out var monster) ? monster : null;
        }

        /// <summary>
        /// Dead Branch: a random normal monster from anywhere in Midgard. Blood Branch: a random mini-boss or MVP
        /// (it brings its MVP rewards with it).
        /// </summary>
        public static MonsterDefinition PickForBranch(bool boss, IRandomSource random)
        {
            var pool = new List<MonsterDefinition>();
            foreach (var monster in Ordered)
            {
                if (IsBranchSummonable(monster) && monster.IsBoss == boss)
                {
                    pool.Add(monster);
                }
            }

            return pool.Count == 0 ? null : pool[Math.Min(pool.Count - 1, (int)(random.NextDouble() * pool.Count))];
        }

        public static bool IsBranchSummonable(MonsterDefinition monster)
        {
            return monster != null && !monster.Immortal && !monster.Stationary && !monster.SummonOnly;
        }

        private static DropEntry Drop(string itemId, float chancePercent)
        {
            return new DropEntry(itemId, chancePercent);
        }

        private static void Register(MonsterDefinition monster)
        {
            if (monster.IsBoss)
            {
                // Bosses respawn on the map's boss timer (BossTracker), never on the 8-second field timer.
                monster.RespawnSeconds = 0f;
                monster.LeashRange = Math.Max(monster.LeashRange, 40f);
                monster.AggroRange = Math.Max(monster.AggroRange, 11f);
            }

            ById[monster.Id] = monster;
            Ordered.Add(monster);
        }

        // ---------------------------------------------------------------- skills shared by several monsters
        private static MonsterSkill PackHowl(float cooldown = 30f, float chance = 25f)
        {
            return new MonsterSkill
            {
                Id = "pack_howl", Name = "Pack Howl", Kind = MonsterSkillKind.Buff, BuffId = MonsterBuffs.PackHowl, Radius = 10f,
                Cooldown = cooldown, ChancePercent = chance, Shout = "Awooo!",
            };
        }

        private static MonsterSkill Dive(float percent, float cooldown = 10f, float chance = 35f)
        {
            return new MonsterSkill
            {
                Id = "dive", Name = "Dive", Kind = MonsterSkillKind.Leap, MinRange = 4f, Range = 10f, Radius = 1.5f, CastTime = 0.5f,
                Percent = percent, Cooldown = cooldown, ChancePercent = chance,
            };
        }

        private static MonsterSkill SelfBuff(string id, string name, string buffId, float belowHp, float cooldown, float chance = 50f, string shout = null)
        {
            return new MonsterSkill
            {
                Id = id, Name = name, Kind = MonsterSkillKind.Buff, BuffId = buffId, BelowHpPercent = belowHp, Cooldown = cooldown,
                ChancePercent = chance, Shout = shout,
            };
        }

        private static MonsterSkill Summon(string id, string name, string monsterId, int count, float cooldown, float chance = 60f, int minPhase = 0, string shout = null)
        {
            return new MonsterSkill
            {
                Id = id, Name = name, Kind = MonsterSkillKind.Summon, SummonId = monsterId, SummonCount = count, CastTime = 1.5f,
                Cooldown = cooldown, ChancePercent = chance, MinPhase = minPhase, Shout = shout,
            };
        }
    }
}
