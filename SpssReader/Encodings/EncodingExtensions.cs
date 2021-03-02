using System.Text;

namespace Spss.Encodings
{
    public static class EncodingExtensions
    {
        public static int GetCodePage(this int characterCode)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return characterCode switch
            {
                2 => Encoding.ASCII.CodePage,
                3 => Encoding.UTF8.CodePage,
                _ => Encoding.GetEncoding(characterCode).CodePage
            };
        }
    }
}