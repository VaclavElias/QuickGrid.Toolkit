using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace QuickGrid.Toolkit.Core;

/// <summary>
/// Utility class for performing quick searches on properties of an object.
/// </summary>
public static class QuickSearchUtility
{
    /// <summary>
    /// Cache for type property information to improve reflection performance.
    /// This cache is safe to keep for the application lifetime as type metadata is immutable.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

    private static readonly HashSet<Type> _simpleTypes =
    [
        typeof(string),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid)
    ];

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
        if (item is null || string.IsNullOrWhiteSpace(query)) return false;

        options ??= new QuickSearchOptions();

        var terms = GetSearchTerms(query, options);

        if (terms.Length == 0) return false;

        return options.MultiTermOperator == SearchOperator.And
            ? terms.All(term => MatchesObject(item, term, typeof(T), 0, options))
            : terms.Any(term => MatchesObject(item, term, typeof(T), 0, options));
    }

    private static bool MatchesObject(object item, string term, Type type, int depth, QuickSearchOptions options)
    {
        var properties = GetSearchableProperties(type, depth, options);

        foreach (var property in properties)
        {
            var value = property.GetValue(item);

            if (value is null || IsNonStringEnumerable(value))
            {
                continue;
            }

            if (IsSearchableLeafValue(value) && MatchesQuery(value, term, options))
            {
                return true;
            }

            if (ShouldTraverse(value, depth, options) && MatchesObject(value, term, value.GetType(), depth + 1, options))
            {
                return true;
            }
        }

        return false;
    }

    private static string[] GetSearchTerms(string query, QuickSearchOptions options)
    {
        if (options.ExactMatch)
        {
            return [query.Trim()];
        }

        return query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PropertyInfo[] GetSearchableProperties(Type type, int depth, QuickSearchOptions options)
    {
        var properties = _propertyCache.GetOrAdd(type, static t =>
            t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(static property => property.CanRead && property.GetIndexParameters().Length == 0)
                .ToArray());

        if (depth > 0)
        {
            return properties;
        }

        return properties
            .Where(property => IsIncludedProperty(property.Name, options))
            .ToArray();
    }

    private static bool IsIncludedProperty(string propertyName, QuickSearchOptions options)
    {
        if (options.ExcludedColumns?.Contains(propertyName, StringComparer.OrdinalIgnoreCase) == true)
        {
            return false;
        }

        if (options.ColumnNames is null || options.ColumnNames.Count == 0)
        {
            return true;
        }

        return options.ColumnNames.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSearchableLeafValue(object value)
    {
        var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();

        return type.IsPrimitive || type.IsEnum || _simpleTypes.Contains(type);
    }

    private static bool IsNonStringEnumerable(object value)
        => value is IEnumerable && value is not string;

    private static bool ShouldTraverse(object value, int depth, QuickSearchOptions options)
    {
        if (!options.IncludeChildProperties || depth >= options.MaxSearchDepth)
        {
            return false;
        }

        if (IsSearchableLeafValue(value) || value is string || IsNonStringEnumerable(value))
        {
            return false;
        }

        var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();

        return type.IsClass || type.IsValueType;
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