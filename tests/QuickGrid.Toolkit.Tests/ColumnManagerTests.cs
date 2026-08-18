namespace QuickGrid.Toolkit.Tests;

public class ColumnManagerTests
{
    private sealed class Sale
    {
        public string Region { get; set; } = "";
        public decimal Amount { get; set; }
    }

    [Fact]
    public void Add_AssignsSequentialOneBasedIds()
    {
        var manager = new ColumnManager<Sale>();

        manager.AddSimple(s => s.Region);
        manager.AddSimple(s => s.Amount);

        Assert.Equal(1, manager.Columns[0].Id);
        Assert.Equal(2, manager.Columns[1].Id);
    }

    [Fact]
    public void AddSimple_DerivesTitleAndPropertyName_FromExpression()
    {
        var manager = new ColumnManager<Sale>();

        manager.AddSimple(s => s.Region);

        Assert.Equal("Region", manager.Columns[0].Title);
        Assert.Equal("Region", manager.Columns[0].PropertyName);
    }

    [Fact]
    public void Add_WithColumnInfo_ForwardsItsPropertyName()
    {
        // Regression: the ColumnInfo overload used to drop columnInfo.PropertyName.
        var manager = new ColumnManager<Sale>();
        var info = new ColumnInfo("Amt", "Total amount", "text-end", propertyName: "AmountExport");

        manager.Add(s => s.Amount, info);

        Assert.Equal("AmountExport", manager.Columns[0].PropertyName);
        Assert.Equal("Amt", manager.Columns[0].Title);
        Assert.Equal("Total amount", manager.Columns[0].FullTitle);
    }

    [Fact]
    public void Add_WithColumnInfo_ExplicitPropertyNameParameter_Wins()
    {
        var manager = new ColumnManager<Sale>();
        var info = new ColumnInfo("Amt", "Total amount", "text-end", propertyName: "FromInfo");

        manager.Add(s => s.Amount, info, propertyName: "Explicit");

        Assert.Equal("Explicit", manager.Columns[0].PropertyName);
    }

    [Fact]
    public void Get_ReturnsOnlyVisibleColumns()
    {
        var manager = new ColumnManager<Sale>();

        manager.AddSimple(s => s.Region);
        manager.AddSimple(s => s.Amount, visible: false);

        Assert.Single(manager.Get());
        Assert.Equal("Region", manager.Get().Single().Title);
    }
}

public class ColumnManagerFooterTests
{
    private sealed class Sale
    {
        public string Region { get; set; } = "";
        public decimal Amount { get; set; }
    }

    private static List<Sale> Sales() =>
    [
        new() { Region = "North", Amount = 1000.25m },
        new() { Region = "South", Amount = 2000.50m }
    ];

    [Fact]
    public void AddFooterColumn_FormatsFixedValue_WithInvariantCulture()
    {
        var manager = new ColumnManager<Sale>();

        manager.AddFooterColumn(1, 1234.5m, format: "N2", @class: "text-end");

        Assert.Equal("<td class=\"text-end\">1,234.50</td>", manager.FooterColumns[0].Content);
    }

    [Fact]
    public void AddFooterColumn_OmitsClassAttribute_WhenNoClassGiven()
    {
        var manager = new ColumnManager<Sale>();

        manager.AddFooterColumn(1, "Total");

        Assert.Equal("<td>Total</td>", manager.FooterColumns[0].Content);
    }

    [Fact]
    public void AddFooterColumn_RendersEmptyCell_ForNullValue()
    {
        var manager = new ColumnManager<Sale>();

        manager.AddFooterColumn(1, null);

        Assert.Equal("<td></td>", manager.FooterColumns[0].Content);
    }

    [Fact]
    public void AddFooterColumn_HtmlEncodesValueAndClass()
    {
        // The footer ends up in tfoot.innerHTML, so caller-supplied content must be encoded.
        var manager = new ColumnManager<Sale>();

        manager.AddFooterColumn(1, "<b>bold</b>", @class: "a\"b");

        Assert.Equal("<td class=\"a&quot;b\">&lt;b&gt;bold&lt;/b&gt;</td>", manager.FooterColumns[0].Content);
    }

    [Fact]
    public void AddFooterColumn_WithExpression_ComputesOverTheGivenRows()
    {
        var manager = new ColumnManager<Sale>();

        manager.AddFooterColumn(2, items => items.Sum(i => i.Amount), format: "N2");

        var content = manager.FooterColumns[0].StringContent!(Sales());

        Assert.Equal("<td>3,000.75</td>", content);
    }

    [Fact]
    public void AddFooterColumn_WithExpression_ReturnsNull_ForNullRows()
    {
        var manager = new ColumnManager<Sale>();

        manager.AddFooterColumn(2, items => items.Sum(i => i.Amount));

        Assert.Null(manager.FooterColumns[0].StringContent!(null!));
    }

    [Fact]
    public void AddFooterColumnWithSum_SumsTheColumnProperty()
    {
        var manager = new ColumnManager<Sale>();
        var column = new DynamicColumn<Sale> { Id = 3, Property = s => s.Amount };

        manager.AddFooterColumnWithSum(column);

        var content = manager.FooterColumns[0].StringContent!(Sales());

        Assert.Equal("<td>3,001</td>", content);
    }

    [Fact]
    public void AddFooterColumnWithSum_StripsRemoveClass_FromTheColumnClass()
    {
        var manager = new ColumnManager<Sale>();
        var column = new DynamicColumn<Sale> { Id = 3, Class = "text-end bg-info", Property = s => s.Amount };

        manager.AddFooterColumnWithSum(column, removeClass: "bg-info");

        var content = manager.FooterColumns[0].StringContent!(Sales());

        Assert.Equal("<td class=\"text-end\">3,001</td>", content);
    }

    [Fact]
    public void AddFooterColumnWithSum_Throws_WhenColumnHasNoProperty()
    {
        var manager = new ColumnManager<Sale>();
        var column = new DynamicColumn<Sale> { Id = 3, Title = "Region" };

        Assert.Throws<InvalidOperationException>(() => manager.AddFooterColumnWithSum(column));
    }
}
