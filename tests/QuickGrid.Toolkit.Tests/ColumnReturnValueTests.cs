namespace QuickGrid.Toolkit.Tests;

/// <summary>
/// Every <c>Add*</c> hands back the column it created (D1), so a caller can configure it or refer to it from a
/// footer without looking it up by title afterwards. These pin that the returned instance really is the one the
/// manager rendered — a copy would look correct in the debugger and silently do nothing.
/// </summary>
public class ColumnReturnValueTests
{
    private sealed class Sale
    {
        public int Id { get; set; }
        public string Region { get; set; } = "";
        public decimal Amount { get; set; }
        public int Units { get; set; }
        public double Ratio { get; set; }
        public bool Shipped { get; set; }
        public string? ImageUrl { get; set; }
    }

    private static ColumnManager<Sale> NewManager() => new();

    [Fact]
    public void EveryHelper_ReturnsTheInstanceItAppended()
    {
        var columns = NewManager();

        // One manager, every helper, in declaration order — so this also pins that nothing adds a hidden column.
        var added = new List<DynamicColumn<Sale>>
        {
            columns.AddIndexColumn(),
            columns.AddSimple(s => s.Region, "Region"),
            columns.AddSimpleDate(s => s.Id, "Date"),
            columns.Add(s => s.Region, "Raw"),
            columns.AddNumber(s => s.Amount, "Amount"),
            columns.AddNumber(s => s.Units, "Units"),
            columns.AddNumber(s => s.Ratio, "Ratio"),
            columns.AddStyledNumber<decimal>(s => s.Amount, "Styled"),
            columns.AddTickColumn(s => s.Shipped, "Shipped"),
            columns.AddToggleColumn(s => s.Shipped, "Toggle"),
            columns.AddImageColumn(s => s.ImageUrl, "Image"),
            columns.AddTemplateColumn(_ => _ => { }, "Template"),
            columns.AddAction(s => s.Region, "Action"),
            columns.AddAction("Open", "Static"),
            columns.AddMarkup(s => s.Region, "Markup")
        };

        Assert.Equal(columns.Columns.Count, added.Count);
        Assert.Equal(columns.Columns, added);
    }

    [Fact]
    public void TheReturnedColumn_CarriesTheIdTheFooterMatchesOn()
    {
        var columns = NewManager();

        columns.AddIndexColumn();
        var region = columns.AddSimple(s => s.Region, "Region");
        var amount = columns.AddNumber(s => s.Amount, "Amount");

        // Ids are 1-based and assigned in declaration order; footers address columns by them.
        Assert.Equal(2, region.Id);
        Assert.Equal(3, amount.Id);
    }

    [Fact]
    public void MutatingTheReturnedColumn_ChangesWhatTheManagerRenders()
    {
        var columns = NewManager();

        var region = columns.AddSimple(s => s.Region, "Region");
        region.Visible = false;

        Assert.False(columns.Columns[0].Visible);
        Assert.Empty(columns.Get());
    }

    [Fact]
    public void ColumnInfoOverloads_ReturnTheColumnToo()
    {
        var columns = NewManager();
        var info = new ColumnInfo("Amt", "Total amount", "text-end");

        var simple = columns.AddSimple(s => s.Region, info);
        var styled = columns.AddStyledNumber<decimal>(s => s.Amount, info);
        var action = columns.AddAction(s => s.Region, info);

        Assert.Equal([simple, styled, action], columns.Columns);
        Assert.All(columns.Columns, column => Assert.Equal("text-end", column.Class));
    }

    [Fact]
    public void TickColumn_ReturnsItsOwnSubtype_NotADowngradedBaseColumn()
    {
        var columns = NewManager();

        var tick = columns.AddTickColumn(s => s.Shipped, "Shipped", trueClass: "yes", showOnlyTrue: true);

        // The renderer dispatches on this subtype; returning the base type would compile and render wrongly.
        var tickColumn = Assert.IsType<TickPropertyColumn<Sale>>(tick);
        Assert.Equal("yes", tickColumn.TrueClass);
        Assert.True(tickColumn.ShowOnlyTrue);
    }

    [Fact]
    public void AddingARawColumn_ReturnsIt_AndNullStaysNull()
    {
        var columns = NewManager();

        var added = columns.Add(new DynamicColumn<Sale> { Property = s => s.Region, Title = "Region" });

        Assert.NotNull(added);
        Assert.Same(columns.Columns[0], added);
        Assert.Null(columns.Add(null));
        Assert.Single(columns.Columns);
    }

    [Fact]
    public void TheReturnedColumn_ComposesWithAddFooterColumnWithSum()
    {
        var columns = NewManager();
        columns.AddIndexColumn();

        // The composition D1 exists to enable: declare and total in one expression.
        columns.AddFooterColumnWithSum(columns.AddNumber(s => s.Amount, "Amount"));

        var footer = Assert.Single(columns.FooterColumns);
        Assert.Equal(2, footer.Id);
        Assert.Contains("400", footer.StringContent!([new Sale { Amount = 250 }, new Sale { Amount = 150 }]));
    }
}
