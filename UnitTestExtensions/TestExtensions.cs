using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

static class TestExtensions
{
    private static Dictionary<string, long> timers = new Dictionary<string, long>();
    public static void StartTimer(this Assert assert, string name = "default")
    {
        if (timers.ContainsKey(name))
        {
            timers.Remove(name);
        }

        timers.Add(name, Now());
    }

    public static void IsTimedOut(this Assert assert, int expectedDuration, string name = "default")
    {
        if (!timers.ContainsKey(name))
        {
            throw new AssertFailedException("Timer is not found");
        }

        TimeSpan t = TimeSpan.FromTicks(Now() - timers[name]);
        if (t.TotalMilliseconds > expectedDuration)
        {
            throw new AssertFailedException($"The duration is longer than expected: {Math.Round(t.TotalMilliseconds) }msec > {expectedDuration }msec");
        }
    }
    private static long Now()
    {
        return System.DateTime.UtcNow.Ticks;
    }
}


