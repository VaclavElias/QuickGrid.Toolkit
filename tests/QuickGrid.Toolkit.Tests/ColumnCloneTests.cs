using System.Reflection;

namespace QuickGrid.Toolkit.Tests;

public class ColumnCloneTests
{
    private sealed class Sale
    {
        public string Region { get; set; } = "";
        public decimal Amount { get; set; }
        public bool Shipped { get; set; }
    }

    private static ColumnManager<Sale> BuildSharedColumns()
    {
        var shared = new ColumnManager<Sale>();

        shared.AddSimple(s => s.Region, fullTitle: "Sales region", @class: "text-start");
        shared.AddStyledNumber<decimal>(s => s.Amount, "Amt", "Total amount", format: "N2", calculateTotal: true);
        shared.AddTickColumn(s => s.Shipped, "Shipped", trueClass: "yes", falseClass: "no", showOnlyTrue: true);

        return shared;
    }

    [Fact]
    public void Clone_ProducesAnIndependentColumn()
    {
        var manager = new ColumnManager<Sale>();
        manager.AddSimple(s => s.Region);
        var original = manager.Columns[0];

        var clone = original.Clone();
        clone.Visible = false;
        clone.Title = "Changed";

        Assert.True(original.Visible);
        Assert.Equal("Region", original.Title);
    }

    // MemberwiseClone copies every field, so this passes for free today. It exists to fail loudly if the clone is
    // ever rewritten as a hand-maintained property list, which is how it lost eight properties the first time.
    [Fact]
    public void Clone_CopiesEveryPublicProperty()
    {
        var manager = new ColumnManager<Sale>();
        manager.AddStyledNumber<decimal>(s => s.Amount, "Amt", "Total amount", format: "N2",
            @class: "text-end", align: Align.Right, visible: false, calculateTotal: true, propertyName: "AmountExport");
        var original = manager.Columns[0];
        original.OnActionAsync = _ => Task.CompletedTask;

        var clone = original.Clone();

        foreach (var property in typeof(DynamicColumn<Sale>).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Assert.Equal(property.GetValue(original), property.GetValue(clone));
        }
    }

    // The renderer dispatches on the runtime type (col is TickPropertyColumn<T>), so a clone that loses the subtype
    // silently renders a tick column as a plain property column.
    [Fact]
    public void Clone_PreservesTheColumnSubtypeAndItsState()
    {
        var shared = BuildSharedColumns();
        var tick = Assert.IsType<TickPropertyColumn<Sale>>(shared.Columns[2]);

        var clone = Assert.IsType<TickPropertyColumn<Sale>>(tick.Clone());

        Assert.Equal("yes", clone.TrueClass);
        Assert.Equal("no", clone.FalseClass);
        Assert.True(clone.ShowOnlyTrue);
    }

    [Fact]
    public void AddRange_DoesNotRenumberTheSourceColumns()
    {
        var shared = BuildSharedColumns();
        var target = new ColumnManager<Sale>();
        target.AddIndexColumn();

        target.AddRange(shared.Columns);

        Assert.Equal([1, 2, 3], shared.Columns.Select(c => c.Id));
        Assert.Equal([1, 2, 3, 4], target.Columns.Select(c => c.Id));
    }

    // The point of the fix: one shared column set can feed several grids without them sharing visibility state.
    [Fact]
    public void AddRange_GivesEachManagerIndependentColumns()
    {
        var shared = BuildSharedColumns();
        var first = new ColumnManager<Sale>();
        var second = new ColumnManager<Sale>();

        first.AddRange(shared.Columns);
        second.AddRange(shared.Columns);

        first.Columns[0].Visible = false;

        Assert.True(second.Columns[0].Visible);
        Assert.True(shared.Columns[0].Visible);
    }

    [Fact]
    public void AddRange_CarriesTheFullColumnState()
    {
        var shared = BuildSharedColumns();
        var target = new ColumnManager<Sale>();

        target.AddRange(shared.Columns);

        var amount = target.Columns[1];
        Assert.Equal("Amt", amount.Title);
        Assert.Equal("Total amount", amount.FullTitle);
        Assert.Equal("N2", amount.Format);
        Assert.Equal(Align.Right, amount.Align);
        Assert.Equal("Amount", amount.PropertyName);
        Assert.True(amount.IsNumeric);
        Assert.True(amount.CalculateTotal);
        Assert.NotNull(amount.SortBy);
        Assert.Same(shared.Columns[1].ChildContent, amount.ChildContent);

        Assert.Equal("text-start", target.Columns[0].Class);
        Assert.IsType<TickPropertyColumn<Sale>>(target.Columns[2]);
    }

    [Fact]
    public void SimpleClone_ReturnsIndependentColumns()
    {
        var shared = BuildSharedColumns();

        var copies = shared.SimpleClone();
        copies.ForEach(c => c.Visible = false);

        Assert.All(shared.Columns, c => Assert.True(c.Visible));
        Assert.Equal(shared.Columns.Count, copies.Count);
        Assert.Equal(shared.Columns.Select(c => c.Id), copies.Select(c => c.Id));
        Assert.IsType<TickPropertyColumn<Sale>>(copies[2]);
    }
}
