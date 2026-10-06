using System.Collections.Generic;
using Runeheir.Hotkeys;
using Runeheir.Items;
using Runeheir.Player;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// E / Alt+E inventory (Ragnarok tabs: Items · Gear · Etc). Double-click or right-click: use a consumable, wear
    /// gear, or pick a piece to compound a Soul Card into. While the storage is open, it deposits instead.
    /// Drag consumables or gear onto F1–F10.
    /// </summary>
    public sealed class InventoryWindow
    {
        private enum Tab
        {
            Items = 0,
            Gear = 1,
            Etc = 2,
        }

        private const int Columns = 7;
        private const int Rows = 5;
        private const float SlotSize = 52f;
        private const float Gap = 6f;
        private const float GridTop = 34f;

        private static readonly string[] TabNames = { "Items", "Gear", "Etc" };

        private readonly HudController _hud;
        private readonly PlayerCharacter _player;
        private readonly Image[] _icons = new Image[Columns * Rows];
        private readonly Text[] _glyphs = new Text[Columns * Rows];
        private readonly Text[] _amounts = new Text[Columns * Rows];
        private readonly Text[] _badges = new Text[Columns * Rows];
        private readonly Button[] _tabButtons = new Button[TabNames.Length];
        private readonly List<ItemStack> _shown = new List<ItemStack>();
        private readonly Text _footer;
        private readonly Text _pageLabel;
        private Tab _tab;
        private int _page;

        public InventoryWindow(HudController hud, PlayerCharacter player)
        {
            _hud = hud;
            _player = player;
            float gridHeight = Rows * (SlotSize + Gap);
            Window = UIWindow.Create(hud.Canvas.transform, "Items", 1480f, 110f, 428f, GridTop + gridHeight + 110f);

            for (int t = 0; t < TabNames.Length; t++)
            {
                var tab = (Tab)t;
                _tabButtons[t] = UIFactory.CreateButton(Window.Content, TabNames[t], () => SelectTab(tab), 13);
                _tabButtons[t].GetComponent<RectTransform>().SetRect(t * 102f, 0f, 96f, 26f);
            }

            for (int i = 0; i < _icons.Length; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                var slot = UIFactory.CreatePanel(Window.Content, "ItemSlot" + i, UITheme.SlotBg);
                slot.rectTransform.SetRect(column * (SlotSize + Gap), GridTop + row * (SlotSize + Gap), SlotSize, SlotSize);
                _icons[i] = UIFactory.CreateIcon(slot.transform, string.Empty, Color.gray, 14);
                _icons[i].rectTransform.Stretch(4f, 4f, 4f, 4f);
                _glyphs[i] = _icons[i].GetComponentInChildren<Text>();
                _amounts[i] = UIFactory.CreateText(slot.transform, string.Empty, 13, Color.white, TextAnchor.LowerRight, FontStyle.Bold);
                _amounts[i].rectTransform.Stretch(2f, 2f, 5f, 3f);
                UIFactory.AddOutline(_amounts[i], Color.black, 1f);
                _badges[i] = UIFactory.CreateText(slot.transform, string.Empty, 12, new Color(0.55f, 1f, 0.6f), TextAnchor.UpperLeft, FontStyle.Bold);
                _badges[i].rectTransform.Stretch(4f, 2f, 2f, 2f);
                UIFactory.AddOutline(_badges[i], Color.black, 1f);

                int index = i;
                var drag = slot.gameObject.AddComponent<UIDragSource>();
                drag.PayloadProvider = () => Payload(index);
                var pointer = slot.gameObject.AddComponent<UIPointerHandler>();
                pointer.DoubleClick = () => Activate(index);
                pointer.RightClick = () => Activate(index);
                pointer.PointerEnter = () => UITooltip.Show(Tooltip(index));
                pointer.PointerExit = UITooltip.Hide;
            }

            float below = GridTop + gridHeight + 2f;
            var previous = UIFactory.CreateButton(Window.Content, "<", () => TurnPage(-1), 14);
            previous.GetComponent<RectTransform>().SetRect(0f, below, 34f, 26f);
            _pageLabel = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.TextDim, TextAnchor.MiddleCenter);
            _pageLabel.rectTransform.SetRect(36f, below, 90f, 26f);
            var next = UIFactory.CreateButton(Window.Content, ">", () => TurnPage(1), 14);
            next.GetComponent<RectTransform>().SetRect(128f, below, 34f, 26f);
            var equipment = UIFactory.CreateButton(Window.Content, "Equipment (Q)", hud.ToggleEquipment, 13);
            equipment.GetComponent<RectTransform>().SetRect(270f, below, 130f, 26f);

            _footer = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.TextDim, TextAnchor.UpperLeft);
            _footer.rectTransform.SetRect(0f, below + 32f, 404f, 40f);

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

            _shown.Clear();
            foreach (var stack in _player.Inventory.Stacks)
            {
                if (TabOf(stack.Definition) == _tab)
                {
                    _shown.Add(stack);
                }
            }

            int pageSize = _icons.Length;
            int pages = Mathf.Max(1, (_shown.Count + pageSize - 1) / pageSize);
            _page = Mathf.Clamp(_page, 0, pages - 1);
            _pageLabel.text = $"{_page + 1} / {pages}";

            for (int i = 0; i < _icons.Length; i++)
            {
                var entry = EntryAt(i);
                var item = entry?.Definition;
                _icons[i].gameObject.SetActive(item != null);
                _amounts[i].text = item != null && item.IsStackable ? entry.Amount.ToString("N0") : string.Empty;
                _badges[i].text = item != null && item.IsEquipment ? Badge(entry) : string.Empty;
                if (item != null)
                {
                    _icons[i].color = RuntimeMaterials.Hex(item.IconColorHex);
                    _glyphs[i].text = item.IconLabel;
                }
            }

            for (int t = 0; t < _tabButtons.Length; t++)
            {
                _tabButtons[t].GetComponent<Image>().color = t == (int)_tab ? UITheme.Gold : Color.white;
            }

            string hint = _hud.InventoryClickOverride != null ? "Double-click: move to storage" : "Double-click: use / wear";
            _footer.text = $"Weight {_player.CurrentWeight:N0} / {_player.Stats.WeightCapacity:N0}    Zeny {_player.Record.Zeny:N0}\n" +
                           $"<size=12>{_player.Inventory.Stacks.Count}/{Inventory.MaxEntries} entries · {hint}</size>";
        }

        private static Tab TabOf(ItemDefinition item)
        {
            if (item == null)
            {
                return Tab.Etc;
            }

            if (item.IsEquipment)
            {
                return Tab.Gear;
            }

            return item.Kind == ItemKind.Consumable ? Tab.Items : Tab.Etc;
        }

        private static string Badge(ItemStack entry)
        {
            string refine = entry.Refine > 0 ? "+" + entry.Refine : string.Empty;
            string cards = entry.CardCount > 0 ? new string('◆', entry.CardCount) : string.Empty;
            return refine.Length > 0 && cards.Length > 0 ? refine + "\n" + cards : refine + cards;
        }

        private void SelectTab(Tab tab)
        {
            _tab = tab;
            _page = 0;
            Refresh();
        }

        private void TurnPage(int delta)
        {
            _page += delta;
            Refresh();
        }

        private ItemStack EntryAt(int index)
        {
            int i = _page * _icons.Length + index;
            return i < _shown.Count ? _shown[i] : null;
        }

        private string Tooltip(int index)
        {
            var entry = EntryAt(index);
            var item = entry?.Definition;
            if (item == null)
            {
                return null;
            }

            string hint = _hud.InventoryClickOverride != null ? "double-click: store"
                : item.IsEquipment ? "double-click: wear"
                : item.IsCard ? "double-click: compound"
                : item.IsUsable ? "double-click: use"
                : null;
            return ItemTooltips.For(entry, hint);
        }

        private DragPayload Payload(int index)
        {
            var item = EntryAt(index)?.Definition;
            return item == null || !(item.IsUsable || item.IsEquipment)
                ? null
                : new DragPayload
                {
                    Slot = HotkeySlot.Item(item.Id),
                    Glyph = item.IconLabel,
                    Color = RuntimeMaterials.Hex(item.IconColorHex),
                };
        }

        private void Activate(int index)
        {
            var entry = EntryAt(index);
            var item = entry?.Definition;
            if (item == null)
            {
                return;
            }

            if (_hud.InventoryClickOverride != null)
            {
                _hud.InventoryClickOverride(entry);
                return;
            }

            if (item.IsEquipment)
            {
                _player.Equip(entry);
            }
            else if (item.IsCard)
            {
                _hud.OpenCardCompound(item.Id);
            }
            else if (item.IsUsable)
            {
                _player.UseItem(item.Id);
            }
        }
    }
}
