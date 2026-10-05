using System.Collections.Generic;
using Runeheir.Hotkeys;
using Runeheir.Player;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>Alt+S skill list for the current job line. Drag an icon onto F1–F10; double-click to use.</summary>
    public sealed class SkillWindow
    {
        private const float RowHeight = 50f;

        private readonly PlayerCharacter _player;
        private readonly SkillCaster _caster;
        private readonly HotkeyController _hotkeys;
        private readonly RectTransform _list;
        private readonly Text _header;
        private readonly List<GameObject> _rows = new List<GameObject>();

        public SkillWindow(HudController hud, PlayerCharacter player)
        {
            _player = player;
            _caster = player.GetComponent<SkillCaster>();
            _hotkeys = player.GetComponent<HotkeyController>();
            Window = UIWindow.Create(hud.Canvas.transform, "Skills", 345f, 352f, 460f, 470f);

            _header = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.TextDim, TextAnchor.UpperLeft);
            _header.rectTransform.SetRect(0f, 0f, 330f, 36f);
            var jobChange = UIFactory.CreateButton(Window.Content, "Job Change", hud.ToggleJobChange, 13);
            jobChange.GetComponent<RectTransform>().SetRect(336f, 2f, 104f, 30f);
            _list = UIFactory.CreateRect("List", Window.Content);
            _list.Stretch(0f, 40f, 0f, 0f);

            player.Progression.JobChanged += Rebuild;
            player.StatsRecalculated += RefreshHeader;
            Window.VisibilityChanged += Rebuild;
            Rebuild();
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Dispose()
        {
            _player.Progression.JobChanged -= Rebuild;
            _player.StatsRecalculated -= RefreshHeader;
        }

        private void RefreshHeader()
        {
            _header.text = $"<b><color=#EBC466>{_player.Job.Name}</color></b> skills · Skill Points {_player.Record.SkillPoints} (tree: Phase 3)\n" +
                           "Drag onto F1–F10 · double-click to use";
        }

        private void Rebuild()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            RefreshHeader();
            foreach (var row in _rows)
            {
                Object.Destroy(row);
            }

            _rows.Clear();
            var skills = SkillCatalog.ForJob(_player.Record.Job);
            for (int i = 0; i < skills.Count; i++)
            {
                _rows.Add(CreateRow(skills[i], i * RowHeight));
            }
        }

        private GameObject CreateRow(SkillDefinition skill, float y)
        {
            var row = UIFactory.CreatePanel(_list, "Skill_" + skill.Id, new Color(1f, 1f, 1f, 0.03f));
            row.rectTransform.SetRect(0f, y, 440f, RowHeight - 4f);

            var icon = UIFactory.CreateIcon(row.transform, skill.IconLabel, RuntimeMaterials.Hex(skill.IconColorHex), 14);
            icon.rectTransform.SetRect(4f, 3f, 40f, 40f);
            icon.raycastTarget = true;

            var name = UIFactory.CreateText(row.transform, skill.Name, 16, UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.rectTransform.SetRect(54f, 2f, 240f, 22f);
            string cast = skill.CastTime > 0f ? $"Cast {skill.CastTime:0.0}s" : "Instant";
            var info = UIFactory.CreateText(row.transform, $"SP {skill.SpCost} · {cast} · {HudIcons.TargetLabel(skill.Target)}", 12, UITheme.TextDim);
            info.rectTransform.SetRect(54f, 23f, 380f, 18f);

            var assign = UIFactory.CreateButton(row.transform, "Bind", () => AssignToFreeSlot(skill), 13);
            assign.GetComponent<RectTransform>().SetRect(380f, 10f, 52f, 26f);

            var drag = icon.gameObject.AddComponent<UIDragSource>();
            drag.PayloadProvider = () => new DragPayload
            {
                Slot = HotkeySlot.Skill(skill.Id),
                Glyph = skill.IconLabel,
                Color = RuntimeMaterials.Hex(skill.IconColorHex),
            };

            var pointer = icon.gameObject.AddComponent<UIPointerHandler>();
            pointer.DoubleClick = () => _caster.RequestSkill(skill.Id, null, null);
            pointer.PointerEnter = () => UITooltip.Show(HudIcons.SkillTooltip(skill, _player.Stats.CastTimeMultiplier));
            pointer.PointerExit = UITooltip.Hide;
            return row.gameObject;
        }

        private void AssignToFreeSlot(SkillDefinition skill)
        {
            int index = _player.Hotkeys.AssignToFirstEmpty(_hotkeys.CurrentPage, HotkeySlot.Skill(skill.Id));
            if (index < 0)
            {
                Session.ChatLog.Error("No free slot on this hotkey page. Drag the skill onto a slot to replace it.");
            }
            else
            {
                Session.ChatLog.System($"{skill.Name} bound to {_hotkeys.KeyForSlot(index % HotkeyLayout.SlotsPerPage)}.");
            }
        }
    }
}
