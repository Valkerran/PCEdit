using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Services;

public sealed class SaveCopyNameTests
{
    private static readonly string Folder = Path.Combine("saves", "Planet Crafter");

    private static string In(string name) => Path.Combine(Folder, name);

    [Fact]
    public void ANumberedSlot_SuggestsTheNextFreeSlotOfTheSameMode()
    {
        // The game lists "<Mode>-<N>.json" slots; a copy in the next free one shows up in-game.
        var taken = new HashSet<string> { In("Standard-2.json"), In("Standard-3.json") };

        Assert.Equal(In("Standard-4.json"), SaveCopyName.Suggest(In("Standard-2.json"), taken.Contains));
    }

    [Fact]
    public void ANameWithoutASlotNumber_GetsARepairedSuffix()
    {
        Assert.Equal(In("Backup-repaired.json"), SaveCopyName.Suggest(In("Backup.json"), _ => false));
    }

    [Fact]
    public void ARepairedNameAlreadyTaken_IsNumbered()
    {
        var taken = new HashSet<string> { In("Backup-repaired.json") };

        Assert.Equal(In("Backup-repaired-2.json"), SaveCopyName.Suggest(In("Backup.json"), taken.Contains));
    }

    [Fact]
    public void TheSuggestion_IsNeverTheOriginal()
    {
        var original = In("Chill-1.json");

        Assert.NotEqual(original, SaveCopyName.Suggest(original, _ => false));
    }
}
