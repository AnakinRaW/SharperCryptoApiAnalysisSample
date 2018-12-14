﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SharperCryptoApiAnalysisSample
{
    /*
    * This class is based on https://gist.github.com/4336842
    * http://creativecommons.org/publicdomain/mark/1.0/ 
    */
	public static class SymmetricCryptoProvider
	{

		//Preconfigured Encryption Parameters
        public static readonly int BlockBitSize = 128;
        public static readonly int KeyBitSize = 256;

        ﻿		//Preconfigured Password Key Derivation Parameters
		public static readonly int SaltBitSize = 512;
		public static readonly int Iterations = 10000;

		﻿        /// <summary>
        /// Encrypts a file with a password. The password get salted. The file will be rewritten. The first bytes of the encrypted file contains the used salt and IV.
        /// </summary>
        /// <param name="file">The file to encrypt</param>
        /// <param name="password">The password </param>
        public static void SimpleEncryptFileWithPassword(FileInfo file, string password)
        {
            if (file == null || !file.Exists)
                throw new FileNotFoundException();

            var bytes = File.ReadAllBytes(file.FullName);

            byte[] cipherText;
            using (var fileStream = new FileStream(file.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                cipherText = SimpleEncryptWithPassword(bytes, password);
                fileStream.Write(cipherText, 0, cipherText.Length);
            }
        }

        /// <summary>
        /// Decrypts a file with a password. Salt and IV are extracted from the file cipher.
        /// </summary>
        /// <param name="file">The file to decrypt</param>
        /// <param name="password">The password</param>
        public static void SimpleDecryptFileWithPassword(FileInfo file, string password)
        {
            if (file == null || !file.Exists)
                throw new FileNotFoundException();

            var bytes = File.ReadAllBytes(file.FullName);
     
            using (var fileStream =
                new FileStream(file.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                var m = SimpleDecryptWithPassword(bytes, password);
                fileStream.Write(m, 0, m.Length);
            }
        }

        /// <summary>
        /// Encrypts a message with a password.
        /// </summary>
        /// <param name="secretMessage">The message to encrypt</param>
        /// <param name="password">The password</param>
        /// <param name="encoding">The encoding of the message</param>
        /// <returns>The encrypted message converted to a Base64 string</returns>
        public static string SimpleEncryptWithPassword(string secretMessage, string password, Encoding encoding)
        {
            if (string.IsNullOrEmpty(secretMessage))
                throw new ArgumentException("Secret Message Required!", nameof(secretMessage));

            var plainText = encoding.GetBytes(secretMessage);
            var cipherText = SimpleEncryptWithPassword(plainText, password);
            return Convert.ToBase64String(cipherText);
        }

        /// <summary>
        /// Encrypts a message with a password.
        /// </summary>
        /// <param name="secretMessage">The message to encrypt</param>
        /// <param name="password">The password</param>
        /// <returns>The encrypted message</returns>
        public static byte[] SimpleEncryptWithPassword(byte[] secretMessage, string password)
        {
            var nonSecretPayload = new byte[] { };
            var payload = new byte[SaltBitSize / 8];
            Array.Copy(nonSecretPayload, payload, nonSecretPayload.Length);
            var payloadIndex = nonSecretPayload.Length;

            byte[] cryptKey;
            using (var generator = new Rfc2898DeriveBytes(password, SaltBitSize / 8, Iterations, HashAlgorithmName.SHA512))
            {
                var salt = generator.Salt;
                cryptKey = generator.GetBytes(KeyBitSize / 8);
                Array.Copy(salt, 0, payload, payloadIndex, salt.Length);
            }
            return SimpleEncrypt(secretMessage, cryptKey, payload);
        }

        /// <summary>
        /// Decrypts a message with a password
        /// </summary>
        /// <param name="encryptedMessage">The encrypted message</param>
        /// <param name="password">The password</param>
        /// <param name="encoding">The encoding of the decrypted message</param>
        /// <returns>The decrypted message</returns>
        public static string SimpleDecryptWithPassword(string encryptedMessage, string password, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(encryptedMessage))
                throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));

            var cipherText = Convert.FromBase64String(encryptedMessage);
            var plainText = SimpleDecryptWithPassword(cipherText, password);
            return plainText == null ? null : Encoding.UTF8.GetString(plainText);
        }

        /// <summary>
        /// Decrypts a message with a password
        /// </summary>
        /// <param name="encryptedMessage">The encrypted message</param>
        /// <param name="password">The password</param>
        /// <returns>The decrypted message</returns>
        public static byte[] SimpleDecryptWithPassword(byte[] encryptedMessage, string password)
        {
            if (encryptedMessage == null || encryptedMessage.Length == 0)
                throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));

            var cryptSalt = new byte[SaltBitSize / 8];

            //Grab Salt from Non-Secret Payload
            Array.Copy(encryptedMessage, 0, cryptSalt, 0, cryptSalt.Length);

            byte[] cryptKey;

            //Generate crypt key
            using (var generator = new Rfc2898DeriveBytes(password, cryptSalt, Iterations, HashAlgorithmName.SHA512))
            {
                cryptKey = generator.GetBytes(KeyBitSize / 8);
            }

            return SimpleDecrypt(encryptedMessage, cryptKey, cryptSalt.Length);
        }


		/// <summary>
        /// Encrypts a message with a given secret key.
        /// </summary>
        /// <param name="secretMessage">The secret message</param>
        /// <param name="secretKey">The secret key</param>
        /// <param name="encoding">The encoding of the secret message</param>
        /// <returns>The encrypted message in a Base64 string</returns>
        public static string EncryptMessage(string secretMessage, byte[] secretKey, Encoding encoding)
        {
            var plainText = encoding.GetBytes(secretMessage);
            var cipherText = EncryptMessage(plainText, secretKey);
            return Convert.ToBase64String(cipherText);
        }

        /// <summary>
        /// Encrypts a message with a given secret key.
        /// </summary>
        /// <param name="secretMessage">The secret message</param>
        /// <param name="secretKey">The secret key</param>
        /// <returns>The encrypted message</returns>
        public static byte[] EncryptMessage(byte[] secretMessage, byte[] secretKey)
        {
            if (secretMessage == null || secretMessage.Length == 0)
                throw new ArgumentException("Secret Message Required!", nameof(secretMessage));

            if (secretKey == null || secretKey.Length == 0)
                throw new ArgumentException("Secret Key Required!", nameof(secretKey));

            return SimpleEncrypt(secretMessage, secretKey, null);
        }


        /// <summary>
        /// Decrypts a message with a given secret key.
        /// </summary>
        /// <param name="encryptedMessage">The encrypted message.</param>
        /// <param name="secretKey">The secret key</param>
        /// <returns>The decrypted message</returns>
        public static byte[] DecryptMessage(byte[] encryptedMessage, byte[] secretKey)
        {
            if (encryptedMessage == null || encryptedMessage.Length == 0)
                throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));

            return SimpleDecrypt(encryptedMessage, secretKey, 0);
        }

        /// <summary>
        /// Decrypts a message with a given secret key.
        /// </summary>
        /// <param name="encryptedMessage">The encrypted message in Base64 format</param>
        /// <param name="secretKey">The secret key</param>
        /// <param name="encoding">The encoding of the decrypted message</param>
        /// <returns>The decrypted message</returns>
        public static string DecryptMessage(string encryptedMessage, byte[] secretKey, Encoding encoding)
        {
            var cipherText = Convert.FromBase64String(encryptedMessage);
            var plainText = DecryptMessage(cipherText, secretKey);
            return plainText == null ? null : encoding.GetString(plainText);
        }


		private static byte[] SimpleEncrypt(byte[] secretMessage, byte[] cryptKey, byte[] nonSecretPayload)
        {
            //User Error Checks
            if (cryptKey == null || cryptKey.Length != KeyBitSize / 8)
                throw new ArgumentException($"Key needs to be {KeyBitSize} bit!", nameof(cryptKey));

            if (secretMessage == null || secretMessage.Length < 1)
                throw new ArgumentException("Secret Message Required!", nameof(secretMessage));

            //non-secret payload optional
            nonSecretPayload = nonSecretPayload ?? new byte[] { };

            byte[] cipherText;
            byte[] iv;

            using (var aes = new AesCryptoServiceProvider
            {
                KeySize = KeyBitSize,
                BlockSize = BlockBitSize,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            })
            {

                //Use random IV
                aes.GenerateIV();
                iv = aes.IV;

                using (var encrypter = aes.CreateEncryptor(cryptKey, iv))
                {
                    using (var cipherStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(cipherStream, encrypter, CryptoStreamMode.Write))
                        {
                            using (var binaryWriter = new BinaryWriter(cryptoStream))
                                binaryWriter.Write(secretMessage);
                        }
                        cipherText = cipherStream.ToArray();
                    }
                }

                using (var encryptedStream = new MemoryStream())
                {
                    using (var binaryWriter = new BinaryWriter(encryptedStream))
                    {
                        binaryWriter.Write(nonSecretPayload);
                        //Prepend IV
                        binaryWriter.Write(iv);
                        binaryWriter.Write(cipherText);
                        binaryWriter.Flush();
                    }
                    return encryptedStream.ToArray();
                }
            }
        }

        private static byte[] SimpleDecrypt(byte[] encryptedMessage, byte[] cryptKey, int nonSecretPayloadLength)
        {

            //Basic Usage Error Checks
            if (cryptKey == null || cryptKey.Length != KeyBitSize / 8)
                throw new ArgumentException($"CryptKey needs to be {KeyBitSize} bit!", nameof(cryptKey));

            if (encryptedMessage == null || encryptedMessage.Length == 0)
                throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));


            var ivLength = (BlockBitSize / 8);

            using (var aes = new AesCryptoServiceProvider
            {
                KeySize = KeyBitSize,
                BlockSize = BlockBitSize,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            })
            {

                //Grab IV from message
                var iv = new byte[ivLength];
                Array.Copy(encryptedMessage, nonSecretPayloadLength, iv, 0, iv.Length);

                using (var decrypter = aes.CreateDecryptor(cryptKey, iv))
                using (var plainTextStream = new MemoryStream())
                {
                    using (var decrypterStream = new CryptoStream(plainTextStream, decrypter, CryptoStreamMode.Write))
                    using (var binaryWriter = new BinaryWriter(decrypterStream))
                    {
                        //Decrypt Cipher Text from Message
                        binaryWriter.Write(
                            encryptedMessage,
                            nonSecretPayloadLength + iv.Length,
                            encryptedMessage.Length - nonSecretPayloadLength - iv.Length
                        );
                    }
                    //Return Plain Text

                    var pt = plainTextStream.ToArray();
                    return pt;
                }
            }
        }
	}
}