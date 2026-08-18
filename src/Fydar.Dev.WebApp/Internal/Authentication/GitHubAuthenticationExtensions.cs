using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Fydar.Dev.WebApp.Internal.Authentication;

/// <summary>
/// Wires up signing in with a GitHub account, and the endpoints that drive it.
/// </summary>
internal static class GitHubAuthenticationExtensions
{
    private const string authorizationEndpoint = "https://github.com/login/oauth/authorize";
    private const string tokenEndpoint = "https://github.com/login/oauth/access_token";
    private const string userInformationEndpoint = "https://api.github.com/user";

    /// <summary>
    /// Adds sign-in with GitHub, and the authorization policy guarding owner-only pages.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">Configuration to read the OAuth application from.</param>
    /// <param name="environment">The environment, which decides how strict the cookies are.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddGitHubAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var options = configuration
            .GetSection(GitHubAuthenticationOptions.SectionName)
            .Get<GitHubAuthenticationOptions>() ?? new GitHubAuthenticationOptions();

        services.AddSingleton(options);

        bool requireSecureCookie = !environment.IsDevelopment();

        var authenticationBuilder = services
            .AddAuthentication(authentication =>
            {
                authentication.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authentication.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(cookie =>
            {
                cookie.Cookie.Name = requireSecureCookie ? "__Host-Fydar.Auth" : "Fydar.Auth";
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.Path = "/";
                cookie.Cookie.SecurePolicy = requireSecureCookie
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;

                cookie.Cookie.SameSite = SameSiteMode.Lax;

                cookie.ExpireTimeSpan = TimeSpan.FromDays(14);
                cookie.SlidingExpiration = true;

                cookie.LoginPath = GitHubAuthenticationDefaults.SignInPath;
                cookie.LogoutPath = GitHubAuthenticationDefaults.SignOutPath;
                cookie.AccessDeniedPath = GitHubAuthenticationDefaults.AccessDeniedPath;
                cookie.ReturnUrlParameter = "returnUrl";
            });

        if (options.IsConfigured)
        {
            authenticationBuilder.AddOAuth(GitHubAuthenticationDefaults.AuthenticationScheme, oauth =>
            {
                oauth.ClientId = options.ClientId;
                oauth.ClientSecret = options.ClientSecret;

                oauth.AuthorizationEndpoint = authorizationEndpoint;
                oauth.TokenEndpoint = tokenEndpoint;
                oauth.UserInformationEndpoint = userInformationEndpoint;
                oauth.CallbackPath = GitHubAuthenticationDefaults.CallbackPath;

                oauth.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                oauth.Scope.Clear();
                oauth.SaveTokens = false;

                oauth.UsePkce = false;

                oauth.CorrelationCookie.SameSite = SameSiteMode.Lax;
                oauth.CorrelationCookie.SecurePolicy = requireSecureCookie
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;

                oauth.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                oauth.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
                oauth.ClaimActions.MapJsonKey(GitHubAuthenticationDefaults.LoginClaimType, "login");
                oauth.ClaimActions.MapJsonKey(GitHubAuthenticationDefaults.ProfileClaimType, "html_url");

                oauth.Events.OnCreatingTicket = OnCreatingTicketAsync;
                oauth.Events.OnRemoteFailure = OnRemoteFailure;
            });
        }

        services.AddAuthorizationBuilder()
            .AddPolicy(
                GitHubAuthenticationDefaults.AuthorizationPolicy,
                policy => policy
                    .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context => options.IsAuthorized(context.User)));

        services.AddCascadingAuthenticationState();

        return services;
    }

    /// <summary>
    /// Maps the endpoints that start and end a session.
    /// </summary>
    /// <param name="app">The application to map the endpoints on.</param>
    /// <returns>The application, for chaining.</returns>
    public static WebApplication MapGitHubAuthenticationEndpoints(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<GitHubAuthenticationOptions>();

        if (!options.IsConfigured)
        {
            app.Logger.LogWarning(
                "Signing in with GitHub is unconfigured, so the pages behind it are closed. Set {ClientIdKey} and {ClientSecretKey}.",
                $"{GitHubAuthenticationOptions.SectionName}:{nameof(GitHubAuthenticationOptions.ClientId)}",
                $"{GitHubAuthenticationOptions.SectionName}:{nameof(GitHubAuthenticationOptions.ClientSecret)}");
        }

        app.MapGet(GitHubAuthenticationDefaults.SignInPath, (
            GitHubAuthenticationOptions signInOptions,
            string? returnUrl) =>
        {
            if (!signInOptions.IsConfigured)
            {
                return Results.Problem(
                    title: "Signing in is unavailable.",
                    detail: "This deployment has no GitHub OAuth application configured.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = LocalReturnUrl(returnUrl) },
                [GitHubAuthenticationDefaults.AuthenticationScheme]);
        });

        app.MapPost(GitHubAuthenticationDefaults.SignOutPath, async (
            HttpContext httpContext,
            IAntiforgery antiforgery) =>
        {
            if (!await antiforgery.IsRequestValidAsync(httpContext))
            {
                return Results.BadRequest();
            }

            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Results.LocalRedirect("/");
        });

        return app;
    }

    /// <summary>
    /// Reads the account that authorized us, since the token response says nothing about who
    /// the reader is.
    /// </summary>
    private static async Task OnCreatingTicketAsync(OAuthCreatingTicketContext context)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

        request.Headers.UserAgent.ParseAdd("fydar.dev");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        using var response = await context.Backchannel.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            context.HttpContext.RequestAborted);

        response.EnsureSuccessStatusCode();

        using var user = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));

        context.RunClaimActions(user.RootElement);
    }

    /// <summary>
    /// Turns a refused or abandoned sign-in into the access denied page, rather than an
    /// unhandled exception and a 500.
    /// </summary>
    private static Task OnRemoteFailure(RemoteFailureContext context)
    {
        context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(GitHubAuthenticationExtensions))
            .LogInformation(context.Failure, "Signing in with GitHub did not complete.");

        context.Response.Redirect(GitHubAuthenticationDefaults.AccessDeniedPath);
        context.HandleResponse();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reduces a requested return URL to one that stays on this site.
    /// </summary>
    /// <remarks>
    /// The parameter arrives from the query string, so an absolute or protocol relative URL
    /// would turn signing in into an open redirect that borrows this site's name.
    /// </remarks>
    private static string LocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl)
            || !returnUrl.StartsWith('/')
            || returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.StartsWith("/\\", StringComparison.Ordinal))
        {
            return "/";
        }

        return returnUrl;
    }
}
