using System.Security.Cryptography;
using System.Text;

namespace DFN_BMS.Services
{
    /// <summary>
    /// AES-256-CBC encryption/decryption helper.
    /// 
    /// Store the key in appsettings.json:
    ///   "EncryptionSettings": { "Key": "YOUR-32-CHAR-SECRET-KEY-HERE!!" }
    /// 
    /// The key MUST be exactly 32 characters (256 bits).
    /// </summary>
    public class EncryptionService
    {
        // ── Key must be 32 bytes (256-bit AES) ──────────────────────────
        private readonly byte[] _key;

        public EncryptionService(IConfiguration config)
        {
            var keyStr = config["EncryptionSettings:Key"]
                ?? throw new InvalidOperationException(
                    "EncryptionSettings:Key is missing from appsettings.json");

            if (keyStr.Length != 32)
                throw new InvalidOperationException(
                    "EncryptionSettings:Key must be exactly 32 characters.");

            _key = Encoding.UTF8.GetBytes(keyStr);
        }

        // ─────────────────────────────────────────────────────────────────
        // ENCRYPT  →  returns  Base64( IV[16 bytes] + CipherText )
        // ─────────────────────────────────────────────────────────────────
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.GenerateIV();   // random 16-byte IV per encryption

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Prepend IV → Base64 encode the whole thing
            var combined = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(combined);
        }

        // ─────────────────────────────────────────────────────────────────
        // DECRYPT  →  returns original plain text
        // ─────────────────────────────────────────────────────────────────
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                var combined = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                aes.Key = _key;
                aes.Mode = CipherMode.CBC;

                // First 16 bytes = IV
                var iv = new byte[16];
                var cipherBytes = new byte[combined.Length - 16];
                Buffer.BlockCopy(combined, 0, iv, 0, 16);
                Buffer.BlockCopy(combined, 16, cipherBytes, 0, cipherBytes.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // Return empty if corrupt/tampered — do NOT leak error details
                return string.Empty;
            }
        }
    }
}