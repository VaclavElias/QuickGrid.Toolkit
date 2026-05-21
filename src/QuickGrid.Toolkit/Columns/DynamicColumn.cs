namespace QuickGrid.Toolkit.Columns;

// We can't use directly PropertyColumn because it has [Parameter] attributes, otherwise we could inherit from it
public class DynamicColumn<TGridItem>
{
    private readonly static RenderFragment<TGridItem> EmptyChildContent = _ => builder => { };

    private string? _fullTitle;

    // We need id so we could list all columns e.g. as checkbox and select which one is visible
    public int Id { get; set; }
    public string ColumnId => $"column-{Id}";

    /// <summary>
    /// Gets or sets the name of the property from <typeparamref name="TGridItem"/> that is displayed in this column.
    /// This value is used when exporting data for the selected columns and should match the corresponding property name on the grid item.
    /// </summary>
    public string? PropertyName { get; set; }
    public bool Visible { get; set; } = true;
    public bool Sortable { get; set; } = true;
    public bool IsNumeric { get; set; }
    public bool? CalculateTotal { get; set; }
    public string? Title { get; set; } = string.Empty;
    public string? FullTitle
    {
        get => string.IsNullOrWhiteSpace(_fullTitle) ? Title : _fullTitle;
        set => _fullTitle = value;
    }
    public Align Align { get; set; }
    public string? Format { get; set; }
    public string? Class { get; set; }
    public Expression<Func<TGridItem, object?>>? Property { get; set; }

    public RenderFragment<TGridItem> ChildContent { get; set; } = EmptyChildContent;
    public GridSort<TGridItem>? SortBy { get; set; }
    public Action? OnClick { get; set; }
    public Func<TGridItem, Task>? OnActionAsync { get; set; }

    public Type ColumnType { get; set; } = typeof(PropertyColumn<TGridItem, object?>);
}