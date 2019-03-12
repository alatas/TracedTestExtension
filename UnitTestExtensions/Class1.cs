using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Linq;
using System.Threading;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class KernelTraceSessionAttribute : TraceSessionAttribute
{
    public KernelTraceSessionAttribute(KernelTraceEventParser.Keywords KernelSessionKeywords = KernelTraceEventParser.Keywords.None,
        string[] acceptedEventProviderNames = null,
        string[] rejectedEventProviderNames = null,
        string[] acceptedEventNames = null,
        string[] rejectedEventNames = null,
        string[] inclusiveProcessNames = null,
        int[] includedEventIds = null,
        int[] excludedEventIds = null) : base("kernel",
            typeof(KernelTraceEventParser),
            KernelTraceEventParser.KernelSessionName,
            TraceEventLevel.Verbose,
            acceptedEventProviderNames,
            rejectedEventProviderNames,
            acceptedEventNames,
            rejectedEventNames,
            inclusiveProcessNames,
            includedEventIds,
            excludedEventIds)
    {
        this.KernelSessionKeywords = KernelSessionKeywords;
    }

    public readonly KernelTraceEventParser.Keywords KernelSessionKeywords;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TraceSessionAttribute : Attribute
{
    public TraceSessionAttribute(string providerName,
        Type eventParserType,
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
        this.providerName = providerName;
        this.eventParserType = eventParserType;
        this.sessionName = sessionName;
        this.providerLevel = providerLevel;
        this.acceptedEventProviderNames = acceptedEventProviderNames;
        this.rejectedEventProviderNames = rejectedEventProviderNames;
        this.acceptedEventNames = acceptedEventNames;
        this.rejectedEventNames = rejectedEventNames;
        this.inclusiveProcessNames = inclusiveProcessNames;
        this.includedEventIds = includedEventIds;
        this.excludedEventIds = excludedEventIds;
    }

    public TraceEventProviderOptions TraceEventProviderOptions
    {
        get
        {
            return new TraceEventProviderOptions()
            {
                EventIDsToDisable = excludedEventIds,
                EventIDsToEnable = includedEventIds,
                ProcessNameFilter = inclusiveProcessNames.ToList()
            };
        }
    }

    public bool IsKernelSession
    {
        get
        {
            return (sessionName == KernelTraceEventParser.KernelSessionName);
        }
    }

    public bool CanProviderNameAccepted(string providerName)
    {
        if (rejectedEventProviderNames != null && rejectedEventProviderNames.Contains(providerName)) return false;
        if (acceptedEventProviderNames != null && acceptedEventProviderNames.Contains(providerName)) return true;
        return false;
    }

    public bool CanEventNameAccepted(string eventName)
    {
        if (rejectedEventNames != null && rejectedEventNames.Contains(eventName)) return false;
        if (acceptedEventNames != null && acceptedEventNames.Contains(eventName)) return true;
        return false;
    }

    public bool IsEventIdIncluded(int eventId)
    {
        if (excludedEventIds != null && excludedEventIds.Contains(eventId)) return false;
        if (includedEventIds != null && includedEventIds.Contains(eventId)) return true;
        return false;
    }

    public readonly string providerName;
    public readonly Type eventParserType;
    public readonly string sessionName;
    public readonly TraceEventLevel providerLevel;
    public readonly string[] acceptedEventProviderNames;
    public readonly string[] rejectedEventProviderNames;
    public readonly string[] acceptedEventNames;
    public readonly string[] rejectedEventNames;
    public readonly string[] inclusiveProcessNames;
    public readonly int[] includedEventIds = null;
    public readonly int[] excludedEventIds = null;
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
            typeof(ClrTraceEventParser),
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

public class TestTraceSession
{
    public readonly TraceSessionAttribute SessionAttribute;
    public readonly Thread TestTraceThread;
    public readonly Guid Id;


    private TraceEventSession traceEventSession;
    private TraceEventParser traceEventParser;
    private readonly int mainTestThreadId;

    public static TestTraceSession Create(TraceSessionAttribute sessionAttribute, int mainTestThreadId)
    {
        return new TestTraceSession(sessionAttribute, mainTestThreadId);
    }

    private TestTraceSession()
    {

    }


    private TestTraceSession(TraceSessionAttribute sessionAttribute, int mainTestThreadId)
    {
        SessionAttribute = sessionAttribute;
        this.mainTestThreadId = mainTestThreadId;
        Id = Guid.NewGuid();

        traceEventSession = new TraceEventSession(sessionAttribute.sessionName, sessionAttribute.IsKernelSession ? TraceEventSessionOptions.NoRestartOnCreate : TraceEventSessionOptions.Create)
        {
            BufferSizeMB = 128,
            CpuSampleIntervalMSec = 10,
        };

        TestTraceThread = new Thread(TraceListener)
        {
            Priority = ThreadPriority.Lowest,
            IsBackground = true,
            Name = "TestTraceThread_" + Id.ToString().ToLower()
        };
    }

    private void TraceListener()
    {
        try
        {
            if (SessionAttribute.IsKernelSession)
            {
                if (SessionAttribute is KernelTraceSessionAttribute)
                {
                    traceEventSession.EnableKernelProvider(((KernelTraceSessionAttribute)SessionAttribute).KernelSessionKeywords);
                }
                else
                {
                    traceEventSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Default);
                }
                traceEventParser = new KernelTraceEventParser(traceEventSession.Source);
            }
            else
            {
                traceEventSession.EnableProvider(SessionAttribute.providerName, SessionAttribute.providerLevel, options: SessionAttribute.TraceEventProviderOptions);
                traceEventParser = Activator.CreateInstance(SessionAttribute.eventParserType) as TraceEventParser;
            }


            traceEventParser.AddCallbackForProviderEvents((providerName, eventName) =>
            {
                if (SessionAttribute.CanProviderNameAccepted(providerName) && SessionAttribute.CanEventNameAccepted(eventName))
                {
                    return EventFilterResponse.AcceptEvent;
                }
                else
                {
                    return EventFilterResponse.RejectEvent;
                }
            }, (TraceEvent evt) =>
            {
                if (evt.ThreadID == mainTestThreadId)
                {
                    //accepted;
                }
            });
            traceEventSession.Source.Process();
        }
        catch (ThreadAbortException)
        {
            traceEventSession.Source.StopProcessing();
        }
    }

}