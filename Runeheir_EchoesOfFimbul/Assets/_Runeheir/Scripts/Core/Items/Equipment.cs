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
        public const int Positions = 10;

        /// <summary>Save format of the equipment array: 0 = saved before equipment existed.</summary>
        public const int CurrentDataVersion = 1;

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
            if (item == null || !item.IsEquipment)
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

            if (job.Tier < item.MinTier)
            {
                reason = item.MinTier >= 3 ? $"{item.Name} is for Ascended jobs only." : $"{item.Name} requires a tier {item.MinTier} job.";
                return false;
            }

            if (item.RequiredLine.HasValue && job.Line != item.RequiredLine.Value)
            {
                reason = $"Only the {JobDatabase.Get(item.RequiredLine.Value).Name} line can wear {item.Name}.";
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

            if (_inventory.Stacks.Count - 1 + displaced.Count > Inventory.MaxEntries)
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

        /// <summary>Unequips pieces the current job/level can no longer wear (after a GM job change). Returns how many.</summary>
        public int RemoveUnwearable()
        {
            int removed = 0;
            foreach (var pair in new List<KeyValuePair<EquipPosition, ItemStack>>(Worn()))
            {
                if (!CanWear(_record, pair.Value.Definition, out _) && TryUnequip(pair.Key, out _))
                {
                    removed++;
                }
            }

            return removed;
        }

        /// <summary>
        /// Job-change gift: a new copy of the job's starter weapon goes into the bag. It is equipped (the old weapon goes to the
        /// bag) when the hands are empty, the old weapon can't be wielded by the new job, or the old weapon is just an earlier
        /// job's plain starter weapon. A weapon you invested in (refined, carded, etched) stays in your hands. Null when the
        /// bag is full.
        /// </summary>
        public ItemStack GiftJobWeapon()
        {
            var item = ItemCatalog.Get(JobDatabase.Get(_record.Job).StarterWeaponId);
            if (item == null)
            {
                return null;
            }

            var gift = ItemStack.NewInstance(item);
            if (!_inventory.AddEntry(gift))
            {
                return null;
            }

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

        /// <summary>Always 10 entries; empty positions are null.</summary>
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

                entry.Amount = 1;
                entry.Sanitize();
                var position = (EquipPosition)i;
                bool accessory = item.Slots == EquipSlot.Accessory;
                bool fits = item.IsEquipment && (item.Slots & EquipKinds.SlotOf(position)) != 0
                            && (accessory ? position == EquipPosition.Accessory1 || position == EquipPosition.Accessory2 : set.TargetPosition(item) == position)
                            && set.Covering(position) == null && CanWear(record, item, out _);
                if (fits)
                {
                    record.Equipment[i] = entry;
                }
                else
                {
                    bag.AddEntry(entry);
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

        public WeaponProfile Weapon = WeaponProfile.BareHands;
        public bool HasWeapon;
        public bool WeaponUnbreakable;
        public Element ArmorElement = Element.Neutral;

        /// <summary>One bit per <see cref="StatusEffect"/> (see <c>StatusResistances.ImmunityMask</c>).</summary>
        public int ImmunityMask;

        public StatusEffect ExtraResistStatus;
        public float ExtraResistPercent;

        /// <summary>Runeword on the weapon, if its two glyphs spell one.</summary>
        public RunewordDefinition Runeword;

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
                if (item == null || entry.IsEmpty)
                {
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

            return stats;
        }

        private void Add(EquipEffect effect, CharacterRecord record)
        {
            if (effect == null)
            {
                return;
            }

            Modifiers.Add(effect.Modifiers);
            for (int i = 0; i < CombatEnumCounts.Races; i++)
            {
                Bonuses.VsRace[i] += effect.VsRace[i];
                Bonuses.TakenFromRace[i] += effect.TakenFromRace[i];
            }

            for (int i = 0; i < CombatEnumCounts.Sizes; i++)
            {
                Bonuses.VsSize[i] += effect.VsSize[i];
            }

            for (int i = 0; i < CombatEnumCounts.Elements; i++)
            {
                Bonuses.VsElement[i] += effect.VsElement[i];
                Bonuses.TakenFromElement[i] += effect.TakenFromElement[i];
            }

            OnHit.AddRange(effect.OnHit);
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
                ExtraResistStatus = effect.ResistStatus;
                ExtraResistPercent += effect.ResistPercent;
            }

            WeaponUnbreakable |= effect.Unbreakable;
            if (effect.AtkPerBaseLevel > 0f)
            {
                Modifiers.Atk += (int)(effect.AtkPerBaseLevel * record.BaseLevel);
            }

            var condition = effect.Condition;
            if (condition != null && record.Stats != null && record.Stats[condition.Stat] >= condition.MinBaseValue)
            {
                Add(condition.Bonus, record);
            }
        }
    }
}
