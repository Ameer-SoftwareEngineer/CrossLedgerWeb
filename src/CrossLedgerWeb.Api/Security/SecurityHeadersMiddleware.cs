namespace CrossLedgerWeb.Api.Security;

/// <summary>Response headers with no equivalent anywhere else in the pipeline - unlike
/// CORS, auth and rate limiting, nothing fails loudly if these are missing, so they are
/// easy to simply never add. Applied to every response, API and Swagger UI alike.</summary>
public static class SecurityHeadersMiddleware
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool isDevelopment)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";

            // HSTS only makes sense once the app is actually served over HTTPS end-to-end -
            // in local dev, Kestrel's dev certificate makes browsers remember this host as
            // HTTPS-only for a year, which then breaks plain http://localhost testing.
            if (!isDevelopment)
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            // default-src 'none' is safe for a pure JSON API. Swagger UI (dev-only, served
            // from this same app) needs inline scripts/styles, so skip CSP while it's active
            // rather than widen the policy for every other response.
            if (!isDevelopment)
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            await next();
        });
    }
}
