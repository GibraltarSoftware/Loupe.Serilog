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
    public class LoupeSink : ILogEventSink
    {
        private readonly LoupeConfiguration _configuration;
        private readonly IFormatProvider _formatProvider;
        private readonly Func<LogEvent, string> _resolveCategory;
        private readonly Func<LogEvent, string> _resolveDetails;

        public LoupeSink(AgentConfiguration loupeConfiguration, LoupeConfiguration sinkConfiguration,
            IFormatProvider formatProvider = null)
            : this(sinkConfiguration, formatProvider)
        {
            Log.Initialize(loupeConfiguration);
        }

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

            // We pass a null for the user name so that Log.WriteMessage() will figure it out for itself.
            Log.WriteMessage(severity, LogWriteMode.Queued, "Serilog", category,
                (IMessageSourceProvider)null, null, logEvent.Exception, 
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
            var category = logEvent.Properties[propertyName].ToString() ?? "Serilog";

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
