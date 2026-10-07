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
    /// Sigrun's job change (Phase 7: the whole Ragnarok roster). Lists every next step of the character's path: the six first
    /// jobs and four expanded jobs for an Initiate, the second jobs of a first job, and after rebirth only the way back to the
    /// first life's transcendent job. Choices the character can't take yet say why. Rebirth itself is the Norns' (Urðr's Well).
    /// </summary>
    public sealed class JobChangeWindow
    {
        private readonly HudController _hud;
        private readonly PlayerCharacter _player;
        private readonly Text _summary;
        private readonly UIScrollList _list;
        private bool _announcedReady;

        public JobChangeWindow(HudController hud, PlayerCharacter player)
        {
            _hud = hud;
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Job Change", 700f, 200f, 520f, 520f);
            _summary = UIFactory.CreateText(Window.Content, string.Empty, 14, UITheme.TextDim, TextAnchor.UpperLeft);
            _summary.rectTransform.SetRect(0f, 0f, 496f, 64f);
            _summary.horizontalOverflow = HorizontalWrapMode.Wrap;
            _list = new UIScrollList(Window.Content, 0f, 70f, 496f, 400f, rowHeight: 72f) { EmptyText = "This is the end of your path." };

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
            if (!_announcedReady && JobDatabase.NextJobs(_player.Record).Count > 0 && jobLevel >= job.JobChangeLevel)
            {
                _announcedReady = true;
                ChatLog.Notice($"You may now advance beyond {JobDatabase.NameFor(_player.Record)}! Talk to Sigrun in Vigrid Haven, or open Skills (S) → Job Change.");
            }

            Rebuild();
        }

        private void Rebuild()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            var record = _player.Record;
            var job = _player.Job;
            string name = JobDatabase.NameFor(record);
            var next = JobDatabase.NextJobs(record);
            _summary.text = Summary(record, job, name, next.Count);

            _list.Clear();
            foreach (var target in next)
            {
                bool eligible = JobDatabase.CanChangeJob(record, target.Id, out string reason);
                var newSkills = new List<string>();
                foreach (var skill in SkillCatalog.OwnedBy(target.Id))
                {
                    newSkills.Add(skill.Name);
                }

                string gift = target.StarterWeaponId != null ? target.StarterWeapon.Name : "bare hands";
                string detail = eligible
                    ? $"{target.Description}\nGift: {gift} · {newSkills.Count} new skills"
                    : $"{target.Description}\n<color=#FF8A80>{reason}</color>";
                var chosen = target;
                _list.Add(target.Name.Substring(0, 1), RuntimeMaterials.Hex(target.ColorHex), $"{target.Name}  <size=12><color=#9AA8BC>({target.RoName})</color></size>",
                    detail, () => Choose(chosen), () => Tooltip(chosen, newSkills), dimmed: !eligible);
            }

            _list.ScrollToTop();
        }

        private static string Summary(Characters.CharacterRecord record, JobInfo job, string name, int choices)
        {
            string header = $"<b><color=#EBC466>{name}</color></b>  ·  Job Lv {record.JobLevel} / {job.MaxJobLevel}";
            if (choices > 0)
            {
                return header + $"  ·  Job Lv {job.JobChangeLevel} to advance.\nJob level resets to 1; base level, stats and skills are kept.";
            }

            if (job.Family == JobFamily.Normal && job.Tier == 2 && !record.Reborn)
            {
                return header + $"\nThe end of your first life's path. At Base Lv {RebirthRules.NormalBaseLevelCap} and Job Lv {RebirthRules.MinJobLevel}, " +
                       $"the Norns at Urðr's Well in Vigrid Haven can weave you a new thread: rebirth, and the road to {JobDatabase.TranscendentOf(job.Id)?.Name}.";
            }

            return header + "\nThe end of your path. Keep growing: Base Lv " + RebirthRules.BaseLevelCap(record) + ", Job Lv " + job.MaxJobLevel + ".";
        }

        private static string Tooltip(JobInfo target, List<string> newSkills)
        {
            return $"<b>{target.Name}</b> (Ragnarok's {target.RoName})\n{target.Description}\n\n" +
                   $"Job levels: up to {target.MaxJobLevel}\nWeapons: {Combat.WeaponMasks.Describe(target.AllowedWeapons)}\n" +
                   (newSkills.Count > 0 ? "Skills: " + string.Join(", ", newSkills) : string.Empty);
        }

        private void Choose(JobInfo target)
        {
            if (!JobDatabase.CanChangeJob(_player.Record, target.Id, out string reason))
            {
                ChatLog.Error(reason);
                return;
            }

            _hud.Confirm($"Become {JobDatabase.WithArticle(target.Name)}? This can't be undone.", () => Advance(target), "Advance");
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
