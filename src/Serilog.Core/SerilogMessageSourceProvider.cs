using System;
using System.Diagnostics;
using System.Reflection;
using Gibraltar.Monitor;
using Serilog.Events;

namespace Loupe.Serilog
{
    internal class SerilogMessageSourceProvider : IMessageSourceProvider
    {
        private static volatile bool _breakPointEnable = false; // Can be changed in the debugger

        public SerilogMessageSourceProvider(LogEvent logEvent, int skipframes, bool attributeToException)
        {

            FindMessageSource(skipframes + 1, false, attributeToException ? logEvent.Exception : null,
                out var className, out var methodName, out var fileName, out var lineNumber);

            MethodName = methodName;
            ClassName = className;
            FileName = fileName;
            LineNumber = lineNumber;
        }

        /// <inheritdoc />
        public string MethodName { get; }

        /// <inheritdoc />
        public string ClassName { get;  }

        /// <inheritdoc />
        public string FileName { get; }

        /// <inheritdoc />
        public int LineNumber { get; }


        /// <summary>
        /// Extracts needed message source information from the current call stack.
        /// </summary>
        /// <remarks>This is used internally to perform the actual stack frame walk.  Constructors for derived classes
        /// all call this method.  This constructor also allows the caller to specify a log message as being
        /// of local origin, so Gibraltar stack frames will not be automatically skipped over when determining
        /// the originator for internally-issued log messages.</remarks>
        /// <param name="skipFrames">The number of stack frames to skip over to find the first candidate to be
        /// identified as the source of the log message.  (Should generally use 0 if exception parameter is not null.)</param>
        /// <param name="trustSkipFrames">True if logging a message originating in Gibraltar code (or to just trust skipFrames).
        /// False if logging a message from the client application and Gibraltar frames should be explicitly skipped over.</param>
        /// <param name="exception">An exception declared as the source of this log message (or null for normal call stack source).</param>
        /// <param name="className">The class name of the identified source (usually available).</param>
        /// <param name="methodName">The method name of the identified source (usually available).</param>
        /// <param name="fileName">The file name of the identified source (if available).</param>
        /// <param name="lineNumber">The line number of the identified source (if available).</param>
        /// <returns>The index of the stack frame chosen</returns>
        internal static int FindMessageSource(int skipFrames, bool trustSkipFrames, Exception exception, out string className,
                                             out string methodName, out string fileName, out int lineNumber)
        {
            int selectedFrame = -1;

            try
            {
                // We use skipFrames+1 here so that callers can pass in 0 to designate themselves rather than have to know to start with 1.
                // But for an exception stack trace, we didn't get added to the stack, so don't add anything in that case.
                StackTrace stackTrace = (exception == null) ? new StackTrace(skipFrames + 1, true) : new StackTrace(exception, true);
                StackFrame frame = null;
                StackFrame firstSystem = null;
                StackFrame newFrame;
                MethodBase method = null;
                string frameModule;

                int frameIndex = 0; // we already accounted for skip frames in getting the stack trace
                while (true)
                {
                    // Careful:  We may be out of frames, in which case we're going to stop, hopefully without an exception.
                    try
                    {
                        newFrame = stackTrace.GetFrame(frameIndex);
                        frameIndex++; // Do this here so any continue statement below in this loop will be okay.
                        if (newFrame == null) // Not sure if this check is actually needed, but it doesn't hurt.
                            break; // We're presumably off the end of the stack, bail out of the loop!

                        method = newFrame.GetMethod();
                        if (method == null) // The method we found might be null (if the frame is invalid?).
                            break; // We're presumably off the end of the stack, bail out of the loop!

                        frameModule = method.Module.Name;
                        var frameClass = method.DeclaringType?.FullName;

                        if (((ReferenceEquals(frameClass, null) == false)
                             && (frameClass.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
                             || frameClass.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
                             || frameClass.StartsWith("Serilog.", StringComparison.OrdinalIgnoreCase)))
                            || frameModule.Equals("System.dll")  //Doesn't apply in .NET Core
                            || frameModule.Equals("mscorlib.dll")) //Doesn't apply in .NET Core
                        {
                            // Ahhh, a frame in the system libs... Next non-system frame will be our pick!
                            if (firstSystem == null) // ...unless we find no better candidate, so remember the first one.
                            {
                                firstSystem = newFrame;
                            }
                        }
                        else
                        {
                            frame = newFrame; // New one is valid, and not system, so update our chosen frame to use it.
                            // We already got its corresponding method, above, to validate the module.

                            // Okay, it's not in the system libs, so it might be a good candidate,
                            // but do we need to filter out Loupe or is this a deliberate local invocation?

                            if (trustSkipFrames || (firstSystem != null))
                                break;

                            if ((ReferenceEquals(frameClass, null)
                                 || (frameClass.StartsWith("Loupe.Agent.", StringComparison.OrdinalIgnoreCase) == false
                                     && frameClass.StartsWith("Loupe.Core.", StringComparison.OrdinalIgnoreCase) == false
                                     && frameClass.StartsWith("Loupe.Serilog.", StringComparison.OrdinalIgnoreCase) == false))
                                && frameModule.Equals("Loupe.Agent.NETCore.dll") == false //Doesn't apply in .NET Core
                                && frameModule.Equals("Loupe.Core.NETCore.dll") == false) //Doesn't apply in .NET Core
                            {
                                // This is the first frame which is not in our known ecosystem,
                                // so this must be the client code calling us.
                                break; // We found it!  Break out of the loop.
                            }
                        }
                    }
                    catch
                    {
                        // Hmmm, we got some sort of failure which we didn't know enough to prevent?
                        // We could comment on that... but we can't do logging here, it gets recursive!
                        // So use our safe breakpoint to alert a debugging user.  This is ignored in production.
                        DebugBreak(); // Stop the debugger here (if it's running, otherwise we won't alert on it).

                        // Well, whatever we found - that's where we are.  We have to give up our search.
                        break;
                    }

                    method = null; // Invalidate it for the next loop.

                    // Remember, frameIndex was already incremented near the top of the loop
                    if (frameIndex > 200) // Note: We're assuming stacks can never be this deep (without finding our target)
                    {
                        // Maybe we messed up our failure-detection, so to prevent an infinite loop from hanging the application...
                        DebugBreak(); // Stop the debugger here (if it's running).  This shouldn't ever be hit.

                        break; // Okay, it's just not sensible for stack to be so deep, so let's give up.
                    }
                }

                if (frame == null || method == null)
                {
                    frame = stackTrace.GetFrame(0); // If we went off the end, go back to the first frame (after skipFrames).
                    selectedFrame = 0;
                }
                else
                {
                    selectedFrame = frameIndex;
                }

                method = (frame == null) ? null : frame.GetMethod(); // Make sure these are in sync!
                if (method == null)
                {
                    frame = firstSystem; // Use that first system frame we found if no later candidate arose.
                    method = (frame == null) ? null : frame.GetMethod();
                }

                // Now that we've selected the best possible frame, we need to make sure we really found one.
                if (method != null)
                {
                    // Whew, we found a valid method to attribute this message to.  Get the details safely....
                    className = method.DeclaringType == null ? null : method.DeclaringType.FullName;
                    methodName = method.Name;

                    try
                    {
                        //now see if we have file information
                        fileName = frame.GetFileName();
                        if (string.IsNullOrEmpty(fileName) == false)
                        {
                            // m_FileName = Path.GetFileName(m_FileName); // Drops full path... but we want that info!
                            lineNumber = frame.GetFileLineNumber();
                        }
                        else
                            lineNumber = 0; // Not meaningful if there's no file name!
                    }
                    catch
                    {
                        fileName = null;
                        lineNumber = 0;
                    }
                }
                else
                {
                    // Ack! We got nothing!  Invalidate all of these which depend on it and are thus meaningless.
                    methodName = null;
                    className = null;
                    fileName = null;
                    lineNumber = 0;
                }
            }
            catch
            {
                // Bleagh!  We got an unexpected failure (not caught and handled by a lower catch block as being expected).
                DebugBreak(); // Stop the debugger here (if it's running, otherwise we won't alert on it).

                methodName = null;
                className = null;
                fileName = null;
                lineNumber = 0;
            }

            return selectedFrame;
        }

        /// <summary>
        /// Automatically stop debugger like a breakpoint, if enabled.
        /// </summary>
        /// <remarks>This will check the state of Log.BreakPointEnable and whether a debugger is attached,
        /// and will breakpoint only if both are true.  This should probably be extended to handle additional
        /// configuration options using an enum, assuming the basic usage works out.  This method is conditional
        /// upon a DEBUG build and will be safely ignored in release builds, so it is not necessary to wrap calls
        /// to this method in #if DEBUG (acts much like Debug class methods).</remarks>
        [Conditional("DEBUG")]
        private static void DebugBreak()
        {
            if (_breakPointEnable && Debugger.IsAttached)
            {
                Debugger.Break(); // Stop here only when debugging
                // ...then Shift-F11 to step out to where it is getting called...
            }
        }
    }
}
