// Keep these attributes in the assembly in every build configuration.
#define CODE_ANALYSIS
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("NDepend", "ND1207", Target = "QuickGrid.Toolkit.Columns.TogglePropertyColumn<TGridItem>",
    Justification = "Public Blazor component instantiated by consuming applications; sample usage does not determine whether it is needed.")]

[assembly: SuppressMessage("NDepend", "ND1208", Target = "QuickGrid.Toolkit.QuickGridWrapper<TGridItem>.QuickSearchAction(TGridItem,String,QuickSearchOptions)",
    Justification = "Retain the public instance method for source and binary compatibility with existing consumers.")]

[assembly: SuppressMessage("NDepend", "ND1311", Target = "QuickGrid.Toolkit.QuickGridWrapper<TGridItem>.ResolveSearchOptions()",
    Justification = "Compatibility adapter must honour the obsolete IsNestedSearch parameter until its planned removal in v2.")]