using Runeheir.Player;
using Runeheir.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace Runeheir.UI
{
    /// <summary>Alt+A status window: spend status points on STR..LUK and read every derived stat (incl. ASPD).</summary>
    public sealed class StatusWindow
    {
        private readonly PlayerCharacter _player;
        private readonly Text[] _values = new Text[StatTypes.Count];
        private readonly Text[] _costs = new Text[StatTypes.Count];
        private readonly Button[] _raise = new Button[StatTypes.Count];
        private readonly Text _points;
        private readonly Text _derivedLeft;
        private readonly Text _derivedRight;

        public StatusWindow(HudController hud, PlayerCharacter player)
        {
            _player = player;
            Window = UIWindow.Create(hud.Canvas.transform, "Status", 345f, 12f, 600f, 330f);
            var content = Window.Content;

            for (int i = 0; i < StatTypes.Count; i++)
            {
                var stat = StatTypes.All[i];
                float y = i * 34f;
                var label = UIFactory.CreateText(content, StatTypes.Label(stat), 16, UITheme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                label.rectTransform.SetRect(0f, y, 46f, 28f);

                _values[i] = UIFactory.CreateText(content, string.Empty, 16, UITheme.Text);
                _values[i].rectTransform.SetRect(48f, y, 110f, 28f);

                _raise[i] = UIFactory.CreateButton(content, "+", () => Raise(stat), 18);
                _raise[i].GetComponent<RectTransform>().SetRect(160f, y + 2f, 28f, 24f);

                _costs[i] = UIFactory.CreateText(content, string.Empty, 13, UITheme.TextDim);
                _costs[i].rectTransform.SetRect(194f, y, 60f, 28f);
            }

            _points = UIFactory.CreateText(content, string.Empty, 15, UITheme.Frost, TextAnchor.MiddleLeft, FontStyle.Bold);
            _points.rectTransform.SetRect(0f, 210f, 250f, 24f);

            var hint = UIFactory.CreateText(content, "Cost to raise shown on the right. 150 DEX = instant cast.\nASPD 150–197 → attack anim 1.0x–3.0x.", 12, UITheme.TextDim, TextAnchor.UpperLeft);
            hint.rectTransform.SetRect(0f, 238f, 260f, 46f); // three lines at 12 pt

            var divider = UIFactory.CreatePanel(content, "Divider", new Color(1f, 1f, 1f, 0.08f), rounded: false, blocksRaycasts: false);
            divider.rectTransform.SetRect(262f, 0f, 2f, 280f);

            _derivedLeft = UIFactory.CreateText(content, string.Empty, 15, UITheme.Text, TextAnchor.UpperLeft);
            _derivedLeft.rectTransform.SetRect(276f, 0f, 110f, 284f);
            _derivedLeft.lineSpacing = 1.15f;
            _derivedRight = UIFactory.CreateText(content, string.Empty, 15, UITheme.Text, TextAnchor.UpperLeft);
            _derivedRight.rectTransform.SetRect(380f, 0f, 200f, 284f);
            _derivedRight.lineSpacing = 1.15f;

            player.StatsRecalculated += Refresh;
            Window.VisibilityChanged += Refresh;
            Refresh();
            Window.Hide();
        }

        public UIWindow Window { get; }

        public void Dispose()
        {
            _player.StatsRecalculated -= Refresh;
        }

        public void Refresh()
        {
            if (!Window.IsOpen)
            {
                return;
            }

            var record = _player.Record;
            var stats = _player.Stats;
            var progression = _player.Progression;
            for (int i = 0; i < StatTypes.Count; i++)
            {
                var stat = StatTypes.All[i];
                int bonus = stats.Bonus[stat];
                _values[i].text = bonus != 0
                    ? $"{record.Stats[stat]} <color=#7FD8FF>{(bonus > 0 ? "+" : string.Empty)}{bonus}</color>"
                    : record.Stats[stat].ToString();
                bool maxed = record.Stats[stat] >= StatFormulas.MaxStat;
                _costs[i].text = maxed ? "MAX" : progression.GetRaiseCost(stat).ToString();
                _raise[i].interactable = progression.CanRaiseStat(stat);
            }

            _points.text = $"Status Points: {record.StatPoints}";
            _derivedLeft.text = "ATK\nMATK\nHIT\nCRIT\nDEF\nMDEF\nFLEE\nASPD\n\nCast time\nMove speed\nWeapon";
            _derivedRight.text =
                $"{stats.StatusAtk} + {stats.WeaponAtk}\n" +
                $"{stats.MatkMin} ~ {stats.MatkMax}\n" +
                $"{stats.Hit}\n" +
                $"{stats.Crit:0.0}%\n" +
                $"{stats.Def} + {stats.SoftDef}\n" +
                $"{stats.Mdef} + {stats.SoftMdef}\n" +
                $"{stats.Flee}\n" +
                $"<b>{stats.Aspd:0.0}</b>\n" +
                $"<size=12><color=#9AA8BC>anim x{stats.AttackPlayRate:0.00} · {stats.AttacksPerSecond:0.00} hits/s</color></size>\n" +
                $"{stats.CastTimeMultiplier * 100f:0}%{(stats.CastTimeMultiplier <= 0f ? " <color=#7FD8FF>(instant)</color>" : string.Empty)}\n" +
                $"{stats.MoveSpeedMultiplier * 100f:0}%\n" +
                $"<size=13>{_player.Weapon.Name}</size>";
        }

        private void Raise(StatType stat)
        {
            _player.Progression.TryRaiseStat(stat);
        }
    }
}
