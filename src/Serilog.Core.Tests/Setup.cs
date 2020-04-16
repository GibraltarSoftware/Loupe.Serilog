using System;
using System.Collections.Generic;
using System.Text;
using Gibraltar.Agent;
using Loupe.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loupe.Serilog.Core.Tests
{
    [TestClass]
    public class Setup
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            Log.StartSession(new AgentConfiguration()
            {
                Publisher = new PublisherConfiguration()
                {
                    ProductName = "Loupe",
                    ApplicationName = "Serilog Core"
                }
            });
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            Log.EndSession("End of unit test fixture");
        }
    }
}
