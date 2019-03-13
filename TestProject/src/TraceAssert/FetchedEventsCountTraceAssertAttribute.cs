using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class FetchedEventsCountTraceAssertAttribute : TraceAssertAttribute
{
    public readonly string EventName = null;
    public readonly int EventId;
    public readonly int Count;
    public readonly Comparison CountComparison;


    public FetchedEventsCountTraceAssertAttribute(string EventName, int Count, Comparison comparison)
    {
        this.EventName = EventName;
        this.Count = Count;
        CountComparison = comparison;
    }

    public FetchedEventsCountTraceAssertAttribute(int EventId, int Count, Comparison comparison)
    {
        this.EventId = EventId;
        this.Count = Count;
        CountComparison = comparison;
    }

    public override void Assert(List<TraceEvent> traceEvent)
    {
        int actual;
        if (EventName == null)
        {
            actual = traceEvent.Where(evt => ((ushort)EventId).Equals(evt.ID)).Count();
        }
        else
        {
            actual = traceEvent.Where(evt => evt.EventName == EventName).Count();
        }

        if (!Compare(Count, actual, CountComparison))
        {
            throw new Exception($"Fetched Events Count Trace Assert is Failed: expected {Count}, actual {actual}, comparison {Enum.GetName(typeof(Comparison), CountComparison)}");
        }
    }

    private bool Compare(int expected, int actual, Comparison comparison)
    {
        switch (comparison)
        {
            case Comparison.equal:
                return (expected == actual);
            case Comparison.notEqual:
                return (expected != actual);
            case Comparison.greaterThan:
                return actual > expected;
            case Comparison.greaterOrEqualThan:
                return actual >= expected;
            case Comparison.lowerThan:
                return actual < expected;
            case Comparison.lowerOrEqualThan:
                return actual <= expected;
            default:
                return false;
        }
    }

   
}
