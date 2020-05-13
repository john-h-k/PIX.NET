﻿using System;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using static PIX.NET.PIXEncoding;
using static PIX.NET.NativeMethods;

namespace PIX.NET
{
    /*
     * the intermediates between the horrific generic mess we used for the public API and the real lowlevel implementation
     * note we take a pointer and length because we wanna get rid of the <T0, ..., TN> mess early rather than having it leak down here
     */
    internal static unsafe class PIXEvents
    {
        public static void BeginEvent(
            ulong* args,
            uint argsLength,
            PIXEventsThreadInfo* threadInfo,
            ulong time
        )
        {
            SerializeForCpuCapture(args, argsLength, threadInfo, time);
        }

        public static void BeginEvent(
            ID3D12GraphicsCommandList* context,
            ulong* args,
            uint argsLength,
            PIXEventsThreadInfo* threadInfo,
            ulong time
        )
        {
            SerializeForCpuCapture(args, argsLength, threadInfo, time);

            BeginGPUEventOnContext(context, args, argsLength * sizeof(ulong));
        }

        public static void BeginEvent(
            ID3D12CommandQueue* context,
            ulong* args,
            uint argsLength,
            PIXEventsThreadInfo* threadInfo,
            ulong time
        )
        {
            SerializeForCpuCapture(args, argsLength, threadInfo, time);

            BeginGPUEventOnContext(context, args, argsLength * sizeof(ulong));
        }
        
        public static void SetMarker(
            ulong* args,
            uint argsLength,
            PIXEventsThreadInfo* threadInfo,
            ulong time
        )
        {
            SerializeForCpuCapture(args, argsLength, threadInfo, time);
        }

        public static void SetMarker(
            ID3D12GraphicsCommandList* context,
            ulong* args,
            uint argsLength,
            PIXEventsThreadInfo* threadInfo,
            ulong time
        )
        {
            SerializeForCpuCapture(args, argsLength, threadInfo, time);

            SetGPUMarkerOnContext(context, args, argsLength * sizeof(ulong));
        }

        public static void SetMarker(
            ID3D12CommandQueue* context,
            ulong* args,
            uint argsLength,
            PIXEventsThreadInfo* threadInfo,
            ulong time
        )
        {
            SerializeForCpuCapture(args, argsLength, threadInfo, time);

            SetGPUMarkerOnContext(context, args, argsLength * sizeof(ulong));
        }
        
        public static void EndEvent(
            ulong* args,
            uint argsLength,
            PIXEventsThreadInfo* threadInfo,
            ulong time
        )
        {
            SerializeForCpuCapture(args, argsLength, threadInfo, time);
        }
        
        public static void EndEvent(
            ID3D12GraphicsCommandList* context,
            ulong* args,
            uint argsLength,
            PIXEventsThreadInfo* threadInfo,
            ulong time
        )
        {
            SerializeForCpuCapture(args, argsLength, threadInfo, time);

            EndGPUEventOnContext(context);
        }

        public static void EndEvent(
            ID3D12CommandQueue* context,
            ulong* args,
            uint argsLength,
            PIXEventsThreadInfo* threadInfo,
            ulong time
        )
        {
            SerializeForCpuCapture(args, argsLength, threadInfo, time);

            EndGPUEventOnContext(context);
        }

        internal static PIXEventsThreadInfo* PIXRetrieveTimeData(out ulong time)
        {
            
            PIXEventsThreadInfo* threadInfo = PIXGetThreadInfo();
            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;
            
            if (CanWrite(destination, limit))
            {
                time = PIXGetTimestampCounter();
            }
            else if (limit != null) // old block is full. get a fresh spanking new one
            {
                time = PIXEventsReplaceBlock(threadInfo, false);
            }
            else
            {
                // cpu capture isn't occuring
                time = default;
                return null;
            }

            return threadInfo;
        }


        /*
         * This serializes the type, color, context (if not on xbox), etc when a CPU capture occurs
         * The if else if won't be entered if CPU capture isn't occuring (PIXGetThreadInfo will return an empty struct)
         */
        private static void SerializeForCpuCapture(
            ulong* args, 
            uint argsLength, 
            PIXEventsThreadInfo* threadInfo, 
            ulong time
        )    
        {
            Debug.Assert(threadInfo != null, "jeepers scooby! something is horrifically wrong!!");
            // this is probably caused by a internal WinPixEventRuntime issue or a bad reassignment
            
            ulong* destination = threadInfo->Destination;
            ulong* limit = threadInfo->BiasedLimit;
            if (CanWrite(destination, limit))
            {
                limit += EventsSafeFastCopySpaceQwords;

                CopyArgs(ref destination, limit, args, argsLength);

                *destination = EventsBlockEndMarker;
                threadInfo->Destination = destination;
            }
            else if (limit != null) // the old block had ran out, so we got a new one (yay!)
            {
                if (time == 0)
                {
                    return;
                }

                destination = threadInfo->Destination;
                limit = threadInfo->BiasedLimit;

                if (destination >= limit)
                {
                    return;
                }

                limit += EventsSafeFastCopySpaceQwords;

                CopyArgs(ref destination, limit, args, argsLength);

                *destination = EventsBlockEndMarker;
                threadInfo->Destination = destination;
            }
        }


        // Copies the 8 byte args between mem and corrects offsets dest
        private static void CopyArgs(
            ref ulong* dest,
            ulong* destLimit,
            ulong* source,
            uint argsLength
        )
        {
            var diff = (uint) Unsafe.ByteOffset(ref *dest, ref *destLimit);
            Debug.Assert((int) diff > 0);
            Buffer.MemoryCopy(source, dest, diff, argsLength * sizeof(ulong));
            dest += argsLength;
        }
    }
}