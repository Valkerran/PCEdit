using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;
using PCEdit.App.Core.ViewModels;

namespace PCEdit.App.Core.Tests.ViewModels;

public sealed class LogisticsEditorViewModelTests
{
    private const string Path = @"C:\fake\save.txt";

    private static (LogisticsEditorViewModel Vm, SaveFileWorkspace Workspace, FakeNavigationService Nav) Create()
    {
        var store = new FakeSaveFileStore();
        var save = WorkspaceFixtures.Create();
        var index = save.Inventories.FindIndex(i => i.Id == 30);
        save.Inventories[index] = save.Inventories[index] with
        {
            DemandGroups = "Iron,Cobalt",
            SupplyGroups = "",
            Priority = 1
        };
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer);
        workspace.Load(Path);
        var nav = new FakeNavigationService();
        var vm = new LogisticsEditorViewModel(
            new InventoryEditor(workspace, new ItemCatalog(), localizer),
            new LogisticsGroupCatalog(),
            new FakeScreenReaderAnnouncer(),
            nav,
            localizer);
        return (vm, workspace, nav);
    }

    [Fact]
    public void Initialize_LoadsCurrentGroupsAndPriority()
    {
        var (vm, _, _) = Create();

        vm.Initialize(30);

        Assert.Equal(["Iron", "Cobalt"], vm.DemandGroups.Select(g => g.Id));
        Assert.Empty(vm.SupplyGroups);
        Assert.Equal(LogisticsPriority.High, vm.SelectedPriority);
    }

    [Fact]
    public void PriorityChoices_AreTheSevenGameLevels_LowestFirst_WithFriendlyNames()
    {
        var (vm, _, _) = Create();

        Assert.Equal(
            [LogisticsPriority.Lowest, LogisticsPriority.VeryLow, LogisticsPriority.Low, LogisticsPriority.Normal,
             LogisticsPriority.High, LogisticsPriority.VeryHigh, LogisticsPriority.Highest],
            vm.PriorityChoices.Select(c => c.Value));
        Assert.Equal("Lowest", vm.PriorityChoices[0].DisplayName);
        Assert.Equal("Normal", vm.PriorityChoices[3].DisplayName);
        Assert.Equal("Highest", vm.PriorityChoices[^1].DisplayName);
    }

    [Fact]
    public void AddDemandGroup_ByTypedCustomId_AddsItFlaggedUnknown()
    {
        var (vm, _, _) = Create();
        vm.Initialize(30);

        vm.DemandGroupText = "SomeModId";
        vm.AddDemandGroupCommand.Execute(null);

        var added = vm.DemandGroups.Single(g => g.Id == "SomeModId");
        Assert.False(added.IsKnown);
        Assert.Empty(vm.DemandGroupText);
    }

    [Fact]
    public void AddDemandGroup_Duplicate_IsIgnored()
    {
        var (vm, _, _) = Create();
        vm.Initialize(30);

        vm.DemandGroupText = "Iron";
        vm.AddDemandGroupCommand.Execute(null);

        Assert.Single(vm.DemandGroups, g => g.Id == "Iron");
    }

    [Fact]
    public async Task Apply_WritesGroupsAndPriority_AndClosesModal()
    {
        var (vm, workspace, nav) = Create();
        vm.Initialize(30);
        vm.RemoveDemandGroupCommand.Execute(vm.DemandGroups.First(g => g.Id == "Cobalt"));
        vm.SupplyGroupText = "Magnesium";
        vm.AddSupplyGroupCommand.Execute(null);
        vm.SelectedPriority = LogisticsPriority.Highest;

        await vm.ApplyCommand.ExecuteAsync(null);

        var inv = workspace.Current!.Inventories.Single(i => i.Id == 30);
        Assert.Equal("Iron", inv.DemandGroups);
        Assert.Equal("Magnesium", inv.SupplyGroups);
        Assert.Equal(3, inv.Priority);
        Assert.Equal(1, nav.CloseModalCount);
    }

    [Fact]
    public async Task Apply_ANegativePriorityLevel_IsAcceptedAndWritten()
    {
        var (vm, workspace, nav) = Create();
        vm.Initialize(30);
        vm.SelectedPriority = LogisticsPriority.Lowest;

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.Equal(-3, workspace.Current!.Inventories.Single(i => i.Id == 30).Priority);
        Assert.Equal(1, nav.CloseModalCount);
    }
}
