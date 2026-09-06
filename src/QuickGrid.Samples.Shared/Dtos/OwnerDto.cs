namespace QuickGrid.Samples.Dtos;

public class OwnerDto
{
    public string Name { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>
    /// Two levels below the row, so <c>QuickSearchOptions.MaxSearchDepth</c> has something to reach for on the
    /// search example. <c>Owner.Team</c> is one level down; this is two.
    /// </summary>
    public OfficeDto Office { get; set; } = new();
}