using System;
using System.IO;
using System.Text;

namespace FalseCrypt.Crypto
{
    public static class EncryptionCryptoWrapper
    {
        public static void EncryptFileWithPassword(FileInfo file, string password)
        {
            if (file == null || !file.Exists)
                throw new FileNotFoundException();

            var bytes = File.ReadAllBytes(file.FullName);

            using (var fileStream = new FileStream(file.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                var cipherText = EncryptMessage(bytes, password);
                fileStream.Write(cipherText, 0, cipherText.Length);
            }

            file.MoveTo(Path.Combine(file.Directory.FullName, file.Name + ".falsecrypt"));
        }

        public static void EncryptFile(FileInfo file, byte[] key, byte[] salt)
        {
            if (file == null || !file.Exists)
                throw new FileNotFoundException();

            var bytes = File.ReadAllBytes(file.FullName);

            using (var fileStream = new FileStream(file.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                var cipherText = WeakSymmetricEncryption.Encrypt(bytes, key, salt);
                fileStream.Write(cipherText, 0, cipherText.Length);
            }

            file.MoveTo(Path.Combine(file.Directory.FullName, file.Name + ".falsecrypt"));
        }

        public static string EncryptMessage(string secretMessage, string password, Encoding encoding)
        {
            if (string.IsNullOrEmpty(secretMessage))
                throw new ArgumentException("Secret Message Required!", nameof(secretMessage));
            return EncryptMessage(secretMessage, encoding.GetBytes(secretMessage), encoding);
        }

        public static string EncryptMessage(string secretMessage, byte[] key, Encoding encoding)
        {
            if (string.IsNullOrEmpty(secretMessage))
                throw new ArgumentException("Secret Message Required!", nameof(secretMessage));

            var cipherText = EncryptMessage(encoding.GetBytes(secretMessage), key);
            return Convert.ToBase64String(cipherText);
        }

        public static byte[] EncryptMessage(byte[] secretMessage, byte[] key)
        {
            return WeakSymmetricEncryption.Encrypt(secretMessage, key, null);
        }

        public static byte[] EncryptMessage(byte[] secretMessage, string password)
        {
            var data = WeakPasswordDerivation.DerivePassword(password);
            return WeakSymmetricEncryption.Encrypt(secretMessage, data.Key, data.Salt);
        }

        public static void DecryptFileWithPassword(FileInfo file, string password)
        {
            if (file == null || !file.Exists)
                throw new FileNotFoundException();

            var bytes = File.ReadAllBytes(file.FullName);
            var tmpName = file.FullName + ".tmp";
            try
            {
                using (var fileStream =
                    new FileStream(tmpName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    var m = DecryptMessage(bytes, password);
                    fileStream.Write(m, 0, m.Length);
                }
                File.Delete(file.FullName);
                File.Move(tmpName, tmpName.Replace(".falsecrypt.tmp", ""));
            }
            catch (Exception)
            {
                File.Delete(tmpName);
                throw;
            }

        }

        public static void DecryptFile(FileInfo file, byte[] key)
        {
            if (file == null || !file.Exists)
                throw new FileNotFoundException();

            var bytes = File.ReadAllBytes(file.FullName);
            var tmpName = file.FullName + ".tmp";
            try
            {
                using (var fileStream =
                    new FileStream(tmpName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    var m = WeakSymmetricEncryption.Decrypt(bytes, key, WeakCryptoConfig.SaltSizeBytes);
                    fileStream.Write(m, 0, m.Length);
                }
                File.Delete(file.FullName);
                File.Move(tmpName, tmpName.Replace(".falsecrypt.tmp", ""));
            }
            catch (Exception)
            {
                File.Delete(file.FullName + ".tmp");
                throw;
            }
        }

        public static string DecryptMessage(string encryptedMessage, string password, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(encryptedMessage))
                throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));

            var cipherText = Convert.FromBase64String(encryptedMessage);
            var plainText = DecryptMessage(cipherText, password);
            return plainText == null ? null : Encoding.UTF8.GetString(plainText);
        }

        public static string DecryptMessage(string encryptedMessage, byte[] key, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(encryptedMessage))
                throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));

            var cipherText = Convert.FromBase64String(encryptedMessage);
            var plainText = WeakSymmetricEncryption.Decrypt(cipherText, key, 0);
            return plainText == null ? null : Encoding.UTF8.GetString(plainText);
        }

        public static byte[] DecryptMessage(byte[] encryptedMessage, string password)
        {
            if (encryptedMessage == null || encryptedMessage.Length == 0)
                throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));

            var data = WeakPasswordDerivation.DerivePassword(password);
            return WeakSymmetricEncryption.Decrypt(encryptedMessage, data.Key, data.Salt.Length);
        }
    }
}
