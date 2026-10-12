using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.App.Core.Tests.Services;

public sealed class ScreenReaderAnnouncerExtensionsTests
{
    [Fact]
    public void AnError_IsAnnouncedWithTheErrorPrefix()
    {
        var announcer = new FakeScreenReaderAnnouncer();

        announcer.Announce(new Localizer(), StatusKind.Error, "The amount is not a number.");

        Assert.Equal("Error: The amount is not a number.", Assert.Single(announcer.Announcements));
    }

    [Theory]
    [InlineData(StatusKind.Info)]
    [InlineData(StatusKind.Success)]
    public void AnythingElse_IsAnnouncedAsItIs(StatusKind kind)
    {
        var announcer = new FakeScreenReaderAnnouncer();

        announcer.Announce(new Localizer(), kind, "Granted 5 tokens.");

        Assert.Equal("Granted 5 tokens.", Assert.Single(announcer.Announcements));
    }
}
