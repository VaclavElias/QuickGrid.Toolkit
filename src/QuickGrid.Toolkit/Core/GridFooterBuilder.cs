using System.Globalization;
using System.Net;
using System.Text;

namespace QuickGrid.Toolkit.Core;

/// <summary>
/// Builds the grid's footer row as HTML, either from footer cells the caller declared on the
/// <see cref="ColumnManager{TGridItem}"/> or by totalling the numeric columns automatically.
/// </summary>
/// <remarks>
/// Deliberately stateless: it takes the columns, the totals settings and the rows on show, and returns markup.
/// Nothing here reaches into the component, so the whole footer pipeline — cell alignment against visible columns,
/// label placement, total arithmetic, HTML encoding — is testable without rendering anything.
/// <para>
/// The markup is injected into the table through <c>tfoot.innerHTML</c> on the JS side, which is why every value
/// and class is HTML-encoded here.
/// </para>
/// </remarks>
internal static class GridFooterBuilder<TGridItem>
{
    /// <summary>Columns carrying this class are laid out as if hidden, so the footer must skip them too.</summary>
    private const string HiddenColumnClass = "d-none";

    public static bool HasFooter(ColumnManager<TGridItem> columns, TotalFooter totalFooter)
        => columns.FooterColumns.Count > 0 || totalFooter.IsTotalFooter;

    /// <summary>
    /// Builds the complete <c>&lt;tr&gt;</c> for the footer. Declared footer cells win over automatic totals.
    /// </summary>
    /// <param name="columns">The column set being rendered; only visible, non-hidden columns get a cell.</param>
    /// <param name="totalFooter">Label, default format and class handling for automatic totals.</param>
    /// <param name="items">The rows the totals aggregate over — the search result when one is active.</param>
    public static string Build(ColumnManager<TGridItem> columns, TotalFooter totalFooter, IReadOnlyList<TGridItem> items)
    {
        var html = columns.FooterColumns.Count > 0
            ? BuildDeclaredCells(columns, items)
            : BuildAutomaticCells(columns, totalFooter, items);

        return $"<tr class=\"table-warning fw-bold\">{html}</tr>";
    }

    private static string BuildDeclaredCells(ColumnManager<TGridItem> columns, IReadOnlyList<TGridItem> items)
    {
        StringBuilder html = new();

        foreach (var column in GetFooterColumns(columns))
        {
            var footerColumn = columns.FooterColumns.FirstOrDefault(w => w.Id == column.Id);

            if (footerColumn is null)
            {
                html.Append("<td></td>");
            }
            else if (footerColumn.Content != null)
            {
                html.Append(footerColumn.Content);
            }
            else
            {
                html.Append(footerColumn.StringContent?.Invoke(items));
            }
        }

        return html.ToString();
    }

    private static string BuildAutomaticCells(ColumnManager<TGridItem> columns, TotalFooter totalFooter, IReadOnlyList<TGridItem> items)
    {
        StringBuilder html = new();

        var visibleColumns = GetFooterColumns(columns).ToList();
        var labelColumn = GetLabelColumn(visibleColumns, totalFooter);

        foreach (var column in visibleColumns)
        {
            if (column == labelColumn)
            {
                html.Append(BuildCell(totalFooter.TotalFooterLabel, column.Class));
            }
            else if (ShouldCalculateTotal(column))
            {
                html.Append(BuildTotalCell(column, totalFooter, items));
            }
            else
            {
                html.Append("<td></td>");
            }
        }

        return html.ToString();
    }

    /// <summary>The columns that occupy a cell in the rendered table, in render order.</summary>
    private static IEnumerable<DynamicColumn<TGridItem>> GetFooterColumns(ColumnManager<TGridItem> columns)
        => columns.Columns.Where(w => w.Visible && w.Class != HiddenColumnClass);

    private static DynamicColumn<TGridItem>? GetLabelColumn(List<DynamicColumn<TGridItem>> visibleColumns, TotalFooter totalFooter)
        => totalFooter.TotalFooterLabelColumnId is int labelColumnId
            ? visibleColumns.FirstOrDefault(w => w.Id == labelColumnId)
            : visibleColumns.FirstOrDefault(w => !w.IsNumeric);

    private static bool ShouldCalculateTotal(DynamicColumn<TGridItem> column)
        => column.Property is not null && column.CalculateTotal switch
        {
            true => true,
            false => false,
            _ => column.IsNumeric
        };

    private static string BuildTotalCell(DynamicColumn<TGridItem> column, TotalFooter totalFooter, IReadOnlyList<TGridItem> items)
    {
        // Compiled once per column and reused: compiling here would run on every render of every total.
        var compiledProperty = column.GetCompiledProperty()!;
        var total = items.Sum(item => Convert.ToDecimal(compiledProperty(item)));
        var format = column.Format ?? totalFooter.DefaultFormat;
        var cssClass = totalFooter.RemoveClass is { Length: > 0 } removeClass
            ? column.Class?.Replace(removeClass, "")
            : column.Class;

        return BuildCell(total.ToString(format, CultureInfo.InvariantCulture), cssClass);
    }

    private static string BuildCell(string? value, string? cssClass)
    {
        var classAttribute = string.IsNullOrWhiteSpace(cssClass)
            ? string.Empty
            : $" class=\"{WebUtility.HtmlEncode(cssClass)}\"";

        return $"<td{classAttribute}>{WebUtility.HtmlEncode(value)}</td>";
    }
}