using PCEdit.App.Core.Localization;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.App.Core.Services;

public static class ScreenReaderAnnouncerExtensions
{
    /// <summary>
    /// Announces a status message, saying "Error: " first when it is one. The one place that
    /// rule lives: an error must not depend on colour alone to be recognised as an error.
    /// </summary>
    public static void Announce(this IScreenReaderAnnouncer announcer, ILocalizer localizer, StatusKind kind, string message) =>
        announcer.Announce(kind == StatusKind.Error
            ? localizer.Format(LocKeys.Announce_ErrorPrefix, message)
            : message);
}
