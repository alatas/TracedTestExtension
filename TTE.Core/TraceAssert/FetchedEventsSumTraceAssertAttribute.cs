using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TTE.Core.TraceAssert
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class FetchedEventsSumTraceAssertAttribute : TraceAssertAttribute
    {
        public readonly string EventName = null;
        public readonly int EventId;
        public readonly long ExpectedSum;
        public readonly DecimalComparison SumComparison;
        public readonly Type TraceEventType;
        public readonly string PropertyName;


        public FetchedEventsSumTraceAssertAttribute(string EventName, Type TraceEventType, string PropertyName, long ExpectedSum, DecimalComparison comparison)
        {
            this.EventName = EventName;
            this.ExpectedSum = ExpectedSum;
            SumComparison = comparison;
            this.TraceEventType = TraceEventType;
            this.PropertyName = PropertyName;
        }

        public FetchedEventsSumTraceAssertAttribute(int EventId, Type TraceEventType, string PropertyName, long ExpectedSum, DecimalComparison comparison)
        {
            this.EventId = EventId;
            this.ExpectedSum = ExpectedSum;
            SumComparison = comparison;
            this.TraceEventType = TraceEventType;
            this.PropertyName = PropertyName;
        }

        public override void Assert(List<TraceEvent> traceEvent)
        {
            long actual = 0;
            List<TraceEvent> list;
            PropertyInfo property = TraceEventType.GetProperty(PropertyName);
            if (property == null) throw new Exception("Property name cannot be found");

            if (EventName == null)
            {
                list = traceEvent.Where(evt => ((ushort)EventId).Equals(evt.ID)).ToList();
            }
            else
            {
                list = traceEvent.Where(evt => evt.EventName == EventName).ToList();
            }


            list.ForEach(evt =>
            {
                if (long.TryParse(property.GetValue(evt).ToString(), out long parsed)) actual += parsed;
            });


            if (!Compare(ExpectedSum, actual, SumComparison))
            {
                throw new Exception($"Fetched Events Count Trace Assert is Failed: expected {ExpectedSum}, actual {actual}, comparison {Enum.GetName(typeof(DecimalComparison), SumComparison)}");
            }
        }

        private bool Compare(long expected, long actual, DecimalComparison comparison)
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