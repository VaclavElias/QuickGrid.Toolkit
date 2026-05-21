namespace QuickGrid.Toolkit;

public class TotalFooter
{
    public bool IsTotalFooter { get; set; }
    public string TotalFooterLabel { get; set; } = "Total";
    public int? TotalFooterLabelColumnId { get; set; }
    public string DefaultFormat { get; set; } = "N0";
    public string? RemoveClass { get; set; }
}