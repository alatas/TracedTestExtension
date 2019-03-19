using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace TTE.Core
{
    public static class Utility
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
            var matcher = new ContainsMatcher<T>();
            if (rejectedArray != null && rejectedArray.Contains(value, matcher)) return false;
            if (acceptedArray != null && acceptedArray.Contains(value, matcher)) return true;

            if (acceptedArray == null && rejectedArray != null) return true;
            if (acceptedArray != null && rejectedArray == null) return false;

            if (acceptedArray == null && rejectedArray == null) return true;
            if (acceptedArray != null && rejectedArray != null) return false;
            return false;
        }

        private class ContainsMatcher<T> : IEqualityComparer<T>
        {
            public bool Equals(T x, T y)
            {
                if (typeof(T) == typeof(string))
                {
                    string X = x as string;
                    string Y = y as string;
                    return (X.Equals(Y) || X.Contains(Y) || Y.Contains(X));
                }
                else
                {
                    return x.Equals(y);
                }
            }

            public int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }
        }

        public static IEnumerable<O> FilterBySub<T, O>(this IEnumerable<T> list) where O : T
        {
            return list?.Where(item => item is O).Select(item => (O)item);
        }

    }

}