using System.Runtime.CompilerServices;

namespace PIX.NET
{
    internal static class StackSentinel
    {
        public const int MaxStackallocBytes = 512;

        public static bool SafeToStackalloc<T>(int count)
            => Unsafe.SizeOf<T>() * count <= MaxStackallocBytes;
    }
}