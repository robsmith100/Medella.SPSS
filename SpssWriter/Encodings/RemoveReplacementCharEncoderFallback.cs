using System.Text;

namespace Spss.Encodings
{
    public class RemoveReplacementCharEncoderFallback : EncoderFallback
    {
        public override int MaxCharCount => 0; 

        public override EncoderFallbackBuffer CreateFallbackBuffer() => new RemoveReplacementCharEncoderFallbackBuffer();

        public class RemoveReplacementCharEncoderFallbackBuffer : EncoderFallbackBuffer
        {
            public override int Remaining => 0;

            public override bool Fallback(char unknownChar, int index) => true;

            public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index) => false;

            public override char GetNextChar() => default(char);

            public override bool MovePrevious() => false;

            public override void Reset()
            {
            }
        }
    }
}