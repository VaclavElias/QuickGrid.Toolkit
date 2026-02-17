namespace QuickGrid.Toolkit.Columns;

// source https://github.com/aspnet/AspLabs/blob/main/src/QuickGrid/src/Microsoft.AspNetCore.Components.QuickGrid/Columns/PropertyColumn.cs
public class TickColumn<TGridItem> : ColumnBase<TGridItem>
{
    private const string TrueSign = "true-sign";
    private const string FalseSign = "false-sign";

    public override GridSort<TGridItem>? SortBy
    {
        get => _sortBuilder;
        set => throw new NotSupportedException($"PropertyColumn generates this member internally. For custom sorting rules, see '{typeof(TemplateColumn<TGridItem>)}'.");
    }

    [Parameter] public Expression<Func<TGridItem, object>> Property { get; set; } = default!;
    [Parameter] public bool ShowOnlyTrue { get; set; }
    [Parameter] public string? TrueClass { get; set; }
    [Parameter] public string? FalseClass { get; set; }
    [Parameter] public Func<TGridItem, Task>? OnClickAsync { get; set; }

    private Expression<Func<TGridItem, object>>? _lastAssignedProperty;
    private Func<TGridItem, object?>? _cellTextFunc;
    private GridSort<TGridItem>? _sortBuilder;

    //GridSort<TGridItem>? ISortBuilderColumn<TGridItem>.SortBuilder => _sortBuilder;

    protected override void CellContent(RenderTreeBuilder builder, TGridItem item)
    {
        var isTrue = _cellTextFunc!(item)?.ToString() == "True";
        var isNull = _cellTextFunc!(item) is null;

        if (isNull)
        {
            builder.AddContent(0, string.Empty);
            return;
        }

        if (ShowOnlyTrue && !isTrue)
        {
            builder.AddContent(0, string.Empty);
            return;
        }
        var cssClass = isTrue ? (TrueClass ?? TrueSign) : (FalseClass ?? FalseSign);

        if (OnClickAsync is null)
        {
            builder.AddMarkupContent(1, $"<i class=\"{cssClass}\"></i>");
        }
        else
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => OnClickAsync(item)));
            builder.AddMarkupContent(2, $"<i class=\"{cssClass}\"></i>");
            builder.CloseElement();
        }
    }

    protected override void OnParametersSet()
    {
        if (_lastAssignedProperty != Property)
        {
            _lastAssignedProperty = Property;

            var compiledPropertyExpression = Property.Compile();

            _cellTextFunc = item => compiledPropertyExpression!(item)?.ToString();

            _sortBuilder = GridSort<TGridItem>.ByAscending(Property);
        }

        if (Title is null && Property.Body is MemberExpression memberExpression)
        {
            Title = memberExpression.Member.Name;
        }
    }
}