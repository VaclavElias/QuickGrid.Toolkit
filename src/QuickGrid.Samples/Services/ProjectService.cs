using System.Text;

namespace QuickGrid.Samples.Services;

/// <summary>
/// Deterministic demo data. Values are hand-picked so every example has something interesting to show:
/// over-budget rows, missing completion dates, a range of statuses and a mix of flags.
/// </summary>
public static class ProjectService
{
    private static readonly DateTime BaseDate = new(2026, 1, 12);

    public static List<ProjectDto> GetProjects() =>
    [
        Create(1, "Atlas Migration", "ATL-01", "Priya Nayar", "Platform", 0, 42, 120_000m, 98_400m, 0.82, true, false, ProjectStatus.Active, 0),
        Create(2, "Beacon Redesign", "BCN-14", "Tom Ferris", "Design", 18, 27, 64_000m, 71_250m, 0.61, true, true, ProjectStatus.Active, 0),
        Create(3, "Cobalt Reporting", "CBL-07", "Ana Duarte", "Data", 35, 15, 38_500m, 12_900m, 0.30, true, false, ProjectStatus.Planning, 0),
        Create(4, "Delta Onboarding", "DLT-22", "Sam Okafor", "Growth", 62, 58, 91_000m, 90_100m, 1.00, false, false, ProjectStatus.Delivered, 190),
        Create(5, "Ember Billing", "EMB-03", "Lena Kraus", "Finance", 74, 33, 145_000m, 151_800m, 0.94, true, true, ProjectStatus.Active, 0),
        Create(6, "Fjord Search", "FJD-11", "Ravi Menon", "Platform", 88, 21, 52_000m, 18_600m, 0.24, false, false, ProjectStatus.OnHold, 0),
        Create(7, "Gale Analytics", "GLE-09", "Ana Duarte", "Data", 96, 47, 78_400m, 60_050m, 0.71, true, false, ProjectStatus.Active, 0),
        Create(8, "Harbour Portal", "HRB-18", "Tom Ferris", "Design", 110, 12, 29_750m, 31_200m, 0.55, true, true, ProjectStatus.Active, 0),
        Create(9, "Iris Notifications", "IRS-05", "Priya Nayar", "Platform", 124, 36, 41_200m, 39_900m, 1.00, false, false, ProjectStatus.Delivered, 240),
        Create(10, "Juniper Mobile", "JNP-27", "Sam Okafor", "Growth", 138, 64, 168_000m, 84_300m, 0.48, true, false, ProjectStatus.Active, 0),
        Create(11, "Kestrel Sync", "KST-31", "Lena Kraus", "Finance", 152, 19, 33_600m, 9_450m, 0.18, false, false, ProjectStatus.Planning, 0),
        Create(12, "Lumen Docs", "LMN-42", "Ravi Menon", "Platform", 166, 25, 24_900m, 26_400m, 0.88, true, true, ProjectStatus.OnHold, 0),
    ];

    private static ProjectDto Create(
        int id, string name, string code, string owner, string team, int startOffsetDays, int tasks,
        decimal budget, decimal spent, double completionRate, bool isActive, bool isFlagged,
        ProjectStatus status, int completedOffsetDays) => new()
        {
            Id = id,
            Name = name,
            Code = code,
            Owner = new OwnerDto
            {
                Name = owner,
                Team = team,
                AvatarUrl = BuildAvatar(owner)
            },
            StartedOn = BaseDate.AddDays(startOffsetDays),
            CompletedOn = completedOffsetDays > 0 ? BaseDate.AddDays(completedOffsetDays) : null,
            Tasks = tasks,
            Budget = budget,
            Spent = spent,
            CompletionRate = completionRate,
            IsActive = isActive,
            IsFlagged = isFlagged,
            Status = status
        };

    /// <summary>
    /// Builds an inline SVG avatar so the image column has something to render without a network call.
    /// </summary>
    private static string BuildAvatar(string name)
    {
        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24"><rect width="24" height="24" rx="12" fill="#dee2e6"/><text x="12" y="16" font-family="sans-serif" font-size="10" fill="#495057" text-anchor="middle">{Initials(name)}</text></svg>
            """;

        return $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))}";
    }

    private static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length > 1 ? $"{parts[0][0]}{parts[1][0]}" : name[..1];
    }
}
