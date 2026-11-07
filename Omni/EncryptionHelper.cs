/* 
    The OMNI telemetry app logs data and uploads data captured by 
    eChook.uk systems for Greenpower electric cars.
    Copyright (C) 2026  Keith Roberts

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Security.Cryptography;

namespace Omni
{
    public static class EncryptionHelper
    {
        // based on: https://ianvink.wordpress.com/2022/12/03/a-straightforward-way-in-c-net-to-encrypt-and-decrypt-a-string-using-aes/

        private static string? internalEncryptionKey = null;

        public static void Init(string? InternalEncryptionKey)
        {
            ArgumentNullException.ThrowIfNull(InternalEncryptionKey);
            internalEncryptionKey = InternalEncryptionKey;
        }

        public static string? EncryptString(string? plaintext)
        {
            if (string.IsNullOrWhiteSpace(internalEncryptionKey)) throw new Exception("Internal encryption key not defined");
            return EncryptString(plaintext, internalEncryptionKey);
        }

        public static string? DecryptString(string? encrypted)
        {
            if (string.IsNullOrWhiteSpace(internalEncryptionKey)) throw new Exception("Internal encryption key not defined");
            return DecryptString(encrypted, internalEncryptionKey);
        }

        public static string? EncryptString(string? plaintext, string? password)
        {
            // Validate arguments
            ArgumentNullException.ThrowIfNull(plaintext);
            ArgumentNullException.ThrowIfNull(password);

            // Convert the plaintext string to a byte array
            var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);

            // Create a random salt
            var saltBytes = RandomNumberGenerator.GetBytes(32);

            // Create random IV
            var ivBytes = RandomNumberGenerator.GetBytes(16);

            // Use the password to encrypt the plaintext
            var aes = Aes.Create();
            aes.Key = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 1000, HashAlgorithmName.SHA256, 32);
            aes.IV = ivBytes;
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(plaintextBytes, 0, plaintextBytes.Length);
                cs.FlushFinalBlock();
            }

            // Create the final bytes as a concatenation of the 32 random salt bytes, the 16 random iv bytes and the n ciphertext bytes
            byte[] finalBytes = [.. saltBytes, .. ivBytes, .. ms.ToArray()];

            // Convert final bytes to base64 string
            var encryptedString = Convert.ToBase64String(finalBytes);
            return encryptedString;
        }

        public static string? DecryptString(string? encrypted, string? password)
        {
            // Validate arguments
            ArgumentNullException.ThrowIfNull(encrypted);
            ArgumentNullException.ThrowIfNull(password);

            // Convert the encrypted string to a byte array comprising 32 salt bytes, 16 IV bytes, n bytes of ciphertext
            var encryptedBytes = Convert.FromBase64String(encrypted);

            // Get the salt
            var saltBytes = encryptedBytes.Take(32).ToArray();

            // Get the IV
            var ivBytes = encryptedBytes.Skip(32).Take(16).ToArray();

            // Get the ciphertext
            var cipherBytes = encryptedBytes.Skip(48).Take(encryptedBytes.Length - 48).ToArray();

            // Use the password to decrypt the encrypted string
            var aes = Aes.Create();
            aes.Key = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 1000, HashAlgorithmName.SHA256, 32);
            aes.IV = ivBytes;
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(cipherBytes, 0, cipherBytes.Length);
            }

            // Get decrypted string
            var decryptedString = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            return decryptedString;
        }

    }
}
