using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
            TestResult<TestResult[]> testResult = testRunner.RunTest(() => base.Execute(testMethod),
                testMethod.MethodInfo.GetCustomAttributes(true).FilterBySub<object, Attribute>().ToArray());

            if (testResult.Exception != null)
            {
                testResult.Result[0].TestFailureException = testResult.Exception;
                testResult.Result[0].Outcome = UnitTestOutcome.Error;
            }
                
            //testResults[0].Duration = TimeSpan.FromSeconds(1);
            //testResults[0].LogError = "Test Error";
            //testResults[0].LogOutput = "Test Output";
            //testResults[0].DebugTrace  = "Test Trace";
            //testResults[0].TestContextMessages = "Test Context";
            //testResults[0].ResultFiles = new List<string>() { "C:\\windows\\notepad.exe" };

            return testResult.Result;
        }
    }
}
