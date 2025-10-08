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
    /// Maximum depth to search in nested properties. 0 = current level only, 1 = first-level children, etc.
    /// Default is 1.
    /// </summary>
    public int MaxSearchDepth { get; set; } = 1;
}

public enum SearchOperator
{
    And,
    Or
}