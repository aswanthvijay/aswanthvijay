using Runeheir.Characters;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Jobs;
using UnityEngine;

namespace Runeheir.Visuals
{
    /// <summary>Appearance inputs for a humanoid avatar (job outfit, hair, weapon and worn gear).</summary>
    public struct AvatarLook
    {
        public Color Outfit;
        public Color Skin;
        public Color Hair;
        public int HairStyle;
        public Gender Gender;
        public WeaponType Weapon;

        /// <summary>The job whose outfit is worn (null: NPCs and humanoid monsters wear the common clothes in <see cref="Outfit"/> color).</summary>
        public JobId? Job;

        /// <summary>Phase 7: Freyja's Kin are cat-folk (smaller, with ears and a tail).</summary>
        public CharacterRace Race;

        /// <summary>Worn pieces drawn on the model (item ids; null when nothing is worn there).</summary>
        public string HeadUpper;

        public string HeadMid;
        public string HeadLower;
        public string Shield;
        public string Garment;

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

            // A record that never had equipment (the creation preview) shows the job's starter weapon.
            bool hasGear = record.EquipmentDataVersion >= EquipmentSet.CurrentDataVersion;
            return new AvatarLook
            {
                Job = record.Job,
                Outfit = RuntimeMaterials.Hex(job.ColorHex),
                Skin = DefaultSkin,
                Hair = HairPalette[CharacterFactory.Wrap(record.HairColor, HairPalette.Length)],
                HairStyle = CharacterFactory.Wrap(record.HairStyle, CharacterFactory.HairStyleCount),
                Gender = record.Gender,
                Race = record.Race,
                Weapon = hasGear ? Worn(record, EquipPosition.Weapon)?.WeaponType ?? WeaponType.Unarmed : job.StarterWeapon.Type,
                HeadUpper = Worn(record, EquipPosition.HeadUpper)?.Id,
                HeadMid = Worn(record, EquipPosition.HeadMid)?.Id,
                HeadLower = Worn(record, EquipPosition.HeadLower)?.Id,
                Shield = Worn(record, EquipPosition.Shield)?.Id,
                Garment = Worn(record, EquipPosition.Garment)?.Id,
            };
        }

        /// <summary>
        /// The look as one line of text (Phase 6: other players see your outfit, hair and gear):
        /// job|gender|hair style|hair color|weapon|head upper|head mid|head lower|shield|garment|race (Phase 7, added last so
        /// older codes still read).
        /// </summary>
        public static string Code(CharacterRecord record)
        {
            var look = FromRecord(record);
            return string.Join("|", (int)record.Job, (int)record.Gender, look.HairStyle,
                CharacterFactory.Wrap(record.HairColor, HairPalette.Length), (int)look.Weapon,
                look.HeadUpper, look.HeadMid, look.HeadLower, look.Shield, look.Garment, (int)look.Race);
        }

        /// <summary>Reads <see cref="Code"/>; anything missing or unknown falls back to an Initiate's plain look.</summary>
        public static AvatarLook FromCode(string code)
        {
            string[] parts = (code ?? string.Empty).Split('|');
            int Int(int index) => index < parts.Length && int.TryParse(parts[index], out int value) ? value : 0;
            string Item(int index) => index < parts.Length && ItemCatalog.Get(parts[index]) != null ? parts[index] : null;

            var jobId = (JobId)Int(0);
            var job = JobDatabase.Get(JobDatabase.Exists(jobId) ? jobId : JobId.Initiate);
            int weapon = Int(4);
            return new AvatarLook
            {
                Job = job.Id,
                Outfit = RuntimeMaterials.Hex(job.ColorHex),
                Skin = DefaultSkin,
                Hair = HairPalette[CharacterFactory.Wrap(Int(3), HairPalette.Length)],
                HairStyle = CharacterFactory.Wrap(Int(2), CharacterFactory.HairStyleCount),
                Gender = Int(1) == (int)Gender.Female ? Gender.Female : Gender.Male,
                Race = Int(10) == (int)CharacterRace.Doram ? CharacterRace.Doram : CharacterRace.Human,
                Weapon = System.Enum.IsDefined(typeof(WeaponType), weapon) ? (WeaponType)weapon : WeaponType.Unarmed,
                HeadUpper = Item(5),
                HeadMid = Item(6),
                HeadLower = Item(7),
                Shield = Item(8),
                Garment = Item(9),
            };
        }

        private static ItemDefinition Worn(CharacterRecord record, EquipPosition position)
        {
            var equipment = record.Equipment;
            int index = (int)position;
            return equipment != null && index < equipment.Length && equipment[index] != null && !equipment[index].IsEmpty
                ? equipment[index].Definition
                : null;
        }
    }
}
