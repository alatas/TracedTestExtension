using System;
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
}

