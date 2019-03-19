using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TTE.Core;

namespace TTE.MSTest
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TracedTestMethodAttribute : TestMethodAttribute
    {
        TestRunner testRunner;

        public TracedTestMethodAttribute()
        {
            testRunner = new TestRunner();
        }

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            TestResult[] testResults = testRunner.RunTest(() => base.Execute(testMethod), 
                testMethod.MethodInfo.GetCustomAttributes(true).FilterBySub<object, Attribute>().ToArray());
            //testResults[0].Duration = TimeSpan.FromSeconds(1);
            //testResults[0].LogError = "Test Error";
            //testResults[0].LogOutput = "Test Output";
            //testResults[0].DebugTrace  = "Test Trace";
            //testResults[0].TestContextMessages = "Test Context";
            //testResults[0].ResultFiles = new List<string>() { "C:\\windows\\notepad.exe" };
            //testResults[0].TestFailureException = new Exception("Test Exp");
            //testResults[0].Outcome = UnitTestOutcome.Error; 
            return testResults;
        }
    }
}
