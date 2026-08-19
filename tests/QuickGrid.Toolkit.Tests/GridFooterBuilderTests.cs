namespace QuickGrid.Toolkit.Tests;

/// <summary>
/// The footer is generated as raw HTML and pushed into the table through <c>tfoot.innerHTML</c>, so cell-for-cell
/// alignment against the visible columns is the whole correctness story: one missing or extra <c>&lt;td&gt;</c>
/// shifts every total under the wrong heading. These tests exist because that pipeline was previously reachable
/// only by rendering the component.
/// </summary>
public class GridFooterBuilderTests
{
    private sealed class Sale
    {
        public string Region { get; set; } = "";
        public decimal Amount { get; set; }
        public int Units { get; set; }
    }

    private static readonly List<Sale> _sales =
    [
        new() { Region = "North", Amount = 100.5m, Units = 2 },
        new() { Region = "South", Amount = 200.25m, Units = 3 },
    ];

    private static ColumnManager<Sale> BuildColumns()
    {
        var manager = new ColumnManager<Sale>();

        manager.AddSimple(s => s.Region);
        manager.AddStyledNumber<decimal>(s => s.Amount, "Amount", format: "N2");
        manager.AddNumber(s => s.Units, "Units");

        return manager;
    }

    private static int CountCells(string footer) => footer.Split("<td").Length - 1;

    [Fact]
    public void HasFooter_IsFalse_WithNeitherDeclaredCellsNorTotals()
        => Assert.False(GridFooterBuilder<Sale>.HasFooter(BuildColumns(), new TotalFooter()));

    [Fact]
    public void HasFooter_IsTrue_WhenTotalsAreEnabled()
        => Assert.True(GridFooterBuilder<Sale>.HasFooter(BuildColumns(), new TotalFooter { IsTotalFooter = true }));

    [Fact]
    public void Build_TotalsNumericColumns_AndLabelsTheFirstNonNumericOne()
    {
        var footer = GridFooterBuilder<Sale>.Build(BuildColumns(), new TotalFooter { IsTotalFooter = true }, _sales);

        // Region carries the label, Amount uses its own "N2" format, Units falls back to the default "N0".
        Assert.Equal("<tr class=\"table-warning fw-bold\"><td>Total</td><td>300.75</td><td>5</td></tr>", footer);
    }

    [Fact]
    public void Build_PlacesTheLabelOnTheNominatedColumn()
    {
        var columns = BuildColumns();
        var totals = new TotalFooter { IsTotalFooter = true, TotalFooterLabel = "Sum", TotalFooterLabelColumnId = 3 };

        var footer = GridFooterBuilder<Sale>.Build(columns, totals, _sales);

        Assert.Equal("<tr class=\"table-warning fw-bold\"><td></td><td>300.75</td><td>Sum</td></tr>", footer);
    }

    [Fact]
    public void Build_EmitsOneCellPerRenderedColumn_SkippingHiddenOnes()
    {
        var columns = BuildColumns();
        columns.Columns[1].Visible = false;      // hidden by the column selector
        columns.Columns[2].Class = "d-none";     // laid out as hidden by CSS

        var footer = GridFooterBuilder<Sale>.Build(columns, new TotalFooter { IsTotalFooter = true }, _sales);

        Assert.Equal(1, CountCells(footer));
    }

    [Fact]
    public void Build_RespectsCalculateTotal_OverTheNumericDefault()
    {
        var columns = BuildColumns();
        columns.Columns[1].CalculateTotal = false;  // numeric, but opted out
        columns.Columns[0].CalculateTotal = true;   // non-numeric, but opted in — and it is the label column

        var footer = GridFooterBuilder<Sale>.Build(columns, new TotalFooter { IsTotalFooter = true }, _sales);

        Assert.Equal("<tr class=\"table-warning fw-bold\"><td>Total</td><td></td><td>5</td></tr>", footer);
    }

    [Fact]
    public void Build_StripsRemoveClassFromTheTotalCell()
    {
        var columns = new ColumnManager<Sale>();
        columns.AddSimple(s => s.Region);
        columns.AddStyledNumber<decimal>(s => s.Amount, "Amount", format: "N2", @class: "text-end action");

        var totals = new TotalFooter { IsTotalFooter = true, RemoveClass = "action" };

        var footer = GridFooterBuilder<Sale>.Build(columns, totals, _sales);

        Assert.Contains("class=\"text-end \"", footer);
    }

    [Fact]
    public void Build_UsesDeclaredFooterCells_InPreferenceToTotals()
    {
        var columns = BuildColumns();
        columns.AddFooterColumn(2, "fixed");

        var footer = GridFooterBuilder<Sale>.Build(columns, new TotalFooter { IsTotalFooter = true }, _sales);

        // One cell per rendered column, with the declared cell under its own column and blanks elsewhere.
        Assert.Equal("<tr class=\"table-warning fw-bold\"><td></td><td>fixed</td><td></td></tr>", footer);
    }

    [Fact]
    public void Build_TotalsOnlyTheRowsItIsGiven()
    {
        var columns = BuildColumns();
        var searchResult = _sales.Take(1).ToList();

        var footer = GridFooterBuilder<Sale>.Build(columns, new TotalFooter { IsTotalFooter = true }, searchResult);

        Assert.Contains("<td>100.50</td>", footer);
    }

    [Fact]
    public void Build_HandlesAnEmptyResult()
    {
        var footer = GridFooterBuilder<Sale>.Build(BuildColumns(), new TotalFooter { IsTotalFooter = true }, []);

        Assert.Equal("<tr class=\"table-warning fw-bold\"><td>Total</td><td>0.00</td><td>0</td></tr>", footer);
    }
}
