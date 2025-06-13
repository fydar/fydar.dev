using Fydar.Dev.WebApp.Toolkit.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Fydar.Dev.WebApp.Internal;

internal static class IEndpointRouteBuilderExtensions
{
	public static RouteHandlerBuilder MapIconLibrary<TIconLibrary>(this IEndpointRouteBuilder endpoints, string pattern)
		where TIconLibrary : IconLibrary
	{
		return endpoints.MapGet(pattern, async (HttpContext httpContext, [FromServices] HtmlRenderer htmlRenderer) =>
		{
			httpContext.Response.Headers.CacheControl = $"public,max-age={TimeSpan.FromHours(24).TotalSeconds}";

			return await htmlRenderer.Dispatcher.InvokeAsync(async () =>
			{
				// Pass the parameters and render the component
				var html = await htmlRenderer.RenderComponentAsync<TIconLibrary>(ParameterView.Empty);

				return Results.Content(html.ToHtmlString(), "image/svg+xml; charset=utf-8", Encoding.UTF8);
			});
		});
	}
}
