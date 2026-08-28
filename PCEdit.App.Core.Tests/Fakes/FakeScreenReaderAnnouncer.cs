using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Fakes;

/// <summary>No-op <see cref="IScreenReaderAnnouncer"/> that records what it was asked to announce.</summary>
internal sealed class FakeScreenReaderAnnouncer : IScreenReaderAnnouncer
{
    public List<string?> Announcements { get; } = [];

    public void Announce(string? message) => Announcements.Add(message);
}
