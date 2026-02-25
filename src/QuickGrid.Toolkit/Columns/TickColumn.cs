namespace QuickGrid.Toolkit.Columns;

// source https://github.com/aspnet/AspLabs/blob/main/src/QuickGrid/src/Microsoft.AspNetCore.Components.QuickGrid/Columns/PropertyColumn.cs
public class TickColumn<TGridItem> : PropertyColumnBase<TGridItem>
{
    private const string TrueSign = "true-sign";
    private const string FalseSign = "false-sign";

    public override GridSort<TGridItem>? SortBy
    {
        get => _sortBuilder;
        set => throw new NotSupportedException($"PropertyColumn generates this member internally. For custom sorting rules, see '{typeof(TemplateColumn<TGridItem>)}'.");
    }

    [Parameter] public bool ShowOnlyTrue { get; set; }
    [Parameter] public string? TrueClass { get; set; }
    [Parameter] public string? FalseClass { get; set; }
    [Parameter] public Func<TGridItem, Task>? OnClickAsync { get; set; }

    private GridSort<TGridItem>? _sortBuilder;

    protected override void OnNewPropertySet()
        => _sortBuilder = GridSort<TGridItem>.ByAscending(Property);

    protected override void CellContent(RenderTreeBuilder builder, TGridItem item)
    {
        var rawValue = CellValueFunc!(item);

        if (rawValue is null)
        {
            builder.AddContent(0, string.Empty);
            return;
        }

        var isTrue = rawValue.ToString() == "True";

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
}