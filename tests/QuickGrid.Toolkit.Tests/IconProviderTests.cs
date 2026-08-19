using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using System.Text;

namespace QuickGrid.Toolkit.Tests;

public class DefaultQuickGridIconProviderTests
{
    // Every declared icon must resolve. Consumers implement IQuickGridIconProvider with an exhaustive switch, so a
    // member the toolkit itself cannot render is a member that was added without updating the icon set.
    [Fact]
    public void Render_HandlesEveryDeclaredIcon()
    {
        var provider = new DefaultQuickGridIconProvider();

        foreach (var icon in Enum.GetValues<QuickGridIcon>())
        {
            var markup = RenderHelper.ToMarkup(provider.Render(icon));

            Assert.False(string.IsNullOrWhiteSpace(markup), $"{icon} rendered nothing.");
        }
    }
}

public class ResilientQuickGridIconProviderTests
{
    [Fact]
    public void Render_PassesThroughWhatTheProviderReturns()
    {
        var guarded = ResilientQuickGridIconProvider.Wrap(new StubIconProvider());

        var markup = RenderHelper.ToMarkup(guarded.Render(QuickGridIcon.Search));

        Assert.Equal("<i class=\"custom\"></i>", markup);
    }

    [Fact]
    public void Render_FallsBackToTheDefaultIcon_WhenTheProviderThrows()
    {
        var guarded = ResilientQuickGridIconProvider.Wrap(new StubIconProvider { ThrowFor = QuickGridIcon.Search });

        var markup = RenderHelper.ToMarkup(guarded.Render(QuickGridIcon.Search));

        Assert.Equal(RenderHelper.ToMarkup(new DefaultQuickGridIconProvider().Render(QuickGridIcon.Search)), markup);
    }

    [Fact]
    public void Render_FallsBackToTheDefaultIcon_WhenTheProviderReturnsNull()
    {
        var guarded = ResilientQuickGridIconProvider.Wrap(new StubIconProvider { ReturnNullFor = QuickGridIcon.Tick });

        var markup = RenderHelper.ToMarkup(guarded.Render(QuickGridIcon.Tick));

        Assert.Equal(RenderHelper.ToMarkup(new DefaultQuickGridIconProvider().Render(QuickGridIcon.Tick)), markup);
    }

    // Worst case: neither the consumer provider nor the default can render the icon. Nothing is shown, but the grid
    // still renders — this is what keeps a future QuickGridIcon member from being a breaking change.
    [Fact]
    public void Render_RendersNothing_WhenNeitherProviderHandlesTheIcon()
    {
        var unknown = (QuickGridIcon)999;
        var guarded = ResilientQuickGridIconProvider.Wrap(new StubIconProvider { ThrowFor = unknown });

        var markup = RenderHelper.ToMarkup(guarded.Render(unknown));

        Assert.Equal(string.Empty, markup);
    }

    [Fact]
    public void Wrap_DoesNotWrapTwice()
    {
        var guarded = ResilientQuickGridIconProvider.Wrap(new StubIconProvider());

        Assert.Same(guarded, ResilientQuickGridIconProvider.Wrap(guarded));
    }

    private sealed class StubIconProvider : IQuickGridIconProvider
    {
        public QuickGridIcon? ThrowFor { get; init; }
        public QuickGridIcon? ReturnNullFor { get; init; }

        public RenderFragment Render(QuickGridIcon icon, string? extraCss = null)
        {
            if (icon == ThrowFor) { throw new NotImplementedException(); }
            if (icon == ReturnNullFor) { return null!; }

            return builder => builder.AddMarkupContent(0, "<i class=\"custom\"></i>");
        }
    }
}

// BL0006: reading render-tree frames is discouraged in application code because the types may change between
// releases. Tests are exactly the place where that trade-off is acceptable — inspecting the frames is the cheapest
// way to assert on a RenderFragment without pulling in a component-test framework, and a future .NET change here
// surfaces as a compile error rather than as silent misbehaviour.
#pragma warning disable BL0006
internal static class RenderHelper
{
    /// <summary>Executes a fragment and concatenates the markup and text it produced.</summary>
    public static string ToMarkup(RenderFragment fragment)
    {
        using var builder = new RenderTreeBuilder();

        fragment(builder);

        var frames = builder.GetFrames();
        var markup = new StringBuilder();

        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames.Array[i];

            markup.Append(frame.FrameType switch
            {
                RenderTreeFrameType.Markup => frame.MarkupContent,
                RenderTreeFrameType.Text => frame.TextContent,
                _ => string.Empty
            });
        }

        return markup.ToString();
    }
}
#pragma warning restore BL0006
