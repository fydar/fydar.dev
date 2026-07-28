using Fydar.AspNetCore.CSP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Fydar.AspNetCore.CSP;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Adds content security policy services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddContentSecurityPolicy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAdd(ServiceDescriptor.Scoped<CspRendering, CspRendering>());

        return services;
    }

    /// <summary>
    /// Adds content security policy services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An <see cref="Action{CspOptions}"/> to configure the provided <see cref="CspOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddContentSecurityPolicy(this IServiceCollection services, Action<CspOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(setupAction);

        services.AddContentSecurityPolicy();
        services.Configure(setupAction);

        return services;
    }
}
