using System;
using Runeheir.Stats;

namespace Runeheir.Combat
{
    /// <summary>
    /// Card-style damage bonuses, in percent. Bonuses inside one category add; categories multiply
    /// (GDD: Runic Berserker [size] x Forest Outlaw [race] = multiplicative PvP math).
    /// Offense (Vs*) is used on the attacker, defense (TakenFrom*) on the defender. Null means "no bonuses".
    /// </summary>
    public sealed class DamageBonuses
    {
        public readonly float[] VsRace = new float[CombatEnumCounts.Races];
        public readonly float[] VsSize = new float[CombatEnumCounts.Sizes];
        public readonly float[] VsElement = new float[CombatEnumCounts.Elements];

        /// <summary>% damage taken from an attacker race / attack element (Draugr Footman: DemiHuman -30).</summary>
        public readonly float[] TakenFromRace = new float[CombatEnumCounts.Races];

        public readonly float[] TakenFromElement = new float[CombatEnumCounts.Elements];

        public float Multiplier(in DefenderProfile defender)
        {
            return (1f + VsRace[(int)defender.Race] / 100f)
                   * (1f + VsSize[(int)defender.Size] / 100f)
                   * (1f + VsElement[(int)defender.Element] / 100f);
        }

        /// <summary>Defensive multiplier against a hit from <paramref name="attackerRace"/> with <paramref name="attackElement"/>.</summary>
        public float TakenMultiplier(Race attackerRace, Element attackElement)
        {
            return Math.Max(0f, 1f + TakenFromRace[(int)attackerRace] / 100f)
                   * Math.Max(0f, 1f + TakenFromElement[(int)attackElement] / 100f);
        }
    }

    public struct AttackerProfile
    {
        public int StatusAtk;
        public int WeaponAtk;

        /// <summary>Random spread of weapon ATK, e.g. 0.05 = ±5%.</summary>
        public float WeaponVariance;

        public WeaponType Weapon;
        public Element AttackElement;
        public int Hit;

        /// <summary>Critical chance in percent.</summary>
        public float CritChance;

        /// <summary>Tiwaz runestone: the next basic attack is a guaranteed crit.</summary>
        public bool ForceCritical;

        /// <summary>Skips the HIT vs FLEE roll (Fist of Odin). HIT alone can't do this: hit chance caps at 95%.</summary>
        public bool NeverMiss;

        public int MatkMin;
        public int MatkMax;

        /// <summary>+% physical damage (Miasma Weapon = 300).</summary>
        public float PhysicalDamagePercent;

        public float MagicDamagePercent;

        /// <summary>Percent of DEF ignored (Jormungandr's Brood card = 40, Occult Strike = 100).</summary>
        public float DefBypassPercent;

        /// <summary>Percent of MDEF ignored (Jormungandr's Brood 40, Frost Wyrm 10).</summary>
        public float MdefBypassPercent;

        /// <summary>+% on top of the 140% critical damage (Dire Wolf Card).</summary>
        public float CritDamagePercent;

        /// <summary>The attacker's race, for the defender's race cards (players are Demi-Human).</summary>
        public Race Race;

        public DamageBonuses Bonuses;
    }

    public struct DefenderProfile
    {
        public int Def;
        public int SoftDef;
        public int Mdef;
        public int SoftMdef;
        public int Flee;
        public Element Element;
        public Race Race;
        public Size Size;

        /// <summary>Damage multiplier vs blunt weapons (1 normally, 3 while frozen).</summary>
        public float BluntDamageTakenMultiplier;

        /// <summary>The defender's race/element damage reduction (shield, garment and armor cards). Null = none.</summary>
        public DamageBonuses Resist;
    }

    public struct DamageResult
    {
        public int Amount;
        public bool IsCritical;
        public bool IsMiss;
        public bool IsBlocked;

        /// <summary>Poison/bleeding tick: no hit reaction, no cast interrupt, shown smaller.</summary>
        public bool IsDamageOverTime;

        public float ElementMultiplier;

        /// <summary>A spell (Hagalaz Rebound reflects these).</summary>
        public bool IsMagical;

        /// <summary>Part of the hit was soaked up by a shield (Isa); <see cref="Amount"/> is what got through.</summary>
        public int Absorbed;

        public static DamageResult Miss()
        {
            return new DamageResult { IsMiss = true, ElementMultiplier = 1f };
        }

        public static DamageResult Blocked()
        {
            return new DamageResult { IsBlocked = true, ElementMultiplier = 1f };
        }

        public static DamageResult Fixed(int amount)
        {
            return new DamageResult { Amount = Math.Max(0, amount), ElementMultiplier = 1f };
        }
    }

    /// <summary>
    /// Physical and magical damage. Pure and deterministic given an <see cref="IRandomSource"/>,
    /// so the same code can run on a Mirror server later.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// GDD §4 LUK: crits "deal flat 140% true damage". Read as: a crit ignores DEF and elemental resistance
        /// (the element multiplier never drops below 100%), while weaknesses, cards and buffs still apply.
        /// Set to false for classic Ragnarok crits, which are still scaled down by the element table.
        /// </summary>
        public const bool CritsIgnoreElementResistance = true;

        public const float MinHitChance = 5f;
        public const float MaxHitChance = 95f;

        /// <summary>Hard DEF constant: damage x (4000 + DEF) / (4000 + DEF x 10). Diminishing returns.</summary>
        public const float HardDefConstant = 4000f;

        /// <summary>Hard MDEF constant: damage x (1000 + MDEF) / (1000 + MDEF x 10).</summary>
        public const float HardMdefConstant = 1000f;

        /// <summary>Ragnarok hit rate: 80 + HIT - FLEE, clamped to 5..95%.</summary>
        public static float HitChance(int hit, int flee)
        {
            return StatFormulas.Clamp(80f + hit - flee, MinHitChance, MaxHitChance);
        }

        public static float HardDefReduction(float def)
        {
            def = Math.Max(0f, def);
            return (HardDefConstant + def) / (HardDefConstant + def * 10f);
        }

        public static float HardMdefReduction(float mdef)
        {
            mdef = Math.Max(0f, mdef);
            return (HardMdefConstant + mdef) / (HardMdefConstant + mdef * 10f);
        }

        /// <param name="skillPercent">100 for a basic attack, 300 for Bash, etc.</param>
        /// <param name="canCrit">Basic attacks can crit; most skills cannot.</param>
        public static DamageResult Physical(
            in AttackerProfile attacker,
            in DefenderProfile defender,
            float skillPercent,
            bool canCrit,
            IRandomSource random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            bool critical = canCrit && (attacker.ForceCritical || random.Chance(attacker.CritChance));

            // Crits always connect (Ragnarok rule); everything else rolls HIT vs FLEE.
            if (!critical && !attacker.NeverMiss && !random.Chance(HitChance(attacker.Hit, defender.Flee)))
            {
                return DamageResult.Miss();
            }

            double variance = 1.0 + (random.NextDouble() * 2.0 - 1.0) * attacker.WeaponVariance;
            double sizeModifier = WeaponRules.SizeModifierPercent(attacker.Weapon, defender.Size) / 100.0;
            double damage = attacker.StatusAtk + attacker.WeaponAtk * variance * sizeModifier;
            damage *= skillPercent / 100.0;

            float bypass = StatFormulas.Clamp(attacker.DefBypassPercent, 0f, 100f) / 100f;
            if (critical)
            {
                // GDD: crits bypass physical DEF and deal 140% (plus crit-damage cards).
                damage *= StatFormulas.CriticalDamageMultiplier * (1.0 + attacker.CritDamagePercent / 100.0);
            }
            else
            {
                damage *= HardDefReduction(defender.Def * (1f - bypass));
                damage -= defender.SoftDef * (1f - bypass);
            }

            float elementMultiplier = ElementTable.Multiplier(attacker.AttackElement, defender.Element);
            if (critical && CritsIgnoreElementResistance)
            {
                elementMultiplier = Math.Max(1f, elementMultiplier);
            }

            damage *= elementMultiplier;

            if (attacker.Bonuses != null)
            {
                damage *= attacker.Bonuses.Multiplier(defender);
            }

            damage *= 1.0 + attacker.PhysicalDamagePercent / 100.0;

            if (WeaponRules.IsBlunt(attacker.Weapon) && defender.BluntDamageTakenMultiplier > 0f)
            {
                damage *= defender.BluntDamageTakenMultiplier;
            }

            if (defender.Resist != null)
            {
                damage *= defender.Resist.TakenMultiplier(attacker.Race, attacker.AttackElement);
            }

            return new DamageResult
            {
                Amount = Finalize(damage, elementMultiplier),
                IsCritical = critical,
                ElementMultiplier = elementMultiplier,
            };
        }

        /// <summary>Magic always hits; reduced by hard and soft MDEF.</summary>
        public static DamageResult Magical(
            in AttackerProfile attacker,
            in DefenderProfile defender,
            float skillPercent,
            Element element,
            IRandomSource random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            double matk = random.Range(attacker.MatkMin, Math.Max(attacker.MatkMin, attacker.MatkMax));
            double damage = matk * skillPercent / 100.0;

            float bypass = StatFormulas.Clamp(attacker.MdefBypassPercent, 0f, 100f) / 100f;
            damage *= HardMdefReduction(defender.Mdef * (1f - bypass));
            damage -= defender.SoftMdef * (1f - bypass);

            float elementMultiplier = ElementTable.Multiplier(element, defender.Element);
            damage *= elementMultiplier;
            damage *= 1.0 + attacker.MagicDamagePercent / 100.0;
            if (defender.Resist != null)
            {
                damage *= defender.Resist.TakenMultiplier(attacker.Race, element);
            }

            return new DamageResult
            {
                Amount = Finalize(damage, elementMultiplier),
                ElementMultiplier = elementMultiplier,
                IsMagical = true,
            };
        }

        private static int Finalize(double damage, float elementMultiplier)
        {
            if (elementMultiplier <= 0f)
            {
                return 0;
            }

            return Math.Max(1, (int)Math.Round(damage));
        }
    }
}
