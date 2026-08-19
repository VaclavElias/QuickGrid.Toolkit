using Microsoft.EntityFrameworkCore;

namespace QuickGrid.Toolkit.Core;

/// <summary>
/// Owns everything about narrowing a grid's rows: the query text, the search options, the computed result and the
/// two ways of producing it — reflection-based quick search over an in-memory source, or a
/// <see cref="FilterCriteria{TGridItem}"/> expression executed against the source.
/// </summary>
/// <remarks>
/// <para>
/// This type holds state but raises no events and touches no rendering. <see cref="QuickGridWrapper{TGridItem}"/>
/// pushes the current parameter values in through <see cref="SyncInputs"/>, asks <see cref="InputsChanged"/>
/// whether anything relevant moved, and calls <see cref="Recompute"/> when it did. Notification stays with the
/// component, which is the only thing that knows about <c>EventCallback</c> and the render loop.
/// </para>
/// <para>
/// <strong>It deliberately does not hold the item source.</strong> The rows are passed to each call that needs
/// them, and the component falls back to its own <c>Items</c> parameter whenever <see cref="Result"/> is null.
/// A snapshot here would go stale: Blazor renders a component whose <c>OnInitializedAsync</c> is still running
/// <em>before</em> <c>OnParametersSetAsync</c> has ever been called, so a grid inside a subclass that awaits
/// anything on init would show an empty table until the next interaction.
/// </para>
/// <para>
/// A changed source <em>reference</em> is likewise not part of <see cref="InputsChanged"/>. Callers signal changed
/// data with <c>ItemsVersion</c> or <c>RefreshDataAsync</c>; a page whose <c>Items</c> expression allocates a new
/// queryable on every render would otherwise re-run the search continuously.
/// </para>
/// </remarks>
internal sealed class GridSearch<TGridItem>
{
    /// <summary>
    /// Minimum number of characters before a <see cref="FilterCriteria{TGridItem}"/> backed search queries the
    /// source. Shorter terms leave the grid unfiltered.
    /// </summary>
    public const int MinFilterSearchLength = 3;

    private bool _inputsSeeded;
    private string? _lastQuickSearchParameter;
    private string? _computedQuery;
    private bool _computedExactMatch;
    private bool _computedNestedSearch;

    private FilterCriteria<TGridItem>? _filterCriteria;
    private bool _exactMatch;
    private bool _nestedSearch = true;

    /// <summary>
    /// The active search text. Bound directly by the in-memory search box, so it is settable.
    /// </summary>
    public string? Query { get; set; }

    /// <summary>The rows the last <see cref="FilterCriteria{TGridItem}"/> query returned. Exposed for diagnostics.</summary>
    public List<TGridItem>? EvaluatedItems { get; private set; }

    /// <summary>
    /// The active search result, or <see langword="null"/> when no search is narrowing the grid — in which case
    /// the caller shows its own unfiltered rows. Only <see cref="Recompute"/> writes it.
    /// </summary>
    /// <remarks>
    /// Held as a queryable, not just a list: <c>AsQueryable()</c> allocates a new wrapper on every call and
    /// QuickGrid re-queries whenever its <c>Items</c> reference changes, so handing it a fresh one each render
    /// would refresh the grid continuously.
    /// </remarks>
    public IQueryable<TGridItem>? Result { get; private set; }

    /// <summary>
    /// Takes the current parameter values from the component. Call before <see cref="InputsChanged"/> or
    /// <see cref="Recompute"/> so both see the same state.
    /// </summary>
    public void SyncInputs(FilterCriteria<TGridItem>? filterCriteria, bool exactMatch, bool isNestedSearch)
    {
        _filterCriteria = filterCriteria;
        _exactMatch = exactMatch;
        _nestedSearch = isNestedSearch;

        // Take the initial search options as the baseline, so setting them in markup is not read as a change on
        // the first parameter set. An initial query is a real search and is deliberately not seeded here.
        if (!_inputsSeeded)
        {
            _inputsSeeded = true;
            _computedExactMatch = exactMatch;
            _computedNestedSearch = isNestedSearch;
        }
    }

    /// <summary>
    /// Applies the component's <c>QuickSearch</c> parameter, including when the caller clears it.
    /// </summary>
    /// <remarks>
    /// Only an actual change to the parameter is applied. Text typed into the built-in search box lives in
    /// <see cref="Query"/> alone, so comparing against the last parameter value keeps a parent re-render from
    /// wiping it, while still letting the parent reset the search by setting the parameter to null or empty.
    /// </remarks>
    public void ApplyQuickSearchParameter(string? quickSearch)
    {
        if (_lastQuickSearchParameter == quickSearch) return;

        _lastQuickSearchParameter = quickSearch;
        Query = quickSearch;
    }

    /// <summary>Whether anything the result is computed from has changed since the last <see cref="Recompute"/>.</summary>
    public bool InputsChanged()
        => _computedQuery != Query
            || _computedExactMatch != _exactMatch
            || _computedNestedSearch != _nestedSearch;

    /// <summary>Rebuilds <see cref="Result"/> from the current query and options.</summary>
    /// <param name="items">The rows to search. Ignored when no search is active.</param>
    public void Recompute(IQueryable<TGridItem>? items)
    {
        var query = Query;

        _computedQuery = query;
        _computedExactMatch = _exactMatch;
        _computedNestedSearch = _nestedSearch;

        // No search: a null result tells the caller to show its own rows, so a source that changed between
        // renders reaches the grid immediately instead of through a snapshot taken here.
        if (string.IsNullOrWhiteSpace(query))
        {
            Result = null;

            return;
        }

        if (_filterCriteria is null)
        {
            var options = new QuickSearchOptions()
            {
                IncludeChildProperties = _nestedSearch,
                ExactMatch = _exactMatch
            };

            Result = items?.Where(item => QuickSearchUtility.QuickSearch(item, query, options: options)).ToList().AsQueryable();

            return;
        }

        // FilterCriteria path: the source was queried in RunFilterCriteriaSearchAsync. A null result means there
        // is nothing to narrow by yet (the term is still below MinFilterSearchLength), so leave the grid
        // unfiltered rather than blanking it.
        Result = EvaluatedItems?.AsQueryable();
    }

    /// <summary>
    /// Runs a <see cref="FilterCriteria{TGridItem}"/> search against the source and recomputes the result.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the source was actually queried, so the caller can show a loading state around
    /// it. A term shorter than <see cref="MinFilterSearchLength"/> returns <see langword="false"/> and leaves the
    /// grid unfiltered rather than showing a stale or empty result.
    /// </returns>
    public async Task<bool> RunFilterCriteriaSearchAsync(string text, IQueryable<TGridItem> items)
    {
        Query = text;

        if (_filterCriteria is null || text.Length < MinFilterSearchLength)
        {
            EvaluatedItems = null;
            Recompute(items);

            return false;
        }

        _filterCriteria.SearchTerm = text;

        EvaluatedItems = await items.Where(_filterCriteria.CreateExpression()).ToListAsync();

        Recompute(items);

        return true;
    }

    /// <summary>
    /// Clears the query and any evaluated result, so the grid falls back to the full item set.
    /// </summary>
    public void Clear()
    {
        Query = null;
        EvaluatedItems = null;

        if (_filterCriteria is not null)
        {
            _filterCriteria.SearchTerm = string.Empty;
        }

        // With no query the result is null whatever the source is, so this needs no rows.
        Recompute(items: null);
    }
}