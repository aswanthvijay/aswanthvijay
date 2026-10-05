using System;
using System.Collections.Generic;
using Runeheir.Characters;

namespace Runeheir.Accounts
{
    /// <summary>
    /// Login-server + char-server rules over an <see cref="AccountDatabase"/>: register, login,
    /// character slots, create/delete/save. Storage is injected so the same rules back the offline
    /// prototype (JSON file) and, later, a real server. Thread-safe: password hashing may run on a
    /// worker thread. Character records cross this boundary as copies, like network messages would.
    /// A failed write (disk full, read-only file, file changed by another instance) never throws: the change
    /// is rolled back in memory, so memory and disk stay in step, and the call returns a failure.
    /// </summary>
    public sealed class AccountStore
    {
        private readonly object _gate = new object();
        private readonly AccountDatabase _db;
        private readonly Action<AccountDatabase> _persist;
        private readonly Func<long> _nowUnixMs;

        public AccountStore(AccountDatabase database, Action<AccountDatabase> persist, Func<long> nowUnixMs = null)
        {
            _db = database ?? new AccountDatabase();
            _db.Accounts = _db.Accounts ?? new List<AccountRecord>();
            _persist = persist ?? (_ => { });
            _nowUnixMs = nowUnixMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        public OpResult<string> Register(string username, string password)
        {
            if (!AccountRules.ValidateUsername(username, out string error) || !AccountRules.ValidatePassword(password, out error))
            {
                return OpResult<string>.Fail(error);
            }

            PasswordHasher.Hash(password, out string hash, out string salt, out int iterations);

            lock (_gate)
            {
                if (FindAccount(username) != null)
                {
                    return OpResult<string>.Fail("That username is already taken.");
                }

                var account = new AccountRecord
                {
                    Username = username,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    PasswordIterations = iterations,
                    CreatedUnixMs = _nowUnixMs(),
                };
                _db.Accounts.Add(account);
                if (!TryPersist(out string saveError))
                {
                    _db.Accounts.Remove(account);
                    return OpResult<string>.Fail(saveError);
                }

                return OpResult<string>.Ok(account.Username);
            }
        }

        public OpResult<string> Login(string username, string password)
        {
            AccountRecord account;
            lock (_gate)
            {
                account = FindAccount(username);
            }

            // Same message for unknown user and wrong password (no username probing).
            const string invalid = "Incorrect username or password.";
            if (account == null)
            {
                return OpResult<string>.Fail(invalid);
            }

            if (!PasswordHasher.Verify(password, account.PasswordHash, account.PasswordSalt, account.PasswordIterations))
            {
                return OpResult<string>.Fail(invalid);
            }

            lock (_gate)
            {
                // Bookkeeping only: if this write fails the login still succeeds.
                account.LastLoginUnixMs = _nowUnixMs();
                TryPersist(out _);
            }

            return OpResult<string>.Ok(account.Username);
        }

        public List<CharacterRecord> GetCharacters(string username)
        {
            lock (_gate)
            {
                var result = new List<CharacterRecord>();
                var account = FindAccount(username);
                if (account == null)
                {
                    return result;
                }

                foreach (var character in account.Characters)
                {
                    character.Sanitize();
                    result.Add(character.Clone());
                }

                result.Sort((a, b) => a.Slot.CompareTo(b.Slot));
                return result;
            }
        }

        public OpResult<CharacterRecord> CreateCharacter(string username, int slot, CharacterCreateRequest request)
        {
            if (request == null)
            {
                return OpResult<CharacterRecord>.Fail("Missing character data.");
            }

            if (!AccountRules.ValidateCharacterName(request.Name, out string error))
            {
                return OpResult<CharacterRecord>.Fail(error);
            }

            if (slot < 0 || slot >= AccountRules.MaxCharacterSlots)
            {
                return OpResult<CharacterRecord>.Fail("Invalid character slot.");
            }

            lock (_gate)
            {
                var account = FindAccount(username);
                if (account == null)
                {
                    return OpResult<CharacterRecord>.Fail("Not logged in.");
                }

                if (account.Characters.Exists(c => c.Slot == slot))
                {
                    return OpResult<CharacterRecord>.Fail("That slot is already used.");
                }

                if (IsNameTakenLocked(request.Name))
                {
                    return OpResult<CharacterRecord>.Fail("That name is already taken.");
                }

                var record = CharacterFactory.Create(request, slot, _nowUnixMs());
                account.Characters.Add(record);
                if (!TryPersist(out string saveError))
                {
                    account.Characters.Remove(record);
                    return OpResult<CharacterRecord>.Fail(saveError);
                }

                return OpResult<CharacterRecord>.Ok(record.Clone());
            }
        }

        /// <summary>Deletion requires re-typing the character name (stand-in for Ragnarok's e-mail check).</summary>
        public OpResult DeleteCharacter(string username, int slot, string confirmName)
        {
            lock (_gate)
            {
                var account = FindAccount(username);
                var character = account?.Characters.Find(c => c.Slot == slot);
                if (character == null)
                {
                    return OpResult.Fail("No character in that slot.");
                }

                if (!string.Equals(CharacterNames.Normalize(confirmName), character.Name, StringComparison.Ordinal))
                {
                    return OpResult.Fail("Type the character name exactly to confirm deletion.");
                }

                int index = account.Characters.IndexOf(character);
                account.Characters.RemoveAt(index);
                if (!TryPersist(out string saveError))
                {
                    account.Characters.Insert(index, character);
                    return OpResult.Fail(saveError);
                }

                return OpResult.Ok();
            }
        }

        public OpResult SaveCharacter(string username, CharacterRecord record)
        {
            if (record == null)
            {
                return OpResult.Fail("Missing character data.");
            }

            lock (_gate)
            {
                var account = FindAccount(username);
                if (account == null)
                {
                    return OpResult.Fail("Not logged in.");
                }

                int index = account.Characters.FindIndex(c => c.Slot == record.Slot);
                if (index < 0 || !string.Equals(account.Characters[index].Name, record.Name, StringComparison.Ordinal))
                {
                    return OpResult.Fail("Character does not belong to this account.");
                }

                var previous = account.Characters[index];
                var copy = record.Clone();
                copy.Sanitize();
                copy.LastPlayedUnixMs = _nowUnixMs();
                account.Characters[index] = copy;
                if (!TryPersist(out string saveError))
                {
                    account.Characters[index] = previous;
                    return OpResult.Fail(saveError);
                }

                return OpResult.Ok();
            }
        }

        public bool IsNameTaken(string name)
        {
            lock (_gate)
            {
                return IsNameTakenLocked(name);
            }
        }

        public int CountCharacters()
        {
            lock (_gate)
            {
                int total = 0;
                foreach (var account in _db.Accounts)
                {
                    total += account.Characters.Count;
                }

                return total;
            }
        }

        private bool TryPersist(out string error)
        {
            try
            {
                _persist(_db);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "Could not save account data: " + exception.Message;
                return false;
            }
        }

        private bool IsNameTakenLocked(string name)
        {
            string normalized = CharacterNames.Normalize(name);
            foreach (var account in _db.Accounts)
            {
                foreach (var character in account.Characters)
                {
                    if (string.Equals(character.Name, normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private AccountRecord FindAccount(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            foreach (var account in _db.Accounts)
            {
                if (string.Equals(account.Username, username, StringComparison.OrdinalIgnoreCase))
                {
                    account.Characters = account.Characters ?? new List<CharacterRecord>();
                    return account;
                }
            }

            return null;
        }
    }
}
