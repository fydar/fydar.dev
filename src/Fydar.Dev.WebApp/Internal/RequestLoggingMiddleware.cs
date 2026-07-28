using Serilog.Context;

namespace Fydar.Dev.WebApp.Internal;

internal class RequestLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILoggerFactory loggerFactory)
    {
        this.next = next;
        logger = loggerFactory.CreateLogger<RequestLoggingMiddleware>();
    }

    public async Task Invoke(
        HttpContext context)
    {
        using (LogContext.PushProperty("RequestPath", context.Request?.Path.Value))
        using (LogContext.PushProperty("RequestMethod", context.Request?.Method))
        {
            try
            {
                await next(context);
            }
            finally
            {
                using (LogContext.PushProperty("ResponseStatusCode", context.Response?.StatusCode))
                {
                    logger.LogInformation("RequestLog");
                }
            }
        }
    }
}
