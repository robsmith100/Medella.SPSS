using System.IO;
using System.Text;
using Spss.Extensions;
using Xunit;

namespace SPSS.Tests
{
    public class EncodingTests
    {
        [Theory]
        [InlineData("za\u0306\u01FD\u03B2", 120, 8, "za\u0306\u01FD\u03B2       ")]
        [InlineData("za\u0306\u01FD\u03B2", 8, 8, "za\u0306\u01FD\u03B2       ")]
        [InlineData("za\u0306\u01FD\u03B2za\u0306\u01FD\u03B2", 8, 8, "za\u0306\u01FD\u03B2       ")]
        [InlineData("12345678", 16, 8, "12345678       ")]
        [InlineData("1234567", 16, 7, "1234567")]
        public void TestGetPaddedRounded(string value, int maxLength, int expectedLength, string expected)
        {
            var x = Encoding.UTF8.GetBytes(value);
            var y = Encoding.UTF8.GetChars(x, 0, 3);
            var (byteLength, label) = Encoding.UTF8.GetValueLabelAsByteArray(value, maxLength);
            Assert.Equal(expectedLength, byteLength);
            Assert.Equal(expected, Encoding.UTF8.GetString(label));
        }
    }
}
