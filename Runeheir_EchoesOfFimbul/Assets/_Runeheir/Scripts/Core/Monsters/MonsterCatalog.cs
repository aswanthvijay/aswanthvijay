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
            RegisterRosterDrops();
        }

        /// <summary>
        /// Phase 7: the new jobs' weapons drop where their kind would be found, like the GDD gift weapons: second-job
        /// pieces from the matching mid-level monsters, the god-forged fourth-level pieces from the hardest foes and MVPs.
        /// </summary>
        private static void RegisterRosterDrops()
        {
            AddDrops("horned_grazer", Drop("bearded_axe", 1f));
            AddDrops("wood_sprite", Drop("willow_lyre", 0.8f), Drop("leather_lash", 0.8f), Drop("trjegul_staff", 0.6f));
            AddDrops("wild_boar", Drop("dane_axe", 0.4f));
            AddDrops("forest_outlaw", Drop("cutpurse_dagger", 0.15f), Drop("thunder_carbine", 0.6f));
            AddDrops("dire_wolf", Drop("iron_huuma", 0.6f));
            AddDrops("fjord_harpy", Drop("tagelharpa", 0.15f), Drop("seidr_lash", 0.15f));
            AddDrops("runic_berserker", Drop("dwarven_axe", 0.15f), Drop("eddic_codex", 0.15f));
            AddDrops("frost_wolf", Drop("frost_huuma", 0.3f));
            AddDrops("sea_drake", Drop("storm_rod", 0.3f), Drop("herbwife_sickle", 0.15f));
            AddDrops("banshee", Drop("fylgja_wand", 0.15f), Drop("rune_primer", 1f));
            AddDrops("jotun_brawler", Drop("eitri_great_axe", 0.02f));
            AddDrops("frost_wyrm", Drop("fenrir_huuma", 0.02f), Drop("thors_wrath", 0.02f));
            AddDrops("hels_executioner", Drop("lokis_sting", 0.03f), Drop("book_of_mimir", 0.02f));
            AddDrops("abyssal_leech", Drop("serpent_lash", 0.02f));
            AddDrops("naga_queen", Drop("serpent_lash", 3f), Drop("bragis_harp", 3f));
            AddDrops("ancient_golem", Drop("eitri_great_axe", 3f), Drop("idunn_sickle", 3f));
            AddDrops("fenrir", Drop("fenrir_huuma", 5f), Drop("thors_wrath", 5f), Drop("lokis_sting", 5f));
            AddDrops("hels_vanguard", Drop("book_of_mimir", 5f), Drop("idunn_sickle", 5f), Drop("brisingamen_staff", 5f));
            AddDrops("jormungandrs_brood", Drop("serpent_lash", 5f), Drop("bragis_harp", 5f), Drop("brisingamen_staff", 5f));
        }

        private static void AddDrops(string monsterId, params DropEntry[] drops)
        {
            if (!ById.TryGetValue(monsterId, out var monster))
            {
                throw new InvalidOperationException("Roster drops for an unknown monster: " + monsterId);
            }

            monster.Drops.AddRange(drops);
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
