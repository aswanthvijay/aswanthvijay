using Runeheir.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>Cast bar above the hotkeys plus the "pick a target" hint while a skill cursor is up.</summary>
    public sealed class CastBarView
    {
        private readonly SkillCaster _caster;
        private readonly RectTransform _castRoot;
        private readonly UIBar _bar;
        private readonly Text _hint;

        public CastBarView(HudController hud, PlayerCharacter player)
        {
            _caster = player.GetComponent<SkillCaster>();

            var frame = UIFactory.CreateFramedPanel(hud.Canvas.transform, "CastBar", UITheme.WindowBg);
            _castRoot = frame.rectTransform;
            _castRoot.Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(360f, 30f));
            _bar = UIFactory.CreateBar(frame.transform, new Color(0.55f, 0.8f, 1f), 14);
            _bar.Root.Stretch(4f, 4f, 4f, 4f);

            // Display only: clicks must reach monsters and ground behind the bar.
            foreach (var graphic in frame.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

            _castRoot.gameObject.SetActive(false);

            _hint = UIFactory.CreateText(hud.Canvas.transform, string.Empty, 20, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _hint.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 32f));
            UIFactory.AddOutline(_hint, Color.black, 1.5f);
        }

        public void Tick()
        {
            bool casting = _caster.IsCasting;
            _castRoot.gameObject.SetActive(casting);
            if (casting)
            {
                _bar.Set(_caster.CastProgress, $"{_caster.CastingSkill.Name}  {_caster.CastRemaining:0.0}s");
            }

            var targeting = _caster.TargetingSkill;
            _hint.text = targeting != null
                ? $"{targeting.Name}: {HudIcons.TargetLabel(targeting.Target).ToLowerInvariant()} — left-click to cast, right-click / Esc to cancel"
                : string.Empty;
        }
    }
}
