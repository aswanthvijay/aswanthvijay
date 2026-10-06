using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// A vertical, mouse-wheel scrollable list of clickable rows (icon + title + detail line), rebuilt from scratch on
    /// every refresh. Used by the shop, forge, storage and card windows.
    /// </summary>
    public sealed class UIScrollList
    {
        private readonly RectTransform _content;
        private readonly ScrollRect _scroll;
        private readonly List<GameObject> _rows = new List<GameObject>();
        private readonly float _rowHeight;
        private readonly Text _empty;

        public UIScrollList(Transform parent, float x, float y, float width, float height, float rowHeight = 46f)
        {
            _rowHeight = rowHeight;
            var viewport = UIFactory.CreatePanel(parent, "ScrollList", UITheme.PanelInner, rounded: false);
            viewport.rectTransform.SetRect(x, y, width, height);
            viewport.gameObject.AddComponent<RectMask2D>();
            Root = viewport.rectTransform;

            _content = UIFactory.CreateRect("Content", viewport.transform);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = Vector2.zero;

            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.content = _content;
            _scroll.viewport = viewport.rectTransform;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 28f;
            _scroll.inertia = false;

            _empty = UIFactory.CreateText(viewport.transform, string.Empty, 13, UITheme.TextDim, TextAnchor.MiddleCenter);
            _empty.rectTransform.Stretch(10f, 10f, 10f, 10f);
        }

        public RectTransform Root { get; }

        public int Count => _rows.Count;

        /// <summary>Text shown while the list is empty.</summary>
        public string EmptyText { get; set; } = "Nothing here.";

        public void Clear()
        {
            foreach (var row in _rows)
            {
                UnityEngine.Object.Destroy(row);
            }

            _rows.Clear();
            _content.sizeDelta = Vector2.zero;
            _empty.text = EmptyText;
        }

        /// <summary>Adds one row. <paramref name="tooltip"/> is built lazily on hover.</summary>
        public void Add(string glyph, Color iconColor, string title, string detail, Action onClick, Func<string> tooltip = null,
            bool selected = false, bool dimmed = false, Action onDoubleClick = null)
        {
            float width = Root.rect.width > 1f ? Root.rect.width : Root.sizeDelta.x;
            var row = UIFactory.CreatePanel(_content, "Row", selected ? UITheme.Selected : new Color(1f, 1f, 1f, _rows.Count % 2 == 0 ? 0.03f : 0.06f));
            row.rectTransform.SetRect(2f, 2f + _rows.Count * _rowHeight, width - 4f, _rowHeight - 4f);

            float iconSize = _rowHeight - 12f;
            var icon = UIFactory.CreateIcon(row.transform, glyph, iconColor, iconSize > 34f ? 13 : 11);
            icon.rectTransform.SetRect(4f, 4f, iconSize, iconSize);

            var titleText = UIFactory.CreateText(row.transform, title, 14, dimmed ? UITheme.TextDim : UITheme.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            titleText.rectTransform.SetRect(iconSize + 12f, 3f, width - iconSize - 24f, 20f);
            var detailText = UIFactory.CreateText(row.transform, detail ?? string.Empty, 12, UITheme.TextDim, TextAnchor.UpperLeft);
            detailText.rectTransform.SetRect(iconSize + 12f, 21f, width - iconSize - 24f, _rowHeight - 24f);

            var pointer = row.gameObject.AddComponent<UIPointerHandler>();
            pointer.LeftClick = onClick;
            pointer.DoubleClick = onDoubleClick;
            if (tooltip != null)
            {
                pointer.PointerEnter = () => UITooltip.Show(tooltip());
                pointer.PointerExit = UITooltip.Hide;
            }

            _rows.Add(row.gameObject);
            _content.sizeDelta = new Vector2(0f, 4f + _rows.Count * _rowHeight);
            _empty.text = string.Empty;
        }

        public void ScrollToTop()
        {
            _scroll.verticalNormalizedPosition = 1f;
        }
    }
}
