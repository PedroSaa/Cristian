using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace DocFlow.Api.Middleware;

/// <summary>
/// CSRF protection via Double Submit Cookie pattern.
///
/// - Safe methods (GET, HEAD, OPTIONS, TRACE): set the XSRF-TOKEN cookie if missing, then pass.
/// - Mutations: validate that the X-CSRF-TOKEN header matches the XSRF-TOKEN cookie value.
/// - Backward-compat fallback: if no cookie exists yet, accept X-Requested-With presence.
/// </summary>
public class CsrfProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private const string CsrfCookieName = "XSRF-TOKEN";
    private const string CsrfHeaderName = "X-CSRF-TOKEN";
    private const string RequestedWithHeader = "X-Requested-With";

    public CsrfProtectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // ── Exempt safe HTTP methods ───────────────────────────────────────
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method) ||
            HttpMethods.IsTrace(context.Request.Method))
        {
            SetCsrfCookieIfMissing(context);
            await _next(context);
            return;
        }

        // ── Exempt auth bootstrap endpoints and OIDC callback ──────────────
        if (context.Request.Path.StartsWithSegments("/api/auth/login") ||
            context.Request.Path.StartsWithSegments("/api/auth/refresh") ||
            context.Request.Path.StartsWithSegments("/api/auth/claveunica/callback"))
        {
            await _next(context);
            return;
        }

        // ── Exempt OnlyOffice editor callback ──────────────────────────────
        // Lo invoca el Document Server (servidor externo, sin cabeceras CSRF). Se
        // autentica con su propio JWT (validado en el controller con OnlyOffice:Secret).
        if ((context.Request.Path.Value ?? string.Empty)
                .EndsWith("/editor-callback", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // ── Validate Double Submit Cookie ──────────────────────────────────
        var csrfCookie = context.Request.Cookies[CsrfCookieName];
        var csrfHeader = context.Request.Headers[CsrfHeaderName].FirstOrDefault();

        if (!string.IsNullOrEmpty(csrfCookie))
        {
            // Cookie exists → header MUST match (constant-time comparison)
            if (string.IsNullOrEmpty(csrfHeader) ||
                !CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(csrfCookie),
                    System.Text.Encoding.UTF8.GetBytes(csrfHeader)))
            {
                // Sanar en el servidor: reemitimos una cookie nueva y limpia EN esta misma
                // respuesta 400. Así el reintento del cliente lee la cookie fresca y su
                // X-CSRF-TOKEN coincide sin intervención manual — cubre el caso de una
                // cookie vieja/corrupta (p. ej. de antes del fix base64url) que el borrado
                // del lado del cliente no lograba regenerar de forma confiable.
                AppendFreshCsrfCookie(context);

                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new
                {
                    mensaje = "Error de seguridad: el token de protección no es válido.",
                    // Señal estable (no texto traducido) para que el front pueda
                    // auto-recuperarse: regenerar la cookie y reintentar una vez.
                    codigo = "CSRF_TOKEN_INVALID"
                });
                return;
            }
        }
        else
        {
            // No cookie yet → backward-compat: accept X-Requested-With presence
            // Also set the cookie so future requests get full protection
            SetCsrfCookieIfMissing(context);

            if (!context.Request.Headers.ContainsKey(RequestedWithHeader))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new
                {
                    mensaje = "Error de seguridad: faltan cabeceras de seguridad requeridas.",
                    codigo = "CSRF_MISSING_HEADERS"
                });
                return;
            }
        }

        await _next(context);
    }

    private static void SetCsrfCookieIfMissing(HttpContext context)
    {
        if (context.Request.Cookies.ContainsKey(CsrfCookieName))
            return;

        AppendFreshCsrfCookie(context);
    }

    /// <summary>
    /// Emite (reemplaza) la cookie XSRF-TOKEN con un token nuevo. Se usa tanto para
    /// sembrarla cuando falta como para sanarla al rechazar una petición por token inválido.
    /// </summary>
    private static void AppendFreshCsrfCookie(HttpContext context)
    {
        // URL-safe token (base64url, no padding): avoids '+', '/', '=' which ASP.NET
        // URL-encodes in the cookie and break the document.cookie ↔ X-CSRF-TOKEN round-trip.
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        context.Response.Cookies.Append(CsrfCookieName, token, new CookieOptions
        {
            HttpOnly = false,    // JS reads it → sends as X-CSRF-TOKEN header
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            MaxAge = TimeSpan.FromHours(8) // matches access_token cookie TTL
        });
    }
}
