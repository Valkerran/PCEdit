using Avalonia;
using Avalonia.Headless;
using PCEdit.Desktop.Platform;
using PCEdit.Desktop.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApp))]

// The harness redirects process-wide state (AppPaths.RootOverride, App.Services), so nothing in
// this assembly may run beside it - a plain [Fact] otherwise would.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace PCEdit.Desktop.Tests;

/// <summary>Starts the real <see cref="App"/> with no display, under a private data root.</summary>
public class TestApp
{
    /// <summary>Everything the app would keep in the user's profile goes under here instead.</summary>
    public static string Root { get; } = Path.Combine(Path.GetTempPath(), $"pcedit-ui-tests-{Guid.NewGuid():N}");

    public static AppBuilder BuildAvaloniaApp()
    {
        // Before the app initialises: it opens its log file as its first act.
        AppPaths.RootOverride = Root;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DeleteRoot();
        return AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }

    /// <summary>Each test removes its own folder; this takes what is left: the run's log.</summary>
    private static void DeleteRoot()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The log is still open on Windows. A leftover temp folder is not worth a failure.
        }
    }
}
