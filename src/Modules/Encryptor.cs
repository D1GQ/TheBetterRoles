using System.Security.Cryptography;
using System.Text;

namespace TheBetterRoles.Modules;

class Encryptor
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("FEDCBA9876543210FEDCBA9876543210");
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("1234567890ABCDEF");

    internal static string Encrypt(string input)
    {
        using Aes aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;

        using MemoryStream memoryStream = new();
        using CryptoStream cryptoStream = new(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using (StreamWriter streamWriter = new(cryptoStream))
        {
            streamWriter.Write(input);
        }
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    internal static string Decrypt(string input)
    {
        using Aes aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;

        using MemoryStream memoryStream = new(Convert.FromBase64String(input));
        using CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using StreamReader streamReader = new(cryptoStream);
        return streamReader.ReadToEnd();
    }
}
