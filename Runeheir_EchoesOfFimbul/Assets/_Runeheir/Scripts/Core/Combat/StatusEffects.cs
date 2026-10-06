using System;
using System.Collections.Generic;
using Runeheir.Stats;

namespace Runeheir.Combat
{
    /// <summary>Negative statuses (GDD §3 skills, §6 cards). Stagger comes from the poise system, not from skills.</summary>
    public enum StatusEffect
    {
        None = 0,
        Stun = 1,
        Freeze = 2,
        StoneCurse = 3,
        Sleep = 4,
        Poison = 5,
        Bleeding = 6,
        Silence = 7,
        Blind = 8,
        Frostbite = 9,
        Curse = 10,
        Root = 11,
        Stagger = 12,
    }

    [Flags]
    public enum StatusFlags
    {
        None = 0,

        /// <summary>Can't move, attack, cast or use skills.</summary>
        Incapacitates = 1 << 0,

        /// <summary>Can't use skills (still attacks and walks).</summary>
        BlocksSkills = 1 << 1,

        /// <summary>Can't walk (still attacks and casts).</summary>
        BlocksMovement = 1 << 2,

        /// <summary>Ends when the target takes damage (after that hit lands).</summary>
        BreaksOnDamage = 1 << 3,

        /// <summary>No natural HP/SP regeneration.</summary>
        BlocksRegen = 1 << 4,
    }

    /// <summary>Which defender stat resists a status (chance and duration).</summary>
    public enum StatusResistStat
    {
        None = 0,
        Vit = 1,
        Int = 2,
        Luk = 3,
        Agi = 4,

        /// <summary>INT/2 + hard MDEF (freeze and stone, as in Ragnarok).</summary>
        Mdef = 5,
    }

    public sealed class StatusInfo
    {
        public StatusEffect Status;
        public string Name;
        public string IconLabel;
        public string ColorHex;
        public StatusFlags Flags;
        public StatusResistStat ResistStat;

        /// <summary>Percent of max HP lost per DoT tick (never kills: stops at 1 HP).</summary>
        public float DotPercent;

        public float DotInterval = 1f;
        public StatModifiers Modifiers = StatModifiers.Empty();

        public bool Has(StatusFlags flag)
        {
            return (Flags & flag) != 0;
        }
    }

    /// <summary>The defender's stats that resist statuses.</summary>
    public struct StatusResistances
    {
        public int Vit;
        public int Int;
        public int Luk;
        public int Agi;
        public int Mdef;

        /// <summary>Immune to every status (MVPs in Phase 5).</summary>
        public bool Immune;
    }

    public static class StatusRules
    {
        /// <summary>Most a stat can resist: even 255 VIT leaves a 20% stun chance.</summary>
        public const float MaxResistPercent = 80f;

        /// <summary>Resist percent = stat / 2 (VIT 100 → 50% less likely and 25% shorter).</summary>
        public const float StatPerResistPercent = 2f;

        private static readonly Dictionary<StatusEffect, StatusInfo> Table = new Dictionary<StatusEffect, StatusInfo>();

        static StatusRules()
        {
            Add(StatusEffect.Stun, "Stun", "STN", "#F4D03F", StatusFlags.Incapacitates, StatusResistStat.Vit);
            Add(StatusEffect.Freeze, "Frozen", "FRZ", "#85C1E9", StatusFlags.Incapacitates, StatusResistStat.Mdef);
            Add(StatusEffect.StoneCurse, "Stone Curse", "STN", "#A6ACAF", StatusFlags.Incapacitates | StatusFlags.BreaksOnDamage, StatusResistStat.Mdef,
                mods: new StatModifiers { DefPercent = -50f, MdefPercent = 25f });
            Add(StatusEffect.Sleep, "Sleep", "SLP", "#A569BD", StatusFlags.Incapacitates | StatusFlags.BreaksOnDamage, StatusResistStat.Int);
            Add(StatusEffect.Poison, "Poison", "PSN", "#7D3C98", StatusFlags.BlocksRegen, StatusResistStat.Vit,
                dotPercent: 1.5f, dotInterval: 1f, mods: new StatModifiers { DefPercent = -25f });
            Add(StatusEffect.Bleeding, "Bleeding", "BLD", "#C0392B", StatusFlags.BlocksRegen, StatusResistStat.Vit,
                dotPercent: 2f, dotInterval: 2f);
            Add(StatusEffect.Silence, "Silence", "SIL", "#5D6D7E", StatusFlags.BlocksSkills, StatusResistStat.Vit);
            Add(StatusEffect.Blind, "Blind", "BLN", "#1C2833", StatusFlags.None, StatusResistStat.Int,
                mods: new StatModifiers { HitPercent = -25f, FleePercent = -25f });

            // GDD Jormungandr's Brood card: "halving enemy movement speed and ASPD; unblockable".
            Add(StatusEffect.Frostbite, "Frostbite", "FRB", "#AED6F1", StatusFlags.None, StatusResistStat.None,
                mods: new StatModifiers { MoveSpeedPercent = -50f, AspdPercent = -50f });
            Add(StatusEffect.Curse, "Curse", "CRS", "#4A235A", StatusFlags.None, StatusResistStat.Luk,
                mods: new StatModifiers { PhysicalDamagePercent = -25f, MoveSpeedPercent = -10f }.SetStat(StatType.Luk, -StatFormulas.MaxStat));
            Add(StatusEffect.Root, "Snared", "SNR", "#6E2C00", StatusFlags.BlocksMovement, StatusResistStat.Agi);
            Add(StatusEffect.Stagger, "Staggered", "STG", "#E59866", StatusFlags.Incapacitates, StatusResistStat.None);
        }

        public static StatusInfo Get(StatusEffect status)
        {
            return Table.TryGetValue(status, out var info) ? info : null;
        }

        public static IEnumerable<StatusInfo> All => Table.Values;

        /// <summary>Percent (0..80) by which the defender resists <paramref name="status"/>.</summary>
        public static float ResistPercent(StatusEffect status, in StatusResistances defender)
        {
            var info = Get(status);
            if (info == null)
            {
                return 0f;
            }

            float stat;
            switch (info.ResistStat)
            {
                case StatusResistStat.Vit: stat = defender.Vit; break;
                case StatusResistStat.Int: stat = defender.Int; break;
                case StatusResistStat.Luk: stat = defender.Luk; break;
                case StatusResistStat.Agi: stat = defender.Agi; break;
                case StatusResistStat.Mdef: stat = defender.Int / 2f + defender.Mdef; break;
                default: return 0f;
            }

            return StatFormulas.Clamp(stat / StatPerResistPercent, 0f, MaxResistPercent);
        }

        /// <summary>Chance after resistance, in percent. A 100% skill chance stays guaranteed only against 0 resist.</summary>
        public static float EffectiveChance(StatusEffect status, float baseChancePercent, in StatusResistances defender)
        {
            if (defender.Immune || status == StatusEffect.None)
            {
                return 0f;
            }

            return Math.Max(0f, baseChancePercent) * (1f - ResistPercent(status, defender) / 100f);
        }

        /// <summary>Duration after resistance: resist shortens it by half its percent (VIT 100 → 25% shorter).</summary>
        public static float EffectiveDuration(StatusEffect status, float baseSeconds, in StatusResistances defender)
        {
            return Math.Max(0f, baseSeconds) * (1f - ResistPercent(status, defender) / 200f);
        }

        /// <summary>Rolls a status application. 100% chance against no resistance never rolls (Random can return 1.0).</summary>
        public static bool Roll(StatusEffect status, float baseChancePercent, in StatusResistances defender, IRandomSource random)
        {
            float chance = EffectiveChance(status, baseChancePercent, defender);
            if (chance <= 0f)
            {
                return false;
            }

            return chance >= 100f || random.Chance(chance);
        }

        private static void Add(StatusEffect status, string name, string icon, string color, StatusFlags flags, StatusResistStat resist,
            float dotPercent = 0f, float dotInterval = 1f, StatModifiers mods = null)
        {
            Table[status] = new StatusInfo
            {
                Status = status,
                Name = name,
                IconLabel = icon,
                ColorHex = color,
                Flags = flags,
                ResistStat = resist,
                DotPercent = dotPercent,
                DotInterval = dotInterval,
                Modifiers = mods ?? StatModifiers.Empty(),
            };
        }
    }

    public sealed class ActiveStatus
    {
        public StatusInfo Info;
        public double ExpiresAt;
        public double NextDotAt;

        public StatusEffect Status => Info.Status;

        public double Remaining(double now)
        {
            return Math.Max(0.0, ExpiresAt - now);
        }
    }

    /// <summary>
    /// Statuses on one combatant. Time is passed in (seconds), like <see cref="BuffContainer"/>, so the same
    /// code can run on a Mirror server. Damage-over-time ticks are reported back for the owner to apply.
    /// </summary>
    public sealed class StatusContainer
    {
        private readonly List<ActiveStatus> _active = new List<ActiveStatus>();
        private readonly StatModifiers _aggregate = StatModifiers.Empty();
        private StatusFlags _flags;
        private bool _dirty = true;

        public event Action Changed;

        public IReadOnlyList<ActiveStatus> Active => _active;

        public bool IsIncapacitated => (Flags & StatusFlags.Incapacitates) != 0;

        public bool BlocksSkills => (Flags & (StatusFlags.Incapacitates | StatusFlags.BlocksSkills)) != 0;

        public bool BlocksMovement => (Flags & (StatusFlags.Incapacitates | StatusFlags.BlocksMovement)) != 0;

        public bool BlocksRegen => (Flags & StatusFlags.BlocksRegen) != 0;

        public StatModifiers Aggregate
        {
            get
            {
                Refresh();
                return _aggregate;
            }
        }

        private StatusFlags Flags
        {
            get
            {
                Refresh();
                return _flags;
            }
        }

        /// <summary>Adds or extends a status (the longer remaining time wins). Returns false for unknown/zero.</summary>
        public bool Apply(StatusEffect status, float seconds, double now)
        {
            var info = StatusRules.Get(status);
            if (info == null || seconds <= 0f)
            {
                return false;
            }

            var existing = Find(status);
            if (existing != null)
            {
                existing.ExpiresAt = Math.Max(existing.ExpiresAt, now + seconds);
            }
            else
            {
                _active.Add(new ActiveStatus { Info = info, ExpiresAt = now + seconds, NextDotAt = now + info.DotInterval });
            }

            MarkChanged();
            return true;
        }

        public bool Has(StatusEffect status)
        {
            return Find(status) != null;
        }

        public double Remaining(StatusEffect status, double now)
        {
            return Find(status)?.Remaining(now) ?? 0.0;
        }

        public bool Remove(StatusEffect status)
        {
            int index = _active.FindIndex(s => s.Status == status);
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

        /// <summary>Removes statuses that end when hit (sleep, stone). Call after the hit has landed.</summary>
        public void BreakOnDamage()
        {
            if (_active.RemoveAll(s => s.Info.Has(StatusFlags.BreaksOnDamage)) > 0)
            {
                MarkChanged();
            }
        }

        /// <summary>Expires statuses and reports due damage-over-time ticks (percent of max HP) into <paramref name="dots"/>.</summary>
        public void Tick(double now, List<float> dots = null)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var status = _active[i];
                if (status.Info.DotPercent > 0f)
                {
                    // Ticks that fell due before expiry still count, so a 1 s poison deals its one tick.
                    while (status.NextDotAt <= now && status.NextDotAt <= status.ExpiresAt)
                    {
                        dots?.Add(status.Info.DotPercent);
                        status.NextDotAt += Math.Max(0.1f, status.Info.DotInterval);
                    }
                }

                if (status.ExpiresAt <= now)
                {
                    _active.RemoveAt(i);
                    MarkChanged();
                }
            }
        }

        public ActiveStatus Find(StatusEffect status)
        {
            foreach (var active in _active)
            {
                if (active.Status == status)
                {
                    return active;
                }
            }

            return null;
        }

        private void Refresh()
        {
            if (!_dirty)
            {
                return;
            }

            _aggregate.Clear();
            _flags = StatusFlags.None;
            foreach (var status in _active)
            {
                _aggregate.Add(status.Info.Modifiers);
                _flags |= status.Info.Flags;
            }

            _dirty = false;
        }

        private void MarkChanged()
        {
            _dirty = true;
            Changed?.Invoke();
        }
    }
}
