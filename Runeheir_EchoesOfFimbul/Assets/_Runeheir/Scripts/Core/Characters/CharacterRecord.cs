using System;
using System.Collections.Generic;
using Runeheir.Hotkeys;
using Runeheir.Items;
using Runeheir.Jobs;
using Runeheir.Skills;
using Runeheir.Stats;
using Runeheir.World;

namespace Runeheir.Characters
{
    public enum Gender
    {
        Male = 0,
        Female = 1,
    }

    /// <summary>One learned skill and its level.</summary>
    [Serializable]
    /// <summary>Who the character is born as (Phase 7). Doram are Freyja's Kin, the cat-folk.</summary>
    public enum CharacterRace
    {
        Human = 0,
        Doram = 1,
    }

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

        public CharacterRace Race = CharacterRace.Human;

        public JobId Job = JobId.Initiate;

        /// <summary>Reborn at Urðr's Well (Phase 7): High Initiate / High first job / transcendent, Base 255.</summary>
        public bool Reborn;

        /// <summary>The second job of the first life: decides the only transcendent job a reborn character can take.</summary>
        public JobId RebirthPath = JobId.Initiate;
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

        /// <summary>Rented a Merchant Pushcart (Phase 6): +8,000 weight capacity, and street vending in Vigrid Haven.</summary>
        public bool HasPushcart;

        public string MapId;

        /// <summary>Where you revive and where Raven Feathers take you: the map of the last Norn Courier you saved with.</summary>
        public string SaveMapId;

        public bool HasSavedPosition;
        public float PosX;
        public float PosY;
        public float PosZ;

        public List<ItemStack> Inventory = new List<ItemStack>();

        /// <summary>
        /// The Pushcart hold (Phase 6): goods out on a street stall, and anything a trade or a purchase couldn't fit in
        /// the bag. Returned to the bag when there's room (see <see cref="Social.ItemTransfer.ReturnCartToBag"/>).
        /// </summary>
        public List<ItemStack> Cart = new List<ItemStack>();

        public HotkeySlot[] Hotkeys = HotkeyLayout.CreateEmptyArray();

        /// <summary>The 10-slot paperdoll, indexed by <see cref="EquipPosition"/>. Null/empty entries are empty slots.</summary>
        public ItemStack[] Equipment = new ItemStack[EquipmentSet.Positions];

        /// <summary>0 = saved before equipment existed; <see cref="Sanitize"/> migrates it (equips the job's starter weapon).</summary>
        public int EquipmentDataVersion;

        /// <summary>Learned skills and levels (Phase 3). Granted skills (First Aid) are always present.</summary>
        public List<LearnedSkill> Skills = new List<LearnedSkill>();

        /// <summary>0 = saved before skill levels existed; <see cref="Sanitize"/> migrates it to <see cref="SkillBook.CurrentDataVersion"/>.</summary>
        public int SkillDataVersion;

        /// <summary>Loki's Mimicry (Phase 7): the skill this Outlaw copied last, and the level it can be used at.</summary>
        public string MimicSkillId;

        public int MimicSkillLevel;

        /// <summary>Phase 8: every pet this character has hatched (remembered while back in its egg), and the egg of the one that's out.</summary>
        public List<Pets.PetState> Pets = new List<Pets.PetState>();

        public string ActivePetEgg;

        /// <summary>Phase 8: the mercenary under contract (none when its id is empty).</summary>
        public Mercenaries.MercenaryContract Mercenary;

        /// <summary>Phase 8: the homunculus a Brewmaster wove (none when it has no name).</summary>
        public Homunculi.HomunculusState Homunculus;

        /// <summary>Phase 8: what the character is riding (<see cref="MountRules"/>); null on foot.</summary>
        public string Mount;

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

            copy.Cart = new List<ItemStack>();
            if (Cart != null)
            {
                foreach (var stack in Cart)
                {
                    if (stack != null)
                    {
                        copy.Cart.Add(stack.Clone());
                    }
                }
            }

            copy.Equipment = new ItemStack[EquipmentSet.Positions];
            var equipment = EquipmentSet.Normalize(Equipment);
            for (int i = 0; i < equipment.Length; i++)
            {
                copy.Equipment[i] = equipment[i]?.Clone();
            }

            copy.Hotkeys = (HotkeySlot[])HotkeyLayout.Normalize(Hotkeys).Clone();
            copy.Pets = new List<Pets.PetState>();
            if (Pets != null)
            {
                foreach (var pet in Pets)
                {
                    if (pet != null)
                    {
                        copy.Pets.Add(new Pets.PetState { EggItemId = pet.EggItemId, Name = pet.Name, Intimacy = pet.Intimacy, Hunger = pet.Hunger, ArmorItemId = pet.ArmorItemId });
                    }
                }
            }
            copy.Mercenary = Mercenary == null ? null : new Mercenaries.MercenaryContract
            {
                MercenaryId = Mercenary.MercenaryId, SecondsLeft = Mercenary.SecondsLeft, Hp = Mercenary.Hp, Kills = Mercenary.Kills,
            };
            copy.Homunculus = Homunculus == null ? null : new Homunculi.HomunculusState
            {
                Kind = Homunculus.Kind, Name = Homunculus.Name, Level = Homunculus.Level, Exp = Homunculus.Exp, Hp = Homunculus.Hp,
                Out = Homunculus.Out, RestSeconds = Homunculus.RestSeconds,
            };
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
            Cart = Social.ItemTransfer.Clean(Cart);
            Pets = Pets ?? new List<Pets.PetState>();
            Pets.RemoveAll(p => p == null || Runeheir.Pets.PetCatalog.ForEgg(p.EggItemId) == null);
            foreach (var pet in Pets)
            {
                pet.Intimacy = Math.Max(0, Math.Min(Runeheir.Pets.PetCatalog.MaxIntimacy, pet.Intimacy));
                pet.Hunger = Math.Max(0, Math.Min(100, pet.Hunger));
            }

            if (Runeheir.Pets.PetCatalog.Active(this) == null)
            {
                ActivePetEgg = null;
            }

            if (Runeheir.Mercenaries.MercenaryCatalog.Active(this) == null)
            {
                Mercenary = null;
            }
            else
            {
                Mercenary.SecondsLeft = Math.Min(Runeheir.Mercenaries.MercenaryCatalog.ContractSeconds, Mercenary.SecondsLeft);
                Mercenary.Hp = Math.Max(0, Mercenary.Hp);
            }

            if (!MountRules.StillValid(this, Mount))
            {
                Mount = null;
            }

            if (Homunculus == null || !Homunculus.Exists || !Enum.IsDefined(typeof(Homunculi.HomunculusKind), Homunculus.Kind))
            {
                Homunculus = null;
            }
            else
            {
                Homunculus.Level = Math.Max(1, Math.Min(Homunculi.HomunculusRules.MaxLevel, Homunculus.Level));
                Homunculus.Exp = Math.Max(0L, Homunculus.Exp);
                Homunculus.Hp = Math.Max(0, Homunculus.Hp);
                Homunculus.RestSeconds = Math.Max(0f, Math.Min(Homunculi.HomunculusRules.RestAfterFalling, Homunculus.RestSeconds));
            }

            Hotkeys = HotkeyLayout.Normalize(Hotkeys);
            // Unknown enum values (hand-edited save, or written by a newer build) are repaired, never thrown on,
            // so one bad record can't hide every character on the account.
            if (!JobDatabase.Exists(Job))
            {
                Job = JobId.Initiate;
            }

            // Freyja's Kin are Doram and the Doram are Freyja's Kin.
            if (Race != CharacterRace.Human && Race != CharacterRace.Doram)
            {
                Race = CharacterRace.Human;
            }

            if (Job == JobId.FreyjasKin)
            {
                Race = CharacterRace.Doram;
            }
            else if (Race == CharacterRace.Doram)
            {
                Race = CharacterRace.Human;
            }

            RebirthRules.Sanitize(this);
            MimicryRules.Sanitize(this);

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

            if (string.IsNullOrEmpty(SaveMapId) || MapCatalog.Get(SaveMapId) == null)
            {
                SaveMapId = MapCatalog.StartingMapId;
            }

            SkillPoints = Math.Max(0, SkillPoints);
            SkillBook.SanitizeSkills(this);
            EquipmentSet.SanitizeEquipment(this);
            Zeny = Math.Max(0, Zeny);
        }
    }
}
