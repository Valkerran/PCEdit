using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.App.Core.Tests.ViewModels;

public sealed class OpenFileViewModelTests
{
    // A refused save must say where it is broken, or neither the player nor a bug report can
    // tell what to fix; the detail used to go only to a trace nobody listens to (issue #115).
    [Fact]
    public async Task PickAndLoad_MalformedSave_ShowsTheDetail()
    {
        var vm = Create(new InvalidDataException("Save-file section 4, record 2 is not valid: bad id"));

        await vm.PickAndLoadCommand.ExecuteAsync(null);

        Assert.Equal(StatusKind.Error, vm.StatusKind);
        Assert.Contains("section 4, record 2", vm.StatusMessage);
    }

    // Anything else - a locked file, a missing drive - keeps the generic text: its message can
    // carry paths and OS detail that are no use on screen.
    [Fact]
    public async Task PickAndLoad_OtherFailure_ShowsOnlyTheGenericMessage()
    {
        var vm = Create(new IOException("/home/someone/secret/path is locked"));

        await vm.PickAndLoadCommand.ExecuteAsync(null);

        Assert.Equal(new Localizer()[LocKeys.OpenFile_LoadFailed], vm.StatusMessage);
    }

    [Fact]
    public async Task WhenTheSaveIsClosed_TheLoadedMessageGoesToo()
    {
        // A failed repair can close the save and send the player back here; "Loaded ..." beside
        // "No file loaded" would contradict the error they were just shown.
        var store = new FakeSaveFileStore();
        store.Seed("/saves/Standard-1.json", Fixtures.WorkspaceFixtures.Create());
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        var vm = new OpenFileViewModel(
            workspace,
            new FakeFilePickerService { PickResult = "/saves/Standard-1.json" },
            new FakeScreenReaderAnnouncer(),
            new FakeNavigationService(),
            new FakeDialogService(),
            localizer);
        await vm.PickAndLoadCommand.ExecuteAsync(null);
        Assert.NotNull(vm.StatusMessage);

        workspace.Close();

        Assert.Null(vm.StatusMessage);
    }

    private static OpenFileViewModel Create(Exception loadFailure)
    {
        var store = new FakeSaveFileStore { LoadFailure = loadFailure };
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        return new OpenFileViewModel(
            workspace,
            new FakeFilePickerService { PickResult = "/saves/Standard-1.json" },
            new FakeScreenReaderAnnouncer(),
            new FakeNavigationService(),
            new FakeDialogService(),
            localizer);
    }
}
