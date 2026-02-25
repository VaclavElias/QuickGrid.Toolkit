namespace QuickGrid.Toolkit.Columns;

public class ImageColumn<TGridItem> : PropertyColumnBase<TGridItem>
{
    public override GridSort<TGridItem>? SortBy { get; set; }

    protected override void CellContent(RenderTreeBuilder builder, TGridItem item)
    {
        var imagePath = CellValueFunc!(item)?.ToString();

        builder.AddMarkupContent(2, $"<img alt=\"\" src=\"{imagePath}\">");
    }
}