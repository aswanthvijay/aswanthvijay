using System;
using System.Collections.Generic;
using Runeheir.Characters;

namespace Runeheir.Accounts
{
    [Serializable]
    public sealed class AccountRecord
    {
        public string Username;
        public string PasswordHash;
        public string PasswordSalt;
        public int PasswordIterations;
        public long CreatedUnixMs;
        public long LastLoginUnixMs;
        public List<CharacterRecord> Characters = new List<CharacterRecord>();
    }

    /// <summary>Root object persisted by the local (offline) account service.</summary>
    [Serializable]
    public sealed class AccountDatabase
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public List<AccountRecord> Accounts = new List<AccountRecord>();
    }

    [Serializable]
    public sealed class ServerInfo
    {
        public string Id;
        public string Name;
        public string Description;
        public bool Online;
        public int Population;
    }

    public readonly struct OpResult
    {
        private OpResult(bool success, string error)
        {
            Success = success;
            Error = error;
        }

        public bool Success { get; }

        public string Error { get; }

        public static OpResult Ok()
        {
            return new OpResult(true, null);
        }

        public static OpResult Fail(string error)
        {
            return new OpResult(false, error);
        }
    }

    public readonly struct OpResult<T>
    {
        private OpResult(bool success, T value, string error)
        {
            Success = success;
            Value = value;
            Error = error;
        }

        public bool Success { get; }

        public T Value { get; }

        public string Error { get; }

        public static OpResult<T> Ok(T value)
        {
            return new OpResult<T>(true, value, null);
        }

        public static OpResult<T> Fail(string error)
        {
            return new OpResult<T>(false, default, error);
        }
    }
}
