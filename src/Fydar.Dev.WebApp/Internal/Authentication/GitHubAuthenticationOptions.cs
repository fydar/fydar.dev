using System.Security.Claims;

namespace Fydar.Dev.WebApp.Internal.Authentication;

/// <summary>
/// The GitHub OAuth application backing sign-in, bound from the "Authentication:GitHub" configuration section.
/// </summary>
internal sealed class GitHubAuthenticationOptions
{
    /// <summary>
    /// The configuration section these options are bound from.
    /// </summary>
    public const string SectionName = "Authentication:GitHub";

    /// <summary>
    /// The OAuth application's client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth application's client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The GitHub usernames allowed past the authorization policy.
    /// </summary>
    public string[] AuthorizedLogins { get; set; } = [];

    /// <summary>
    /// Whether the OAuth application has been configured. Without it the sign-in endpoint
    /// reports itself unavailable instead of starting a flow that cannot complete.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);

    /// <summary>
    /// Determines whether a signed in account is one of the <see cref="AuthorizedLogins"/>.
    /// </summary>
    /// <param name="user">The signed in account.</param>
    /// <returns><see langword="true"/> if the account may see owner-only pages.</returns>
    public bool IsAuthorized(ClaimsPrincipal user)
    {
        string? login = user.FindFirstValue(GitHubAuthenticationDefaults.LoginClaimType);

        if (string.IsNullOrEmpty(login))
        {
            return false;
        }

        return AuthorizedLogins.Contains(login, StringComparer.OrdinalIgnoreCase);
    }
}
