namespace QuickGrid.Toolkit.Tests;

public class GridSearchTests
{
    private sealed class Person
    {
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
    }

    private static readonly List<Person> _people =
    [
        new() { Name = "Anna", City = "Prague" },
        new() { Name = "Ann", City = "Brno" },
        new() { Name = "Bob", City = "Prague" },
    ];

    private static GridSearch<Person> BuildSearch(bool exactMatch = false, bool nested = true)
    {
        var search = new GridSearch<Person>();

        search.SyncInputs(_people.AsQueryable(), filterCriteria: null, exactMatch, nested);

        return search;
    }

    [Fact]
    public void VisibleItems_FallsThroughToItems_WhenNoSearchIsActive()
    {
        var search = BuildSearch();

        search.Recompute();

        Assert.Null(search.Result);
        Assert.Equal(3, search.VisibleItems?.Count());
    }

    [Fact]
    public void Recompute_NarrowsToMatchingRows()
    {
        var search = BuildSearch();
        search.Query = "Prague";

        search.Recompute();

        Assert.Equal(2, search.Result?.Count());
        Assert.Equal(2, search.VisibleItems?.Count());
    }

    [Fact]
    public void Recompute_HonoursExactMatch()
    {
        var partial = BuildSearch();
        partial.Query = "Ann";
        partial.Recompute();

        var exact = BuildSearch(exactMatch: true);
        exact.Query = "Ann";
        exact.Recompute();

        Assert.Equal(2, partial.Result?.Count());   // Anna and Ann
        Assert.Equal(1, exact.Result?.Count());     // Ann only
    }

    // The grid re-queries whenever its Items reference changes, so an unchanged result must keep its identity.
    [Fact]
    public void Result_KeepsItsIdentity_UntilRecomputed()
    {
        var search = BuildSearch();
        search.Query = "Prague";
        search.Recompute();

        var first = search.Result;

        Assert.Same(first, search.VisibleItems);
        Assert.Same(first, search.VisibleItems);
    }

    [Fact]
    public void Recompute_ClearsTheResult_WhenTheQueryIsBlank()
    {
        var search = BuildSearch();
        search.Query = "Prague";
        search.Recompute();

        search.Query = "  ";
        search.Recompute();

        Assert.Null(search.Result);
        Assert.Equal(3, search.VisibleItems?.Count());
    }

    [Fact]
    public void InputsChanged_TracksQueryAndBothSearchOptions()
    {
        var search = BuildSearch();
        search.Recompute();

        Assert.False(search.InputsChanged());

        search.Query = "Bob";
        Assert.True(search.InputsChanged());

        search.Recompute();
        Assert.False(search.InputsChanged());

        search.SyncInputs(_people.AsQueryable(), filterCriteria: null, exactMatch: true, isNestedSearch: true);
        Assert.True(search.InputsChanged());

        search.Recompute();
        search.SyncInputs(_people.AsQueryable(), filterCriteria: null, exactMatch: true, isNestedSearch: false);
        Assert.True(search.InputsChanged());
    }

    // A page whose Items expression allocates a new queryable each render must not be treated as changed data,
    // or the search would re-run on every render. ItemsVersion / RefreshDataAsync are the signal for that.
    [Fact]
    public void InputsChanged_IgnoresANewItemsReference()
    {
        var search = BuildSearch();
        search.Query = "Prague";
        search.Recompute();

        search.SyncInputs(_people.AsQueryable(), filterCriteria: null, exactMatch: false, isNestedSearch: true);

        Assert.False(search.InputsChanged());
    }

    // Setting the options in markup is the caller's starting point, not a change to react to.
    [Fact]
    public void InputsChanged_IsFalse_ForNonDefaultOptionsOnTheFirstSync()
    {
        var search = new GridSearch<Person>();

        search.SyncInputs(_people.AsQueryable(), filterCriteria: null, exactMatch: true, isNestedSearch: false);

        Assert.False(search.InputsChanged());
    }

    [Fact]
    public void ApplyQuickSearchParameter_DoesNotWipeTypedText_WhenTheParameterIsUnchanged()
    {
        var search = BuildSearch();

        search.ApplyQuickSearchParameter(null);   // parent's bound value, never set
        search.Query = "typed by the user";
        search.ApplyQuickSearchParameter(null);   // parent re-renders

        Assert.Equal("typed by the user", search.Query);
    }

    [Fact]
    public void ApplyQuickSearchParameter_AppliesARealChange_IncludingAReset()
    {
        var search = BuildSearch();

        search.ApplyQuickSearchParameter("from the parent");
        Assert.Equal("from the parent", search.Query);

        search.ApplyQuickSearchParameter(null);
        Assert.Null(search.Query);
    }

    [Fact]
    public void Clear_ResetsTheQueryAndTheResult()
    {
        var search = BuildSearch();
        search.Query = "Prague";
        search.Recompute();

        search.Clear();

        Assert.Null(search.Query);
        Assert.Null(search.Result);
        Assert.Equal(3, search.VisibleItems?.Count());
        Assert.False(search.InputsChanged());
    }

    [Fact]
    public async Task RunFilterCriteriaSearchAsync_LeavesTheGridUnfiltered_ForATermBelowTheMinimum()
    {
        var criteria = new FilterCriteria<Person>(term => p => p.Name.Contains(term));
        var search = new GridSearch<Person>();
        search.SyncInputs(_people.AsQueryable(), criteria, exactMatch: false, isNestedSearch: true);

        var queried = await search.RunFilterCriteriaSearchAsync("ab");

        Assert.False(queried);
        Assert.Null(search.Result);
        Assert.Equal(3, search.VisibleItems?.Count());
        Assert.Equal("ab", search.Query);
    }

    // The path above the minimum length cannot be covered here: it calls ToListAsync, which needs an
    // IAsyncQueryProvider, and a List.AsQueryable() does not have one. That is not a test-setup problem — it is
    // exactly the defect B6 describes, since a caller passing in-memory items alongside FilterCriteria gets the
    // same throw at runtime. Cover it once B6 lands and the EF sample (F1) exists to run it against.
    [Fact]
    public void RunFilterCriteriaSearchAsync_AboveTheMinimum_RequiresAnAsyncQueryProvider()
    {
        var criteria = new FilterCriteria<Person>(term => p => p.Name.Contains(term));
        var search = new GridSearch<Person>();
        search.SyncInputs(_people.AsQueryable(), criteria, exactMatch: false, isNestedSearch: true);

        Assert.ThrowsAny<InvalidOperationException>(() => search.RunFilterCriteriaSearchAsync("Ann").GetAwaiter().GetResult());
    }
}
