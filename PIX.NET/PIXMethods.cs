using System;
using System.Diagnostics;
using TerraFX.Interop;
using static PIX.NET.PIXEncoding;

namespace PIX.NET
{
    public static unsafe partial class PIXMethods
    {
       
        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void ReportCounter(ReadOnlySpan<char> name, float value)
        {
            fixed (char* p = name)
            {
                NativeMethods.PIXReportCounter(p, value);
            }
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void NotifyWakeFromFenceSignal(IntPtr @event)
        {
            NativeMethods.PIXNotifyWakeFromFenceSignal(@event);
        }
    }
}