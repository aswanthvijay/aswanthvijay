using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Jobs;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>Appearance inputs for the placeholder humanoid (job outfit, hair, weapon).</summary>
    public struct AvatarLook
    {
        public Color Outfit;
        public Color Skin;
        public Color Hair;
        public int HairStyle;
        public Gender Gender;
        public WeaponType Weapon;

        public static readonly string[] HairStyleNames =
        {
            "Short", "Spiky", "Long", "Ponytail", "Twin Tails", "Bun", "Mohawk", "Viking Braids",
        };

        public static readonly string[] HairColorNames =
        {
            "Raven", "Chestnut", "Wheat", "Ember", "Frost Silver", "Snow", "Fjord Blue", "Moss", "Amethyst",
        };

        public static readonly Color[] HairPalette =
        {
            new Color(0.10f, 0.09f, 0.10f),
            new Color(0.40f, 0.23f, 0.12f),
            new Color(0.93f, 0.78f, 0.45f),
            new Color(0.72f, 0.22f, 0.12f),
            new Color(0.75f, 0.80f, 0.86f),
            new Color(0.97f, 0.97f, 0.97f),
            new Color(0.22f, 0.42f, 0.78f),
            new Color(0.30f, 0.55f, 0.30f),
            new Color(0.52f, 0.30f, 0.68f),
        };

        public static readonly Color DefaultSkin = new Color(0.98f, 0.84f, 0.72f);

        public static AvatarLook FromRecord(CharacterRecord record)
        {
            var job = JobDatabase.Get(record.Job);
            return new AvatarLook
            {
                Outfit = RuntimeMaterials.Hex(job.ColorHex),
                Skin = DefaultSkin,
                Hair = HairPalette[CharacterFactory.Wrap(record.HairColor, HairPalette.Length)],
                HairStyle = CharacterFactory.Wrap(record.HairStyle, CharacterFactory.HairStyleCount),
                Gender = record.Gender,
                Weapon = job.StarterWeapon.Type,
            };
        }
    }
}
