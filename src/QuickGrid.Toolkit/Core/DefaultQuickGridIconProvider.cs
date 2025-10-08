namespace QuickGrid.Toolkit.Core;

public class DefaultQuickGridIconProvider : IQuickGridIconProvider
{
    public RenderFragment Render(QuickGridIcon icon, string? extraCss = null) => icon switch
    {
        QuickGridIcon.ColumnLayout => builder
            => builder.AddMarkupContent(0, "<i class=\"bi bi-window-sidebar\"></i>"),
        QuickGridIcon.Settings => builder
            => builder.AddMarkupContent(0, "<i class=\"bi bi-gear\"></i>"),
        QuickGridIcon.Search => builder
            => builder.AddMarkupContent(0, "<i class=\"bi bi-search me-2\"></i>"),
        QuickGridIcon.Filter => builder
            => builder.AddMarkupContent(0, "<i class=\"bi bi-funnel\"></i>"),
        QuickGridIcon.Export => builder
            => builder.AddMarkupContent(0, "<i class=\"bi bi-download me-2\"></i>"),
        QuickGridIcon.ExportSelected => builder
            => builder.AddMarkupContent(0, "<i class=\"bi bi-download me-2\"></i>"),
        QuickGridIcon.Tick => builder
            => builder.AddMarkupContent(0, "<i class=\"bi bi-check-lg me-2 text-success\"></i>"),
        QuickGridIcon.Wrench => builder
            => builder.AddMarkupContent(0, "<i class=\"bi bi-wrench me-2\"></i>"),
        QuickGridIcon.EmptyIcon => builder
            => builder.AddMarkupContent(0, "<i class=\"me-2\"></i>"),
        _ => throw new NotImplementedException()
    };
}