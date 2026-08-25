using System.Text;

namespace Fydar.Dev.WebApp.Internal.SvgFavicon;

internal static class WebApplicationExtensions
{
    /// <summary>
    /// Serves the site's mark as an SVG favicon, to browsers that ask for one.
    /// </summary>
    /// <remarks>
    /// The SVG carries a <c>prefers-color-scheme</c> rule, so the icon follows the reader's
    /// theme. Browsers that don't accept SVG fall through to the <c>.ico</c> in <c>wwwroot</c>.
    /// </remarks>
    /// <param name="app">The application to serve the favicon from.</param>
    /// <returns>The application, for chaining.</returns>
    internal static WebApplication UseSvgFavicon(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.Equals("/favicon.ico")
                && context.Request.Headers.Accept.Any(a => a?.Contains("image/svg+xml", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                context.Response.Headers.CacheControl = $"nocache";
                context.Response.ContentType = "image/svg+xml; charset=utf-8";
                context.Request.Headers.Vary = "Accept Accept-Encoding";

                await context.Response.WriteAsync("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"384\" height=\"384\"><path d=\"M233.428 14.412C265.304 14.62 311.987 9.74 363.951 0c1.041 22.757-14.503 60.983-32.854 71.518-18.498 10.618-36.849 13.411-58.89 16.265-22.041 2.853-55.803 2.43-73.355.857l-16.642 85.035c70.296-1.05 79.615-8.081 126.71-22.621-7.71 21.682-20.155 62.374-36.094 78.089-15.938 15.716-49.7 17.965-77.149 17.965l-27.199-.294c-9.792 39.887-21.149 89.511-9.615 114.707-98.535 47.355-99.76 7.98-102.933-10.153-2.344-13.394 17.383-90.831 18.67-100.7-16.323 3.159-33.583 9.04-53.348 16.699 7.663-20.297 20.422-57.416 31.612-72.934 11.19-15.517 25.155-18.997 35.524-20.171l17.855-83.469c-30.21 2.35-63.165 33.548-86.243 50.922 7.507-27.936 10.638-84.046 45.11-112.792C81.662 15.12 120.865 14.02 173.204 14.02l60.224.391z\"/><style>@media (prefers-color-scheme:dark){path{fill:#fff}}</style></svg>", Encoding.UTF8, context.RequestAborted);
                return;
            }
            await next.Invoke();
        });

        return app;
    }
}
