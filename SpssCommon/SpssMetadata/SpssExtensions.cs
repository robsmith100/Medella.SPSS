namespace Spss.SpssMetadata;

public static class SpssExtensions
{
    public static bool IsDate(this FormatType format)
    {
        return format == FormatType.ADATE
               || format == FormatType.DATE
               || format == FormatType.DATETIME
               || format == FormatType.EDATE
               || format == FormatType.JDATE
               || format == FormatType.SDATE;
    }
}