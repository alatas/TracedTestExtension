using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using System;
using System.Collections;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TraceAssertAttribute : Attribute
{
    public TraceAssertAttribute(Type traceAssertRule) 
    {

    }
}

class TraceEventCountAssertRule : ITraceAssertRule
{
    public bool Result(List<TraceEvent> traceEvent)
    {
        throw new NotImplementedException();
    }
}

public interface ITraceAssertRule
{
    bool Result(List<TraceEvent> traceEvent);
}


public class TraceAssertRules : ITraceAssertRule, IEnumerable<ITraceAssertRule>
{
    public IEnumerator<ITraceAssertRule> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public bool Result(List<TraceEvent> traceEvent)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}