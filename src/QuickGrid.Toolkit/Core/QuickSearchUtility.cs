using System.Collections;

namespace QuickGrid.Toolkit.Core;

/// <summary>
/// Utility class for performing quick searches on properties of an object.
/// </summary>
public static class QuickSearchUtility
{
    /// <summary>
    /// Searches the properties of an object to find a match for the specified query.
    /// Optionally, it can also search in the first-level child properties.
    /// </summary>
    /// <typeparam name="T">The type of the object being searched.</typeparam>
    /// <param name="item">The object to search.</param>
    /// <param name="query">The search query string.</param>
    /// <param name="includeChildProperties">Flag to determine whether to search in child properties as well.</param>
    /// <returns>True if the query is found in any property; otherwise, false.</returns>
    public static bool QuickSearch<T>(T item, string query, bool includeChildProperties = true)
    {
        var options = new QuickSearchOptions
        {
            IncludeChildProperties = includeChildProperties
        };
        return QuickSearch(item, query, options);
    }

    /// <summary>
    /// Searches the properties of an object to find a match for the specified query with configurable options.
    /// </summary>
    /// <typeparam name="T">The type of the object being searched.</typeparam>
    /// <param name="item">The object to search.</param>
    /// <param name="query">The search query string.</param>
    /// <param name="options">Options for configuring the search behavior.</param>
    /// <returns>True if the query is found in any property; otherwise, false.</returns>
    public static bool QuickSearch<T>(T item, string query, QuickSearchOptions options)
    {
        options ??= new QuickSearchOptions();

        foreach (var property in typeof(T).GetProperties())
        {
            var value = property.GetValue(item);

            // Skip if the value is a collection (but not a string)
            if (value is IEnumerable && value is not string)
            {
                continue;
            }

            if (MatchesQuery(value, query, options)) return true;

            // Optionally search inside child properties if the value is a class (but not a string)
            if (options.IncludeChildProperties && IsClass(value) && value is not string)
            {
                foreach (var childProperty in value!.GetType().GetProperties())
                {
                    var childValue = childProperty.GetValue(value);

                    if (MatchesQuery(childValue, query, options)) return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the provided object is a class and not a primitive type or string.
    /// </summary>
    /// <param name="value">The object to check.</param>
    /// <returns>True if the object is a class (but not a string); otherwise, false.</returns>
    private static bool IsClass(object? value)
    {
        if (value is null) return false;

        var type = value.GetType();

        return type.IsClass && !type.IsPrimitive && type != typeof(string);
    }

    /// <summary>
    /// Determines whether a given value matches the search query.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="query">The search query string.</param>
    /// <param name="options">Options for configuring the match behavior.</param>
    /// <returns>True if the value matches the query; otherwise, false.</returns>
    private static bool MatchesQuery(object? value, string query, QuickSearchOptions options)
    {
        if (value is null) return false;

        var valueString = value.ToString();

        if (valueString is null) return false;

        var comparison = options.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        if (options.ExactMatch)
        {
            return valueString.Equals(query, comparison);
        }

        return valueString.Contains(query, comparison);
    }
}