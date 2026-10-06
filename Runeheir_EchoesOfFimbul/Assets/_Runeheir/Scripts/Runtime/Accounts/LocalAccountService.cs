using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Runeheir.Characters;
using UnityEngine;

namespace Runeheir.Accounts
{
    /// <summary>
    /// What the client needs from the login + char servers. The offline prototype uses
    /// <see cref="LocalAccountService"/>; Phase 6 swaps in a Mirror/HTTP implementation
    /// without touching the login or character-select screens.
    /// </summary>
    public interface IAccountService
    {
        bool IsOffline { get; }

        Task<OpResult<string>> RegisterAsync(string username, string password);

        Task<OpResult<string>> LoginAsync(string username, string password);

        Task<IReadOnlyList<ServerInfo>> GetServersAsync();

        Task<List<CharacterRecord>> GetCharactersAsync(string username);

        Task<OpResult<CharacterRecord>> CreateCharacterAsync(string username, int slot, CharacterCreateRequest request);

        Task<OpResult> DeleteCharacterAsync(string username, int slot, string confirmName);

        Task<OpResult> SaveCharacterAsync(string username, CharacterRecord record);
    }

    /// <summary>
    /// Offline account/character storage in a JSON file under <c>Application.persistentDataPath</c>.
    /// Passwords are PBKDF2-hashed; hashing runs on a worker thread so the UI never hitches.
    /// Safety rules: writes go to <c>.tmp</c> and replace the file atomically, keeping the previous version as
    /// <c>.bak</c>; a damaged file falls back to <c>.tmp</c>/<c>.bak</c>; a file that can't be read (locked, no
    /// permission) or that another game instance changed is never overwritten. Saves then fail with a message.
    /// </summary>
    public sealed class LocalAccountService : IAccountService
    {
        public const string FileName = "runeheir_local_accounts.json";

        private readonly string _path;
        private readonly AccountStore _store;

        /// <summary>Why writes are refused (file unreadable or changed elsewhere), or null when storage is healthy.</summary>
        private string _storageError;

        /// <summary>The main file as this instance last saw it (including "absent"), to detect other writers.</summary>
        private bool _fileStateKnown;
        private bool _knownExists;
        private DateTime _knownWriteUtc;
        private long _knownLength;

        public LocalAccountService(string path = null)
        {
            _path = path ?? Path.Combine(Application.persistentDataPath, FileName);
            var db = Load();

            // Recorded whichever way Load went (parsed, recovered from .tmp/.bak, or no file yet), so the first
            // save can tell whether another window created or changed the file in the meantime.
            RememberFileState();
            _store = new AccountStore(db, Persist);
        }

        /// <summary>Non-null when the account file can't be written safely; the login screen shows it.</summary>
        public string StorageError => _storageError;

        public bool IsOffline => true;

        public string DatabasePath => _path;

        public Task<OpResult<string>> RegisterAsync(string username, string password)
        {
            return Task.Run(() => _store.Register(username, password));
        }

        public Task<OpResult<string>> LoginAsync(string username, string password)
        {
            return Task.Run(() => _store.Login(username, password));
        }

        public Task<IReadOnlyList<ServerInfo>> GetServersAsync()
        {
            IReadOnlyList<ServerInfo> servers = new List<ServerInfo>
            {
                new ServerInfo
                {
                    Id = "local",
                    Name = "Vigrid Haven",
                    Description = "Offline prototype server (this PC)",
                    Online = true,
                    Population = _store.CountCharacters(),
                },
                new ServerInfo
                {
                    Id = "asgard",
                    Name = "Asgard",
                    Description = "Online realm — Mirror networking arrives in Phase 6",
                    Online = false,
                },
            };
            return Task.FromResult(servers);
        }

        public Task<List<CharacterRecord>> GetCharactersAsync(string username)
        {
            return Completed(() => _store.GetCharacters(username));
        }

        public Task<OpResult<CharacterRecord>> CreateCharacterAsync(string username, int slot, CharacterCreateRequest request)
        {
            return Completed(() => _store.CreateCharacter(username, slot, request));
        }

        public Task<OpResult> DeleteCharacterAsync(string username, int slot, string confirmName)
        {
            return Completed(() => _store.DeleteCharacter(username, slot, confirmName));
        }

        public Task<OpResult> SaveCharacterAsync(string username, CharacterRecord record)
        {
            return Completed(() => _store.SaveCharacter(username, record));
        }

        /// <summary>Runs a quick store call; an exception becomes a faulted task, never a synchronous throw.</summary>
        private static Task<T> Completed<T>(Func<T> operation)
        {
            try
            {
                return Task.FromResult(operation());
            }
            catch (Exception exception)
            {
                return Task.FromException<T>(exception);
            }
        }

        private AccountDatabase Load()
        {
            // The main file first; then the replacement left by an interrupted save; then the previous version.
            foreach (string candidate in new[] { _path, _path + ".tmp", _path + ".bak" })
            {
                if (!File.Exists(candidate))
                {
                    continue;
                }

                string json;
                try
                {
                    json = File.ReadAllText(candidate);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    // Locked (another instance, antivirus, backup) or no permission: NOT corruption. Starting empty
                    // and saving later would erase every account, so refuse to write for this session instead.
                    _storageError = $"Account data can't be read ({exception.Message}). Close other Runeheir windows and restart.";
                    Debug.LogError($"[Runeheir] {_storageError}");
                    return new AccountDatabase();
                }

                AccountDatabase db = null;
                try
                {
                    db = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<AccountDatabase>(json);
                }
                catch (Exception exception)
                {
                    // JsonUtility reports malformed JSON as ArgumentException; treat any parse failure as damage.
                    Debug.LogWarning($"[Runeheir] {candidate} is damaged ({exception.Message}).");
                }

                if (db != null)
                {
                    if (candidate != _path)
                    {
                        Debug.LogWarning($"[Runeheir] Recovered account data from {Path.GetFileName(candidate)}.");
                    }

                    return db;
                }

                // Set the damaged main file aside so the next save can't rotate it into .bak.
                if (KeepCorruptCopy(candidate) && candidate == _path)
                {
                    TryDelete(_path);
                }
            }

            return new AccountDatabase();
        }

        private void Persist(AccountDatabase db)
        {
            if (_storageError != null)
            {
                throw new IOException(_storageError);
            }

            // Last-writer-wins would silently undo another instance's progress (two game windows, or the
            // editor's Delete Local Account Database menu during Play): refuse instead.
            if (_fileStateKnown && FileChangedElsewhere())
            {
                _storageError = "Account data was changed outside this game window. Restart to load the latest data.";
                throw new IOException(_storageError);
            }

            string json = JsonUtility.ToJson(db, true);
            string temp = _path + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
            File.WriteAllText(temp, json);
            if (File.Exists(_path))
            {
                File.Replace(temp, _path, _path + ".bak");
            }
            else
            {
                File.Move(temp, _path);
            }

            RememberFileState();
        }

        private void RememberFileState()
        {
            var info = new FileInfo(_path);
            _fileStateKnown = true;
            _knownExists = info.Exists;
            _knownWriteUtc = _knownExists ? info.LastWriteTimeUtc : default;
            _knownLength = _knownExists ? info.Length : 0;
        }

        private bool FileChangedElsewhere()
        {
            var info = new FileInfo(_path);
            if (info.Exists != _knownExists)
            {
                return true;
            }

            return info.Exists && (info.LastWriteTimeUtc != _knownWriteUtc || info.Length != _knownLength);
        }

        private static bool KeepCorruptCopy(string path)
        {
            string backup = path + ".corrupt-" + DateTime.UtcNow.Ticks;
            try
            {
                File.Copy(path, backup, true);
                Debug.LogWarning($"[Runeheir] Kept a copy of the damaged file as {backup}.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Runeheir] Could not copy the damaged file: {exception.Message}");
                return false;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Runeheir] Could not remove {path}: {exception.Message}");
            }
        }
    }
}
