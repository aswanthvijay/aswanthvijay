using System;
using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Player;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// Active buffs and statuses (top-right) with remaining time, charges and stacks, Ragnarok status-icon style.
    /// Debuffs and negative statuses get a red frame; Spirit Spheres show their count.
    /// </summary>
    public sealed class BuffTray
    {
        private const float Size = 40f;
        private const float Gap = 8f;
        private const int PerRow = 8;

        private static readonly Color Harmful = new Color(0.9f, 0.3f, 0.25f, 1f);

        private readonly PlayerCharacter _player;
        private readonly RectTransform _root;
        private readonly List<Entry> _entries = new List<Entry>();

        public BuffTray(HudController hud, PlayerCharacter player)
        {
            _player = player;
            _root = UIFactory.CreateRect("Buffs", hud.Canvas.transform);
            SetRightInset(0f);
            player.Buffs.Changed += Rebuild;
            player.Statuses.Changed += Rebuild;
            Rebuild();
        }

        /// <summary>Keeps the tray clear of whatever sits in the top-right corner (the minimap, at its current size).</summary>
        public void SetRightInset(float occupied)
        {
            _root.Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f - occupied, -14f), new Vector2(PerRow * (Size + Gap), 140f));
        }

        public void Dispose()
        {
            _player.Buffs.Changed -= Rebuild;
            _player.Statuses.Changed -= Rebuild;
        }

        public void Tick()
        {
            double now = Time.timeAsDouble;
            foreach (var entry in _entries)
            {
                double remaining = entry.Remaining(now);
                string suffix = entry.Suffix != null ? entry.Suffix() : string.Empty;
                entry.Timer.text = (remaining >= 60 ? $"{remaining / 60:0}m" : $"{remaining:0}s") + suffix;
            }
        }

        private void Rebuild()
        {
            foreach (var entry in _entries)
            {
                UnityEngine.Object.Destroy(entry.Root);
            }

            _entries.Clear();
            foreach (var status in _player.Statuses.Active)
            {
                var info = status.Info;
                var captured = status;
                Add(info.IconLabel, RuntimeMaterials.Hex(info.ColorHex), harmful: true,
                    $"<b><color=#E6735C>{info.Name}</color></b>\n{StatusHelp(info)}",
                    now => captured.Remaining(now), null);
            }

            foreach (var buff in _player.Buffs.Active)
            {
                var definition = buff.Definition;
                var captured = buff;
                string level = buff.Level > 1 ? $" Lv {buff.Level}" : string.Empty;
                Add(definition.IconLabel, RuntimeMaterials.Hex(definition.IconColorHex), definition.IsDebuff,
                    $"<b><color={(definition.IsDebuff ? "#E6735C" : "#EBC466")}>{definition.Name}{level}</color></b>\n{definition.Description}",
                    now => captured.Remaining(now),
                    () => captured.ChargesLeft > 0 ? $" ×{captured.ChargesLeft}" : definition.MaxStacks > 1 ? $" ●{captured.Stacks}" : string.Empty);
            }

            Tick();
        }

        private void Add(string glyph, Color color, bool harmful, string tooltip, Func<double, double> remaining, Func<string> suffix)
        {
            int index = _entries.Count;
            float x = -(index % PerRow) * (Size + Gap);
            float y = -(index / PerRow) * (Size + 24f);
            var icon = UIFactory.CreateIcon(_root, glyph, color, 12);
            icon.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(x, y), new Vector2(Size, Size));
            icon.raycastTarget = true;
            if (harmful)
            {
                UIFactory.AddOutline(icon, Harmful, 2f);
            }

            var pointer = icon.gameObject.AddComponent<UIPointerHandler>();
            pointer.PointerEnter = () => UITooltip.Show(tooltip);
            pointer.PointerExit = UITooltip.Hide;

            var timer = UIFactory.CreateText(icon.transform, string.Empty, 12, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
            timer.rectTransform.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(70f, 18f));
            UIFactory.AddOutline(timer, Color.black, 1f);
            _entries.Add(new Entry { Root = icon.gameObject, Timer = timer, Remaining = remaining, Suffix = suffix });
        }

        private static string StatusHelp(StatusInfo info)
        {
            switch (info.Status)
            {
                case StatusEffect.Stun: return "Can't move, attack or use skills.";
                case StatusEffect.Freeze: return "Frozen solid: can't act. Blunt weapons deal triple damage to you.";
                case StatusEffect.StoneCurse: return "Turned to stone: can't act, DEF halved. Breaks when hit.";
                case StatusEffect.Sleep: return "Asleep: can't act. Wakes when hit.";
                case StatusEffect.Poison: return "Losing HP every second, -25% DEF, no natural regen.";
                case StatusEffect.Bleeding: return "Losing HP every 2 seconds, no natural regen.";
                case StatusEffect.Silence: return "Can't use skills.";
                case StatusEffect.Blind: return "-25% HIT and FLEE.";
                case StatusEffect.Frostbite: return "Half movement speed and attack speed.";
                case StatusEffect.Curse: return "LUK 0, -25% physical damage, slower.";
                case StatusEffect.Root: return "Snared: can't walk (you can still attack and cast).";
                case StatusEffect.Stagger: return "Poise broken: briefly can't act.";
                default: return string.Empty;
            }
        }

        private sealed class Entry
        {
            public GameObject Root;
            public Text Timer;
            public Func<double, double> Remaining;
            public Func<string> Suffix;
        }
    }
}
