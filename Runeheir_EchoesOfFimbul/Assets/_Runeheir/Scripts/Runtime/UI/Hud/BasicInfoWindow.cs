using Runeheir.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>Top-left Ragnarok "basic info" box: name, job, HP/SP, Base/Job level + EXP, weight, zeny.</summary>
    public sealed class BasicInfoWindow
    {
        private readonly PlayerCharacter _player;
        private readonly Text _name;
        private readonly Text _job;
        private readonly UIBar _hp;
        private readonly UIBar _sp;
        private readonly Text _baseLevel;
        private readonly UIBar _baseExp;
        private readonly Text _jobLevel;
        private readonly UIBar _jobExp;
        private readonly Text _footer;

        public BasicInfoWindow(HudController hud, PlayerCharacter player)
        {
            _player = player;
            var window = UIWindow.Create(hud.Canvas.transform, "Basic Info", 12f, 12f, 320f, 228f, closable: false);
            var content = window.Content;

            _name = UIFactory.CreateText(content, string.Empty, 18, UITheme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            _name.rectTransform.SetRect(0f, 0f, 300f, 22f);
            _job = UIFactory.CreateText(content, string.Empty, 14, UITheme.TextDim, TextAnchor.MiddleRight);
            _job.rectTransform.SetRect(150f, 0f, 150f, 22f);

            Label(content, "HP", 0f, 28f);
            _hp = UIFactory.CreateBar(content, UITheme.Hp);
            _hp.Root.SetRect(30f, 28f, 270f, 16f);
            Label(content, "SP", 0f, 48f);
            _sp = UIFactory.CreateBar(content, UITheme.Sp);
            _sp.Root.SetRect(30f, 48f, 270f, 16f);

            _baseLevel = Label(content, string.Empty, 0f, 72f, 110f);
            _baseExp = UIFactory.CreateBar(content, UITheme.BaseExp, 12);
            _baseExp.Root.SetRect(110f, 74f, 190f, 13f);
            _jobLevel = Label(content, string.Empty, 0f, 92f, 110f);
            _jobExp = UIFactory.CreateBar(content, UITheme.JobExp, 12);
            _jobExp.Root.SetRect(110f, 94f, 190f, 13f);

            _footer = UIFactory.CreateText(content, string.Empty, 13, UITheme.TextDim);
            _footer.rectTransform.SetRect(0f, 112f, 300f, 18f);

            string[] labels = { "Status", "Skills", "Items", "Menu" };
            System.Action[] actions = { hud.ToggleStatus, hud.ToggleSkills, hud.ToggleInventory, hud.ToggleMenu };
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                var button = UIFactory.CreateButton(content, labels[i], () => actions[index](), 14);
                button.GetComponent<RectTransform>().SetRect(i * 76f, 140f, 72f, 26f);
            }

            player.VitalsChanged += Refresh;
            player.StatsRecalculated += Refresh;
            player.Progression.ExpChanged += Refresh;
            player.Progression.JobChanged += Refresh;
            player.Inventory.Changed += Refresh;
            Refresh();
        }

        public void Dispose()
        {
            _player.VitalsChanged -= Refresh;
            _player.StatsRecalculated -= Refresh;
            _player.Progression.ExpChanged -= Refresh;
            _player.Progression.JobChanged -= Refresh;
            _player.Inventory.Changed -= Refresh;
        }

        public void Refresh()
        {
            var record = _player.Record;
            var progression = _player.Progression;
            _name.text = record.Name;
            _job.text = _player.Job.Name;

            float hpFraction = _player.Hp / (float)_player.MaxHp;
            _hp.Set(hpFraction, $"{_player.Hp:N0} / {_player.MaxHp:N0}");
            _hp.SetColor(hpFraction < 0.25f ? UITheme.HpLow : UITheme.Hp);
            _sp.Set(_player.Sp / (float)_player.MaxSp, $"{_player.Sp:N0} / {_player.MaxSp:N0}");

            _baseLevel.text = $"Base Lv. <b>{record.BaseLevel}</b>";
            _baseExp.Set(progression.BaseExpPercent / 100f, progression.IsMaxBaseLevel ? "MAX" : $"{progression.BaseExpPercent:0.0}%");
            _jobLevel.text = $"Job Lv. <b>{record.JobLevel}</b>";
            _jobExp.Set(progression.JobExpPercent / 100f, progression.IsMaxJobLevel ? "MAX" : $"{progression.JobExpPercent:0.0}%");

            int weight = _player.Inventory.TotalWeight();
            _footer.text = $"Weight {weight:N0} / {_player.Stats.WeightCapacity:N0}    Zeny {record.Zeny:N0}";
        }

        private static Text Label(Transform parent, string content, float x, float y, float width = 30f)
        {
            var text = UIFactory.CreateText(parent, content, 14, UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            text.rectTransform.SetRect(x, y, width, 18f);
            return text;
        }
    }
}
