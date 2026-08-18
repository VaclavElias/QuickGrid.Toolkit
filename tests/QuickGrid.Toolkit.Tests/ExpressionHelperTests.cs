namespace QuickGrid.Toolkit.Tests;

public class ExpressionHelperTests
{
    private sealed class Owner
    {
        public string Name { get; set; } = "";
    }

    private sealed class Item
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public decimal Amount { get; set; }
        public decimal Other { get; set; }
        public Owner Owner { get; set; } = new();
        public List<int> Numbers { get; set; } = [];
#pragma warning disable CA1051 // a public field on purpose: GetPropertyName must reject it
        public string PublicField = "";
#pragma warning restore CA1051
    }

    // GetPropertyName

    [Fact]
    public void GetPropertyName_ReturnsName_ForSimpleProperty()
        => Assert.Equal("Name", ExpressionHelper.GetPropertyName((Item s) => s.Name));

    [Fact]
    public void GetPropertyName_ReturnsName_ForBoxedValueTypeProperty()
        => Assert.Equal("Age", ExpressionHelper.GetPropertyName((Item s) => (object?)s.Age));

    [Fact]
    public void GetPropertyName_ReturnsLeafName_ForNestedProperty()
        => Assert.Equal("Name", ExpressionHelper.GetPropertyName((Item s) => s.Owner.Name));

    [Fact]
    public void GetPropertyName_ReturnsNull_ForNullExpression()
        => Assert.Null(ExpressionHelper.GetPropertyName<Item, string>(null));

    [Fact]
    public void GetPropertyName_Throws_ForMethodCall()
        => Assert.Throws<ArgumentException>(() => ExpressionHelper.GetPropertyName((Item s) => s.Name.ToUpperInvariant()));

    [Fact]
    public void GetPropertyName_Throws_ForField()
        => Assert.Throws<ArgumentException>(() => ExpressionHelper.GetPropertyName((Item s) => s.PublicField));

    [Fact]
    public void GetPropertyName_Throws_ForComputedExpression()
        => Assert.Throws<ArgumentException>(() => ExpressionHelper.GetPropertyName((Item s) => (object?)(s.Amount + s.Other)));

    // GetSafePropertyName

    [Fact]
    public void GetSafePropertyName_ReturnsName_ForSimpleProperty()
        => Assert.Equal("Age", ExpressionHelper.GetSafePropertyName((Item s) => (object?)s.Age));

    [Fact]
    public void GetSafePropertyName_ReturnsLeafName_ForNestedProperty()
        => Assert.Equal("Name", ExpressionHelper.GetSafePropertyName((Item s) => s.Owner.Name));

    [Fact]
    public void GetSafePropertyName_ReturnsLeafName_ForCollectionCountAccess()
        => Assert.Equal("Count", ExpressionHelper.GetSafePropertyName((Item s) => (object?)s.Numbers.Count));

    [Fact]
    public void GetSafePropertyName_JoinsProperties_ForBinaryExpression()
        => Assert.Equal("Amount_Other", ExpressionHelper.GetSafePropertyName((Item s) => (object?)(s.Amount + s.Other)));

    [Fact]
    public void GetSafePropertyName_ExtractsProperty_FromMethodCall()
        => Assert.Equal("Name", ExpressionHelper.GetSafePropertyName((Item s) => (object?)s.Name.ToUpperInvariant()));

    [Fact]
    public void GetSafePropertyName_FallsBackToGeneratedName_WhenNoPropertyInvolved()
    {
        var name = ExpressionHelper.GetSafePropertyName((Item s) => (object?)"constant");

        Assert.NotNull(name);
        Assert.StartsWith("Expr_", name);
    }

    [Fact]
    public void GetSafePropertyName_ReturnsNull_ForNullExpression()
        => Assert.Null(ExpressionHelper.GetSafePropertyName<Item, string>(null));

    // ConvertToObjectExpression

    [Fact]
    public void ConvertToObjectExpression_BoxesValueType()
    {
        var converted = ExpressionHelper.ConvertToObjectExpression((Item s) => (int?)s.Age);

        var value = converted.Compile()(new Item { Age = 42 });

        Assert.Equal(42, value);
    }

    [Fact]
    public void ConvertToObjectExpression_PassesThroughReferenceType()
    {
        var converted = ExpressionHelper.ConvertToObjectExpression((Item s) => s.Name);

        var value = converted.Compile()(new Item { Name = "abc" });

        Assert.Equal("abc", value);
    }

    [Fact]
    public void ConvertToObjectExpression_ReturnsSameExpression_WhenAlreadyObject()
    {
        Expression<Func<Item, object?>> expression = s => s.Name;

        var converted = ExpressionHelper.ConvertToObjectExpression(expression);

        Assert.Same(expression, converted);
    }
}
