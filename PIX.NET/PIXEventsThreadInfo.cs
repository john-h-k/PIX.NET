using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

// ReSharper disable IdentifierTypo

namespace PIX.NET
{
#pragma warning disable 649
    public unsafe struct PIXEventsThreadInfo
    {
        public override string ToString()
        {
            return $"Block: {(IntPtr) Block: X8}" +
                   $"Destination: {((IntPtr)Destination): X8}" +
                   $"BiasedLimit: {(IntPtr) BiasedLimit: X8}";
        }

        public void* Block; // EventsBlockInfo*
        public ulong* BiasedLimit;
        public ulong* Destination;
    }
#pragma warning restore 649
}