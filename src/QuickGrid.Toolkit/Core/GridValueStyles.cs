namespace QuickGrid.Toolkit.Core;

/// <summary>
/// Which of the built-in value natures a styled number column marks up.
/// </summary>
/// <remarks>
/// <para>
/// <c>AddStyledNumber</c> wraps its value in <c>&lt;span content="positive|negative|zero"&gt;</c> so CSS can colour
/// it. Not every grid wants all three: a report that highlights overspend typically wants negatives red and zeros
/// greyed out as noise, while a green positive adds nothing. Clearing a flag stops the marker being emitted at
/// all, so the value renders as plain text and no stylesheet has to unstyle it afterwards.
/// </para>
/// <para>
/// Set it on the <see cref="ColumnManager{TGridItem}"/> that builds the columns. Names produced by a
/// <see cref="CellStyleMap{TValue}"/> are the application's own vocabulary and are never suppressed — unless the
/// map deliberately produces one of these three names, which is treated the same as the built-in nature.
/// </para>
/// </remarks>
[Flags]
public enum GridValueStyles
{
    /// <summary>No value is marked up; styled numbers render as plain formatted text.</summary>
    None = 0,

    /// <summary>Values below zero are marked <c>negative</c>.</summary>
    Negative = 1,

    /// <summary>Values above zero are marked <c>positive</c>.</summary>
    Positive = 2,

    /// <summary>Zero is marked <c>zero</c>.</summary>
    Zero = 4,

    /// <summary>The default: every nature is marked up.</summary>
    All = Negative | Positive | Zero
}
