namespace Fydar.Dev.WebApp.Internal.UnityFiles;

internal static class WebApplicationExtensions
{
    internal static WebApplication UseStaticUnityFiles(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.Value?.EndsWith(".br", StringComparison.OrdinalIgnoreCase) == true)
            {
                context.Response.OnStarting(() =>
                {
                    if (context.Response.StatusCode == StatusCodes.Status200OK)
                    {
                        context.Response.Headers.ContentEncoding = "br";
                    }
                    return Task.CompletedTask;
                });
            }
            await next(context);
        });

        return app;
    }
}
