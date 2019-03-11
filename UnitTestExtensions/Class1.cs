using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class KernelTraceSessionAttribute : TraceSessionAttribute
{
    public KernelTraceSessionAttribute(KernelTraceEventParser.Keywords KernelSession = KernelTraceEventParser.Keywords.None,
        string[] acceptedEventProviderNames = null,
        string[] rejectedEventProviderNames = null,
        string[] acceptedEventNames = null,
        string[] rejectedEventNames = null,
        string[] inclusiveProcessNames = null,
        int[] includedEventIds = null,
        int[] excludedEventIds = null) : base("kernel", KernelTraceEventParser.KernelSessionName,
        TraceEventLevel.Verbose,
        acceptedEventProviderNames,
        rejectedEventProviderNames,
        acceptedEventNames,
        rejectedEventNames,
        inclusiveProcessNames,
        includedEventIds,
        excludedEventIds)
    {

    }

}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TraceSessionAttribute : Attribute
{
    public TraceSessionAttribute(string providerName,
        string sessionName = null,
        TraceEventLevel providerLevel = TraceEventLevel.Verbose,
        string[] acceptedEventProviderNames = null,
        string[] rejectedEventProviderNames = null,
        string[] acceptedEventNames = null,
        string[] rejectedEventNames = null,
        string[] inclusiveProcessNames = null,
        int[] includedEventIds = null,
        int[] excludedEventIds = null)
    {

    }
}

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
        int[] excludedEventIds = null) : base(ClrTraceEventParser.ProviderName,
            sessionName,
            TraceEventLevel.Verbose,
        acceptedEventProviderNames,
        rejectedEventProviderNames,
        acceptedEventNames,
        rejectedEventNames,
        inclusiveProcessNames,
        includedEventIds,
        excludedEventIds)
    {

    }
}