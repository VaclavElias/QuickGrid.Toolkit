namespace QuickGrid.Toolkit.Core;

/// <summary>
/// Options for configuring quick search behavior.
/// </summary>
public class QuickSearchOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to search in first-level child properties.
    /// Default is true.
    /// </summary>
    public bool IncludeChildProperties { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use exact matching (case-insensitive) instead of substring matching.
    /// Default is false (substring matching).
    /// </summary>
    public bool ExactMatch { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the search should be case-sensitive.
    /// Default is false (case-insensitive).
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Gets or sets a list of property names to search in. If null or empty, searches all properties.
    /// </summary>
    public List<string>? ColumnNames { get; set; }

    /// <summary>
    /// Properties to exclude from search.
    /// </summary>
    public List<string>? ExcludedColumns { get; set; }

    public SearchOperator MultiTermOperator { get; set; } = SearchOperator.And;

    /// <summary>
    /// Gets or sets a value indicating whether a term prefixed with <c>-</c> excludes the rows it matches, so
    /// <c>london -manager</c> keeps London rows that say nothing about a manager. Default is true.
    /// </summary>
    /// <remarks>
    /// An exclusion always vetoes the item, whatever <see cref="MultiTermOperator"/> is, and a query made only of
    /// exclusions keeps everything they do not veto. A lone <c>-</c>, and a <c>-</c> anywhere but at the start of
    /// a term, are ordinary text. Turn this off when a leading hyphen is meaningful data - searching a column of
    /// negative numbers for <c>-500</c>, say - and the prefix is then matched literally.
    /// </remarks>
    public bool EnableExclusionTerms { get; set; } = true;

    /// <summary>
    /// Maximum depth to search in nested properties. 0 = current level only, 1 = first-level children, etc.
    /// Default is 1, so <c>Owner.Address</c> is searched but <c>Owner.Address.City</c> is not - raise it to 2
    /// for that. Each level costs a reflection walk of every property on every row, per term, so raise it
    /// deliberately, and per grid through <c>QuickGridWrapper.SearchOptions</c> rather than for the whole app.
    /// </summary>
    public int MaxSearchDepth { get; set; } = 1;

    /// <summary>
    /// An independent copy. The two list properties are copied as well, so a caller can go on mutating theirs
    /// without changing the copy underneath whoever is holding it.
    /// </summary>
    internal QuickSearchOptions Clone() => new()
    {
        IncludeChildProperties = IncludeChildProperties,
        ExactMatch = ExactMatch,
        CaseSensitive = CaseSensitive,
        ColumnNames = ColumnNames is null ? null : [.. ColumnNames],
        ExcludedColumns = ExcludedColumns is null ? null : [.. ExcludedColumns],
        MultiTermOperator = MultiTermOperator,
        EnableExclusionTerms = EnableExclusionTerms,
        MaxSearchDepth = MaxSearchDepth
    };

    /// <summary>
    /// Whether two option sets would narrow a grid the same way, compared by value.
    /// </summary>
    /// <remarks>
    /// Reference equality is not usable here. A grid whose markup reads
    /// <c>SearchOptions="new() { MaxSearchDepth = 3 }"</c> allocates a fresh instance on every render, and taking
    /// that for a change would re-run the search - and re-raise the search-result event - on every render. The
    /// lists are compared by content for the mirror-image reason: a caller who mutates one in place keeps the
    /// same reference, and a reference check would never notice.
    /// </remarks>
    internal static bool ValuesEqual(QuickSearchOptions? left, QuickSearchOptions? right)
    {
        if (ReferenceEquals(left, right)) return true;

        if (left is null || right is null) return false;

        return left.IncludeChildProperties == right.IncludeChildProperties
            && left.ExactMatch == right.ExactMatch
            && left.CaseSensitive == right.CaseSensitive
            && left.MultiTermOperator == right.MultiTermOperator
            && left.EnableExclusionTerms == right.EnableExclusionTerms
            && left.MaxSearchDepth == right.MaxSearchDepth
            && SameContent(left.ColumnNames, right.ColumnNames)
            && SameContent(left.ExcludedColumns, right.ExcludedColumns);
    }

    private static bool SameContent(List<string>? left, List<string>? right)
    {
        if (ReferenceEquals(left, right)) return true;

        // Null and empty both mean "no filter" to the search, so moving between them is not a change.
        if (left is null || right is null)
        {
            return left is null or { Count: 0 } && right is null or { Count: 0 };
        }

        return left.SequenceEqual(right, StringComparer.Ordinal);
    }
}

public enum SearchOperator
{
    And,
    Or
}