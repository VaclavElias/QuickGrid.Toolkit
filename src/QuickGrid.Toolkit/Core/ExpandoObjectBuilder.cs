using System.Dynamic;

namespace QuickGrid.Toolkit.Core;

public static class ExpandoObjectBuilder<TGridItem>
{
    /// <summary>
    /// Creates an ExpandoObject with properties extracted from an item based on the provided column names.
    /// </summary>
    /// <param name="item">The item to extract properties from.</param>
    /// <param name="columnNames">List of property names to extract.</param>
    /// <returns>An ExpandoObject with the extracted properties, or null if the item is null.</returns>
    public static IDictionary<string, object?>? Create(TGridItem? item, List<string> columnNames)
    {
        if (item is null) return null;

        var obj = new ExpandoObject() as IDictionary<string, object?>;

        foreach (var column in columnNames)
        {
            if (column is null) continue;

            if (IsNestedProperty(column))
            {
                ExtractNestedProperty(obj, item, column);
            }
            else
            {
                ExtractSimpleProperty(obj, item, column);
            }
        }

        return obj;
    }

    /// <summary>
    /// Checks if a property name represents a nested property (contains a dot).
    /// </summary>
    private static bool IsNestedProperty(string propertyName) => propertyName.Contains('.');

    /// <summary>
    /// Walks a dotted property path (for example <c>Client.Address.City</c>) and adds the value it resolves to.
    /// Nothing is added when any step in the path is missing or null. Any depth is supported.
    /// </summary>
    private static void ExtractNestedProperty(IDictionary<string, object?> dict, TGridItem item, string propertyPath)
    {
        var propertyParts = propertyPath.Split('.');

        if (propertyParts.Length < 2) return;

        object? current = item;

        foreach (var part in propertyParts)
        {
            if (current is null) return;

            var property = current.GetType().GetProperty(part);

            if (property is null) return;

            current = property.GetValue(current);
        }

        dict[propertyPath] = current;
    }

    /// <summary>
    /// Extracts a simple property value from an item and adds it to the provided dictionary.
    /// </summary>
    private static void ExtractSimpleProperty(IDictionary<string, object?> dict, TGridItem item, string propertyName)
    {
        var property = item!.GetType().GetProperty(propertyName);

        if (property is null) return;

        dict[propertyName] = property.GetValue(item);
    }
}