using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace QuickGrid.Toolkit;

public partial class QuickGridWrapper<TGridItem> : ComponentBase, IAsyncDisposable
{
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
    [Parameter] public bool IsColumnSelection { get; set; } = true;
    [Parameter] public bool IsColumnItemsSelection { get; set; }
    [Parameter] public bool IsFilter { get; set; } = true;

    /// <summary>
    /// Renders a diagnostics panel above the grid showing the wrapper's live state: item counts, the active
    /// search and its mode, pagination, and column visibility. Intended for development only.
    /// </summary>
    [Parameter] public bool IsDebug { get; set; }

    [Parameter] public bool IsToolbar { get; set; } = true;
    [Parameter] public bool IsNestedSearch { get; set; } = true;
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
    /// The number of items to display per page when pagination is enabled. The default value is 20.
    /// </summary>
    [Parameter] public int ItemsPerPage { get; set; } = 20;
    [Parameter] public FilterCriteria<TGridItem>? FilterCriteria { get; set; }
    [Parameter] public RenderFragment? SelectedItemsActionDropDown { get; set; }
    [Parameter] public RenderFragment? FilterSection { get; set; }
    [Parameter] public RenderFragment? DropdownItems { get; set; }

    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = default!;
    [Inject] protected ILogger<QuickGridWrapper<TGridItem>> Logger { get; set; } = default!;

    /// <summary>
    /// Named column layouts offered in the column-layout menu. Supply them from markup, or assign them from a
    /// subclass once they have been loaded from storage.
    /// </summary>
    [Parameter] public List<ColumnConfig> ColumnConfigurations { get; set; } = [];
    public ColumnManager<TGridItem> UsedColumnManager { get; set; } = new();
    public ColumnConfig? SelectedConfiguration { get; set; }

    /// <summary>
    /// Icon set for this grid's toolbar. Overrides any <see cref="IQuickGridIconProvider"/> registered in DI,
    /// which is normally where the application sets its icons once for every grid.
    /// </summary>
    [Parameter] public IQuickGridIconProvider? Icons { get; set; }

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
    private IJSObjectReference? _module;

    /// <summary>
    /// Owns the query, the search options and the computed result. The component keeps only the parts that need
    /// the render loop: pushing parameters in, and raising <see cref="SearchResultChanged"/> afterwards.
    /// </summary>
    private readonly GridSearch<TGridItem> _search = new();

    // AsEnumerable() keeps the pattern match in C#: counting straight off the IQueryable would build an
    // expression tree, which cannot contain an 'is' pattern.
    private int _selectedItemsCount => VisibleItems?.AsEnumerable().Count(item => item is ISelectionDto { IsSelected: true }) ?? 0;

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
    private IQueryable<TGridItem>? VisibleItems => _search.Result ?? Items;

    /// <summary>
    /// Recomputes the search result and reports the rows now on show through <see cref="SearchResultChanged"/>.
    /// </summary>
    /// <remarks>
    /// Call this from lifecycle and event handlers whenever the displayed set may have changed — never from a
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

    protected override async Task OnParametersSetAsync()
    {
        SyncColumnManager();

        EnsurePaginationState();
        SetTableIndex();

        _search.SyncInputs(FilterCriteria, ExactMatch, IsNestedSearch);
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
        _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/QuickGrid.Toolkit/quickGridToolkit.js");

        await _module.InvokeVoidAsync(identifier, args);
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

        await RefreshColumnTitlesAsync();
        await AddOrUpdateFooterAsync();
    }

    /// <summary>
    /// Runs after the user toggles column visibility in the <see cref="ColumnSelector{TGridItem}"/>.
    /// </summary>
    /// <remarks>
    /// Refreshing the data alone is not enough: the JS helper maps tooltips onto header cells by index,
    /// so hiding or showing a column leaves every title shifted until they are pushed again.
    /// </remarks>
    private async Task OnColumnVisibilityChangedAsync()
    {
        await RefreshDataAsync();
        await OnColumnSelectionChangedAsync();
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
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}