using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TTE.Core.TraceAssert;
using TTE.Core.TraceSession;


namespace TTE.Core
{
    public struct TestResult<T>
    {
        public T Result { get; set; }
        public Exception Exception { get; set; }
    }

    public class TestRunner
    {
        public TestResult<T> RunTest<T>(Func<T> testMethod, Attribute[] attributes)
        {
            T testResult = default(T);

            try
            {
                if (attributes == null) attributes = new Attribute[0];

                //These two are used to sync the test thread and the watcher threads
                ManualResetEvent mainTestStartedEvent = new ManualResetEvent(false);
                ManualResetEvent mainTestStoppedEvent = new ManualResetEvent(false);

                long testStarted = 0;

                //this thread is the main test thread, it is created first
                //and waits until all watcher threads are ready
                Thread testThread = new Thread(() =>
                {
                    Thread.CurrentThread.Name = "Test Thread";
                    mainTestStartedEvent.WaitOne();
                    testStarted = DateTime.Now.Ticks;
                    testResult = testMethod.Invoke();
                });

                //we need main test thread native OS Id to filter the ETW events
                testThread.Start();
                int mainTestThreadId = Utility.GetNativeThreadId(testThread);

                //start of the trace sessions preparation
                List<TraceSessionAttribute> traceSessionAttributes = attributes
                    .FilterBySub<Attribute, TraceSessionAttribute>()
                    .Where(attr => !(attr is ThreadStopKernelTraceSession))
                    .ToList();

                traceSessionAttributes.Add(new ThreadStopKernelTraceSession());

                List<TestTraceSession> traceSessions = new List<TestTraceSession>();

                //start of the trace asserters preparation
                List<TraceAssertAttribute> traceAssertAttributes = attributes
                    .FilterBySub<Attribute, TraceAssertAttribute>()
                    .ToList();

                //this is used to sync all watchers before the test is started
                CountdownEvent traceSessionReadyCounter = new CountdownEvent(traceSessionAttributes.Count());

                //this is used to store all collected ETW events
                ConcurrentBag<TraceEvent> traceEvents = new ConcurrentBag<TraceEvent>();

                //trace watcher sessions is getting created
                foreach (TraceSessionAttribute attr in traceSessionAttributes)
                {
                    var ts = new TestTraceSession(attr, mainTestThreadId, mainTestStartedEvent, mainTestStoppedEvent, traceSessionReadyCounter);
                    ts.TraceFetched += (evt) => traceEvents.Add(evt.Clone());
                    ts.TraceSessionError += (ex) => throw ex;

                    if (attr is ThreadStopKernelTraceSession) ts.TestThreadStopped += (evt) =>
                    {
                        TimeSpan duration = TimeSpan.FromTicks(DateTime.Now.Ticks - testStarted);
                        if (duration.TotalMilliseconds < 10000) Thread.Sleep((int)(10000 - duration.TotalMilliseconds));
                        mainTestStoppedEvent.Set();
                    };

                    traceSessions.Add(ts);
                }

                traceSessions.ForEach(ts=>ts.StartSession());

                //wait 10 sec to ready all trace watcher sessions to be ready
                traceSessionReadyCounter.Wait(10000);

                //start the main test thread, 
                //then wait until the thread kill event to be captured
                mainTestStartedEvent.Set();
                testThread.Join();
                mainTestStoppedEvent.WaitOne();


                //after the test finishes, sort all the event captured
                List<TraceEvent> fetchedEvents = traceEvents.OrderBy(evt => evt.TimeStamp.Ticks).ToList();

                //run assertions one by one with the events collected
                foreach (TraceAssertAttribute attr in traceAssertAttributes)
                {
                    attr.Assert(fetchedEvents);
                }

                return new TestResult<T> { Result = testResult };
            }
            catch (Exception ex)
            {
                return new TestResult<T> { Result = testResult, Exception = ex };
            }
        }
    }
}
