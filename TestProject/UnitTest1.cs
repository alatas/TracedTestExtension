using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

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
            Assert.Fail("Fail Message");
        }

        [TracedTestMethod]
        public void TestMethod2()
        {
            int[] buffer = new int[(int)Math.Pow(2, 24)];
            for (int i = 1; i < Math.Pow(2, 24); i++)
            {
                Random r = new Random();
                buffer[i] = r.Next(i);
            }
        }

        [TracedTestMethod]
        public void TestMethod3()
        {
            for (int i = 1; i < Math.Pow(2, 22); i++)
            {
                Random r = new Random();
                int a = r.Next(i);
            }
        }

        [TracedTestMethod]
        [KernelTraceSession(acceptedEventNames: new string[] { "FileIO/Write" })]
        [ClrTraceSession(acceptedEventNames: new string[] { "GC/SetGCHandle" })]
        [FetchedEventsCountTraceAssert("FileIO/Write", 4, Comparison.greaterOrEqualThan)]
        [FetchedEventsCountTraceAssert("FileIO/Write", 6, Comparison.lowerThan)]
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
        [KernelTraceSession(acceptedEventNames: new string[] { "FileIO", "Memory" })]
        public void TestMethod5()
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
    }
}
