using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;

namespace UnitTestExtensions
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

        [ControlledTestMethod]
        public void TestMethod2()
        {
            int[] buffer = new int[(int)Math.Pow(2, 24)];
            for (int i = 1; i < Math.Pow(2, 24); i++)
            {
                Random r = new Random();
                buffer[i] = r.Next(i);
            }
        }

        [ControlledTestMethod]
        public void TestMethod3()
        {
            Assert.That.StartTimer();
            for (int i = 1; i < Math.Pow(2, 22); i++)
            {
                Random r = new Random();
                int a = r.Next(i);
            }
            Assert.That.IsTimedOut(3000);
        }

        [ControlledTestMethod]
        [KernelTraceSession(KernelTraceEventParser.Keywords.All, acceptedEventNames: new string[] { "FileIO/Write" })]
        [ClrTraceSession]
        [TraceAssert(typeof(TraceEventCountAssertRule))]
        public void TestMethod4()
        {
            using (FileStream stream = new FileStream("testfile.txt", FileMode.Create))
            {
                Thread.Sleep(2000);
                stream.WriteByte(200);
                stream.Seek(99, SeekOrigin.Begin);
                stream.WriteByte(201);
                stream.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[100];
                stream.Read(buffer, 0, 100);
                stream.Seek(0, SeekOrigin.End);
                stream.Write(buffer, 0, 100);
                //Thread.Sleep(5000);
            }
        }

        [ControlledTestMethod]
        [KernelTraceSession(acceptedEventNames: new string[] { "FileIO", "DriveIO" })]
        [ClrTraceSession]
        public void TestMethod5()
        {
        }
    }
}
