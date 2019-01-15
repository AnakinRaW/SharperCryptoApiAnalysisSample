using System;

namespace FalseCrypt.Crypto
{
    public static class WeakKeyGenerator
    {
        // Bug 3: Constant seed makes it attackers easy to recreate the pseudo random sequence
        private const int SecretSeed = 852641973; 

        // BUG 4: A property with object initializer means that with each access a new instance of random with the same seed will be created. 
        private static Random RandomGenerator => new Random(SecretSeed);

        public static byte[] GenerateSalt()
        {
            var byteArray = new byte[WeakCryptoConfig.SaltSizeBytes]; 
            // BUG 5: System.Random is not cryptographically secure
            RandomGenerator.NextBytes(byteArray);
            return byteArray;
        } 
    }
}
