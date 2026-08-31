using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;
using PCEdit.SaveFileHandler;

namespace PCEdit.App.Core.Tests.Services;

public sealed class PlanetIndexTests
{
    private const string Path = @"C:\fake\save.txt";

    private static PlanetIndex Create()
    {
        var store = new FakeSaveFileStore();
        store.Seed(Path, WorkspaceFixtures.CreateMultiWorld());
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), new Localizer());
        workspace.Load(Path);
        return new PlanetIndex(workspace);
    }

    [Fact]
    public void KnownPlanetIds_IsTheOrderedDistinctUnionOfEveryPlanetIdInTheSave()
    {
        var index = Create();

        Assert.Equal(["Aqualis", "Prime"], index.KnownPlanetIds());
    }

    [Fact]
    public void KnownPlanetIds_IsEmptyWhenNoSaveIsLoaded()
    {
        var workspace = new SaveFileWorkspace(new FakeSaveFileStore(), new FakeScreenReaderAnnouncer(), new Localizer());

        Assert.Empty(new PlanetIndex(workspace).KnownPlanetIds());
    }

    [Fact]
    public void ResolvePlanetId_MapsAWorldObjectPlanetHashBackToItsPlanetId()
    {
        var index = Create();

        Assert.Equal("Aqualis", index.ResolvePlanetId(PlanetHash.Of("Aqualis")));
        Assert.Equal("Prime", index.ResolvePlanetId(PlanetHash.Of("Prime")));
    }

    [Fact]
    public void ResolvePlanetId_ReturnsNullForANullOrUnknownHash()
    {
        var index = Create();

        Assert.Null(index.ResolvePlanetId(null));
        Assert.Null(index.ResolvePlanetId(PlanetHash.Of("Selenea")));
    }
}
