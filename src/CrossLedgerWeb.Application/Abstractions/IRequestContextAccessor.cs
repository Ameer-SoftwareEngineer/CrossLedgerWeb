namespace CrossLedgerWeb.Application.Abstractions;

/// <summary>The caller's IP address and User-Agent for the in-flight request. Kept as an
/// abstraction, implemented in the Api layer from HttpContext, so Application handlers -
/// including this solution's pure-unit-test domain and application layers - never take a
/// dependency on ASP.NET Core directly.</summary>
public interface IRequestContextAccessor
{
    string? IpAddress { get; }
    string? UserAgent { get; }
}
