using System;
using System.Diagnostics;
using TerraFX.Interop;
using static PIX.NET.PIXEncoding;

/*
 * K, quick debrief on how PIX works cuz y'all gonna need to know to understand this codebase.
 * 
 * First and foremost, perf is absolutely critical here. Don't get clever and start optimising the generics
 * with params object[]
 *
 * PIX has 2 types of events/markers
 * - CPU
 * - CPU and GPU
 *
 * The CPU args on Windows (not XBOX; see below) contain an additional value 'context', which is either a command list or command queue pointer.
 *
 * For performance reasons, PIX doesn't serialize format strings immediately, and writes them and all provided
 * arguments to memory for serialization later
 *
 * PIX serialized data supports any data type that is either 'wchar_t*', 'char*' (note: we don't support this [reasoning: i am lazy]), or a value that is less than or
 * equal to the size of a ulong (8 bytes). Anything more will be truncated/generally not work. Doesn't matter tho
 * as if its a custom type it won't have a accepted format specifier
 *
 * PIX stores your data in 8 byte chunks. So 2 vararg ints are promoted to longs and take up 16 bytes, not 8
 * Floats require special casing as there is not single precision format specifier, so they are promoted to double
 * Signed types are also specially recognised to properly sign extend, whereas other types are simply written directly
 * to the 8 byte area. This means every data type (except individual string chars) is guaranteed to be 8 byte aligned too.
 *
 * The basic layout of a PIX data string is:
 *    - EventEncoding | Color | [Optional: Only present on CPU] Context | Format String | [optional varargs...]
 *
 *    - The EventEncoding, Color, and Format String are mandatory. I'm not sure how it behaves with a null format string. Should probably find out
 *    - Every thing except the format string and any varargs which are string (char*, wchar_t*) is 8 bytes
 *    - Strings have an 8 byte header that defines the alignment, what size chunks it was copied in (so it can trim excess data), whether it is ansi,
 *      and a isShortcut bool that is unused currently. They are null terminated with 8 bytes of 0
 *    - String copying is done in chunks if PIX_ENABLE_BLOCK_ARGUMENT_COPY is defined, but this can (safely) over-read data on windows. Need to determine if this is ok in managed code
 *      (e.g it can trigger AddressSanitizer from native code)
 *    - Color is a 4 byte ARGB type, see PIXColor
 *    - EventEncoding is a set of flags, see Constants.EncodeEventInfo
 *
 * Because context is thrown slap bang in the middle of the args, we need to basically separate the first 16 bytes from the rest (format string and args).
 * Then we can  insert the context if necessary
 * We allocate enough for the context, and when copying CPU event args all is good.
 * Then for GPU, we skip sizeof(ulong) and rewrite the first 16 bytes. The format strings and args are still in the same place.
 *
 * This rewrite isn't just as simple as shoving everything forward 8 bytes and removing 8 bytes from the size. We need to reencode the event too
 * 
 * We rewrite in PIXEncoding.RewriteEncoding, and PIXEncoding.IsVarargs
 * - We strip out the timestamp as it is only relevant to the CPU
 * - We check if the 
 * 
 * This is more efficient than rewriting the format strings and args to be earlier, as these are likely to be >16 bytes.
 *
 * XBOX implicitly records the context when the actual marker/event command is called on the command queue/list, so it isn't stored on the CPU
 * This means the CPU/GPU data buffers are completely homogeneous on XBOX (yay!)
 *
 * We use this ugly generic mess for perf. To prevent it infecting the codebase, we serialize the args as early as possible to a 512 byte (64 ulong) local buffer. >512 bytes of combined data
 * (EventEncoding, Color, Context, Format String, and varargs combined) isn't supported and is simply truncated to 512 bytes.
 */

namespace PIX.NET
{
    // the public API. supports 0->16 additional format values, generic'd to avoid boxing
    public static unsafe partial class PIXMethods
    {
        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent(
            in PIXColor color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(0, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(1, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(2, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(3, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(4, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(5, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(6, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(7, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(8, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(9, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(10, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(11, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(12, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(13, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(14, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(15, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(16, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(0, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(1, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(2, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(3, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(4, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(5, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(6, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(7, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(8, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(9, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(10, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(11, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(12, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(13, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(14, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(15, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(16, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(0, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(1, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, 4 + 1 + ((uint)formatString.Length + 7) / 8, thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(2, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(3, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(4, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(5, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(6, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(7, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(8, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(9, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(10, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(11, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(12, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(13, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(14, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(15, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(16, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker(
            in PIXColor color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(0, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(1, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(2, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(3, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(4, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(5, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(6, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(7, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(8, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(9, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(10, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(11, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(12, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(13, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(14, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(15, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(16, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(0, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(1, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(2, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(3, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(4, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(5, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(6, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(7, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(8, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(9, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(10, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(11, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(12, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(13, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(14, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(15, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(16, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(0, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(1, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(2, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(3, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(4, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(5, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(6, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(7, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(8, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(9, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(10, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(11, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(12, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(13, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(14, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(15, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = PIXColor.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(16, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }


        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void EndEvent()
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.EndEvent(0, true)));
            *destination = 0UL;

            PIXEvents.EndEvent(buffer, 1, thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void EndEvent(
            ID3D12CommandQueue* context
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.EndEvent(0, true)));
            WriteContext(ref destination, limit, context);
            *destination = 0UL;

            PIXEvents.EndEvent(context, buffer, 2, thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void EndEvent(
            ID3D12GraphicsCommandList* context
        )
        {
            var thread = PIXEvents.PIXRetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.EndEvent(0, true)));
            WriteContext(ref destination, limit, context);
            *destination = 0UL;

            PIXEvents.EndEvent(context, buffer, 2, thread, time);
        }

    }
}