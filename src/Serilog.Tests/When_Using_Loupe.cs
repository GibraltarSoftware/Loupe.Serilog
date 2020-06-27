using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Events;

namespace Loupe.Serilog.Tests
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

        [TestMethod]
        public void Can_Specify_Category_As_Property()
        {
            var methodInfo = nameof(Can_Specify_Category_As_Property);

            var categoryPropertyName = "LogCategory";

            using (var log = new LoggerConfiguration()
                .WriteTo.Loupe(categoryPropertyName: categoryPropertyName)
                .CreateLogger())
            {
                log.ForContext(categoryPropertyName, methodInfo + ".Debug")
                    .Debug("This is a debug message about {methodInfo} which should be in category {" + categoryPropertyName + "}", methodInfo);

                log.ForContext(categoryPropertyName, methodInfo + ".Verbose")
                    .Verbose("This is a verbose message about {methodInfo} which should be in category {" + categoryPropertyName + "}", methodInfo);

                log.ForContext(categoryPropertyName, methodInfo + ".Info")
                    .Information("This is an informational message about {methodInfo} which should be in category {" + categoryPropertyName + "}", methodInfo);

                log.ForContext(categoryPropertyName, methodInfo + ".Error")
                    .Error("This is an error message about {methodInfo} which should be in category {" + categoryPropertyName + "}", methodInfo);
            }
        }

        [TestMethod]
        public void Can_Specify_Category_As_Missing_Property()
        {
            var methodInfo = nameof(Can_Specify_Category_As_Property);

            using (var log = new LoggerConfiguration()
                .WriteTo.Loupe(categoryPropertyName: "CategoryWeWillNotFind")
                .CreateLogger())
            {
                log.ForContext("LoupeCategoryProperty", methodInfo + ".Debug")
                    .Debug("This is a debug message about {methodInfo} which should be in the default category", methodInfo);

                log.ForContext("LoupeCategoryProperty", methodInfo + ".Verbose")
                    .Verbose("This is a verbose message about {methodInfo} which should be in the default category", methodInfo);

                log.ForContext("LoupeCategoryProperty", methodInfo + ".Info")
                    .Information("This is an informational message about {methodInfo} which should be in the default category", methodInfo);

                log.ForContext("LoupeCategoryProperty", methodInfo + ".Error")
                    .Error("This is an error message about {methodInfo} which should be in the default category", methodInfo);
            }
        }

        [TestMethod]
        public void Can_Split_Caption_And_Description()
        {
            var methodInfo = nameof(Can_Split_Caption_And_Description);

            using (var log = new LoggerConfiguration()
                .WriteTo.Loupe(restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger())
            {
                log.Debug("This is a debug message about {methodInfo} and should NOT be displayed\r\n" +
                          "But if it was, this would be in the description field.", methodInfo);
                log.Verbose("This is a verbose message about {methodInfo} and should NOT be displayed\r\n" +
                            "But if it was, this would be in the description field.", methodInfo);
                log.Information("This is an informational message about {methodInfo} and SHOULD be displayed\r\n" +
                                "But if it was, this would be in the description field.", methodInfo);
                log.Error("This is an error message about {methodInfo} and SHOULD be displayed\r\n" +
                          "But if it was, this would be in the description field.", methodInfo);

            }
        }
    }
}
