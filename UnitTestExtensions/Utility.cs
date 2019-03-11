using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

/*
     private static ClrThread GetClrThread(Thread t)
        {

            ClrRuntime runtime = GetClrRuntime();
            foreach (ClrThread thread in runtime.Threads)
            {
                if (thread.ManagedThreadId == t.ManagedThreadId)
                {
                    return thread;
                }
            }

            return null;
        }


        private static ClrRuntime GetClrRuntime()
        {
            using (DataTarget target = DataTarget.AttachToProcess(Process.GetCurrentProcess().Id, 5000, AttachFlag.Passive))
            {
                return target.ClrVersions.First().CreateRuntime();
            }
        }
     
     */

namespace Utility
{
    public static class MiniDump
    {
        [DllImport("DbgHelp.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        private static extern bool MiniDumpWriteDump(
                                    IntPtr hProcess,
                                    Int32 processId,
                                    IntPtr fileHandle,
                                    MiniDumpType dumpType,
                                    ref MinidumpExceptionInfo excepInfo,
                                    IntPtr userInfo,
                                    IntPtr extInfo);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern int GetCurrentThreadId();


        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        struct MinidumpExceptionInfo
        {
            public int ThreadId;
            public IntPtr ExceptionPointers;
            public bool ClientPointers;
        }

        public static bool TryDump(string dmpPath, MiniDumpType dmpType, int ThreadId)
        {
            using (FileStream stream = new FileStream(dmpPath, FileMode.Create))
            {
                Process process = Process.GetCurrentProcess();

                MinidumpExceptionInfo mei = new MinidumpExceptionInfo
                {
                    ThreadId = GetCurrentThreadId(),
                    ExceptionPointers = Marshal.GetExceptionPointers(),
                    ClientPointers = true
                };

                bool res = MiniDumpWriteDump(
                                    process.Handle,
                                    process.Id,
                                    stream.SafeFileHandle.DangerousGetHandle(),
                                    dmpType,
                                    ref mei,
                                    IntPtr.Zero,
                                    IntPtr.Zero);

                stream.Flush();
                stream.Close();

                if (!res)
                {
                    uint err = GetLastError();

                }

                return res;
            }
        }
    }

    public enum MiniDumpType
    {
        None = 0x00010000,
        Normal = 0x00000000,
        WithDataSegs = 0x00000001,
        WithFullMemory = 0x00000002,
        WithHandleData = 0x00000004,
        FilterMemory = 0x00000008,
        ScanMemory = 0x00000010,
        WithUnloadedModules = 0x00000020,
        WithIndirectlyReferencedMemory = 0x00000040,
        FilterModulePaths = 0x00000080,
        WithProcessThreadData = 0x00000100,
        WithPrivateReadWriteMemory = 0x00000200,
        WithoutOptionalData = 0x00000400,
        WithFullMemoryInfo = 0x00000800,
        WithThreadInfo = 0x00001000,
        WithCodeSegs = 0x00002000
    }
}