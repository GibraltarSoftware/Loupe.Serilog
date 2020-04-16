using System;
using System.Collections.Generic;
using System.Text;
using Gibraltar.Agent;
using Gibraltar.Agent.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loupe.Serilog.Tests
{
    [TestClass]
    public class Setup
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            var config = new AgentConfiguration();
            config.Publisher.ProductName = "Loupe";
            config.Publisher.ApplicationName = "Serilog";

            Log.StartSession(config);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            Log.EndSession("End of unit test fixture");
        }
    }
}
