using System.Security.Cryptography;
using System.Text;

namespace Mero_Dainiki.Common
{
    /// <summary>
    /// Utility class for security operations like hashing
    /// </summary>
    public static class SecurityUtils
    {
        /// <summary>
        /// Hashes a string using SHA256
        /// </summary>
        /// <param name="input">The string to hash</param>
        /// <returns>Base64 encoded hash string</returns>
        public static string HashString(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Compares a plain text input with a hash
        /// </summary>
        public static bool VerifyHash(string input, string hash)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(hash)) return false;
            return HashString(input) == hash;
        }
    }
}
