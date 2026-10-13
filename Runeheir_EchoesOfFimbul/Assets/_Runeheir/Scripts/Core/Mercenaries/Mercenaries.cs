using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Monsters;

namespace Runeheir.Mercenaries
{
    /// <summary>
    /// A hired sword (Phase 8, Ragnarok's mercenaries): a fighter of the Hirð (bowman, spearman or huscarl, grades 1–10)
    /// or a monster under contract. It fights as a monster that sides with its master, so its stats, skills and look are a
    /// <see cref="MonsterDefinition"/>.
    /// </summary>
    public sealed class MercenaryDefinition
    {
        public MonsterDefinition Monster;

        /// <summary>1–10 for the Hirð's fighters; 0 for monster contracts.</summary>
        public int Grade;

        /// <summary>"Bowman", "Spearman" or "Huscarl"; null for monster contracts.</summary>
        public string Role;

        /// <summary>The base level a hirer needs.</summary>
        public int MinBaseLevel = 1;

        public string Id => Monster?.Id;

        public string Name => Monster?.Name;
    }

    /// <summary>The mercenary a character has hired: who, the contract time left (it runs while you play), its HP, its kills.</summary>
    [Serializable]
    public sealed class MercenaryContract
    {
        public string MercenaryId;
        public float SecondsLeft;

        /// <summary>Its HP when last seen (0 = full); it keeps its wounds across map changes.</summary>
        public int Hp;

        public int Kills;
    }

    /// <summary>Every mercenary, and Ragnarok's contract rules: one at a time, 30 minutes, a base level to hire each grade.</summary>
    public static partial class MercenaryCatalog
    {
        public const float ContractSeconds = 1800f;

        private static readonly Dictionary<string, MercenaryDefinition> ById = new Dictionary<string, MercenaryDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<MercenaryDefinition> Ordered = new List<MercenaryDefinition>();

        static MercenaryCatalog()
        {
            RegisterXileMercenaries();
        }

        public static IReadOnlyList<MercenaryDefinition> All => Ordered;

        public static MercenaryDefinition Get(string id)
        {
            return id != null && ById.TryGetValue(id, out var mercenary) ? mercenary : null;
        }

        private static void Register(MercenaryDefinition mercenary)
        {
            ById[mercenary.Id] = mercenary;
            Ordered.Add(mercenary);
        }

        /// <summary>The contract that's running, or null (none, or its time ran out).</summary>
        public static MercenaryContract Active(CharacterRecord record)
        {
            var contract = record?.Mercenary;
            return contract != null && contract.SecondsLeft > 0f && Get(contract.MercenaryId) != null ? contract : null;
        }

        /// <summary>Signs a new contract (ending the old one). False, with the reason, when the hirer's level is too low.</summary>
        public static bool Hire(CharacterRecord record, string mercenaryId, out string message)
        {
            var mercenary = Get(mercenaryId);
            if (record == null || mercenary == null)
            {
                message = "No such mercenary.";
                return false;
            }

            if (record.BaseLevel < mercenary.MinBaseLevel)
            {
                message = $"{mercenary.Name} won't follow anyone below base level {mercenary.MinBaseLevel}.";
                return false;
            }

            var old = Active(record);
            record.Mercenary = new MercenaryContract { MercenaryId = mercenary.Id, SecondsLeft = ContractSeconds };
            message = old != null && !string.Equals(old.MercenaryId, mercenary.Id, StringComparison.OrdinalIgnoreCase)
                ? $"{Get(old.MercenaryId).Name}'s contract ends. {mercenary.Name} answers your call for {ContractSeconds / 60f:0} minutes."
                : $"{mercenary.Name} answers your call for {ContractSeconds / 60f:0} minutes.";
            return true;
        }

        /// <summary>Runs the contract's clock. Returns true when it has just ended.</summary>
        public static bool Tick(CharacterRecord record, float seconds)
        {
            var contract = Active(record);
            if (contract == null)
            {
                return false;
            }

            contract.SecondsLeft -= seconds;
            if (contract.SecondsLeft > 0f)
            {
                return false;
            }

            record.Mercenary = null;
            return true;
        }

        public static void End(CharacterRecord record)
        {
            if (record != null)
            {
                record.Mercenary = null;
            }
        }

        public static string TimeLeftText(MercenaryContract contract)
        {
            int seconds = contract == null ? 0 : Math.Max(0, (int)Math.Ceiling(contract.SecondsLeft));
            return $"{seconds / 60}:{seconds % 60:00}";
        }
    }
}
