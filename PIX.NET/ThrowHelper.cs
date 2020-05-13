using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;

namespace PIX.NET
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfFailed(int hr, [CallerArgumentExpression("hr")] string? name = null)
        {
            if (Windows.FAILED(hr))
            {
                ThrowExternalException(name!, hr);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowExternalException(string message, int hr)
        {
            throw new ExternalException(message, hr);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentException(string message)
        {
            throw new ArgumentException(message);
        }
    }
}