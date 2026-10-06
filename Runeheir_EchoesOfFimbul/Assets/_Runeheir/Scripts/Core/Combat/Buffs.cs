using System;
using System.Collections.Generic;
using Runeheir.Skills;
using Runeheir.Stats;

namespace Runeheir.Combat
{
    [Flags]
    public enum BuffTraits
    {
        None = 0,

        /// <summary>Charges make basic attacks guaranteed crits (Tiwaz).</summary>
        CriticalCharges = 1 << 0,

        /// <summary>Charges block physical melee hits (Runic Aegis).</summary>
        MeleeBlockCharges = 1 << 1,

        /// <summary>Immune to every status, stagger included (Sowilo).</summary>
        CrowdControlImmune = 1 << 2,

        /// <summary>Hidden: monsters lose track of you and can't target you. Ends when you attack or use a skill.</summary>
        Stealth = 1 << 3,

        /// <summary>With <see cref="Stealth"/>: the attack that breaks it is a guaranteed critical (Shadow Veil).</summary>
        AmbushCritical = 1 << 4,

        /// <summary>Ends when the next damaging spell is cast (Rune Amplify).</summary>
        ConsumedBySpell = 1 << 5,
    }

    public sealed class BuffDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public string IconLabel;
        public string IconColorHex = "#8E9AAF";

        /// <summary>Seconds. Charge-based buffs also expire when their charges run out.</summary>
        public float Duration;

        public int Charges;
        public BuffTraits Traits;
        public StatModifiers Modifiers = StatModifiers.Empty();

        /// <summary>Added once per skill level above 1 (Blessing: +1 STR/INT/DEX per level).</summary>
        public StatModifiers ModifiersPerLevel;

        /// <summary>Recasting adds a stack up to this many (Spirit Spheres). 1 = recasting just refreshes.</summary>
        public int MaxStacks = 1;

        /// <summary>Added once per stack (a Spirit Sphere's +3 ATK).</summary>
        public StatModifiers ModifiersPerStack;

        /// <summary>Shown red in the buff tray and removed by Purify/Sowilo (Provoke, Decrease AGI).</summary>
        public bool IsDebuff;

        /// <summary>Auto-casts a skill on basic-attack hits while active (Auto Rune).</summary>
        public ProcDefinition Proc;

        public bool Has(BuffTraits trait)
        {
            return (Traits & trait) != 0;
        }
    }

    public sealed class ActiveBuff
    {
        public BuffDefinition Definition;
        public double ExpiresAt;
        public int ChargesLeft;

        /// <summary>Skill level it was cast at (scales <see cref="BuffDefinition.ModifiersPerLevel"/>).</summary>
        public int Level = 1;

        public int Stacks = 1;

        public double Remaining(double now)
        {
            return Math.Max(0.0, ExpiresAt - now);
        }
    }

    /// <summary>
    /// Active buffs on one combatant. Time is passed in (seconds) so it is testable and engine-agnostic.
    /// </summary>
    public sealed class BuffContainer
    {
        private readonly List<ActiveBuff> _active = new List<ActiveBuff>();
        private readonly StatModifiers _aggregate = StatModifiers.Empty();
        private bool _dirty = true;

        public event Action Changed;

        public IReadOnlyList<ActiveBuff> Active => _active;

        public StatModifiers Aggregate
        {
            get
            {
                if (_dirty)
                {
                    _aggregate.Clear();
                    foreach (var buff in _active)
                    {
                        var definition = buff.Definition;
                        _aggregate.Add(definition.Modifiers);
                        _aggregate.AddScaled(definition.ModifiersPerLevel, buff.Level - 1);
                        _aggregate.AddScaled(definition.ModifiersPerStack, buff.Stacks);
                    }

                    _dirty = false;
                }

                return _aggregate;
            }
        }

        /// <summary>
        /// Applies or refreshes a buff. <paramref name="level"/> is the skill level it was cast at;
        /// <paramref name="duration"/> and <paramref name="charges"/> override the definition when &gt; 0.
        /// Stacking buffs gain one stack per application up to <see cref="BuffDefinition.MaxStacks"/>.
        /// </summary>
        /// <param name="stackLimit">Caps stacks below <see cref="BuffDefinition.MaxStacks"/> when &gt; 0 (Spirit Call: one sphere per level).</param>
        public ActiveBuff Apply(BuffDefinition definition, double now, int level = 1, float duration = 0f, int charges = 0, int stackLimit = 0)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            float seconds = duration > 0f ? duration : definition.Duration;
            int chargeCount = charges > 0 ? charges : definition.Charges;
            int maxStacks = Math.Max(1, stackLimit > 0 ? Math.Min(stackLimit, definition.MaxStacks) : definition.MaxStacks);
            var existing = Find(definition.Id);
            if (existing != null)
            {
                existing.ExpiresAt = now + seconds;
                existing.ChargesLeft = chargeCount;
                existing.Level = Math.Max(1, level);
                existing.Stacks = Math.Min(maxStacks, existing.Stacks + 1);
            }
            else
            {
                existing = new ActiveBuff
                {
                    Definition = definition,
                    ExpiresAt = now + seconds,
                    ChargesLeft = chargeCount,
                    Level = Math.Max(1, level),
                    Stacks = 1,
                };
                _active.Add(existing);
            }

            MarkChanged();
            return existing;
        }

        /// <summary>Removes up to <paramref name="count"/> stacks; returns how many were taken (Finger Offensive).</summary>
        public int TakeStacks(string id, int count)
        {
            var buff = Find(id);
            if (buff == null || count <= 0)
            {
                return 0;
            }

            int taken = Math.Min(count, buff.Stacks);
            buff.Stacks -= taken;
            if (buff.Stacks <= 0)
            {
                _active.Remove(buff);
            }

            MarkChanged();
            return taken;
        }

        /// <summary>Removes every buff with <paramref name="trait"/>; returns how many.</summary>
        public int RemoveWithTrait(BuffTraits trait)
        {
            return RemoveWhere(b => b.Definition.Has(trait));
        }

        public int StacksOf(string id)
        {
            return Find(id)?.Stacks ?? 0;
        }

        /// <summary>Removes every buff matching <paramref name="predicate"/>; returns how many.</summary>
        public int RemoveWhere(Predicate<ActiveBuff> predicate)
        {
            int removed = _active.RemoveAll(predicate);
            if (removed > 0)
            {
                MarkChanged();
            }

            return removed;
        }

        /// <summary>Ends stealth (attacking or using a skill). Returns the stealth buff that ended, if any.</summary>
        public ActiveBuff BreakStealth()
        {
            foreach (var buff in _active)
            {
                if (buff.Definition.Has(BuffTraits.Stealth))
                {
                    _active.Remove(buff);
                    MarkChanged();
                    return buff;
                }
            }

            return null;
        }

        public bool Remove(string id)
        {
            int index = _active.FindIndex(b => b.Definition.Id == id);
            if (index < 0)
            {
                return false;
            }

            _active.RemoveAt(index);
            MarkChanged();
            return true;
        }

        public void Clear()
        {
            if (_active.Count == 0)
            {
                return;
            }

            _active.Clear();
            MarkChanged();
        }

        /// <summary>Removes expired buffs. Call once per frame (or per server tick).</summary>
        public void Tick(double now)
        {
            int removed = _active.RemoveAll(b => b.ExpiresAt <= now);
            if (removed > 0)
            {
                MarkChanged();
            }
        }

        public bool Has(string id)
        {
            return Find(id) != null;
        }

        public bool HasTrait(BuffTraits trait)
        {
            foreach (var buff in _active)
            {
                if ((buff.Definition.Traits & trait) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasCharge(BuffTraits trait)
        {
            foreach (var buff in _active)
            {
                if ((buff.Definition.Traits & trait) != 0 && buff.ChargesLeft > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Uses one charge of a buff with <paramref name="trait"/>; removes it at zero.</summary>
        public bool TryConsumeCharge(BuffTraits trait)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                var buff = _active[i];
                if ((buff.Definition.Traits & trait) == 0 || buff.ChargesLeft <= 0)
                {
                    continue;
                }

                buff.ChargesLeft--;
                if (buff.ChargesLeft == 0)
                {
                    _active.RemoveAt(i);
                }

                MarkChanged();
                return true;
            }

            return false;
        }

        public ActiveBuff Find(string id)
        {
            foreach (var buff in _active)
            {
                if (buff.Definition.Id == id)
                {
                    return buff;
                }
            }

            return null;
        }

        private void MarkChanged()
        {
            _dirty = true;
            Changed?.Invoke();
        }
    }

    /// <summary>Buffs used by skills (registered by <see cref="SkillBuffs"/>) and runestones (GDD §3 and §7).</summary>
    public static class BuffCatalog
    {
        public const string TwoHandSurge = "two_hand_surge";
        public const string RageOfThor = "rage_of_thor";
        public const string MiasmaWeapon = "miasma_weapon";
        public const string RunicAegis = "runic_aegis";
        public const string UruzMight = "uruz_might";
        public const string TiwazPrecision = "tiwaz_precision";
        public const string SowiloWard = "sowilo_ward";

        private static readonly Dictionary<string, BuffDefinition> ById = new Dictionary<string, BuffDefinition>();

        static BuffCatalog()
        {
            Register(new BuffDefinition
            {
                Id = TwoHandSurge,
                Name = "Two-Hand Surge",
                Description = "+0.7 ASPD per level (+7 at Lv 10) and attack recovery canceling.",
                IconLabel = "THS",
                IconColorHex = "#D35400",
                Duration = 60f,
                Modifiers = new StatModifiers { AspdFlat = 0.7f, CancelAttackRecovery = true },
                ModifiersPerLevel = new StatModifiers { AspdFlat = 0.7f },
            });
            Register(new BuffDefinition
            {
                Id = RageOfThor,
                Name = "Rage of Thor",
                Description = "Max HP x3, ASPD locked to 195, items locked, hyper-armor (no flinch, knockback or stagger; casts can't be interrupted).",
                IconLabel = "ROT",
                IconColorHex = "#C0392B",
                Duration = 30f,
                Modifiers = new StatModifiers
                {
                    MaxHpMultiplier = 3f,
                    AspdOverride = 195f,
                    ItemsLocked = true,
                    UninterruptibleCasting = true,
                    HyperArmor = true,
                },
            });
            Register(new BuffDefinition
            {
                Id = MiasmaWeapon,
                Name = "Miasma Weapon",
                Description = "Physical damage x2, +50% per level (x4 at Lv 5).",
                IconLabel = "MIA",
                IconColorHex = "#6C3483",
                Duration = 40f,
                Modifiers = new StatModifiers { PhysicalDamagePercent = 100f },
                ModifiersPerLevel = new StatModifiers { PhysicalDamagePercent = 50f },
            });
            Register(new BuffDefinition
            {
                Id = RunicAegis,
                Name = "Runic Aegis",
                Description = "Blocks physical melee strikes (one per skill level).",
                IconLabel = "AEG",
                IconColorHex = "#2E86C1",
                Duration = 60f,
                Charges = 10,
                Traits = BuffTraits.MeleeBlockCharges,
            });
            Register(new BuffDefinition
            {
                Id = UruzMight,
                Name = "Uruz Might",
                Description = "+25 STR.",
                IconLabel = "URZ",
                IconColorHex = "#A04000",
                Duration = 60f,
                Modifiers = new StatModifiers().SetStat(StatType.Str, 25),
            });
            Register(new BuffDefinition
            {
                Id = TiwazPrecision,
                Name = "Tiwaz Precision",
                Description = "Next 3 basic attacks are guaranteed criticals.",
                IconLabel = "TIW",
                IconColorHex = "#F1C40F",
                Duration = 60f,
                Charges = 3,
                Traits = BuffTraits.CriticalCharges,
            });
            Register(new BuffDefinition
            {
                Id = SowiloWard,
                Name = "Sowilo Ward",
                Description = "Every negative status cleansed; immune to statuses and stagger for 10 seconds.",
                IconLabel = "SOW",
                IconColorHex = "#F7DC6F",
                Duration = 10f,
                Traits = BuffTraits.CrowdControlImmune,
            });

            SkillBuffs.RegisterAll(Register);
        }

        public static IEnumerable<BuffDefinition> All => ById.Values;

        public static BuffDefinition Get(string id)
        {
            return id != null && ById.TryGetValue(id, out var definition) ? definition : null;
        }

        private static void Register(BuffDefinition definition)
        {
            ById[definition.Id] = definition;
        }
    }
}
