#region usings

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

#endregion

namespace MySimpleBlockchainWithPoW.Blockchain;

public class Helper
{
    #region Private methods

    private static byte[] ObjectToByteArray(object obj)
    {
        if (obj == null)
            return null;
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
    }

    #endregion

    #region Public methods

    public static string GetSha256Hash(object obj)
    {
        var sha256 = SHA256.Create();
        var hashBuilder = new StringBuilder();

        // zamiana obiektu na tablicę bajtów
        var bytes = ObjectToByteArray(obj);
        // obliczanie hash-a
        var hash = sha256.ComputeHash(bytes);

        // konwersja tablicy bajtów na łańcuch znaków hexadecymalnych
        foreach (var x in hash)
            hashBuilder.Append($"{x:x2}");

        return hashBuilder.ToString();
    }

    public static IConfiguration GetConfigFromFile(string fileName)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(fileName,
                true,
                true);

        return builder.Build();
    }

    #endregion
}