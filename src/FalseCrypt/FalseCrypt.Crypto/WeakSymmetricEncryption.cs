using System;
using System.IO;
using System.Security.Cryptography;

namespace FalseCrypt.Crypto
{
    public static class WeakSymmetricEncryption
    {
        internal static byte[] Encrypt(byte[] secretMessage, byte[] key, byte[] saltPayload)
        {
            if (key == null || key.Length != WeakCryptoConfig.KeySizeBytes)
                throw new ArgumentException($"Key needs to be {WeakCryptoConfig.KeySizeBytes} bytes!", nameof(key));

            if (secretMessage == null || secretMessage.Length < 1)
                throw new ArgumentException("Secret Message Required!", nameof(secretMessage));

            var payload = saltPayload ?? new byte[]{};

            // Bug 12: Weak Encryption Provider
            // Bug 13: Weak Operation Mode
            // Bug 14: Not disposing IDisposable
            var des = new DESCryptoServiceProvider
            {
                KeySize = WeakCryptoConfig.KeySizeBytes * 8,
                BlockSize = WeakCryptoConfig.BlockSizeBytes * 8,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            // Bug 15: Constant IV
            des.IV = WeakCryptoConfig.IV;
            des.Key = key;

            byte[] cipherText;
            // Bug 16: Constant IV
            // Bug 17: Not disposing IDisposable
            var encrypter = des.CreateEncryptor(key, WeakCryptoConfig.IV);
            using (var cipherStream = new MemoryStream())
            {
                // Bug 18: Not disposing IDisposable
                var cryptoStream = new CryptoStream(cipherStream, encrypter, CryptoStreamMode.Write);
                using (var binaryWriter = new BinaryWriter(cryptoStream))
                    binaryWriter.Write(secretMessage);
                cipherText = cipherStream.ToArray();

                // BUG 19: array with clear text content should be destroyed after a encryption. Comment below shows how.
                //Array.Clear(bytes, 0, bytes.Length);
            }

            using (var encryptedStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(encryptedStream))
                {
                    binaryWriter.Write(payload);
                    binaryWriter.Write(des.IV);
                    binaryWriter.Write(cipherText);
                }
                return encryptedStream.ToArray();
            }
        }

        internal static byte[] Decrypt(byte[] encryptedMessage, byte[] cryptKey, int saltLength)
        {
            //Basic Usage Error Checks
            if (cryptKey == null || cryptKey.Length != WeakCryptoConfig.KeySizeBytes)
                throw new ArgumentException($"CryptKey needs to be {WeakCryptoConfig.KeySizeBytes} bytes!",
                    nameof(cryptKey));

            if (encryptedMessage == null || encryptedMessage.Length == 0)
                throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));

            var ivLength = WeakCryptoConfig.BlockSizeBytes;

            // Bug 20: Weak Encryption Provider
            // Bug 21: Weak Operation Mode
            using (var des = new DESCryptoServiceProvider
            {
                KeySize = WeakCryptoConfig.KeySizeBytes * 8,
                BlockSize = WeakCryptoConfig.BlockSizeBytes * 8,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            })
            {
                var iv = new byte[ivLength];
                Array.Copy(encryptedMessage, saltLength, iv, 0, iv.Length);

                using (var decrypter = des.CreateDecryptor(cryptKey, iv))
                {
                    using (var plainTextStream = new MemoryStream())
                    {
                        using (var decrypterStream = new CryptoStream(plainTextStream, decrypter, CryptoStreamMode.Write))
                        {
                            using (var binaryWriter = new BinaryWriter(decrypterStream))
                            {
                                binaryWriter.Write(
                                    encryptedMessage,
                                    saltLength + iv.Length,
                                    encryptedMessage.Length - saltLength - iv.Length
                                );
                            }
                        }
                        var pt = plainTextStream.ToArray();
                        return pt;
                    }
                }
            }   
        }
    }
}
