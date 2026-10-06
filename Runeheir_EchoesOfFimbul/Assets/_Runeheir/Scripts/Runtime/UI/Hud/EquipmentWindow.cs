using System.Text;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Player;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// Q / Alt+Q paperdoll (GDD §5): the ten equipment positions around a summary of what the gear adds.
    /// Double-click or right-click a piece to take it off. Drag-free: wear gear from the inventory.
    /// </summary>
    public sealed class EquipmentWindow
    {
        private const float SlotWidth = 180f;
        private const float SlotHeight = 52f;

        private static readonly EquipPosition[] LeftColumn =
        {
            EquipPosition.HeadUpper, EquipPosition.HeadMid, EquipPosition.HeadLower, EquipPosition.Armor, EquipPosition.Weapon,
        };

        private static readonly EquipPosition[] RightColumn =
        {
            EquipPosition.Garment, EquipPosition.Footgear, EquipPosition.Shield, EquipPosition.Accessory1, EquipPosition.Accessory2,
        };

        private readonly PlayerCharacter _player;
        private readonly Image[] _icons = new Image[EquipmentSet.Positions];
        private readonly Text[] _glyphs = new Text[EquipmentSet.Positions];
        private readonly Text[] _names = new Text[EquipmentSet.Positions];
        private readonly Text _summary;

        public EquipmentWindow(HudController hud, PlayerCharacter player)
        {
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Equipment", 900f, 110f, 620f, 330f);

            for (int i = 0; i < LeftColumn.Length; i++)
            {
                BuildSlot(LeftColumn[i], 0f, i * (SlotHeight + 4f), rightAligned: false);
                BuildSlot(RightColumn[i], 600f - SlotWidth - 20f, i * (SlotHeight + 4f), rightAligned: true);
            }

            _summary = UIFactory.CreateText(Window.Content, string.Empty, 12, UITheme.Text, TextAnchor.UpperCenter);
            _summary.rectTransform.SetRect(SlotWidth + 8f, 0f, 600f - 2f * SlotWidth - 36f, 280f);

            player.Equipment.Changed += Refresh;
            player.StatsRecalculated += Refresh;
            player.Inventory.Changed += Refresh; // refine/cards change while the forge works on worn pieces
            Window.VisibilityChanged += Refresh;
            Refresh();
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Dispose()
        {
            _player.Equipment.Changed -= Refresh;
            _player.StatsRecalculated -= Refresh;
            _player.Inventory.Changed -= Refresh;
        }

        private void BuildSlot(EquipPosition position, float x, float y, bool rightAligned)
        {
            int index = (int)position;
            var slot = UIFactory.CreatePanel(Window.Content, "Equip" + position, UITheme.SlotBg);
            slot.rectTransform.SetRect(x, y, SlotWidth, SlotHeight);
            _icons[index] = UIFactory.CreateIcon(slot.transform, string.Empty, Color.gray, 12);
            _icons[index].rectTransform.SetRect(rightAligned ? SlotWidth - 46f : 4f, 4f, 42f, 42f);
            _glyphs[index] = _icons[index].GetComponentInChildren<Text>();
            _names[index] = UIFactory.CreateText(slot.transform, string.Empty, 12, UITheme.Text,
                rightAligned ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            _names[index].rectTransform.SetRect(rightAligned ? 4f : 52f, 2f, SlotWidth - 58f, SlotHeight - 4f);

            var pointer = slot.gameObject.AddComponent<UIPointerHandler>();
            pointer.DoubleClick = () => _player.Unequip(position);
            pointer.RightClick = () => _player.Unequip(position);
            pointer.PointerEnter = () =>
            {
                var entry = _player.Equipment.Covering(position);
                UITooltip.Show(entry != null ? ItemTooltips.For(entry, entry == _player.Equipment.Get(position) ? "double-click: take off" : null) : null);
            };
            pointer.PointerExit = UITooltip.Hide;
        }

        public void Refresh()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            for (int i = 0; i < EquipmentSet.Positions; i++)
            {
                var position = (EquipPosition)i;
                var own = _player.Equipment.Get(position);
                var covering = own ?? _player.Equipment.Covering(position);
                var item = covering?.Definition;
                _icons[i].gameObject.SetActive(item != null);
                if (item == null)
                {
                    _names[i].text = $"<color=#5D6D7E>{EquipKinds.Label(position)}</color>";
                    continue;
                }

                _icons[i].color = RuntimeMaterials.Hex(item.IconColorHex);
                _glyphs[i].text = item.IconLabel;
                _names[i].text = own != null
                    ? $"{covering.DisplayName}\n<size=11><color=#9AA8BC>{EquipKinds.Label(position)}</color></size>"
                    : $"<color=#7F8C8D>{item.Name}</color>\n<size=11><color=#7F8C8D>(covers this slot)</color></size>";
            }

            _summary.text = Summary();
        }

        private string Summary()
        {
            var gear = _player.Gear;
            var stats = _player.Stats;
            var weapon = _player.Weapon;
            var text = new StringBuilder();
            text.Append($"<b><color=#EBC466>{_player.DisplayName}</color></b>\n<color=#9AA8BC>{_player.Job.Name} · Base Lv {_player.Record.BaseLevel}</color>\n\n");
            text.Append($"<b>{(gear.HasWeapon ? weapon.Name : "Bare Hands")}</b>");
            if (weapon.Refine > 0)
            {
                text.Append($" <color=#7DCEA0>+{weapon.Refine}</color>");
            }

            text.Append($"\nATK {stats.StatusAtk} + {stats.WeaponAtk}  ·  MATK {stats.MatkMin}–{stats.MatkMax}");
            text.Append($"\nDEF {stats.Def} + {stats.SoftDef}  ·  MDEF {stats.Mdef} + {stats.SoftMdef}");
            text.Append($"\nWeapon element: {weapon.Element}  ·  Armor element: {gear.ArmorElement}");
            if (gear.Runeword != null)
            {
                text.Append($"\n<color=#F7DC6F>Runeword: {gear.Runeword.Name}</color>");
            }

            var immunities = new StringBuilder();
            foreach (StatusEffect status in System.Enum.GetValues(typeof(StatusEffect)))
            {
                if (status != StatusEffect.None && (gear.ImmunityMask & StatusResistances.Bit(status)) != 0)
                {
                    immunities.Append(immunities.Length > 0 ? ", " : string.Empty).Append(StatusRules.Get(status)?.Name ?? status.ToString());
                }
            }

            if (immunities.Length > 0)
            {
                text.Append($"\n<color=#85C1E9>Immune: {immunities}</color>");
            }

            AppendIf(text, stats.CritDamagePercent, "Crit damage {0:+0;-0}%");
            AppendIf(text, stats.LifeStealPercent, "Life steal {0:0}%");
            AppendIf(text, stats.SpStealPercent, "SP steal {0:0}%");
            AppendIf(text, stats.ReflectMeleePercent, "Reflect melee {0:0}%");
            AppendIf(text, stats.DefBypassPercent, "Ignore DEF {0:0}%");
            AppendIf(text, stats.MdefBypassPercent, "Ignore MDEF {0:0}%");
            AppendIf(text, stats.CooldownPercent, "Cooldowns {0:+0;-0}%");
            text.Append($"\n\n<color=#9AA8BC>Weight {_player.CurrentWeight:N0} / {stats.WeightCapacity:N0}\nDouble-click a piece to take it off.</color>");
            return text.ToString();
        }

        private static void AppendIf(StringBuilder text, float value, string format)
        {
            if (Mathf.Abs(value) > 0.01f)
            {
                text.Append('\n').AppendFormat(format, value);
            }
        }
    }
}
