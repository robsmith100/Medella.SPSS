namespace Spss.SpssMetadata;

/// <summary>
///     The measurement type of the variable
/// </summary>
public enum MeasurementType
{
    /// <summary>
    ///     Nominal scale fixed set of values no order
    /// </summary>
    Nominal = 1,

    /// <summary>
    ///     Ordinal scale fixed set of values with order
    /// </summary>
    Ordinal = 2,

    /// <summary>
    ///     Continuous scale
    /// </summary>
    Scale = 3
}