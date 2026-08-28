using Avalonia.Controls;

namespace PCEdit.Desktop.Platform;

/// <summary>Holds the app's main window so platform services can reach a <c>TopLevel</c>.</summary>
public sealed class MainWindowAccessor
{
    public Window? Window { get; set; }

    public Window Require() =>
        Window ?? throw new InvalidOperationException("The main window is not available yet.");
}
