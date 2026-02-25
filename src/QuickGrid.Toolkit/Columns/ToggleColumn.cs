namespace QuickGrid.Toolkit.Columns;

public class ToggleColumn<TGridItem> : PropertyColumnBase<TGridItem>
{
    public override GridSort<TGridItem>? SortBy
    {
        get => _sortBuilder;
        set => throw new NotSupportedException($"PropertyColumn generates this member internally. For custom sorting rules, see '{typeof(TemplateColumn<TGridItem>)}'.");
    }

    [Parameter] public Func<TGridItem, Task>? OnChangeAsync { get; set; }

    private GridSort<TGridItem>? _sortBuilder;

    protected override void OnNewPropertySet()
        => _sortBuilder = GridSort<TGridItem>.ByAscending(Property);

    protected override void CellContent(RenderTreeBuilder builder, TGridItem item)
    {
        var rawValue = CellValueFunc!(item);
        var isTrue = rawValue is not null && rawValue.ToString() == "True";

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "form-switch d-inline-block");
        builder.OpenElement(2, "input");
        if (OnChangeAsync is not null)
        {
            builder.AddAttribute(3, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, () => OnChangeAsync.Invoke(item)));
        }
        builder.AddAttribute(4, "class", "form-check-input");
        builder.AddAttribute(5, "type", "checkbox");
        builder.AddAttribute(6, "role", "switch");
        if (isTrue)
        {
            builder.AddAttribute(7, "checked");
        }
        builder.CloseElement();
        builder.CloseElement();
    }
}