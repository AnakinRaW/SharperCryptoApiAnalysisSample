using System.Security.Cryptography;
using System.Text;

namespace FalseCrypt.Crypto
{
    public static class WeakPasswordDerivation
    {
        public static string StringToHash(string input)
        {
            // Bug 6: Using a weak hashing provider
            // Bug 7: Not disposing IDisposable
            if (input == null)
                return null;
            var hashProvider = new MD5CryptoServiceProvider();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hash = hashProvider.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach (var t in hash)
                sb.Append(t.ToString("X2"));
            return sb.ToString();
        }


        public static PasswordDeriveData DerivePassword(string password)
        {
            var salt = WeakKeyGenerator.GenerateSalt();
            // Bug 8: Iteration count is too low
            // Bug 9: Salt size is constant 8 bytes long and hence to short
            // Bug 10: Not disposing IDisposable
            // Bug 11: Password as a string allows memory dump attacks
            var generator = new Rfc2898DeriveBytes(password, salt, WeakCryptoConfig.IterationCount);

            var cryptKey = generator.GetBytes(WeakCryptoConfig.KeySizeBytes);
            return new PasswordDeriveData(cryptKey, salt);
        }
    }
}
