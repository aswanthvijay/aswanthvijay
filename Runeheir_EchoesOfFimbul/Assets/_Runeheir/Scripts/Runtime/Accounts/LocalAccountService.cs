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
    /// </summary>
    public sealed class LocalAccountService : IAccountService
    {
        public const string FileName = "runeheir_local_accounts.json";

        private readonly string _path;
        private readonly AccountStore _store;

        public LocalAccountService(string path = null)
        {
            _path = path ?? Path.Combine(Application.persistentDataPath, FileName);
            _store = new AccountStore(Load(), Persist);
        }

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
            return Task.FromResult(_store.GetCharacters(username));
        }

        public Task<OpResult<CharacterRecord>> CreateCharacterAsync(string username, int slot, CharacterCreateRequest request)
        {
            return Task.FromResult(_store.CreateCharacter(username, slot, request));
        }

        public Task<OpResult> DeleteCharacterAsync(string username, int slot, string confirmName)
        {
            return Task.FromResult(_store.DeleteCharacter(username, slot, confirmName));
        }

        public Task<OpResult> SaveCharacterAsync(string username, CharacterRecord record)
        {
            return Task.FromResult(_store.SaveCharacter(username, record));
        }

        private AccountDatabase Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var db = JsonUtility.FromJson<AccountDatabase>(File.ReadAllText(_path));
                    if (db != null)
                    {
                        return db;
                    }
                }
            }
            catch (Exception exception)
            {
                string backup = _path + ".corrupt-" + DateTime.UtcNow.Ticks;
                Debug.LogWarning($"[Runeheir] Could not read {_path} ({exception.Message}). Backing it up to {backup} and starting fresh.");
                try
                {
                    File.Copy(_path, backup, true);
                }
                catch (Exception copyException)
                {
                    Debug.LogWarning($"[Runeheir] Backup failed: {copyException.Message}");
                }
            }

            return new AccountDatabase();
        }

        private void Persist(AccountDatabase db)
        {
            string json = JsonUtility.ToJson(db, true);
            string temp = _path + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
            File.WriteAllText(temp, json);
            if (File.Exists(_path))
            {
                File.Replace(temp, _path, null);
            }
            else
            {
                File.Move(temp, _path);
            }
        }
    }
}
