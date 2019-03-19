using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TTE.Core.TraceSession
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TraceSessionAttribute : Attribute
    {
        public readonly string providerName;
        public readonly Type eventParserType;
        public readonly string sessionName;
        public readonly TraceEventLevel providerLevel;
        public readonly string[] acceptedEventProviderNames;
        public readonly string[] rejectedEventProviderNames;
        public readonly string[] acceptedEventNames;
        public readonly string[] rejectedEventNames;
        public readonly string[] inclusiveProcessNames;
        public readonly int[] includedEventIds = null;
        public readonly int[] excludedEventIds = null;
        public readonly bool includeOwnProcess = false;

        public TraceSessionAttribute(string providerName,
            Type eventParserType,
            string sessionName = null,
            TraceEventLevel providerLevel = TraceEventLevel.Verbose,
            string[] acceptedEventProviderNames = null,
            string[] rejectedEventProviderNames = null,
            string[] acceptedEventNames = null,
            string[] rejectedEventNames = null,
            string[] inclusiveProcessNames = null,
            int[] includedEventIds = null,
            int[] excludedEventIds = null,
            bool includeOwnProcess = false)
        {
            this.providerName = providerName;
            this.eventParserType = eventParserType;
            this.sessionName = sessionName ?? "TraceSession_" + DateTime.Now.ToString("yyyyMMss_HHmmss");
            this.providerLevel = providerLevel;
            this.acceptedEventProviderNames = acceptedEventProviderNames;
            this.rejectedEventProviderNames = rejectedEventProviderNames;
            this.acceptedEventNames = acceptedEventNames;
            this.rejectedEventNames = rejectedEventNames;
            this.inclusiveProcessNames = inclusiveProcessNames;
            this.includedEventIds = includedEventIds;
            this.excludedEventIds = excludedEventIds;
            this.includeOwnProcess = includeOwnProcess;
        }

        public TraceEventProviderOptions TraceEventProviderOptions
        {
            get
            {
                return new TraceEventProviderOptions()
                {
                    EventIDsToDisable = excludedEventIds?.ToList() ?? new List<int>(),
                    EventIDsToEnable = includedEventIds?.ToList() ?? new List<int>(),
                    ProcessNameFilter = inclusiveProcessNames?.ToList() ?? new List<string>()
                };
            }
        }

        public bool IsKernelSession
        {
            get
            {
                return (sessionName == KernelTraceEventParser.KernelSessionName);
            }
        }

        public bool CanProviderNameAccepted(string providerName)
        {
            return Utility.CanAccepted(providerName, acceptedEventProviderNames, rejectedEventProviderNames);
        }

        public bool CanEventNameAccepted(string eventName)
        {
            return Utility.CanAccepted(eventName, acceptedEventNames, rejectedEventNames);
        }

        public bool IsEventIdIncluded(int eventId)
        {
            return Utility.CanAccepted(eventId, includedEventIds, excludedEventIds);
        }

        public bool IsProcessTracing(string processName)
        {
            if (inclusiveProcessNames != null && inclusiveProcessNames.Contains(processName)) return true;
            return false;
        }

    }
}