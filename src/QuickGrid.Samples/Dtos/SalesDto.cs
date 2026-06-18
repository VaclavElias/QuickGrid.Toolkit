namespace QuickGrid.Samples.Dtos;

public sealed class SalesDto
{
    public int Id { get; set; }

    public string Region { get; set; } = string.Empty;

    public string Product { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
}