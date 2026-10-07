using System.Collections.Generic;
using Runeheir.Field;
using Runeheir.Items;
using Runeheir.Online;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Social;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// Player trade (Ragnarok style): double-click items in your bag to offer them (amount box empty = the whole stack),
    /// set zeny, press OK to lock your side, then Trade once both are locked. Opens and closes with the trade itself.
    /// </summary>
    public sealed class TradeWindow
    {
        private readonly HudController _hud;
        private readonly PlayerCharacter _player;
        private readonly Text _title;
        private readonly UIScrollList _mine;
        private readonly UIScrollList _theirs;
        private readonly Text _mineZeny;
        private readonly Text _theirZeny;
        private readonly InputField _amount;
        private readonly InputField _zeny;
        private readonly Button _lock;
        private readonly Button _confirm;

        public TradeWindow(HudController hud, PlayerCharacter player, SocialHud social)
        {
            _hud = hud;
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Trade", 560f, 140f, 560f, 520f, closable: false);
            _title = UIFactory.CreateText(Window.Content, string.Empty, 15, UITheme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            _title.rectTransform.SetRect(0f, 0f, 536f, 26f);

            Label("Your offer (double-click bag items)", 0f, 30f);
            Label("Their offer", 276f, 30f);
            _mine = new UIScrollList(Window.Content, 0f, 54f, 260f, 260f, 40f) { EmptyText = "Nothing yet." };
            _theirs = new UIScrollList(Window.Content, 276f, 54f, 260f, 260f, 40f) { EmptyText = "Nothing yet." };
            _mineZeny = UIFactory.CreateText(Window.Content, string.Empty, 14, UITheme.Text, TextAnchor.MiddleLeft);
            _mineZeny.rectTransform.SetRect(0f, 320f, 260f, 24f);
            _theirZeny = UIFactory.CreateText(Window.Content, string.Empty, 14, UITheme.Text, TextAnchor.MiddleLeft);
            _theirZeny.rectTransform.SetRect(276f, 320f, 260f, 24f);

            Label("Amount", 0f, 352f);
            _amount = UIFactory.CreateInputField(Window.Content, "all", characterLimit: 5, fontSize: 14);
            _amount.contentType = InputField.ContentType.IntegerNumber;
            _amount.GetComponent<RectTransform>().SetRect(70f, 350f, 90f, 28f);
            Label("Zeny", 180f, 352f);
            _zeny = UIFactory.CreateInputField(Window.Content, "0", characterLimit: 10, fontSize: 14);
            _zeny.contentType = InputField.ContentType.IntegerNumber;
            _zeny.GetComponent<RectTransform>().SetRect(230f, 350f, 150f, 28f);
            var setZeny = UIFactory.CreateButton(Window.Content, "Set zeny", SetZeny, 14);
            setZeny.GetComponent<RectTransform>().SetRect(390f, 348f, 146f, 32f);

            _lock = UIFactory.CreateButton(Window.Content, "OK (lock)", () => SocialHud.Service?.TradeLock(), 16);
            _lock.GetComponent<RectTransform>().SetRect(0f, 400f, 172f, 40f);
            _confirm = UIFactory.CreateButton(Window.Content, "Trade", () => SocialHud.Service?.TradeConfirm(), 16);
            _confirm.GetComponent<RectTransform>().SetRect(182f, 400f, 172f, 40f);
            var cancel = UIFactory.CreateButton(Window.Content, "Cancel", () => SocialHud.Service?.TradeCancel(), 16);
            cancel.GetComponent<RectTransform>().SetRect(364f, 400f, 172f, 40f);
            Window.VisibilityChanged += OnVisibilityChanged;
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Dispose()
        {
            _hud.ClearInventoryClickOverride(Offer);
        }

        /// <summary>Opens, refreshes or closes with the trade in the social state.</summary>
        public void Sync()
        {
            var trade = SocialHud.State?.Trade;
            if (trade == null)
            {
                Window.Hide();
                return;
            }

            if (!Window.IsOpen)
            {
                Window.Show();
                _hud.ShowInventory();
            }

            _title.text = $"Trading with {trade.Partner}";
            Fill(_mine, trade.Mine);
            Fill(_theirs, trade.Theirs);
            _mineZeny.text = $"Zeny: {trade.Mine.Zeny:N0}  {State(trade.Mine)}";
            _theirZeny.text = $"Zeny: {trade.Theirs.Zeny:N0}  {State(trade.Theirs)}";
            _lock.interactable = !trade.Mine.Locked;
            _confirm.interactable = trade.Mine.Locked && trade.Theirs.Locked && !trade.Mine.Confirmed;
        }

        private static string State(TradeOffer offer)
        {
            return offer.Confirmed ? "<color=#7CFC9A>✔ Trade</color>" : offer.Locked ? "<color=#EBC466>OK</color>" : string.Empty;
        }

        private static void Fill(UIScrollList list, TradeOffer offer)
        {
            list.Clear();
            foreach (var stack in offer.Items)
            {
                var item = stack?.Definition;
                if (item == null)
                {
                    continue;
                }

                var captured = stack;
                list.Add(item.IconLabel, RuntimeMaterials.Hex(item.IconColorHex), stack.DisplayName, item.IsStackable ? $"x{stack.Amount:N0}" : null,
                    null, () => ItemTooltips.For(captured, null));
            }
        }

        private void OnVisibilityChanged()
        {
            if (Window.IsOpen)
            {
                _hud.SetInventoryClickOverride(Offer);
            }
            else
            {
                _hud.ClearInventoryClickOverride(Offer);
            }
        }

        private void Offer(ItemStack entry)
        {
            if (entry == null || _player.IsWorn(entry))
            {
                ChatLog.Error("Take it off before trading it.");
                return;
            }

            int amount = int.TryParse(_amount.text, out int value) && value > 0 ? Mathf.Min(value, entry.Amount) : entry.Amount;
            SocialHud.Service?.TradeAddItem(entry, amount);
        }

        private void SetZeny()
        {
            if (long.TryParse(_zeny.text, out long zeny) && zeny >= 0)
            {
                SocialHud.Service?.TradeSetZeny(zeny);
            }
        }

        private void Label(string text, float x, float y)
        {
            var label = UIFactory.CreateText(Window.Content, text, 13, UITheme.TextDim, TextAnchor.MiddleLeft);
            label.rectTransform.SetRect(x, y, 260f, 22f);
        }
    }

    /// <summary>
    /// Street stall setup (V, or Gunnar in Vigrid Haven): needs a Pushcart and Vigrid's streets. Double-click bag items to
    /// put them on the stall with the amount and price typed above, click a line to take it off, name the stall and open
    /// it. While it's open this window shows what's left and what it earned.
    /// </summary>
    public sealed class VendingSetupWindow
    {
        private readonly HudController _hud;
        private readonly PlayerCharacter _player;
        private readonly InputField _title;
        private readonly InputField _amount;
        private readonly InputField _price;
        private readonly UIScrollList _list;
        private readonly Text _footer;
        private readonly Button _open;
        private readonly List<VendingRequest> _lines = new List<VendingRequest>();

        public VendingSetupWindow(HudController hud, PlayerCharacter player, SocialHud social)
        {
            _hud = hud;
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Street Stall", 960f, 110f, 460f, 580f);
            var help = UIFactory.CreateText(Window.Content,
                "Type an amount and a price, then double-click items in your bag (E). Click a line to remove it. Up to 12 lines.",
                13, UITheme.TextDim, TextAnchor.UpperLeft);
            help.rectTransform.SetRect(0f, 0f, 436f, 36f);
            help.horizontalOverflow = HorizontalWrapMode.Wrap;

            Label("Title", 0f, 42f);
            _title = UIFactory.CreateInputField(Window.Content, VendingRules.DefaultTitle, characterLimit: VendingRules.MaxTitleLength, fontSize: 14);
            _title.GetComponent<RectTransform>().SetRect(60f, 40f, 376f, 28f);
            Label("Amount", 0f, 76f);
            _amount = UIFactory.CreateInputField(Window.Content, "all", characterLimit: 5, fontSize: 14);
            _amount.contentType = InputField.ContentType.IntegerNumber;
            _amount.GetComponent<RectTransform>().SetRect(60f, 74f, 100f, 28f);
            Label("Price (z)", 180f, 76f);
            _price = UIFactory.CreateInputField(Window.Content, "each", characterLimit: 10, fontSize: 14);
            _price.contentType = InputField.ContentType.IntegerNumber;
            _price.GetComponent<RectTransform>().SetRect(250f, 74f, 186f, 28f);

            _list = new UIScrollList(Window.Content, 0f, 110f, 436f, 320f, 42f) { EmptyText = "Nothing on the stall yet." };
            _footer = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.Text, TextAnchor.MiddleLeft);
            _footer.rectTransform.SetRect(0f, 436f, 436f, 36f);
            _footer.horizontalOverflow = HorizontalWrapMode.Wrap;
            _open = UIFactory.CreateButton(Window.Content, "Open stall", OpenOrClose, 16);
            _open.GetComponent<RectTransform>().SetRect(118f, 478f, 200f, 40f);
            Window.VisibilityChanged += OnVisibilityChanged;
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Dispose()
        {
            _hud.ClearInventoryClickOverride(Add);
        }

        public void Toggle()
        {
            if (Window.IsOpen)
            {
                Window.Hide();
                return;
            }

            if (!SocialHud.RequireOnline())
            {
                return;
            }

            var stall = SocialHud.State?.MyStall;
            if (stall == null && !VendingRules.CanOpen(_player.Record, FieldContext.Map, out string error))
            {
                ChatLog.Error(error);
                return;
            }

            Window.Show();
            if (stall == null)
            {
                _hud.ShowInventory();
            }
        }

        public void Refresh()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            var stall = SocialHud.State?.MyStall;
            _list.Clear();
            if (stall != null)
            {
                foreach (var entry in stall.Entries)
                {
                    var item = entry.Item?.Definition;
                    if (item != null)
                    {
                        _list.Add(item.IconLabel, RuntimeMaterials.Hex(item.IconColorHex), entry.Item.DisplayName,
                            $"x{entry.Item.Amount:N0} left · {entry.Price:N0} z each", null);
                    }
                }

                _footer.text = $"<b>{stall.Title}</b> is open · earned {stall.Earned:N0} zeny. Closing returns unsold goods to your bag.";
                UIFactory.SetButtonLabel(_open, "Close stall");
                return;
            }

            for (int i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                var item = line.Entry?.Definition;
                if (item == null)
                {
                    continue;
                }

                int index = i;
                _list.Add(item.IconLabel, RuntimeMaterials.Hex(item.IconColorHex), line.Entry.DisplayName,
                    $"x{line.Amount:N0} · {line.Price:N0} z each · click to remove", () =>
                    {
                        _lines.RemoveAt(index);
                        Refresh();
                    });
            }

            _footer.text = $"{_lines.Count}/{VendingRules.MaxEntries} lines · Pushcart: {(_player.Record.HasPushcart ? "yes" : "no")} · " +
                           $"{(VendingRules.MapAllowsVending(FieldContext.Map) ? "Vigrid's streets" : "not a market")}";
            UIFactory.SetButtonLabel(_open, "Open stall");
        }

        private void OnVisibilityChanged()
        {
            if (Window.IsOpen && SocialHud.State?.MyStall == null)
            {
                _hud.SetInventoryClickOverride(Add);
            }
            else
            {
                _hud.ClearInventoryClickOverride(Add);
            }

            Refresh();
        }

        private void Add(ItemStack entry)
        {
            if (entry == null || SocialHud.State?.MyStall != null)
            {
                return;
            }

            if (_player.IsWorn(entry))
            {
                ChatLog.Error("Take it off before selling it.");
                return;
            }

            if (!long.TryParse(_price.text, out long price) || price < VendingRules.MinPrice || price > VendingRules.MaxPrice)
            {
                ChatLog.Error($"Type a price per item first ({VendingRules.MinPrice:N0}-{VendingRules.MaxPrice:N0} zeny).");
                return;
            }

            if (_lines.Count >= VendingRules.MaxEntries)
            {
                ChatLog.Error($"A stall holds at most {VendingRules.MaxEntries} lines.");
                return;
            }

            int have = entry.Definition.IsStackable ? _player.Inventory.Count(entry.ItemId) : 1;
            int amount = int.TryParse(_amount.text, out int value) && value > 0 ? Mathf.Min(value, have) : have;
            _lines.RemoveAll(l => l.Entry == entry || (entry.Definition.IsStackable && l.Entry.ItemId == entry.ItemId));
            _lines.Add(new VendingRequest(entry, amount, price));
            Refresh();
        }

        private void OpenOrClose()
        {
            var social = SocialHud.Service;
            if (social == null)
            {
                return;
            }

            if (SocialHud.State?.MyStall != null)
            {
                social.VendClose();
                return;
            }

            if (!VendingRules.TryOpen(_player.Record, _player.Inventory, FieldContext.Map, _title.text, _lines, out var stall, out string error))
            {
                ChatLog.Error(error);
                return;
            }

            _lines.Clear();
            _player.SaveNow(); // the goods are in the cart hold now
            social.VendOpen(stall);
            _hud.ClearInventoryClickOverride(Add);
        }

        private void Label(string text, float x, float y)
        {
            var label = UIFactory.CreateText(Window.Content, text, 13, UITheme.TextDim, TextAnchor.MiddleLeft);
            label.rectTransform.SetRect(x, y, 80f, 24f);
        }
    }

    /// <summary>Someone else's stall: what's for sale and the price. Type an amount, double-click a line to buy.</summary>
    public sealed class StallWindow
    {
        private readonly PlayerCharacter _player;
        private readonly UIScrollList _list;
        private readonly Text _footer;
        private readonly InputField _amount;
        private VendingStall _shown;

        public StallWindow(HudController hud, PlayerCharacter player, SocialHud social)
        {
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Stall", 520f, 120f, 440f, 520f);
            Label(hud, "Amount");
            _amount = UIFactory.CreateInputField(Window.Content, "1", characterLimit: 5, fontSize: 14);
            _amount.contentType = InputField.ContentType.IntegerNumber;
            _amount.GetComponent<RectTransform>().SetRect(80f, 0f, 100f, 28f);
            var hint = UIFactory.CreateText(Window.Content, "double-click a line to buy", 13, UITheme.TextDim, TextAnchor.MiddleRight);
            hint.rectTransform.SetRect(190f, 0f, 226f, 28f);
            _list = new UIScrollList(Window.Content, 0f, 36f, 416f, 380f, 44f) { EmptyText = "Sold out." };
            _footer = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.Text, TextAnchor.MiddleLeft);
            _footer.rectTransform.SetRect(0f, 424f, 416f, 24f);
            Window.VisibilityChanged += () =>
            {
                if (!Window.IsOpen && SocialHud.State != null)
                {
                    SocialHud.State.Browsing = null;
                }
            };
            Window.Hide();
        }

        public UIWindow Window { get; }

        /// <summary>Opens on a stall the realm sent; refreshes when it changes.</summary>
        public void Sync()
        {
            var stall = SocialHud.State?.Browsing;
            if (stall == null)
            {
                _shown = null;
                Window.Hide();
                return;
            }

            _shown = stall;
            Window.Title.text = $"{stall.Owner}'s stall";
            if (!Window.IsOpen)
            {
                Window.Show();
            }

            _list.Clear();
            for (int i = 0; i < stall.Entries.Count; i++)
            {
                var entry = stall.Entries[i];
                var item = entry.Item?.Definition;
                if (item == null)
                {
                    continue;
                }

                int index = i;
                var captured = entry.Item;
                _list.Add(item.IconLabel, RuntimeMaterials.Hex(item.IconColorHex), $"<b>{stall.Title}</b> · {entry.Item.DisplayName}",
                    $"{entry.Price:N0} z each · {entry.Item.Amount:N0} left", null, () => ItemTooltips.For(captured, "double-click: buy"),
                    onDoubleClick: () => Buy(index));
            }

            _footer.text = $"Your zeny: {_player.Record.Zeny:N0}";
        }

        private void Buy(int index)
        {
            if (_shown == null)
            {
                return;
            }

            int amount = int.TryParse(_amount.text, out int value) && value > 0 ? value : 1;
            SocialHud.Service?.VendBuy(_shown.Owner, index, amount);
            _footer.text = $"Your zeny: {_player.Record.Zeny:N0}";
        }

        private void Label(HudController hud, string text)
        {
            var label = UIFactory.CreateText(Window.Content, text, 13, UITheme.TextDim, TextAnchor.MiddleLeft);
            label.rectTransform.SetRect(0f, 2f, 80f, 24f);
        }
    }
}
