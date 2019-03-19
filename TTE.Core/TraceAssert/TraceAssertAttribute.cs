using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;

namespace TTE.Core.TraceAssert
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public abstract class TraceAssertAttribute : Attribute
    {
        public abstract void Assert(List<TraceEvent> traceEvent);
    }
    public enum DecimalComparison
    {
        equal,
        notEqual,
        greaterThan,
        greaterOrEqualThan,
        lowerThan,
        lowerOrEqualThan
    }
}