using System.Security.Cryptography;
using System.Text;

namespace SportHub.Services.Security
{
    public static class PasswordHasher
    {
        public static string Hash(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
