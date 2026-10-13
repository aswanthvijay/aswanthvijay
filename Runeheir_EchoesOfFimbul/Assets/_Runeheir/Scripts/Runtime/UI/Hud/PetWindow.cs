using Runeheir.Items;
using Runeheir.Pets;
using Runeheir.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>J: the pet window (Ragnarok's Alt+J): name, intimacy, hunger, its Loyal bonus, Feed and Return to Egg.</summary>
    public sealed class PetWindow
    {
        private readonly PlayerCharacter _player;
        private readonly Text _info;
        private readonly Image _hunger;
        private readonly Image _intimacy;
        private readonly Button _takeOff;
        private float _nextRefresh;

        public PetWindow(HudController hud, PlayerCharacter player)
        {
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Pet", 40f, 420f, 300f, 282f);
            _info = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.Text, TextAnchor.UpperLeft);
            _info.rectTransform.SetRect(0f, 0f, 276f, 100f);
            _intimacy = Bar("Intimacy", 104f, new Color(0.95f, 0.55f, 0.7f));
            _hunger = Bar("Fullness", 132f, new Color(0.55f, 0.85f, 0.45f));
            var feed = UIFactory.CreateButton(Window.Content, "Feed", () => _player.FeedPet(), 15);
            feed.GetComponent<RectTransform>().SetRect(0f, 164f, 132f, 36f);
            var back = UIFactory.CreateButton(Window.Content, "Return to Egg", () => _player.ReturnPet(), 15);
            back.GetComponent<RectTransform>().SetRect(144f, 164f, 132f, 36f);
            _takeOff = UIFactory.CreateButton(Window.Content, "Take Off Accessory", () => _player.TakeOffPetArmor(), 13);
            _takeOff.GetComponent<RectTransform>().SetRect(0f, 206f, 276f, 26f);
            Window.VisibilityChanged += Refresh;
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Toggle()
        {
            if (Window.IsOpen)
            {
                Window.Hide();
            }
            else
            {
                Window.Show();
                Refresh();
            }
        }

        public void Tick()
        {
            if (Window.IsOpen && Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.5f;
                Refresh();
            }
        }

        private Image Bar(string label, float y, Color color)
        {
            var text = UIFactory.CreateText(Window.Content, label, 12, UITheme.TextDim, TextAnchor.MiddleLeft);
            text.rectTransform.SetRect(0f, y, 70f, 22f);
            var back = UIFactory.CreatePanel(Window.Content, label + "Back", new Color(0f, 0f, 0f, 0.45f));
            back.rectTransform.SetRect(72f, y + 4f, 204f, 14f);
            var fill = UIFactory.CreatePanel(back.transform, label + "Fill", color);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            return fill;
        }

        private void Refresh()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            var state = PetCatalog.Active(_player.Record);
            var pet = state != null ? PetCatalog.ForEgg(state.EggItemId) : null;
            if (state == null || pet == null)
            {
                _info.text = "<color=#9AA8BC>No pet is out.\n\nTame a monster with its taming item (use it while you fight the monster), then use the egg to hatch it.</color>";
                SetBar(_intimacy, 0f);
                SetBar(_hunger, 0f);
                _takeOff.gameObject.SetActive(false);
                return;
            }

            var tier = PetCatalog.IntimacyOf(state.Intimacy);
            string food = ItemCatalog.Get(pet.FoodItemId)?.Name ?? "its food";
            int have = _player.Inventory.Count(pet.FoodItemId);
            string accessory = ItemCatalog.Get(pet.ArmorItemId)?.Name;
            _takeOff.gameObject.SetActive(!string.IsNullOrEmpty(state.ArmorItemId));
            _info.text = $"<b><color=#EBC466>{state.Name}</color></b>   <color=#9AA8BC>{tier}</color>\n" +
                         $"Eats {food} <color=#9AA8BC>(you have {have})</color>\n" +
                         (accessory == null ? string.Empty
                             : string.IsNullOrEmpty(state.ArmorItemId) ? $"<color=#9AA8BC>Can wear: {accessory}</color>\n" : $"Wearing: {accessory}\n") +
                         (string.IsNullOrEmpty(pet.LoyalText) ? string.Empty
                             : $"<color={(tier == PetIntimacy.Loyal ? "#7DCEA0" : "#7F8C8D")}>When Loyal: {pet.LoyalText}</color>");
            SetBar(_intimacy, state.Intimacy / (float)PetCatalog.MaxIntimacy);
            SetBar(_hunger, state.Hunger / 100f);
        }

        private static void SetBar(Image fill, float fraction)
        {
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }
    }
}
