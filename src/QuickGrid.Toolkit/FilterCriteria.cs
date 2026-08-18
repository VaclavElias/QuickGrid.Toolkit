namespace QuickGrid.Toolkit;

/// <summary>
/// Turns a search term into a filter expression, letting <see cref="QuickGridWrapper{TGridItem}"/> push the
/// search down to the data source instead of filtering in memory.
/// </summary>
/// <param name="expressionBuilder">Builds the predicate for a given search term.</param>
public class FilterCriteria<TGridItem>(Func<string, Expression<Func<TGridItem, bool>>> expressionBuilder)
{
    public string SearchTerm { get; set; } = null!;

    public Expression<Func<TGridItem, bool>> CreateExpression() => expressionBuilder(SearchTerm);
}