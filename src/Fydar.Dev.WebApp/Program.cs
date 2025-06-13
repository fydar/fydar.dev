using Amazon.S3;
using Amazon.SimpleEmail;
using Fydar.Dev.Services.EmailTickets;
using Fydar.Dev.WebApp.Client.Components.Pages;
using Fydar.Dev.WebApp.Components;
using Fydar.Dev.WebApp.Components.Iconography;
using Fydar.Dev.WebApp.Internal;
using Fydar.Dev.WebApp.Internal.AntiforgeryNoStoreWorkaround;
using Fydar.Dev.WebApp.Toolkit.Icons;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Serilog;
using Serilog.Events;
using System.Net;
using System.Text;

namespace Fydar.Dev.WebApp;

public class Program
{
	public static async Task<int> Main(string[] args)
	{
		var loggerConfiguration = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
			.MinimumLevel.Override("Microsoft.AspNetCore.Server.Kestrel", LogEventLevel.Error)
			.Enrich.FromLogContext()
			.WriteTo.Sink(new ColoredConsoleLogEventSink());

		var logger = loggerConfiguration.CreateLogger();
		Log.Logger = logger;

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

		builder.Host.UseSerilog();

		builder.WebHost.UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "True");

		builder.Configuration.AddEnvironmentVariables("CONFIG_");

		// Add services to the container.
		builder.Services.AddHealthChecks();

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

		builder.Services.AddRazorComponents()
			.AddInteractiveWebAssemblyComponents();

		builder.Services.AddLettuceEncrypt();

		builder.Services.AddSingleton(new S3EmailReaderServiceConfiguration()
		{
			Bucket = "fydar.dev-inbound-email"
		});
		builder.Services.AddSingleton<IEmailReaderService, S3EmailReaderService>();
		builder.Services.AddScoped<HtmlRenderer>();

		builder.Services.AddScoped<IContactSubmitSink, SaveTicketSubmitSink>();
		builder.Services.AddScoped<IContactSubmitSink, ContactNotificationSubmitSink>();

		builder.Services.AddAWSService<IAmazonSimpleEmailService>();
		builder.Services.AddAWSService<IAmazonS3>();

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

			string domainName = builder.Configuration.GetValue<string>("DOMAINNAME") ?? string.Empty;

			builder.Services.AddLettuceEncrypt(options =>
			{
				options.AcceptTermsOfService = true;
				options.DomainNames = [domainName];
				options.EmailAddress = "dev.anthonymarmont@gmail.com";
			});
			// 	.PersistDataToDirectory(new DirectoryInfo("lettuce"), null);
		}

		builder.WebHost.UseKestrel(kestrel =>
		{
			var appServices = kestrel.ApplicationServices;

			kestrel.Listen(IPAddress.Any, 8060);

			kestrel.Listen(
				IPAddress.Any, 8061,
				listen =>
				{
					listen.Protocols = HttpProtocols.Http1 | HttpProtocols.Http2;

					listen.UseHttps(adapter =>
					{
						if (builder.WebHost.GetSetting("Environment") != "Development")
						{
							adapter.UseLettuceEncrypt(appServices);
						}
					});
				});
		});

		var app = builder.Build();

		app.UseHealthChecks("/api/health");

		app.Use(async (context, next) =>
		{
			if (context.Request.Path.Equals("/favicon.ico")
				&& context.Request.Headers.Accept.Any(a => a?.Contains("image/svg+xml", StringComparison.OrdinalIgnoreCase) ?? false))
			{
				context.Response.Headers.CacheControl = $"nocache";
				context.Response.ContentType = "image/svg+xml; charset=utf-8";

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
			app.UseHttpsRedirection();
			app.UseHsts();
			app.UseExceptionHandler("/error");
		}

		app.UseStatusCodePagesWithReExecute("/error/{0}");

		app.MapIconLibrary<SiteIcons>("/icons.svg");

		var provider = new FileExtensionContentTypeProvider();
		provider.Mappings.Remove(".br");
		provider.Mappings.Clear();
		provider.Mappings[".js"] = "application/javascript";
		provider.Mappings[".br"] = "application/octet-stream";
		provider.Mappings[".data"] = "application/octet-stream";
		provider.Mappings[".bank"] = "application/octet-stream";

		app.UseStaticFiles(new StaticFileOptions
		{
			FileProvider = new PhysicalFileProvider(
				Path.Combine(builder.Environment.ContentRootPath, "precompressed")),
			ContentTypeProvider = provider,
			OnPrepareResponse = context =>
			{
				var headers = context.Context.Response.Headers;

				if (context.File.Name.EndsWith(".br", StringComparison.OrdinalIgnoreCase))
				{
					headers.ContentEncoding = "br";

					if (context.File.Name.Contains(".wasm", StringComparison.OrdinalIgnoreCase))
					{
						headers.ContentType = "application/wasm";
					}
					else if (context.File.Name.Contains(".js", StringComparison.OrdinalIgnoreCase))
					{
						headers.ContentType = "application/javascript";
					}
					else if (context.File.Name.Contains(".data", StringComparison.OrdinalIgnoreCase))
					{
						headers.ContentType = "application/octet-stream";
					}
				}

				if (context.File.Name.EndsWith(".data", StringComparison.OrdinalIgnoreCase))
				{
					headers.ContentType = "application/octet-stream";
				}
			}
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
}
