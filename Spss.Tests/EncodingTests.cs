using System.Text;
using Spss.Encodings;
using Xunit;

namespace SPSS.Tests
{
    public class EncodingTests
    {
        [Theory]
        [InlineData("za\u0306\u01FD\u03B2", 120, 8, 15, "za\u0306\u01FD\u03B2       ")]
        [InlineData("za\u0306\u01FD\u03B2", 8, 8, 15, "za\u0306\u01FD\u03B2       ")]
        [InlineData("za\u0306\u01FD\u03B2za\u0306\u01FD\u03B2", 8, 8, 15, "za\u0306\u01FD\u03B2       ")]
        [InlineData("12345678", 16, 8, 15, "12345678       ")]
        [InlineData("1234567", 16, 7, 7, "1234567")]
        [InlineData("1234567890AB🤓", 15, 12, 15, "1234567890AB   ")]
        [InlineData("1👩🏽‍🚒", 1, 1, 7, "1      ")]
        public void TestGetPaddedRounded(string value, int maxLength, int expectedLength, int arraySize, string expected)
        {
            var enc = Encoding.GetEncoding(Encoding.UTF8.CodePage, new RemoveReplacementCharEncoderFallback(), DecoderFallback.ReplacementFallback);
            var (byteLength, label) = enc.GetValueLabelAsByteArray(value, maxLength);
            Assert.Equal(expectedLength, byteLength);
            Assert.Equal(arraySize, label.Length);
            Assert.Equal(expected, Encoding.UTF8.GetString(label));
        }
    }
}