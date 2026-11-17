using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using PCEdit.Desktop.Platform;

namespace PCEdit.Desktop.Tests;

/// <summary>The live region only speaks when its text changes, so a repeat has to be made a change (#103).</summary>
public sealed class ScreenReaderAnnouncerTests
{
    private static (AvaloniaScreenReaderAnnouncer Announcer, List<string> Spoken) Create()
    {
        var spoken = new List<string>();
        return (new AvaloniaScreenReaderAnnouncer { Sink = spoken.Add }, spoken);
    }

    [AvaloniaFact]
    public void ARepeatedMessage_IsClearedAndSetAgain()
    {
        var (announcer, spoken) = Create();

        announcer.Announce("Moved Iron to Chest.");
        announcer.Announce("Moved Iron to Chest.");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["Moved Iron to Chest.", "", "Moved Iron to Chest."], spoken);
    }

    [AvaloniaFact]
    public void ADifferentMessage_IsSetAtOnce()
    {
        var (announcer, spoken) = Create();

        announcer.Announce("Saved.");
        announcer.Announce("Moved Iron to Chest.");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["Saved.", "Moved Iron to Chest."], spoken);
    }

    [AvaloniaFact]
    public void ARepeatOvertakenByANewMessage_EndsOnTheNewMessage()
    {
        var (announcer, spoken) = Create();

        announcer.Announce("Saved.");
        announcer.Announce("Saved.");
        announcer.Announce("Moved Iron to Chest.");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Moved Iron to Chest.", spoken[^1]);
    }
}
