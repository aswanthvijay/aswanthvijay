using System.Collections.Generic;
using System.Text;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// Brokk's Dwarven Forge (GDD §7): Refine to +20 (safe limits, Rune of Preservation, shattering), carve glyphs into
    /// a weapon's Runic Fuller (two grooves → Runewords), and pull Soul Cards out with a Rune of Extraction.
    /// Works on worn gear too: the piece comes off for the work and goes back on if it survives.
    /// </summary>
    public sealed class ForgeWindow
    {
        public enum Mode
        {
            Refine = 0,
            Etch = 1,
            Extract = 2,
        }

        private static readonly string[] ModeNames = { "Refine", "Runic Fuller", "Extract Cards" };

        private readonly HudController _hud;
        private readonly PlayerCharacter _player;
        private readonly Button[] _tabs = new Button[ModeNames.Length];
        private readonly UIScrollList _list;
        private readonly Text _detail;
        private readonly Text _result;
        private readonly RectTransform _refinePanel;
        private readonly RectTransform _etchPanel;
        private readonly RectTransform _extractPanel;
        private readonly Toggle _preservation;
        private readonly Button[] _grooveButtons = new Button[RunewordRules.Grooves];
        private readonly List<Button> _glyphButtons = new List<Button>();
        private Mode _mode;
        private ItemStack _selected;
        private bool _autoSelect;
        private int _groove;

        public ForgeWindow(HudController hud, PlayerCharacter player)
        {
            _hud = hud;
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Brokk's Dwarven Forge", 300f, 120f, 780f, 600f);

            for (int i = 0; i < ModeNames.Length; i++)
            {
                var mode = (Mode)i;
                _tabs[i] = UIFactory.CreateButton(Window.Content, ModeNames[i], () => SetMode(mode), 14);
                _tabs[i].GetComponent<RectTransform>().SetRect(i * 150f, 0f, 144f, 28f);
            }

            _list = new UIScrollList(Window.Content, 0f, 36f, 330f, 510f);
            _detail = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.Text, TextAnchor.UpperLeft);
            _detail.rectTransform.SetRect(342f, 36f, 414f, 250f);
            _result = UIFactory.CreateText(Window.Content, string.Empty, 15, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _result.rectTransform.SetRect(342f, 500f, 414f, 46f);

            // Refine controls.
            _refinePanel = UIFactory.CreateRect("RefineControls", Window.Content);
            _refinePanel.SetRect(342f, 296f, 414f, 200f);
            _preservation = UIFactory.CreateToggle(_refinePanel, "Use a Rune of Preservation (failure: -1 instead of shatter)", false, _ => RefreshDetail());
            _preservation.GetComponent<RectTransform>().SetRect(0f, 0f, 414f, 30f);
            var refine = UIFactory.CreateButton(_refinePanel, "Refine", Refine, 17);
            refine.GetComponent<RectTransform>().SetRect(107f, 44f, 200f, 44f);

            // Etch controls: pick a groove, then a glyph.
            _etchPanel = UIFactory.CreateRect("EtchControls", Window.Content);
            _etchPanel.SetRect(342f, 296f, 414f, 200f);
            for (int g = 0; g < RunewordRules.Grooves; g++)
            {
                int groove = g;
                _grooveButtons[g] = UIFactory.CreateButton(_etchPanel, $"Groove {g + 1}", () => SelectGroove(groove), 14);
                _grooveButtons[g].GetComponent<RectTransform>().SetRect(g * 140f, 0f, 132f, 28f);
            }

            for (int i = 0; i < RunewordRules.GlyphNames.Count; i++)
            {
                string glyphId = RunewordRules.GlyphNames[i].Key;
                var button = UIFactory.CreateButton(_etchPanel, RunewordRules.GlyphNames[i].Value, () => Etch(glyphId), 13);
                button.GetComponent<RectTransform>().SetRect(i % 3 * 138f, 40f + i / 3 * 40f, 132f, 34f);
                _glyphButtons.Add(button);
            }

            // Extract controls.
            _extractPanel = UIFactory.CreateRect("ExtractControls", Window.Content);
            _extractPanel.SetRect(342f, 296f, 414f, 200f);
            var extract = UIFactory.CreateButton(_extractPanel, "Extract every card", Extract, 16);
            extract.GetComponent<RectTransform>().SetRect(87f, 20f, 240f, 44f);

            player.Inventory.Changed += Refresh;
            player.Equipment.Changed += Refresh;
            Window.VisibilityChanged += Refresh;
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Dispose()
        {
            _player.Inventory.Changed -= Refresh;
            _player.Equipment.Changed -= Refresh;
        }

        public void Open(Mode mode)
        {
            _mode = mode;
            _selected = null;
            _autoSelect = true;
            _result.text = string.Empty;
            Window.Show();
            _list.ScrollToTop();
            Refresh();
        }

        private void SetMode(Mode mode)
        {
            _mode = mode;
            _result.text = string.Empty;
            if (_selected != null && !Fits(_selected))
            {
                _selected = null;
                _autoSelect = true;
            }

            Refresh();
        }

        private bool Fits(ItemStack entry)
        {
            var item = entry?.Definition;
            if (item == null || !item.IsEquipment)
            {
                return false;
            }

            switch (_mode)
            {
                case Mode.Refine: return item.Refinable;
                case Mode.Etch: return item.IsWeapon;
                default: return entry.CardCount > 0;
            }
        }

        /// <summary>Worn pieces first, then the bag.</summary>
        private List<ItemStack> Candidates()
        {
            var list = new List<ItemStack>();
            foreach (var pair in _player.Equipment.Worn())
            {
                if (Fits(pair.Value))
                {
                    list.Add(pair.Value);
                }
            }

            foreach (var entry in _player.Inventory.Stacks)
            {
                if (Fits(entry))
                {
                    list.Add(entry);
                }
            }

            return list;
        }

        private void Refresh()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i].GetComponent<Image>().color = i == (int)_mode ? UITheme.Gold : Color.white;
            }

            var candidates = Candidates();
            if (_selected != null && !candidates.Contains(_selected))
            {
                _selected = null; // shattered, emptied of cards, sold or stored: never jump to another piece by itself
            }

            if (_selected == null && _autoSelect && candidates.Count > 0)
            {
                _selected = candidates[0];
            }

            _autoSelect = false;

            _list.EmptyText = _mode == Mode.Refine ? "No refinable gear." : _mode == Mode.Etch ? "No weapons." : "No gear with cards in it.";
            _list.Clear();
            foreach (var entry in candidates)
            {
                var item = entry.Definition;
                var captured = entry;
                string worn = _player.IsWorn(entry) ? "<color=#7DCEA0>[worn]</color> " : string.Empty;
                _list.Add(item.IconLabel, RuntimeMaterials.Hex(item.IconColorHex), entry.DisplayName, worn + RowDetail(entry),
                    () => Select(captured), () => ItemTooltips.For(captured), selected: entry == _selected);
            }

            _refinePanel.gameObject.SetActive(_mode == Mode.Refine);
            _etchPanel.gameObject.SetActive(_mode == Mode.Etch);
            _extractPanel.gameObject.SetActive(_mode == Mode.Extract);
            RefreshDetail();
        }

        private string RowDetail(ItemStack entry)
        {
            var item = entry.Definition;
            switch (_mode)
            {
                case Mode.Refine:
                    return entry.Refine >= RefineRules.MaxRefine
                        ? "max refine"
                        : $"next +{entry.Refine + 1}: {RefineRules.SuccessChance(item, entry.Refine + 1):0}% · safe to +{RefineRules.SafeLimit(item)}";
                case Mode.Etch:
                    var runeword = RunewordRules.ActiveRuneword(entry);
                    return runeword != null ? $"<color=#F7DC6F>{runeword.Name}</color>" : $"{GlyphLabel(entry, 0)} · {GlyphLabel(entry, 1)}";
                default:
                    return $"{entry.CardCount} card{(entry.CardCount == 1 ? string.Empty : "s")}";
            }
        }

        private void Select(ItemStack entry)
        {
            _selected = entry;
            _result.text = string.Empty;
            Refresh();
        }

        private void SelectGroove(int groove)
        {
            _groove = groove;
            RefreshDetail();
        }

        private void RefreshDetail()
        {
            var entry = _selected;
            var item = entry?.Definition;
            var text = new StringBuilder();
            text.Append($"Zeny <color=#EBC466>{_player.Record.Zeny:N0}</color>\n\n");
            if (item == null)
            {
                text.Append(_mode == Mode.Refine
                    ? "Pick a piece on the left.\n\nRefining adds ATK to weapons and 3 DEF per + to armor. Up to the safe limit it always works; " +
                      "past it, a failure shatters the piece and its cards unless a Rune of Preservation holds it at -1."
                    : _mode == Mode.Etch
                        ? "Pick a weapon on the left."
                        : "Pick a piece with cards. A Rune of Extraction returns every card to your bag; the piece keeps its refine.");
                _detail.text = text.ToString();
                return;
            }

            text.Append($"<b><color=#EBC466>{entry.DisplayName}</color></b>\n");
            switch (_mode)
            {
                case Mode.Refine:
                    AppendRefine(text, entry, item);
                    break;
                case Mode.Etch:
                    AppendEtch(text, entry);
                    break;
                default:
                    foreach (string cardId in entry.Cards)
                    {
                        var card = ItemCatalog.Get(cardId);
                        if (card != null)
                        {
                            text.Append($"◆ {card.Name}\n");
                        }
                    }

                    text.Append($"\nRune of Extraction: you have {_player.Inventory.Count(ItemCatalog.RuneOfExtraction)} " +
                                $"(Brokk sells them for {ItemCatalog.Get(ItemCatalog.RuneOfExtraction).Price:N0} z).");
                    break;
            }

            _detail.text = text.ToString();
        }

        private void AppendRefine(StringBuilder text, ItemStack entry, ItemDefinition item)
        {
            if (entry.Refine >= RefineRules.MaxRefine)
            {
                text.Append($"Already +{RefineRules.MaxRefine}. Nothing left to hammer.");
                return;
            }

            int next = entry.Refine + 1;
            float chance = RefineRules.SuccessChance(item, next);
            var material = ItemCatalog.Get(RefineRules.MaterialFor(item));
            text.Append($"+{entry.Refine} → <b>+{next}</b>    Success <b>{(chance >= 100f ? "<color=#7DCEA0>100%</color>" : $"<color=#F5B041>{chance:0}%</color>")}</b>" +
                        $"  (safe up to +{RefineRules.SafeLimit(item)})\n");
            text.Append(item.IsWeapon
                ? $"ATK bonus: +{WeaponRules.RefineAtkBonus(item.WeaponLevel, entry.Refine)} → +{WeaponRules.RefineAtkBonus(item.WeaponLevel, next)}\n"
                : $"DEF bonus: +{entry.Refine * RefineRules.DefPerRefine} → +{next * RefineRules.DefPerRefine}\n");
            text.Append($"Cost: 1 {material.Name} (you have {_player.Inventory.Count(material.Id)}) + {RefineRules.ZenyCost(item):N0} z\n");
            if (chance < 100f)
            {
                int runes = _player.Inventory.Count(ItemCatalog.RuneOfPreservation);
                text.Append(_preservation.isOn && runes > 0
                    ? $"<color=#F5B041>On failure: drops to +{Mathf.Max(0, entry.Refine - 1)} (uses 1 of your {runes} Runes of Preservation).</color>"
                    : $"<color=#FF786B>On failure: {item.Name}{(entry.CardCount > 0 ? " and its cards" : string.Empty)} SHATTER.</color>" +
                      (runes > 0 ? string.Empty : "\nNo Rune of Preservation in your bag."));
            }
        }

        private void AppendEtch(StringBuilder text, ItemStack entry)
        {
            for (int g = 0; g < RunewordRules.Grooves; g++)
            {
                _grooveButtons[g].GetComponent<Image>().color = g == _groove ? UITheme.Gold : Color.white;
                text.Append($"Groove {g + 1}: {GlyphLabel(entry, g)}\n");
            }

            var runeword = RunewordRules.ActiveRuneword(entry);
            text.Append(runeword != null ? $"<color=#F7DC6F>Runeword {runeword.Name}: {runeword.Description}</color>\n" : "No Runeword yet.\n");
            text.Append($"\nPick a groove, then a glyph to carve ({RunewordRules.EtchZeny:N0} z each). Recipes:\n");
            foreach (var recipe in RunewordRules.Runewords)
            {
                text.Append($"<size=12>· {recipe.Name}: {ItemCatalog.Get(recipe.GlyphA)?.Name} + {ItemCatalog.Get(recipe.GlyphB)?.Name}</size>\n");
            }

            for (int i = 0; i < _glyphButtons.Count; i++)
            {
                string glyphId = RunewordRules.GlyphNames[i].Key;
                UIFactory.SetButtonLabel(_glyphButtons[i], $"{RunewordRules.GlyphNames[i].Value} ({_player.Inventory.Count(glyphId)})");
            }
        }

        private static string GlyphLabel(ItemStack entry, int groove)
        {
            var glyphs = entry.Glyphs;
            string id = glyphs != null && groove < glyphs.Length ? glyphs[groove] : null;
            return string.IsNullOrEmpty(id) ? "<color=#7F8C8D>empty</color>" : ItemCatalog.Get(id)?.Name ?? id;
        }

        // ------------------------------------------------------------ actions
        private void Refine()
        {
            var entry = _selected;
            var item = entry?.Definition;
            if (item == null)
            {
                return;
            }

            bool preserve = _preservation.isOn;
            if (!RefineRules.CheckCosts(_player.Record, _player.Inventory, entry, preserve, out string reason))
            {
                ShowResult(reason, UITheme.Error);
                return;
            }

            float chance = RefineRules.SuccessChance(item, entry.Refine + 1);
            if (chance < 100f && !(preserve && _player.Inventory.Has(ItemCatalog.RuneOfPreservation)))
            {
                _hud.Confirm($"Past the safe limit: <b>{chance:0}%</b> to reach +{entry.Refine + 1}.\n" +
                             $"On failure <b>{entry.DisplayName}</b>{(entry.CardCount > 0 ? " and its cards" : string.Empty)} shatter forever.",
                    () => DoRefine(entry, false), "Hammer it");
                return;
            }

            DoRefine(entry, preserve);
        }

        private void DoRefine(ItemStack entry, bool preserve)
        {
            string message = null;
            var outcome = _player.WorkOnPiece(entry, () =>
                RefineRules.TryRefine(_player.Record, _player.Inventory, entry, preserve, SystemRandomSource.Shared, out message));
            switch (outcome)
            {
                case RefineOutcome.Success:
                    ShowResult(message, UITheme.Success);
                    WorldFeedback.Announce(_player, $"+{entry.Refine}!", new Color(1f, 0.85f, 0.3f));
                    GroundRing.SpawnPulse(_player.Position, new Color(1f, 0.7f, 0.25f, 1f), 0.3f, 1.8f, 0.5f);
                    break;
                case RefineOutcome.Downgraded:
                    ShowResult(message, new Color(1f, 0.7f, 0.3f));
                    break;
                case RefineOutcome.Shattered:
                    ShowResult(message, UITheme.Error);
                    WorldFeedback.Announce(_player, "Shattered!", new Color(1f, 0.35f, 0.3f));
                    break;
                default:
                    ShowResult(message ?? "The forge refuses.", UITheme.Error);
                    return;
            }

            ChatLog.Notice(message);
            _player.SaveNow();
            Refresh();
        }

        private void Etch(string glyphId)
        {
            var entry = _selected;
            if (entry?.Definition == null || !entry.Definition.IsWeapon)
            {
                return;
            }

            int groove = _groove;
            var glyphs = entry.Glyphs;
            string current = glyphs != null && groove < glyphs.Length ? glyphs[groove] : null;
            if (!string.IsNullOrEmpty(current))
            {
                _hud.Confirm($"Groove {groove + 1} already holds {ItemCatalog.Get(current)?.Name}. Carve over it? The old glyph is lost.",
                    () => DoEtch(entry, groove, glyphId), "Carve");
                return;
            }

            DoEtch(entry, groove, glyphId);
        }

        private void DoEtch(ItemStack entry, int groove, string glyphId)
        {
            string message = null;
            bool ok = _player.WorkOnPiece(entry, () => RunewordRules.TryEtch(_player.Record, _player.Inventory, entry, groove, glyphId, out message));
            ShowResult(message ?? "The forge refuses.", ok ? UITheme.Success : UITheme.Error);
            if (ok)
            {
                ChatLog.Notice(message);
                if (RunewordRules.ActiveRuneword(entry) != null)
                {
                    WorldFeedback.Announce(_player, "Runeword!", new Color(0.97f, 0.86f, 0.44f));
                }

                _player.SaveNow();
            }

            Refresh();
        }

        private void Extract()
        {
            var entry = _selected;
            if (entry?.Definition == null)
            {
                return;
            }

            string message = null;
            bool ok = _player.WorkOnPiece(entry, () => CardRules.TryExtract(_player.Inventory, entry, out message));
            ShowResult(message ?? "The forge refuses.", ok ? UITheme.Success : UITheme.Error);
            if (ok)
            {
                ChatLog.Loot(message);
                _player.SaveNow();
            }

            Refresh();
        }

        private void ShowResult(string message, Color color)
        {
            _result.text = message;
            _result.color = color;
        }
    }
}
