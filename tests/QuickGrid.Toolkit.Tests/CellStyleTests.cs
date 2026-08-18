namespace QuickGrid.Toolkit.Tests;

public class CellStyleHelperTests
{
    [Theory]
    [InlineData(-5, "negative")]
    [InlineData(7, "positive")]
    [InlineData(0, "zero")]
    public void DetermineNumericValueNature_DescribesSign_ForInt(int value, string expected)
        => Assert.Equal(expected, CellStyleHelper.DetermineNumericValueNature<int>(value));

    [Fact]
    public void DetermineNumericValueNature_DescribesSign_ForDecimal()
        => Assert.Equal("negative", CellStyleHelper.DetermineNumericValueNature<decimal>(-0.01m));

    [Fact]
    public void DetermineNumericValueNature_HandlesWiderNumericTypes()
    {
        Assert.Equal("positive", CellStyleHelper.DetermineNumericValueNature<long>(9_000_000_000L));
        Assert.Equal("negative", CellStyleHelper.DetermineNumericValueNature<float>(-1.5f));
        Assert.Equal("positive", CellStyleHelper.DetermineNumericValueNature<short>(3));
    }

    [Fact]
    public void DetermineNumericValueNature_ReturnsNoValue_ForNull()
        => Assert.Equal("no-value", CellStyleHelper.DetermineNumericValueNature<int>(null));

    [Fact]
    public void DetermineNumericValueNature_ReturnsUnknown_ForNaN()
        => Assert.Equal("unknown", CellStyleHelper.DetermineNumericValueNature<double>(double.NaN));

    [Fact]
    public void DetermineNumericValueNature_PrefersCustomStyle_WhenValueIsMapped()
    {
        var map = new CellStyleMap<int>().Add(7, "highlight");

        Assert.Equal("highlight", CellStyleHelper.DetermineNumericValueNature<int>(7, map));
        Assert.Equal("negative", CellStyleHelper.DetermineNumericValueNature<int>(-1, map));
    }

    [Fact]
    public void DetermineNumericValueNature_UsesDefaultMapping_ForNull_WhenDefaultIsMapped()
    {
        var map = new CellStyleMap<int>().Add(0, "empty-style");

        Assert.Equal("empty-style", CellStyleHelper.DetermineNumericValueNature<int>(null, map));
    }

    [Fact]
    public void GetValueStyle_ReturnsEmpty_WithoutMap()
        => Assert.Equal(string.Empty, CellStyleHelper.GetValueStyle<string>("x"));

    [Fact]
    public void GetValueStyle_ReturnsMappedStyle()
    {
        var map = new CellStyleMap<string>().Add("active", "text-success");

        Assert.Equal("text-success", CellStyleHelper.GetValueStyle("active", map));
        Assert.Equal(string.Empty, CellStyleHelper.GetValueStyle("other", map));
    }
}

public class CellStyleMapTests
{
    [Fact]
    public void Add_KeepsFirstStyle_ForDuplicateValue()
    {
        var map = new CellStyleMap<int>().Add(1, "first").Add(1, "second");

        Assert.Equal("first", map.GetStyle(1));
    }

    [Fact]
    public void Add_WithNullValue_SetsNullStyle()
    {
        var map = new CellStyleMap<string>().Add(null!, "null-style");

        Assert.Equal("null-style", map.GetStyle(null));
        Assert.True(map.ContainsValue(null));
    }

    [Fact]
    public void GetStyle_ReturnsEmpty_ForUnmappedValue()
        => Assert.Equal(string.Empty, new CellStyleMap<int>().GetStyle(5));

    [Fact]
    public void SetNullStyle_AppliesToNullLookups()
    {
        var map = new CellStyleMap<int?>().SetNullStyle("missing");

        Assert.Equal("missing", map.GetStyle(null));
    }

    [Fact]
    public void FromMappings_BuildsMap_WithOptionalNullStyle()
    {
        var map = CellStyleMap<int?>.FromMappings([new(1, "one"), new(2, "two")], nullValueStyle: "none");

        Assert.Equal("one", map.GetStyle(1));
        Assert.Equal("two", map.GetStyle(2));
        Assert.True(map.ContainsValue(null));
    }

    [Fact]
    public void ReplaceWith_DiscardsOldMappings()
    {
        var map = new CellStyleMap<int?>().Add(1, "old").SetNullStyle("old-null");

        map.ReplaceWith([new(2, "new")]);

        Assert.Equal(string.Empty, map.GetStyle(1));
        Assert.Equal("new", map.GetStyle(2));
        Assert.False(map.ContainsValue(null));
    }

    [Fact]
    public void Remove_DeletesMapping_AndReportsWhetherItExisted()
    {
        var map = new CellStyleMap<int>().Add(1, "one");

        Assert.True(map.Remove(1));
        Assert.False(map.Remove(1));
        Assert.Equal(string.Empty, map.GetStyle(1));
    }

    [Fact]
    public void Clear_CanPreserveNullStyle()
    {
        var map = new CellStyleMap<int?>().Add(1, "one").SetNullStyle("null-style");

        map.Clear(clearNullStyle: false);

        Assert.Equal(string.Empty, map.GetStyle(1));
        Assert.True(map.ContainsValue(null));
    }
}
