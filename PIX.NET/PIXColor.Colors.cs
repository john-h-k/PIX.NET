namespace PIX.NET
{
    public partial struct PIXColor
    {
        public static PIXColor Red => new PIXColor(r: 0xFF);
        public static PIXColor Green => new PIXColor(g: 0xFF);
        public static PIXColor Blue => new PIXColor(b: 0xFF);
        public static PIXColor Black => new PIXColor(r: 0x00, g: 0x00, b: 0x00);
        public static PIXColor White => new PIXColor(r: 0xFF, g: 0xFF, b: 0xFF);
    }
}