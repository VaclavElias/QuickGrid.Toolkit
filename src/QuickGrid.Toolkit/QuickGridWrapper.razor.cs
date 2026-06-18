using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Globalization;
using System.Net;
using System.Text;

namespace QuickGrid.Toolkit;

public partial class QuickGridWrapper<TGridItem> : ComponentBase, IAsyncDisposable
{
    [Parameter] public string? Id { get; set; }
    [Parameter] public string? Class { get; set; } = "table table-sm table-index table-striped small table-fit table-thead-sticky table-no-empty-lines mb-0";
    [Parameter] public string? DownloadFileName { get; set; }
    [Parameter] public string? QuickSearch { get; set; }

    /// <summary>
    /// A version number that should be incremented whenever the Items collection changes. It is used to detect changes and refresh the grid accordingly, specifically footer totals and if external filter is applied while search is active.
    /// </summary>
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
    [Parameter] public bool IsToolbar { get; set; } = true;
    [Parameter] public bool IsNestedSearch { get; set; } = true;
    [Parameter] public TotalFooter TotalFooter { get; set; } = new();
    [Parameter] public bool ExactMatch { get; set; }
    [Parameter] public bool IsExportEnabled { get; set; }
    [Parameter] public bool IsPreviewFeature { get; set; }
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

    public List<ColumnConfig> ColumnConfigurations { get; set; } = [];
    public ColumnManager<TGridItem> UsedColumnManager { get; set; } = new();
    public ColumnConfig? SelectedConfiguration { get; set; }

    // Resolve icon provider lazily with a safe default so the component doesn't throw if it's not registered in DI
    private IQuickGridIconProvider? _iconProvider;
    protected IQuickGridIconProvider IconProvider =>
        _iconProvider ??= ServiceProvider.GetService<IQuickGridIconProvider>() ?? new DefaultQuickGridIconProvider();

    private const string ColumnTitleSetupErrorMessage = "Non-critical: Failed to setup column titles for {Id}. Application continues to run without this feature.";
    private const string FooterSetupErrorMessage = "Non-critical: Failed to setup footer for {Id}. Application continues to run without this feature.";

    private bool _titlesLoaded;
    private bool _isTableIndex;
    private bool _isInMemorySearch;
    private bool _showFilterSection;
    private bool _isDebug = false;
    private bool _refreshGridAfterRender;

    private long _prevItemsVersion;
    private string? _searchQuery;
    private string? _lastSearchQuery;

    private QuickGrid<TGridItem>? _grid;
    private PaginationState? _pagination;
    private ColumnManager<TGridItem> _defaultColumnManager = new();

    private List<TGridItem>? _cachedFilteredItems;
    private List<string> _defaultVisibleColumns = [];
    private List<TGridItem>? _evaluatedItems;
    private readonly List<FooterColumn<TGridItem>> _footerColumns = [];
    private IJSObjectReference? _module;

    private int _selectedItemsCount => _filteredItems?.Count(item => (ISelectionDto?)item != null && ((ISelectionDto?)item)!.IsSelected) ?? 0;

    private IQueryable<TGridItem>? _filteredItems
    {
        get
        {
            IQueryable<TGridItem>? result;

            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                result = Items;

            }
            else if (FilterCriteria is null)
            {
                var searchOptions = new QuickSearchOptions()
                {
                    IncludeChildProperties = IsNestedSearch,
                    ExactMatch = ExactMatch
                };

                if (_searchQuery != _lastSearchQuery)
                {
                    _cachedFilteredItems = Items?.Where(item => QuickSearchAction(item, _searchQuery, searchOptions)).ToList();
                }

                result = _cachedFilteredItems?.AsQueryable();
            }
            else
            {
                result = _evaluatedItems?.AsQueryable();
            }

            if (_searchQuery != _lastSearchQuery)
            {
                _ = SearchResultChanged.InvokeAsync(result?.ToList() ?? []);

                _lastSearchQuery = _searchQuery;
            }

            return result;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _defaultColumnManager = ColumnManager;

        //var result = await ColumnConfigurationService.GetConfigurationsAsync(Id, _authenticatedUserId, default);

        //if (result.IsSuccess)
        //{
        //    _columnConfigurations = result.Value;
        //}

        SetDefaultColumns();

        _isInMemorySearch = FilterCriteria is null;

        await Task.CompletedTask;
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
        EnsurePaginationState();
        SetTableIndex();
        UpdateSearchQuery();

        if (Items is not null && QueryableItems is not null && Events is not null)
        {
            await Events.WarningRequested.InvokeAsync("Provide only one of Items or QueryableItems.");
        }

        if (_prevItemsVersion != ItemsVersion)
        {
            _prevItemsVersion = ItemsVersion;

            // Invalidate caches and force re-evaluation for current search
            _lastSearchQuery = null;
            _refreshGridAfterRender = true;

            await AddOrUpdateFooterAsync();
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

    private void UpdateSearchQuery()
    {
        if (string.IsNullOrWhiteSpace(QuickSearch) || _searchQuery == QuickSearch) return;

        _searchQuery = QuickSearch;
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

    public async ValueTask AddOrUpdateFooterAsync()
    {
        if (Id is null || !HasFooter()) return;

        try
        {
            await InvokeModuleVoidAsync("addOrUpdateFooter", Id, GenerateTableFooterWithTotals());
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
        => UsedColumnManager.FooterColumns.Count > 0 || TotalFooter.IsTotalFooter;

    private void SetDefaultColumns()
    {
        InitializeDefaultColumnVisibility();

        SelectedConfiguration = ColumnConfigurations.FirstOrDefault(w => w.Default);

        if (SelectedConfiguration != null)
        {
            SetColumnVisibility(SelectedConfiguration);
        }

        UsedColumnManager = _defaultColumnManager;

        //StateHasChanged();

        //foreach (var column in _defaultColumnManager.Columns)
        //{
        //    column.Visible = _selectedConfiguration.IsColumnSelected(column.FullTitle);

        //    _usedColumnManager.Add(column);
        //}

        //ColumnManager.SetVisibleColumns(_selectedConfiguration.ColumnSelections.Select(s => s.ColumnName));

        //ColumnManager.ResetColumnVisibility();
    }

    private void InitializeDefaultColumnVisibility()
        => _defaultVisibleColumns = _defaultColumnManager.Columns
            .Where(w => w.Visible && w.FullTitle != null)
            .Select(s => s.FullTitle!)
            .ToList();

    private string GenerateTableFooterWithTotals()
    {
        var html = UsedColumnManager.FooterColumns.Count > 0
            ? GenerateManualFooterCells()
            : GenerateAutomaticFooterCells();

        return $"<tr class=\"table-warning fw-bold\">{html}</tr>";
    }

    private string GenerateManualFooterCells()
    {
        StringBuilder html = new();
        var footerItems = GetFooterItems();

        foreach (var column in UsedColumnManager.Columns.Where(w => w.Visible && w.Class != "d-none"))
        {
            var footerColumn = UsedColumnManager.FooterColumns.FirstOrDefault(w => w.Id == column.Id);

            if (footerColumn is null)
            {
                html.Append("<td></td>");
            }
            else if (footerColumn.Content != null)
            {
                html.Append(footerColumn.Content);
            }
            else
            {
                html.Append(footerColumn.StringContent?.Invoke(footerItems));
            }
        }

        return html.ToString();
    }

    private string GenerateAutomaticFooterCells()
    {
        StringBuilder html = new();

        var visibleColumns = UsedColumnManager.Columns
            .Where(w => w.Visible && w.Class != "d-none")
            .ToList();

        var labelColumn = GetTotalFooterLabelColumn(visibleColumns);

        foreach (var column in visibleColumns)
        {
            if (column == labelColumn)
            {
                html.Append(BuildFooterCell(TotalFooter.TotalFooterLabel, column.Class));
            }
            else if (ShouldCalculateTotal(column))
            {
                html.Append(BuildTotalFooterCell(column));
            }
            else
            {
                html.Append("<td></td>");
            }
        }

        return html.ToString();
    }

    private DynamicColumn<TGridItem>? GetTotalFooterLabelColumn(List<DynamicColumn<TGridItem>> visibleColumns)
        => TotalFooter.TotalFooterLabelColumnId is int labelColumnId
            ? visibleColumns.FirstOrDefault(w => w.Id == labelColumnId)
            : visibleColumns.FirstOrDefault(w => !w.IsNumeric);

    private static bool ShouldCalculateTotal(DynamicColumn<TGridItem> column)
        => column.Property is not null && column.CalculateTotal switch
        {
            true => true,
            false => false,
            _ => column.IsNumeric
        };

    private string BuildTotalFooterCell(DynamicColumn<TGridItem> column)
    {
        var compiledExpression = column.Property!.Compile();
        var total = GetFooterItems().Sum(item => Convert.ToDecimal(compiledExpression(item)));
        var format = column.Format ?? TotalFooter.DefaultFormat;
        var cssClass = TotalFooter.RemoveClass is { Length: > 0 } removeClass
            ? column.Class?.Replace(removeClass, "")
            : column.Class;

        return BuildFooterCell(total.ToString(format, CultureInfo.InvariantCulture), cssClass);
    }

    private IEnumerable<TGridItem> GetFooterItems()
        => _filteredItems ?? Items ?? Enumerable.Empty<TGridItem>();

    private static string BuildFooterCell(string? value, string? cssClass)
    {
        var classAttribute = string.IsNullOrWhiteSpace(cssClass)
            ? string.Empty
            : $" class=\"{WebUtility.HtmlEncode(cssClass)}\"";

        return $"<td{classAttribute}>{WebUtility.HtmlEncode(value)}</td>";
    }

    private async Task SearchTextChanged(string? text)
    {
        if (FilterCriteria is null || Items is null) return;

        if (string.IsNullOrWhiteSpace(text))
        {
            ClearSearch();
            //await AddOrUpdateFooterAsync();

            return;
        }
        else if (text.Length < 3)
        {
            _searchQuery = text;

            return;
        }
        else
        {
            _searchQuery = text;
        }

        FilterCriteria.SearchTerm = _searchQuery;

        IsLoading = true;

        _evaluatedItems = await Items.Where(FilterCriteria.CreateExpression()).ToListAsync();

        IsLoading = false;

        //await AddOrUpdateFooterAsync();
    }

    private async Task OnInMemorySearchChanged()
    {
        //await AddOrUpdateFooterAsync();

        if (string.IsNullOrEmpty(_searchQuery))
        {
            await QuickSearchChanged.InvokeAsync(_searchQuery);
        }
    }

    public void ClearSearch() => ClearSearch(true);

    public void ClearSearch(bool shouldInvokeCallback = false)
    {
        //_filteredItems = Items;
        _searchQuery = null;

        if (shouldInvokeCallback)
        {
            QuickSearchChanged.InvokeAsync(_searchQuery);
        }
    }

    public bool QuickSearchAction(TGridItem item, string query, QuickSearchOptions searchOptions)
        => QuickSearchUtility.QuickSearch(item, query, options: searchOptions);

    public Task ExportAsync() => Events?.OnExport.InvokeAsync(_filteredItems) ?? Task.CompletedTask;

    public async Task ExportSelectedColumnsAsync()
    {
        if (Events is null) return;

        if (_filteredItems is null)
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
            await Events.WarningRequested.InvokeAsync("No columns to export. Contact Vaclav if this should be working.");

            return;
        }

        var exportItems = _filteredItems.ToList()
            .Select(item => ExpandoObjectBuilder<TGridItem>.Create(item, visibleColumns))
            .Where(obj => obj != null);

        await Events.OnSelectedColumnsExport.InvokeAsync(exportItems);
    }

    public async Task RefreshDataAsync()
    {
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
        if (Events?.OnResetViewToDefault is null && IsColumnSelection)
        {
            IsColumnItemsSelection = true;
        }

        if (Events?.OnManageColumns is null) return;

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

        await RefreshColumnTitlesAsync();
        await AddOrUpdateFooterAsync();
    }

    private void OnColumnSelectorClose()
    {
        Console.WriteLine("Test");
    }

    private async Task SelectView(ColumnConfig config)
    {
        SelectedConfiguration = config;

        SetColumnVisibility(SelectedConfiguration);

        //ColumnManager.SetVisibleColumns(config.ColumnSelections.Select(s => s.ColumnName));

        //ColumnManager.ResetColumnVisibility();

        if (Events is not null)
        {
            await Events.OnSelectView.InvokeAsync(config);
        }

        await OnColumnSelectionChangedAsync();
    }

    private async void ResetViewToDefault()
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

    //public string GetTableClass() => $"table table-sm {IsTableIndex()} table-striped small table-blazor table-fit table-thead-sticky table-bg-transparent mb-0";

    public async Task DisableExactMatch()
    {
        ExactMatch = false;
        await ExactMatchChanged.InvokeAsync(ExactMatch);
    }

    public async Task EnableExactMatch()
    {
        ExactMatch = true;
    }

    public string GetTableClass() => $"{Class} {IsTableIndex()}";

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