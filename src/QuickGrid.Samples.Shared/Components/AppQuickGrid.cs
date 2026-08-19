using QuickGrid.Samples.Services;

namespace QuickGrid.Samples.Components;

/// <summary>
/// An application's own grid component: <see cref="QuickGridWrapper{TGridItem}"/> with the house style and the
/// shared event wiring already applied, so pages only supply items and columns.
/// </summary>
/// <remarks>
/// This is the pattern to reach for once several pages use the toolkit. Defaults set in the constructor behave
/// like any other parameter default, so a page can still override them.
/// </remarks>
public class AppQuickGrid<TGridItem> : QuickGridWrapper<TGridItem> where TGridItem : class
{
    [Inject] protected ToastService Toasts { get; set; } = default!;

    public AppQuickGrid()
    {
        // House style for every grid in the application, overridable per page.
        Class = "table table-sm table-striped small table-fit table-thead-sticky table-no-empty-lines mb-0";
        IsExportEnabled = true;
        ItemsPerPage = 10;
    }

    /// <summary>
    /// Deliberately <c>async</c>, even though nothing here needs to be.
    /// </summary>
    /// <remarks>
    /// Real subclasses await on init — both production consumers of this toolkit load authorization state and
    /// saved column layouts here. That changes the render sequence: when <c>OnInitializedAsync</c> returns an
    /// incomplete task, Blazor renders the component <em>before</em> <c>OnParametersSetAsync</c> has ever run, so
    /// anything the wrapper caches from its parameters is still unset on that first render. Keeping this async
    /// means the samples exercise that path, which a synchronous <c>OnInitialized</c> silently skips.
    /// </remarks>
    protected override async Task OnInitializedAsync()
    {
        // Stands in for the real work: an auth check, or loading saved views from storage.
        await Task.Yield();

        // Only fill in what the page has not supplied, so a page can still handle an event itself.
        Events ??= new QuickGridWrapperEvents<TGridItem>();

        if (!Events.WarningRequested.HasDelegate)
        {
            Events.WarningRequested = EventCallback.Factory.Create<string>(this, Toasts.ShowWarning);
        }

        if (!Events.OnExport.HasDelegate)
        {
            Events.OnExport = EventCallback.Factory
                .Create<IQueryable<TGridItem>>(this, (IQueryable<TGridItem> rows) => ExportAsync(rows));
        }
    }

    private Task ExportAsync(IQueryable<TGridItem> rows)
    {
        // A real application would stream a file here; the sample just reports what it would have exported.
        Toasts.ShowInfo($"Exported {rows.Count()} row(s) of {typeof(TGridItem).Name}.");

        return Task.CompletedTask;
    }
}
