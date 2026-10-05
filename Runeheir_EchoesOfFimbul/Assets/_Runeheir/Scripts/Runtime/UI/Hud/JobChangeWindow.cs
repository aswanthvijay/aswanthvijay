using System.Collections.Generic;
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
    /// Advance along the GDD job tree (Initiate → Warrior/Scout/Mystic/Devotee → ... → Ascended).
    /// Stand-in for the job-change NPC quests; enforces the same job-level requirements.
    /// </summary>
    public sealed class JobChangeWindow
    {
        private readonly PlayerCharacter _player;
        private readonly Text _summary;
        private readonly RectTransform _list;
        private readonly List<GameObject> _entries = new List<GameObject>();
        private bool _announcedReady;

        public JobChangeWindow(HudController hud, PlayerCharacter player)
        {
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Job Change", 735f, 250f, 450f, 380f);
            _summary = UIFactory.CreateText(Window.Content, string.Empty, 14, UITheme.TextDim, TextAnchor.UpperLeft);
            _summary.rectTransform.SetRect(0f, 0f, 430f, 44f);
            _list = UIFactory.CreateRect("Choices", Window.Content);
            _list.Stretch(0f, 50f, 0f, 0f);

            player.Progression.JobLevelUp += OnJobLevelUp;
            player.Progression.JobChanged += Rebuild;
            Window.VisibilityChanged += Rebuild;
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Dispose()
        {
            _player.Progression.JobLevelUp -= OnJobLevelUp;
            _player.Progression.JobChanged -= Rebuild;
        }

        private void OnJobLevelUp(int jobLevel)
        {
            var job = _player.Job;
            if (!_announcedReady && JobDatabase.ChildrenOf(job.Id).Count > 0 && jobLevel >= JobDatabase.JobChangeLevelByTier[job.Tier])
            {
                _announcedReady = true;
                ChatLog.Notice($"You may now advance beyond {job.Name}! Open Skills (S) → Job Change.");
            }

            Rebuild();
        }

        private void Rebuild()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            foreach (var entry in _entries)
            {
                Object.Destroy(entry);
            }

            _entries.Clear();
            var job = _player.Job;
            var children = JobDatabase.ChildrenOf(job.Id);
            int required = JobDatabase.JobChangeLevelByTier[job.Tier];
            _summary.text = children.Count == 0
                ? $"<b><color=#EBC466>{job.Name}</color></b> is an Ascended class — the end of the path."
                : $"<b><color=#EBC466>{job.Name}</color></b>  ·  Job Lv {_player.Record.JobLevel} / {required} required to advance.\nJob level resets to 1; base level and stats are kept.";

            for (int i = 0; i < children.Count; i++)
            {
                _entries.Add(CreateChoice(children[i], i * 74f));
            }
        }

        private GameObject CreateChoice(JobInfo target, float y)
        {
            bool eligible = JobDatabase.CanChangeJob(_player.Record.Job, _player.Record.JobLevel, target.Id, out string reason);
            var button = UIFactory.CreateButton(_list, string.Empty, () => Advance(target), 16);
            button.GetComponent<RectTransform>().SetRect(0f, y, 430f, 66f);
            button.interactable = eligible;

            var swatch = UIFactory.CreatePanel(button.transform, "Color", RuntimeMaterials.Hex(target.ColorHex), rounded: true, blocksRaycasts: false);
            swatch.rectTransform.SetRect(10f, 13f, 40f, 40f);
            var name = UIFactory.CreateText(button.transform, target.Name, 18, eligible ? UITheme.Gold : UITheme.TextDim, TextAnchor.UpperLeft, FontStyle.Bold);
            name.rectTransform.SetRect(62f, 8f, 360f, 24f);

            var newSkills = new List<string>();
            foreach (var skill in SkillCatalog.All)
            {
                if (skill.Job == target.Id)
                {
                    newSkills.Add(skill.Name);
                }
            }

            string detail = eligible
                ? $"{target.StarterWeapon.Name} · new skills: {(newSkills.Count > 0 ? string.Join(", ", newSkills) : "—")}"
                : reason;
            // Two lines at 12 pt: third-class skill lists ("Vortex Cleave, Two-Hand Surge, Rage of Thor") wrap.
            var info = UIFactory.CreateText(button.transform, detail, 12, eligible ? UITheme.Text : UITheme.Error, TextAnchor.UpperLeft);
            info.rectTransform.SetRect(62f, 31f, 360f, 32f);
            return button.gameObject;
        }

        private void Advance(JobInfo target)
        {
            if (_player.Progression.TryChangeJob(target.Id, out string reason))
            {
                _announcedReady = false;
                Window.Hide();
                _player.SaveNow();
            }
            else
            {
                ChatLog.Error(reason);
            }
        }
    }
}
