namespace QuickGrid.Samples.Core;

/// <summary>
/// One entry in the example progression.
/// </summary>
/// <param name="Route">Route of the page, without a leading slash.</param>
/// <param name="Title">Title shown in the nav, on the home page and as the page heading.</param>
/// <param name="Summary">One-line description shown on the home page.</param>
/// <param name="SourceFile">File name under <c>Pages/Examples/</c>, used to build the GitHub source link.</param>
public record Example(string Route, string Title, string Summary, string SourceFile);
