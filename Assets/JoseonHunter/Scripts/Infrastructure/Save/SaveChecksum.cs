using System.Security.Cryptography;
using System.Text;

namespace JoseonHunter.Infrastructure.Save
{
    public static class SaveChecksum
    {
        public static string ForCanonicalPayload(string payload)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) builder.Append(value.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
