using Runeheir.World;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>The map's name in large letters when it loads, fading in and out ("Whispering Woods · Lv 61–120").</summary>
    public sealed class MapBanner
    {
        private const float FadeIn = 0.6f;
        private const float Hold = 3f;
        private const float FadeOut = 1.2f;

        private readonly CanvasGroup _group;
        private readonly Text _title;
        private readonly Text _subtitle;
        private float _shownAt = -100f;

        public MapBanner(HudController hud)
        {
            var root = UIFactory.CreateRect("MapBanner", hud.Canvas.transform);
            root.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 110f));
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _title = UIFactory.CreateText(root, string.Empty, 42, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _title.rectTransform.SetRect(0f, 0f, 900f, 60f);
            UIFactory.AddOutline(_title, new Color(0f, 0f, 0f, 0.9f), 2f);
            _subtitle = UIFactory.CreateText(root, string.Empty, 18, UITheme.Frost, TextAnchor.MiddleCenter, FontStyle.Italic);
            _subtitle.rectTransform.SetRect(0f, 62f, 900f, 30f);
            UIFactory.AddOutline(_subtitle, new Color(0f, 0f, 0f, 0.9f), 1.5f);
        }

        public void Show(MapDefinition map)
        {
            if (map == null)
            {
                return;
            }

            _title.text = map.Name;
            _subtitle.text = $"{KindLabel(map)} · {map.LevelLabel}";
            _shownAt = Time.unscaledTime;
        }

        public void Tick()
        {
            float t = Time.unscaledTime - _shownAt;
            float alpha = t < FadeIn ? t / FadeIn : t < FadeIn + Hold ? 1f : 1f - (t - FadeIn - Hold) / FadeOut;
            _group.alpha = Mathf.Clamp01(alpha);
        }

        private static string KindLabel(MapDefinition map)
        {
            switch (map.Kind)
            {
                case MapKind.Town: return "Capital of Midgard";
                case MapKind.Arena: return "Arena";
                case MapKind.Dungeon: return "Dungeon";
                case MapKind.Lair: return "MVP Lair";
                default: return "Field";
            }
        }
    }
}
