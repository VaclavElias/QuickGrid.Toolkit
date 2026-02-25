namespace QuickGrid.Toolkit.Helpers;

/// <summary>
/// Provides utility methods for working with expressions, particularly for extracting property names.
/// </summary>
public static class ExpressionHelper
{
    /// <summary>
    /// Extracts the property name from a simple property access expression.
    /// </summary>
    /// <typeparam name="TGridItem">The type of the grid item.</typeparam>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="expression">The expression to extract the property name from.</param>
    /// <returns>The property name, or null if the expression is null or invalid.</returns>
    /// <exception cref="ArgumentException">Thrown when the expression doesn't refer to a property.</exception>
    public static string? GetPropertyName<TGridItem, TValue>(Expression<Func<TGridItem, TValue?>>? expression)
    {
        if (expression is null) return null;

        MemberExpression? memberExpression;

        if (expression.Body is UnaryExpression unaryExpression)
            memberExpression = unaryExpression.Operand as MemberExpression;
        else
            memberExpression = expression.Body as MemberExpression;

        if (memberExpression is null)
            throw new ArgumentException($"Expression '{expression}' refers to a method, not a property.");

        if (memberExpression.Member is not PropertyInfo propertyInfo)
            throw new ArgumentException($"Expression '{expression}' refers to a field, not a property.");

        return propertyInfo.Name;
    }

    /// <summary>
    /// Extracts a safe property name from an expression, handling complex expressions by joining multiple property names or generating a hash-based name.
    /// </summary>
    /// <typeparam name="TGridItem">The type of the grid item.</typeparam>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="expression">The expression to extract the property name from.</param>
    /// <returns>A safe property name string.</returns>
    public static string? GetSafePropertyName<TGridItem, TValue>(Expression<Func<TGridItem, TValue?>>? expression)
    {
        if (expression is null) return null;

        Expression body = expression.Body;

        while (body is UnaryExpression unaryExpression)
            body = unaryExpression.Operand;

        if (body is MemberExpression memberExpression && memberExpression.Member is PropertyInfo propertyInfo)
            return propertyInfo.Name;

        // Handle more complex expressions by extracting property references
        var propertyVisitor = new PropertyReferenceVisitor();
        propertyVisitor.Visit(expression);

        if (propertyVisitor.PropertyNames.Count > 0)
        {
            return string.Join("_", propertyVisitor.PropertyNames);
        }

        // If we couldn't extract any property, generate a safe name based on the expression
        return $"Expr_{Math.Abs(expression.ToString().GetHashCode())}";
    }

    /// <summary>
    /// Converts an expression from TValue? to object? for use in column properties.
    /// </summary>
    /// <typeparam name="TGridItem">The type of the grid item.</typeparam>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="expression">The expression to convert.</param>
    /// <returns>An expression that returns object?.</returns>
    public static Expression<Func<TGridItem, object?>> ConvertToObjectExpression<TGridItem, TValue>(
        Expression<Func<TGridItem, TValue?>> expression)
    {
        // Check if the body's result type is already object to avoid unnecessary conversion
        if (typeof(TValue) == typeof(object))
        {
            return expression as Expression<Func<TGridItem, object?>>
                   ?? throw new InvalidOperationException("Failed to convert expression.");
        }

        // Prepare a conversion of the expression's body to object?
        var body = expression.Body;

        // Handle nullable value types by converting to object
        if (typeof(TValue).IsValueType)
        {
            body = Expression.Convert(body, typeof(object));
        }

        // Rebuild the lambda expression with the converted body
        var convertedExpression = Expression.Lambda<Func<TGridItem, object?>>(body, expression.Parameters);

        return convertedExpression;
    }

    // Helper class to extract property names from expressions
    private sealed class PropertyReferenceVisitor : ExpressionVisitor
    {
        public List<string> PropertyNames { get; } = [];

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member is PropertyInfo propertyInfo)
            {
                PropertyNames.Add(propertyInfo.Name);
            }

            return base.VisitMember(node);
        }
    }
}