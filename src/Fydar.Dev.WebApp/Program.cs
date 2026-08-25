using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;
using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;
using Amazon.SimpleEmail;
using Fydar.AspNetCore.CSP;
using Fydar.Dev.Services.EmailTickets;
using Fydar.Dev.WebApp.Client.Components.Pages;
using Fydar.Dev.WebApp.Components;
using Fydar.Dev.WebApp.Components.Iconography;
using Fydar.Dev.WebApp.Internal;
using Fydar.Dev.WebApp.Internal.AntiforgeryNoStoreWorkaround;
using Fydar.Dev.WebApp.Internal.Authentication;
using Fydar.Dev.WebApp.Internal.ServiceWorkers;
using Fydar.Dev.WebApp.Internal.SvgFavicon;
using Fydar.Dev.WebApp.Internal.UnityFiles;
using Fydar.Dev.WebApp.Toolkit.Icons;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Net.Http.Headers;
using MimeKit;
using Serilog;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Fydar.Dev.WebApp;

public class Program
{
    /// <summary>
    /// Runs the web host until the process is asked to stop.
    /// </summary>
    /// <param name="args">The command line the process was started with.</param>
    /// <returns>The exit code for the process.</returns>
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.ColoredConsole()
            .CreateBootstrapLogger();

        try
        {
            var host = await CreateHostAsync(args);

            await host.StartAsync();

            var server = host.Services.GetRequiredService<IServer>();
            var addresses = server.Features.GetRequiredFeature<IServerAddressesFeature>().Addresses;

            Log.Information($"Web host started listening on '{string.Join("', '", addresses)}'.");

            await host.WaitForShutdownAsync();

            return 0;
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Host terminated unexpectedly.");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Builds the host, its services, and the pipeline that requests travel through.
    /// </summary>
    /// <param name="args">The command line the process was started with.</param>
    /// <returns>The host, ready to be started.</returns>
    public static async Task<IHost> CreateHostAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables("CONFIG_");

        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services));

        builder.WebHost.UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "True");

        // A deployment names the ACM certificate its HTTPS endpoints serve. Without one Kestrel
        // keeps its own default, which is the ASP.NET Core development certificate.
        string? certificateArn = builder.Configuration["CertificateArn"];
        if (!string.IsNullOrEmpty(certificateArn))
        {
            var certificate = await ExportAcmCertificateAsync(builder.Configuration, certificateArn);

            builder.WebHost.ConfigureKestrel(kestrel =>
            {
                kestrel.ConfigureHttpsDefaults(https =>
                {
                    https.ServerCertificate = certificate;
                });
            });
        }

        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        // Add services to the container.
        builder.Services.AddHealthChecks();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services.AddContentSecurityPolicy(options =>
        {
            options.SupplyHeader = true;
        });

        builder.Services.AddScoped<Noncer>();
        builder.Services.AddScoped<HeadFragmentRegistry>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddAntiforgery();
        builder.Services.RemoveAntiforgeryNoStore();

        builder.Services.AddGitHubAuthentication(builder.Configuration, builder.Environment);

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;

            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes
                .Concat(["image/svg+xml"]);
        });

        builder.Services.Configure<HstsOptions>(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
            options.Preload = true;
        });

        builder.Services.Configure<HttpsRedirectionOptions>(opts =>
        {
            opts.RedirectStatusCode = (int)HttpStatusCode.PermanentRedirect;
        });

        // Rendering a component builds its base URI from the request's host, so a request without
        // one (port scanners send HTTP/1.0 with no Host header) produces 'http:///' and throws.
        // Reject it up front, as a request with no host is malformed regardless.
        builder.Services.Configure<HostFilteringOptions>(options =>
        {
            options.AllowEmptyHosts = false;
        });

        // The error pages are components, and rendering one re-enters the components pipeline in
        // the same request. Without a fresh scope the second render finds the scoped
        // NavigationManager already initialized by the first and throws.
        builder.Services.Configure<StatusCodePagesOptions>(options =>
        {
            options.CreateScopeForStatusCodePages = true;
        });

        builder.Services.Configure<CachedEmailReaderServiceConfiguration>(options =>
        {
            options.Expiration = TimeSpan.FromHours(1);
            options.ListingExpiration = TimeSpan.FromSeconds(30);
        });

        builder.Services.Configure<S3EmailReaderServiceConfiguration>(options =>
        {
            options.Bucket = "fydar.dev-inbound-email";
        });

        builder.Services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 32 * 1024 * 1024;
        });

        if (!builder.Environment.IsDevelopment())
        {
            builder.Services.Configure<StaticFileOptions>(opts =>
            {
                opts.OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] = $"public,max-age=31536000";
                };
            });
        }

        builder.Services.AddSingleton<IHybridCacheSerializer<MimeMessage>, MimeMessageHybridCacheSerializer>();

        builder.Services.AddSingleton<S3EmailReaderService>();
        builder.Services.AddSingleton<IEmailReaderService, CachedEmailReaderService>();
        builder.Services.AddSingleton<TicketHtmlSanitizer>();
        builder.Services.AddScoped<HtmlRenderer>();

        builder.Services.AddScoped<IContactSubmitSink, SaveTicketSubmitSink>();
        builder.Services.AddScoped<IContactSubmitSink, ContactNotificationSubmitSink>();

        builder.Services.AddAWSService<IAmazonSimpleEmailService>();
        builder.Services.AddAWSService<IAmazonS3>();

        var app = builder.Build();

        app.UseHealthChecks("/api/health");

        app.UseContentSecurityPolicy();
        app.UseCors();
        app.Use(async (context, next) =>
        {
            context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
            context.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
            context.Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
            await next.Invoke();
        });

        app.UseSvgFavicon();

        if (Environment.GetEnvironmentVariable("__ASPNETCORE_BROWSER_TOOLS") is null)
        {
            app.UseResponseCompression();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseHttpsRedirection();
            app.UseHsts();
            app.UseExceptionHandler(new ExceptionHandlerOptions()
            {
                ExceptionHandlingPath = "/error",
                CreateScopeForErrors = true
            });
        }

        app.UseStatusCodePagesWithReExecute("/error/{0}");

        app.MapSocialRedirects();
        app.MapIconLibrary<SiteIcons>("/icons.svg");
        app.UseStaticUnityFiles();
        app.UseServiceWorkerScope();

        app.MapStaticAssets();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.MapGitHubAuthenticationEndpoints();

        app.MapRazorComponents<App>()
            .WithStaticAssets()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(
                typeof(Counter).Assembly,
                typeof(Icon).Assembly);

        return app;
    }

    /// <summary>
    /// Exports a certificate and its private key from AWS Certificate Manager, in the form
    /// Kestrel serves it from.
    /// </summary>
    /// <param name="configuration">Configuration describing the AWS account to read from.</param>
    /// <param name="certificateArn">The ARN of the exportable certificate.</param>
    /// <returns>The exported certificate.</returns>
    private static async Task<X509Certificate2> ExportAcmCertificateAsync(
        IConfiguration configuration,
        string certificateArn)
    {
        using var acmClient = configuration.GetAWSOptions().CreateServiceClient<IAmazonCertificateManager>();

        byte[] passphraseBytes = RandomNumberGenerator.GetBytes(32);
        string passphrase = Convert.ToBase64String(passphraseBytes);

        var response = await acmClient.ExportCertificateAsync(new ExportCertificateRequest
        {
            CertificateArn = certificateArn,
            Passphrase = new MemoryStream(Encoding.ASCII.GetBytes(passphrase))
        });

        string certPem = $"{response.Certificate}\n{response.CertificateChain}";
        return X509Certificate2.CreateFromEncryptedPem(
            certPem, response.PrivateKey, passphrase);
    }
}
