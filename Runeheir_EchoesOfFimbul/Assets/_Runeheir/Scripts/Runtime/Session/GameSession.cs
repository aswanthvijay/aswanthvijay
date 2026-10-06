using System;
using System.Threading.Tasks;
using Runeheir.Accounts;
using Runeheir.Characters;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runeheir.Session
{
    /// <summary>Scene names. Keep in sync with Build Settings (the setup wizard adds them for you).</summary>
    public static class SceneFlow
    {
        public const string LoginScene = "RH_Login";

        public static void LoadLogin()
        {
            Load(LoginScene);
        }

        public static void LoadMap(string mapId)
        {
            var map = MapCatalog.Get(mapId) ?? MapCatalog.Get(MapCatalog.StartingMapId);
            Load(map.SceneName);
        }

        public static void Load(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Runeheir] Scene '{sceneName}' is not in Build Settings. Run 'Runeheir > Setup > Build Prototype Scenes' " +
                               "or add it under File > Build Profiles (Build Settings).");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Survives scene loads and carries "who is logged in" and "which character is playing" from the
    /// login scene into the field (Ragnarok's login → char-select → map flow).
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        private static GameSession s_instance;

        private IAccountService _accounts;

        public static GameSession Instance
        {
            get
            {
                if (s_instance == null)
                {
                    var go = new GameObject("[GameSession]");
                    DontDestroyOnLoad(go);
                    s_instance = go.AddComponent<GameSession>();
                }

                return s_instance;
            }
        }

        public static bool Exists => s_instance != null;

        /// <summary>Account backend. Replace before login to plug in a networked service.</summary>
        public IAccountService Accounts
        {
            get => _accounts ?? (_accounts = new LocalAccountService());
            set => _accounts = value;
        }

        public string Username { get; private set; }

        public bool IsLoggedIn => !string.IsNullOrEmpty(Username);

        public ServerInfo Server { get; set; }

        public CharacterRecord ActiveCharacter { get; private set; }

        /// <summary>True when the field scene was played directly (no login); nothing is saved.</summary>
        public bool IsTemporaryCharacter { get; private set; }

        /// <summary>Set when leaving the field so the login scene opens straight on character select.</summary>
        public bool OpenCharacterSelectOnLoad { get; set; }

        public void SetLoggedIn(string username)
        {
            Username = username;
        }

        public void Logout()
        {
            ChatLog.Clear();
            Username = null;
            Server = null;
            ActiveCharacter = null;
            IsTemporaryCharacter = false;
        }

        public void EnterWorld(CharacterRecord record)
        {
            ActiveCharacter = record ?? throw new ArgumentNullException(nameof(record));
            IsTemporaryCharacter = false;
            ChatLog.Clear(); // the previous character's chat, loot and GM output stays with them
            SceneFlow.LoadMap(record.MapId);
        }

        /// <summary>Dev convenience: pressing Play in a field scene without logging in.</summary>
        public CharacterRecord CreateTemporaryCharacter()
        {
            var record = CharacterFactory.Create(
                new CharacterCreateRequest { Name = "Wanderer", Gender = Gender.Male, HairStyle = 1, HairColor = 2 },
                0,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            ActiveCharacter = record;
            IsTemporaryCharacter = true;
            ChatLog.Clear();
            return record;
        }

        /// <summary>
        /// Saves a snapshot of the active character. Never throws: it runs inside gameplay events (level-ups,
        /// monster deaths), where a storage error must not break the game. A failure is logged and shown once
        /// in chat, so progress is never dropped silently.
        /// </summary>
        public Task<OpResult> SaveActiveCharacter()
        {
            if (ActiveCharacter == null || IsTemporaryCharacter || !IsLoggedIn)
            {
                return Task.FromResult(OpResult.Fail("This character is not saved (no account is logged in)."));
            }

            Task<OpResult> save;
            try
            {
                // A copy: gameplay keeps changing the live record while a networked save is in flight.
                save = Accounts.SaveCharacterAsync(Username, ActiveCharacter.Clone());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                save = Task.FromResult(OpResult.Fail(exception.Message));
            }

            return ReportFailures(save);
        }

        public async void ReturnToCharacterSelect()
        {
            if (_returning)
            {
                return;
            }

            _returning = true;
            try
            {
                // Character select reloads the list from the service, so the save must land first.
                await SaveActiveCharacter();
            }
            catch (Exception)
            {
                // Already logged by ObserveFaults; leaving the field must still work.
            }
            finally
            {
                _returning = false;
            }

            bool canReturn = IsLoggedIn && !IsTemporaryCharacter;
            ActiveCharacter = null;
            IsTemporaryCharacter = false;
            OpenCharacterSelectOnLoad = canReturn;
            SceneFlow.LoadLogin();
        }

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool _returning;

        private string _lastReportedSaveError;

        private async Task<OpResult> ReportFailures(Task<OpResult> task)
        {
            OpResult result;
            try
            {
                result = await task;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result = OpResult.Fail(exception.Message);
            }

            if (result.Success)
            {
                _lastReportedSaveError = null;
            }
            else
            {
                Debug.LogWarning($"[Runeheir] Could not save character: {result.Error}");

                // Autosaves retry often; one chat line per distinct error is enough.
                if (result.Error != _lastReportedSaveError)
                {
                    _lastReportedSaveError = result.Error;
                    ChatLog.Error("Progress could not be saved: " + result.Error);
                }
            }

            return result;
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
