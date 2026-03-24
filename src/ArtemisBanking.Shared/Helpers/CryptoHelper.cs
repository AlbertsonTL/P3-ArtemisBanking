using System.Security.Cryptography;
using System.Text;

namespace ArtemisBanking.Shared.Helpers;

public static class CryptoHelper
{
    private const string SystemPepper = "ArtemisBanking_Secure_2025_Pepper$";

    public static string HashSHA256(string plainText)
    {
        var saltedText = plainText + SystemPepper;
        var bytes = Encoding.UTF8.GetBytes(saltedText);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static bool VerifySHA256(string plainText, string hash)
        => string.Equals(HashSHA256(plainText), hash, StringComparison.OrdinalIgnoreCase);
}