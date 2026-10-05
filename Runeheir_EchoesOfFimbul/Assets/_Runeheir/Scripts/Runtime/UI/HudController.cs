using Runeheir.Controls;
using Runeheir.Field;
using Runeheir.Player;
using Runeheir.Session;
using UnityEngine;

namespace Runeheir.UI
{
    /// <summary>
    /// Builds and drives the in-game HUD. Keys (when not typing in chat):
    /// A or Alt+A status · S / Alt+S skills · E / Alt+E items · Enter chat · Esc close / menu.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        private PlayerCharacter _player;
        private SkillCaster _caster;
        private FieldBootstrap _field;

        private BasicInfoWindow _basicInfo;
        private HotkeyBarView _hotkeyBar;
        private StatusWindow _status;
        private SkillWindow _skills;
        private InventoryWindow _inventory;
        private JobChangeWindow _jobChange;
        private ChatWindow _chat;
        private CastBarView _castBar;
        private BuffTray _buffs;
        private UIWindow _menu;
        private UIWindow _deathDialog;
        private bool _chatFocusedLastFrame;

        public Canvas Canvas { get; private set; }

        public static HudController Create(PlayerCharacter player, FieldBootstrap field)
        {
            var canvas = UIFactory.CreateCanvas("HUD", 10);
            var hud = canvas.gameObject.AddComponent<HudController>();
            hud.Canvas = canvas;
            hud._player = player;
            hud._caster = player.GetComponent<SkillCaster>();
            hud._field = field;
            hud.Build();
            return hud;
        }

        public void ToggleStatus()
        {
            _status.Window.Toggle();
        }

        public void ToggleSkills()
        {
            _skills.Window.Toggle();
        }

        public void ToggleInventory()
        {
            _inventory.Window.Toggle();
        }

        public void ToggleMenu()
        {
            _menu.Toggle();
        }

        public void ToggleJobChange()
        {
            _jobChange.Window.Toggle();
        }

        private void Build()
        {
            _chat = new ChatWindow(this, _player);
            _basicInfo = new BasicInfoWindow(this, _player);
            _hotkeyBar = new HotkeyBarView(this, _player);
            _castBar = new CastBarView(this, _player);
            _buffs = new BuffTray(this, _player);
            _status = new StatusWindow(this, _player);
            _skills = new SkillWindow(this, _player);
            _inventory = new InventoryWindow(this, _player);
            _jobChange = new JobChangeWindow(this, _player);
            _menu = BuildMenu();
            _deathDialog = BuildDeathDialog();
            UITooltip.Create(Canvas);
        }

        private UIWindow BuildMenu()
        {
            var menu = UIWindow.Create(Canvas.transform, "Menu", 810f, 380f, 300f, 230f);
            string[] labels = { "Character Select", "Exit Game", "Return to Game" };
            UnityEngine.Events.UnityAction[] actions =
            {
                () => _field.ReturnToCharacterSelect(),
                GameSession.QuitGame,
                () => menu.Hide(),
            };
            for (int i = 0; i < labels.Length; i++)
            {
                var button = UIFactory.CreateButton(menu.Content, labels[i], actions[i], 17);
                button.GetComponent<RectTransform>().SetRect(10f, 10f + i * 52f, 260f, 42f);
            }

            menu.Hide();
            return menu;
        }

        private UIWindow BuildDeathDialog()
        {
            var dialog = UIWindow.Create(Canvas.transform, "Fallen", 760f, 420f, 400f, 170f, closable: false);
            var text = UIFactory.CreateText(dialog.Content, "You have fallen in the Fimbulwinter...", 18, UITheme.Error, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.SetRect(0f, 4f, 380f, 40f);
            var button = UIFactory.CreateButton(dialog.Content, "Return to Save Point", () => _player.RespawnAtSavePoint(), 17);
            button.GetComponent<RectTransform>().SetRect(70f, 60f, 240f, 44f);
            dialog.Hide();
            return dialog;
        }

        private void Update()
        {
            if (_player == null)
            {
                return;
            }

            _hotkeyBar.Tick();
            _castBar.Tick();
            _buffs.Tick();

            if (_player.IsDead != _deathDialog.IsOpen)
            {
                if (_player.IsDead)
                {
                    _deathDialog.Show();
                }
                else
                {
                    _deathDialog.Hide();
                }
            }

            HandleKeys();
            _chatFocusedLastFrame = _chat.IsFocused;
        }

        private void HandleKeys()
        {
            if (GameInput.KeyDown(GameKey.Escape))
            {
                HandleEscape();
                return;
            }

            if (_chat.IsFocused || UIFocus.IsTyping)
            {
                return;
            }

            if (GameInput.KeyDown(GameKey.Enter) && _chat.LastSubmitFrame != Time.frameCount)
            {
                _chat.Focus();
                return;
            }

            if (GameInput.KeyDown(GameKey.A))
            {
                ToggleStatus();
            }

            if (GameInput.KeyDown(GameKey.S))
            {
                ToggleSkills();
            }

            if (GameInput.KeyDown(GameKey.E))
            {
                ToggleInventory();
            }
        }

        private void HandleEscape()
        {
            // The InputField may already have dropped focus on this Escape press.
            if (_chat.IsFocused || _chatFocusedLastFrame)
            {
                _chat.Blur();
                return;
            }

            if (_caster.IsTargeting)
            {
                _caster.CancelTargeting();
                return;
            }

            // Close the top-most open window first (Ragnarok behaviour), then toggle the menu.
            UIWindow top = null;
            foreach (var window in new[] { _status.Window, _skills.Window, _inventory.Window, _jobChange.Window, _menu })
            {
                if (window.IsOpen && (top == null || window.transform.GetSiblingIndex() > top.transform.GetSiblingIndex()))
                {
                    top = window;
                }
            }

            if (top != null)
            {
                top.Hide();
            }
            else
            {
                _menu.Show();
            }
        }

        private void OnDestroy()
        {
            _basicInfo?.Dispose();
            _hotkeyBar?.Dispose();
            _status?.Dispose();
            _skills?.Dispose();
            _inventory?.Dispose();
            _jobChange?.Dispose();
            _chat?.Dispose();
            _buffs?.Dispose();
        }
    }
}
