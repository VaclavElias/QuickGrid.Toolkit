namespace QuickGrid.Toolkit.Core;

/// <summary>
/// One parsed unit of a search query: the text to look for, and whether finding it rules the item out.
/// </summary>
/// <param name="Text">The text to match, with any exclusion prefix already stripped.</param>
/// <param name="IsExcluded">Whether an item matching <paramref name="Text"/> is rejected rather than kept.</param>
/// <remarks>
/// A struct, and passed as an array built once per search: the alternative - re-reading the <c>-</c> prefix inside
/// the per-row predicate - repeats that parse for every item in the grid.
/// </remarks>
internal readonly record struct SearchTerm(string Text, bool IsExcluded);