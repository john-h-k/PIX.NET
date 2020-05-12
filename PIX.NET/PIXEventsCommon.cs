using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TerraFX.Interop;

// ReSharper disable IdentifierTypo

namespace PIX.NET
{
#pragma warning disable 649
    internal unsafe struct PIXEventsThreadInfo
    {
        public void* Block; // PIXEventsBlockInfo*
        public ulong* BiasedLimit;
        public ulong* Destination;
    }
#pragma warning restore 649

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum PIXEventType
    {
        PIXEvent_EndEvent = 0x000,
        PIXEvent_BeginEvent_VarArgs = 0x001,
        PIXEvent_BeginEvent_NoArgs = 0x002,
        PIXEvent_SetMarker_VarArgs = 0x007,
        PIXEvent_SetMarker_NoArgs = 0x008,

        PIXEvent_EndEvent_OnContext = 0x010,
        PIXEvent_BeginEvent_OnContext_VarArgs = 0x011,
        PIXEvent_BeginEvent_OnContext_NoArgs = 0x012,
        PIXEvent_SetMarker_OnContext_VarArgs = 0x017,
        PIXEvent_SetMarker_OnContext_NoArgs = 0x018,
    };

    internal static unsafe class PIXConstants
    {
        // ReSharper disable twice InconsistentNaming
        public const uint WINPIX_EVENT_PIX3BLOB_VERSION = 2;
        public const uint D3D12_EVENT_METADATA = WINPIX_EVENT_PIX3BLOB_VERSION;

        public const ulong PIXEventsReservedRecordSpaceQwords = 64;

        //this is used to make sure SSE string copy always will end 16-byte write in the current block
        //this way only a check if destination < limit can be performed, instead of destination < limit - 1
        //since both these are ulong* and SSE writes in 16 byte chunks, 8 bytes are kept in reserve
        //so even if SSE overwrites 8 extra bytes, those will still belong to the correct block
        //on next iteration check destination will be greater than limit
        //this is used as well for fixed size UMD events and PIXEndEvent since these require less space
        //than other variable length user events and do not need big reserved space
        public const ulong PIXEventsReservedTailSpaceQwords = 2;

        public const ulong PIXEventsSafeFastCopySpaceQwords =
            PIXEventsReservedRecordSpaceQwords - PIXEventsReservedTailSpaceQwords;

        public const int PIXEventsGraphicsRecordSpaceQwords = 64;

        //Bits 7-19 (13 bits)
        public const ulong PIXEventsBlockEndMarker = 0x00000000000FFF80;

        //Bits 10-19 (10 bits)
        public const ulong PIXEventsTypeReadMask = 0x00000000000FFC00;
        public const ulong PIXEventsTypeWriteMask = 0x00000000000003FF;
        public const int PIXEventsTypeBitShift = 10;


        //Bits 20-63 (44 bits)
        public const ulong PIXEventsTimestampReadMask = 0xFFFFFFFFFFF00000;
        public const ulong PIXEventsTimestampWriteMask = 0x00000FFFFFFFFFFF;
        public const int PIXEventsTimestampBitShift = 20;

        public static ulong PIXEncodeEventInfo(ulong timestamp, PIXEventType eventType)
        {
            return ((timestamp & PIXEventsTimestampWriteMask) << PIXEventsTimestampBitShift) |
                   (((ulong)eventType & PIXEventsTypeWriteMask) << PIXEventsTypeBitShift);
        }

        //Bits 60-63 (4)
        public const ulong PIXEventsStringAlignmentWriteMask = 0x000000000000000F;
        public const ulong PIXEventsStringAlignmentReadMask = 0xF000000000000000;
        public const int PIXEventsStringAlignmentBitShift = 60;

        //Bits 55-59 (5)
        public const ulong PIXEventsStringCopyChunkSizeWriteMask = 0x000000000000001F;
        public const ulong PIXEventsStringCopyChunkSizeReadMask = 0x0F80000000000000;
        public const int PIXEventsStringCopyChunkSizeBitShift = 55;

        //Bit 54
        public const ulong PIXEventsStringIsANSIWriteMask = 0x0000000000000001;
        public const ulong PIXEventsStringIsANSIReadMask = 0x0040000000000000;
        public const int PIXEventsStringIsANSIBitShift = 54;

        //Bit 53
        public const ulong PIXEventsStringIsShortcutWriteMask = 0x0000000000000001;
        public const ulong PIXEventsStringIsShortcutReadMask = 0x0020000000000000;
        public const int PIXEventsStringIsShortcutBitShift = 53;

        public static ulong PIXEncodeStringInfo(ulong alignment, ulong copyChunkSize, bool isANSI, bool isShortcut)
        {
            return ((alignment & PIXEventsStringAlignmentWriteMask) << PIXEventsStringAlignmentBitShift) |
                   ((copyChunkSize & PIXEventsStringCopyChunkSizeWriteMask) << PIXEventsStringCopyChunkSizeBitShift) |
                   (((isANSI ? 1U : 0) & PIXEventsStringIsANSIWriteMask) << PIXEventsStringIsANSIBitShift) |
                   (((isShortcut ? 1U : 0) & PIXEventsStringIsShortcutWriteMask) << PIXEventsStringIsShortcutBitShift);
        }

        public static bool PIXIsPointerAligned(void* pointer, uint alignment)
        {
            return (((ulong)pointer) & (alignment - 1)) == 0;
        }

        public static void PIXCopyEventArgument<T>(ref ulong* destination, ulong* limit, T value)
        {
            if (destination < limit)
            {
                *destination = 0;
                Unsafe.WriteUnaligned(destination, value);
                destination++;
            }
        }

        // Specialisations
        public static void PIXCopyEventArgument(ref ulong* destination, ulong* limit, void* value)
        {
            if (destination < limit)
            {
                Unsafe.WriteUnaligned(destination, (ulong)value);
                destination++;
            }
        }

        public static void PIXCopyEventArgument(ref ulong* destination, ulong* limit, int value)
        {
            if (destination < limit)
            {
                Unsafe.WriteUnaligned(destination, (ulong)value);
                destination++;
            }
        }

        public static void PIXCopyEventArgument(ref ulong* destination, ulong* limit, uint value)
        {
            if (destination < limit)
            {
                Unsafe.WriteUnaligned(destination, (ulong)value);
                destination++;
            }
        }

        public static void PIXCopyEventArgument(ref ulong* destination, ulong* limit, long value)
        {
            if (destination < limit)
            {
                Unsafe.WriteUnaligned(destination, (ulong)value);
                destination++;
            }
        }

        public static void PIXCopyEventArgument(ref ulong* destination, ulong* limit, ulong value)
        {
            if (destination < limit)
            {
                Unsafe.WriteUnaligned(destination, value);
                destination++;
            }
        }

        public static void PIXCopyEventArgument(ref ulong* destination, ulong* limit, float value)
        {
            if (destination < limit)
            {
                Unsafe.WriteUnaligned(destination, (double)value);
                destination++;
            }
        }

        public static void PIXCopyEventArgument(ref ulong* destination, ulong* limit, double value)
        {
            if (destination < limit)
            {
                Unsafe.WriteUnaligned(destination, value);
                destination++;
            }
        }

        public static void PIXCopyEventArgument(ref ulong* destination, ulong* limit, sbyte value)
        {
            if (destination < limit)
            {
                Unsafe.WriteUnaligned(destination, (ulong)value);
                destination++;
            }
        }

        public static void PIXCopyEventArgument(ref ulong* destination, ulong* limit, byte value)
        {
            if (destination < limit)
            {
                Unsafe.WriteUnaligned(destination, (ulong)value);
                destination++;
            }
        }

        public static void PIXCopyEventArgument(ref ulong* destination, ulong* limit, bool value)
        {
            if (destination < limit)
            {
                Unsafe.WriteUnaligned(destination, value ? 1UL : 0UL);
                destination++;
            }
        }


        public static void PIXCopyEventArgumentSlowest(ref ulong* destination, ulong* limit, char* argument)
        {
            *destination++ = PIXEncodeStringInfo(0, 8, false, false);
            while (destination < limit)
            {
                ulong c = argument[0];
                if (c == default)
                {
                    *destination++ = 0;
                    return;
                }

                ulong x = c;
                c = argument[1];
                if (c == default)
                {
                    *destination++ = x;
                    return;
                }

                x |= c << 16;
                c = argument[2];
                if (c == default)
                {
                    *destination++ = x;
                    return;
                }

                x |= c << 32;
                c = argument[3];
                if (c == default)
                {
                    *destination++ = x;
                    return;
                }

                x |= c << 48;
                *destination++ = x;
                argument += 4;
            }
        }

        public static void PIXCopyEventArgumentSlow(ref ulong* destination, ulong* limit, char* argument)
        {
#if PIX_ENABLE_BLOCK_ARGUMENT_COPY
            if (PIXIsPointerAligned(argument, 8))
            {
                *destination++ = PIXEncodeStringInfo(0, 8, false, false);
                ulong* source = (ulong*)argument;
                while (destination < limit)
                {
                    ulong qword = *source++;
                    *destination++ = qword;
                    //check if any of the characters is a terminating zero
                    //TODO: check if reversed condition is faster
                    if (!((qword & 0xFFFF000000000000) != 0 &&
                          (qword & 0xFFFF00000000) != 0 &&
                          (qword & 0xFFFF0000) != 0 &&
                          (qword & 0xFFFF) != 0))
                    {
                        break;
                    }
                }
            }
            else
#endif // PIX_ENABLE_BLOCK_ARGUMENT_COPY
            {
                PIXCopyEventArgumentSlowest(ref destination, limit, argument);
            }
        }


        public static void PIXCopyEventArgument(ref ulong* destination, ulong* limit, char* argument)
        {
            if (destination < limit)
            {
                if (argument != null)
                {
#if PIX_ENABLE_BLOCK_ARGUMENT_COPY
                    if (PIXIsPointerAligned(argument, 16))
                    {
                        *destination++ = PIXEncodeStringInfo(0, 16, false, false);
                        var zero = Vector128<int>.Zero;
                        if (PIXIsPointerAligned(destination, 16))
                        {
                            while (destination < limit)
                            {
                                var mem = Sse2.LoadAlignedVector128((int*)argument);
                                Sse2.StoreAligned((int*)destination, mem);
                                //check if any of the characters is a terminating zero
                                var res = Sse2.CompareEqual(mem, zero);
                                destination += 2;
                                if (Sse2.MoveMask(res.AsByte()) != 0)
                                {
                                    break;
                                }

                                argument += 8;
                            }
                        }
                        else
                        {
                            while (destination < limit)
                            {
                                var mem = Sse2.LoadVector128((int*)argument);
                                Sse2.Store((int*)destination, mem);
                                //check if any of the characters is a terminating zero
                                var res = Sse2.CompareEqual(mem, zero);
                                destination += 2;
                                if (Sse2.MoveMask(res.AsByte()) != 0)
                                {
                                    break;
                                }

                                argument += 8;
                            }
                        }
                    }
                    else
#endif // (defined(_M_X64) || defined(_M_IX86)) && PIX_ENABLE_BLOCK_ARGUMENT_COPY
                    {
                        PIXCopyEventArgumentSlow(ref destination, limit, argument);
                    }
                }
                else
                {
                    *destination++ = 0UL;
                }
            }
        }

        public static void PIXSetGPUMarkerOnContext(
            ID3D12GraphicsCommandList* commandList,
            void* data, uint size
        )
        {
            commandList->SetMarker(D3D12_EVENT_METADATA, data, size);
        }

        public static void PIXSetGPUMarkerOnContext(
            ID3D12CommandQueue* commandQueue,
            void* data,
            uint size
        )
        {
            commandQueue->SetMarker(D3D12_EVENT_METADATA, data, size);
        }

        public static void PIXBeginGPUEventOnContext(
            ID3D12GraphicsCommandList* commandList,
            void* data,
            uint size
        )
        {
            commandList->BeginEvent(D3D12_EVENT_METADATA, data, size);
        }

        public static void PIXBeginGPUEventOnContext(
            ID3D12CommandQueue* commandQueue,
            void* data,
            uint size
        )
        {
            commandQueue->BeginEvent(D3D12_EVENT_METADATA, data, size);
        }

        public static void PIXEndGPUEventOnContext(
            ID3D12GraphicsCommandList* commandList
        )
        {
            commandList->EndEvent();
        }

        public static void PIXEndGPUEventOnContext(
            ID3D12CommandQueue* commandQueue
        )
        {
            commandQueue->EndEvent();
        }
    }
}
