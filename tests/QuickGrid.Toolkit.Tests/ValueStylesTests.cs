namespace QuickGrid.Toolkit.Tests;

public class ValueStylesTests
{
    private sealed class Row
    {
        public decimal Amount { get; set; }
    }

    private static string Render(ColumnManager<Row> columns, decimal amount)
        => RenderHelper.ToMarkup(columns.Columns[0].ChildContent!(new Row { Amount = amount }));

    private static ColumnManager<Row> BuildManager(GridValueStyles? styles = null, CellStyleMap<decimal>? cellStyle = null)
    {
        var columns = new ColumnManager<Row>();

        if (styles.HasValue) columns.ValueStyles = styles.Value;

        columns.AddStyledNumber(r => r.Amount, "Amount", cellStyle: cellStyle);

        return columns;
    }

    [Theory]
    [InlineData(-5, "negative")]
    [InlineData(5, "positive")]
    [InlineData(0, "zero")]
    public void EveryNature_IsMarkedUp_ByDefault(int amount, string expected)
    {
        var columns = BuildManager();

        Assert.Contains($"content=\"{expected}\"", Render(columns, amount));
    }

    [Fact]
    public void ADisabledNature_EmitsNoSpanAtAll_SoNoCssHasToUnstyleIt()
    {
        var columns = BuildManager(GridValueStyles.Negative | GridValueStyles.Zero);

        var positive = Render(columns, 5m);

        Assert.DoesNotContain("<span", positive);
        Assert.Equal("5", positive);
    }

    [Fact]
    public void DisablingOneNature_LeavesTheOthersAlone()
    {
        var columns = BuildManager(GridValueStyles.Negative | GridValueStyles.Zero);

        Assert.Contains("content=\"negative\"", Render(columns, -5m));
        Assert.Contains("content=\"zero\"", Render(columns, 0m));
    }

    [Fact]
    public void None_MarksUpNothing()
    {
        var columns = BuildManager(GridValueStyles.None);

        Assert.DoesNotContain("<span", Render(columns, -5m));
        Assert.DoesNotContain("<span", Render(columns, 5m));
        Assert.DoesNotContain("<span", Render(columns, 0m));
    }

    // Load-bearing: the setting is read as the cell renders, not as the column is built, so a caller that
    // configures its columns through a shared method can still change it afterwards.
    [Fact]
    public void SettingIt_AfterTheColumnsWereAdded_StillApplies()
    {
        var columns = BuildManager();

        Assert.Contains("content=\"positive\"", Render(columns, 5m));

        columns.ValueStyles = GridValueStyles.Negative;

        Assert.DoesNotContain("<span", Render(columns, 5m));
    }

    // A CellStyleMap is the application's own vocabulary; the flags only govern the three built-in names.
    [Fact]
    public void ACustomStyleName_IsNeverSuppressed()
    {
        var map = new CellStyleMap<decimal>().Add(5m, "status-good");
        var columns = BuildManager(GridValueStyles.None, map);

        Assert.Contains("content=\"status-good\"", Render(columns, 5m));
    }

    // The rule is by name, not by origin: a map that deliberately produces a built-in name opts into the flag.
    [Fact]
    public void AMapProducingABuiltInName_IsGovernedByTheFlags()
    {
        var map = new CellStyleMap<decimal>().Add(5m, "zero");
        var columns = BuildManager(GridValueStyles.Negative | GridValueStyles.Positive, map);

        Assert.DoesNotContain("<span", Render(columns, 5m));
    }

    // Documents a real limitation: Clone copies the render delegate by reference, and that delegate reads the
    // ValueStyles of the builder that created it. Copied columns therefore keep their source manager's styling —
    // build the column set a second time to vary it.
    [Fact]
    public void CopiedColumns_KeepTheStylingOfTheManagerThatBuiltThem()
    {
        var source = BuildManager(GridValueStyles.All);

        var target = new ColumnManager<Row>() { ValueStyles = GridValueStyles.None };
        target.AddRange(source.Columns);

        Assert.Contains("content=\"positive\"", Render(target, 5m));
    }

    [Theory]
    [InlineData("negative", GridValueStyles.Negative, true)]
    [InlineData("negative", GridValueStyles.Positive, false)]
    [InlineData("positive", GridValueStyles.All, true)]
    [InlineData("zero", GridValueStyles.None, false)]
    [InlineData("unknown", GridValueStyles.None, true)]
    [InlineData("no-value", GridValueStyles.None, true)]
    [InlineData(null, GridValueStyles.None, true)]
    public void IsStyleEnabled_GovernsOnlyTheBuiltInNames(string? style, GridValueStyles enabled, bool expected)
        => Assert.Equal(expected, CellStyleHelper.IsStyleEnabled(style, enabled));
}