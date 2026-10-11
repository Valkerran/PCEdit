using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class QuietTraceListenerTests
{
    /// <summary>A log on a disk that has filled up or gone away.</summary>
    private sealed class FullDisk : StringWriter
    {
        public override void Write(string? value) => throw new IOException("No space left on device");

        public override void Flush() => throw new IOException("No space left on device");
    }

    [Fact]
    public void WhatIsWritten_ReachesTheLog()
    {
        var log = new StringWriter();
        var listener = new QuietTraceListener(log, maxChars: 1000);

        listener.Write("Could not save: ");
        listener.WriteLine("disk full");

        Assert.Equal("Could not save: disk full" + Environment.NewLine, log.ToString());
    }

    [Fact]
    public void ALogThatCannotBeWritten_NeverThrowsIntoTheCaller()
    {
        // Diagnostics are written from inside catch blocks. A throwing log would replace the
        // "Save failed" the player was about to be shown with nothing at all.
        var listener = new QuietTraceListener(new FullDisk(), maxChars: 1000);

        listener.WriteLine("Could not save");
        listener.Write("more");
        listener.Flush();
    }

    [Fact]
    public void AfterTheLimit_NothingMoreIsWritten()
    {
        // The app keeps running after an unexpected error, so one that repeats - in a layout
        // pass, say - would otherwise grow the log without end.
        var log = new StringWriter();
        var listener = new QuietTraceListener(log, maxChars: 10);

        listener.WriteLine("0123456789");
        listener.WriteLine("this one is dropped");

        Assert.Equal("0123456789" + Environment.NewLine, log.ToString());
    }
}
