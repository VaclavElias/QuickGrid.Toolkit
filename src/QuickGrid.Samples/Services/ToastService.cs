namespace QuickGrid.Samples.Services;

/// <summary>
/// Stand-in for whatever notification system an application already has. The toolkit never shows messages
/// itself; it raises <c>WarningRequested</c> and lets the host decide.
/// </summary>
public class ToastService
{
    private readonly List<Toast> _toasts = [];

    public IReadOnlyList<Toast> Toasts => _toasts;

    public event Action? Changed;

    public void ShowWarning(string message) => Show(message, "warning");

    public void ShowInfo(string message) => Show(message, "info");

    public void Clear()
    {
        _toasts.Clear();

        Changed?.Invoke();
    }

    private void Show(string message, string level)
    {
        _toasts.Insert(0, new Toast(message, level));

        if (_toasts.Count > 5)
        {
            _toasts.RemoveRange(5, _toasts.Count - 5);
        }

        Changed?.Invoke();
    }
}

public record Toast(string Message, string Level);
