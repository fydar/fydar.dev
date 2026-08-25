namespace Fydar.Dev.WebApp.Internal.ServiceWorkers;

internal static class WebApplicationExtensions
{
    /// <summary>
    /// Widens the scope a service worker is allowed to claim, so that the games under
    /// <c>/play/</c> can register one that was served from a folder beneath it.
    /// </summary>
    /// <remarks>
    /// A service worker may only control the folder it was served from, unless the response
    /// carrying it says otherwise.
    /// </remarks>
    /// <param name="app">The application serving the service workers.</param>
    /// <returns>The application, for chaining.</returns>
    internal static WebApplication UseServiceWorkerScope(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.Value?.EndsWith("/ServiceWorker.js", StringComparison.OrdinalIgnoreCase) == true
                || context.Request.Path.Value?.EndsWith(".serviceworker.js", StringComparison.OrdinalIgnoreCase) == true)
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["Service-Worker-Allowed"] = "/play/";
                    return Task.CompletedTask;
                });
            }
            await next.Invoke();
        });

        return app;
    }
}
