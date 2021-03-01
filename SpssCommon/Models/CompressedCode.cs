namespace SpssCommon.Models
{
    public class CompressedCode
    {
        public const byte Padding = 0;
        public const byte EndOfFile = 252;
        public const byte Uncompressed = 253;
        public const byte SpaceCharsBlock = 254;
        public const byte SysMiss = 255;
    }
}
