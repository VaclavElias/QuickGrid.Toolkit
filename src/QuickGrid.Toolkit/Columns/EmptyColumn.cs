namespace QuickGrid.Toolkit.Columns;

public class EmptyColumn<TGridItem> : ColumnBase<TGridItem>
{
    private static readonly RenderFragment<TGridItem> EmptyChildContent = _ => __ => { };

    public override GridSort<TGridItem>? SortBy { get; set; }

    [Parameter] public RenderFragment<TGridItem> ChildContent { get; set; } = EmptyChildContent;

    protected override void CellContent(RenderTreeBuilder builder, TGridItem item)
            => builder.AddContent(0, "");
}

//public class EmptyColumn<TGridItem> : ColumnBase<TGridItem>
//{
//    /// <summary>
//    /// Dummy Property otherwise it doesn't render
//    /// </summary>
//    [Parameter] public TGridItem? Property { get; set; }

//    protected override void CellContent(RenderTreeBuilder builder, TGridItem item) { }
//    //protected override void CellContent(RenderTreeBuilder builder, TGridItem item) => builder.AddContent(0, "Hi");
//}