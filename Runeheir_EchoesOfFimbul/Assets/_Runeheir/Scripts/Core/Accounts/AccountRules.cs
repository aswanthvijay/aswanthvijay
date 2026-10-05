using System.Text.RegularExpressions;
using Runeheir.Characters;

namespace Runeheir.Accounts
{
    /// <summary>Validation rules shared by the client UI and (later) the login/char servers.</summary>
    public static class AccountRules
    {
        public const int MinUsernameLength = 4;
        public const int MaxUsernameLength = 23;
        public const int MinPasswordLength = 6;
        public const int MaxPasswordLength = 64;
        public const int MinCharacterNameLength = 4;
        public const int MaxCharacterNameLength = 23;

        /// <summary>Character slots per account (shown as a 3x3 grid on character select).</summary>
        public const int MaxCharacterSlots = 9;

        // \z, not $: in .NET "$" also matches before a trailing newline ("odin\n" would pass).
        private static readonly Regex UsernamePattern = new Regex("^[A-Za-z0-9_]+\\z");
        private static readonly Regex CharacterNamePattern = new Regex("^[A-Za-z0-9 ]+\\z");

        public static bool ValidateUsername(string username, out string error)
        {
            if (string.IsNullOrEmpty(username) || username.Length < MinUsernameLength || username.Length > MaxUsernameLength)
            {
                error = $"Username must be {MinUsernameLength}-{MaxUsernameLength} characters.";
                return false;
            }

            if (!UsernamePattern.IsMatch(username))
            {
                error = "Username may only use letters, numbers and _.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool ValidatePassword(string password, out string error)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength || password.Length > MaxPasswordLength)
            {
                error = $"Password must be {MinPasswordLength}-{MaxPasswordLength} characters.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool ValidateCharacterName(string rawName, out string error)
        {
            string name = CharacterNames.Normalize(rawName);
            if (name.Length < MinCharacterNameLength || name.Length > MaxCharacterNameLength)
            {
                error = $"Name must be {MinCharacterNameLength}-{MaxCharacterNameLength} characters.";
                return false;
            }

            if (!CharacterNamePattern.IsMatch(name))
            {
                error = "Name may only use letters, numbers and single spaces.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
