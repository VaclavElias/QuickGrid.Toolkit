using System.Globalization;

namespace QuickGrid.Toolkit.Helpers;

/// <summary>
/// Provides utility methods for determining cell styling based on values.
/// </summary>
public static class CellStyleHelper
{
    private const string NegativeDescription = "negative";
    private const string PositiveDescription = "positive";
    private const string ZeroDescription = "zero";
    private const string UnknownDescription = "unknown";
    private const string NoValueDescription = "no-value";

    /// <summary>
    /// Determines the style description for a numeric value.
    /// First checks for custom styling from CellStyleMap, then falls back to default numeric nature determination.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value to determine styling for.</param>
    /// <param name="cellStyle">Optional custom cell style map.</param>
    /// <returns>A string describing the value's nature or custom style.</returns>
    public static string DetermineNumericValueNature<TValue>(TValue? value, CellStyleMap<TValue>? cellStyle = null) where TValue : struct
    {
        // First check for custom styling
        if (cellStyle != null)
        {
            if (value.HasValue && cellStyle.ContainsValue(value.Value))
            {
                return cellStyle.GetStyle(value.Value);
            }
            if (!value.HasValue && cellStyle.ContainsValue(default(TValue)))
            {
                return cellStyle.GetStyle(default(TValue));
            }
        }

        // Default numeric value nature determination. Every numeric type is handled through IConvertible,
        // so long, float, short and friends are described as well, not just int/decimal/double.
        return value switch
        {
            null => NoValueDescription,
            IConvertible convertible when IsNumeric(convertible.GetTypeCode())
                => DescribeSign(convertible.ToDouble(CultureInfo.InvariantCulture)),
            _ => UnknownDescription
        };
    }

    /// <summary>
    /// True for the numeric type codes, which sit contiguously between <see cref="TypeCode.SByte"/> and
    /// <see cref="TypeCode.Decimal"/>. Deliberately excludes bool, char and DateTime, which are convertible but not numeric.
    /// </summary>
    private static bool IsNumeric(TypeCode typeCode)
        => typeCode is >= TypeCode.SByte and <= TypeCode.Decimal;

    private static string DescribeSign(double value) => value switch
    {
        < 0 => NegativeDescription,
        > 0 => PositiveDescription,
        0 => ZeroDescription,
        _ => UnknownDescription // NaN
    };

    /// <summary>
    /// Whether a style description should be rendered, given the natures a grid has enabled.
    /// </summary>
    /// <remarks>
    /// Only the three built-in descriptions are governed. Anything else — a name from a
    /// <see cref="CellStyleMap{TValue}"/>, or <c>unknown</c>/<c>no-value</c> — is the caller's own vocabulary and is
    /// always rendered.
    /// </remarks>
    /// <param name="style">The style description, typically from <see cref="DetermineNumericValueNature"/>.</param>
    /// <param name="enabledStyles">The natures the grid marks up.</param>
    public static bool IsStyleEnabled(string? style, GridValueStyles enabledStyles) => style switch
    {
        NegativeDescription => enabledStyles.HasFlag(GridValueStyles.Negative),
        PositiveDescription => enabledStyles.HasFlag(GridValueStyles.Positive),
        ZeroDescription => enabledStyles.HasFlag(GridValueStyles.Zero),
        _ => true
    };

    /// <summary>
    /// Gets the style for a value from the provided cell style map.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value to get styling for.</param>
    /// <param name="cellStyle">The cell style map to use.</param>
    /// <returns>The style string, or empty string if no style map is provided or no mapping exists.</returns>
    public static string GetValueStyle<TValue>(TValue? value, CellStyleMap<TValue>? cellStyle = null)
    {
        return cellStyle?.GetStyle(value) ?? string.Empty;
    }
}