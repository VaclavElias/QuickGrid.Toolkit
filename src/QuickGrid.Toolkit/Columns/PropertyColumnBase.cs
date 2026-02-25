namespace QuickGrid.Toolkit.Columns;

public abstract class PropertyColumnBase<TGridItem> : ColumnBase<TGridItem>
{
    [Parameter] public Expression<Func<TGridItem, object?>> Property { get; set; } = default!;

    private Expression<Func<TGridItem, object?>>? _lastAssignedProperty;
    protected Func<TGridItem, object?>? CellValueFunc { get; private set; }

    protected override void OnParametersSet()
    {
        if (_lastAssignedProperty != Property)
        {
            _lastAssignedProperty = Property;
            CellValueFunc = Property.Compile();
            OnNewPropertySet();
        }

        Title ??= ExpressionHelper.GetPropertyName(Property);
    }

    protected virtual void OnNewPropertySet() { }
}