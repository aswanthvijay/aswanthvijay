using System;
using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Items
{
    /// <summary>Ragnarok's job classes as the XileRetro / rAthena item database names them (bit flags for item job lists).</summary>
    [Flags]
    public enum RoClass : long
    {
        None = 0,
        Novice = 1L << 0,
        Swordman = 1L << 1,
        Mage = 1L << 2,
        Archer = 1L << 3,
        Acolyte = 1L << 4,
        Merchant = 1L << 5,
        Thief = 1L << 6,
        Knight = 1L << 7,
        Priest = 1L << 8,
        Wizard = 1L << 9,
        Blacksmith = 1L << 10,
        Hunter = 1L << 11,
        Assassin = 1L << 12,
        Crusader = 1L << 13,
        Monk = 1L << 14,
        Sage = 1L << 15,
        Rogue = 1L << 16,
        Alchemist = 1L << 17,
        BardDancer = 1L << 18,
        Taekwon = 1L << 19,
        StarGladiator = 1L << 20,
        SoulLinker = 1L << 21,
        Gunslinger = 1L << 22,
        Ninja = 1L << 23,
        SuperNovice = 1L << 24,
        Summoner = 1L << 25,
        All = (1L << 26) - 1,
    }

    /// <summary>Which of Ragnarok's class groups an item allows (rAthena "Classes"): normal, transcendent ("upper").</summary>
    [Flags]
    public enum RoTier
    {
        Normal = 1,
        Upper = 2,
        All = Normal | Upper,
    }

    /// <summary>
    /// Phase 8: the XileRetro item database, adapted under Norse names by Tools/ItemImport (the generated
    /// Generated/ItemCatalog.Xile.*.g.cs files). This half maps Ragnarok's classes onto Runeheir's jobs and holds the
    /// helpers the generated code calls.
    /// </summary>
    public static partial class ItemCatalog
    {
        private static readonly (RoClass Class, JobId[] Normal, JobId[] Upper)[] ClassJobs =
        {
            (RoClass.Novice, new[] { JobId.Initiate }, Array.Empty<JobId>()),
            (RoClass.Swordman, new[] { JobId.Warrior }, Array.Empty<JobId>()),
            (RoClass.Mage, new[] { JobId.Mystic }, Array.Empty<JobId>()),
            (RoClass.Archer, new[] { JobId.Huntsman }, Array.Empty<JobId>()),
            (RoClass.Acolyte, new[] { JobId.Devotee }, Array.Empty<JobId>()),
            (RoClass.Merchant, new[] { JobId.Trader }, Array.Empty<JobId>()),
            (RoClass.Thief, new[] { JobId.Scout }, Array.Empty<JobId>()),
            (RoClass.Knight, new[] { JobId.Berserker }, new[] { JobId.Einherjar }),
            (RoClass.Priest, new[] { JobId.Gothi }, new[] { JobId.HighGothi }),
            (RoClass.Wizard, new[] { JobId.Sorcerer }, new[] { JobId.Archmage }),
            (RoClass.Blacksmith, new[] { JobId.Runesmith }, new[] { JobId.Forgelord }),
            (RoClass.Hunter, new[] { JobId.Ranger }, new[] { JobId.Deadeye }),
            (RoClass.Assassin, new[] { JobId.Assassin }, new[] { JobId.ShadowWalker }),
            (RoClass.Crusader, new[] { JobId.Guardian }, new[] { JobId.Valkyrie }),
            (RoClass.Monk, new[] { JobId.Monk }, new[] { JobId.Champion }),
            (RoClass.Sage, new[] { JobId.Sage }, new[] { JobId.Chronomancer }),
            (RoClass.Rogue, new[] { JobId.Outlaw }, new[] { JobId.Vargr }),
            (RoClass.Alchemist, new[] { JobId.Brewmaster }, new[] { JobId.Lifeweaver }),
            (RoClass.BardDancer, new[] { JobId.Skald, JobId.Seidkona }, new[] { JobId.Thul, JobId.Volva }),
            (RoClass.Taekwon, new[] { JobId.GlimaFighter }, Array.Empty<JobId>()),
            (RoClass.StarGladiator, new[] { JobId.SolGuardian }, Array.Empty<JobId>()),
            (RoClass.SoulLinker, new[] { JobId.FylgjaCaller }, Array.Empty<JobId>()),
            (RoClass.Gunslinger, new[] { JobId.Thunderer }, Array.Empty<JobId>()),
            (RoClass.Ninja, new[] { JobId.Nightraider }, Array.Empty<JobId>()),
            (RoClass.SuperNovice, new[] { JobId.Wanderer }, Array.Empty<JobId>()),
            (RoClass.Summoner, new[] { JobId.FreyjasKin }, Array.Empty<JobId>()),
        };

        private static readonly Dictionary<(RoClass, RoTier), JobId[]> JobsCache = new Dictionary<(RoClass, RoTier), JobId[]>();

        /// <summary>
        /// The Runeheir jobs an item with Ragnarok job list <paramref name="classes"/> allows, or null when it allows every job
        /// (the usual tier and weapon rules still apply). Reborn first jobs share their job id, so "Upper only" is
        /// <see cref="ItemDefinition.RebornOnly"/>.
        /// </summary>
        public static JobId[] JobsFor(RoClass classes, RoTier tier = RoTier.All)
        {
            if ((classes & RoClass.All) == RoClass.All && tier == RoTier.All)
            {
                return null;
            }

            if (JobsCache.TryGetValue((classes, tier), out var cached))
            {
                return cached;
            }

            var jobs = new List<JobId>();
            foreach (var (roClass, normal, upper) in ClassJobs)
            {
                if ((classes & roClass) == 0)
                {
                    continue;
                }

                // First jobs and the Initiate take both tiers under one id; second jobs split into normal and transcendent.
                bool split = upper.Length > 0;
                if (!split || (tier & RoTier.Normal) != 0)
                {
                    jobs.AddRange(normal);
                }

                if (split && (tier & RoTier.Upper) != 0)
                {
                    jobs.AddRange(upper);
                }
            }

            var result = jobs.ToArray();
            JobsCache[(classes, tier)] = result;
            return result;
        }

        /// <summary>Registers a generated item (ids are "xr" + the XileRetro id, so saves survive renames).</summary>
        private static void AddXile(ItemDefinition item)
        {
            if (!ById.ContainsKey(item.Id))
            {
                Register(item);
            }
        }

        /// <summary>A XileRetro item Runeheir already has its own version of: its "xr" id resolves to Runeheir's item.</summary>
        private static void AliasXile(int roId, string runeheirId)
        {
            if (ById.TryGetValue(runeheirId, out var item))
            {
                ById["xr" + roId] = item;
            }
        }

        /// <summary>The Runeheir item adapted from XileRetro item <paramref name="roId"/>, or null.</summary>
        public static ItemDefinition GetXile(int roId)
        {
            return Get("xr" + roId);
        }

        private static RefineBonus AtRefine(int minRefine, EquipEffect bonus)
        {
            return new RefineBonus { MinRefine = minRefine, Bonus = bonus };
        }

        /// <summary>Registers a timed buff an imported item gives (food, potions, gear auto-bonuses) and returns its id.</summary>
        /// <summary>Registers an imported timed bonus (gear's, a pet's) with the buff catalog and returns its id.</summary>
        public static string RegisterXileBuff(string id, string name, string description, float seconds, StatModifiers modifiers, bool debuff = false)
        {
            return XileBuff(id, name, description, seconds, modifiers, debuff);
        }

        private static string XileBuff(string id, string name, string description, float seconds, StatModifiers modifiers, bool debuff = false)
        {
            BuffCatalog.AddExternal(new BuffDefinition
            {
                Id = id, Name = name, Description = description, Duration = seconds, Modifiers = modifiers ?? StatModifiers.Empty(),
                IsDebuff = debuff, IconLabel = name.Length >= 3 ? name.Substring(0, 3).ToUpperInvariant() : name.ToUpperInvariant(),
                IconColorHex = debuff ? "#922B21" : "#B07A1C",
            });
            return id;
        }
    }
}
