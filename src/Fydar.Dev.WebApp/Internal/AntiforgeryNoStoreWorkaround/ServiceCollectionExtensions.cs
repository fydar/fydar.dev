using Microsoft.AspNetCore.Antiforgery;

namespace Fydar.Dev.WebApp.Internal.AntiforgeryNoStoreWorkaround;

internal static class ServiceCollectionExtensions
{
	/// <summary>
	/// <para>Should be added <b>after</b> <c>AddAntiforgery</c>.</para>
	/// </summary>
	public static IServiceCollection RemoveAntiforgeryNoStore(this IServiceCollection serviceCollection)
	{
		for (int i = serviceCollection.Count - 1; i >= 0; i--)
		{
			var serviceDescriptor = serviceCollection[i];

			if (serviceDescriptor.ServiceType == typeof(IAntiforgery))
			{
				serviceCollection.RemoveAt(i);
				serviceCollection.AddKeyedSingleton(typeof(IAntiforgery), "DefaultAntiforgery", serviceDescriptor.ImplementationType!);
			}
		}

		serviceCollection.AddSingleton<IAntiforgery, AntiforgeryWrapper>();
		return serviceCollection;
	}
}
