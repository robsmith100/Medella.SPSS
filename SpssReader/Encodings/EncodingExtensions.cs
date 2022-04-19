using System.Text;

namespace Spss.Encodings;

public static class EncodingExtensions
{
    public static int GetCodePage(this int characterCode)
    {
#if NETCOREAPP
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        return characterCode switch
        {
            2 => Encoding.ASCII.CodePage,
            3 => Encoding.UTF8.CodePage,
            _ => Encoding.GetEncoding(characterCode).CodePage
        };
    }
}