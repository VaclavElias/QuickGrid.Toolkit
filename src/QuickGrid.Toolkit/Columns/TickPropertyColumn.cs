namespace QuickGrid.Toolkit.Columns;

public class TickPropertyColumn<TGridItem> : DynamicColumn<TGridItem>
{
    public bool ShowOnlyTrue { get; set; }
    public string? TrueClass { get; set; }
    public string? FalseClass { get; set; }
}