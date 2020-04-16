using System;
using System.IO;
using Gibraltar.Monitor;
using Loupe.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Log = Gibraltar.Monitor.Log;
using LogWriteMode = Gibraltar.Monitor.LogWriteMode;
using LogMessageSeverity = Loupe.Extensibility.Data.LogMessageSeverity;

namespace Loupe.Serilog
{
    /// <summary>
    /// Records Serilog Log Events to Loupe.
    /// </summary>
    /// <remarks>
    /// <para>To route log events to Loupe, add an instance of this sink to your Serilog configuration.
    /// This sink formats log messages for each log event and sends them to the current Loupe session.
    /// THe lifecycle of the sink is independent of Loupe by default, so an independent sink can be created
    /// for each instance of Serilog and record the Serilog log events to the same Loupe session.</para>
    /// </remarks>
    /// <example>To forward events from Serilog to Loupe, add this sink to your Serilog logger configuration
    /// by calling the <see cref="LoupeExtensions.Loupe" /> extension method, like this:
    /// <code>
    /// var log = new LoggerConfiguration()
    ///     .WriteTo.Loupe()
    ///     .CreateLogger()
    /// </code>
    /// </example>
    /// <seealso cref="LoupeExtensions.Loupe"/>
    public class LoupeSink : ILogEventSink
    {
        private readonly LoupeConfiguration _configuration;
        private readonly IFormatProvider _formatProvider;
        private readonly Func<LogEvent, string> _resolveCategory;
        private readonly Func<LogEvent, string> _resolveDetails;

        /// <summary>
        /// Create a new Loupe Sink for Serilog that will manage the Loupe session directly.
        /// </summary>
        /// <param name="loupeConfiguration">The configuration for the Loupe Agent itself</param>
        /// <param name="sinkConfiguration">The configuration for the sink</param>
        /// <param name="formatProvider">Optional.  A format provider.</param>
        /// <remarks>This constructor will start a Loupe session with the provided configuration and
        /// will end the Loupe session when the sink is disposed.  It's recommended to use the
        /// <see cref="LoupeExtensions.Loupe"/> extension method instead of directly creating this sink.</remarks>
        public LoupeSink(AgentConfiguration loupeConfiguration, LoupeConfiguration sinkConfiguration,
            IFormatProvider formatProvider = null)
            : this(sinkConfiguration, formatProvider)
        {
            Log.Initialize(loupeConfiguration);
        }

        /// <summary>
        /// Create a new Loupe Sink for Serilog that will manage the Loupe session directly.
        /// </summary>
        /// <param name="configuration">The configuration for the sink</param>
        /// <param name="formatProvider">Optional.  A format provider.</param>
        /// <remarks>This constructor will create a sink to use the current Loupe session which should be
        /// independently managed within the application.  It's recommended to use the
        /// <see cref="LoupeExtensions.Loupe"/> extension method instead of directly creating this sink.</remarks>
        public LoupeSink(LoupeConfiguration configuration, IFormatProvider formatProvider = null)
        {
            _configuration = configuration;
            _formatProvider = formatProvider;

            if (string.IsNullOrWhiteSpace(_configuration.CategoryPropertyName))
            {
                _resolveCategory = e => "Serilog";
            }
            else
            {
                _resolveCategory = e => ResolveCategoryFromLogEvent(e, _configuration.CategoryPropertyName);
            }

            if (configuration.RenderProperties)
            {
                _resolveDetails = ResolveDetailsFromLogEvent;
            }
        }

        /// <inheritdoc />
        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level < _configuration.RestrictedToMinimumLevel) return;

            var severity = ConvertLevelToSeverity(logEvent.Level);

            var category = _resolveCategory(logEvent);

            var details = _resolveDetails?.Invoke(logEvent);

            var sourceProvider = _configuration.IncludeCallLocation ? new SerilogMessageSourceProvider(logEvent, 2, false) : null; 

            // We pass a null for the user name so that Log.WriteMessage() will figure it out for itself.
            Log.WriteMessage(severity, LogWriteMode.Queued, "Serilog", category,
                sourceProvider, null, logEvent.Exception, 
                details, logEvent.RenderMessage(_formatProvider), (string)null);
        }

        private LogMessageSeverity ConvertLevelToSeverity(LogEventLevel level)
        {
            switch(level)
            {
                case LogEventLevel.Verbose:
                case LogEventLevel.Debug:
                    return LogMessageSeverity.Verbose;
                case LogEventLevel.Information:
                    return LogMessageSeverity.Information;
                case LogEventLevel.Warning:
                    return LogMessageSeverity.Warning;
                case LogEventLevel.Error:
                    return LogMessageSeverity.Error;
                case LogEventLevel.Fatal:
                    return LogMessageSeverity.Critical;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        private string ResolveCategoryFromLogEvent(LogEvent logEvent, string propertyName)
        {
            var category = "Serilog";

            if (logEvent.Properties.TryGetValue(propertyName, out var value))
            {
                //render the property but with the string format l for literal to prevent Serilog adding quotes.
                category = value?.ToString("l", null) ?? category;
            }

            return category;
        }

        private string ResolveDetailsFromLogEvent(LogEvent logEvent)
        {
            if ((logEvent.Properties == null) || (logEvent.Properties.Count == 0))
                return null;

            //otherwise lets serialize these to JSON...
            var valueFormatter = new JsonValueFormatter();
            var output = new StringWriter();

            output.Write("{\r\n");
            
            foreach (var property in logEvent.Properties)
            {
                var name = property.Key;

                if (name.Length > 0 && name[0] == '@')
                {
                    // Escape first '@' by doubling
                    name = '@' + name;
                }
                output.Write("\t");
                JsonValueFormatter.WriteQuotedJsonString(name, output);
                output.Write(" : ");
                valueFormatter.Format(property.Value, output);
                output.Write(",\r\n");
            }

            output.Write("}\r\n");

            return output.ToString();
        }
    }
}
