namespace QuickGrid.Samples.Core;

/// <summary>
/// Replaces the toolbar icons with emoji, to show that the icon set is pluggable.
/// </summary>
/// <remarks>
/// Register an implementation of <see cref="IQuickGridIconProvider"/> in DI and every
/// <see cref="QuickGridWrapper{TGridItem}"/> picks it up. Without one the toolkit falls back to
/// <c>DefaultQuickGridIconProvider</c>, which emits Bootstrap Icons markup.
/// </remarks>
public class SampleIconProvider : IQuickGridIconProvider
{
    public RenderFragment Render(QuickGridIcon icon, string? extraCss = null) => builder
        => builder.AddContent(0, icon switch
        {
            QuickGridIcon.ColumnLayout => "🧱",
            QuickGridIcon.Settings => "⚙️",
            QuickGridIcon.Search => "🔍 ",
            QuickGridIcon.Filter => "🧪",
            QuickGridIcon.Export => "⬇️ ",
            QuickGridIcon.ExportSelected => "📤 ",
            QuickGridIcon.Tick => "✅ ",
            QuickGridIcon.Wrench => "🔧 ",
            QuickGridIcon.EmptyIcon => "　",
            _ => "•"
        });
}
