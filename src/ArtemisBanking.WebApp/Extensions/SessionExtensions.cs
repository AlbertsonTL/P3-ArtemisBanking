namespace ArtemisBanking.WebApp.Extensions;

public static class SessionExtensions
{
    public static void SetDecimal(this ISession session, string key, decimal value)
    {
        session.SetString(key, value.ToString());
    }

    public static decimal GetDecimal(this ISession session, string key)
    {
        var value = session.GetString(key);
        if (string.IsNullOrEmpty(value))
            return 0m;
        
        return decimal.TryParse(value, out var result) ? result : 0m;
    }

    public static void SetInt(this ISession session, string key, int value)
    {
        session.SetString(key, value.ToString());
    }

    public static int GetInt(this ISession session, string key)
    {
        var value = session.GetString(key);
        if (string.IsNullOrEmpty(value))
            return 0;
        
        return int.TryParse(value, out var result) ? result : 0;
    }
}