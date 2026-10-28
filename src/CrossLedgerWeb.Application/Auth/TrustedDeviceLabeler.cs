namespace CrossLedgerWeb.Application.Auth;

/// <summary>Turns a raw User-Agent header into something a user can recognise on their
/// Security page ("Chrome on Windows") well enough to tell their devices apart and
/// decide what to revoke - not a full UA-parsing library, just enough for display.</summary>
public static class TrustedDeviceLabeler
{
    public static string Describe(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown device";

        var browser = userAgent switch
        {
            _ when userAgent.Contains("Edg/") => "Edge",
            _ when userAgent.Contains("OPR/") || userAgent.Contains("Opera") => "Opera",
            _ when userAgent.Contains("Chrome") => "Chrome",
            _ when userAgent.Contains("CriOS") => "Chrome",
            _ when userAgent.Contains("Firefox") => "Firefox",
            _ when userAgent.Contains("Safari") => "Safari",
            _ => "a browser",
        };

        var os = userAgent switch
        {
            _ when userAgent.Contains("Windows") => "Windows",
            _ when userAgent.Contains("Mac OS X") || userAgent.Contains("Macintosh") => "macOS",
            _ when userAgent.Contains("iPhone") || userAgent.Contains("iPad") => "iOS",
            _ when userAgent.Contains("Android") => "Android",
            _ when userAgent.Contains("Linux") => "Linux",
            _ => "an unknown OS",
        };

        return $"{browser} on {os}";
    }
}
