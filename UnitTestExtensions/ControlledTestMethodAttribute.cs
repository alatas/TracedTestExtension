using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ControlledTestMethodAttribute : TestMethodAttribute
{

    public ControlledTestMethodAttribute()
    {
    }

    string debugOutput = "";

    public override TestResult[] Execute(ITestMethod testMethod)
    {
        TestResult[] ret = null;

        var mainTestStartedEvent = new ManualResetEvent(false);
        var mainTestStoppedEvent = new ManualResetEvent(false);

        Thread testThread = new Thread(() =>
        {
            Thread.CurrentThread.Name = "Test Thread";
            mainTestStartedEvent.WaitOne();
            ret = base.Execute(testMethod);
        });

        testThread.Start();
        int mainTestThreadId = Utility.GetNativeThreadId(testThread);
        List<TestTraceSession> traceSessions = new List<TestTraceSession>();
        List<TraceSessionAttribute> traceSessionAttributes = testMethod.GetAttributes<TraceSessionAttribute>(true).Where(attr => !(attr is ThreadStopKernelTraceSession)).ToList();
        traceSessionAttributes.Add(new ThreadStopKernelTraceSession());
        CountdownEvent traceSessionReadyCounter = new CountdownEvent(traceSessionAttributes.Count());

        ConcurrentBag<TraceEvent> traceEvents = new ConcurrentBag<TraceEvent>();

        foreach (TraceSessionAttribute attr in traceSessionAttributes)
        {
            var ts = new TestTraceSession(attr, mainTestThreadId, mainTestStartedEvent, mainTestStoppedEvent, traceSessionReadyCounter);
            ts.TraceFetched += (evt) => traceEvents.Add(evt.Clone());
            if (attr is ThreadStopKernelTraceSession) ts.TestThreadStopped += (evt) => mainTestStoppedEvent.Set();
            traceSessions.Add(ts);
        }

        traceSessionReadyCounter.Wait(30000);
        mainTestStartedEvent.Set();
        testThread.Join();
        mainTestStoppedEvent.WaitOne();

        traceEvents.OrderBy(evt => evt.TimeStamp.Ticks).ToList().ForEach(evt => debugOutput += "\n" + HandleEvent(evt));
        ret[0].DebugTrace += debugOutput;
        return ret;
    }

    private void TraceFetched(TraceEvent fetchedEvent)
    {
        debugOutput += "\n" + HandleEvent(fetchedEvent);
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

    //public static ProcessThread GetProcessThread(Thread thread)
    //{
    //    int nativeThreadId = GetNativeThreadId(thread);
    //    foreach (ProcessThread th in Process.GetCurrentProcess().Threads)
    //    {
    //        if (th.Id == nativeThreadId)
    //        {
    //            return th;
    //        }
    //    }
    //    return null;
    //}
}



