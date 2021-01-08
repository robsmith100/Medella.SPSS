namespace Curiosity.SPSS.SpssDataset
{
    /// <summary>
    ///     The SPSS data type
    /// </summary>
    public enum DataType
    {
        /// <summary>
        ///     Numeric data. Value should be a double (or null for SysMiss)
        /// </summary>
        Numeric = 0,

        /// <summary>
        ///     Text data. Value should be a string
        /// </summary>
        Text = 1
    }
}