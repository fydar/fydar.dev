using Fydar.Dev.WebApp.Internal;
using Serilog.Configuration;

namespace Serilog;

/// <summary>
/// Adds the <c>WriteTo.ColoredConsole()</c> extension method to <see cref="LoggerConfiguration"/>.
/// </summary>
/// <remarks>
/// This must be public and static so that <c>Serilog.Settings.Configuration</c> can discover it by
/// name from the <c>Serilog:WriteTo</c> section of the application configuration.
/// </remarks>
public static class ColoredConsoleLoggerConfigurationExtensions
{
    /// <summary>
    /// Writes human readable, colored log events to the console.
    /// </summary>
    /// <param name="sinkConfiguration">The configuration to write to.</param>
    public static LoggerConfiguration ColoredConsole(
        this LoggerSinkConfiguration sinkConfiguration)
    {
        ArgumentNullException.ThrowIfNull(sinkConfiguration);

        return sinkConfiguration.Sink(new ColoredConsoleLogEventSink());
    }
}
