using System.Runtime.CompilerServices;

namespace PIX.NET
{
    /// <summary>
    /// Represents a 32 bit ARGB color used by PIX where A must be 0xFF
    /// </summary>
    public struct PIXColor
    {
        /// <summary>
        /// Creates a new instance from 3 separate RGB values
        /// </summary>
        /// <param name="r">The red component of the color</param>
        /// <param name="g">The green component of the color</param>
        /// <param name="b">The blue component of the color</param>
        public PIXColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
            _a = 0xFF;
        }

#pragma warning disable 414
        private byte _a;
#pragma warning restore 414

        /// <summary>
        /// The red component of the color
        /// </summary>
        public byte R;

        /// <summary>
        /// The green component of the color
        /// </summary>
        public byte G;

        /// <summary>
        /// The blue component of the color
        /// </summary>
        public byte B;

        internal static uint GetAs32BitArgb(PIXColor color) => Unsafe.As<PIXColor, uint>(ref color);
        }
}
