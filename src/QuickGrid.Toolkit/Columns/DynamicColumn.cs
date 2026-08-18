namespace QuickGrid.Toolkit.Columns;

// We can't use directly PropertyColumn because it has [Parameter] attributes, otherwise we could inherit from it
public class DynamicColumn<TGridItem>
{
    private readonly static RenderFragment<TGridItem> EmptyChildContent = _ => builder => { };

    private Expression<Func<TGridItem, object?>>? _compiledFrom;
    private Func<TGridItem, object?>? _compiledProperty;

    // We need id so we could list all columns e.g. as checkbox and select which one is visible
    public int Id { get; set; }
    public string ColumnId => $"column-{Id}";

    /// <summary>
    /// Gets or sets the name of the property from <typeparamref name="TGridItem"/> that is displayed in this column.
    /// This value is used when exporting data for the selected columns and should match the corresponding property name on the grid item.
    /// </summary>
    public string? PropertyName { get; set; }
    public bool Visible { get; set; } = true;
    public bool IsNumeric { get; set; }
    public bool? CalculateTotal { get; set; }
    public string? Title { get; set; } = string.Empty;
    /// <summary>
    /// The long form of the column heading, used for tooltips and the column selector.
    /// Falls back to <see cref="Title"/> when not set.
    /// </summary>
    public string? FullTitle
    {
        get => string.IsNullOrWhiteSpace(field) ? Title : field;
        set;
    }
    public Align Align { get; set; }
    public string? Format { get; set; }
    public string? Class { get; set; }
    public Expression<Func<TGridItem, object?>>? Property { get; set; }

    public RenderFragment<TGridItem> ChildContent { get; set; } = EmptyChildContent;
    public GridSort<TGridItem>? SortBy { get; set; }
    public Func<TGridItem, Task>? OnActionAsync { get; set; }

    public Type ColumnType { get; set; } = typeof(PropertyColumn<TGridItem, object?>);

    /// <summary>
    /// Returns the compiled <see cref="Property"/> accessor, or <see langword="null"/> when the column has no property.
    /// </summary>
    /// <remarks>
    /// Compiling an expression is expensive, so the delegate is built once and reused. It is rebuilt automatically
    /// if <see cref="Property"/> is reassigned. Callers on a render path (footer totals, exports) should use this
    /// instead of calling <c>Property.Compile()</c> themselves.
    /// </remarks>
    public Func<TGridItem, object?>? GetCompiledProperty()
    {
        if (Property is null) return null;

        if (!ReferenceEquals(_compiledFrom, Property))
        {
            _compiledFrom = Property;
            _compiledProperty = Property.Compile();
        }

        return _compiledProperty;
    }
}