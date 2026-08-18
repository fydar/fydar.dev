namespace Fydar.Dev.WebApp.Internal.Authentication;

/// <summary>
/// Names and paths shared by the parts of the GitHub sign-in flow.
/// </summary>
internal static class GitHubAuthenticationDefaults
{
    /// <summary>
    /// The authentication scheme that talks to GitHub.</summary>
    public const string AuthenticationScheme = "GitHub";

    /// <summary>
    /// The authorization policy that only the site's owner satisfies.
    /// </summary>
    public const string AuthorizationPolicy = "GitHubOwner";

    /// <summary>
    /// The claim holding the signed in account's GitHub username.
    /// </summary>
    public const string LoginClaimType = "urn:github:login";

    /// <summary>
    /// The claim holding the signed in account's GitHub profile page.
    /// </summary>
    public const string ProfileClaimType = "urn:github:url";

    /// <summary>
    /// Starts the sign-in flow; also where an unauthenticated request is sent.
    /// </summary>
    public const string SignInPath = "/signin/github";

    /// <summary>
    /// Where GitHub returns the reader to once they have approved the sign-in.
    /// </summary>
    public const string CallbackPath = "/signin/github/callback";

    /// <summary>
    /// Ends the session. Only accepts a POST.</summary>
    public const string SignOutPath = "/signout";

    /// <summary>
    /// Where a reader signed in as somebody else is sent.
    /// </summary>
    public const string AccessDeniedPath = "/signin/denied";
}
