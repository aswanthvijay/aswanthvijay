using System.Collections.Generic;
using Runeheir.Combat;
using Runeheir.Hotkeys;
using Runeheir.Jobs;
using Runeheir.Player;
using Runeheir.Session;
using Runeheir.Skills;
using Runeheir.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>
    /// Alt+S skill tree: one tab per job in your line (Initiate → first job → second job → Ascended). Spend skill
    /// points with "+", drag learned skills onto F1–F10, double-click to use. Locked skills show what they need.
    /// </summary>
    public sealed class SkillWindow
    {
        private const float RowHeight = 54f;
        private const float ListWidth = 500f;

        private readonly PlayerCharacter _player;
        private readonly SkillCaster _caster;
        private readonly HotkeyController _hotkeys;
        private readonly RectTransform _tabBar;
        private readonly RectTransform _list;
        private readonly Text _header;
        private readonly List<GameObject> _rows = new List<GameObject>();
        private readonly List<GameObject> _tabs = new List<GameObject>();
        private JobId _page;

        public SkillWindow(HudController hud, PlayerCharacter player)
        {
            _player = player;
            _caster = player.GetComponent<SkillCaster>();
            _hotkeys = player.GetComponent<HotkeyController>();
            Window = UIWindow.Create(hud.Canvas.transform, "Skill Tree", 330f, 300f, 520f, 520f);

            _header = UIFactory.CreateText(Window.Content, string.Empty, 13, UITheme.TextDim, TextAnchor.UpperLeft);
            _header.rectTransform.SetRect(0f, 0f, 380f, 38f);
            var jobChange = UIFactory.CreateButton(Window.Content, "Job Change", hud.ToggleJobChange, 13);
            jobChange.GetComponent<RectTransform>().SetRect(396f, 2f, 104f, 30f);

            _tabBar = UIFactory.CreateRect("Tabs", Window.Content);
            _tabBar.SetRect(0f, 42f, ListWidth, 28f);
            _list = UIFactory.CreateRect("List", Window.Content);
            _list.Stretch(0f, 76f, 0f, 0f);

            _page = player.Record.Job;
            player.Progression.JobChanged += OnJobChanged;
            player.Progression.JobLevelUp += OnJobLevelUp;
            player.Progression.StatsChanged += Rebuild;
            player.SkillBook.Changed += Rebuild;
            Window.VisibilityChanged += Rebuild;
            Rebuild();
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Dispose()
        {
            _player.Progression.JobChanged -= OnJobChanged;
            _player.Progression.JobLevelUp -= OnJobLevelUp;
            _player.Progression.StatsChanged -= Rebuild;
            _player.SkillBook.Changed -= Rebuild;
        }

        private void OnJobChanged()
        {
            _page = _player.Record.Job;
            Rebuild();
        }

        private void OnJobLevelUp(int level)
        {
            Rebuild();
        }

        private void Rebuild()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            if (!JobDatabase.IsSelfOrAncestor(_page, _player.Record.Job))
            {
                _page = _player.Record.Job;
            }

            _header.text = $"<b><color=#EBC466>{_player.Job.Name}</color></b> · Skill Points <b><color=#EBC466>{_player.Record.SkillPoints}</color></b> (one per Job Level)\n" +
                           "Click + to learn · drag an icon onto F1–F10 · double-click to use";
            BuildTabs();

            foreach (var row in _rows)
            {
                Object.Destroy(row);
            }

            _rows.Clear();
            var skills = SkillCatalog.OwnedBy(_page);
            for (int i = 0; i < skills.Count; i++)
            {
                _rows.Add(CreateRow(skills[i], i * RowHeight));
            }
        }

        /// <summary>Tabs for every job of the line, Initiate first.</summary>
        private void BuildTabs()
        {
            foreach (var tab in _tabs)
            {
                Object.Destroy(tab);
            }

            _tabs.Clear();
            var line = new List<JobInfo>();
            for (var job = JobDatabase.Get(_player.Record.Job); ; job = JobDatabase.Get(job.Parent))
            {
                line.Insert(0, job);
                if (!job.HasParent)
                {
                    break;
                }
            }

            float width = ListWidth / Mathf.Max(1, line.Count);
            for (int i = 0; i < line.Count; i++)
            {
                var job = line[i];
                var button = UIFactory.CreateButton(_tabBar, job.Name, () =>
                {
                    _page = job.Id;
                    Rebuild();
                }, 13);
                button.GetComponent<RectTransform>().SetRect(i * width, 0f, width - 4f, 26f);
                if (job.Id == _page)
                {
                    var colors = button.colors;
                    colors.normalColor = UITheme.Selected + new Color(0.1f, 0.08f, 0f, 0.5f);
                    colors.highlightedColor = colors.normalColor;
                    button.colors = colors;
                }

                _tabs.Add(button.gameObject);
            }
        }

        private GameObject CreateRow(SkillDefinition skill, float y)
        {
            var book = _player.SkillBook;
            int level = book.GetLevel(skill.Id);
            bool canLearn = book.CanLearn(skill.Id, out string reason);
            bool lockedByRequirement = level == 0 && !canLearn && _player.Record.SkillPoints > 0 && reason != null && reason.StartsWith("Requires");
            bool usable = level > 0 && !skill.Passive;

            var row = UIFactory.CreatePanel(_list, "Skill_" + skill.Id, new Color(1f, 1f, 1f, level > 0 ? 0.05f : 0.02f));
            row.rectTransform.SetRect(0f, y, ListWidth, RowHeight - 4f);

            Color tint = RuntimeMaterials.Hex(skill.IconColorHex);
            if (level == 0)
            {
                tint = Color.Lerp(tint, new Color(0.25f, 0.27f, 0.3f), 0.6f);
            }

            var icon = UIFactory.CreateIcon(row.transform, skill.IconLabel, tint, 13);
            icon.rectTransform.SetRect(4f, 4f, 42f, 42f);
            icon.raycastTarget = true;

            string passive = skill.Passive ? "  <size=11><color=#9AA8BC>passive</color></size>" : string.Empty;
            var name = UIFactory.CreateText(row.transform, skill.Name + passive, 16, level > 0 ? UITheme.Text : UITheme.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.rectTransform.SetRect(56f, 3f, 300f, 22f);

            var pips = UIFactory.CreateText(row.transform, LevelPips(level, skill.MaxLevel), 13, UITheme.Gold, TextAnchor.MiddleRight);
            pips.rectTransform.SetRect(300f, 3f, 92f, 22f);

            string detail = lockedByRequirement
                ? $"<color=#E6735C>{reason}</color>"
                : HudIcons.LevelSummary(skill, Mathf.Max(1, level), _player.Stats.CastTimeMultiplier);
            var info = UIFactory.CreateText(row.transform, detail, 12, UITheme.TextDim);
            info.rectTransform.SetRect(56f, 26f, 336f, 20f);
            info.horizontalOverflow = HorizontalWrapMode.Wrap;
            info.verticalOverflow = VerticalWrapMode.Truncate;

            var learn = UIFactory.CreateButton(row.transform, "+", () => Learn(skill), 18);
            learn.GetComponent<RectTransform>().SetRect(398f, 9f, 32f, 32f);
            learn.interactable = canLearn;

            if (usable)
            {
                var bind = UIFactory.CreateButton(row.transform, "Bind", () => AssignToFreeSlot(skill), 13);
                bind.GetComponent<RectTransform>().SetRect(436f, 11f, 58f, 28f);

                var drag = icon.gameObject.AddComponent<UIDragSource>();
                drag.PayloadProvider = () => new DragPayload
                {
                    Slot = HotkeySlot.Skill(skill.Id),
                    Glyph = skill.IconLabel,
                    Color = RuntimeMaterials.Hex(skill.IconColorHex),
                };
            }

            var pointer = icon.gameObject.AddComponent<UIPointerHandler>();
            if (usable)
            {
                pointer.DoubleClick = () => _caster.RequestSkill(skill.Id, null, null);
            }

            pointer.PointerEnter = () => UITooltip.Show(HudIcons.SkillTooltip(
                skill, level, _player.Stats.CastTimeMultiplier, showNext: true, requirement: canLearn || level >= skill.MaxLevel ? null : reason));
            pointer.PointerExit = UITooltip.Hide;
            return row.gameObject;
        }

        private static string LevelPips(int level, int max)
        {
            return max == 1 ? (level > 0 ? "learned" : "Lv 0/1") : $"Lv {level}/{max}";
        }

        private void Learn(SkillDefinition skill)
        {
            if (!_player.SkillBook.TryLearn(skill.Id, out string reason))
            {
                ChatLog.Error(reason);
                return;
            }

            int level = _player.SkillBook.GetLevel(skill.Id);
            ChatLog.System(level == 1 ? $"You learned {skill.Name}." : $"{skill.Name} is now Lv {level}.");
            if (level == 1 && !skill.Passive && _player.Hotkeys.AssignToFirstEmpty(_hotkeys.CurrentPage, HotkeySlot.Skill(skill.Id)) is int slot && slot >= 0)
            {
                ChatLog.System($"{skill.Name} bound to {_hotkeys.KeyForSlot(slot % HotkeyLayout.SlotsPerPage)}.");
            }

            _player.SaveNow();
        }

        private void AssignToFreeSlot(SkillDefinition skill)
        {
            int index = _player.Hotkeys.AssignToFirstEmpty(_hotkeys.CurrentPage, HotkeySlot.Skill(skill.Id));
            if (index < 0)
            {
                ChatLog.Error("No free slot on this hotkey page. Drag the skill onto a slot to replace it.");
            }
            else
            {
                ChatLog.System($"{skill.Name} bound to {_hotkeys.KeyForSlot(index % HotkeyLayout.SlotsPerPage)}.");
            }
        }
    }
}
