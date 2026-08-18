namespace QuickGrid.Samples.Core;

/// <summary>
/// The single source of truth for the example pages: drives the nav menu, the home page list and the
/// previous/next links on each example.
/// </summary>
/// <remarks>
/// To add an example, create the page under <c>Pages/Examples/</c> and add one entry here, in the position
/// it should appear. The list runs from the simplest example to the most involved.
/// </remarks>
public static class ExampleRegistry
{
    public static IReadOnlyList<Example> Examples { get; } =
    [
        new("users-grid", "QuickGrid + ColumnManager",
            "Write your own <QuickGrid> and render columns from a ColumnManager. The low-level building blocks.",
            "UsersGrid.razor"),

        new("users-grid-wrapper", "QuickGridWrapper",
            "The same columns inside one component that adds a toolbar, quick search and a column selector.",
            "UsersGridWrapper.razor"),

        new("column-types", "Column Types",
            "Every column helper side by side: text, dates, numbers, ticks, toggles, markup, images, templates and actions.",
            "ColumnTypes.razor"),

        new("formatting-styling", "Formatting & Styling",
            "Format strings, conditional cell styling with CellStyleMap, row classes and shared ColumnInfo definitions.",
            "FormattingStyling.razor"),

        new("loading-paging", "Loading, Paging & Refresh",
            "IsLoading, pagination, and keeping the grid in step with data that changes underneath it.",
            "LoadingPaging.razor"),

        new("search-filtering", "Search & Filtering",
            "Quick search across every column, exact match, nested properties, and your own filter panel.",
            "SearchFiltering.razor"),

        new("row-selection", "Row Selection",
            "Select rows with ISelectionDto and act on the selection from the toolbar.",
            "RowSelection.razor"),

        new("footers-totals", "Footers & Totals",
            "Automatic totals for numeric columns, or hand-built footer cells for full control.",
            "FootersTotals.razor"),

        new("export", "Export",
            "Wire the export events to produce a CSV of what the user is currently looking at.",
            "Export.razor"),

        new("saved-views", "Saved Views & Icons",
            "Persist column layouts as named views, and swap the toolbar icons for your own.",
            "SavedViews.razor"),

        new("app-quickgrid", "Your Own Grid Component",
            "Subclass QuickGridWrapper once to fix the styling and wire the events for a whole application.",
            "AppQuickGridExample.razor"),
    ];

    public static Example? Find(string route)
        => Examples.FirstOrDefault(e => string.Equals(e.Route, route, StringComparison.OrdinalIgnoreCase));

    public static Example? Previous(string route)
    {
        var index = IndexOf(route);

        return index > 0 ? Examples[index - 1] : null;
    }

    public static Example? Next(string route)
    {
        var index = IndexOf(route);

        return index >= 0 && index < Examples.Count - 1 ? Examples[index + 1] : null;
    }

    private static int IndexOf(string route)
    {
        for (var i = 0; i < Examples.Count; i++)
        {
            if (string.Equals(Examples[i].Route, route, StringComparison.OrdinalIgnoreCase)) return i;
        }

        return -1;
    }
}
