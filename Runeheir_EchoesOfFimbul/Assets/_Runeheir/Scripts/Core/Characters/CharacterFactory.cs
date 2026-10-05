using Runeheir.Hotkeys;
using Runeheir.Items;
using Runeheir.Stats;

namespace Runeheir.Characters
{
    public sealed class CharacterCreateRequest
    {
        public string Name;
        public Gender Gender;
        public int HairStyle;
        public int HairColor;
    }

    /// <summary>Builds a brand-new Initiate (Novice) with starter items and default hotkeys.</summary>
    public static class CharacterFactory
    {
        public const int HairStyleCount = 8;
        public const int HairColorCount = 9;
        public const long StartingZeny = 500;

        public static CharacterRecord Create(CharacterCreateRequest request, int slot, long nowUnixMs)
        {
            var record = new CharacterRecord
            {
                Slot = slot,
                Name = CharacterNames.Normalize(request.Name),
                Gender = request.Gender,
                HairStyle = Wrap(request.HairStyle, HairStyleCount),
                HairColor = Wrap(request.HairColor, HairColorCount),
                StatPoints = StatFormulas.StartingStatPoints,
                Zeny = StartingZeny,
                MapId = MapCatalog.StartingMapId,
                CreatedUnixMs = nowUnixMs,
            };

            record.Inventory.Add(new ItemStack(ItemCatalog.LingonberryTonic, 30));
            record.Inventory.Add(new ItemStack(ItemCatalog.AetherSap, 10));
            record.Inventory.Add(new ItemStack(ItemCatalog.WindRuneShard, 10));
            record.Inventory.Add(new ItemStack(ItemCatalog.RavenFeather, 3));
            record.Inventory.Add(new ItemStack(ItemCatalog.RuneUruz, 2));
            record.Inventory.Add(new ItemStack(ItemCatalog.RuneTiwaz, 2));
            record.Inventory.Add(new ItemStack(ItemCatalog.RuneSowilo, 2));

            record.Hotkeys[0] = HotkeySlot.Skill("first_aid");
            record.Hotkeys[1] = HotkeySlot.Item(ItemCatalog.LingonberryTonic);
            record.Hotkeys[2] = HotkeySlot.Item(ItemCatalog.AetherSap);
            record.Hotkeys[3] = HotkeySlot.Item(ItemCatalog.WindRuneShard);
            record.Hotkeys[4] = HotkeySlot.Item(ItemCatalog.RavenFeather);
            return record;
        }

        public static int Wrap(int value, int count)
        {
            int result = value % count;
            return result < 0 ? result + count : result;
        }
    }

    /// <summary>Collapses whitespace in character names ("  Ragnar   Lodbrok " → "Ragnar Lodbrok").</summary>
    public static class CharacterNames
    {
        public static string Normalize(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var parts = name.Trim().Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }
    }
}
