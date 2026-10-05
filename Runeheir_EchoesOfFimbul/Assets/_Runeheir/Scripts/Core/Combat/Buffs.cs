using System;
using System.Collections.Generic;
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

        /// <summary>Immune to stun/freeze (Sowilo).</summary>
        CrowdControlImmune = 1 << 2,
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
    }

    public sealed class ActiveBuff
    {
        public BuffDefinition Definition;
        public double ExpiresAt;
        public int ChargesLeft;

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
                        _aggregate.Add(buff.Definition.Modifiers);
                    }

                    _dirty = false;
                }

                return _aggregate;
            }
        }

        public void Apply(BuffDefinition definition, double now)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var existing = Find(definition.Id);
            if (existing != null)
            {
                existing.ExpiresAt = now + definition.Duration;
                existing.ChargesLeft = definition.Charges;
            }
            else
            {
                _active.Add(new ActiveBuff
                {
                    Definition = definition,
                    ExpiresAt = now + definition.Duration,
                    ChargesLeft = definition.Charges,
                });
            }

            MarkChanged();
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

    /// <summary>Buffs used by Phase 2 skills and runestones (GDD §3 and §7).</summary>
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
                Description = "+7 ASPD and attack recovery canceling.",
                IconLabel = "THS",
                IconColorHex = "#D35400",
                Duration = 60f,
                Modifiers = new StatModifiers { AspdFlat = 7f, CancelAttackRecovery = true },
            });
            Register(new BuffDefinition
            {
                Id = RageOfThor,
                Name = "Rage of Thor",
                Description = "Max HP x3, ASPD locked to 195, items locked, uninterruptible.",
                IconLabel = "ROT",
                IconColorHex = "#C0392B",
                Duration = 30f,
                Modifiers = new StatModifiers
                {
                    MaxHpMultiplier = 3f,
                    AspdOverride = 195f,
                    ItemsLocked = true,
                    UninterruptibleCasting = true,
                },
            });
            Register(new BuffDefinition
            {
                Id = MiasmaWeapon,
                Name = "Miasma Weapon",
                Description = "Physical damage x4 for 40 seconds.",
                IconLabel = "MIA",
                IconColorHex = "#6C3483",
                Duration = 40f,
                Modifiers = new StatModifiers { PhysicalDamagePercent = 300f },
            });
            Register(new BuffDefinition
            {
                Id = RunicAegis,
                Name = "Runic Aegis",
                Description = "Blocks the next 10 physical melee strikes.",
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
                Description = "Immune to stun and freeze for 10 seconds.",
                IconLabel = "SOW",
                IconColorHex = "#F7DC6F",
                Duration = 10f,
                Traits = BuffTraits.CrowdControlImmune,
            });
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
