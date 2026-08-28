namespace PCEdit.App.Core.Localization;

/// <summary>
/// Process-wide handle to the active <see cref="ILocalizer"/> for XAML markup extensions, which
/// cannot use constructor injection. Set once during app start-up (DI owns the real instance).
/// </summary>
public static class Loc
{
    /// <summary>The active localizer. Defaults to a standalone instance for design-time / early XAML.</summary>
    public static ILocalizer Instance { get; set; } = new Localizer();
}
