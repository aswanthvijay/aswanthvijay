using System;
using System.Collections.Generic;
using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Jobs;
using Runeheir.Stats;

namespace Runeheir.Items
{
    /// <summary>
    /// The 10-slot paperdoll (GDD §5) over <c>CharacterRecord.Equipment</c>. Each worn piece is stored once, at its
    /// main position; two-handed weapons also block the shield slot and full helms block the mid slot.
    /// Equipping moves the piece out of the bag; anything it displaces goes back into the bag.
    /// </summary>
    public sealed class EquipmentSet
    {
        /// <summary>Ten paperdoll positions, the ammunition slot (Phase 8) and four costume slots (Phase 8.4).</summary>
        public const int Positions = 15;

        /// <summary>
        /// Save format of the equipment array: 0 = saved before equipment existed, 2 = with the ammunition slot, 3 = with the
        /// costume slots.
        /// </summary>
        public const int CurrentDataVersion = 3;

        private readonly CharacterRecord _record;
        private readonly Inventory _inventory;

        public EquipmentSet(CharacterRecord record, Inventory inventory)
        {
            _record = record ?? throw new ArgumentNullException(nameof(record));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _record.Equipment = Normalize(_record.Equipment);
        }

        /// <summary>Something was equipped, unequipped, refined or carded.</summary>
        public event Action Changed;

        /// <summary>One shot of worn ammunition was spent (the stack is still there; nothing else changed).</summary>
        public event Action AmmoSpent;

        /// <summary>The piece stored at <paramref name="position"/> (null when empty or only blocked by another piece).</summary>
        public ItemStack Get(EquipPosition position)
        {
            var entry = _record.Equipment[(int)position];
            return entry == null || entry.IsEmpty ? null : entry;
        }

        /// <summary>The piece that occupies <paramref name="position"/>, including a two-handed weapon over the shield slot.</summary>
        public ItemStack Covering(EquipPosition position)
        {
            var own = Get(position);
            if (own != null)
            {
                return own;
            }

            var slot = EquipKinds.SlotOf(position);
            for (int i = 0; i < Positions; i++)
            {
                var other = Get((EquipPosition)i);
                if (other != null && slot != EquipSlot.Accessory && (other.Definition.Slots & slot) != 0)
                {
                    return other;
                }
            }

            return null;
        }

        public IEnumerable<KeyValuePair<EquipPosition, ItemStack>> Worn()
        {
            for (int i = 0; i < Positions; i++)
            {
                var entry = Get((EquipPosition)i);
                if (entry != null)
                {
                    yield return new KeyValuePair<EquipPosition, ItemStack>((EquipPosition)i, entry);
                }
            }
        }

        public int TotalWeight()
        {
            int total = 0;
            foreach (var pair in Worn())
            {
                total += pair.Value.Definition?.Weight ?? 0;
            }

            return total;
        }

        /// <summary>Job, tier, line and level rules for wearing <paramref name="item"/>.</summary>
        public static bool CanWear(CharacterRecord record, ItemDefinition item, out string reason)
        {
            if (item == null || !item.IsWearable)
            {
                reason = "That can't be equipped.";
                return false;
            }

            var job = JobDatabase.Get(record.Job);
            if (record.BaseLevel < item.EquipLevel)
            {
                reason = $"{item.Name} requires Base Level {item.EquipLevel}.";
                return false;
            }

            if (job.GearTier < item.MinTier)
            {
                reason = item.MinTier >= 3 ? $"{item.Name} is for transcendent and expanded jobs only." : $"{item.Name} requires a second job or higher.";
                return false;
            }

            if (item.RequiredLine.HasValue && job.Line != item.RequiredLine.Value)
            {
                reason = $"Only the {JobDatabase.Get(item.RequiredLine.Value).Name} line can wear {item.Name}.";
                return false;
            }

            if (item.RebornOnly && !record.Reborn && !job.IsExpanded)
            {
                reason = $"{item.Name} is for reborn characters only.";
                return false;
            }

            if (item.RequiredGender.HasValue && record.Gender != item.RequiredGender.Value)
            {
                reason = $"Only {(item.RequiredGender.Value == Gender.Female ? "women" : "men")} can wear {item.Name}.";
                return false;
            }

            if (item.AllowedJobs != null && Array.IndexOf(item.AllowedJobs, record.Job) < 0)
            {
                reason = $"{JobDatabase.NameFor(record)}s can't use {item.Name}.";
                return false;
            }

            if (item.IsWeapon && !WeaponMasks.Allows(job.AllowedWeapons, item.WeaponType))
            {
                reason = $"{job.Name}s can't wield a {WeaponRules.Label(item.WeaponType)}.";
                return false;
            }

            reason = null;
            return true;
        }

        public bool TryEquip(ItemStack entry, out string reason)
        {
            var item = entry?.Definition;
            if (item == null || !_inventory.Contains(entry))
            {
                reason = "That item isn't in your bag.";
                return false;
            }

            if (!CanWear(_record, item, out reason))
            {
                return false;
            }

            var position = TargetPosition(item);
            if (item.IsAmmo)
            {
                return EquipAmmo(entry, out reason);
            }

            var displaced = new List<EquipPosition>();
            for (int i = 0; i < Positions; i++)
            {
                var current = Get((EquipPosition)i);
                if (current == null)
                {
                    continue;
                }

                bool clash = item.Slots == EquipSlot.Accessory
                    ? i == (int)position
                    : (current.Definition.Slots & item.Slots & ~EquipSlot.Accessory) != 0;
                if (clash)
                {
                    displaced.Add((EquipPosition)i);
                }
            }

            // Wearing takes one entry out of the bag; only a swap that grows a full bag is refused.
            if (displaced.Count > 1 && _inventory.Stacks.Count - 1 + displaced.Count > Inventory.MaxEntries)
            {
                reason = "Your bag is too full to swap that in.";
                return false;
            }

            _inventory.RemoveEntry(entry);
            foreach (var slot in displaced)
            {
                _inventory.AddEntry(Get(slot));
                _record.Equipment[(int)slot] = null;
            }

            entry.Amount = 1;
            _record.Equipment[(int)position] = entry;
            reason = null;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Ammunition goes on as a whole stack; the same kind tops up the stack already worn.</summary>
        private bool EquipAmmo(ItemStack entry, out string reason)
        {
            var worn = Get(EquipPosition.Ammo);
            _inventory.RemoveEntry(entry);
            if (worn != null && worn.ItemId == entry.ItemId)
            {
                worn.Amount += entry.Amount;
            }
            else
            {
                if (worn != null)
                {
                    ForceIntoBag(_record, _inventory, worn);
                }

                _record.Equipment[(int)EquipPosition.Ammo] = entry;
            }

            reason = null;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Spends one shot of worn ammunition (the stack comes off when it runs out). False when none is worn.</summary>
        public bool SpendAmmo()
        {
            var worn = Get(EquipPosition.Ammo);
            if (worn == null)
            {
                return false;
            }

            worn.Amount--;
            if (worn.Amount <= 0)
            {
                _record.Equipment[(int)EquipPosition.Ammo] = null;
                Changed?.Invoke();
            }
            else
            {
                AmmoSpent?.Invoke();
            }

            return true;
        }

        public bool TryUnequip(EquipPosition position, out string reason)
        {
            var entry = Get(position);
            if (entry == null)
            {
                reason = "Nothing is equipped there.";
                return false;
            }

            if (!_inventory.AddEntry(entry))
            {
                reason = "Your bag is full.";
                return false;
            }

            _record.Equipment[(int)position] = null;
            reason = null;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Unequips pieces the current job/level can no longer wear (after a job change). A full bag overflows rather than
        /// keeping or losing the piece. Returns how many came off.
        /// </summary>
        public int RemoveUnwearable()
        {
            int removed = 0;
            foreach (var pair in new List<KeyValuePair<EquipPosition, ItemStack>>(Worn()))
            {
                if (CanWear(_record, pair.Value.Definition, out _))
                {
                    continue;
                }

                if (!TryUnequip(pair.Key, out _))
                {
                    _record.Equipment[(int)pair.Key] = null;
                    ForceIntoBag(_record, _inventory, pair.Value);
                    Changed?.Invoke();
                }

                removed++;
            }

            return removed;
        }

        /// <summary>
        /// Moves a piece into the bag even past <see cref="Inventory.MaxEntries"/>: gear is never destroyed by a full bag
        /// (an over-full bag just refuses new loot until you make room).
        /// </summary>
        private static void ForceIntoBag(CharacterRecord record, Inventory bag, ItemStack entry)
        {
            if (!bag.AddEntry(entry))
            {
                entry.Amount = Math.Max(1, entry.Amount);
                record.Inventory.Add(entry);
                bag.NotifyChanged();
            }
        }

        /// <summary>
        /// Job-change gift: a new copy of the job's starter weapon goes into the bag. It is equipped (the old weapon goes to the
        /// bag) when the hands are empty, the old weapon can't be wielded by the new job, or the old weapon is just an earlier
        /// job's plain starter weapon. A weapon you invested in (refined, carded, etched) stays in your hands. A full bag
        /// overflows rather than losing the gift. Null only when the job has no starter weapon.
        /// </summary>
        public ItemStack GiftJobWeapon()
        {
            var item = ItemCatalog.Get(JobDatabase.Get(_record.Job).StarterWeaponId);
            if (item == null)
            {
                return null;
            }

            var gift = ItemStack.NewInstance(item);
            ForceIntoBag(_record, _inventory, gift);

            var current = Get(EquipPosition.Weapon);
            if (current == null || !CanWear(_record, current.Definition, out _) || IsPlainStarterWeapon(current))
            {
                TryEquip(gift, out _);
            }

            return gift;
        }

        /// <summary>Some job's starter weapon with no refine, cards or glyphs.</summary>
        public static bool IsPlainStarterWeapon(ItemStack entry)
        {
            if (entry == null || entry.Refine > 0 || entry.CardCount > 0 || Array.Exists(entry.Glyphs ?? Array.Empty<string>(), g => !string.IsNullOrEmpty(g)))
            {
                return false;
            }

            foreach (var job in JobDatabase.All)
            {
                if (job.StarterWeaponId == entry.ItemId)
                {
                    return true;
                }
            }

            return false;
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }

        /// <summary>Where a piece goes: its top-most slot; accessories take the free ring finger (or replace the first).</summary>
        public EquipPosition TargetPosition(ItemDefinition item)
        {
            var slots = item.Slots;
            if (item.IsAmmo || (slots & EquipSlot.Ammo) != 0)
            {
                return EquipPosition.Ammo;
            }

            if ((slots & EquipSlot.Costume) != 0)
            {
                if ((slots & EquipSlot.CostumeUpper) != 0) return EquipPosition.CostumeUpper;
                if ((slots & EquipSlot.CostumeMid) != 0) return EquipPosition.CostumeMid;
                if ((slots & EquipSlot.CostumeLower) != 0) return EquipPosition.CostumeLower;
                return EquipPosition.CostumeGarment;
            }

            if ((slots & EquipSlot.Accessory) != 0)
            {
                return Get(EquipPosition.Accessory1) == null || Get(EquipPosition.Accessory2) != null ? EquipPosition.Accessory1 : EquipPosition.Accessory2;
            }

            if ((slots & EquipSlot.Weapon) != 0) return EquipPosition.Weapon;
            if ((slots & EquipSlot.Shield) != 0) return EquipPosition.Shield;
            if ((slots & EquipSlot.Armor) != 0) return EquipPosition.Armor;
            if ((slots & EquipSlot.Garment) != 0) return EquipPosition.Garment;
            if ((slots & EquipSlot.Footgear) != 0) return EquipPosition.Footgear;
            if ((slots & EquipSlot.HeadUpper) != 0) return EquipPosition.HeadUpper;
            return (slots & EquipSlot.HeadMid) != 0 ? EquipPosition.HeadMid : EquipPosition.HeadLower;
        }

        /// <summary>Always <see cref="Positions"/> entries; empty positions are null.</summary>
        public static ItemStack[] Normalize(ItemStack[] equipment)
        {
            var normalized = new ItemStack[Positions];
            if (equipment != null)
            {
                for (int i = 0; i < Math.Min(Positions, equipment.Length); i++)
                {
                    normalized[i] = equipment[i] == null || equipment[i].IsEmpty ? null : equipment[i];
                }
            }

            return normalized;
        }

        /// <summary>
        /// Repairs the paperdoll from an old or hand-edited save: pieces in the wrong slot or not wearable go back to the
        /// bag, overlaps are resolved, and Phase 2/3 characters (version 0) get their job's starter weapon equipped.
        /// Called by <see cref="CharacterRecord.Sanitize"/>.
        /// </summary>
        public static void SanitizeEquipment(CharacterRecord record)
        {
            var equipment = Normalize(record.Equipment);
            record.Equipment = new ItemStack[Positions];
            var bag = new Inventory(record.Inventory);
            var set = new EquipmentSet(record, bag);
            for (int i = 0; i < Positions; i++)
            {
                var entry = equipment[i];
                var item = entry?.Definition;
                if (item == null)
                {
                    continue;
                }

                if (!item.IsAmmo)
                {
                    entry.Amount = 1;
                }

                entry.Sanitize();
                var position = (EquipPosition)i;
                bool accessory = item.Slots == EquipSlot.Accessory;
                bool fits = item.IsWearable && (item.Slots & EquipKinds.SlotOf(position)) != 0
                            && (accessory ? position == EquipPosition.Accessory1 || position == EquipPosition.Accessory2 : set.TargetPosition(item) == position)
                            && set.Covering(position) == null && CanWear(record, item, out _);
                if (fits)
                {
                    record.Equipment[i] = entry;
                }
                else
                {
                    ForceIntoBag(record, bag, entry);
                }
            }

            if (record.EquipmentDataVersion < 1)
            {
                // Phase 2/3 characters fought with an implicit job weapon: give it to them for real.
                var starter = ItemCatalog.Get(JobDatabase.Get(record.Job).StarterWeaponId);
                if (starter != null && set.Get(EquipPosition.Weapon) == null)
                {
                    var weapon = ItemStack.NewInstance(starter);
                    record.Equipment[(int)EquipPosition.Weapon] = weapon;
                }
            }

            record.EquipmentDataVersion = CurrentDataVersion;
        }
    }

    /// <summary>
    /// Everything the worn equipment adds to the stat engine and to combat: stat modifiers (item stats, refine DEF,
    /// cards, runewords), the weapon profile, race/size/element damage tables, armor element, on-hit procs and
    /// status immunities. Computed by <see cref="Compute"/>.
    /// </summary>
    public sealed class EquipmentStats
    {
        public readonly StatModifiers Modifiers = StatModifiers.Empty();
        public readonly DamageBonuses Bonuses = new DamageBonuses();
        public readonly List<OnHitEffect> OnHit = new List<OnHitEffect>();

        /// <summary>Auto-casts and auto-buffs from gear and cards (Phase 8).</summary>
        public readonly List<AutoCast> AutoCasts = new List<AutoCast>();

        /// <summary>Skills the gear lets you use, at the highest level any piece grants.</summary>
        public readonly Dictionary<string, int> GrantedSkills = new Dictionary<string, int>();

        /// <summary>+% damage per skill id.</summary>
        public readonly Dictionary<string, float> SkillDamagePercent = new Dictionary<string, float>();

        public WeaponProfile Weapon = WeaponProfile.BareHands;
        public bool HasWeapon;
        public bool WeaponUnbreakable;
        public Element ArmorElement = Element.Neutral;

        /// <summary>One bit per <see cref="StatusEffect"/> (see <c>StatusResistances.ImmunityMask</c>).</summary>
        public int ImmunityMask;

        /// <summary>Extra status resistance in percent, one entry per <see cref="StatusEffect"/> (see <c>StatusResistances.ExtraResist</c>).</summary>
        public readonly float[] ExtraResist = new float[StatusCount];

        private static readonly int StatusCount = MaxStatusValue() + 1;

        /// <summary>Runeword on the weapon, if its two glyphs spell one.</summary>
        public RunewordDefinition Runeword;

        /// <summary>Worn ammunition that fits the weapon: each basic attack spends one (Phase 8).</summary>
        public bool AmmoInUse;

        public string AmmoItemId;

        public static EquipmentStats Compute(CharacterRecord record)
        {
            var stats = new EquipmentStats();
            if (record?.Equipment == null)
            {
                return stats;
            }

            for (int i = 0; i < Math.Min(EquipmentSet.Positions, record.Equipment.Length); i++)
            {
                var entry = record.Equipment[i];
                var item = entry?.Definition;
                // A broken weapon does nothing at all (no ATK, cards or runeword) until it's repaired.
                if (item == null || entry.IsEmpty || (item.IsWeapon && entry.Broken))
                {
                    continue;
                }

                if (item.IsAmmo)
                {
                    // Ammunition only counts when it fits the weapon in hand: its ATK, element and bonuses ride the shot.
                    if (stats.HasWeapon && item.AmmoFor == stats.Weapon.Type && entry.Amount > 0)
                    {
                        stats.AmmoInUse = true;
                        stats.AmmoItemId = item.Id;
                        stats.Modifiers.Atk += item.Atk;
                        if (item.Element != Element.Neutral)
                        {
                            stats.Weapon.Element = item.Element;
                        }

                        stats.Add(item.Effect, record);
                    }

                    continue;
                }

                if (item.IsWeapon)
                {
                    stats.Weapon = item.ToWeaponProfile(entry.Refine);
                    stats.HasWeapon = true;
                }
                else
                {
                    stats.Modifiers.Def += item.Def + (item.Refinable ? RefineRules.DefPerRefine * entry.Refine : 0);
                }

                stats.Modifiers.Mdef += item.Mdef;
                if (!item.IsWeapon && (item.Slots & EquipSlot.Armor) != 0 && item.Element != Element.Neutral)
                {
                    stats.ArmorElement = item.Element;
                }

                stats.Add(item.Effect, record);
                if (item.PerRefine != null && entry.Refine > 0)
                {
                    stats.Add(item.PerRefine, record, item.PerRefineLimit > 0 ? Math.Min(entry.Refine, item.PerRefineLimit) : entry.Refine);
                }

                if (item.RefineBonuses != null)
                {
                    foreach (var bonus in item.RefineBonuses)
                    {
                        if (entry.Refine >= bonus.MinRefine)
                        {
                            stats.Add(bonus.Bonus, record);
                        }
                    }
                }

                foreach (string cardId in entry.Cards ?? Array.Empty<string>())
                {
                    stats.Add(ItemCatalog.Get(cardId)?.Effect, record);
                }

                if (item.IsWeapon)
                {
                    stats.Runeword = RunewordRules.ActiveRuneword(entry);
                    stats.Add(stats.Runeword?.Effect, record);
                }
            }

            stats.Add(Pets.PetCatalog.LoyalBonus(record), record); // Phase 8: a Loyal pet's bonus
            return stats;
        }

        /// <param name="times">How many times over (per refine level); flags, procs and element changes apply once.</param>
        private void Add(EquipEffect effect, CharacterRecord record, float times = 1f)
        {
            if (effect == null || times <= 0f)
            {
                return;
            }

            Modifiers.AddScaled(effect.Modifiers, times);
            for (int i = 0; i < CombatEnumCounts.Races; i++)
            {
                Bonuses.VsRace[i] += effect.VsRace[i] * times;
                Bonuses.TakenFromRace[i] += effect.TakenFromRace[i] * times;
                Bonuses.CritVsRace[i] += effect.CritVsRace[i] * times;
                Bonuses.DefBypassVsRace[i] += effect.DefBypassVsRace[i] * times;
                Bonuses.MdefBypassVsRace[i] += effect.MdefBypassVsRace[i] * times;
                Bonuses.MagicVsRace[i] += effect.MagicVsRace[i] * times;
            }

            for (int i = 0; i < CombatEnumCounts.Sizes; i++)
            {
                Bonuses.VsSize[i] += effect.VsSize[i] * times;
                Bonuses.TakenFromSize[i] += effect.TakenFromSize[i] * times;
            }

            for (int i = 0; i < CombatEnumCounts.Elements; i++)
            {
                Bonuses.VsElement[i] += effect.VsElement[i] * times;
                Bonuses.TakenFromElement[i] += effect.TakenFromElement[i] * times;
                Bonuses.MagicVsElement[i] += effect.MagicVsElement[i] * times;
            }

            Bonuses.VsBoss += effect.VsBoss * times;
            Bonuses.VsNormal += effect.VsNormal * times;

            OnHit.AddRange(effect.OnHit);
            foreach (var auto in effect.AutoCasts)
            {
                AutoCasts.Add(Math.Abs(times - 1f) < 0.0001f ? auto : auto.Scaled(times));
            }

            foreach (var grant in effect.GrantedSkills)
            {
                GrantedSkills[grant.Key] = Math.Max(grant.Value, GrantedSkills.TryGetValue(grant.Key, out int had) ? had : 0);
            }

            foreach (var bonus in effect.SkillDamage)
            {
                SkillDamagePercent[bonus.Key] = bonus.Value * times + (SkillDamagePercent.TryGetValue(bonus.Key, out float had) ? had : 0f);
            }

            if (effect.ArmorElement.HasValue)
            {
                ArmorElement = effect.ArmorElement.Value;
            }

            if (effect.WeaponElement.HasValue)
            {
                Weapon.Element = effect.WeaponElement.Value;
            }

            foreach (var status in effect.Immunities)
            {
                ImmunityMask |= StatusResistances.Bit(status);
            }

            if (effect.ResistStatus != StatusEffect.None && effect.ResistPercent > 0f)
            {
                ExtraResist[(int)effect.ResistStatus] += effect.ResistPercent * times;
            }

            for (int i = 0; i < Math.Min(effect.StatusResist.Length, ExtraResist.Length); i++)
            {
                ExtraResist[i] += effect.StatusResist[i] * times;
            }

            WeaponUnbreakable |= effect.Unbreakable;
            if (effect.AtkPerBaseLevel > 0f)
            {
                Modifiers.Atk += (int)(effect.AtkPerBaseLevel * record.BaseLevel * times);
            }

            var condition = effect.Condition;
            if (condition != null && record.Stats != null && record.Stats[condition.Stat] >= condition.MinBaseValue)
            {
                Add(condition.Bonus, record);
            }
        }

        private static int MaxStatusValue()
        {
            int max = 0;
            foreach (StatusEffect status in Enum.GetValues(typeof(StatusEffect)))
            {
                max = Math.Max(max, (int)status);
            }

            return max;
        }
    }
}
