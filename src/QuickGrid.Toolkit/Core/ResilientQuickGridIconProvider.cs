using Microsoft.Extensions.Logging;

namespace QuickGrid.Toolkit.Core;

/// <summary>
/// Wraps an <see cref="IQuickGridIconProvider"/> so that an icon the provider does not handle degrades to the
/// built-in markup instead of taking the whole render down.
/// </summary>
/// <remarks>
/// Providers are commonly written as an exhaustive <c>switch</c> ending in <c>throw new NotImplementedException()</c>
/// (<see cref="DefaultQuickGridIconProvider"/> included), which makes adding a member to <see cref="QuickGridIcon"/>
/// a breaking change for every application that ships its own provider. This decorator removes that coupling: the
/// toolkit can introduce an icon and older providers keep working, showing the default glyph for the new member.
/// <para>
/// Only the <see cref="Render"/> call is guarded — that is where the switch is evaluated. A provider whose returned
/// fragment throws while it is being executed cannot be recovered here, because by then it may have written a
/// partial subtree into the <see cref="RenderTreeBuilder"/>.
/// </para>
/// </remarks>
internal sealed class ResilientQuickGridIconProvider : IQuickGridIconProvider
{
    private static readonly DefaultQuickGridIconProvider _fallback = new();
    private static readonly RenderFragment _nothing = _ => { };

    private readonly IQuickGridIconProvider _inner;
    private readonly ILogger? _logger;

    private ResilientQuickGridIconProvider(IQuickGridIconProvider inner, ILogger? logger)
    {
        _inner = inner;
        _logger = logger;
    }

    /// <summary>
    /// Returns <paramref name="provider"/> guarded against unhandled icons. Already-guarded providers are returned
    /// unchanged, so repeated wrapping is free.
    /// </summary>
    public static IQuickGridIconProvider Wrap(IQuickGridIconProvider provider, ILogger? logger = null)
        => provider is ResilientQuickGridIconProvider ? provider : new ResilientQuickGridIconProvider(provider, logger);

    public RenderFragment Render(QuickGridIcon icon, string? extraCss = null)
    {
        try
        {
            return _inner.Render(icon, extraCss) ?? Fallback(icon, extraCss, exception: null);
        }
        catch (Exception ex)
        {
            return Fallback(icon, extraCss, ex);
        }
    }

    private RenderFragment Fallback(QuickGridIcon icon, string? extraCss, Exception? exception)
    {
        _logger?.LogWarning(
            exception,
            "Non-critical: icon provider {Provider} did not render {Icon}; falling back to the default icon set.",
            _inner.GetType().FullName,
            icon);

        try
        {
            // The default provider throws on an undefined enum value too, so the last resort is to render nothing
            // rather than to let the fallback fail the render it was meant to save.
            return _fallback.Render(icon, extraCss) ?? _nothing;
        }
        catch
        {
            return _nothing;
        }
    }
}