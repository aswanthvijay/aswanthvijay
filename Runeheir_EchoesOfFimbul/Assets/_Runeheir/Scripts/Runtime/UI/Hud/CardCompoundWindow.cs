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
    /// Double-click a Soul Card: pick the piece to compound it into (GDD §6, 4-slot compounding). Only gear of the card's
    /// family with a free socket is listed, worn pieces included. Permanent unless pulled out with a Rune of Extraction.
    /// </summary>
    public sealed class CardCompoundWindow
    {
        private readonly HudController _hud;
        private readonly PlayerCharacter _player;
        private readonly Text _header;
        private readonly UIScrollList _list;
        private string _cardId;

        public CardCompoundWindow(HudController hud, PlayerCharacter player)
        {
            _hud = hud;
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Compound Card", 760f, 200f, 440f, 480f);
            _header = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.Text, TextAnchor.UpperLeft);
            _header.rectTransform.SetRect(0f, 0f, 416f, 70f);
            _list = new UIScrollList(Window.Content, 0f, 76f, 416f, 340f);
            player.Inventory.Changed += Refresh;
            player.Equipment.Changed += Refresh;
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Dispose()
        {
            _player.Inventory.Changed -= Refresh;
            _player.Equipment.Changed -= Refresh;
        }

        public void Open(string cardId)
        {
            _cardId = cardId;
            Window.Show();
            Refresh();
        }

        private void Refresh()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            var card = ItemCatalog.Get(_cardId);
            if (card == null || !_player.Inventory.Has(card.Id))
            {
                Window.Hide();
                return;
            }

            _header.text = $"<b><color=#EBC466>{card.Name}</color></b> x{_player.Inventory.Count(card.Id)}\n{card.Description}\n" +
                           $"<color=#9AA8BC>Pick {EquipKinds.Label(card.CardTarget)} gear with a free socket.</color>";
            _list.EmptyText = $"No {EquipKinds.Label(card.CardTarget).ToLowerInvariant()} gear with a free socket.";
            _list.Clear();
            foreach (var entry in Candidates(card))
            {
                var item = entry.Definition;
                var captured = entry;
                string worn = _player.IsWorn(entry) ? "<color=#7DCEA0>[worn]</color> " : string.Empty;
                _list.Add(item.IconLabel, RuntimeMaterials.Hex(item.IconColorHex), entry.DisplayName, $"{worn}{entry.FreeSockets} free socket(s)",
                    () => Compound(captured), () => ItemTooltips.For(captured));
            }
        }

        private IEnumerable<ItemStack> Candidates(ItemDefinition card)
        {
            foreach (var pair in _player.Equipment.Worn())
            {
                if (Fits(card, pair.Value))
                {
                    yield return pair.Value;
                }
            }

            foreach (var entry in _player.Inventory.Stacks)
            {
                if (Fits(card, entry))
                {
                    yield return entry;
                }
            }
        }

        private static bool Fits(ItemDefinition card, ItemStack entry)
        {
            var item = entry.Definition;
            return item != null && item.IsEquipment && item.EquipKind == card.CardTarget && entry.FreeSockets > 0;
        }

        private void Compound(ItemStack target)
        {
            var card = ItemCatalog.Get(_cardId);
            _hud.Confirm($"Compound <b>{card.Name}</b> into <b>{target.DisplayName}</b>?\nOnly a Rune of Extraction can take it back out.", () =>
            {
                string message = null;
                bool ok = _player.WorkOnPiece(target, () => CardRules.TryCompound(_player.Inventory, card.Id, target, out message));
                if (ok)
                {
                    ChatLog.Notice(message);
                    WorldFeedback.Announce(_player, "Compounded!", new Color(0.8f, 0.6f, 1f));
                    _player.SaveNow();
                }
                else
                {
                    ChatLog.Error(message ?? "It won't fit.");
                }
            }, "Compound");
        }
    }
}
