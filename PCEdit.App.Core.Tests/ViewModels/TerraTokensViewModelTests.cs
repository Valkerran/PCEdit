using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.App.Core.Tests.ViewModels;

public sealed class TerraTokensViewModelTests
{
    private const string Path = @"C:\fake\save.txt";

    [Theory]
    [InlineData("1,000")]
    [InlineData("1.5")]
    [InlineData("5-")]
    [InlineData("-5")]
    [InlineData("0")]
    [InlineData("")]
    public void Grant_AmountThatIsNotAPositiveWholeNumber_IsRefused(string amount)
    {
        var (vm, workspace, localizer) = Create();
        var before = workspace.Current!.Unlocks.TerraTokens;
        vm.Amount = amount;

        vm.GrantCommand.Execute(null);

        Assert.Equal(StatusKind.Error, vm.StatusKind);
        Assert.Equal(localizer[LocKeys.TerraTokens_InvalidAmount], vm.StatusMessage);
        Assert.Equal(before, workspace.Current!.Unlocks.TerraTokens);
        Assert.False(workspace.IsDirty);
    }

    [Fact]
    public void Grant_WholeNumberWithSpacesAroundIt_IsGranted()
    {
        var (vm, workspace, _) = Create();
        var before = workspace.Current!.Unlocks.TerraTokens;
        vm.Amount = " 12 ";

        vm.GrantCommand.Execute(null);

        Assert.Equal(StatusKind.Success, vm.StatusKind);
        Assert.Equal(before + 12, workspace.Current!.Unlocks.TerraTokens);
    }

    private static (TerraTokensViewModel Vm, SaveFileWorkspace Workspace, Localizer Localizer) Create()
    {
        var store = new FakeSaveFileStore();
        store.Seed(Path, WorkspaceFixtures.Create());
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer, new FakeSaveBackupService());
        workspace.Load(Path);
        var vm = new TerraTokensViewModel(workspace, new FakeScreenReaderAnnouncer(), localizer, new FakeNavigationService());
        vm.Load();
        return (vm, workspace, localizer);
    }

    [Fact]
    public void Relocalize_KeepsTheTypedAmount_AndClearsTheStatus()
    {
        var (vm, _, localizer) = Create();
        vm.Amount = "oops";
        vm.GrantCommand.Execute(null);
        vm.Amount = "250";

        localizer.SetCulture("de");
        vm.Relocalize();

        Assert.Equal("250", vm.Amount);
        Assert.Null(vm.StatusMessage);
    }
}
