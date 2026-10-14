using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Skills;
using Runeheir.Stats;

namespace Runeheir.Social
{
    /// <summary>
    /// What a realm handed one character, and what the character handed on, since its last accepted save.
    /// <list type="bullet">
    /// <item>Credits are things the realm sent: EXP, drops, steals, trade and stall goods, stall takings, refunds. One
    /// still on its way when the character saved carries over to the next save (only that one: every later save shows
    /// it or it lapses), and each is usable once.</item>
    /// <item>Debits are things the character gave away: a trade offer, stall goods, a stall payment. The player's game
    /// takes them out before it tells the realm, so every later save shows them.</item>
    /// </list>
    /// </summary>
    public sealed class GuardLedger
    {
        public long BaseExp;
        public long JobExp;
        public long ZenyIn;
        public long ZenyOut;

        /// <summary>EXP awards (about one per kill): bounds Cut Purse, which takes zeny on the player's own screen.</summary>
        public int Awards;

        public readonly Dictionary<string, int> ItemsIn = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, int> ItemsOut = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The part of each credit that already waited through one save (it lapses at the next).</summary>
        internal long CarriedBaseExp;

        internal long CarriedJobExp;
        internal long CarriedZenyIn;
        internal readonly Dictionary<string, int> CarriedItemsIn = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public void AddExp(long baseExp, long jobExp, double now)
        {
            BaseExp += Math.Max(0, baseExp);
            JobExp += Math.Max(0, jobExp);
            Awards++;
        }

        public void AddZenyIn(long zeny, double now)
        {
            ZenyIn += Math.Max(0, zeny);
        }

        public void AddZenyOut(long zeny, double now)
        {
            ZenyOut += Math.Max(0, zeny);
        }

        public void AddItemIn(string itemId, int amount, double now)
        {
            Bump(ItemsIn, itemId, amount);
        }

        public void AddItemOut(string itemId, int amount, double now)
        {
            Bump(ItemsOut, itemId, amount);
        }

        /// <summary>Goods that changed hands, with any cards in them.</summary>
        public void AddGoods(IEnumerable<ItemStack> goods, bool incoming, double now)
        {
            if (goods == null)
            {
                return;
            }

            foreach (var stack in goods)
            {
                if (stack == null || stack.IsEmpty)
                {
                    continue;
                }

                Bump(incoming ? ItemsIn : ItemsOut, stack.ItemId, stack.Amount);
                foreach (string card in stack.Cards ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrEmpty(card))
                    {
                        Bump(incoming ? ItemsIn : ItemsOut, card, stack.Amount);
                    }
                }
            }
        }

        public int In(string itemId) => ItemsIn.TryGetValue(itemId, out int amount) ? amount : 0;

        public int Out(string itemId) => ItemsOut.TryGetValue(itemId, out int amount) ? amount : 0;

        public void Clear()
        {
            BaseExp = JobExp = ZenyIn = ZenyOut = 0;
            CarriedBaseExp = CarriedJobExp = CarriedZenyIn = 0;
            Awards = 0;
            ItemsIn.Clear();
            ItemsOut.Clear();
            CarriedItemsIn.Clear();
        }

        private static void Bump(Dictionary<string, int> tally, string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
            {
                return;
            }

            tally.TryGetValue(itemId, out int have);
            tally[itemId] = (int)Math.Min(int.MaxValue, (long)have + amount);
        }

    }

    public sealed class GuardOptions
    {
        /// <summary>
        /// Check points, progress and wealth. Off while a realm allows @commands, which change characters legitimately;
        /// the shape checks (unknown items, overfull stacks, refines past +20, more cards than sockets) run either way.
        /// </summary>
        public bool Economy = true;

        /// <summary>The most a character's own bonuses (gear, manuals, songs, duets) can multiply the EXP the realm sends.</summary>
        public float ExpCeiling = 4f;
    }

    /// <summary>The guard's answer: violations refuse the save; warnings are only logged for the realm's GMs.</summary>
    public sealed class GuardVerdict
    {
        public readonly List<string> Violations = new List<string>();
        public readonly List<string> Warnings = new List<string>();

        public bool Refused => Violations.Count > 0;

        public string Summary => Refused ? string.Join("; ", Violations) : Warnings.Count > 0 ? string.Join("; ", Warnings) : "ok";
    }

    /// <summary>
    /// Phase 9, the realm guard (anti-cheat). Characters are still run by their players' own games, so before keeping a
    /// save the realm checks it against what it can know for itself:
    /// <list type="bullet">
    /// <item>impossible characters: stats or skills beyond what their levels give, skills from another line, a job they
    /// can't have reached, levels past the caps, unknown items, overfull stacks, refines past +20, more cards than
    /// sockets;</item>
    /// <item>progress: more EXP than the realm awarded (times the most any bonus allows);</item>
    /// <item>wealth: items a player's game can't make (drops, MVP prizes, cards, chest gear) appearing without the realm
    /// handing them over, goods given away in trades and stalls but kept, and zeny beyond what sales, trades and stalls
    /// explain.</item>
    /// </list>
    /// A refused save leaves the realm's copy as it was. Everything here is pure, so the tests run it directly.
    /// </summary>
    public static class RealmGuard
    {
        private static HashSet<string> _clientMade;
        private static Dictionary<string, List<ItemGrant>> _boxedIn;

        /// <summary>Audits <paramref name="submitted"/> (as received, before any repair) against the realm's last good copy.</summary>
        /// <param name="previousStorage">The account's storage as the realm has it (used only when this save carries storage).</param>
        /// <param name="submittedStorage">The storage sent with this save, or null.</param>
        public static GuardVerdict Audit(CharacterRecord previous, List<ItemStack> previousStorage, CharacterRecord submitted,
            List<ItemStack> submittedStorage, GuardLedger ledger, GuardOptions options)
        {
            var verdict = new GuardVerdict();
            if (submitted == null)
            {
                verdict.Violations.Add("no character data");
                return verdict;
            }

            options = options ?? new GuardOptions();
            ledger = ledger ?? new GuardLedger();
            CheckShape(submitted, submittedStorage, verdict);
            if (previous != null)
            {
                CheckIdentity(previous, submitted, verdict);
            }

            // @commands set levels, stats, skills, items and zeny: on a realm that allows them only the shape is checked.
            if (!options.Economy || !JobDatabase.Exists(submitted.Job))
            {
                return verdict;
            }

            CheckBudgets(submitted, verdict);
            if (previous == null)
            {
                return verdict;
            }

            bool reborn = !previous.Reborn && submitted.Reborn;
            CheckJobAndLevels(previous, submitted, reborn, verdict);
            CheckExperience(previous, submitted, reborn, ledger, options, verdict);
            CheckWealth(previous, previousStorage, submitted, submittedStorage, ledger, verdict);
            return verdict;
        }

        /// <summary>
        /// After a save is accepted: credits it shows are used up (the oldest first), ones it doesn't (still in flight)
        /// carry over to the next save, ones that already waited through a save lapse, and debits are settled.
        /// </summary>
        public static void Settle(GuardLedger ledger, CharacterRecord previous, List<ItemStack> previousStorage, CharacterRecord accepted,
            List<ItemStack> acceptedStorage, double now)
        {
            if (ledger == null)
            {
                return;
            }

            if (previous == null || accepted == null)
            {
                ledger.Clear();
                return;
            }

            bool reborn = !previous.Reborn && accepted.Reborn;
            long baseGain = reborn ? CumulativeBaseExp(accepted) : CumulativeBaseExp(accepted) - CumulativeBaseExp(previous);
            long jobGain = accepted.Job == previous.Job && !reborn ? CumulativeJobExp(accepted) - CumulativeJobExp(previous) : CumulativeJobExp(accepted);
            long baseLeft = Keep(ledger.BaseExp, ledger.CarriedBaseExp, Math.Max(0, baseGain));
            long jobLeft = Keep(ledger.JobExp, ledger.CarriedJobExp, Math.Max(0, jobGain));

            var before = Pool(previous, acceptedStorage != null ? previousStorage : null);
            var after = Pool(accepted, acceptedStorage);
            var itemsLeft = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in ledger.ItemsIn)
            {
                // What the save shows arrived (its increase, plus whatever went straight back out) uses the credit up.
                int used = Math.Max(0, Count(after, pair.Key) - Count(before, pair.Key) + ledger.Out(pair.Key));
                ledger.CarriedItemsIn.TryGetValue(pair.Key, out int carried);
                int left = (int)Keep(pair.Value, carried, used);
                if (left > 0)
                {
                    itemsLeft[pair.Key] = left;
                }
            }

            long explained = SaleAllowance(before, after, ledger) + CutPurseAllowance(accepted, ledger);
            long zenyUsed = Math.Max(0, accepted.Zeny - previous.Zeny + ledger.ZenyOut - explained);
            long zenyLeft = Keep(ledger.ZenyIn, ledger.CarriedZenyIn, zenyUsed);

            ledger.Clear();
            ledger.BaseExp = ledger.CarriedBaseExp = baseLeft;
            ledger.JobExp = ledger.CarriedJobExp = jobLeft;
            ledger.ZenyIn = ledger.CarriedZenyIn = zenyLeft;
            foreach (var pair in itemsLeft)
            {
                ledger.ItemsIn[pair.Key] = pair.Value;
                ledger.CarriedItemsIn[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// Of a credit <paramref name="total"/> (<paramref name="carried"/> of it already waited through a save), what
        /// waits for the next save once <paramref name="used"/> is taken, the oldest first: only the new part.
        /// </summary>
        private static long Keep(long total, long carried, long used)
        {
            long left = Math.Max(0, total - used);
            long fresh = Math.Max(0, total - carried);
            return Math.Min(left, fresh);
        }

        // ================================================================ shape (always)
        private static void CheckShape(CharacterRecord record, List<ItemStack> storage, GuardVerdict verdict)
        {
            if (record.Zeny < 0 || record.Zeny > ItemTransfer.MaxZeny)
            {
                verdict.Violations.Add($"zeny out of range ({record.Zeny:N0})");
            }

            if (!JobDatabase.Exists(record.Job))
            {
                verdict.Violations.Add("an unknown job");
                return;
            }

            if (record.BaseLevel < 1 || record.BaseLevel > RebirthRules.BaseLevelCap(record))
            {
                verdict.Violations.Add($"Base Level {record.BaseLevel} is past the cap");
            }

            if (record.JobLevel < 1 || record.JobLevel > JobDatabase.MaxJobLevel(record.Job))
            {
                verdict.Violations.Add($"Job Level {record.JobLevel} is past the cap");
            }

            if (record.BaseExp < 0 || record.JobExp < 0)
            {
                verdict.Violations.Add("negative EXP");
            }
            else if (record.BaseLevel >= 1 && record.BaseLevel < RebirthRules.BaseLevelCap(record)
                                           && record.BaseExp >= ExperienceTable.BaseExpToNext(record.BaseLevel))
            {
                verdict.Violations.Add("more Base EXP than the level holds");
            }

            foreach (var stat in StatTypes.All)
            {
                if (record.Stats == null || record.Stats[stat] < StatFormulas.MinStat || record.Stats[stat] > StatFormulas.MaxStat)
                {
                    verdict.Violations.Add($"{stat} out of range");
                    break;
                }
            }

            CheckStacks(record.Inventory, "bag", verdict);
            CheckStacks(record.Cart, "cart", verdict);
            CheckStacks(storage, "storage", verdict);
            if (record.Equipment == null)
            {
                return;
            }

            if (record.Equipment.Length > EquipmentSet.Positions)
            {
                verdict.Violations.Add("too many equipment slots");
            }

            foreach (var worn in record.Equipment)
            {
                if (worn == null || worn.IsEmpty)
                {
                    continue;
                }

                CheckStack(worn, "worn", verdict);
                var item = worn.Definition;
                if (item == null)
                {
                    continue;
                }

                if (!item.IsWearable)
                {
                    verdict.Violations.Add($"{item.Name} can't be worn");
                }
                else if (!EquipmentSet.CanWear(record, item, out string reason))
                {
                    verdict.Warnings.Add($"wears {item.Name} ({reason})");
                }
            }
        }

        private static void CheckStacks(List<ItemStack> stacks, string where, GuardVerdict verdict)
        {
            if (stacks == null)
            {
                return;
            }

            if (stacks.Count > Inventory.MaxEntries * 3)
            {
                verdict.Violations.Add($"{stacks.Count} entries in the {where}");
                return;
            }

            foreach (var stack in stacks)
            {
                if (stack != null)
                {
                    CheckStack(stack, where, verdict);
                }
            }
        }

        private static void CheckStack(ItemStack stack, string where, GuardVerdict verdict)
        {
            var item = ItemCatalog.Get(stack.ItemId);
            if (item == null)
            {
                verdict.Violations.Add($"an unknown item '{stack.ItemId}' in the {where}");
                return;
            }

            if (stack.Amount <= 0 || stack.Amount > Inventory.MaxStack || (!item.IsStackable && stack.Amount != 1))
            {
                verdict.Violations.Add($"{stack.Amount} {item.Name} in one {where} entry");
            }

            if (stack.Refine < 0 || stack.Refine > RefineRules.MaxRefine || (stack.Refine > 0 && !(item.IsEquipment && item.Refinable)))
            {
                verdict.Violations.Add($"{item.Name} refined to +{stack.Refine}");
            }

            int cards = 0;
            foreach (string cardId in stack.Cards ?? Array.Empty<string>())
            {
                if (string.IsNullOrEmpty(cardId))
                {
                    continue;
                }

                cards++;
                var card = ItemCatalog.Get(cardId);
                if (card == null || !card.IsCard)
                {
                    verdict.Violations.Add($"{item.Name} holds '{cardId}', which isn't a card");
                }
                else if (card.CardTarget != item.EquipKind)
                {
                    verdict.Warnings.Add($"{card.Name} in {item.Name}");
                }
            }

            if (cards > Math.Max(0, item.Sockets))
            {
                verdict.Violations.Add($"{cards} cards in {item.Name} ({item.Sockets} sockets)");
            }
        }

        private static void CheckIdentity(CharacterRecord previous, CharacterRecord submitted, GuardVerdict verdict)
        {
            if (!string.Equals(previous.Name, submitted.Name, StringComparison.Ordinal) || previous.Slot != submitted.Slot)
            {
                verdict.Violations.Add("a different character");
            }

            if (previous.Gender != submitted.Gender)
            {
                verdict.Violations.Add("a changed gender");
            }

            if (previous.CreatedUnixMs != 0 && previous.CreatedUnixMs != submitted.CreatedUnixMs)
            {
                verdict.Violations.Add("a changed creation date");
            }

            if (previous.Reborn && !submitted.Reborn)
            {
                verdict.Violations.Add("an undone rebirth");
            }
        }

        // ================================================================ points (always)
        private static void CheckBudgets(CharacterRecord record, GuardVerdict verdict)
        {
            if (record.Stats != null)
            {
                int earned = StatFormulas.TotalStatPointsAtLevel(record.BaseLevel) + (record.Reborn ? RebirthRules.BonusStatPoints : 0);
                int spent = StatFormulas.SpentStatPoints(record.Stats);
                if (record.StatPoints < 0 || spent + record.StatPoints > earned)
                {
                    verdict.Violations.Add($"stats worth {spent + Math.Max(0, record.StatPoints):N0} points; Base Level {record.BaseLevel} gives {earned:N0}");
                }
            }

            int skillSpent = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var learned in record.Skills ?? new List<LearnedSkill>())
            {
                if (learned == null)
                {
                    continue;
                }

                var skill = SkillCatalog.Get(learned.Id);
                if (skill == null || skill.Hidden)
                {
                    verdict.Violations.Add($"an unknown skill '{learned.Id}'");
                    continue;
                }

                if (!seen.Add(skill.Id) || learned.Level < 0 || learned.Level > skill.MaxLevel)
                {
                    verdict.Violations.Add($"{skill.Name} at level {learned.Level}");
                    continue;
                }

                if (!skill.Granted && !JobDatabase.IsSelfOrAncestor(skill.Job, record.Job))
                {
                    verdict.Violations.Add($"{skill.Name} belongs to another line");
                    continue;
                }

                skillSpent += Math.Max(0, learned.Level - (skill.Granted ? 1 : 0));
            }

            int budget = SkillPointBudget(record);
            if (record.SkillPoints < 0 || skillSpent + record.SkillPoints > budget)
            {
                verdict.Violations.Add($"skills worth {skillSpent + Math.Max(0, record.SkillPoints)} points; the job levels give {budget}");
            }
        }

        /// <summary>
        /// The most skill points the character's path can have given: each job before this one at its top job level,
        /// and this one at its current level. A reborn transcendent job retraces High Initiate → High first job → itself.
        /// </summary>
        public static int SkillPointBudget(CharacterRecord record)
        {
            var job = JobDatabase.Get(record.Job);
            int budget = StatFormulas.SkillPointsAtJobLevel(record.JobLevel);
            var current = job;
            for (int step = 0; step < 8 && current.HasParent; step++)
            {
                current = JobDatabase.Get(current.Parent);
                if (record.Reborn && job.Tier == 3 && current.Tier == 2)
                {
                    continue;
                }

                budget += StatFormulas.SkillPointsAtJobLevel(current.MaxJobLevel);
            }

            return budget;
        }

        // ================================================================ progress
        private static void CheckJobAndLevels(CharacterRecord previous, CharacterRecord submitted, bool reborn, GuardVerdict verdict)
        {
            if (reborn)
            {
                if (!RebirthRules.CanRebirth(previous, out string why))
                {
                    verdict.Violations.Add("a rebirth the realm didn't see earned (" + why + ")");
                }

                if (submitted.Job != JobId.Initiate && !Reachable(RebornProbe(previous), submitted.Job))
                {
                    verdict.Violations.Add($"{JobDatabase.Get(submitted.Job).Name} right after rebirth");
                }

                return;
            }

            if (submitted.Job != previous.Job && !Reachable(previous, submitted.Job))
            {
                verdict.Violations.Add($"{JobDatabase.Get(previous.Job).Name} can't become {JobDatabase.Get(submitted.Job).Name}");
            }

            if (submitted.BaseLevel < previous.BaseLevel)
            {
                verdict.Violations.Add("a lower Base Level");
            }

            if (submitted.Job == previous.Job && submitted.JobLevel < previous.JobLevel)
            {
                verdict.Violations.Add("a lower Job Level");
            }
        }

        private static CharacterRecord RebornProbe(CharacterRecord previous)
        {
            var probe = previous.Clone();
            probe.RebirthPath = previous.Job;
            probe.Reborn = true;
            probe.Job = JobId.Initiate;
            return probe;
        }

        /// <summary>Can <paramref name="from"/>'s job become <paramref name="target"/> in up to two job changes?</summary>
        private static bool Reachable(CharacterRecord from, JobId target)
        {
            foreach (var first in JobDatabase.NextJobs(from))
            {
                if (first.Id == target)
                {
                    return true;
                }

                var step = from.Clone();
                step.Job = first.Id;
                if (JobDatabase.NextJobs(step).Exists(j => j.Id == target))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CheckExperience(CharacterRecord previous, CharacterRecord submitted, bool reborn, GuardLedger ledger,
            GuardOptions options, GuardVerdict verdict)
        {
            long baseGain = reborn ? CumulativeBaseExp(submitted) : CumulativeBaseExp(submitted) - CumulativeBaseExp(previous);
            long baseAllowed = (long)(ledger.BaseExp * (double)options.ExpCeiling)
                               + ExperienceTable.BaseExpToNext(Math.Max(1, submitted.BaseLevel)) / 50 + 10;
            if (baseGain > baseAllowed)
            {
                verdict.Violations.Add($"{baseGain:N0} Base EXP gained; the realm awarded {ledger.BaseExp:N0}");
            }

            bool sameJob = submitted.Job == previous.Job && !reborn;
            long jobGain = sameJob ? CumulativeJobExp(submitted) - CumulativeJobExp(previous) : CumulativeJobExp(submitted);
            var job = JobDatabase.Get(submitted.Job);
            long jobAllowed = (long)(ledger.JobExp * (double)options.ExpCeiling)
                              + ExperienceTable.JobExpToNext(Math.Max(1, submitted.JobLevel), job.Tier, job.MaxJobLevel) / 50 + 10;
            if (jobGain > jobAllowed)
            {
                verdict.Violations.Add($"{jobGain:N0} Job EXP gained; the realm awarded {ledger.JobExp:N0}");
            }
        }

        /// <summary>All Base EXP earned to reach this level and progress (death penalties take it back down).</summary>
        public static long CumulativeBaseExp(CharacterRecord record)
        {
            long total = Math.Max(0, record.BaseExp);
            for (int level = 1; level < Math.Min(record.BaseLevel, StatFormulas.MaxBaseLevel); level++)
            {
                total += ExperienceTable.BaseExpToNext(level);
            }

            return total;
        }

        /// <summary>All Job EXP earned in the current job.</summary>
        public static long CumulativeJobExp(CharacterRecord record)
        {
            if (!JobDatabase.Exists(record.Job))
            {
                return 0;
            }

            var job = JobDatabase.Get(record.Job);
            long total = Math.Max(0, record.JobExp);
            for (int level = 1; level < Math.Min(record.JobLevel, job.MaxJobLevel); level++)
            {
                total += ExperienceTable.JobExpToNext(level, job.Tier, job.MaxJobLevel);
            }

            return total;
        }

        // ================================================================ wealth
        private static void CheckWealth(CharacterRecord previous, List<ItemStack> previousStorage, CharacterRecord submitted,
            List<ItemStack> submittedStorage, GuardLedger ledger, GuardVerdict verdict)
        {
            // Storage counts on both sides only when this save carries it (every storage move saves the bag with it).
            var before = Pool(previous, submittedStorage != null ? previousStorage : null);
            var after = Pool(submitted, submittedStorage);
            var ids = new HashSet<string>(after.Keys, StringComparer.OrdinalIgnoreCase);
            ids.UnionWith(ledger.ItemsOut.Keys);
            EnsureSources();
            foreach (string id in ids)
            {
                int delta = Count(after, id) - Count(before, id);
                int expected = ledger.In(id) - ledger.Out(id);
                if (delta <= expected || _clientMade.Contains(id))
                {
                    continue;
                }

                int allowed = expected + BoxAllowance(id, before, after, ledger);
                if (delta > allowed)
                {
                    verdict.Violations.Add(ledger.Out(id) > 0
                        ? $"kept {Name(id)} after handing it over"
                        : $"{delta - Math.Max(0, allowed)} {Name(id)} the realm never handed over");
                }
            }

            long zenyAllowed = ledger.ZenyIn - ledger.ZenyOut + SaleAllowance(before, after, ledger) + CutPurseAllowance(submitted, ledger)
                               + 100 + Math.Max(0, previous.Zeny) / 200;
            long zenyDelta = submitted.Zeny - previous.Zeny;
            if (zenyDelta > zenyAllowed)
            {
                verdict.Violations.Add($"{zenyDelta:N0} zeny gained; sales, trades and stalls explain {Math.Max(0, zenyAllowed):N0}");
            }
        }

        /// <summary>Boxes that are gone without being handed over were opened, and each can have held this item.</summary>
        private static int BoxAllowance(string id, Dictionary<string, int> before, Dictionary<string, int> after, GuardLedger ledger)
        {
            if (!_boxedIn.TryGetValue(id, out var boxes))
            {
                return 0;
            }

            long allowed = 0;
            foreach (var box in boxes)
            {
                int opened = Gone(box.ItemId, before, after, ledger);
                if (opened > 0)
                {
                    allowed += (long)opened * Math.Max(1, box.Amount);
                }
            }

            return (int)Math.Min(int.MaxValue, allowed);
        }

        /// <summary>How many of an item left without the realm's say (sold to a shop, used, opened, dropped).</summary>
        private static int Gone(string id, Dictionary<string, int> before, Dictionary<string, int> after, GuardLedger ledger)
        {
            return Count(before, id) + ledger.In(id) - ledger.Out(id) - Count(after, id);
        }

        /// <summary>What everything that left without the realm's say could have fetched: sold at the best price, or a zeny pouch opened.</summary>
        private static long SaleAllowance(Dictionary<string, int> before, Dictionary<string, int> after, GuardLedger ledger)
        {
            var ids = new HashSet<string>(before.Keys, StringComparer.OrdinalIgnoreCase);
            ids.UnionWith(ledger.ItemsIn.Keys);
            long total = 0;
            foreach (string id in ids)
            {
                int gone = Gone(id, before, after, ledger);
                var item = gone > 0 ? ItemCatalog.Get(id) : null;
                if (item != null)
                {
                    total += Math.Max(TradeRules.SellPrice(item, TradeRules.MaxPricePercent), item.GivesZenyMax) * (long)gone;
                }
            }

            return total;
        }

        /// <summary>Cut Purse takes a handful of zeny from each monster on the thief's own screen: bounded by the kills.</summary>
        private static long CutPurseAllowance(CharacterRecord record, GuardLedger ledger)
        {
            foreach (var learned in record.Skills ?? new List<LearnedSkill>())
            {
                var skill = learned != null ? SkillCatalog.Get(learned.Id) : null;
                if (skill != null && skill.Special == SkillSpecial.StealCoin && learned.Level > 0)
                {
                    return (long)ledger.Awards * StatFormulas.MaxBaseLevel * 16 * (10 + learned.Level) / 10;
                }
            }

            return 0;
        }

        /// <summary>Every item the character holds anywhere (bag, worn, cart, a pet's accessory, storage), counting slotted cards.</summary>
        public static Dictionary<string, int> Pool(CharacterRecord record, List<ItemStack> storage)
        {
            var pool = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (record == null)
            {
                return pool;
            }

            AddAll(pool, record.Inventory);
            AddAll(pool, record.Cart);
            AddAll(pool, storage);
            if (record.Equipment != null)
            {
                AddAll(pool, record.Equipment);
            }

            foreach (var pet in record.Pets ?? new List<Pets.PetState>())
            {
                if (pet != null && !string.IsNullOrEmpty(pet.ArmorItemId))
                {
                    Add(pool, pet.ArmorItemId, 1);
                }
            }

            return pool;
        }

        private static void AddAll(Dictionary<string, int> pool, IEnumerable<ItemStack> stacks)
        {
            if (stacks == null)
            {
                return;
            }

            foreach (var stack in stacks)
            {
                if (stack == null || stack.IsEmpty)
                {
                    continue;
                }

                Add(pool, stack.ItemId, stack.Amount);
                foreach (string card in stack.Cards ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrEmpty(card))
                    {
                        Add(pool, card, stack.Amount);
                    }
                }
            }
        }

        private static void Add(Dictionary<string, int> pool, string id, int amount)
        {
            pool.TryGetValue(id, out int have);
            pool[id] = (int)Math.Min(int.MaxValue, (long)have + amount);
        }

        private static int Count(Dictionary<string, int> pool, string id)
        {
            return pool.TryGetValue(id, out int amount) ? amount : 0;
        }

        private static string Name(string id)
        {
            return ItemCatalog.Get(id)?.Name ?? id;
        }

        // ================================================================ combat
        /// <summary>
        /// The most one hit from this character could ever do to a realm monster: its ATK and MATK from its worn gear,
        /// times far more than the strongest skill, critical, card and element bonuses together. It stops forged
        /// million-point hits without ever trimming a real one.
        /// </summary>
        public static int DamageCeiling(CharacterRecord record)
        {
            if (record?.Stats == null)
            {
                return 0;
            }

            var gear = EquipmentStats.Compute(record);
            var stats = DerivedStats.Compute(Math.Max(1, record.BaseLevel), record.Stats, gear.Modifiers, gear.Weapon);
            long power = (long)Math.Max(0, stats.StatusAtk) + Math.Max(0, stats.WeaponAtk) + Math.Max(0, stats.MatkMax) + 300;
            return (int)Math.Min(int.MaxValue, power * 400);
        }

        // ================================================================ where items come from
        /// <summary>Items a player's own game can make: shop stock, crafts, starter kits, pet eggs.</summary>
        public static bool IsClientMade(string itemId)
        {
            EnsureSources();
            return _clientMade.Contains(itemId);
        }

        /// <summary>Does some box, pouch or chest hold this item?</summary>
        public static bool IsBoxed(string itemId)
        {
            EnsureSources();
            return _boxedIn.ContainsKey(itemId);
        }

        private static void EnsureSources()
        {
            if (_clientMade != null)
            {
                return;
            }

            var made = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var boxed = new Dictionary<string, List<ItemGrant>>(StringComparer.OrdinalIgnoreCase);
            foreach (var shop in ShopCatalog.All)
            {
                foreach (string id in shop.ItemIds ?? Array.Empty<string>())
                {
                    made.Add(id);
                }
            }

            foreach (CraftKind kind in Enum.GetValues(typeof(CraftKind)))
            {
                foreach (var recipe in CraftingRules.For(kind) ?? Array.Empty<CraftRecipe>())
                {
                    made.Add(recipe.ProductId);
                }
            }

            foreach (var job in JobDatabase.All)
            {
                if (!string.IsNullOrEmpty(job.StarterWeaponId))
                {
                    made.Add(job.StarterWeaponId);
                }
            }

            var starter = CharacterFactory.Create(new CharacterCreateRequest { Name = "Guard" }, 0, 0);
            foreach (var stack in starter.Inventory)
            {
                made.Add(stack.ItemId);
            }

            foreach (var item in ItemCatalog.All)
            {
                if (item.Special == ItemSpecialEffect.HatchPet)
                {
                    made.Add(item.Id);
                }

                foreach (var gives in new[] { item.GivesItems, item.RandomGives })
                {
                    foreach (var grant in gives ?? Array.Empty<ItemGrant>())
                    {
                        if (grant == null || string.IsNullOrEmpty(grant.ItemId))
                        {
                            continue;
                        }

                        if (!boxed.TryGetValue(grant.ItemId, out var boxes))
                        {
                            boxed[grant.ItemId] = boxes = new List<ItemGrant>();
                        }

                        boxes.Add(new ItemGrant(item.Id, Math.Max(1, grant.Amount)));
                    }
                }
            }

            _boxedIn = boxed;
            _clientMade = made;
        }
    }
}
