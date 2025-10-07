namespace QuickGrid.Toolkit.Columns;

// TODO [Design Review]: Consider adding an 'Action' property to DynamicColumn<TGridItem> to support interactive columns (e.g., toggles or buttons).
/// <summary>
/// Represents a dynamic column that displays a toggleable boolean property for items of type <typeparamref name="TGridItem"/>.
/// </summary>
/// <remarks>
/// This column can be used in a grid to display and optionally filter items based on a boolean property.
/// </remarks>
public class TogglePropertyColumn<TGridItem> : DynamicColumn<TGridItem>
{
    /// <summary>
    /// Gets or sets a value indicating whether only items with the property set to <c>true</c> are shown.
    /// When set to <c>true</c>, the column will display only items where the associated property is <c>true</c>.
    /// When set to <c>false</c>, the column will be empty.
    /// </summary>
    public bool ShowOnlyTrue { get; set; }
}