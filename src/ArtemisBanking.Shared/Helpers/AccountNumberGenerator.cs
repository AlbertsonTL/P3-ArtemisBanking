using System.Security.Cryptography;

namespace ArtemisBanking.Shared.Helpers;

public static class AccountNumberGenerator
{
    /// <summary>Genera número único de 9 dígitos para cuentas y préstamos.</summary>
    public static string Generate9Digits()
        => RandomNumberGenerator.GetInt32(100_000_000, 1_000_000_000).ToString();

    /// <summary>Genera número único de 16 dígitos para tarjetas.</summary>
    public static string Generate16Digits()
    {
        long p1 = RandomNumberGenerator.GetInt32(10_000_000, 100_000_000);
        long p2 = RandomNumberGenerator.GetInt32(10_000_000, 100_000_000);
        return $"{p1}{p2}";
    }

    /// <summary>Fecha de expiración de tarjeta en formato MM/yy sumando N años.</summary>
    public static string GetExpirationDate(int yearsToAdd = 3)
        => DateTime.UtcNow.AddYears(yearsToAdd).ToString("MM/yy");
}