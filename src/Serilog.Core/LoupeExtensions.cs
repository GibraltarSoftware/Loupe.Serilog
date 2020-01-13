using System;
using Loupe.Configuration;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Loupe.Serilog
{
    /// <summary>
    /// Extension methods for Loupe
    /// </summary>
    public static class LoupeExtensions
    {
        /// <summary>
        /// Write Serilog Events to Loupe
        /// </summary>
        /// <param name="loggerConfiguration">The Serilog configuration</param>
        /// <param name="categoryPropertyName">The name of the property in the event to use as the Loupe Category</param>
        /// <param name="includeCallLocation">True to include class and method info for each log message</param>
        /// <param name="renderProperties">True to render the Serilog event properties in the Loupe message details</param>
        /// <param name="restrictedToMinimumLevel">The minimum level of events to pass to Loupe</param>
        /// <param name="formatProvider">Optional.  A custom formatter for rendering events.</param>
        /// <returns>the updated Serilog configuration</returns>
        public static LoggerConfiguration Loupe(
            this LoggerSinkConfiguration loggerConfiguration,
            string categoryPropertyName = null,
            bool includeCallLocation = true,
            bool renderProperties = true,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            IFormatProvider formatProvider = null)
        {
            var config = new LoupeConfiguration
            {
                CategoryPropertyName = categoryPropertyName,
                EndSessionOnClose = false, //we don't own the Loupe agent.
                IncludeCallLocation = includeCallLocation,
                RenderProperties = renderProperties,
                RestrictedToMinimumLevel = restrictedToMinimumLevel
            };

            return loggerConfiguration.Sink(new LoupeSink(config, formatProvider));
        }

        /// <summary>
        /// Write Serilog Events to Loupe
        /// </summary>
        /// <param name="loggerConfiguration">The Serilog configuration</param>
        /// <param name="loupeConfiguration">The Loupe Agent configuration</param>
        /// <param name="categoryPropertyName">The name of the property in the event to use as the Loupe Category</param>
        /// <param name="includeCallLocation">True to include class and method info for each log message</param>
        /// <param name="renderProperties">True to render the Serilog event properties in the Loupe message details</param>
        /// <param name="restrictedToMinimumLevel">The minimum level of events to pass to Loupe</param>
        /// <param name="formatProvider">Optional.  A custom formatter for rendering events.</param>
        /// <returns>the updated Serilog configuration</returns>
        /// <remarks>This will start the Loupe agent and end its session when the logger is closed.
        /// It's recommended that the Loupe Agent lifecycle be managed directly instead of using this method whenever feasible.</remarks>
        public static LoggerConfiguration Loupe(
            this LoggerSinkConfiguration loggerConfiguration,
            AgentConfiguration loupeConfiguration,
            string categoryPropertyName = null,
            bool includeCallLocation = true,
            bool renderProperties = true,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            IFormatProvider formatProvider = null)
        {
            var config = new LoupeConfiguration
            {
                CategoryPropertyName = categoryPropertyName,
                EndSessionOnClose = true, //we own the Loupe agent.
                IncludeCallLocation = includeCallLocation,
                RenderProperties = renderProperties,
                RestrictedToMinimumLevel = restrictedToMinimumLevel
            };

            return loggerConfiguration.Sink(new LoupeSink(loupeConfiguration, config, formatProvider));
        }
    }
}
