using System.Collections.Generic;
using Runeheir.Items;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// Norn Courier storage, shared by every character on the account. Double-click a stored item to take it; while this
    /// window is open, double-clicking in the inventory stores. Every move saves character and storage in one write;
    /// if that write fails the move is undone, so nothing is ever duplicated or lost.
    /// </summary>
    public sealed class StorageWindow
    {
        private readonly HudController _hud;
        private readonly PlayerCharacter _player;
        private readonly UIScrollList _list;
        private readonly Text _footer;
        private readonly InputField _amount;
        private List<ItemStack> _storage;
        private bool _busy;
        private bool _opening;

        public StorageWindow(HudController hud, PlayerCharacter player)
        {
            _hud = hud;
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Norn Storage", 960f, 110f, 460f, 600f);

            var help = UIFactory.CreateText(Window.Content, "Double-click to take out. With this open, double-click items in your bag (E) to store them. " +
                                                            "Empty amount = the whole stack.", 13, UITheme.TextDim, TextAnchor.UpperLeft);
            help.rectTransform.SetRect(0f, 0f, 436f, 36f);
            var amountLabel = UIFactory.CreateText(Window.Content, "Amount", 13, UITheme.TextDim, TextAnchor.MiddleRight);
            amountLabel.rectTransform.SetRect(240f, 40f, 90f, 28f);
            _amount = UIFactory.CreateInputField(Window.Content, "all", characterLimit: 5, fontSize: 14);
            _amount.contentType = InputField.ContentType.IntegerNumber;
            _amount.GetComponent<RectTransform>().SetRect(336f, 40f, 100f, 28f);

            _list = new UIScrollList(Window.Content, 0f, 76f, 436f, 430f) { EmptyText = "Your storage is empty." };
            _footer = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.Text, TextAnchor.MiddleLeft);
            _footer.rectTransform.SetRect(0f, 512f, 436f, 24f);

            Window.VisibilityChanged += OnVisibilityChanged;
            Window.Hide();
        }

        public UIWindow Window { get; }

        public async void Open()
        {
            if (_opening)
            {
                return;
            }

            _opening = true;
            try
            {
                _storage = await GameSession.Instance.LoadStorage();
            }
            finally
            {
                _opening = false;
            }

            if (_storage == null || _player == null)
            {
                return;
            }

            Window.Show();
            _hud.ShowInventory();
            Refresh();
        }

        private void OnVisibilityChanged()
        {
            if (Window.IsOpen)
            {
                _hud.SetInventoryClickOverride(Deposit);
            }
            else
            {
                _hud.ClearInventoryClickOverride(Deposit);
            }
        }

        /// <summary>Amount from the box, or the whole stack when it's empty.</summary>
        private int AmountFor(ItemStack entry)
        {
            return int.TryParse(_amount.text, out int value) && value > 0 ? Mathf.Min(value, entry.Amount) : entry.Amount;
        }

        private void Refresh()
        {
            if (!Window.IsOpen || _storage == null)
            {
                return;
            }

            _list.Clear();
            foreach (var entry in _storage)
            {
                var item = entry.Definition;
                if (item == null)
                {
                    continue;
                }

                var captured = entry;
                _list.Add(item.IconLabel, RuntimeMaterials.Hex(item.IconColorHex), entry.DisplayName,
                    item.IsStackable ? $"x{entry.Amount:N0}" : EquipKinds.Label(item.EquipKind),
                    null, () => ItemTooltips.For(captured, "double-click: take out"), onDoubleClick: () => Withdraw(captured));
            }

            _footer.text = $"{_storage.Count} / {StorageRules.MaxEntries} entries · shared by your whole account";
        }

        private void Deposit(ItemStack entry)
        {
            Move(() =>
            {
                bool ok = StorageRules.TryDeposit(_player.Inventory, _storage, entry, AmountFor(entry), out string message);
                Report(ok, message);
                return ok;
            });
        }

        private void Withdraw(ItemStack entry)
        {
            Move(() =>
            {
                bool ok = StorageRules.TryWithdraw(_storage, _player.Inventory, entry, AmountFor(entry), _player.Stats.WeightCapacity,
                    _player.CurrentWeight, out string message);
                Report(ok, message);
                return ok;
            });
        }

        private static void Report(bool ok, string message)
        {
            if (ok)
            {
                ChatLog.Loot(message);
            }
            else
            {
                ChatLog.Error(message);
            }
        }

        /// <summary>Does one move, saves bag + storage together, and rolls the move back if the save fails.</summary>
        private async void Move(System.Func<bool> move)
        {
            if (_busy || _storage == null || _player.IsDead)
            {
                return;
            }

            var bagBefore = Snapshot(_player.Record.Inventory);
            var storageBefore = Snapshot(_storage);
            if (!move())
            {
                return;
            }

            _busy = true;
            Refresh();
            try
            {
                _player.WriteBackToRecord();
                var result = await GameSession.Instance.SaveActiveCharacterAndStorage(_storage);
                if (!result.Success)
                {
                    Restore(_player.Record.Inventory, bagBefore);
                    Restore(_storage, storageBefore);
                    _player.Inventory.NotifyChanged();
                    ChatLog.Error("That move was undone because it could not be saved.");
                }
            }
            finally
            {
                _busy = false;
                Refresh();
            }
        }

        private static List<ItemStack> Snapshot(List<ItemStack> list)
        {
            var copy = new List<ItemStack>(list.Count);
            foreach (var stack in list)
            {
                copy.Add(stack.Clone());
            }

            return copy;
        }

        private static void Restore(List<ItemStack> target, List<ItemStack> snapshot)
        {
            target.Clear();
            target.AddRange(snapshot);
        }
    }
}
