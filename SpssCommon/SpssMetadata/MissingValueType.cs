namespace Spss.SpssMetadata;

/// <summary>
///     The kind of custom missing values for a variable
/// </summary>
public enum MissingValueType
{
    /// <summary>
    ///     No custom missing values for the variable
    /// </summary>
    NoMissingValues = 0,

    /// <summary>
    ///     One specific custom missing value. The missing value should be specified on the fist item on
    /// </summary>
    OneDiscreteMissingValue = 1,

    /// <summary>
    ///     Two specific custom missing values. The missing values should be specified on the fist and second items on
    /// </summary>
    TwoDiscreteMissingValue = 2,

    /// <summary>
    ///     Two specific custom missing values. The missing values should be specified on the fist, second and third items on
    /// </summary>
    ThreeDiscreteMissingValue = 3,

    /// <summary>
    ///     Defines a range to be treated as missing values, from the first item in the on
    /// </summary>
    Range = -2,

    /// <summary>
    ///     Identical to Range, but with an additional discrete value specified on the third item of
    /// </summary>
    RangeAndDiscrete = -3
}