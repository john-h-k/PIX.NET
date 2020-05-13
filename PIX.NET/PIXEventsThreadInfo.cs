using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

// ReSharper disable IdentifierTypo

namespace PIX.NET
{
#pragma warning disable 649
    internal unsafe struct PIXEventsThreadInfo
    {
        public void* Block; // EventsBlockInfo*
        public ulong* BiasedLimit;
        public ulong* Destination;
    }
#pragma warning restore 649
}