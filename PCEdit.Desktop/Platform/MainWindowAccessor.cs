using Avalonia.Controls;

namespace PCEdit.Desktop.Platform;

/// <summary>Holds the app's main window so platform services can reach a <c>TopLevel</c>.</summary>
public sealed class MainWindowAccessor
{
    public Window? Window { get; set; }

    /// <summary>The open modal nearest the user, or the main window when none is open.
    /// Follow ownership even when a native picker has temporarily taken activation away.</summary>
    public Window ActiveWindow() => Innermost(Require());

    private static Window Innermost(Window owner)
    {
        var children = owner.OwnedWindows.Where(window => window.IsVisible).ToList();
        var child = children.FirstOrDefault(window => window.IsActive) ?? children.LastOrDefault();
        return child is null ? owner : Innermost(child);
    }

    public Window Require() =>
        Window ?? throw new InvalidOperationException("The main window is not available yet.");
}
