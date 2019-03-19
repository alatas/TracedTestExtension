using Microsoft.Diagnostics.Tracing.Parsers;
using System;

namespace TTE.Core.TraceSession
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ThreadStopKernelTraceSession : KernelTraceSessionAttribute
    {
        public ThreadStopKernelTraceSession() : base(KernelTraceEventParser.Keywords.Thread, acceptedEventNames: new string[] { "Thread/Stop" })
        {

        }
    }
}