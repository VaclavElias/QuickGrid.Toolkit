namespace QuickGrid.Toolkit.Columns;

public class ImageColumn<TGridItem> : PropertyColumnBase<TGridItem>
{
    public override GridSort<TGridItem>? SortBy { get; set; }

    protected override void CellContent(RenderTreeBuilder builder, TGridItem item)
    {
        var imagePath = CellValueFunc!(item)?.ToString();

        // Built as elements rather than markup so Blazor encodes the path: a value containing a quote
        // would otherwise break out of the src attribute.
        builder.OpenElement(0, "img");
        builder.AddAttribute(1, "alt", "");
        builder.AddAttribute(2, "src", imagePath);
        builder.CloseElement();
    }
}