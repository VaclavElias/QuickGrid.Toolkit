namespace QuickGrid.Toolkit.Tests;

public class QuickSearchUtilityTests
{
    private enum PersonKind
    {
        Employee,
        Manager
    }

    private sealed class Region
    {
        public string Code { get; set; } = "";
    }

    private sealed class Address
    {
        public string City { get; set; } = "";
        public Region Region { get; set; } = new();
    }

    private sealed class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public decimal Salary { get; set; }
        public Guid Reference { get; set; }
        public PersonKind Kind { get; set; }
        public Address Address { get; set; } = new();
        public List<string> Tags { get; set; } = [];
    }

    private static Person CreatePerson() => new()
    {
        Name = "Alice Johnson",
        Age = 42,
        Salary = 1234.56m,
        Reference = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        Kind = PersonKind.Manager,
        Address = new Address { City = "London", Region = new Region { Code = "UK-LDN" } },
        Tags = ["vip", "priority"]
    };

    [Fact]
    public void Matches_Substring_CaseInsensitive_ByDefault()
        => Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "alice"));

    [Fact]
    public void DoesNotMatch_UnrelatedTerm()
        => Assert.False(QuickSearchUtility.QuickSearch(CreatePerson(), "zebra"));

    [Fact]
    public void ReturnsFalse_ForNullItem()
        => Assert.False(QuickSearchUtility.QuickSearch<Person?>(null, "alice"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReturnsFalse_ForBlankQuery(string query)
        => Assert.False(QuickSearchUtility.QuickSearch(CreatePerson(), query));

    [Fact]
    public void Matches_NumericValue_ByItsStringForm()
        => Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "42"));

    [Fact]
    public void Matches_GuidValue_BySubstring()
        => Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "2222-3333"));

    [Fact]
    public void Matches_EnumValue_ByName()
        => Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "manager"));

    [Fact]
    public void CaseSensitive_Option_RejectsWrongCasing()
    {
        var options = new QuickSearchOptions { CaseSensitive = true };

        Assert.False(QuickSearchUtility.QuickSearch(CreatePerson(), "alice", options));
        Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "Alice", options));
    }

    [Fact]
    public void ExactMatch_RequiresWholeValue()
    {
        var options = new QuickSearchOptions { ExactMatch = true };

        Assert.False(QuickSearchUtility.QuickSearch(CreatePerson(), "Alice", options));
        Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "Alice Johnson", options));
    }

    [Fact]
    public void MultiTerm_And_RequiresAllTerms_AcrossProperties()
    {
        var options = new QuickSearchOptions { MultiTermOperator = SearchOperator.And };

        Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "alice 42", options));
        Assert.False(QuickSearchUtility.QuickSearch(CreatePerson(), "alice zebra", options));
    }

    [Fact]
    public void MultiTerm_Or_RequiresAnyTerm()
    {
        var options = new QuickSearchOptions { MultiTermOperator = SearchOperator.Or };

        Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "zebra alice", options));
        Assert.False(QuickSearchUtility.QuickSearch(CreatePerson(), "zebra lion", options));
    }

    [Fact]
    public void Matches_ChildProperty_AtDefaultDepth()
        => Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "london"));

    [Fact]
    public void DoesNotMatch_ChildProperty_WhenChildSearchDisabled()
        => Assert.False(QuickSearchUtility.QuickSearch(CreatePerson(), "london", includeChildProperties: false));

    [Fact]
    public void DoesNotMatch_GrandchildProperty_AtDefaultDepth()
        => Assert.False(QuickSearchUtility.QuickSearch(CreatePerson(), "UK-LDN"));

    [Fact]
    public void Matches_GrandchildProperty_WhenDepthRaised()
    {
        var options = new QuickSearchOptions { MaxSearchDepth = 2 };

        Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "UK-LDN", options));
    }

    [Fact]
    public void Skips_EnumerableProperties()
        => Assert.False(QuickSearchUtility.QuickSearch(CreatePerson(), "vip"));

    [Fact]
    public void ColumnNames_RestrictSearch_ToListedRootProperties()
    {
        var options = new QuickSearchOptions { ColumnNames = ["Name"] };

        Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "alice", options));
        Assert.False(QuickSearchUtility.QuickSearch(CreatePerson(), "42", options));
    }

    [Fact]
    public void ExcludedColumns_RemoveRootProperties_FromSearch()
    {
        var options = new QuickSearchOptions { ExcludedColumns = ["Name"] };

        Assert.False(QuickSearchUtility.QuickSearch(CreatePerson(), "alice", options));
        Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "42", options));
    }

    [Fact]
    public void ColumnFilter_MatchesCaseInsensitively()
    {
        var options = new QuickSearchOptions { ColumnNames = ["name"] };

        Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "alice", options));
    }

    [Fact]
    public void DuplicateTerms_AreDeduplicated_NotDoubleRequired()
        => Assert.True(QuickSearchUtility.QuickSearch(CreatePerson(), "alice alice"));
}
