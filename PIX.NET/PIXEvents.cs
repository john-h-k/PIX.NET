using System.Runtime.CompilerServices;
using TerraFX.Interop;
using static PIX.NET.PIXConstants;
using static PIX.NET.NativeMethods;
using static PIX.NET.PIXEventType;

namespace PIX.NET
{
    internal static class PIXEventTypeInferer
    {
        public static PIXEventType Begin(int length)
        {
            return length == 0 ? PIXEvent_BeginEvent_NoArgs : PIXEvent_BeginEvent_VarArgs;
        }

        public static PIXEventType SetMarker(int length)
        {
            return length == 0 ? PIXEvent_SetMarker_NoArgs : PIXEvent_SetMarker_VarArgs;
        }

        public static PIXEventType BeginOnContext(int length)
        {
            return length == 0 ? PIXEvent_BeginEvent_OnContext_NoArgs : PIXEvent_BeginEvent_OnContext_VarArgs;
        }

        public static PIXEventType SetMarkerOnContext(int length)
        {
            return length == 0 ? PIXEvent_SetMarker_OnContext_NoArgs : PIXEvent_SetMarker_OnContext_VarArgs;
        }

        // Xbox and Windows store different types of events for context events.
        // On Xbox these include a context argument, while on Windows they do
        // not. It is important not to change the event types used on the
        // Windows version as there are OS components (eg debug layer & DRED)
        // that decode event structs.
#if PIX_XBOX
        public   static PIXEventType GpuBeginOnContext(int length) { return length == 0 ? PIXEvent_BeginEvent_OnContext_NoArgs : PIXEvent_BeginEvent_OnContext_VarArgs; }
        public  static PIXEventType GpuSetMarkerOnContext(int length) { return length == 0 ? PIXEvent_SetMarker_OnContext_NoArgs : PIXEvent_SetMarker_OnContext_VarArgs; }
#else
        public static PIXEventType GpuBeginOnContext(int length)
        {
            return length == 0 ? PIXEvent_BeginEvent_NoArgs : PIXEvent_BeginEvent_VarArgs;
        }

        public static PIXEventType GpuSetMarkerOnContext(int length)
        {
            return length == 0 ? PIXEvent_SetMarker_NoArgs : PIXEvent_SetMarker_VarArgs;
        }
#endif
    }

    internal static unsafe class PIXEvents
    {
        public static void PIXCopyEventArguments(ref ulong* destination, ulong* limit)
        {
            // nothing
        }

        public static void PIXCopyEventArguments(ref ulong* destination, ulong* limit, void* context, char* format,
            params object[] args)
        {
            PIXCopyEventArgument(ref destination, limit, context);
            PIXCopyEventArguments(ref destination, limit, format, args);
        }

        public static void PIXCopyEventArguments(ref ulong* destination, ulong* limit, char* format,
            params object[] args)
        {
            PIXCopyEventArgument(ref destination, limit, format);
            foreach (object arg in args)
            {
                PIXCopyEventArgument(ref destination, limit, arg);
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void PIXBeginEventAllocate(PIXEventsThreadInfo* threadInfo, ulong color, char* formatString,
            params object[] args)
        {
            ulong time = PIXEventsReplaceBlock(threadInfo, false);
            if (time == 0)
            {
                return;
            }

            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;
            if (destination >= limit)
            {
                return;
            }

            limit += PIXEventsSafeFastCopySpaceQwords;
            *destination++ = PIXEncodeEventInfo(time, PIXEventTypeInferer.Begin(args.Length));
            *destination++ = color;

            PIXCopyEventArguments(ref destination, limit, formatString, args);

            *destination = PIXEventsBlockEndMarker;
            threadInfo->Destination = destination;
        }


        public static void PIXBeginEvent(ulong color, char* formatString, params object[] args)
        {
            PIXEventsThreadInfo* threadInfo = PIXGetThreadInfo();
            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;

            if (destination < limit)
            {
                limit += PIXEventsSafeFastCopySpaceQwords;
                ulong time = PIXGetTimestampCounter();
                *destination++ = PIXEncodeEventInfo(time, PIXEventTypeInferer.Begin(args.Length));
                *destination++ = color;

                PIXCopyEventArguments(ref destination, limit, formatString, args);

                *destination = PIXEventsBlockEndMarker;
                threadInfo->Destination = destination;
            }
            else if (limit != null)
            {
                PIXBeginEventAllocate(threadInfo, color, formatString);
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void PIXSetMarkerAllocate(PIXEventsThreadInfo* threadInfo, ulong color, char* formatString,
            params object[] args)
        {
            ulong time = PIXEventsReplaceBlock(threadInfo, false);
            if (time == 0)
            {
                return;
            }

            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;

            if (destination >= limit)
            {
                return;
            }

            limit += PIXEventsSafeFastCopySpaceQwords;
            *destination++ = PIXEncodeEventInfo(time, PIXEventTypeInferer.SetMarker(args.Length));
            *destination++ = color;

            PIXCopyEventArguments(ref destination, limit, formatString, args);

            *destination = PIXEventsBlockEndMarker;
            threadInfo->Destination = destination;
        }


        public static void PIXSetMarker(ulong color, char* formatString, params object[] args)
        {
            PIXEventsThreadInfo* threadInfo = PIXGetThreadInfo();
            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;
            if (destination < limit)
            {
                limit += PIXEventsSafeFastCopySpaceQwords;
                ulong time = PIXGetTimestampCounter();
                *destination++ = PIXEncodeEventInfo(time, PIXEventTypeInferer.SetMarker(args.Length));
                *destination++ = color;

                PIXCopyEventArguments(ref destination, limit, formatString, args);

                *destination = PIXEventsBlockEndMarker;
                threadInfo->Destination = destination;
            }
            else if (limit != null)
            {
                PIXSetMarkerAllocate(threadInfo, color, formatString, args);
            }
        }

#if !PIX_XBOX

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void PIXBeginEventOnContextCpuAllocate(PIXEventsThreadInfo* threadInfo, void* context,
            ulong color,
            char* formatString, params object[] args)
        {
            ulong time = PIXEventsReplaceBlock(threadInfo, false);
            if (time == 0)
            {
                return;
            }

            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;

            if (destination >= limit)
            {
                return;
            }

            limit += PIXEventsSafeFastCopySpaceQwords;
            *destination++ = PIXEncodeEventInfo(time, PIXEventTypeInferer.BeginOnContext(args.Length));
            *destination++ = color;

            PIXCopyEventArguments(ref destination, limit, context, formatString, args);

            *destination = PIXEventsBlockEndMarker;
            threadInfo->Destination = destination;
        }


        public static void PIXBeginEventOnContextCpu(void* context, ulong color, char* formatString,
            params object[] args)
        {
            PIXEventsThreadInfo* threadInfo = PIXGetThreadInfo();
            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;
            if (destination < limit)
            {
                limit += PIXEventsSafeFastCopySpaceQwords;
                ulong time = PIXGetTimestampCounter();
                *destination++ = PIXEncodeEventInfo(time, PIXEventTypeInferer.BeginOnContext(args.Length));
                *destination++ = color;

                PIXCopyEventArguments(ref destination, limit, context, formatString, args);

                *destination = PIXEventsBlockEndMarker;
                threadInfo->Destination = destination;
            }
            else if (limit != null)
            {
                PIXBeginEventOnContextCpuAllocate(threadInfo, context, color, formatString, args);
            }
        }
#endif


        public static void PIXBeginEvent(
            ID3D12GraphicsCommandList* context,
            ulong color,
            char* formatString,
            params object[] args
        )
        {
#if PIX_XBOX
            PIXBeginEvent(color, formatString, args);
#else
            PIXBeginEventOnContextCpu(context, color, formatString, args);
#endif

            ulong* buffer = stackalloc ulong[PIXEventsGraphicsRecordSpaceQwords];
// TODO: we've already encoded this once for the CPU event - figure out way to avoid doing it again
            ulong* destination = buffer;
            ulong* limit = (buffer + PIXEventsGraphicsRecordSpaceQwords) - PIXEventsReservedTailSpaceQwords;

            *destination++ = PIXEncodeEventInfo(0, PIXEventTypeInferer.Begin(args.Length));
            *destination++ = color;

            PIXCopyEventArguments(ref destination, limit, formatString, args);
            *destination = 0UL;

            var size = (uint) Unsafe.ByteOffset(ref *buffer, ref *destination);
            PIXBeginGPUEventOnContext(context, buffer, size);
        }

        public static void PIXBeginEvent(
            ID3D12CommandQueue* context,
            ulong color,
            char* formatString,
            params object[] args
        )
        {
#if PIX_XBOX
            PIXBeginEvent(color, formatString, args);
#else
            PIXBeginEventOnContextCpu(context, color, formatString, args);
#endif

            ulong* buffer = stackalloc ulong[PIXEventsGraphicsRecordSpaceQwords];
// TODO: we've already encoded this once for the CPU event - figure out way to avoid doing it again
            ulong* destination = buffer;
            ulong* limit = (buffer + PIXEventsGraphicsRecordSpaceQwords) - PIXEventsReservedTailSpaceQwords;

            *destination++ = PIXEncodeEventInfo(0, PIXEventTypeInferer.Begin(args.Length));
            *destination++ = color;

            PIXCopyEventArguments(ref destination, limit, formatString, args);
            *destination = 0UL;

            PIXBeginGPUEventOnContext(context, buffer, (uint) Unsafe.ByteOffset(ref *buffer, ref *destination));
        }

#if !PIX_XBOX

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void PIXSetMarkerOnContextCpuAllocate(PIXEventsThreadInfo* threadInfo, void* context, ulong color,
            char* formatString, params object[] args)
        {
            ulong time = PIXEventsReplaceBlock(threadInfo, false);
            if (time == 0)
            {
                return;
            }

            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;

            if (destination >= limit)
            {
                return;
            }

            limit += PIXEventsSafeFastCopySpaceQwords;
            *destination++ = PIXEncodeEventInfo(time, PIXEventTypeInferer.SetMarkerOnContext(args.Length));
            *destination++ = color;

            PIXCopyEventArguments(ref destination, limit, context, formatString, args);

            *destination = PIXEventsBlockEndMarker;
            threadInfo->Destination = destination;
        }


        public static void PIXSetMarkerOnContextCpu(void* context, ulong color, char* formatString,
            params object[] args)
        {
            PIXEventsThreadInfo* threadInfo = PIXGetThreadInfo();
            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;
            if (destination < limit)
            {
                limit += PIXEventsSafeFastCopySpaceQwords;
                ulong time = PIXGetTimestampCounter();
                *destination++ = PIXEncodeEventInfo(time, PIXEventTypeInferer.SetMarkerOnContext(args.Length));
                *destination++ = color;

                PIXCopyEventArguments(ref destination, limit, context, formatString, args);

                *destination = PIXEventsBlockEndMarker;
                threadInfo->Destination = destination;
            }
            else if (limit != null)
            {
                PIXSetMarkerOnContextCpuAllocate(threadInfo, context, color, formatString, args);
            }
        }
#endif


        public static void PIXSetMarker(ID3D12CommandQueue* context, ulong color, char* formatString,
            params object[] args)
        {
#if PIX_XBOX
        PIXSetMarker(color, formatString, args);
#else
            PIXSetMarkerOnContextCpu(context, color, formatString, args);
#endif

            ulong* buffer = stackalloc ulong[PIXEventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = buffer + PIXEventsGraphicsRecordSpaceQwords - PIXEventsReservedTailSpaceQwords;

            *destination++ = PIXEncodeEventInfo(0, PIXEventTypeInferer.GpuSetMarkerOnContext(args.Length));
            *destination++ = color;

            PIXCopyEventArguments(ref destination, limit, formatString, args);
            *destination = 0UL;

            PIXSetGPUMarkerOnContext(context, (void*) buffer,
                (uint) Unsafe.ByteOffset(ref *buffer, ref *destination));
        }

        public static void PIXSetMarker(ID3D12GraphicsCommandList* context, ulong color, char* formatString,
            params object[] args)
        {
#if PIX_XBOX
        PIXSetMarker(color, formatString, args);
#else
            PIXSetMarkerOnContextCpu(context, color, formatString, args);
#endif

            ulong* buffer = stackalloc ulong[PIXEventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + PIXEventsGraphicsRecordSpaceQwords) - PIXEventsReservedTailSpaceQwords;

            *destination++ = PIXEncodeEventInfo(0, PIXEventTypeInferer.GpuSetMarkerOnContext(args.Length));
            *destination++ = color;

            PIXCopyEventArguments(ref destination, limit, formatString, args);
            *destination = 0UL;

            PIXSetGPUMarkerOnContext(context, (void*) buffer,
                (uint) Unsafe.ByteOffset(ref *buffer, ref *destination));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void PIXEndEventAllocate(PIXEventsThreadInfo* threadInfo)
        {
            ulong time = PIXEventsReplaceBlock(threadInfo, true);
            if (time == 0)
            {
                return;
            }

            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;

            if (destination >= limit)
            {
                return;
            }

            limit += PIXEventsSafeFastCopySpaceQwords;
            *destination++ = PIXEncodeEventInfo(time, PIXEvent_EndEvent);
            *destination = PIXEventsBlockEndMarker;
            threadInfo->Destination = destination;
        }

        public static void PIXEndEvent()
        {
            PIXEventsThreadInfo* threadInfo = PIXGetThreadInfo();
            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;
            if (destination < limit)
            {
                limit += PIXEventsSafeFastCopySpaceQwords;
                ulong time = PIXGetTimestampCounter();
                *destination++ = PIXEncodeEventInfo(time, PIXEvent_EndEvent);
                *destination = PIXEventsBlockEndMarker;
                threadInfo->Destination = destination;
            }
            else if (limit != null)
            {
                PIXEndEventAllocate(threadInfo);
            }
        }

#if !PIX_XBOX
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void PIXEndEventOnContextCpuAllocate(PIXEventsThreadInfo* threadInfo, void* context)
        {
            ulong time = PIXEventsReplaceBlock(threadInfo, true);
            if (time == 0)
            {
                return;
            }

            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;

            if (destination >= limit)
            {
                return;
            }

            limit += PIXEventsSafeFastCopySpaceQwords;
            *destination++ = PIXEncodeEventInfo(time, PIXEvent_EndEvent_OnContext);
            PIXCopyEventArgument(ref destination, limit, context);
            *destination = PIXEventsBlockEndMarker;
            threadInfo->Destination = destination;
        }

        public static void PIXEndEventOnContextCpu(void* context)
        {
            PIXEventsThreadInfo* threadInfo = PIXGetThreadInfo();
            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;
            if (destination < limit)
            {
                limit += PIXEventsSafeFastCopySpaceQwords;
                ulong time = PIXGetTimestampCounter();
                *destination++ = PIXEncodeEventInfo(time, PIXEvent_EndEvent_OnContext);
                PIXCopyEventArgument(ref destination, limit, context);
                *destination = PIXEventsBlockEndMarker;
                threadInfo->Destination = destination;
            }
            else if (limit != null)
            {
                PIXEndEventOnContextCpuAllocate(threadInfo, context);
            }
        }
#endif


        public static void PIXEndEvent(ID3D12CommandQueue* context)
        {
#if PIX_XBOX
            PIXEndEvent();
#else
            PIXEndEventOnContextCpu(context);
#endif
            PIXEndGPUEventOnContext(context);
        }

        public static void PIXEndEvent(ID3D12GraphicsCommandList* context)
        {
#if PIX_XBOX
            PIXEndEvent();
#else
            PIXEndEventOnContextCpu(context);
#endif
            PIXEndGPUEventOnContext(context);
        }
    }

    public static unsafe class PublicApi
    {
        public static void PIXBeginEvent(PIXColor color, char* formatString, params object[] args)
        {
            PIXEvents.PIXBeginEvent(PIXColor.GetAs32BitArgb(color), formatString, args);
        }

        public static void PIXSetMarker(PIXColor color, char* formatString, params object[] args)
        {
            PIXEvents.PIXSetMarker(PIXColor.GetAs32BitArgb(color), formatString, args);
        }


        public static void PIXBeginEvent(ID3D12CommandQueue* context, PIXColor color, char*
            formatString, params object[] args)
        {
            PIXEvents.PIXBeginEvent(context, PIXColor.GetAs32BitArgb(color), formatString, args);
        }

        public static void PIXBeginEvent(ID3D12GraphicsCommandList* context, PIXColor color, char*
            formatString, params object[] args)
        {
            PIXEvents.PIXBeginEvent(context, PIXColor.GetAs32BitArgb(color), formatString, args);
        }


        public static void PIXSetMarker(ID3D12CommandQueue* context, PIXColor color, char*
            formatString, params object[] args)
        {
            PIXEvents.PIXSetMarker(context, PIXColor.GetAs32BitArgb(color), formatString, args);
        }

        public static void PIXSetMarker(ID3D12GraphicsCommandList* context, PIXColor color, char*
            formatString, params object[] args)
        {
            PIXEvents.PIXSetMarker(context, PIXColor.GetAs32BitArgb(color), formatString, args);
        }

        public static void PIXEndEvent()
        {
            PIXEvents.PIXEndEvent();
        }


        public static void PIXEndEvent(ID3D12CommandQueue* context)
        {
            PIXEvents.PIXEndEvent(context);
        }

        public static void PIXEndEvent(ID3D12GraphicsCommandList* context)
        {
            PIXEvents.PIXEndEvent(context);
        }
    }
}