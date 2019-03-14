using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TracedTestMethodAttribute : TestMethodAttribute
{

    public TracedTestMethodAttribute()
    {
    }

    public override TestResult[] Execute(ITestMethod testMethod)
    {
        TestResult[] ret = null;

        ManualResetEvent mainTestStartedEvent = new ManualResetEvent(false);
        ManualResetEvent mainTestStoppedEvent = new ManualResetEvent(false);
        long testStarted = 0;
        Thread testThread = new Thread(() =>
        {
            Thread.CurrentThread.Name = "Test Thread";
            mainTestStartedEvent.WaitOne();
            testStarted = DateTime.Now.Ticks;
            ret = base.Execute(testMethod);
        });

        testThread.Start();
        int mainTestThreadId = Utility.GetNativeThreadId(testThread);


        List<TraceSessionAttribute> traceSessionAttributes = testMethod
            .GetAttributes<TraceSessionAttribute>(true)
            .Where(attr => !(attr is ThreadStopKernelTraceSession))
            .ToList();

        List<TraceAssertAttribute> traceAssertAttributes = testMethod.GetAttributes<TraceAssertAttribute>(true).ToList();

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

#if DEBUG
        string debugOutput = "";
        traceEvents.OrderBy(evt => evt.TimeStamp.Ticks).ToList().ForEach(evt => debugOutput += "\n" + HandleEvent(evt));
        ret[0].DebugTrace += debugOutput;
#endif
        return ret;
    }

    static string HandleEvent(TraceEvent evt)
    {
        try
        {
            if (evt is FileIOReadWriteTraceData fevt)
            {
                return $"{fevt.TimeStamp.ToString("o")} {fevt.EventName} - {fevt.ProcessName} - {fevt.ThreadID} - {fevt.FileName} - {fevt.Offset} - {fevt.IoSize}";
            }
            else if (evt is FileIOCreateTraceData cevt)
            {
                return $"{cevt.TimeStamp.ToString("o")} {cevt.EventName} - {cevt.ProcessName} - {cevt.ThreadID} - {cevt.FileName}";
            }
            else if (evt is VirtualAllocTraceData mevt)
                return $"{mevt.TimeStamp.ToString("o")} {mevt.EventName} - {mevt.ProcessName} - {mevt.ThreadID} -l {mevt.Length} -a {mevt.BaseAddr}";
            else
            {
                return $"{evt.TimeStamp.ToString("o")} {evt.EventName} - {evt.ProcessName} - {evt.ThreadID}";
            }
        }
        catch (Exception ex)
        {

            return "Exception: " + ex.Message;
        }
    }
}