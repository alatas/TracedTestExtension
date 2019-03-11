using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ControlledTestMethodAttribute : TestMethodAttribute
{

    public ControlledTestMethodAttribute()
    {
    }

    public ControlledTestMethodAttribute(params Type[] t)
    {

    }
    public override TestResult[] Execute(ITestMethod testMethod)
    {
        TestResult[] ret = null;

        var manResetEvent = new ManualResetEvent(false);
        string debugOutput = "";


        Thread testThread = new Thread(() =>
        {
            Thread.CurrentThread.Name = "Test Thread";
            manResetEvent.WaitOne();
            ret = base.Execute(testMethod);
        });

        testThread.Start();
        long testWorkingSet;
        Thread watcherThread = new Thread(() =>
       {
           Thread.CurrentThread.Name = "Test WatchDog Thread";
           testWorkingSet = Process.GetCurrentProcess().WorkingSet64;
           manResetEvent.WaitOne();
           while (true)
           {
               Thread.Sleep(3000);
               ProcessThread testProcessThread = GetProcessThread(testThread);
               var CPUTime = Math.Round(testProcessThread.TotalProcessorTime.TotalSeconds, 2);
               var UserCPU = Math.Round(((decimal)testProcessThread.UserProcessorTime.Ticks / testProcessThread.TotalProcessorTime.Ticks * 100) / Environment.ProcessorCount, 1);
               var KernelCPU = Math.Round((((decimal)testProcessThread.PrivilegedProcessorTime.Ticks / testProcessThread.TotalProcessorTime.Ticks) * 100 / Environment.ProcessorCount), 1);
               var TotalCPU = UserCPU + KernelCPU;
               long DeltaWorkingSet = Process.GetCurrentProcess().WorkingSet64 - testWorkingSet;
               debugOutput += "\n" + "User: %" + UserCPU + " Kernel: %" + KernelCPU + " Total: %" + TotalCPU + " DeltaWorkingSet: " + DeltaWorkingSet + "bytes";
           }
       })
        {
            Priority = ThreadPriority.Lowest,
            IsBackground = true
        };


        TraceEventSession _kernelSession;
        KernelTraceEventParser _kernelParser;
        TraceEventSession _customSession;
        ClrTraceEventParser _clrParser;

        _kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName, TraceEventSessionOptions.NoRestartOnCreate)
        {
            BufferSizeMB = 128,
            CpuSampleIntervalMSec = 10,
        };

        _customSession = new TraceEventSession("CLRSession")
        {
            BufferSizeMB = 128,
            CpuSampleIntervalMSec = 10
        };

        Thread kernelTraceThread = new Thread(() =>
        {
            try
            {
                Thread.CurrentThread.Name = "Test Kernel Trace Thread";
                int testThreadId = GetNativeThreadId(testThread);

                _kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.All);
                _kernelParser = new KernelTraceEventParser(_kernelSession.Source);
                _kernelParser.AddCallbackForProviderEvents((a, b) =>
                {
                    if (b.StartsWith("FileIO"))
                    {
                        return EventFilterResponse.AcceptEvent;
                    }
                    else
                    {
                        return EventFilterResponse.RejectEvent;
                    }
                }, (evt) =>
                {
                    if (evt.ThreadID == testThreadId)
                    {
                        debugOutput += "\n" + HandleEvent(evt);
                    }
                });
                _kernelSession.Source.Process();
            }
            catch (ThreadAbortException)
            {
                _kernelSession.Source.StopProcessing();
            }
        })
        {
            Priority = ThreadPriority.Lowest,
            IsBackground = true
        };

        Thread clrTraceThread = new Thread(() =>
        {
            try
            {
                Thread.CurrentThread.Name = "Test Trace Thread";
                int testThreadId = GetNativeThreadId(testThread);

                _customSession.EnableProvider(ClrTraceEventParser.ProviderName);
                _clrParser = new ClrTraceEventParser(_customSession.Source);
                _clrParser.AddCallbackForProviderEvents((a, b) =>
                {
                    if (b.StartsWith("FileIO"))
                    {
                        return EventFilterResponse.AcceptEvent;
                    }
                    else
                    {
                        return EventFilterResponse.RejectEvent;
                    }

                }, (evt) =>
                {
                    if (evt.ThreadID == testThreadId)
                    {
                        debugOutput += "\n" + HandleEvent(evt);
                    }
                });

                _customSession.Source.Process();

            }
            catch (ThreadAbortException)
            {
                _customSession.Source.StopProcessing();
            }
        })
        {
            Priority = ThreadPriority.Lowest,
            IsBackground = true
        };

        watcherThread.Start();
        kernelTraceThread.Start();
        clrTraceThread.Start();
        Thread.Sleep(5000);
        manResetEvent.Set();
        testThread.Join();
        watcherThread.Abort();

        kernelTraceThread.Abort();

        ret[0].DebugTrace += debugOutput;
        return ret;
    }

    public static int GetNativeThreadId(Thread thread)
    {
        var f = typeof(Thread).GetField("DONT_USE_InternalThread", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
        var pInternalThread = (IntPtr)f.GetValue(thread);
        var nativeId = Marshal.ReadInt32(pInternalThread, (IntPtr.Size == 8) ? 0x022C : 0x0160);
        return nativeId;
    }

    static string HandleEvent(TraceEvent evt)
    {
        try
        {
            if (evt is FileIOReadWriteTraceData fevt)
            {
                return $"{fevt.EventName} - {fevt.ProcessName} - {fevt.ThreadID} - {fevt.FileName} - {fevt.Offset} - {fevt.IoSize}";
            }
            else if (evt is FileIOCreateTraceData cevt)
            {
                return $"{cevt.EventName} - {cevt.ProcessName} - {cevt.ThreadID} - {cevt.FileName}";
            }
            else
            {
                return $"{evt.EventName} - {evt.ProcessName} - {evt.ThreadID}";
            }
        }
        catch (Exception ex)
        {

            return "Exception: " + ex.Message;
        }

    }

    public static ProcessThread GetProcessThread(Thread thread)
    {
        int nativeThreadId = GetNativeThreadId(thread);
        foreach (ProcessThread th in Process.GetCurrentProcess().Threads)
        {
            if (th.Id == nativeThreadId)
            {
                return th;
            }
        }
        return null;
    }
}



