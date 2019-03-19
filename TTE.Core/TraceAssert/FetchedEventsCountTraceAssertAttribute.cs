using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TTE.Core.TraceAssert
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class FetchedEventsCountTraceAssertAttribute : TraceAssertAttribute
    {
        public readonly string EventName = null;
        public readonly int EventId;
        public readonly int Count;
        public readonly DecimalComparison CountComparison;


        public FetchedEventsCountTraceAssertAttribute(string EventName, int Count, DecimalComparison comparison)
        {
            this.EventName = EventName;
            this.Count = Count;
            CountComparison = comparison;
        }

        public FetchedEventsCountTraceAssertAttribute(int EventId, int Count, DecimalComparison comparison)
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
                throw new Exception($"Fetched Events Count Trace Assert is Failed: expected {Count}, actual {actual}, comparison {Enum.GetName(typeof(DecimalComparison), CountComparison)}");
            }
        }

        private bool Compare(int expected, int actual, DecimalComparison comparison)
        {
            switch (comparison)
            {
                case DecimalComparison.equal:
                    return (expected == actual);
                case DecimalComparison.notEqual:
                    return (expected != actual);
                case DecimalComparison.greaterThan:
                    return actual > expected;
                case DecimalComparison.greaterOrEqualThan:
                    return actual >= expected;
                case DecimalComparison.lowerThan:
                    return actual < expected;
                case DecimalComparison.lowerOrEqualThan:
                    return actual <= expected;
                default:
                    return false;
            }
        }


    }
}