using System;
using System.Collections.Generic;
using Runeheir.Hotkeys;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Skills;
using Runeheir.Stats;

namespace Runeheir.Characters
{
    public enum Gender
    {
        Male = 0,
        Female = 1,
    }

    /// <summary>One learned skill and its level.</summary>
    [Serializable]
    public sealed class LearnedSkill
    {
        public string Id;
        public int Level;

        public LearnedSkill()
        {
        }

        public LearnedSkill(string id, int level)
        {
            Id = id;
            Level = level;
        }
    }

    /// <summary>
    /// Everything persisted for one character (what a char-server would store).
    /// Plain public fields so JsonUtility (and later Mirror messages) can serialize it.
    /// </summary>
    [Serializable]
    public sealed class CharacterRecord
    {
        public int Slot;
        public string Name;
        public Gender Gender;
        public int HairStyle;
        public int HairColor;

        public JobId Job = JobId.Initiate;
        public int BaseLevel = 1;
        public int JobLevel = 1;
        public long BaseExp;
        public long JobExp;
        public int StatPoints = StatFormulas.StartingStatPoints;
        public int SkillPoints;
        public BaseStats Stats = new BaseStats();

        /// <summary>Current HP/SP; -1 means "full" (fresh character).</summary>
        public int Hp = -1;

        public int Sp = -1;

        public long Zeny;
        public string MapId;
        public bool HasSavedPosition;
        public float PosX;
        public float PosY;
        public float PosZ;

        public List<ItemStack> Inventory = new List<ItemStack>();
        public HotkeySlot[] Hotkeys = HotkeyLayout.CreateEmptyArray();

        /// <summary>The 10-slot paperdoll, indexed by <see cref="EquipPosition"/>. Null/empty entries are empty slots.</summary>
        public ItemStack[] Equipment = new ItemStack[EquipmentSet.Positions];

        /// <summary>0 = saved before equipment existed; <see cref="Sanitize"/> migrates it (equips the job's starter weapon).</summary>
        public int EquipmentDataVersion;

        /// <summary>Learned skills and levels (Phase 3). Granted skills (First Aid) are always present.</summary>
        public List<LearnedSkill> Skills = new List<LearnedSkill>();

        /// <summary>0 = saved before skill levels existed; <see cref="Sanitize"/> migrates it to <see cref="SkillBook.CurrentDataVersion"/>.</summary>
        public int SkillDataVersion;

        public long CreatedUnixMs;
        public long LastPlayedUnixMs;

        public CharacterRecord Clone()
        {
            var copy = (CharacterRecord)MemberwiseClone();
            copy.Stats = Stats?.Clone() ?? new BaseStats();
            copy.Inventory = new List<ItemStack>();
            if (Inventory != null)
            {
                foreach (var stack in Inventory)
                {
                    copy.Inventory.Add(stack.Clone());
                }
            }

            copy.Equipment = new ItemStack[EquipmentSet.Positions];
            var equipment = EquipmentSet.Normalize(Equipment);
            for (int i = 0; i < equipment.Length; i++)
            {
                copy.Equipment[i] = equipment[i]?.Clone();
            }

            copy.Hotkeys = (HotkeySlot[])HotkeyLayout.Normalize(Hotkeys).Clone();
            copy.Skills = new List<LearnedSkill>();
            if (Skills != null)
            {
                foreach (var skill in Skills)
                {
                    if (skill != null)
                    {
                        copy.Skills.Add(new LearnedSkill(skill.Id, skill.Level));
                    }
                }
            }

            return copy;
        }

        /// <summary>Repairs nulls/sizes from older or hand-edited saves.</summary>
        public void Sanitize()
        {
            Stats = Stats ?? new BaseStats();
            Inventory = Inventory ?? new List<ItemStack>();
            Inventory.RemoveAll(s => s == null || s.Amount <= 0 || ItemCatalog.Get(s.ItemId) == null);
            var repaired = new List<ItemStack>();
            foreach (var stack in Inventory)
            {
                var item = stack.Definition;
                stack.Sanitize();
                if (item.IsStackable)
                {
                    stack.Amount = Math.Min(stack.Amount, Runeheir.Items.Inventory.MaxStack);
                    repaired.Add(stack);
                    continue;
                }

                // Equipment never stacks: a hand-edited "Amount": 3 becomes three separate +0 pieces after the first.
                int copies = Math.Min(stack.Amount, Runeheir.Items.Inventory.MaxEntries);
                stack.Amount = 1;
                repaired.Add(stack);
                for (int i = 1; i < copies; i++)
                {
                    repaired.Add(ItemStack.NewInstance(item));
                }
            }

            Inventory = repaired;

            Hotkeys = HotkeyLayout.Normalize(Hotkeys);
            // Unknown enum values (hand-edited save, or written by a newer build) are repaired, never thrown on,
            // so one bad record can't hide every character on the account.
            if (!JobDatabase.Exists(Job))
            {
                Job = JobId.Initiate;
            }

            if (Gender != Gender.Male && Gender != Gender.Female)
            {
                Gender = Gender.Male;
            }

            BaseExp = Math.Max(0, BaseExp);
            JobExp = Math.Max(0, JobExp);
            BaseLevel = StatFormulas.Clamp(BaseLevel, 1, StatFormulas.MaxBaseLevel);
            JobLevel = StatFormulas.Clamp(JobLevel, 1, JobDatabase.MaxJobLevel(Job));
            foreach (var stat in StatTypes.All)
            {
                Stats[stat] = StatFormulas.Clamp(Stats[stat], StatFormulas.MinStat, StatFormulas.MaxStat);
            }

            if (string.IsNullOrEmpty(MapId) || MapCatalog.Get(MapId) == null)
            {
                MapId = MapCatalog.StartingMapId;
                HasSavedPosition = false;
            }

            SkillPoints = Math.Max(0, SkillPoints);
            SkillBook.SanitizeSkills(this);
            EquipmentSet.SanitizeEquipment(this);
            Zeny = Math.Max(0, Zeny);
        }
    }

    public sealed class MapInfo
    {
        public string Id;
        public string Name;
        public string SceneName;
        public int MinLevel;
        public int MaxLevel;
    }

    /// <summary>GDD §2 maps. Phase 2 ships only the first field; Phase 5 adds the rest.</summary>
    public static class MapCatalog
    {
        public const string StartingMapId = "whisperwood_plains";

        private static readonly Dictionary<string, MapInfo> Maps = new Dictionary<string, MapInfo>(StringComparer.OrdinalIgnoreCase)
        {
            {
                StartingMapId,
                new MapInfo
                {
                    Id = StartingMapId,
                    Name = "Whisperwood Plains",
                    SceneName = "RH_Field_WhisperwoodPlains",
                    MinLevel = 1,
                    MaxLevel = 60,
                }
            },
        };

        public static MapInfo Get(string id)
        {
            return id != null && Maps.TryGetValue(id, out var map) ? map : null;
        }
    }
}
