namespace PCEdit.App.Core.Services;

/// <summary>
/// Speaks a short message through the platform screen reader so that status and
/// validation feedback (which is otherwise only a colour change on an inline
/// <c>Label</c>) reaches assistive-technology users. WCAG 2.1 SC 4.1.3.
/// </summary>
public interface IScreenReaderAnnouncer
{
    void Announce(string? message);
}
