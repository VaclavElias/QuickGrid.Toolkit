namespace QuickGrid.Samples.Dtos;

/// <summary>
/// Demo row with one of everything the column helpers can render: text, dates, whole and fractional numbers,
/// nullable values, booleans, a nested object, an image and a status. Also selectable, for the selection example.
/// </summary>
public class ProjectDto : ISelectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OwnerDto Owner { get; set; } = new();
    public DateTime StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public int Tasks { get; set; }
    public decimal Budget { get; set; }
    public decimal Spent { get; set; }
    public double CompletionRate { get; set; }
    public bool IsActive { get; set; }
    public bool IsFlagged { get; set; }
    public ProjectStatus Status { get; set; }

    /// <summary>Set by the grid when the row is selected. Required by <see cref="ISelectionDto"/>.</summary>
    public bool IsSelected { get; set; }

    /// <summary>Difference between budget and spend; negative means over budget.</summary>
    public decimal Remaining => Budget - Spent;
}

public class OwnerDto
{
    public string Name { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

public enum ProjectStatus
{
    Planning,
    Active,
    OnHold,
    Delivered
}
