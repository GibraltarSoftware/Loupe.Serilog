using System;
using System.Collections.Generic;
using System.Text;
using Serilog.Events;

namespace Loupe.Serilog
{
    /// <summary>
    /// Configuration for the Loupe Sink (independent of the Loupe Agent configuration)
    /// </summary>
    public class LoupeConfiguration
    {
        /// <summary>
        /// Only record log events with this severity or higher.
        /// </summary>
        public LogEventLevel RestrictedToMinimumLevel { get; set; }

        /// <summary>
        /// End the Loupe Session when the log sink is closed
        /// </summary>
        /// <remarks>Use only when direct integration with the Loupe Agent is not possible, and only on one logger in a process.</remarks>
        public bool EndSessionOnClose { get; set; }

        /// <summary>
        /// Enable tracking of what class &amp; method called Serilog
        /// </summary>
        /// <remarks>True by default, enables Loupe Method Source Info for each log call.</remarks>
        public bool IncludeCallLocation { get; set; }

        /// <summary>
        /// Enable rendering properties as a JSON block in Log Message Details
        /// </summary>
        public bool RenderProperties { get; set; }

        /// <summary>
        /// The property to check for Category name, if present.
        /// </summary>
        public string CategoryPropertyName { get; set; }
    }
}
