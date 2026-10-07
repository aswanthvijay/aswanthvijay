using System;
using Runeheir.Accounts;
using Runeheir.Controls;
using Runeheir.Online;
using Runeheir.Session;
using Runeheir.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runeheir.FrontEnd
{
    /// <summary>ID / password login box with "Save ID", Register and Exit (Ragnarok client style).</summary>
    public sealed class LoginScreen : FrontEndScreen
    {
        private const string SavedIdKey = "runeheir.saved_id";
        private const float Width = 460f;

        private readonly InputField _username;
        private readonly InputField _password;
        private readonly Toggle _saveId;
        private readonly Button _loginButton;
        private readonly Button _registerButton;
        private readonly Text _status;

        public LoginScreen(FrontEndController context) : base(context, "LoginScreen")
        {
            var panel = UIFactory.CreateFramedPanel(Frame, "LoginPanel", UITheme.WindowBg);
            panel.rectTransform.SetRect((1920f - Width) / 2f, 430f, Width, 380f);
            var content = UIFactory.CreateRect("Content", panel.transform);
            content.Stretch(24f, 18f, 24f, 18f);
            float innerWidth = Width - 48f;

            CreateHeading(content, "Login", 0f, innerWidth);

            Label(content, "ID", 52f);
            _username = UIFactory.CreateInputField(content, "Username", characterLimit: AccountRules.MaxUsernameLength);
            _username.GetComponent<RectTransform>().SetRect(100f, 48f, innerWidth - 100f, 36f);

            Label(content, "Password", 98f);
            _password = UIFactory.CreateInputField(content, "Password", password: true, characterLimit: AccountRules.MaxPasswordLength);
            _password.GetComponent<RectTransform>().SetRect(100f, 94f, innerWidth - 100f, 36f);

            _saveId = UIFactory.CreateToggle(content, "Save ID", false);
            _saveId.GetComponent<RectTransform>().SetRect(100f, 140f, 160f, 26f);

            _status = CreateStatus(content, 0f, 174f, innerWidth);

            _loginButton = UIFactory.CreateButton(content, "Login", OnLogin, 18);
            _loginButton.GetComponent<RectTransform>().SetRect(0f, 210f, innerWidth / 2f - 6f, 44f);
            _registerButton = UIFactory.CreateButton(content, "Register", OnRegister, 18);
            _registerButton.GetComponent<RectTransform>().SetRect(innerWidth / 2f + 6f, 210f, innerWidth / 2f - 6f, 44f);

            var realms = UIFactory.CreateButton(content, "Realms", OnRealms, 16);
            realms.GetComponent<RectTransform>().SetRect(innerWidth / 2f - 146f, 270f, 140f, 36f);
            var exit = UIFactory.CreateButton(content, "Exit", GameSession.QuitGame, 16);
            exit.GetComponent<RectTransform>().SetRect(innerWidth / 2f + 6f, 270f, 140f, 36f);

            var hint = UIFactory.CreateText(content, "New here? Type a username + password and press Register.", 13, UITheme.TextDim, TextAnchor.MiddleCenter);
            hint.rectTransform.SetRect(0f, 314f, innerWidth, 24f);
        }

        public override void HandleKeys()
        {
            if (Busy)
            {
                return;
            }

            if (GameInput.KeyDown(GameKey.Escape))
            {
                OnRealms();
                return;
            }

            if (GameInput.KeyDown(GameKey.Tab))
            {
                Focus(_username.isFocused ? _password : _username);
            }
            else if (GameInput.KeyDown(GameKey.Enter))
            {
                OnLogin();
            }
        }

        protected override void OnShow()
        {
            string saved = PlayerPrefs.GetString(SavedIdKey, string.Empty);
            _saveId.isOn = !string.IsNullOrEmpty(saved);
            _username.text = saved;
            _password.text = string.Empty;
            string storageError = (Context.Session.Accounts as LocalAccountService)?.StorageError;
            if (storageError != null)
            {
                SetStatus(_status, storageError, error: true);
            }
            else if (OnlineSession.Launcher != null && OnlineSession.Launcher.IsConnected && !Context.Session.Accounts.IsOffline)
            {
                SetStatus(_status, "Connected. Log in, or Register a new account on this realm.");
            }
            else
            {
                SetStatus(_status, "Offline realm (this PC). The Fimbulwinter has begun.");
            }

            Focus(string.IsNullOrEmpty(saved) ? _username : _password);
        }

        /// <summary>Back to the realm choice (leaves an online realm).</summary>
        private void OnRealms()
        {
            if (Busy)
            {
                return;
            }

            OnlineSession.Launcher?.Shutdown();
            Context.Session.Logout();
            Context.ShowRealm();
        }

        private static void Label(Transform parent, string text, float y)
        {
            var label = UIFactory.CreateText(parent, text, 16, UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.rectTransform.SetRect(0f, y, 96f, 28f);
        }

        private static void Focus(InputField field)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(field.gameObject);
            }

            field.ActivateInputField();
        }

        private async void OnLogin()
        {
            if (Busy)
            {
                return;
            }

            string username = _username.text.Trim();
            string password = _password.text;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                SetStatus(_status, "Enter your ID and password.", error: true);
                return;
            }

            int visit = Visit;
            SetBusy(true, "Connecting to the realm...");
            try
            {
                var result = await Context.Session.Accounts.LoginAsync(username, password);
                if (!IsCurrent(visit))
                {
                    return;
                }

                if (!result.Success)
                {
                    SetStatus(_status, result.Error, error: true);
                    return;
                }

                if (_saveId.isOn)
                {
                    PlayerPrefs.SetString(SavedIdKey, result.Value);
                }
                else
                {
                    PlayerPrefs.DeleteKey(SavedIdKey);
                }

                PlayerPrefs.Save();
                Context.Session.SetLoggedIn(result.Value);
                Context.ShowServerSelect();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (IsCurrent(visit))
                {
                    SetStatus(_status, "Login failed: " + exception.Message, error: true);
                }
            }
            finally
            {
                // Only touch the UI if it still exists and is still this visit (Play mode may have stopped).
                if (IsCurrent(visit))
                {
                    SetBusy(false, null);
                }
            }
        }

        private async void OnRegister()
        {
            if (Busy)
            {
                return;
            }

            string username = _username.text.Trim();
            string password = _password.text;
            if (!AccountRules.ValidateUsername(username, out string error) || !AccountRules.ValidatePassword(password, out error))
            {
                SetStatus(_status, error, error: true);
                return;
            }

            bool registered = false;
            int visit = Visit;
            SetBusy(true, "Carving your name into the runestones...");
            try
            {
                var result = await Context.Session.Accounts.RegisterAsync(username, password);
                if (!IsCurrent(visit))
                {
                    return;
                }

                if (result.Success)
                {
                    registered = true;
                }
                else
                {
                    SetStatus(_status, result.Error, error: true);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (IsCurrent(visit))
                {
                    SetStatus(_status, "Registration failed: " + exception.Message, error: true);
                }
            }
            finally
            {
                if (IsCurrent(visit))
                {
                    SetBusy(false, null);
                }
            }

            if (registered && IsCurrent(visit))
            {
                SetStatus(_status, "Account created! Logging in...");
                OnLogin();
            }
        }

        private void SetBusy(bool busy, string message)
        {
            Busy = busy; // locks every control, Exit included, through the screen's CanvasGroup
            if (message != null)
            {
                SetStatus(_status, message);
            }
        }
    }
}
