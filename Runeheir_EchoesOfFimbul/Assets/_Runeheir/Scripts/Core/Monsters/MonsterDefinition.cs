using System.Collections.Generic;
using Runeheir.Combat;

namespace Runeheir.Monsters
{
    public enum MonsterShape
    {
        Blob = 0,
        Biped = 1,
        Quadruped = 2,
        Flyer = 3,
        Dummy = 4,

        /// <summary>Coiled body and a raised head (nagas, drakes, wyrms, the World Serpent's brood).</summary>
        Serpent = 5,

        /// <summary>Stacked blocks of stone or ice.</summary>
        Golem = 6,

        /// <summary>A hooded shape floating over the ground, no legs (wraiths, banshees).</summary>
        Wraith = 7,

        /// <summary>A person-shaped warrior drawn with the player model: outlaws, berserkers, draugr, Hel's soldiers.</summary>
        Humanoid = 8,
    }

    /// <summary>GDD §6: normal monsters, 2-hour field mini-bosses and 1-hour MVP world bosses.</summary>
    public enum MonsterRank
    {
        Normal = 0,
        MiniBoss = 1,
        Mvp = 2,
    }

    public sealed class DropEntry
    {
        public string ItemId;

        /// <summary>Base chance in percent before the server drop rate.</summary>
        public float ChancePercent;

        public DropEntry(string itemId, float chancePercent)
        {
            ItemId = itemId;
            ChancePercent = chancePercent;
        }
    }

    public sealed class MonsterDefinition
    {
        /// <summary>Basic attacks from this far (meters) or more are shots or spells, not swings.</summary>
        public const float RangedThreshold = 3f;

        public string Id;
        public string Name;
        public int Level;
        public int MaxHp;
        public int AtkMin;
        public int AtkMax;
        public int Def;
        public int Mdef;
        public int Hit;
        public int Flee;
        public Element Element;
        public Race Race;
        public Size Size;
        public long BaseExp;
        public long JobExp;

        public MonsterRank Rank;

        /// <summary>Element of basic attacks (skills carry their own).</summary>
        public Element AttackElement = Element.Neutral;

        /// <summary>Attacks players on sight (otherwise only retaliates).</summary>
        public bool Aggressive;

        /// <summary>Comes to the aid of nearby monsters of the same kind when they're attacked (wolf packs, outlaw bands).</summary>
        public bool Assist;

        /// <summary>Never moves (training dummy).</summary>
        public bool Stationary;

        /// <summary>Never attacks.</summary>
        public bool Passive;

        /// <summary>Refills HP instead of dying (training dummy).</summary>
        public bool Immortal;

        /// <summary>Only appears when a boss calls it (Sköll and Hati): no card, no drops, not in branches.</summary>
        public bool SummonOnly;

        public float MoveSpeed = 3f;
        public float AttackRange = 0.9f;

        /// <summary>Seconds between attacks.</summary>
        public float AttackInterval = 1.4f;

        public float AggroRange = 8f;

        /// <summary>Gives up and walks home past this distance from its spawn.</summary>
        public float LeashRange = 25f;

        public float RespawnSeconds = 8f;
        public List<DropEntry> Drops = new List<DropEntry>();

        /// <summary>Skills in priority order: each think, the first usable one that passes its chance roll is used.</summary>
        public List<MonsterSkill> Skills = new List<MonsterSkill>();

        /// <summary>Boss phases, highest HP threshold first (see <see cref="MonsterSkillRules.PhaseFor"/>).</summary>
        public List<BossPhase> Phases = new List<BossPhase>();

        /// <summary>MVP only: bonus base EXP for the MVP (the top damage dealer).</summary>
        public long MvpExp;

        /// <summary>MVP only: rolled in order for the MVP; the first that succeeds goes straight into their bag.</summary>
        public List<DropEntry> MvpDrops = new List<DropEntry>();

        public MonsterShape Shape;
        public string ColorHex = "#999999";
        public float Scale = 1f;

        /// <summary>Humanoid shape: what it carries and wears (item ids), and its skin tone.</summary>
        public WeaponType LookWeapon = WeaponType.Unarmed;

        public string LookHead;
        public string LookLower;
        public string LookShield;
        public string LookGarment;
        public string SkinHex = "#B8C4C9";

        public bool IsBoss => Rank != MonsterRank.Normal;

        public bool IsMvp => Rank == MonsterRank.Mvp;

        /// <summary>Shoots or casts its basic attacks (projectile visuals, no melee block).</summary>
        public bool IsRanged => AttackRange >= RangedThreshold;

        /// <summary>
        /// Statuses it can never get: bosses shrug off hard crowd control (stun, freeze, stone, sleep, root, silence and
        /// curse; stagger still works, through their much larger poise); Undead-element monsters can't be frozen or
        /// petrified and Water-element ones can't be frozen (the Frozen Revenant and Frost Wolf cards copy this).
        /// </summary>
        public int StatusImmunityMask
        {
            get
            {
                int mask = 0;
                if (IsBoss)
                {
                    foreach (var status in BossImmunities)
                    {
                        mask |= StatusResistances.Bit(status);
                    }
                }

                if (Element == Element.Undead)
                {
                    mask |= StatusResistances.Bit(StatusEffect.Freeze) | StatusResistances.Bit(StatusEffect.StoneCurse);
                }
                else if (Element == Element.Water)
                {
                    mask |= StatusResistances.Bit(StatusEffect.Freeze);
                }

                return mask;
            }
        }

        public static readonly StatusEffect[] BossImmunities =
        {
            StatusEffect.Stun, StatusEffect.Freeze, StatusEffect.StoneCurse, StatusEffect.Sleep, StatusEffect.Root,
            StatusEffect.Silence, StatusEffect.Curse,
        };

        /// <summary>Monsters resist statuses with Level / 2 in every resisting stat plus their MDEF.</summary>
        public StatusResistances Resistances => new StatusResistances
        {
            Vit = Level / 2,
            Int = Level / 2,
            Luk = Level / 2,
            Agi = Level / 2,
            Mdef = Mdef,
            ImmunityMask = StatusImmunityMask,
        };

        /// <summary>Poise before a stagger: the size/level base, x3 for mini-bosses and x5 for MVPs.</summary>
        public float MaxPoise => PoiseRules.MonsterMaxPoise(Size, Level) * (Rank == MonsterRank.Mvp ? 5f : Rank == MonsterRank.MiniBoss ? 3f : 1f);

        public MonsterSkill Skill(string id)
        {
            return Skills.Find(s => s.Id == id);
        }
    }
}
