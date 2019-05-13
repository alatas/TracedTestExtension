using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Diagnostics;
using System.Threading;

namespace TTE.Core.TraceSession
{
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

        public delegate void TraceSessionErrorDelegate(Exception exception);
        public event TraceSessionErrorDelegate TraceSessionError;

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

            //trace event session is created by the trace session properties
            traceEventSession = new TraceEventSession(SessionAttribute.sessionName, SessionAttribute.IsKernelSession ? TraceEventSessionOptions.NoRestartOnCreate : TraceEventSessionOptions.Create)
            {
                BufferSizeMB = 128,
                CpuSampleIntervalMSec = 10,
            };

            //main session listener thread is created
            TestTraceThread = new Thread(TraceListener)
            {
                Priority = ThreadPriority.Lowest,
                IsBackground = true,
                Name = "TestTraceThread_" + Id.ToString().ToLower()
            };

            //main session listener terminator thread is also created 
            //to terminate the main listener thread when the stop signal is set
            TestTraceTerminatorThread = new Thread(TerminateTraceSession)
            {
                Priority = ThreadPriority.Lowest,
                IsBackground = true,
                Name = "TestTraceThread_" + Id.ToString().ToLower() + "_Terminator"
            };
        }

        public void StartSession()
        {
            TestTraceThread.Start();
            TestTraceTerminatorThread.Start();
        }

        private void TerminateTraceSession()
        {
            //wait until the stop signal is set, 
            //then abort the main listener thread of this session
            mainTestStoppedEvent.WaitOne();
            if (TestTraceThread != null) TestTraceThread.Abort();
        }

        private void TraceListener()
        {
            try
            {
                //listener setups the providers and parser first
                SetupProviderAndParser();

                //then signal the main runner that I'm ready to listen
                traceSessionReadyCounter.Signal();

                //wait the start event
                mainTestStartedEvent.WaitOne();

                //start to listen after the start signal is set
                traceEventSession.Source.Process();
            }
            catch (ThreadAbortException)
            {
                //when thread is aborted, it stops listening
                traceEventSession.Source.StopProcessing();
            }
            catch (Exception ex)
            {
                TraceSessionError(ex);
            }
        }

        private void SetupProviderAndParser()
        {
            try
            {
                //enabling the providers
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

                //creating the trace event parser
                traceEventParser = Activator.CreateInstance(SessionAttribute.eventParserType, traceEventSession.Source) as TraceEventParser;

                //set the trace event parser through session's properties
                traceEventParser.AddCallbackForProviderEvents(CallbackForProviderEvents, CallbackForEvents);
            }
            catch (Exception ex)
            {
                TraceSessionError(ex);
            }
        }

        private EventFilterResponse CallbackForProviderEvents(string providerName, string eventName)

        {
            if (SessionAttribute.CanProviderNameAccepted(providerName) && SessionAttribute.CanEventNameAccepted(eventName))
            {
                return EventFilterResponse.AcceptEvent;
            }
            else
            {
                return EventFilterResponse.RejectEvent;
            }
        }

        private void CallbackForEvents(TraceEvent evt)
        {
            if (evt.ThreadID == mainTestThreadId || SessionAttribute.IsProcessTracing(evt.ProcessName) || (SessionAttribute.includeOwnProcess && evt.ProcessID == Process.GetCurrentProcess().Id))
            {
                //returning the fetched event to main runner
                TraceFetched(evt);
            }

            if (evt.ThreadID == mainTestThreadId && evt.EventName == "Thread/Stop" && SessionAttribute is ThreadStopKernelTraceSession)
            {
                //this is a special event to realize the main test thread is stopped
                TestThreadStopped(evt);
            }
        }
    }
}