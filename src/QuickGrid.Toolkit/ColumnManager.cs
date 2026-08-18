using QuickGrid.Toolkit.Builders;
using System.Globalization;
using System.Net;

namespace QuickGrid.Toolkit;

public class ColumnManager<TGridItem>
{
    private readonly ColumnBuilder<TGridItem> _columnBuilder = new();

    public bool IsIndexColumn { get; set; } = true;
    public List<DynamicColumn<TGridItem>> Columns { get; } = [];
    public List<FooterColumn<IEnumerable<TGridItem>>> FooterColumns { get; } = [];

    /// <summary>
    /// Returns visible columns
    /// </summary>
    public IEnumerable<DynamicColumn<TGridItem>> Get() => Columns.Where(w => w.Visible);

    public void Add(DynamicColumn<TGridItem>? column = default)
    {
        if (column == null) return;

        if (string.IsNullOrWhiteSpace(column.Title))
            column.Title = ExpressionHelper.GetPropertyName<TGridItem, object>(column.Property) ?? "Title n/a";

        if (string.IsNullOrEmpty(column.PropertyName))
            column.PropertyName = ExpressionHelper.GetSafePropertyName<TGridItem, object>(column.Property);

        Columns.Add(column);

        column.Id = Columns.Count;
    }

    /// <summary>
    /// Adds a collection of columns to the column manager, ensuring each column receives a correct sequential ID.
    /// </summary>
    /// <remarks>
    /// <para>This method iterates through the provided columns and adds each one individually using the <see cref="Add(DynamicColumn{TGridItem}?)"/> method.
    /// This ensures that each column gets properly initialized with the correct ID, title, and property name.</para>
    /// <para><strong>Important:</strong> Do not use <c>Columns.AddRange</c> directly, as it bypasses the ID assignment logic and other initialization performed by the <see cref="Add(DynamicColumn{TGridItem}?)"/> method.</para>
    /// </remarks>
    /// <param name="columns">The collection of <see cref="DynamicColumn{TGridItem}"/> objects to add. Null columns in the collection are ignored.</param>
    /// <seealso cref="Add(DynamicColumn{TGridItem}?)"/>
    public void AddRange(IEnumerable<DynamicColumn<TGridItem>> columns)
    {
        foreach (var column in columns)
        {
            Add(column);
        }
    }

    public void Add<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        ColumnInfo columnInfo,
        string? format = null,
        Align align = Align.Left,
        CellStyleMap<TValue>? cellStyle = null,
        GridSort<TGridItem>? sortBy = null,
        bool visible = true,
        string? propertyName = null)
    {
        Add(expression, columnInfo.Title, columnInfo.FullTitle, format, columnInfo.Class, align, cellStyle, sortBy, visible, propertyName ?? columnInfo.PropertyName);
    }

    /// <summary>
    /// Adds a column to the grid based on a specified expression.
    /// </summary>
    /// <param name="expression">An expression to determine the property of the grid item to display.</param>
    /// <param name="title">The title of the column. If null or whitespace, the property name is used.</param>
    /// <param name="format">The format string for IFormattable values.</param>
    public void Add<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        string? title = null,
        string? fullTitle = null,
        string? format = null,
        string? @class = null,
        Align align = Align.Left,
        CellStyleMap<TValue>? cellStyle = null,
        GridSort<TGridItem>? sortBy = null,
        bool visible = true,
        string? propertyName = null,
        bool? addToContent = null)
    {
        var column = _columnBuilder.BuildSimpleColumn(
            expression, title, fullTitle, format, @class, align, cellStyle, sortBy, visible, propertyName, addToContent);
        Add(column);
    }

    public void AddSimple<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        ColumnInfo columnInfo,
        string? format = null,
        Align align = Align.Left,
        CellStyleMap<TValue>? cellStyle = null,
        GridSort<TGridItem>? sortBy = null,
        bool visible = true,
        string? propertyName = null)
    {
        Add(expression, columnInfo, format, align, cellStyle, sortBy, visible, propertyName);
    }

    /// <summary>
    /// Adds a simple column to the grid based on a specified expression.
    /// </summary>
    /// <param name="expression">An expression to determine the property of the grid item to display.</param>
    /// <param name="title">The title of the column. If null or whitespace, the property name is used.</param>
    /// <param name="format">The format string for IFormattable values.</param>
    public void AddSimple<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        string? title = null,
        string? fullTitle = null,
        string? format = null,
        string? @class = null,
        Align align = Align.Left,
        CellStyleMap<TValue>? cellStyle = null,
        GridSort<TGridItem>? sortBy = null,
        bool visible = true,
        string? propertyName = null,
        bool? addToContent = null)
    {
        Add(expression, title, fullTitle, format, @class, align, cellStyle, sortBy, visible, propertyName, addToContent);
    }

    // ToDo: Rename to AddDate()
    public void AddSimpleDate<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        string? title = null,
        string? fullTitle = null,
        string? format = "dd/MM/yyyy",
        string? @class = null,
        Align align = Align.Center,
        CellStyleMap<TValue>? cellStyle = null,
        bool visible = true)
            => AddSimple(expression, title, fullTitle, format, @class, align, cellStyle, visible: visible);

    public void AddAction(Expression<Func<TGridItem, object?>> expression, ColumnInfo columnInfo, Align align = Align.Left, GridSort<TGridItem>? sortBy = null,
        bool visible = true, Func<TGridItem, Task>? onClick = null)
    {
        AddAction(expression, columnInfo.Title, columnInfo.FullTitle, align, columnInfo.Class, sortBy, visible, onClick, columnInfo.PropertyName);
    }

    public void AddAction(Expression<Func<TGridItem, object?>> expression, string? title = null, string? fullTitle = null, Align align = Align.Left, string? @class = null, GridSort<TGridItem>? sortBy = null,
        bool visible = true, Func<TGridItem, Task>? onClick = null, string? propertyName = null)
    {
        var column = _columnBuilder.BuildActionColumn(expression, title, fullTitle, align, @class, sortBy, visible, onClick, propertyName);
        Add(column);
    }

    public void AddAction(
        string staticContent,
        string? title = null,
        Align align = Align.Left,
        string? @class = null,
        Func<TGridItem, Task>? onClick = null,
        Expression<Func<TGridItem, bool>>? enabled = null)
    {
        var column = _columnBuilder.BuildStaticActionColumn(staticContent, title, align, @class, onClick, enabled);
        Add(column);
    }

    // Note: these overloads are deliberately NOT merged into one generic AddNumber<TValue>. A generic parameter
    // of type Expression<Func<TGridItem, TValue?>> cannot infer TValue from a non-nullable property such as
    // `s => s.Count` (int), so every such call site would have to name the type argument explicitly.
    // The signatures are kept identical to each other so the same arguments work whatever the numeric type.

    /// <summary>
    /// Adds a right-aligned decimal column to the grid.
    /// </summary>
    /// <param name="format">Format string applied with the invariant culture.</param>
    /// <param name="propertyName">Property name used when exporting selected columns.</param>
    public void AddNumber(Expression<Func<TGridItem, decimal?>> expression, string? title = null, string? fullTitle = null, string format = "N0", string? @class = null, Align align = Align.Right, bool visible = true, string? propertyName = null)
    {
        var column = _columnBuilder.BuildNumberColumn(expression, title, fullTitle, format, @class, align, visible, propertyName);
        Add(column);
    }

    /// <summary>
    /// Adds a right-aligned double column to the grid.
    /// </summary>
    /// <param name="format">Format string applied with the invariant culture.</param>
    /// <param name="propertyName">Property name used when exporting selected columns.</param>
    public void AddNumber(Expression<Func<TGridItem, double?>> expression, string? title = null, string? fullTitle = null, string format = "N0", string? @class = null, Align align = Align.Right, bool visible = true, string? propertyName = null)
    {
        var column = _columnBuilder.BuildNumberColumn(expression, title, fullTitle, format, @class, align, visible, propertyName);
        Add(column);
    }

    /// <summary>
    /// Adds a right-aligned int column to the grid.
    /// </summary>
    /// <param name="format">Format string applied with the invariant culture.</param>
    /// <param name="propertyName">Property name used when exporting selected columns.</param>
    public void AddNumber(Expression<Func<TGridItem, int?>> expression, string? title = null, string? fullTitle = null, string format = "N0", string? @class = null, Align align = Align.Right, bool visible = true, string? propertyName = null)
    {
        var column = _columnBuilder.BuildNumberColumn(expression, title, fullTitle, format, @class, align, visible, propertyName);
        Add(column);
    }

    public void AddStyledNumber<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        ColumnInfo columnInfo,
        string format = "N0",
        Align align = Align.Right,
        bool visible = true,
        CellStyleMap<TValue>? cellStyle = null,
        Func<TGridItem, Task>? onClick = null,
        bool? calculateTotal = null) where TValue : struct, IFormattable
        => AddStyledNumber(expression, columnInfo.Title, columnInfo.FullTitle, format, columnInfo.Class, align, visible, cellStyle, onClick, columnInfo.PropertyName, calculateTotal);

    public void AddStyledNumber<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        string? title = null,
        string? fullTitle = null,
        string format = "N0",
        string? @class = null,
        Align align = Align.Right,
        bool visible = true,
        CellStyleMap<TValue>? cellStyle = null,
        Func<TGridItem, Task>? onClick = null,
        string? propertyName = null,
        bool? calculateTotal = null) where TValue : struct, IFormattable
    {
        var column = _columnBuilder.BuildStyledNumberColumn(
            expression, title, fullTitle, format, @class, align, visible, cellStyle, onClick, propertyName, calculateTotal);
        Add(column);
    }

    public void AddTickColumn(
        Expression<Func<TGridItem, object?>> expression,
        string? title = null,
        string? fullTitle = null,
        string? @class = null,
        Align align = Align.Center,
        bool visible = true,
        string? trueClass = null,
        string? falseClass = null,
        bool showOnlyTrue = false,
        Func<TGridItem, Task>? onClick = null)
    {
        var column = new TickPropertyColumn<TGridItem>()
        {
            Property = expression,
            Title = title,
            FullTitle = fullTitle,
            ColumnType = typeof(TickColumn<TGridItem>),
            Align = align,
            ShowOnlyTrue = showOnlyTrue,
            TrueClass = trueClass,
            FalseClass = falseClass,
            Class = @class,
            OnActionAsync = onClick,
            Visible = visible
        };

        Add(column);
    }

    public void AddToggleColumn(
        Expression<Func<TGridItem, object?>> expression,
        string? title = null,
        string? fullTitle = null,
        string? @class = "text-center",
        Align align = Align.Center,
        Func<TGridItem, Task>? onChange = null)
    {

        Add(new()
        {
            Property = expression,
            Title = title,
            FullTitle = fullTitle,
            ColumnType = typeof(ToggleColumn<TGridItem>),
            Align = align,
            Class = @class,
            OnActionAsync = onChange
        });
    }

    public void AddImageColumn(Expression<Func<TGridItem, object?>> expression, string? title = null, Align align = Align.Center, string? @class = null)
    {
        Add(new() { Property = expression, ColumnType = typeof(ImageColumn<TGridItem>), Title = title, Align = align, Class = @class });
    }

    public void AddTemplateColumn(RenderFragment<TGridItem> childContent, string? title = null, string? fullTitle = null, Align align = Align.Center, GridSort<TGridItem>? sortBy = null, string? cssClass = null)
    {
        Add(new() { ChildContent = childContent, ColumnType = typeof(TemplateColumn<TGridItem>), Title = title, Align = align, FullTitle = fullTitle, SortBy = sortBy, Class = cssClass });
    }

    public void AddIndexColumn(string title = "#", Align align = Align.Center)
        => Add(new() { ColumnType = typeof(EmptyColumn<TGridItem>), Title = title, Align = align, Class = "index-column" });

    /// <summary>
    /// Creates a shallow copy of the current list of <see cref="DynamicColumn{TGridItem}"/> objects,
    /// cloning only basic properties such as Title, FullTitle, Property, ColumnType, Format, and Visibility.
    /// </summary>
    /// <remarks>
    /// This method performs a shallow copy, meaning that only the values of the properties are copied.
    /// Any modifications to the properties of the cloned objects will not affect the original objects in the list.
    /// </remarks>
    /// <returns>A new list containing cloned instances of <see cref="DynamicColumn{TGridItem}"/>
    /// where each column retains the basic property values of the corresponding original column.</returns>
    public List<DynamicColumn<TGridItem>> SimpleClone()
    {
        return Columns.ConvertAll(s => new DynamicColumn<TGridItem>
        {
            Id = s.Id,
            Title = s.Title,
            FullTitle = s.FullTitle,
            Property = s.Property,
            ColumnType = s.ColumnType,
            Format = s.Format,
            Visible = s.Visible
        });
    }

    /// <summary>
    /// Adds a footer cell holding a fixed, pre-computed value. The value is rendered once and does not
    /// change as the grid is searched or filtered.
    /// </summary>
    /// <param name="id">The <see cref="DynamicColumn{TGridItem}.Id"/> of the column this footer cell sits under.</param>
    /// <param name="value">The value to render. <see cref="IFormattable"/> values are formatted with <paramref name="format"/> using the invariant culture.</param>
    /// <param name="format">Optional format string applied to <paramref name="value"/>.</param>
    /// <param name="class">Optional CSS class for the generated <c>&lt;td&gt;</c>.</param>
    public void AddFooterColumn(
        int id,
        object? value,
        string? format = null,
        string? @class = null,
        Align align = Align.Left,
        bool visible = true)
    {
        FooterColumn<IEnumerable<TGridItem>> column = new()
        {
            Id = id,
            Format = format,
            Class = @class,
            Align = align,
            Visible = visible,
        };

        var displayValue = BuildDisplayValue(value, format);

        column.Content = BuildFooterCell(displayValue, column.Class);

        FooterColumns.Add(column);
    }

    /// <summary>
    /// Adds a footer cell whose value is calculated from the rows currently shown in the grid.
    /// </summary>
    /// <remarks>
    /// The expression is compiled once, then re-evaluated every time the footer is rendered, so the
    /// result follows the active quick search or filter.
    /// </remarks>
    /// <param name="id">The <see cref="DynamicColumn{TGridItem}.Id"/> of the column this footer cell sits under.</param>
    /// <param name="expression">An aggregation over the displayed rows, for example <c>items =&gt; items.Sum(i =&gt; i.Amount)</c>.</param>
    /// <param name="format">Optional format string applied to the result.</param>
    /// <param name="class">Optional CSS class for the generated <c>&lt;td&gt;</c>.</param>
    public void AddFooterColumn<TValue>(
        int id,
        Expression<Func<IEnumerable<TGridItem>, TValue?>> expression,
        string? format = null,
        string? @class = null,
        Align align = Align.Left,
        bool visible = true)
    {
        FooterColumn<IEnumerable<TGridItem>> column = new()
        {
            Id = id,
            Format = format,
            Class = @class,
            Align = align,
            Visible = visible,
        };

        var compiledExpression = expression.Compile();

        column.StringContent = (item) =>
        {
            if (item == null) return null;

            var value = compiledExpression.Invoke(item);

            var displayValue = BuildDisplayValue(value, format);

            return BuildFooterCell(displayValue, column.Class);
        };

        FooterColumns.Add(column);
    }

    /// <summary>
    /// Adds a footer cell that sums <paramref name="column"/> over the rows currently shown in the grid,
    /// reusing the column's own CSS class so the total lines up with the cells above it.
    /// </summary>
    /// <remarks>
    /// The sum is re-evaluated every time the footer is rendered, so it follows the active quick search or
    /// filter. The column must expose a <see cref="DynamicColumn{TGridItem}.Property"/> whose values convert
    /// to <see cref="decimal"/>.
    /// </remarks>
    /// <param name="column">The column to total. Its <see cref="DynamicColumn{TGridItem}.Id"/> decides where the footer cell lands.</param>
    /// <param name="removeClass">
    /// Optional CSS class to strip from the column's class when styling the footer cell, for example a per-cell
    /// colour that should not repeat on the totals row. Ignored when null or empty.
    /// </param>
    /// <exception cref="InvalidOperationException">The column has no <see cref="DynamicColumn{TGridItem}.Property"/> to sum.</exception>
    public void AddFooterColumnWithSum(DynamicColumn<TGridItem> column, string? removeClass = null)
    {
        var compiledProperty = column.GetCompiledProperty()
            ?? throw new InvalidOperationException($"Column '{column.Title}' has no Property, so it cannot be summed.");

        AddFooterColumn(
            column.Id,
            items => items.Sum(item => Convert.ToDecimal(compiledProperty(item))),
            format: "N0",
            @class: string.IsNullOrEmpty(removeClass) ? column.Class : column.Class?.Replace(removeClass, "").Trim()
        );
    }

    /// <summary>
    /// Adds a markup column that renders raw HTML content from the expression value, with an optional click handler.
    /// </summary>
    /// <param name="expression">An expression returning HTML markup to render in the cell.</param>
    /// <param name="onClick">Optional async click handler; when provided, the cell content is wrapped in a clickable div.</param>
    public void AddMarkup<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        string? title = null,
        string? fullTitle = null,
        string? @class = null,
        Align align = Align.Left,
        GridSort<TGridItem>? sortBy = null,
        bool visible = true,
        Func<TGridItem, Task>? onClick = null,
        string? propertyName = null)
    {
        var column = _columnBuilder.BuildMarkupColumn(
            expression, title, fullTitle, @class, align, sortBy, visible, onClick, propertyName);
        Add(column);
    }

    private static string BuildDisplayValue(object? value, string? format)
    {
        if (value is null)
            return string.Empty;
        else if (value is IFormattable formattableValue)
            return formattableValue.ToString(format, CultureInfo.InvariantCulture);
        else
            return $"{value}";
    }

    // The footer is injected via tfoot.innerHTML on the JS side, so both the value and the class must be
    // HTML-encoded here; they may carry caller-supplied data.
    private static string BuildFooterCell(string? value, string? cssClass)
    {
        var classAttribute = string.IsNullOrWhiteSpace(cssClass)
            ? string.Empty
            : $" class=\"{WebUtility.HtmlEncode(cssClass)}\"";

        return $"<td{classAttribute}>{WebUtility.HtmlEncode(value)}</td>";
    }
}