using System;
using System.Security.Cryptography;

namespace Runeheir.Accounts
{
    /// <summary>
    /// PBKDF2-SHA256 password hashing with a random 16-byte salt. Even the offline prototype never
    /// stores plain-text passwords; the same code can run on the Phase 6 login server.
    /// </summary>
    public static class PasswordHasher
    {
        public const int DefaultIterations = 60000;
        private const int SaltBytes = 16;
        private const int HashBytes = 32;

        public static void Hash(string password, out string hashBase64, out string saltBase64, out int iterations)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var salt = new byte[SaltBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            iterations = DefaultIterations;
            hashBase64 = Convert.ToBase64String(Derive(password, salt, iterations));
            saltBase64 = Convert.ToBase64String(salt);
        }

        public static bool Verify(string password, string hashBase64, string saltBase64, int iterations)
        {
            if (password == null || string.IsNullOrEmpty(hashBase64) || string.IsNullOrEmpty(saltBase64) || iterations <= 0)
            {
                return false;
            }

            byte[] expected;
            byte[] salt;
            try
            {
                expected = Convert.FromBase64String(hashBase64);
                salt = Convert.FromBase64String(saltBase64);
            }
            catch (FormatException)
            {
                return false;
            }

            return FixedTimeEquals(expected, Derive(password, salt, iterations));
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(HashBytes);
            }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}
