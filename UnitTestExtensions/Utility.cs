using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;


class Utility
    {

    public static int GetNativeThreadId(Thread thread)
    {
        var f = typeof(Thread).GetField("DONT_USE_InternalThread", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
        var pInternalThread = (IntPtr)f.GetValue(thread);
        var nativeId = Marshal.ReadInt32(pInternalThread, (IntPtr.Size == 8) ? 0x022C : 0x0160);
        return nativeId;
    }

    public static bool CanAccepted<T>(T value, T[] acceptedArray, T[] rejectedArray)
    {
        if (rejectedArray != null && rejectedArray.Contains(value)) return false;
        if (acceptedArray != null && acceptedArray.Contains(value)) return true;

        if (acceptedArray == null && rejectedArray != null) return true;
        if (acceptedArray != null && rejectedArray == null) return false;

        if (acceptedArray == null && rejectedArray == null) return true;
        if (acceptedArray != null && rejectedArray != null) return false;
        return false;
    }
}

