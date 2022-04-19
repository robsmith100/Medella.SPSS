using System.Text;

namespace Spss.Encodings;

public class RemoveReplacementCharEncoderFallback : EncoderFallback
{
    public override int MaxCharCount => 0;

    public override EncoderFallbackBuffer CreateFallbackBuffer()
    {
        return new RemoveReplacementCharEncoderFallbackBuffer();
    }

    public class RemoveReplacementCharEncoderFallbackBuffer : EncoderFallbackBuffer
    {
        public override int Remaining => 0;

        public override bool Fallback(char unknownChar, int index)
        {
            return true;
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            return false;
        }

        public override char GetNextChar()
        {
            return default;
        }

        public override bool MovePrevious()
        {
            return false;
        }

        public override void Reset()
        {
        }
    }
}