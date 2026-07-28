using Fydar.AspNetCore.CSP.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Fydar.AspNetCore.CSP;

public static class IApplicationBuilderExtensions
{
    public static void UseContentSecurityPolicy(
        this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(AppendHeader, context);

            await next.Invoke();
        });
    }

    private static Task AppendHeader(object httpContextObject)
    {
        var httpContext = (HttpContext)httpContextObject;

        var cspRendering = httpContext.RequestServices.GetRequiredService<CspRendering>();

        var policyBuilder = new StringBuilder();
        foreach (var kvp in cspRendering.PolicyMap)
        {
            if (kvp.Value.Count > 0)
            {
                policyBuilder.Append($"{kvp.Key} ");
                foreach (string value in kvp.Value)
                {
                    policyBuilder.Append(value);
                    policyBuilder.Append(' ');
                }
                policyBuilder.Append("; ");
            }
        }

        httpContext.Response.Headers.Append("Content-Security-Policy", policyBuilder.ToString());

        return Task.CompletedTask;
    }
}
