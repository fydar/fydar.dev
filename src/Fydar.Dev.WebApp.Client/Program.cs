using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var hostBuilder = WebAssemblyHostBuilder.CreateDefault(args);

        var host = hostBuilder.Build();

        await host.RunAsync();
    }
}
