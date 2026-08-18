namespace QuickGrid.Samples.Extensions;

public static class ColumnManagerExtensions
{
    public static void AddCountry<TGridItem>(this ColumnManager<TGridItem> columnManager)
    => columnManager.Add(new DynamicColumn<TGridItem>
    {
        Title = "Country",
        FullTitle = "Country",
        Property = s => s == null ? string.Empty : ((ICountryDto)s).Country
    });
}