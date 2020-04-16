using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Events;

namespace Loupe.Serilog.Core.Tests
{
    [TestClass]
    public class When_Using_Loupe
    {
        [TestMethod]
        public void Can_Initialize_With_Defaults()
        {
            var methodInfo = nameof(Can_Initialize_With_Defaults);

            using (var log = new LoggerConfiguration()
                .WriteTo.Loupe()
                .CreateLogger())
            {
                log.Debug("This is a debug message about {methodInfo}", methodInfo);
                log.Error("This is an error message about {methodInfo}", methodInfo);
            }
        }

        [TestMethod]
        public void Can_Record_Exceptions_Not_In_Message()
        {
            var methodInfo = nameof(Can_Record_Exceptions_Not_In_Message);

            var unthrownException = new InvalidOperationException("The outermost exception",
                new ApplicationException("The innermost exception"));

            using (var log = new LoggerConfiguration()
                .WriteTo.Loupe()
                .CreateLogger())
            {
                log.Debug(unthrownException, "This is a debug message about {methodInfo}", methodInfo);
                log.Error(unthrownException, "This is an error message about {methodInfo}", methodInfo);
            }
        }

        [TestMethod]
        public void Can_Record_Thrown_Exceptions_Not_In_Message()
        {
            var methodInfo = nameof(Can_Record_Thrown_Exceptions_Not_In_Message);

            using (var log = new LoggerConfiguration()
                .WriteTo.Loupe()
                .CreateLogger())
            {
                try
                {

                    var thrownException = new InvalidOperationException("The outermost exception",
                        new ApplicationException("The innermost exception"));

                    throw thrownException;

                }
                catch (Exception ex)
                {
                    log.Debug(ex, "This is a debug message about {methodInfo}", methodInfo);
                    log.Error(ex, "This is an error message about {methodInfo}", methodInfo);
                }
            }
        }

        [TestMethod]
        public void Can_Record_Exceptions_In_Message()
        {
            var methodInfo = nameof(Can_Record_Exceptions_In_Message);

            var unthrownException = new InvalidOperationException("The outermost exception",
                new ApplicationException("The innermost exception"));

            using (var log = new LoggerConfiguration()
                .WriteTo.Loupe()
                .CreateLogger())
            {
                log.Debug(unthrownException, "This is a debug message with exception {unthrownException} about {methodInfo}", unthrownException, methodInfo);
                log.Error(unthrownException, "This is an error message with exception {unthrownException} about {methodInfo}", unthrownException, methodInfo);
            }
        }

        [TestMethod]
        public void Can_Specify_Minimum_Severity()
        {
            var methodInfo = nameof(Can_Specify_Minimum_Severity);

            using (var log = new LoggerConfiguration()
                .WriteTo.Loupe(restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger())
            {
                log.Debug("This is a debug message about {methodInfo} and should NOT be displayed", methodInfo);
                log.Verbose("This is a verbose message about {methodInfo} and should NOT be displayed", methodInfo);
                log.Information("This is an informational message about {methodInfo} and SHOULD be displayed", methodInfo);
                log.Error("This is an error message about {methodInfo} and SHOULD be displayed", methodInfo);
            }
        }
    }
}
