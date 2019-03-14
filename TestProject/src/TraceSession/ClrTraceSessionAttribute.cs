using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ClrTraceSessionAttribute : TraceSessionAttribute
{
    public ClrTraceSessionAttribute(
        string sessionName = null,
        TraceEventLevel providerLevel = TraceEventLevel.Verbose,
        string[] acceptedEventProviderNames = null,
        string[] rejectedEventProviderNames = null,
        string[] acceptedEventNames = null,
        string[] rejectedEventNames = null,
        string[] inclusiveProcessNames = null,
        int[] includedEventIds = null,
        int[] excludedEventIds = null,
        bool includeOwnProcess = false) : base(ClrTraceEventParser.ProviderName,
            typeof(ClrTraceEventParser),
            sessionName,
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

    }
}
