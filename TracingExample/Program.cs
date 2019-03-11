using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Threading;

class Program
{
    static void Main()
    {
        TraceEventSession _kernelSession;
        KernelTraceEventParser _kernelParser;
        TraceEventSession _customSession;
        ClrTraceEventParser _clrParser;

        Thread _processingThread;

        _kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName, TraceEventSessionOptions.NoRestartOnCreate)
        {
            BufferSizeMB = 128,
            CpuSampleIntervalMSec = 10,
        };

        _customSession = new TraceEventSession("CustomSession")
        {
            BufferSizeMB = 128,
            CpuSampleIntervalMSec = 10
        };



        _processingThread = new Thread(() =>
        {
            _customSession.EnableProvider(ClrTraceEventParser.ProviderName);
            _clrParser = new ClrTraceEventParser(_customSession.Source);
            _clrParser.AddCallbackForProviderEvents((a, b) =>
            {
                Console.WriteLine(a + " / " + b);
                return EventFilterResponse.AcceptEvent;
            }, HandleEvent);

            _kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.All);
            _kernelParser = new KernelTraceEventParser(_kernelSession.Source);
            //_kernelParser.FileIOWrite += obj => HandleEvent(obj);
            //_kernelParser.AddCallbackForProviderEvents((a, b) =>
            //{
            //    Console.WriteLine(a + " / " + b);
            //    return EventFilterResponse.AcceptEvent;
            //}, HandleEvent);
            _customSession.Source.Process(); 
            _kernelSession.Source.Process();
        })
        {
            Priority = ThreadPriority.Lowest,
            IsBackground = true,
            Name = "Processing Thread"
        };

        _processingThread.Start();

        Console.ReadKey();
    }

    static void HandleEvent(TraceEvent evt)
    {
        if (evt is FileIOReadWriteTraceData)
        {
            var fevt = (FileIOReadWriteTraceData)evt;
            Console.WriteLine($"{fevt.EventName} - {fevt.ProcessName} - {fevt.ThreadID} - {fevt.FileName}");
        }
        else
        {
            Console.WriteLine($"{evt.EventName} - {evt.ProcessName} - {evt.ThreadID} - {evt.ProcessID}");
        }
    }
}

class observer : IObserver<FileIOReadWriteTraceData>
{
    public void OnCompleted()
    {
        // throw new NotImplementedException();
    }

    public void OnError(Exception error)
    {
        //throw new NotImplementedException();
    }

    public void OnNext(FileIOReadWriteTraceData value)
    {
        Console.WriteLine("Process Started: Name {0} CmdLine {1}",
             value.ProcessName, value.FileName);
    }
}