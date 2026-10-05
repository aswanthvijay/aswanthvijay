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
            Username = null;
            Server = null;
            ActiveCharacter = null;
            IsTemporaryCharacter = false;
        }

        public void EnterWorld(CharacterRecord record)
        {
            ActiveCharacter = record ?? throw new ArgumentNullException(nameof(record));
            IsTemporaryCharacter = false;
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
            return record;
        }

        public void SaveActiveCharacter()
        {
            if (ActiveCharacter == null || IsTemporaryCharacter || !IsLoggedIn)
            {
                return;
            }

            ObserveFaults(Accounts.SaveCharacterAsync(Username, ActiveCharacter), "save character");
        }

        public void ReturnToCharacterSelect()
        {
            SaveActiveCharacter();
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

        private static async void ObserveFaults(Task<OpResult> task, string what)
        {
            try
            {
                var result = await task;
                if (!result.Success)
                {
                    Debug.LogWarning($"[Runeheir] Could not {what}: {result.Error}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
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
