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
    public class TestRunner
    {
        public T RunTest<T>(Func<T> testMethod, Attribute[] attributes)
        {
            T testResult = default(T);
            if (attributes == null) attributes = new Attribute[0];

            ManualResetEvent mainTestStartedEvent = new ManualResetEvent(false);
            ManualResetEvent mainTestStoppedEvent = new ManualResetEvent(false);
            long testStarted = 0;
            Thread testThread = new Thread(() =>
            {
                Thread.CurrentThread.Name = "Test Thread";
                mainTestStartedEvent.WaitOne();
                testStarted = DateTime.Now.Ticks;
                testResult = testMethod.Invoke();
            });

            testThread.Start();
            int mainTestThreadId = Utility.GetNativeThreadId(testThread);


            List<TraceSessionAttribute> traceSessionAttributes = attributes
                .FilterBySub<Attribute, TraceSessionAttribute>()
                .Where(attr => !(attr is ThreadStopKernelTraceSession))
                .ToList();

            List<TraceAssertAttribute> traceAssertAttributes = attributes
                .FilterBySub<Attribute, TraceAssertAttribute>()
                .ToList();

            List<TestTraceSession> traceSessions = new List<TestTraceSession>();
            traceSessionAttributes.Add(new ThreadStopKernelTraceSession());

            CountdownEvent traceSessionReadyCounter = new CountdownEvent(traceSessionAttributes.Count());

            ConcurrentBag<TraceEvent> traceEvents = new ConcurrentBag<TraceEvent>();

            foreach (TraceSessionAttribute attr in traceSessionAttributes)
            {
                var ts = new TestTraceSession(attr, mainTestThreadId, mainTestStartedEvent, mainTestStoppedEvent, traceSessionReadyCounter);
                ts.TraceFetched += (evt) => traceEvents.Add(evt.Clone());


                if (attr is ThreadStopKernelTraceSession) ts.TestThreadStopped += (evt) =>
                {
                    TimeSpan duration = TimeSpan.FromTicks(DateTime.Now.Ticks - testStarted);
                    if (duration.TotalMilliseconds < 1000) Thread.Sleep((int)(1000 - duration.TotalMilliseconds));
                    mainTestStoppedEvent.Set();
                };

                traceSessions.Add(ts);
            }

            traceSessionReadyCounter.Wait(10000);
            mainTestStartedEvent.Set();
            testThread.Join();
            mainTestStoppedEvent.WaitOne();

            List<TraceEvent> fetchedEvents = traceEvents.OrderBy(evt => evt.TimeStamp.Ticks).ToList();


            traceAssertAttributes.ForEach(attr => attr.Assert(fetchedEvents));

            //#if DEBUG
            //            string debugOutput = "";
            //            traceEvents.OrderBy(evt => evt.TimeStamp.Ticks).ToList().ForEach(evt => debugOutput += "\n" + HandleEvent(evt));
            //            ret[0].DebugTrace += debugOutput;
            //#endif
            return testResult;
        }
    }
}
