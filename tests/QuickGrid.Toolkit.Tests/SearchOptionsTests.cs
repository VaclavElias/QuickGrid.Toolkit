using System.Reflection;

namespace QuickGrid.Toolkit.Tests;

/// <summary>
/// Covers the two members <c>QuickGridWrapper</c> leans on to expose <see cref="QuickSearchOptions"/> as a live
/// parameter: an independent copy, and a comparison by value rather than by reference.
/// </summary>
public class SearchOptionsTests
{
    [Fact]
    public void ValuesEqual_IsTrue_ForTwoDistinctInstancesWithTheSameValues()
    {
        // The case that matters: SearchOptions="new() { MaxSearchDepth = 3 }" allocates a fresh instance on every
        // render, and a reference check would call that a change and re-run the search each time.
        var left = new QuickSearchOptions { MaxSearchDepth = 3 };
        var right = new QuickSearchOptions { MaxSearchDepth = 3 };

        Assert.False(ReferenceEquals(left, right));
        Assert.True(QuickSearchOptions.ValuesEqual(left, right));
    }

    [Fact]
    public void ValuesEqual_ComparesTheColumnListsByContent_NotByIdentity()
    {
        var left = new QuickSearchOptions { ColumnNames = ["Name"] };
        var right = new QuickSearchOptions { ColumnNames = ["Name"] };

        Assert.True(QuickSearchOptions.ValuesEqual(left, right));

        right.ColumnNames!.Add("City");

        Assert.False(QuickSearchOptions.ValuesEqual(left, right));
    }

    [Fact]
    public void ValuesEqual_TreatsNullAndEmptyColumnListsAsTheSame()
    {
        // Both mean "no filter" to the search, so moving between them must not read as a change - D4 will be
        // writing ColumnNames from the visible columns and can legitimately land on either.
        var unset = new QuickSearchOptions();
        var empty = new QuickSearchOptions { ColumnNames = [], ExcludedColumns = [] };

        Assert.True(QuickSearchOptions.ValuesEqual(unset, empty));
    }

    // The drift guard. QuickSearchOptions is a public mutable class, so a property added later and forgotten in
    // ValuesEqual would become a knob that silently changes nothing.
    [Fact]
    public void ValuesEqual_NoticesAChangeToEveryPublicProperty()
    {
        var properties = WritableProperties();

        Assert.NotEmpty(properties);

        foreach (var property in properties)
        {
            var left = new QuickSearchOptions();
            var right = new QuickSearchOptions();

            property.SetValue(right, Different(property.GetValue(right), property.PropertyType));

            Assert.False(
                QuickSearchOptions.ValuesEqual(left, right),
                $"{property.Name} is not compared by ValuesEqual, so changing it would not re-run the search.");
        }
    }

    [Fact]
    public void Clone_CopiesEveryPublicProperty()
    {
        var original = new QuickSearchOptions();

        foreach (var property in WritableProperties())
        {
            property.SetValue(original, Different(property.GetValue(original), property.PropertyType));
        }

        Assert.True(QuickSearchOptions.ValuesEqual(original, original.Clone()));
    }

    [Fact]
    public void Clone_CopiesTheListsRatherThanSharingThem()
    {
        // Without this the wrapper's snapshot of "what the last search ran with" would point at the caller's own
        // list, so mutating it in place would leave both sides equal and the change would go unnoticed.
        var original = new QuickSearchOptions { ColumnNames = ["Name"], ExcludedColumns = ["Code"] };
        var copy = original.Clone();

        original.ColumnNames!.Add("City");
        original.ExcludedColumns!.Add("Id");

        Assert.Equal(["Name"], copy.ColumnNames);
        Assert.Equal(["Code"], copy.ExcludedColumns);
    }

    private static PropertyInfo[] WritableProperties()
        => [.. typeof(QuickSearchOptions)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.CanWrite)];

    /// <summary>
    /// Any value other than the one given. Deliberately throws on an unhandled type: a new property whose type is
    /// not covered here is the same drift the tests above exist to catch, and should fail loudly.
    /// </summary>
    private static object? Different(object? current, Type type)
    {
        if (type == typeof(bool)) return !(bool)current!;

        if (type == typeof(int)) return (int)current! + 1;

        if (type.IsEnum) return Enum.GetValues(type).Cast<object>().First(value => !value.Equals(current));

        if (type == typeof(List<string>)) return current is null ? new List<string> { "Changed" } : null;

        throw new NotSupportedException(
            $"Extend Different() for {type.Name} so the QuickSearchOptions drift guards keep working.");
    }
}
