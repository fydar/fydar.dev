using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Fydar.Dev.WebApp.Internal;

/// <summary>
/// An <see cref="ITextFormatter"/> that writes events in a compact JSON format, for consumption in environments 
/// without message template support. Message templates are rendered into text and a hashed event id is included.
/// </summary>
internal class JsonLogTextFormatter : ITextFormatter
{
    private readonly JsonValueFormatter valueFormatter;

    /// <summary>
    /// Construct a <see cref="JsonLogTextFormatter"/>, optionally supplying a formatter for
    /// <see cref="LogEventPropertyValue"/>s on the event.
    /// </summary>
    /// <param name="valueFormatter">A value formatter, or null.</param>
    public JsonLogTextFormatter(
        JsonValueFormatter? valueFormatter = null)
    {
        this.valueFormatter = valueFormatter ?? new JsonValueFormatter(typeTagName: "$type");
    }

    /// <summary>
    /// Format the log event into the output. Subsequent events will be newline-delimited.
    /// </summary>
    /// <param name="logEvent">The event to format.</param>
    /// <param name="output">The output.</param>
    public void Format(
        LogEvent logEvent,
        TextWriter output)
    {
        FormatEvent(logEvent, output, valueFormatter);
        output.WriteLine();
    }

    /// <summary>
    /// Format the log event into the output.
    /// </summary>
    /// <param name="logEvent">The event to format.</param>
    /// <param name="output">The output.</param>
    /// <param name="valueFormatter">A value formatter for <see cref="LogEventPropertyValue"/>s on the event.</param>
    public static void FormatEvent(
        LogEvent logEvent,
        TextWriter output,
        JsonValueFormatter valueFormatter)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(valueFormatter);

        output.Write("{\"@t\":\"");
        output.Write(logEvent.Timestamp.UtcDateTime.ToString("O"));
        output.Write("\",\"@m\":");
        string message = logEvent.MessageTemplate.Render(logEvent.Properties);
        JsonValueFormatter.WriteQuotedJsonString(message, output);

        // if (logEvent.Level != LogEventLevel.Information)
        {
            output.Write(",\"@l\":\"");
            output.Write(logEvent.Level);
            output.Write('\"');
        }

        if (logEvent.Exception != null)
        {
            output.Write(",\"@x\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
        }

        foreach (var property in logEvent.Properties)
        {
            string name = property.Key;
            if (name.Length > 0 && name[0] == '@')
            {
                // Escape first '@' by doubling
                name = '@' + name;
            }

            output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(name, output);
            output.Write(':');
            valueFormatter.Format(property.Value, output);
        }

        output.Write('}');
    }
}
