namespace QuickGrid.Toolkit.Columns;

/// <summary>
/// Bundles the heading and styling of a column so the same definition can be shared between grids.
/// </summary>
/// <param name="title">Short heading shown in the column header.</param>
/// <param name="fullTitle">Long heading used for tooltips and the column selector.</param>
/// <param name="class">CSS class applied to the column's cells.</param>
/// <param name="propertyName">Property name used when exporting selected columns.</param>
public class ColumnInfo(string? title, string? fullTitle, string? @class, string? propertyName = null)
{
    public string? Title { get; set; } = title;
    public string? FullTitle { get; set; } = fullTitle;
    public string? Class { get; set; } = @class;
    public string? PropertyName { get; set; } = propertyName;
}
