using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace QuickGrid.Toolkit;

public partial class QuickGridWrapper<TGridItem> : ComponentBase, IAsyncDisposable
{
    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = default!;
    [Inject] protected ILogger<QuickGridWrapper<TGridItem>> Logger { get; set; } = default!;

    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// CSS classes for the rendered table. <c>table-index</c> is not included here: it is appended by
    /// <see cref="GetTableClass"/> when an index column is actually visible.
    /// </summary>
    [Parameter] public string? Class { get; set; } = "table table-sm table-striped small table-fit table-thead-sticky table-no-empty-lines mb-0";

    /// <summary>
    /// Optional QuickGrid theme name, passed straight through to <c>QuickGrid.Theme</c>.
    /// Leave unset to render no theme attribute, which opts out of QuickGrid's built-in <c>default</c> styling.
    /// </summary>
    [Parameter] public string? Theme { get; set; }
    [Parameter] public string? DownloadFileName { get; set; }
    [Parameter] public string? QuickSearch { get; set; }

    /// <summary>
    /// Increment this whenever the contents of <see cref="Items"/> change, to have the grid re-read them and
    /// rebuild the quick search result and footer totals.
    /// </summary>
    /// <remarks>
    /// <para>QuickGrid only re-queries when the <c>Items</c> <em>reference</em> changes, and the wrapper caches the
    /// quick search result keyed on the search text. Neither can see rows being added to, removed from or edited
    /// inside a collection they were already handed, which is what this parameter signals.</para>
    /// <para>It is not needed when the caller assigns a new collection each time (for example
    /// <c>Items="@(_items.AsQueryable())"</c> with no search active), since the changed reference is detected
    /// on its own. <see cref="RefreshDataAsync"/> does the same job imperatively if you hold an <c>@ref</c>.</para>
    /// </remarks>
    [Parameter] public long ItemsVersion { get; set; }

    // ToDo: If most callers already have a List and use in-memory search, consider changing Items to IEnumerable<TGridItem> (or IReadOnlyList<TGridItem>) and add QueryableItems for EF-backed scenarios. Use the branching above to support both safely.
    [Parameter] public IQueryable<TGridItem>? Items { get; set; }
    [Parameter] public IQueryable<TGridItem>? QueryableItems { get; set; }
    [Parameter] public ColumnManager<TGridItem> ColumnManager { get; set; } = new();
    [Parameter] public bool IsPaginator { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public bool IsSelection { get; set; }
    [Parameter] public bool IsSelectAllItems { get; set; }
    [Parameter] public bool IsColumnSelection { get; set; } = true;
    [Parameter] public bool IsColumnItemsSelection { get; set; }
    [Parameter] public bool IsFilter { get; set; } = true;

    /// <summary>
    /// Renders a diagnostics panel above the grid showing the wrapper's live state: item counts, the active
    /// search and its mode, pagination, and column visibility. Intended for development only.
    /// </summary>
    [Parameter] public bool IsDebug { get; set; }

    [Parameter] public bool IsToolbar { get; set; } = true;
    [Parameter] public TotalFooter TotalFooter { get; set; } = new();
    [Parameter] public bool ExactMatch { get; set; }
    [Parameter] public bool IsExportEnabled { get; set; }
    [Parameter] public Func<TGridItem, object> ItemKey { get; set; } = x => x!;
    [Parameter] public Func<TGridItem, string?>? RowClass { get; set; }
    [Parameter] public EventCallback ColumnSelectionChanged { get; set; }
    [Parameter] public EventCallback<string> QuickSearchChanged { get; set; }
    [Parameter] public EventCallback<bool> ExactMatchChanged { get; set; }
    [Parameter] public EventCallback<List<TGridItem>> SearchResultChanged { get; set; }
    [Parameter] public QuickGridWrapperEvents<TGridItem>? Events { get; set; }

    /// <summary>
    /// Search behaviour beyond the two bindable parameters: how deep nested properties are walked, case
    /// sensitivity, how multiple terms combine, whether <c>-word</c> excludes, and which properties are searched.
    /// Unset leaves every value at its <see cref="QuickSearchOptions"/> default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read live, so changing a value re-runs an active search rather than waiting for the next keystroke. The
    /// wrapper works from its own copy and never writes to the instance passed in, so one instance may safely be
    /// shared by several grids.
    /// </para>
    /// <para>
    /// <see cref="QuickSearchOptions.ExactMatch"/> is the one member this does not control. The settings menu
    /// assigns exact match on the component itself, and a value the component owns cannot also be owned by an
    /// options object, so <see cref="ExactMatch"/> always wins - set it there.
    /// </para>
    /// </remarks>
    [Parameter] public QuickSearchOptions? SearchOptions { get; set; }

    /// <summary>
    /// Whether nested properties are searched. <see langword="null"/>, the default, leaves the decision to
    /// <see cref="QuickSearchOptions.IncludeChildProperties"/> - which is <see langword="true"/>, so the default
    /// behaviour is unchanged.
    /// </summary>
    [Obsolete("Use SearchOptions.IncludeChildProperties instead. Removed in v2.")]
    [Parameter] public bool? IsNestedSearch { get; set; }

    /// <summary>
    /// The number of items to display per page when pagination is enabled. The default value is 20.
    /// </summary>
    [Parameter] public int ItemsPerPage { get; set; } = 20;
    [Parameter] public FilterCriteria<TGridItem>? FilterCriteria { get; set; }
    [Parameter] public RenderFragment? SelectedItemsAction { get; set; }
    [Parameter] public RenderFragment? SelectedItemsActionDropDown { get; set; }
    [Parameter] public RenderFragment? FilterSection { get; set; }
    [Parameter] public RenderFragment? DropdownItems { get; set; }

    /// <summary>
    /// Named column layouts offered in the column-layout menu. Supply them from markup, or assign them from a
    /// subclass once they have been loaded from storage.
    /// </summary>
    [Parameter] public List<ColumnConfig> ColumnConfigurations { get; set; } = [];

    /// <summary>
    /// Icon set for this grid's toolbar. Overrides any <see cref="IQuickGridIconProvider"/> registered in DI,
    /// which is normally where the application sets its icons once for every grid.
    /// </summary>
    [Parameter] public IQuickGridIconProvider? Icons { get; set; }

    public ColumnManager<TGridItem> UsedColumnManager { get; set; } = new();
    public ColumnConfig? SelectedConfiguration { get; set; }

    // Resolve icon provider lazily with a safe default so the component doesn't throw if it's not registered in DI
    private IQuickGridIconProvider? _registeredIconProvider;
    private IQuickGridIconProvider? _iconProviderSource;
    private IQuickGridIconProvider? _iconProvider;

    /// <summary>
    /// The icon set in use, guarded so that an icon the provider does not handle falls back to the default markup
    /// instead of failing the render. See <see cref="ResilientQuickGridIconProvider"/>.
    /// </summary>
    protected IQuickGridIconProvider IconProvider
    {
        get
        {
            var source = Icons
                ?? (_registeredIconProvider ??= ServiceProvider.GetService<IQuickGridIconProvider>() ?? new DefaultQuickGridIconProvider());

            // Icons is a parameter and can change between renders, so the guarded instance is cached against its source.
            if (!ReferenceEquals(source, _iconProviderSource))
            {
                _iconProviderSource = source;
                _iconProvider = ResilientQuickGridIconProvider.Wrap(source, Logger);
            }

            return _iconProvider!;
        }
    }

    private const string ColumnTitleSetupErrorMessage = "Non-critical: Failed to setup column titles for {Id}. Application continues to run without this feature.";
    private const string FooterSetupErrorMessage = "Non-critical: Failed to setup footer for {Id}. Application continues to run without this feature.";

    private bool _titlesLoaded;
    private bool _isTableIndex;
    private bool _showFilterSection;
    private bool _refreshGridAfterRender;

    private long _prevItemsVersion;
    private string? _lastRenderedFooter;

    private QuickGrid<TGridItem>? _grid;
    private PaginationState? _pagination;
    private ColumnManager<TGridItem> _defaultColumnManager = new();

    private List<string> _defaultVisibleColumns = [];
    private Task<IJSObjectReference>? _moduleTask;
    private bool _isDisposed;

    /// <summary>
    /// Owns the query, the search options and the computed result. The component keeps only the parts that need
    /// the render loop: pushing parameters in, and raising <see cref="SearchResultChanged"/> afterwards.
    /// </summary>
    private readonly GridSearch<TGridItem> _search = new();

    // AsEnumerable() keeps the pattern match in C#: counting straight off the IQueryable would build an
    // expression tree, which cannot contain an 'is' pattern.
    private int SelectedItemsCount => VisibleItems?.AsEnumerable().Count(item => item is ISelectionDto { IsSelected: true }) ?? 0;

    /// <summary>
    /// The rows the grid is showing: the search result when a search is active, otherwise <see cref="Items"/>
    /// unchanged. Reading it is free and has no side effects.
    /// </summary>
    /// <remarks>
    /// The fallback reads the <see cref="Items"/> <em>parameter</em> directly, never a copy held elsewhere.
    /// Blazor renders a component whose <c>OnInitializedAsync</c> is still running before
    /// <c>OnParametersSetAsync</c> has ever run, so anything cached there is still null on that first render —
    /// which showed an empty grid until the next interaction for any subclass that awaits on init.
    /// </remarks>
    private IQueryable<TGridItem>? VisibleItems => _search.Results ?? Items;

    /// <summary>
    /// Recomputes the search result and reports the rows now on show through <see cref="SearchResultChanged"/>.
    /// </summary>
    /// <remarks>
    /// Call this from lifecycle and event handlers whenever the displayed set may have changed - never from a
    /// property getter or from markup. It is the one place that both recomputes and notifies, so the two can
    /// never drift apart. See <see cref="GridSearch{TGridItem}"/> for why a changed <see cref="Items"/> reference
    /// deliberately does not trigger a recompute.
    /// </remarks>
    private async Task RefreshSearchResultAsync()
    {
        _search.Recompute(Items);

        await SearchResultChanged.InvokeAsync(VisibleItems?.ToList() ?? []);
    }

    /// <summary>
    /// Adopts the current <see cref="ColumnManager"/>. Runs on the first parameter set and again whenever the
    /// caller swaps in a different instance, so the rendered columns never drift from the bound manager.
    /// </summary>
    private void SyncColumnManager()
    {
        if (ReferenceEquals(_defaultColumnManager, ColumnManager)) return;

        _defaultColumnManager = ColumnManager;
        _titlesLoaded = false;

        // Repopulates _defaultVisibleColumns from the new manager.
        SetDefaultColumns();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_refreshGridAfterRender)
        {
            _refreshGridAfterRender = false;
            await RefreshDataAsync();
        }

        if (Id is null) return;

        if (!_titlesLoaded && UsedColumnManager.Columns.Count > 0)
        {
            await RefreshColumnTitlesAsync();

            _titlesLoaded = true;
        }

        await AddOrUpdateFooterAsync();
    }

    /// <summary>
    /// The options a search actually runs with: <see cref="SearchOptions"/> with the flat parameters layered on
    /// top.
    /// </summary>
    /// <remarks>
    /// Always a copy, never the caller's instance. <see cref="QuickSearchOptions"/> is a mutable class and nothing
    /// stops a page handing the same one to several grids - the hazard already recorded for <c>TotalFooter</c> —
    /// so writing resolved values into it would let one grid's parameters reach another.
    /// </remarks>
    private QuickSearchOptions ResolveSearchOptions()
    {
        var options = SearchOptions?.Clone() ?? new QuickSearchOptions();

        // The one setting the wrapper assigns itself, from the settings menu, so the parameter has to win:
        // reading it from the options object would put the menu's choice back the next time the parent renders.
        options.ExactMatch = ExactMatch;

        // Null is what makes this resolvable at all. A plain bool defaulting to true cannot tell a caller who
        // wants nesting from one who never set the parameter, so the latter would silently overrule
        // IncludeChildProperties = false.
#pragma warning disable CS0618 // Obsolete, but honoured until it is removed in v2.
        if (IsNestedSearch is bool nested)
        {
            options.IncludeChildProperties = nested;
        }
#pragma warning restore CS0618

        return options;
    }

    protected override async Task OnParametersSetAsync()
    {
        SyncColumnManager();

        EnsurePaginationState();
        SetTableIndex();

        _search.SyncInputs(FilterCriteria, ResolveSearchOptions());
        _search.ApplyQuickSearchParameter(QuickSearch);

        if (Items is not null && QueryableItems is not null && Events is not null)
        {
            await Events.WarningRequested.InvokeAsync("Provide only one of Items or QueryableItems.");
        }

        if (_prevItemsVersion != ItemsVersion)
        {
            _prevItemsVersion = ItemsVersion;

            // The items changed underneath a search that has not itself changed, so recompute unconditionally.
            _refreshGridAfterRender = true;

            await RefreshSearchResultAsync();
        }
        else if (_search.InputsChanged())
        {
            await RefreshSearchResultAsync();
        }

        if (_defaultVisibleColumns.Count == 0)
        {
            SetDefaultColumns();
        }
    }

    private void EnsurePaginationState()
    {
        if (!IsPaginator)
        {
            _pagination = null;
            return;
        }

        _pagination ??= new PaginationState();
        _pagination.ItemsPerPage = ItemsPerPage;
    }

    public async Task RefreshColumnTitlesAsync()
    {
        if (!UsedColumnManager.Columns.Any(w => w.Visible)) return;

        var titles = UsedColumnManager.Columns.Where(w => w.Visible).Select(col => col.FullTitle).ToList();

        try
        {
            await InvokeModuleVoidAsync("setColumnTitles", Id, titles);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, ColumnTitleSetupErrorMessage, Id);
        }
    }

    /// <summary>
    /// Rebuilds the footer row from the rows currently displayed and pushes it to the DOM.
    /// </summary>
    /// <remarks>
    /// Totals are recalculated on every call, so they follow the active search or filter. The JS interop call
    /// is skipped when the generated markup is unchanged, which keeps the repeated renders of an idle grid cheap.
    /// </remarks>
    public async ValueTask AddOrUpdateFooterAsync()
    {
        if (Id is null || !HasFooter()) return;

        var footer = GridFooterBuilder<TGridItem>.Build(UsedColumnManager, TotalFooter, GetFooterItems());

        if (footer == _lastRenderedFooter) return;

        try
        {
            await InvokeModuleVoidAsync("addOrUpdateFooter", Id, footer);

            _lastRenderedFooter = footer;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, FooterSetupErrorMessage, Id);
        }
    }

    private async ValueTask InvokeModuleVoidAsync(string identifier, params object?[]? args)
    {
        if (_isDisposed) return;

        var moduleTask = _moduleTask ??= JS.InvokeAsync<IJSObjectReference>("import", "./_content/QuickGrid.Toolkit/quickGridToolkit.js").AsTask();
        IJSObjectReference module;

        try
        {
            module = await moduleTask;
        }
        catch (ObjectDisposedException) when (_isDisposed)
        {
            return;
        }

        if (_isDisposed) return;

        try
        {
            await module.InvokeVoidAsync(identifier, args);
        }
        catch (ObjectDisposedException) when (_isDisposed)
        {
        }
    }

    private bool HasFooter()
        => GridFooterBuilder<TGridItem>.HasFooter(UsedColumnManager, TotalFooter);

    private void SetDefaultColumns()
    {
        InitializeDefaultColumnVisibility();

        SelectedConfiguration = ColumnConfigurations.FirstOrDefault(w => w.Default);

        if (SelectedConfiguration != null)
        {
            SetColumnVisibility(SelectedConfiguration);
        }

        UsedColumnManager = _defaultColumnManager;
    }

    private void InitializeDefaultColumnVisibility()
        => _defaultVisibleColumns = _defaultColumnManager.Columns
            .Where(w => w.Visible && w.FullTitle != null)
            .Select(s => s.FullTitle!)
            .ToList();

    /// <summary>
    /// The rows the footer aggregates over: the search/filter result when one is active, otherwise all items.
    /// Materialised once, because every total walks the same rows.
    /// </summary>
    private IReadOnlyList<TGridItem> GetFooterItems()
        => VisibleItems?.ToList() ?? [];

    private async Task SearchTextChanged(string? text)
    {
        if (FilterCriteria is null || Items is null) return;

        if (string.IsNullOrWhiteSpace(text))
        {
            ClearSearch();

            return;
        }

        // B6: IsLoading is set without a StateHasChanged around the await, so the spinner never actually appears.
        IsLoading = true;

        await _search.RunFilterCriteriaSearchAsync(text, Items);

        IsLoading = false;

        await SearchResultChanged.InvokeAsync(VisibleItems?.ToList() ?? []);
    }

    private async Task OnInMemorySearchChanged()
    {
        await RefreshSearchResultAsync();

        if (string.IsNullOrEmpty(_search.Query))
        {
            await QuickSearchChanged.InvokeAsync(_search.Query);
        }
    }

    public void ClearSearch() => ClearSearch(true);

    /// <summary>
    /// Clears the active search and any filtered result, so the grid falls back to the full item set.
    /// </summary>
    /// <param name="shouldInvokeCallback">When true, notifies the caller through <see cref="QuickSearchChanged"/>.</param>
    public void ClearSearch(bool shouldInvokeCallback = false)
    {
        _search.Clear();

        // This overload is public and synchronous, so the notifications are dispatched rather than awaited,
        // as QuickSearchChanged already was.
        _ = SearchResultChanged.InvokeAsync(VisibleItems?.ToList() ?? []);

        if (shouldInvokeCallback)
        {
            QuickSearchChanged.InvokeAsync(_search.Query);
        }
    }

    public bool QuickSearchAction(TGridItem item, string query, QuickSearchOptions searchOptions)
        => QuickSearchUtility.QuickSearch(item, query, options: searchOptions);

    public Task ExportAsync() => Events?.OnExport.InvokeAsync(VisibleItems) ?? Task.CompletedTask;

    public async Task ExportSelectedColumnsAsync()
    {
        if (Events is null) return;

        if (VisibleItems is null)
        {
            await Events.WarningRequested.InvokeAsync("No items to export.");

            return;
        }

        var visibleColumns = UsedColumnManager.Columns
            .Where(w => w.Visible && w.PropertyName != null)
            .Select(s => s.PropertyName!)
            .Distinct()
            .ToList();

        if (visibleColumns.Count == 0)
        {
            await Events.WarningRequested.InvokeAsync("No columns to export.");

            return;
        }

        // ToList() first: projecting straight off the IQueryable would push the reflection-based builder
        // into the query tree, which an EF-backed source cannot translate.
        var exportItems = VisibleItems.ToList()
            .Select(item => ExpandoObjectBuilder<TGridItem>.Create(item, visibleColumns))
            .Where(obj => obj != null);

        await Events.OnSelectedColumnsExport.InvokeAsync(exportItems);
    }

    /// <summary>
    /// Re-reads the items and rebuilds anything derived from them: the quick search result, the footer totals
    /// and the grid's own rows. Call this after changing the contents of <see cref="Items"/> in place.
    /// </summary>
    /// <remarks>
    /// This is the imperative equivalent of bumping <see cref="ItemsVersion"/>; use whichever suits the caller.
    /// </remarks>
    public async Task RefreshDataAsync()
    {
        // Rebuild the search result: an in-place change to Items leaves it stale, and none of the inputs it is
        // computed from have changed, so nothing else would trigger it.
        await RefreshSearchResultAsync();

        // The footer lives in the DOM outside Blazor's render tree, so forget the cached markup here:
        // a rebuilt grid must get the footer pushed again even when the totals themselves are unchanged.
        _lastRenderedFooter = null;

        if (_grid is null) return;

        await _grid.RefreshDataAsync();
    }

    public void SetTableIndex()
    {
        _isTableIndex = UsedColumnManager.Columns.Where(w => w.Visible).Any(x => x.Title == "#") && UsedColumnManager.IsIndexColumn;
    }

    private void UnselectAllItems()
    {
        if (Items is null) return;

        foreach (var item in Items)
        {
            if (item is ISelectionDto selectionDto)
            {
                selectionDto.IsSelected = false;
            }
        }
    }

    private void SelectAllItems()
    {
        if (Items is null) return;

        foreach (var item in Items)
        {
            if (item is ISelectionDto selectionDto)
            {
                selectionDto.IsSelected = true;
            }
        }
    }

    private async Task ManageColumns()
    {
        // No custom handler wired up: fall back to the built-in checkbox selector.
        if (Events?.OnManageColumns is null)
        {
            if (IsColumnSelection)
            {
                IsColumnItemsSelection = true;
            }

            return;
        }

        if (Id is null)
        {
            await Events.WarningRequested.InvokeAsync("Table ID is not set. Please set the ID parameter to enable this feature.");

            return;
        }

        await Events.OnManageColumns.Value.InvokeAsync();
    }

    private async Task OnColumnSelectionChangedAsync(ColumnConfig? config = null)
    {
        if (config != null)
        {
            SelectedConfiguration = config;
        }

        if (ColumnSelectionChanged.HasDelegate)
        {
            await ColumnSelectionChanged.InvokeAsync();
        }

        SetTableIndex();

        // Deferred, not pushed here: the JS helper walks the header cells that exist right now, and this runs
        // inside the event handler, before Blazor has rendered the new column set. Pushing a title per visible
        // column at a table that is still one <th> short drops the last one, and the cell rendered a moment later
        // never gets it - so re-showing a column used to leave the final header with no tooltip at all. Clearing
        // the latch hands the push to OnAfterRenderAsync, which runs once the header matches the columns.
        _titlesLoaded = false;

        await AddOrUpdateFooterAsync();
    }

    /// <summary>
    /// Rebuilds everything derived from the column set. Call it after changing which columns a grid shows —
    /// flipping <see cref="DynamicColumn{TGridItem}.Visible"/>, renaming a title, or adding a column to
    /// <c>ColumnManager.Columns</c> - from outside the wrapper.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The columns themselves need no help: the grid re-reads <c>ColumnManager.Get()</c> on every render, so a
    /// column appears or disappears as soon as the page re-renders. What does not follow is the header tooltips,
    /// which are pushed to the DOM once and then latched - leaving each one describing its neighbour until they
    /// are pushed again. This also raises <see cref="ColumnSelectionChanged"/>, so a page that persists the
    /// layout hears about a change it made itself the same way it hears about one made in the toolbar.
    /// </para>
    /// <para>
    /// It is exactly what the built-in <see cref="ColumnSelector{TGridItem}"/> runs, so a hand-rolled control and
    /// the toolbar one cannot drift apart.
    /// </para>
    /// </remarks>
    public async Task RefreshColumnsAsync()
    {
        // Ordered: RefreshDataAsync is what discards the cached footer markup, so rebuilding the footer before it
        // would be dropped as unchanged and the stale row would stay in the DOM.
        await RefreshDataAsync();
        await OnColumnSelectionChangedAsync();

        // The title push is deferred to the next OnAfterRenderAsync, so make sure one happens: a caller that
        // changed a column outside a UI event (a preference load, a timer) has no render of its own to ride on.
        StateHasChanged();
    }

    private async Task SelectView(ColumnConfig config)
    {
        SelectedConfiguration = config;

        SetColumnVisibility(SelectedConfiguration);

        if (Events is not null)
        {
            await Events.OnSelectView.InvokeAsync(config);
        }

        await OnColumnSelectionChangedAsync();
    }

    private async Task ResetViewToDefault()
    {
        SelectedConfiguration = null;

        foreach (var column in _defaultColumnManager.Columns)
        {
            if (column.FullTitle is null) continue;

            column.Visible = _defaultVisibleColumns.Contains(column.FullTitle);
        }

        if (Id != null && Events is not null)
        {
            await Events.OnResetViewToDefault.InvokeAsync();
        }

        await OnColumnSelectionChangedAsync();
    }

    private void SetColumnVisibility(ColumnConfig config)
    {
        foreach (var column in _defaultColumnManager.Columns)
        {
            column.Visible = config.IsColumnSelected(column.FullTitle);
        }
    }


    public async Task DisableExactMatch() => await SetExactMatchAsync(false);

    public async Task EnableExactMatch() => await SetExactMatchAsync(true);

    /// <summary>
    /// Applies the exact-match setting and re-runs any active search with it, so the toggle takes effect on the
    /// rows already on screen rather than only on the next keystroke.
    /// </summary>
    private async Task SetExactMatchAsync(bool exactMatch)
    {
        ExactMatch = exactMatch;

        // Refresh before notifying: if the caller binds ExactMatch, the round-trip finds the result already
        // current and does no further work.
        await RefreshSearchResultAsync();

        await ExactMatchChanged.InvokeAsync(ExactMatch);
    }

    public string GetTableClass() => _isTableIndex ? $"{Class} table-index".Trim() : Class ?? string.Empty;

    public string IsTableIndex() => _isTableIndex ? "table-index" : "";

    public void ToggleFilterSection()
    {
        _showFilterSection = !_showFilterSection;
    }

    public async ValueTask DisposeAsync()
    {
        _isDisposed = true;

        if (_moduleTask is not null)
        {
            try
            {
                var module = await _moduleTask;

                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}