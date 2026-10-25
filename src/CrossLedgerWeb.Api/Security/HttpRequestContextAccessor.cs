using CrossLedgerWeb.Application.Abstractions;

namespace CrossLedgerWeb.Api.Security;

public sealed class HttpRequestContextAccessor : IRequestContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpRequestContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
