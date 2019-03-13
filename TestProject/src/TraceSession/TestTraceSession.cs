using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Threading;

public class TestTraceSession
{
    public readonly TraceSessionAttribute SessionAttribute;
    public readonly Thread TestTraceThread;
    public readonly Thread TestTraceTerminatorThread;

    public readonly Guid Id;

    public delegate void TraceFetchedDelegate(TraceEvent fetchedEvent);
    public event TraceFetchedDelegate TraceFetched;

    public delegate void TestThreadStoppedDelegate(TraceEvent fetchedEvent);
    public event TestThreadStoppedDelegate TestThreadStopped;

    private TraceEventSession traceEventSession;
    private TraceEventParser traceEventParser;
    private readonly int mainTestThreadId;
    private ManualResetEvent mainTestStartedEvent;
    private ManualResetEvent mainTestStoppedEvent;
    private CountdownEvent traceSessionReadyCounter;

    private TestTraceSession()
    {

    }

    public TestTraceSession(TraceSessionAttribute sessionAttribute, int mainTestThreadId, ManualResetEvent mainTestStartedEvent, ManualResetEvent mainTestStoppedEvent, CountdownEvent traceSessionReadyCounter)
    {
        SessionAttribute = sessionAttribute;
        this.mainTestThreadId = mainTestThreadId;
        this.mainTestStartedEvent = mainTestStartedEvent;
        this.mainTestStoppedEvent = mainTestStoppedEvent;
        this.traceSessionReadyCounter = traceSessionReadyCounter;
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

        TestTraceTerminatorThread = new Thread(TerminateTraceSession)
        {
            Priority = ThreadPriority.Lowest,
            IsBackground = true,
            Name = "TestTraceThread_" + Id.ToString().ToLower() + "_Terminator"
        };

        TestTraceThread.Start();
        TestTraceTerminatorThread.Start();
    }

    private void TerminateTraceSession()
    {
        mainTestStoppedEvent.WaitOne();
        if (TestTraceThread != null) TestTraceThread.Abort();
    }

    private void TraceListener()
    {
        try
        {
            SetupProviderAndParser();
            traceSessionReadyCounter.Signal();
            mainTestStartedEvent.WaitOne();
            traceEventSession.Source.Process();
        }
        catch (ThreadAbortException)
        {
            traceEventSession.Source.StopProcessing();
        }
    }

    private void SetupProviderAndParser()
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
        }
        else
        {
            traceEventSession.EnableProvider(SessionAttribute.providerName, SessionAttribute.providerLevel, options: SessionAttribute.TraceEventProviderOptions);
        }

        traceEventParser = Activator.CreateInstance(SessionAttribute.eventParserType, traceEventSession.Source) as TraceEventParser;

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
            if (evt.ThreadID == mainTestThreadId || SessionAttribute.IsProcessTracing(evt.ProcessName))
            {
                TraceFetched(evt);
            }

            if (evt.ThreadID == mainTestThreadId && evt.EventName == "Thread/Stop" && SessionAttribute is ThreadStopKernelTraceSession)
            {
                TestThreadStopped(evt);
            }
        });
    }
}