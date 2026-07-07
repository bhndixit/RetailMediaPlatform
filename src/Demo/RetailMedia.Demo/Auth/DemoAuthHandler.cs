using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace RetailMedia.Demo.Auth;

/// <summary>
/// Authentication handler for local demo use only.
/// Every request is automatically authenticated as a demo user.
/// In production this is replaced by the OAuth2 / OpenID Connect scheme.
/// </summary>
internal sealed class DemoAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "demo-user"),
            new Claim(ClaimTypes.NameIdentifier, "demo-user-id"),
            new Claim("scope", "retail-media.full")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
