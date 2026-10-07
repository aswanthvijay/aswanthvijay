using System.Text;
using Runeheir.Combat;
using Runeheir.Items;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// Phase 7 crafting (Ragnarok's Smithing and Pharmacy): Rune Forging opens the Runesmith's forge, Brewing the Brewmaster's
    /// kettle. Pick a recipe on the left; the right shows the materials, the chance and the buttons. Every attempt costs the
    /// skill's SP, and a failure uses up the materials.
    /// </summary>
    public sealed class CraftWindow
    {
        private const int BatchSize = 5;

        private readonly HudController _hud;
        private readonly PlayerCharacter _player;
        private readonly UIScrollList _list;
        private readonly Text _detail;
        private readonly Text _result;
        private readonly Button _once;
        private readonly Button _batch;
        private CraftKind _kind;
        private SkillDefinition _skill;
        private CraftRecipe _selected;
        private float _ignoreClicksUntil;

        public CraftWindow(HudController hud, PlayerCharacter player)
        {
            _hud = hud;
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Rune Forging", 320f, 130f, 740f, 560f);
            _list = new UIScrollList(Window.Content, 0f, 0f, 330f, 500f);
            _detail = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.Text, TextAnchor.UpperLeft);
            _detail.rectTransform.SetRect(342f, 0f, 374f, 330f);
            _detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            _once = UIFactory.CreateButton(Window.Content, "Forge", () => Craft(1), 17);
            _once.GetComponent<RectTransform>().SetRect(342f, 344f, 180f, 44f);
            _batch = UIFactory.CreateButton(Window.Content, $"Forge ×{BatchSize}", () => Craft(BatchSize), 15);
            _batch.GetComponent<RectTransform>().SetRect(534f, 344f, 180f, 44f);
            _result = UIFactory.CreateText(Window.Content, string.Empty, 15, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _result.rectTransform.SetRect(342f, 400f, 374f, 90f);
            _result.horizontalOverflow = HorizontalWrapMode.Wrap;

            SkillEffects.CraftRequested += OnCraftRequested;
            player.Inventory.Changed += Refresh;
            Window.VisibilityChanged += Refresh;
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Dispose()
        {
            SkillEffects.CraftRequested -= OnCraftRequested;
            _player.Inventory.Changed -= Refresh;
        }

        private void OnCraftRequested(SkillCast cast)
        {
            if (cast.Caster != _player || cast.Skill.Craft == CraftKind.None)
            {
                return;
            }

            if (_kind != cast.Skill.Craft)
            {
                _selected = null;
            }

            _kind = cast.Skill.Craft;
            _skill = cast.Skill;
            _result.text = string.Empty;
            Window.Title.text = _kind == CraftKind.Forge ? "Rune Forging" : "Brewing";
            string verb = _kind == CraftKind.Forge ? "Forge" : "Brew";
            _once.GetComponentInChildren<Text>().text = verb;
            _batch.GetComponentInChildren<Text>().text = $"{verb} ×{BatchSize}";
            Window.Show();
            Refresh();
        }

        private int SkillLevel => _player.SkillBook.UsableLevel(CraftingRules.SkillFor(_kind));

        private int ResearchLevel => _player.SkillBook.UsableLevel(CraftingRules.ResearchFor(_kind));

        private void Refresh()
        {
            if (!Window.IsOpen || _skill == null)
            {
                return;
            }

            int level = SkillLevel;
            _list.Clear();
            _list.EmptyText = "Nothing to make.";
            foreach (var recipe in CraftingRules.For(_kind))
            {
                var product = recipe.Product;
                if (product == null)
                {
                    continue;
                }

                bool able = CraftingRules.CanCraft(recipe, level, _player.Inventory, out _);
                var captured = recipe;
                _selected ??= recipe;
                string amount = recipe.ProductAmount > 1 ? $" ×{recipe.ProductAmount}" : string.Empty;
                string need = _kind == CraftKind.Forge ? $"Weapon Lv {recipe.SkillLevel}" : $"Brewing Lv {recipe.SkillLevel}";
                _list.Add(product.IconLabel, RuntimeMaterials.Hex(product.IconColorHex), product.Name + amount,
                    $"{need} · {Chance(recipe):0}%", () => Select(captured), () => ItemTooltips.For(Preview(product), null),
                    selected: ReferenceEquals(recipe, _selected), dimmed: !able);
            }

            RefreshDetail();
        }

        private float Chance(CraftRecipe recipe)
        {
            return CraftingRules.SuccessChance(recipe, SkillLevel, ResearchLevel, _player.Record.JobLevel, _player.Stats.Total);
        }

        private void Select(CraftRecipe recipe)
        {
            _selected = recipe;
            _result.text = string.Empty;
            Refresh();
        }

        private void RefreshDetail()
        {
            var recipe = _selected;
            var product = recipe?.Product;
            if (product == null)
            {
                _detail.text = string.Empty;
                _once.interactable = _batch.interactable = false;
                return;
            }

            var text = new StringBuilder();
            text.Append($"<b><color=#EBC466>{product.Name}</color></b>");
            if (recipe.ProductAmount > 1)
            {
                text.Append($" ×{recipe.ProductAmount}");
            }

            text.Append($"\n<color=#9AA8BC>{product.Description}</color>\n\n<b>Materials</b>");
            foreach (var material in recipe.Materials)
            {
                int have = _player.Inventory.Count(material.ItemId);
                string color = have >= material.Amount ? "#B8E994" : "#FF8A80";
                text.Append($"\n  <color={color}>{material.Definition?.Name ?? material.ItemId}  {have:N0} / {material.Amount}</color>");
            }

            int sp = _skill.SpCost.AtInt(Mathf.Max(1, SkillLevel));
            text.Append($"\n\nSuccess chance <b>{Chance(recipe):0}%</b>  ·  {sp} SP per attempt");
            text.Append(_kind == CraftKind.Forge
                ? "\n<color=#9AA8BC>DEX, LUK, Job Lv and Weaponry Research help; each weapon level is harder. A failed forging loses the materials.</color>"
                : "\n<color=#9AA8BC>Brewing and Potion Research levels, Job Lv, INT, DEX and LUK help. A spoiled brew loses the materials.</color>");
            bool able = CraftingRules.CanCraft(recipe, SkillLevel, _player.Inventory, out string reason);
            if (!able)
            {
                text.Append($"\n\n<color=#FF8A80>{reason}</color>");
            }

            _detail.text = text.ToString();
            _once.interactable = _batch.interactable = able;
        }

        private static ItemStack Preview(ItemDefinition item)
        {
            return item.IsEquipment ? ItemStack.NewInstance(item) : new ItemStack(item.Id, 1);
        }

        private void Craft(int times)
        {
            if (Time.unscaledTime < _ignoreClicksUntil || _selected == null || _skill == null)
            {
                return;
            }

            _ignoreClicksUntil = Time.unscaledTime + 0.3f;
            if (_player.IsDead)
            {
                return;
            }

            int made = 0;
            int failed = 0;
            string last = null;
            int sp = _skill.SpCost.AtInt(Mathf.Max(1, SkillLevel));
            for (int i = 0; i < times; i++)
            {
                if (!CraftingRules.CanCraft(_selected, SkillLevel, _player.Inventory, out string reason))
                {
                    last = i == 0 ? reason : last;
                    break;
                }

                if (!_player.TrySpendSp(sp))
                {
                    last = i == 0 ? "Not enough SP." : last;
                    break;
                }

                var outcome = CraftingRules.TryCraft(_selected, SkillLevel, ResearchLevel, _player.Record.JobLevel, _player.Stats.Total,
                    _player.Inventory, SystemRandomSource.Shared, out last);
                if (outcome == CraftOutcome.Success)
                {
                    made++;
                    ChatLog.Loot(last);
                }
                else if (outcome == CraftOutcome.Failure)
                {
                    failed++;
                    ChatLog.Error(last);
                }
            }

            if (made + failed > 0)
            {
                GroundRing.SpawnPulse(_player.transform.position, made > 0 ? new Color(1f, 0.8f, 0.35f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f), 0.2f, 1.4f, 0.5f);
                _player.SaveNow();
            }

            _result.text = made + failed > 1 ? $"{made} made, {failed} failed." : last ?? string.Empty;
            _result.color = made > 0 ? UITheme.Gold : new Color(1f, 0.55f, 0.5f);
            Refresh();
        }
    }
}
