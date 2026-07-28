using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fydar.AspNetCore.Socials;

public static class IApplicationBuilderExtensions
{
    public static RouteHandlerBuilder MapSocialRedirect(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        SocialRedirectPolicy options)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGet(pattern, async (HttpContext httpContext, [FromServices] HtmlRenderer htmlRenderer) =>
        {
            // If there is metadata, we will want to generate a page for bots.
            if (options.Metadata != null)
            {
                string userAgent = httpContext.Request.Headers.UserAgent.ToString();

                string[] bots = ["Discordbot", "Twitterbot", "facebookexternalhit", "Slackbot", "LinkedInBot", "TelegramBot"];

                // Check if the request is coming from a bot
                bool isBot = bots.Any(bot => userAgent.Contains(bot, StringComparison.OrdinalIgnoreCase));

                if (isBot)
                {
                    return await htmlRenderer.Dispatcher.InvokeAsync(async () =>
                    {
                        // Pass the parameters and render the component
                        var html = await htmlRenderer.RenderComponentAsync<RedirectPage>(ParameterView.FromDictionary(new Dictionary<string, object?>()
                        {
                            ["Redirect"] = options
                        }));

                        return Results.Content(html.ToHtmlString(), "text/html; charset=utf-8", Encoding.UTF8);
                    });
                }
            }
            return Results.Redirect(options.Destination, true);
        });
    }
}
