using System.Globalization;

namespace QuickGrid.Toolkit.Builders;

/// <summary>
/// Provides methods to build different types of columns for QuickGrid.
/// </summary>
/// <typeparam name="TGridItem">The type of items in the grid.</typeparam>
public class ColumnBuilder<TGridItem>
{
    private const string MissingTitle = "Title n/a";
    private const string DefaultActionTitle = "Action";

    /// <summary>
    /// Builds a base column with common properties.
    /// </summary>
    public static DynamicColumn<TGridItem> BuildColumn<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        string? title,
        string? fullTitle = null,
        string? @class = null,
        Align align = Align.Left,
        GridSort<TGridItem>? sortBy = null,
        bool visible = true) => new()
        {
            Title = title ?? ExpressionHelper.GetPropertyName<TGridItem, TValue>(expression),
            SortBy = sortBy ?? GridSort<TGridItem>.ByAscending(expression),
            ColumnType = typeof(TemplateColumn<TGridItem>),
            Align = align,
            FullTitle = fullTitle,
            Class = @class,
            Visible = visible,
            Property = ExpressionHelper.ConvertToObjectExpression(expression)
        };

    /// <summary>
    /// Builds a simple column that displays formatted values.
    /// </summary>
    public DynamicColumn<TGridItem> BuildSimpleColumn<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        string? title = null,
        string? fullTitle = null,
        string? format = null,
        string? @class = null,
        Align align = Align.Left,
        CellStyleMap<TValue>? cellStyle = null,
        GridSort<TGridItem>? sortBy = null,
        bool visible = true,
        string? propertyName = null,
        bool? addToContent = null)
    {
        DynamicColumn<TGridItem> column = BuildColumn(expression, title, fullTitle, @class, align, sortBy, visible);

        var compiledExpression = expression.Compile();
        column.ChildContent = (item) => (builder) =>
        {
            if (item is null) return;

            var value = compiledExpression.Invoke(item);

            if (value is null)
            {
                builder.AddContent(0, string.Empty);
            }
            else
            {
                string displayValue = value is IFormattable formattableValue
                    ? formattableValue.ToString(format, CultureInfo.InvariantCulture)
                    : $"{value}";

                if (addToContent == true)
                {
                    builder.AddMarkupContent(0, $"<span content=\"{value}\">{displayValue}</span>");
                }
                else if (cellStyle != null)
                {
                    builder.AddMarkupContent(0, $"<span content=\"{cellStyle.GetStyle(value)}\">{displayValue}</span>");
                }
                else
                {
                    builder.AddContent(0, displayValue);
                }
            }
        };

        column.PropertyName = propertyName;

        return column;
    }

    /// <summary>
    /// Builds a numeric column (decimal, double, or int).
    /// </summary>
    public DynamicColumn<TGridItem> BuildNumberColumn<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        string? title = null,
        string? fullTitle = null,
        string format = "N0",
        string? @class = null,
        Align align = Align.Right,
        bool visible = true,
        string? propertyName = null) where TValue : struct, IFormattable
    {
        DynamicColumn<TGridItem> column = BuildColumn(expression, title, fullTitle, @class, align, visible: visible);

        var compiledExpression = expression.Compile();
        column.ChildContent = (item) => (builder) =>
        {
            if (item is null) return;

            var value = compiledExpression.Invoke(item);

            if (value.HasValue)
            {
                builder.AddContent(0, value.Value.ToString(format, CultureInfo.InvariantCulture));
            }
            else
            {
                builder.AddContent(0, string.Empty);
            }
        };

        column.IsNumeric = true;
        column.PropertyName = propertyName;

        return column;
    }

    /// <summary>
    /// Builds a styled numeric column with conditional styling.
    /// </summary>
    public DynamicColumn<TGridItem> BuildStyledNumberColumn<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        string? title = null,
        string? fullTitle = null,
        string format = "N0",
        string? @class = null,
        Align align = Align.Right,
        bool visible = true,
        CellStyleMap<TValue>? cellStyle = null,
        Func<TGridItem, Task>? onClick = null,
        string? propertyName = null) where TValue : struct, IFormattable
    {
        DynamicColumn<TGridItem> column = BuildColumn(expression, title, fullTitle, @class, align, visible: visible);

        var compiledExpression = expression.Compile();
        column.ChildContent = (item) => (builder) =>
        {
            if (item is null) return;

            var value = compiledExpression.Invoke(item);

            if (value.HasValue)
            {
                string formattedValue = value.Value.ToString(format, CultureInfo.InvariantCulture);
                string content = $"<span content=\"{CellStyleHelper.DetermineNumericValueNature(value.Value, cellStyle)}\">{formattedValue}</span>";

                if (onClick is null)
                {
                    builder.AddMarkupContent(0, content);
                }
                else
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => onClick.Invoke(item)));
                    builder.AddMarkupContent(2, content);
                    builder.CloseElement();
                }
            }
            else
            {
                var nullStyle = CellStyleHelper.GetValueStyle(default(TValue), cellStyle);
                if (!string.IsNullOrEmpty(nullStyle))
                {
                    builder.AddMarkupContent(0, $"<span content=\"{nullStyle}\"></span>");
                }
                else
                {
                    builder.AddContent(0, string.Empty);
                }
            }
        };

        column.IsNumeric = true;
        column.PropertyName = propertyName;

        return column;
    }

    /// <summary>
    /// Builds an action column that renders content with optional click handler.
    /// </summary>
    public DynamicColumn<TGridItem> BuildActionColumn(
        Expression<Func<TGridItem, object?>> expression,
        string? title = null,
        string? fullTitle = null,
        Align align = Align.Left,
        string? @class = null,
        GridSort<TGridItem>? sortBy = null,
        bool visible = true,
        Func<TGridItem, Task>? onClick = null,
        string? propertyName = null)
    {
        var compiledExpression = expression.Compile();

        return new()
        {
            Title = string.IsNullOrWhiteSpace(title) ? ExpressionHelper.GetPropertyName<TGridItem, object>(expression) : title,
            FullTitle = fullTitle,
            ChildContent = (item) => (builder) =>
            {
                var value = compiledExpression.Invoke(item);

                if (onClick is null)
                {
                    builder.AddContent(0, value);
                }
                else
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => onClick.Invoke(item)));
                    builder.AddContent(2, value);
                    builder.CloseElement();
                }
            },
            SortBy = sortBy ?? GridSort<TGridItem>.ByAscending(p => p == null ? default : compiledExpression.Invoke(p)),
            ColumnType = typeof(TemplateColumn<TGridItem>),
            Align = align,
            Class = @class,
            Visible = visible,
            PropertyName = propertyName
        };
    }

    /// <summary>
    /// Builds a static action column with content and optional click handler.
    /// Passing <paramref name="enabled"/> restricts rendering to rows where the predicate returns <c>true</c>.
    /// </summary>
    public DynamicColumn<TGridItem> BuildStaticActionColumn(
        string staticContent,
        string? title = null,
        Align align = Align.Left,
        string? @class = null,
        Func<TGridItem, Task>? onClick = null,
        Expression<Func<TGridItem, bool>>? enabled = null)
    {
        var compiledEnabled = enabled?.Compile();
        return new()
        {
            Title = title ?? DefaultActionTitle,
            ChildContent = (item) => (builder) =>
            {
                if ((compiledEnabled?.Invoke(item)) is false) return;

                if (onClick is null)
                {
                    builder.AddContent(0, staticContent);
                }
                else
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => onClick.Invoke(item)));
                    builder.AddContent(2, staticContent);
                    builder.CloseElement();
                }
            },
            ColumnType = typeof(TemplateColumn<TGridItem>),
            Align = align,
            Class = @class
        };
    }

    /// <summary>
    /// Builds a conditional action column with enabled/disabled logic.
    /// </summary>
    [Obsolete("Use BuildStaticActionColumn with an enabled predicate instead.")]
    public DynamicColumn<TGridItem> BuildConditionalActionColumn(
        string staticContent,
        string? title = null,
        Align align = Align.Left,
        string? @class = null,
        Action<TGridItem>? onClick = null,
        Expression<Func<TGridItem, bool>>? enabled = null)
    {
        var compiledEnabled = enabled?.Compile();
        return new()
        {
            Title = title ?? DefaultActionTitle,
            ChildContent = (item) => (builder) =>
            {
                if (compiledEnabled?.Invoke(item) != false)
                {
                    builder.OpenElement(0, "div");
                    if (onClick is not null)
                        builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => onClick.Invoke(item)));
                    builder.AddContent(2, staticContent);
                    builder.CloseElement();
                }
            },
            ColumnType = typeof(TemplateColumn<TGridItem>),
            Align = align,
            Class = @class
        };
    }

    /// <summary>
    /// Builds a markup column that renders raw HTML content from an expression, with an optional click handler.
    /// </summary>
    public DynamicColumn<TGridItem> BuildMarkupColumn<TValue>(
        Expression<Func<TGridItem, TValue?>> expression,
        string? title = null,
        string? fullTitle = null,
        string? @class = null,
        Align align = Align.Left,
        GridSort<TGridItem>? sortBy = null,
        bool visible = true,
        Func<TGridItem, Task>? onClick = null,
        string? propertyName = null)
    {
        DynamicColumn<TGridItem> column = BuildColumn(expression, title, fullTitle, @class, align, sortBy, visible);

        var compiledExpression = expression.Compile();

        column.ChildContent = (item) => (builder) =>
        {
            if (item is null) return;

            var value = compiledExpression.Invoke(item);
            var markup = value?.ToString();

            if (string.IsNullOrEmpty(markup)) return;

            if (onClick is null)
            {
                builder.AddMarkupContent(0, markup);
            }
            else
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => onClick.Invoke(item)));
                builder.AddMarkupContent(2, markup);
                builder.CloseElement();
            }
        };

        column.Class = onClick is not null ? (@class is null ? "action" : $"{@class} action") : @class;
        column.PropertyName = propertyName;

        return column;
    }
}