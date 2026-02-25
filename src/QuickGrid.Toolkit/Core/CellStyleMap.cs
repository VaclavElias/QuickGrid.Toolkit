namespace QuickGrid.Toolkit.Core;

/// <summary>
/// Provides styling mappings for cell values, including support for null values.
/// </summary>
/// <typeparam name="TValue">The type of values to map</typeparam>
public class CellStyleMap<TValue>
{
    private readonly List<CellStyle<TValue>> _styleMappings = [];
    private string? _nullValueStyle;

    /// <summary>
    /// Adds a style mapping for a specific value.
    /// </summary>
    /// <param name="value">The value to map</param>
    /// <param name="style">The style to apply</param>
    /// <returns>This instance for method chaining</returns>
    public CellStyleMap<TValue> Add(TValue value, string style)
    {
        if (value is null)
        {
            _nullValueStyle = style;
        }
        else
        {
            _styleMappings.Add(new CellStyle<TValue>(value, style));
        }

        return this;
    }

    /// <summary>
    /// Adds multiple style mappings.
    /// </summary>
    /// <param name="mappings">The mappings to add</param>
    /// <returns>This instance for method chaining</returns>
    public CellStyleMap<TValue> AddRange(IEnumerable<CellStyle<TValue>> mappings)
    {
        _styleMappings.AddRange(mappings);

        return this;
    }

    /// <summary>
    /// Sets the style to use for null values.
    /// </summary>
    /// <param name="style">The style to apply to null values</param>
    /// <returns>This instance for method chaining</returns>
    public CellStyleMap<TValue> SetNullStyle(string style)
    {
        _nullValueStyle = style;

        return this;
    }

    /// <summary>
    /// Gets the style for a given value.
    /// </summary>
    /// <param name="value">The value to get the style for</param>
    /// <returns>The style string, or empty string if no mapping exists</returns>
    public string GetStyle(TValue? value)
    {
        if (value is null)
        {
            return _nullValueStyle ?? string.Empty;
        }

        var mapping = _styleMappings.FirstOrDefault(m => EqualityComparer<TValue>.Default.Equals(m.Value, value));

        return mapping?.Style ?? string.Empty;
    }

    /// <summary>
    /// Checks if a mapping exists for the given value.
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <returns>True if a mapping exists</returns>
    public bool ContainsValue(TValue? value)
    {
        if (value is null)
        {
            return _nullValueStyle is not null;
        }

        return _styleMappings.Any(m => EqualityComparer<TValue>.Default.Equals(m.Value, value));
    }

    /// <summary>
    /// Creates a CellStyleMap from a collection of CellStyle mappings.
    /// </summary>
    /// <param name="mappings">The style mappings</param>
    /// <param name="nullValueStyle">Optional style for null values</param>
    /// <returns>A new CellStyleMap instance</returns>
    public static CellStyleMap<TValue> FromMappings(IEnumerable<CellStyle<TValue>> mappings, string? nullValueStyle = null)
    {
        var map = new CellStyleMap<TValue>();
        map.AddRange(mappings);

        if (nullValueStyle is not null)
        {
            map.SetNullStyle(nullValueStyle);
        }

        return map;
    }

    /// <summary>
    /// Clears all mappings. Optionally clears the null value style.
    /// </summary>
    /// <param name="clearNullStyle">When true, also clears the null value style.</param>
    /// <returns>This instance for method chaining</returns>
    public CellStyleMap<TValue> Clear(bool clearNullStyle = true)
    {
        _styleMappings.Clear();

        if (clearNullStyle) _nullValueStyle = null;

        return this;
    }

    /// <summary>
    /// Replaces all mappings (and optional null style) in one step.
    /// </summary>
    /// <param name="mappings">New mappings</param>
    /// <param name="nullValueStyle">Optional style for null values</param>
    /// <returns>This instance for method chaining</returns>
    public CellStyleMap<TValue> ReplaceWith(IEnumerable<CellStyle<TValue>> mappings, string? nullValueStyle = null)
    {
        _styleMappings.Clear();
        _styleMappings.AddRange(mappings);
        _nullValueStyle = nullValueStyle;

        return this;
    }

    /// <summary>
    /// Removes the mapping for the given value. When value is null, clears the null style.
    /// </summary>
    public bool Remove(TValue? value)
    {
        if (value is null)
        {
            var hadNull = _nullValueStyle is not null;
            _nullValueStyle = null;

            return hadNull;
        }

        var idx = _styleMappings.FindIndex(m => EqualityComparer<TValue>.Default.Equals(m.Value, value));

        if (idx >= 0)
        {
            _styleMappings.RemoveAt(idx);

            return true;
        }

        return false;
    }
}