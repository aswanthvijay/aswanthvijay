using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>RectTransform layout shortcuts. Coordinates are in reference pixels (1920x1080 canvas).</summary>
    public static class UILayout
    {
        /// <summary>Top-left anchored rect: x to the right, y downward from the parent's top-left.</summary>
        public static RectTransform SetRect(this RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>Fill the parent with insets.</summary>
        public static RectTransform Stretch(this RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Anchor to one point of the parent (e.g. bottom-center) with a pivot, offset and size.</summary>
        public static RectTransform Anchor(this RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }

    /// <summary>Horizontal fill bar with a centered label (HP, SP, EXP, cast bar).</summary>
    public sealed class UIBar
    {
        private readonly RectTransform _fill;
        private readonly Image _fillImage;

        public UIBar(RectTransform root, RectTransform fill, Image fillImage, Text label)
        {
            Root = root;
            _fill = fill;
            _fillImage = fillImage;
            Label = label;
        }

        public RectTransform Root { get; }

        public Text Label { get; }

        public void Set(float fraction, string label = null)
        {
            fraction = Mathf.Clamp01(fraction);
            _fill.gameObject.SetActive(fraction > 0.001f);
            _fill.anchorMax = new Vector2(fraction, 1f);
            if (label != null && Label != null)
            {
                Label.text = label;
            }
        }

        public void SetColor(Color color)
        {
            _fillImage.color = color;
        }
    }

    /// <summary>Builds uGUI controls from code (no prefabs needed for the prototype).</summary>
    public static class UIFactory
    {
        /// <summary>Sorting order for tooltips and drag ghosts: above every HUD window, whatever its sibling order.</summary>
        public const int OverlaySortingOrder = 100;

        /// <summary>Gives a child element its own canvas that always renders on top (no raycasts).</summary>
        public static void RenderOnTop(GameObject element, int extraOrder = 0)
        {
            var canvas = element.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = element.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = OverlaySortingOrder + extraOrder;
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image CreatePanel(Transform parent, string name, Color color, bool rounded = true, bool blocksRaycasts = true)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = blocksRaycasts;
            if (rounded)
            {
                image.sprite = UITheme.Rounded;
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        /// <summary>Window-style panel: gold border + dark fill.</summary>
        public static Image CreateFramedPanel(Transform parent, string name, Color fill)
        {
            var border = CreatePanel(parent, name, UITheme.WindowBorder);
            var inner = CreatePanel(border.transform, "Fill", fill);
            inner.rectTransform.Stretch(2f, 2f, 2f, 2f);
            return border;
        }

        public static Text CreateText(Transform parent, string content, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rect = CreateRect("Text", parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = UITheme.Font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public static void AddShadow(Graphic graphic, float distance = 1.5f)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(distance, -distance);
        }

        public static void AddOutline(Graphic graphic, Color color, float distance = 1.2f)
        {
            var outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }

        public static Button CreateButton(Transform parent, string label, UnityAction onClick, int fontSize = 18)
        {
            var image = CreatePanel(parent, "Button_" + label, Color.white);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            // Mouse-driven UI: a clicked button must not stay selected, or Enter/WASD would press it again.
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            image.gameObject.AddComponent<UIRaiseWindow>();
            var colors = button.colors;
            colors.normalColor = UITheme.Button;
            colors.highlightedColor = UITheme.ButtonHover;
            colors.selectedColor = UITheme.ButtonHover;
            colors.pressedColor = UITheme.ButtonPressed;
            colors.disabledColor = UITheme.ButtonDisabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var text = CreateText(image.transform, label, fontSize, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.Stretch(4f, 2f, 4f, 2f);

            // Single centered line: never truncate it when the font's line height is a bit taller than the button.
            text.verticalOverflow = VerticalWrapMode.Overflow;
            AddShadow(text, 1f);
            return button;
        }

        public static void SetButtonLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }
        }

        public static InputField CreateInputField(Transform parent, string placeholder, bool password = false, int characterLimit = 0, int fontSize = 18)
        {
            var rect = CreateRect("Input", parent);
            rect.gameObject.SetActive(false);
            var background = rect.gameObject.AddComponent<Image>();
            background.sprite = UITheme.Rounded;
            background.type = Image.Type.Sliced;
            background.color = UITheme.InputBg;
            AddOutline(background, new Color(UITheme.Frost.r, UITheme.Frost.g, UITheme.Frost.b, 0.35f), 1f);

            var text = CreateText(rect, string.Empty, fontSize, UITheme.Text);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.rectTransform.Stretch(10f, 4f, 10f, 4f);

            var hint = CreateText(rect, placeholder, fontSize, UITheme.TextDim, TextAnchor.MiddleLeft, FontStyle.Italic);
            hint.rectTransform.Stretch(10f, 4f, 10f, 4f);

            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = background;
            input.textComponent = text;
            input.placeholder = hint;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = characterLimit;
            input.customCaretColor = true;
            input.caretColor = UITheme.Gold;
            input.selectionColor = new Color(UITheme.Frost.r, UITheme.Frost.g, UITheme.Frost.b, 0.35f);
            if (password)
            {
                input.contentType = InputField.ContentType.Password;
                input.asteriskChar = '*';
            }

            rect.gameObject.SetActive(true);
            return input;
        }

        public static Toggle CreateToggle(Transform parent, string label, bool isOn, UnityAction<bool> onChanged = null)
        {
            var rect = CreateRect("Toggle_" + label, parent);
            rect.gameObject.SetActive(false);

            var box = CreatePanel(rect, "Box", UITheme.InputBg);
            box.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
            AddOutline(box, UITheme.Gold, 1f);
            var check = CreatePanel(box.transform, "Check", UITheme.Gold);
            check.rectTransform.Stretch(5f, 5f, 5f, 5f);
            check.raycastTarget = false;

            var text = CreateText(rect, label, 16, UITheme.TextDim);
            text.rectTransform.Stretch(30f, 0f, 0f, 0f);

            var toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.navigation = new Navigation { mode = Navigation.Mode.None };
            rect.gameObject.AddComponent<UIRaiseWindow>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = isOn;
            if (onChanged != null)
            {
                toggle.onValueChanged.AddListener(onChanged);
            }

            rect.gameObject.SetActive(true);
            return toggle;
        }

        public static UIBar CreateBar(Transform parent, Color fillColor, int fontSize = 13)
        {
            var background = CreatePanel(parent, "Bar", UITheme.BarBg, rounded: true, blocksRaycasts: false);
            var fill = CreatePanel(background.transform, "Fill", fillColor, rounded: true, blocksRaycasts: false);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(1f, 1f);
            fill.rectTransform.offsetMax = new Vector2(-1f, -1f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);

            var label = CreateText(background.transform, string.Empty, fontSize, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Stretch();
            label.verticalOverflow = VerticalWrapMode.Overflow; // thin bars (EXP) are shorter than one text line
            AddShadow(label, 1f);
            return new UIBar(background.rectTransform, fill.rectTransform, fill, label);
        }

        /// <summary>Square icon with a text glyph (stand-in for skill/item art).</summary>
        public static Image CreateIcon(Transform parent, string glyph, Color color, int fontSize = 15)
        {
            var icon = CreatePanel(parent, "Icon", color, rounded: true, blocksRaycasts: false);
            var shine = CreatePanel(icon.transform, "Shine", new Color(1f, 1f, 1f, 0.12f), rounded: true, blocksRaycasts: false);
            shine.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            shine.rectTransform.anchorMax = Vector2.one;
            shine.rectTransform.offsetMin = new Vector2(2f, 0f);
            shine.rectTransform.offsetMax = new Vector2(-2f, -2f);
            var text = CreateText(icon.transform, glyph, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.Stretch();
            AddOutline(text, new Color(0f, 0f, 0f, 0.8f), 1f);
            return icon;
        }
    }
}
