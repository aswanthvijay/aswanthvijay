using System;

namespace Runeheir.Hotkeys
{
    public enum HotkeyKind
    {
        Empty = 0,
        Skill = 1,
        Item = 2,
    }

    /// <summary>One hotkey slot: a skill id, an item id, or nothing.</summary>
    [Serializable]
    public struct HotkeySlot
    {
        public HotkeyKind Kind;
        public string Id;

        public bool IsEmpty => Kind == HotkeyKind.Empty || string.IsNullOrEmpty(Id);

        public static HotkeySlot Empty => new HotkeySlot { Kind = HotkeyKind.Empty, Id = string.Empty };

        public static HotkeySlot Skill(string skillId)
        {
            return new HotkeySlot { Kind = HotkeyKind.Skill, Id = skillId };
        }

        public static HotkeySlot Item(string itemId)
        {
            return new HotkeySlot { Kind = HotkeyKind.Item, Id = itemId };
        }
    }

    /// <summary>
    /// Ragnarok-style hotkey bar: pages of 10 slots bound to F1..F10. F12 cycles pages.
    /// Operates directly on the array saved in <c>CharacterRecord.Hotkeys</c>.
    /// </summary>
    public sealed class HotkeyLayout
    {
        public const int SlotsPerPage = 10;
        public const int PageCount = 4;
        public const int TotalSlots = SlotsPerPage * PageCount;

        private readonly HotkeySlot[] _slots;

        public HotkeyLayout(HotkeySlot[] slots)
        {
            if (slots == null || slots.Length != TotalSlots)
            {
                throw new ArgumentException($"Hotkey array must have {TotalSlots} slots.", nameof(slots));
            }

            _slots = slots;
        }

        public event Action Changed;

        public static HotkeySlot[] CreateEmptyArray()
        {
            var slots = new HotkeySlot[TotalSlots];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = HotkeySlot.Empty;
            }

            return slots;
        }

        /// <summary>Returns a correctly sized array, preserving any existing entries (for old saves).</summary>
        public static HotkeySlot[] Normalize(HotkeySlot[] slots)
        {
            if (slots != null && slots.Length == TotalSlots)
            {
                return slots;
            }

            var normalized = CreateEmptyArray();
            if (slots != null)
            {
                Array.Copy(slots, normalized, Math.Min(slots.Length, TotalSlots));
            }

            return normalized;
        }

        public static int ToIndex(int page, int slotInPage)
        {
            if (page < 0 || page >= PageCount)
            {
                throw new ArgumentOutOfRangeException(nameof(page));
            }

            if (slotInPage < 0 || slotInPage >= SlotsPerPage)
            {
                throw new ArgumentOutOfRangeException(nameof(slotInPage));
            }

            return page * SlotsPerPage + slotInPage;
        }

        public HotkeySlot Get(int index)
        {
            return _slots[index];
        }

        public HotkeySlot Get(int page, int slotInPage)
        {
            return _slots[ToIndex(page, slotInPage)];
        }

        public void Assign(int index, HotkeySlot slot)
        {
            _slots[index] = slot.IsEmpty ? HotkeySlot.Empty : slot;
            Changed?.Invoke();
        }

        public void Clear(int index)
        {
            Assign(index, HotkeySlot.Empty);
        }

        public void Swap(int a, int b)
        {
            if (a == b)
            {
                return;
            }

            var temp = _slots[a];
            _slots[a] = _slots[b];
            _slots[b] = temp;
            Changed?.Invoke();
        }

        /// <summary>Puts the entry in the first empty slot of <paramref name="page"/>; returns the index or -1.</summary>
        public int AssignToFirstEmpty(int page, HotkeySlot slot)
        {
            for (int i = 0; i < SlotsPerPage; i++)
            {
                int index = ToIndex(page, i);
                if (_slots[index].IsEmpty)
                {
                    Assign(index, slot);
                    return index;
                }
            }

            return -1;
        }
    }
}
