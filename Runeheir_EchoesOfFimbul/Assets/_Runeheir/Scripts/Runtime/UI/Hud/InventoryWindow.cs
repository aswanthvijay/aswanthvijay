using Runeheir.Hotkeys;
using Runeheir.Items;
using Runeheir.Player;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>Alt+E inventory grid. Drag an item onto F1–F10; double-click or right-click to use it.</summary>
    public sealed class InventoryWindow
    {
        private const int Columns = 7;
        private const int Rows = 4;
        private const float SlotSize = 52f;
        private const float Gap = 6f;

        private readonly PlayerCharacter _player;
        private readonly Image[] _icons = new Image[Columns * Rows];
        private readonly Text[] _glyphs = new Text[Columns * Rows];
        private readonly Text[] _amounts = new Text[Columns * Rows];
        private readonly Text _footer;

        public InventoryWindow(HudController hud, PlayerCharacter player)
        {
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Items", 1480f, 110f, 428f, 340f);

            for (int i = 0; i < _icons.Length; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                var slot = UIFactory.CreatePanel(Window.Content, "ItemSlot" + i, UITheme.SlotBg);
                slot.rectTransform.SetRect(column * (SlotSize + Gap), row * (SlotSize + Gap), SlotSize, SlotSize);
                _icons[i] = UIFactory.CreateIcon(slot.transform, string.Empty, Color.gray, 14);
                _icons[i].rectTransform.Stretch(4f, 4f, 4f, 4f);
                _glyphs[i] = _icons[i].GetComponentInChildren<Text>();
                _amounts[i] = UIFactory.CreateText(slot.transform, string.Empty, 13, Color.white, TextAnchor.LowerRight, FontStyle.Bold);
                _amounts[i].rectTransform.Stretch(2f, 2f, 5f, 3f);
                UIFactory.AddOutline(_amounts[i], Color.black, 1f);

                int index = i;
                var drag = slot.gameObject.AddComponent<UIDragSource>();
                drag.PayloadProvider = () => Payload(index);
                var pointer = slot.gameObject.AddComponent<UIPointerHandler>();
                pointer.DoubleClick = () => Use(index);
                pointer.RightClick = () => Use(index);
                pointer.PointerEnter = () => UITooltip.Show(HudIcons.ItemTooltip(ItemAt(index)));
                pointer.PointerExit = UITooltip.Hide;
            }

            _footer = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.TextDim);
            _footer.rectTransform.SetRect(0f, Rows * (SlotSize + Gap) + 6f, 400f, 20f);

            player.Inventory.Changed += Refresh;
            player.StatsRecalculated += Refresh; // weight capacity follows STR, level and buffs
            Window.VisibilityChanged += Refresh;
            Refresh();
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Dispose()
        {
            _player.Inventory.Changed -= Refresh;
            _player.StatsRecalculated -= Refresh;
        }

        public void Refresh()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            var stacks = _player.Inventory.Stacks;
            for (int i = 0; i < _icons.Length; i++)
            {
                var item = i < stacks.Count ? ItemCatalog.Get(stacks[i].ItemId) : null;
                _icons[i].gameObject.SetActive(item != null);
                _amounts[i].text = item != null ? stacks[i].Amount.ToString("N0") : string.Empty;
                if (item != null)
                {
                    _icons[i].color = RuntimeMaterials.Hex(item.IconColorHex);
                    _glyphs[i].text = item.IconLabel;
                }
            }

            _footer.text = $"Weight {_player.Inventory.TotalWeight():N0} / {_player.Stats.WeightCapacity:N0}    Zeny {_player.Record.Zeny:N0}";
        }

        private ItemDefinition ItemAt(int index)
        {
            var stacks = _player.Inventory.Stacks;
            return index < stacks.Count ? ItemCatalog.Get(stacks[index].ItemId) : null;
        }

        private DragPayload Payload(int index)
        {
            var item = ItemAt(index);
            return item == null
                ? null
                : new DragPayload
                {
                    Slot = HotkeySlot.Item(item.Id),
                    Glyph = item.IconLabel,
                    Color = RuntimeMaterials.Hex(item.IconColorHex),
                };
        }

        private void Use(int index)
        {
            var item = ItemAt(index);
            if (item != null)
            {
                _player.UseItem(item.Id);
            }
        }
    }
}
