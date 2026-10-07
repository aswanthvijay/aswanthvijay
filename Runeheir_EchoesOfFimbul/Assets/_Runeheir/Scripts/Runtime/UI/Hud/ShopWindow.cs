using Runeheir.Items;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>NPC shop: Buy from the shop's list or Sell anything in your bag for half its price. Amount box for stacks.</summary>
    public sealed class ShopWindow
    {
        private readonly HudController _hud;
        private readonly PlayerCharacter _player;
        private readonly UIScrollList _list;
        private readonly Text _header;
        private readonly Text _footer;
        private readonly InputField _amount;
        private readonly Button _buyTab;
        private readonly Button _sellTab;
        private ShopDefinition _shop;
        private bool _selling;
        private float _ignoreClicksUntil;

        public ShopWindow(HudController hud, PlayerCharacter player)
        {
            _hud = hud;
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Shop", 420f, 120f, 460f, 600f);

            _header = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.TextDim, TextAnchor.UpperLeft);
            _header.rectTransform.SetRect(0f, 0f, 436f, 36f);
            _buyTab = UIFactory.CreateButton(Window.Content, "Buy", () => SetSelling(false), 14);
            _buyTab.GetComponent<RectTransform>().SetRect(0f, 40f, 100f, 28f);
            _sellTab = UIFactory.CreateButton(Window.Content, "Sell", () => SetSelling(true), 14);
            _sellTab.GetComponent<RectTransform>().SetRect(106f, 40f, 100f, 28f);

            var amountLabel = UIFactory.CreateText(Window.Content, "Amount", 13, UITheme.TextDim, TextAnchor.MiddleRight);
            amountLabel.rectTransform.SetRect(240f, 40f, 90f, 28f);
            _amount = UIFactory.CreateInputField(Window.Content, "1", characterLimit: 5, fontSize: 14);
            _amount.contentType = InputField.ContentType.IntegerNumber;
            _amount.GetComponent<RectTransform>().SetRect(336f, 40f, 100f, 28f);

            _list = new UIScrollList(Window.Content, 0f, 76f, 436f, 430f);
            _footer = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.Text, TextAnchor.MiddleLeft);
            _footer.rectTransform.SetRect(0f, 512f, 436f, 24f);

            player.Inventory.Changed += Refresh;
            Window.VisibilityChanged += Refresh;
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Dispose()
        {
            _player.Inventory.Changed -= Refresh;
        }

        public void Open(ShopDefinition shop, bool selling)
        {
            _shop = shop;
            _selling = selling;
            Window.Title.text = shop.Name;
            Window.Show();
            _list.ScrollToTop();
            Refresh();
        }

        private void SetSelling(bool selling)
        {
            _selling = selling;
            _list.ScrollToTop();
            Refresh();
        }

        private int Amount => int.TryParse(_amount.text, out int value) && value > 0 ? value : 1;

        private void Refresh()
        {
            if (!Window.IsOpen || _shop == null)
            {
                return;
            }

            float discount = _player.Stats.BuyDiscountPercent;
            float bonus = _player.Stats.SellBonusPercent;
            _header.text = _selling
                ? "Double-click an item to sell it (the amount box sets how many from a stack). Worn gear isn't for sale: take it off first."
                  + (bonus > 0f ? $"\n<color=#EBC466>Silver Tongue: +{bonus:0.#}%</color>" : string.Empty)
                : $"<i>\"{_shop.Greeting}\"</i>\nDouble-click an item to buy the amount in the box."
                  + (discount > 0f ? $"  <color=#EBC466>Haggle: -{discount:0.#}%</color>" : string.Empty);
            _buyTab.GetComponent<Image>().color = _selling ? Color.white : UITheme.Gold;
            _sellTab.GetComponent<Image>().color = _selling ? UITheme.Gold : Color.white;
            _footer.text = $"Zeny <b><color=#EBC466>{_player.Record.Zeny:N0}</color></b>    Weight {_player.CurrentWeight:N0} / {_player.Stats.WeightCapacity:N0}";

            _list.Clear();
            if (_selling)
            {
                _list.EmptyText = "Your bag is empty.";
                foreach (var entry in _player.Inventory.Stacks)
                {
                    var item = entry.Definition;
                    if (item == null || item.SellPrice <= 0)
                    {
                        continue;
                    }

                    var captured = entry;
                    _list.Add(item.IconLabel, RuntimeMaterials.Hex(item.IconColorHex), entry.DisplayName,
                        $"{TradeRules.SellPrice(item, bonus):N0} z each{(entry.Amount > 1 ? $" · x{entry.Amount:N0}" : string.Empty)}",
                        null, () => ItemTooltips.For(captured, "double-click: sell"), onDoubleClick: () => Sell(captured));
                }
            }
            else
            {
                _list.EmptyText = "Sold out.";
                foreach (string id in _shop.ItemIds)
                {
                    var item = ItemCatalog.Get(id);
                    if (item == null)
                    {
                        continue;
                    }

                    int owned = _player.Inventory.Count(id);
                    _list.Add(item.IconLabel, RuntimeMaterials.Hex(item.IconColorHex), item.Name,
                        $"<color=#EBC466>{TradeRules.BuyPrice(item, discount):N0} z</color>{(owned > 0 ? $" · you have {owned:N0}" : string.Empty)}",
                        null, () => ItemTooltips.For(Preview(item), "double-click: buy"), onDoubleClick: () => Buy(id));
                }
            }
        }

        private static ItemStack Preview(ItemDefinition item)
        {
            return item.IsEquipment ? ItemStack.NewInstance(item) : new ItemStack(item.Id, 1);
        }

        /// <summary>The list rebuilds after every trade; a click that lands on the row that moved under the pointer is ignored.</summary>
        private bool Debounce()
        {
            if (Time.unscaledTime < _ignoreClicksUntil)
            {
                return true;
            }

            _ignoreClicksUntil = Time.unscaledTime + 0.4f;
            return false;
        }

        private void Buy(string itemId)
        {
            if (Debounce())
            {
                return;
            }

            if (TradeRules.TryBuy(_player.Record, _player.Inventory, _shop, itemId, Amount, _player.Stats.WeightCapacity, _player.CurrentWeight, out string message,
                    _player.Stats.BuyDiscountPercent))
            {
                ChatLog.Loot(message);
                _player.SaveNow();
            }
            else
            {
                ChatLog.Error(message);
            }

            Refresh();
        }

        private void Sell(ItemStack entry)
        {
            if (Debounce())
            {
                return;
            }

            var item = entry.Definition;
            int amount = Mathf.Min(Amount, entry.Amount);
            if (item.IsEquipment)
            {
                bool invested = entry.Refine > 0 || entry.CardCount > 0 || entry.Glyphs != null && System.Array.Exists(entry.Glyphs, g => !string.IsNullOrEmpty(g));
                _hud.Confirm($"Sell <b>{entry.DisplayName}</b> for {TradeRules.SellPrice(item, _player.Stats.SellBonusPercent):N0} zeny?" + (invested ? "\nIts refine, cards and glyphs are lost with it." : string.Empty),
                    () => DoSell(entry, amount), "Sell");
                return;
            }

            DoSell(entry, amount);
        }

        private void DoSell(ItemStack entry, int amount)
        {
            if (TradeRules.TrySell(_player.Record, _player.Inventory, entry, amount, out string message, _player.Stats.SellBonusPercent))
            {
                ChatLog.Loot(message);
                _player.SaveNow();
            }
            else
            {
                ChatLog.Error(message);
            }

            Refresh();
        }
    }
}
