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
    /// Creates an independent copy of this column, so that changing the copy's <see cref="Visible"/>,
    /// <see cref="Title"/> or any other property leaves the original untouched.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implemented with <see cref="object.MemberwiseClone"/>, which copies <em>every</em> field and preserves the
    /// runtime type — a <see cref="TickPropertyColumn{TGridItem}"/> clones as a tick column, keeping the
    /// <c>TrueClass</c>/<c>FalseClass</c>/<c>ShowOnlyTrue</c> settings the renderer looks for. That matters: a clone
    /// built by hand from a property list silently downgrades tick and toggle columns to plain property columns,
    /// and it goes stale every time a property is added to this class.
    /// </para>
    /// <para>
    /// The copy is shallow. <see cref="Property"/>, <see cref="SortBy"/>, <see cref="ChildContent"/> and
    /// <see cref="OnActionAsync"/> are shared with the original, which is intended — they are behaviour, not state.
    /// Note that <see cref="ChildContent"/> was built with the formatting, cell styling and click handler that were
    /// passed to the <c>Add*</c> call, so changing <see cref="Format"/> or <see cref="Class"/> on a clone does not
    /// change how its cells render, exactly as it does not on the original.
    /// </para>
    /// </remarks>
    public virtual DynamicColumn<TGridItem> Clone() => (DynamicColumn<TGridItem>)MemberwiseClone();

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