using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using TTE.Core.TraceAssert;
using TTE.Core.TraceSession;
using TTE.MSTest;

namespace TTE.Tests.MSTest.net45
{

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            int sum = 1 + 2;
            Assert.AreEqual(3, sum);
        }

        [TracedTestMethod]
        public void TestMethod2()
        {
            int sum = 1 + 2;
            Assert.AreEqual(3, sum);
        }

        [TracedTestMethod]
        public void TestMethod3()
        {
            for (int i = 1; i < Math.Pow(2, 8); i++)
            {
                Random r = new Random();
                int a = r.Next(i);
            }
        }

        [TracedTestMethod]
        [KernelTraceSession(acceptedEventNames: new string[] { "FileIO/Write" })]
        [FetchedEventsCountTraceAssert("FileIO/Write", 4, DecimalComparison.greaterOrEqualThan)]
        [FetchedEventsCountTraceAssert("FileIO/Write", 6, DecimalComparison.lowerThan)]
        [FetchedEventsSumTraceAssert("FileIO/Write", typeof(FileIOReadWriteTraceData), "IoSize", 203,
            DecimalComparison.equal)]
        public void TestMethod4()
        {
            using (FileStream stream = new FileStream("testfile.txt", FileMode.Create))
            {
                stream.WriteByte(200);
                stream.Seek(99, SeekOrigin.Begin);
                stream.WriteByte(201);
                stream.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[100];
                stream.Read(buffer, 0, 100);
                stream.Seek(0, SeekOrigin.End);
                stream.Write(buffer, 0, 100);
            }
        }

        [TracedTestMethod]
        [KernelTraceSession(acceptedEventNames: new string[] { "VirtualMem/Alloc" })]
        [FetchedEventsSumTraceAssert("VirtualMem/Alloc", typeof(VirtualAllocTraceData), "Length", 20000000, DecimalComparison.lowerOrEqualThan)]
        public void TestMethod5()
        {
            for (int i = 0; i < 100; i++)
            {
                var pv_Memory = Marshal.AllocHGlobal(0x800000);
                Thread.Sleep(100);
            }
        }

    }
}
