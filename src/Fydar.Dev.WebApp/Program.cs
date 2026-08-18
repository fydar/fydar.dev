using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;
using Amazon.S3;
using Amazon.SimpleEmail;
using Fydar.AspNetCore.CSP;
using Fydar.Dev.Services.EmailTickets;
using Fydar.Dev.WebApp.Client.Components.Pages;
using Fydar.Dev.WebApp.Components;
using Fydar.Dev.WebApp.Components.Iconography;
using Fydar.Dev.WebApp.Internal;
using Fydar.Dev.WebApp.Internal.AntiforgeryNoStoreWorkaround;
using Fydar.Dev.WebApp.Internal.UnityFiles;
using Fydar.Dev.WebApp.Toolkit.Icons;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Net.Http.Headers;
using Serilog;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Fydar.Dev.WebApp;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.ColoredConsole()
            .CreateBootstrapLogger();

        try
        {
            var host = CreateHost(args);
            host.Start();

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

    public static IHost CreateHost(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services));

        builder.WebHost.UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "True");

        builder.Configuration.AddEnvironmentVariables("CONFIG_");

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

        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddSingleton(new S3EmailReaderServiceConfiguration()
        {
            Bucket = "fydar.dev-inbound-email"
        });
        builder.Services.AddSingleton<IEmailReaderService, S3EmailReaderService>();
        builder.Services.AddSingleton<TicketHtmlSanitizer>();
        builder.Services.AddScoped<HtmlRenderer>();

        builder.Services.AddScoped<IContactSubmitSink, SaveTicketSubmitSink>();
        builder.Services.AddScoped<IContactSubmitSink, ContactNotificationSubmitSink>();

        builder.Services.AddAWSService<IAmazonSimpleEmailService>();
        builder.Services.AddAWSService<IAmazonS3>();

        string certificateArn = builder.Configuration.GetValue<string>("CERTIFICATEARN") ?? string.Empty;
        bool useDevelopmentCertificate = string.IsNullOrEmpty(certificateArn);

        if (!useDevelopmentCertificate)
        {
            builder.Services.AddAWSService<IAmazonCertificateManager>();
        }

        if (builder.WebHost.GetSetting("Environment") != "Development")
        {
            builder.Services.Configure<StaticFileOptions>(opts =>
            {
                opts.OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] = $"public,max-age=31536000";
                };
            });
            builder.Services.Configure<HttpsRedirectionOptions>(opts =>
            {
                opts.RedirectStatusCode = (int)HttpStatusCode.PermanentRedirect;
            });
        }

        int httpPort = builder.Configuration.GetValue("HTTPPORT", 80);
        int httpsPort = builder.Configuration.GetValue("HTTPSPORT", 443);

        builder.WebHost.UseKestrel(kestrel =>
        {
            kestrel.ListenAnyIP(httpPort);

            kestrel.ListenAnyIP(
                httpsPort,
                listen =>
                {
                    listen.Protocols = HttpProtocols.Http1 | HttpProtocols.Http2 | HttpProtocols.Http3;

                    if (useDevelopmentCertificate)
                    {
                        listen.UseHttps();
                    }
                    else
                    {
                        var acmClient = kestrel.ApplicationServices.GetRequiredService<IAmazonCertificateManager>();
                        var cert = ExportAcmCertificateAsync(acmClient, certificateArn).GetAwaiter().GetResult();
                        listen.UseHttps(cert);
                    }
                });
        });

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
            if (!useDevelopmentCertificate)
            {
                app.UseHttpsRedirection();
            }

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

        app.MapStaticAssets();

        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .WithStaticAssets()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(
                typeof(Counter).Assembly,
                typeof(Icon).Assembly);

        return app;
    }

    private static async Task<X509Certificate2> ExportAcmCertificateAsync(IAmazonCertificateManager acmClient, string certificateArn)
    {
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
