using QuickGrid.Toolkit.Builders;
using QuickGrid.Toolkit.Helpers;
using System.Globalization;

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
        AddSimple(expression, columnInfo.Title, columnInfo.FullTitle, format, columnInfo.Class, align, cellStyle, sortBy, visible, propertyName);
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
        var column = _columnBuilder.BuildSimpleColumn(
            expression, title, fullTitle, format, @class, align, cellStyle, sortBy, visible, propertyName, addToContent);
        Add(column);
    }

    // ToDo: This will replace AddSimpleDate
    public void AddSimpleDate2<TValue>(
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
        Func<TGridItem, Task>? onClick = null)
    {
        var column = _columnBuilder.BuildStaticActionColumn(staticContent, title, align, @class, onClick);
        Add(column);
    }

    public void AddAction(
        string staticContent,
        string? title = null,
        Align align = Align.Left,
        string? @class = null,
        Action<TGridItem>? onClick = null,
        Expression<Func<TGridItem, bool>>? enabled = null)
    {
        var column = _columnBuilder.BuildConditionalActionColumn(staticContent, title, align, @class, onClick, enabled);
        Add(column);
    }

    [Obsolete("Use AddNumber instead.", true)]
    public void AddSimpleNumber(Expression<Func<TGridItem, object?>> expression, string? title = null, string format = "N0")
    {
        var compiledExpression = expression.Compile();

        Add(new()
        {
            Title = string.IsNullOrWhiteSpace(title) ? ExpressionHelper.GetPropertyName<TGridItem, object>(expression) : title,
            ChildContent = (item) => (builder) =>
            {
                var value = compiledExpression.Invoke(item);
                if (value is decimal number)
                    builder.AddContent(0, number.ToString(format));
                else
                    builder.AddContent(0, default(string));
            },
            SortBy = GridSort<TGridItem>.ByAscending(p => p == null ? default : compiledExpression.Invoke(p)),
            ColumnType = typeof(TemplateColumn<TGridItem>),
            Format = format
        });
    }

    [Obsolete("Use AddNumber instead.", true)]
    public void AddSimpleNumber2(Expression<Func<TGridItem, object?>> expression, string? title = null)
    {
        Add(new() { Property = expression, Title = title, Format = "N0", Align = Align.Right });
    }

    /// <summary>
    /// Adds a decimal numeric column to the grid.
    /// </summary>
    public void AddNumber(Expression<Func<TGridItem, decimal?>> expression, string? title = null, string? fullTitle = null, string format = "N0", string? @class = null, Align align = Align.Right, bool visible = true)
    {
        var column = _columnBuilder.BuildNumberColumn(expression, title, fullTitle, format, @class, align, visible);
        Add(column);
    }

    /// <summary>
    /// Adds a double numeric column to the grid.
    /// </summary>
    public void AddNumber(Expression<Func<TGridItem, double?>> expression, string? title = null, string? fullTitle = null, string format = "N0", string? @class = null, Align align = Align.Right, bool visible = true)
    {
        var column = _columnBuilder.BuildNumberColumn(expression, title, fullTitle, format, @class, align, visible);
        Add(column);
    }

    /// <summary>
    /// Adds a int numeric column to the grid.
    /// </summary>
    public void AddNumber(Expression<Func<TGridItem, int?>> expression, string? title = null, string? fullTitle = null, string format = "N0", string? @class = null, Align align = Align.Right, string? propertyName = null)
    {
        var column = _columnBuilder.BuildNumberColumn(expression, title, fullTitle, format, @class, align, propertyName: propertyName);
        Add(column);
    }

    public void AddStyledNumber<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        ColumnInfo columnInfo,
        string format = "N0",
        Align align = Align.Right,
        bool visible = true,
        CellStyleMap<TValue>? cellStyle = null,
        Func<TGridItem, Task>? onClick = null) where TValue : struct, IFormattable
        => AddStyledNumber(expression, columnInfo.Title, columnInfo.FullTitle, format, columnInfo.Class, align, visible, cellStyle, onClick, columnInfo.PropertyName);

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
        string? propertyName = null) where TValue : struct, IFormattable
    {
        var column = _columnBuilder.BuildStyledNumberColumn(
            expression, title, fullTitle, format, @class, align, visible, cellStyle, onClick, propertyName);
        Add(column);
    }

    public void AddTickColumn(
        Expression<Func<TGridItem, object?>> expression,
        string? title = null,
        string? fullTitle = null,
        string? @class = null,
        Align align = Align.Center,
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
            OnActionAsync = onClick
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

        column.Content = $"<td class=\"{column.Class}\">{displayValue}</td>";

        FooterColumns.Add(column);
    }

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

            return $"<td class=\"{column.Class}\">{displayValue}</td>";
        };

        FooterColumns.Add(column);
    }

    public void AddFooterColumnWithSum(DynamicColumn<TGridItem> column, string removeClass = "")
    {
        var compiledExpression = column.Property!.Compile();

        AddFooterColumn(
            column.Id,
            items => items.Sum(item => Convert.ToDecimal(compiledExpression(item))),
            format: "N0",
            @class: column.Class?.Replace(removeClass, "")
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
}