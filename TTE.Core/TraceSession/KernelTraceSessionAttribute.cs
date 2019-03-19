using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using System;

namespace TTE.Core.TraceSession
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class KernelTraceSessionAttribute : TraceSessionAttribute
    {
        public KernelTraceSessionAttribute(KernelTraceEventParser.Keywords KernelSessionKeywords = KernelTraceEventParser.Keywords.All,
            string[] acceptedEventProviderNames = null,
            string[] rejectedEventProviderNames = null,
            string[] acceptedEventNames = null,
            string[] rejectedEventNames = null,
            string[] inclusiveProcessNames = null,
            int[] includedEventIds = null,
            int[] excludedEventIds = null,
            bool includeOwnProcess = false) : base("kernel",
                typeof(KernelTraceEventParser),
                KernelTraceEventParser.KernelSessionName,
                TraceEventLevel.Verbose,
                acceptedEventProviderNames,
                rejectedEventProviderNames,
                acceptedEventNames,
                rejectedEventNames,
                inclusiveProcessNames,
                includedEventIds,
                excludedEventIds,
                includeOwnProcess)
        {
            this.KernelSessionKeywords = KernelSessionKeywords;
        }

        public readonly KernelTraceEventParser.Keywords KernelSessionKeywords;
    }
}