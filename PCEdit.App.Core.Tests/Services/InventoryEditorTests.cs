using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;
using PCEdit.App.Core.Tests.Fakes;
using PCEdit.App.Core.Tests.Fixtures;

namespace PCEdit.App.Core.Tests.Services;

public sealed class InventoryEditorTests
{
    private const string Path = @"C:\fake\save.txt";

    private static (InventoryEditor Editor, SaveFileWorkspace Workspace) CreateLoadedEditor()
    {
        var store = new FakeSaveFileStore();
        store.Seed(Path, WorkspaceFixtures.Create());
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer);
        workspace.Load(Path);
        return (new InventoryEditor(workspace, new ItemCatalog(), localizer), workspace);
    }

    [Fact]
    public void BuildInventoryGroups_LabelsPlayerInventory()
    {
        var (editor, _) = CreateLoadedEditor();

        var groups = editor.BuildInventoryGroups();

        var aliceInventory = groups.Single(g => g.InventoryId == 10);
        Assert.Equal("Alice's Inventory", aliceInventory.Label);
        Assert.Equal(2, aliceInventory.Count);
        Assert.Equal("2/5", aliceInventory.CapacityLabel);
    }

    [Fact]
    public void BuildInventoryGroups_PopulatesLogisticsOnlyForContainersWithAPriorityKey()
    {
        var (editor, _) = CreateLogisticsEditor();

        var groups = editor.BuildInventoryGroups().ToDictionary(g => g.InventoryId);

        Assert.False(groups[10].IsLogisticsContainer);
        Assert.True(groups[30].IsLogisticsContainer);
        Assert.Equal(["Iron", "Cobalt"], groups[30].Logistics!.DemandGroupIds);
        Assert.Equal(["Magnesium"], groups[30].Logistics!.SupplyGroupIds);
        Assert.Equal(LogisticsPriority.VeryHigh, groups[30].Logistics!.Priority);
        Assert.Equal("Demand 2 · Supply 1 · Priority Very High", groups[30].LogisticsSummary);
    }

    [Fact]
    public void BuildInventoryGroups_ClampsAnOutOfRangeRawPriority()
    {
        var store = new FakeSaveFileStore();
        var save = WorkspaceFixtures.Create();
        var index = save.Inventories.FindIndex(i => i.Id == 30);
        save.Inventories[index] = save.Inventories[index] with { DemandGroups = "", SupplyGroups = "", Priority = -99 };
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer);
        workspace.Load(Path);
        var editor = new InventoryEditor(workspace, new ItemCatalog(), localizer);

        Assert.Equal(LogisticsPriority.Lowest, editor.BuildInventoryGroups().Single(g => g.InventoryId == 30).Logistics!.Priority);
    }

    [Fact]
    public void UpdateLogistics_ReplacesGroupsAndPriority_AndKeepsOtherInventoryFields()
    {
        var (editor, workspace) = CreateLogisticsEditor();

        editor.UpdateLogistics(30, ["Titanium", "Silicon"], [], LogisticsPriority.Lowest);

        var inv = workspace.Current!.Inventories.Single(i => i.Id == 30);
        Assert.Equal("Titanium,Silicon", inv.DemandGroups);
        Assert.Equal("", inv.SupplyGroups);
        Assert.Equal(-3, inv.Priority);
        Assert.Equal("202", inv.WorldObjectIds);
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void UpdateLogistics_OnAPlainInventory_Throws()
    {
        var (editor, _) = CreateLogisticsEditor();

        Assert.Throws<InvalidOperationException>(() => editor.UpdateLogistics(10, [], [], LogisticsPriority.Normal));
    }

    private static (InventoryEditor Editor, SaveFileWorkspace Workspace) CreateLogisticsEditor()
    {
        var store = new FakeSaveFileStore();
        var save = WorkspaceFixtures.Create();
        var index = save.Inventories.FindIndex(i => i.Id == 30);
        save.Inventories[index] = save.Inventories[index] with
        {
            DemandGroups = "Iron,Cobalt",
            SupplyGroups = "Magnesium",
            Priority = 2
        };
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer);
        workspace.Load(Path);
        return (new InventoryEditor(workspace, new ItemCatalog(), localizer), workspace);
    }

    [Fact]
    public void BuildInventoryGroups_ClassifiesKind()
    {
        var (editor, _) = CreateLoadedEditor();

        var groups = editor.BuildInventoryGroups().ToDictionary(g => g.InventoryId, g => g.Kind);

        Assert.Equal(InventoryKind.PlayerInventory, groups[10]);
        Assert.Equal(InventoryKind.Equipment, groups[11]);
        Assert.Equal(InventoryKind.Container, groups[30]);
        Assert.Equal(InventoryKind.Other, groups[99]);
    }

    [Fact]
    public void BuildInventoryGroups_LabelsPlayerEquipment()
    {
        var (editor, _) = CreateLoadedEditor();

        var groups = editor.BuildInventoryGroups();

        var aliceEquipment = groups.Single(g => g.InventoryId == 11);
        Assert.Equal("Alice's Equipment", aliceEquipment.Label);
    }

    [Fact]
    public void BuildInventoryGroups_LabelsContainerByLinkedWorldObject()
    {
        var (editor, _) = CreateLoadedEditor();

        var groups = editor.BuildInventoryGroups();

        var container = groups.Single(g => g.InventoryId == 30);
        Assert.Equal("StorageContainer (Object #100)", container.Label);
    }

    [Fact]
    public void BuildInventoryGroups_FallsBackToInventoryIdWhenUnowned()
    {
        var (editor, _) = CreateLoadedEditor();

        var groups = editor.BuildInventoryGroups();

        var orphan = groups.Single(g => g.InventoryId == 99);
        Assert.Equal("Inventory #99", orphan.Label);
    }

    [Fact]
    public void BuildInventoryGroups_ItemsContainCorrectWorldObjectIds()
    {
        var (editor, _) = CreateLoadedEditor();

        var groups = editor.BuildInventoryGroups();

        var aliceInventory = groups.Single(g => g.InventoryId == 10);
        Assert.Equal([200, 201], aliceInventory.Items.Select(i => i.WorldObjectId));
    }

    [Fact]
    public void GetDestinationOptions_ExcludesTheItemsOwnInventory()
    {
        var (editor, _) = CreateLoadedEditor();

        var options = editor.GetDestinationOptions(worldObjectId: 200); // lives in inventory 10

        Assert.DoesNotContain(options, o => o.InventoryId == 10);
        Assert.Contains(options, o => o.InventoryId == 30);
    }

    [Fact]
    public void GetDestinationOptions_ReportsCountAndSize()
    {
        var (editor, _) = CreateLoadedEditor();

        var options = editor.GetDestinationOptions(worldObjectId: 200);

        var container = options.Single(o => o.InventoryId == 30);
        Assert.Equal(1, container.Count);
        Assert.Equal(3, container.Size);
        Assert.False(container.IsFull);
    }

    [Fact]
    public void TryMoveItem_Success_RemovesFromSourceAndAddsToDestination()
    {
        var (editor, workspace) = CreateLoadedEditor();

        var result = editor.TryMoveItem(worldObjectId: 200, destinationInventoryId: 30);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        var save = workspace.Current!;
        Assert.Equal([201], WorldObjectIdsCodec.Parse(save.Inventories.Single(i => i.Id == 10).WorldObjectIds));
        Assert.Equal([202, 200], WorldObjectIdsCodec.Parse(save.Inventories.Single(i => i.Id == 30).WorldObjectIds));
    }

    [Fact]
    public void TryMoveItem_PreservesLogisticsConfigAndUnknownKeysOnTheRebuiltInventory()
    {
        var store = new FakeSaveFileStore();
        var save = WorkspaceFixtures.Create();
        // Turn inventory 10 (Alice's, holds items 200/201) into a logistics container with
        // demand/supply groups plus a key this model doesn't name.
        var index = save.Inventories.FindIndex(i => i.Id == 10);
        save.Inventories[index] = save.Inventories[index] with
        {
            DemandGroups = "Iron,Cobalt",
            SupplyGroups = "Magnesium",
            Priority = 2,
            ExtensionData = new() { ["futureField"] = System.Text.Json.JsonSerializer.SerializeToElement("keep-me") }
        };
        store.Seed(Path, save);
        var localizer = new Localizer();
        var workspace = new SaveFileWorkspace(store, new FakeScreenReaderAnnouncer(), localizer);
        workspace.Load(Path);
        var editor = new InventoryEditor(workspace, new ItemCatalog(), localizer);

        var result = editor.TryMoveItem(worldObjectId: 200, destinationInventoryId: 30);

        Assert.True(result.Success);
        var moved = workspace.Current!.Inventories.Single(i => i.Id == 10);
        Assert.Equal([201], WorldObjectIdsCodec.Parse(moved.WorldObjectIds));
        Assert.Equal("Iron,Cobalt", moved.DemandGroups);
        Assert.Equal("Magnesium", moved.SupplyGroups);
        Assert.Equal(2, moved.Priority);
        Assert.Equal("keep-me", moved.ExtensionData!["futureField"].GetString());
    }

    [Fact]
    public void TryMoveItem_ItemNotInAnyInventory_Fails()
    {
        var (editor, _) = CreateLoadedEditor();

        var result = editor.TryMoveItem(worldObjectId: 999999, destinationInventoryId: 30);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void TryMoveItem_SourceEqualsDestination_Fails()
    {
        var (editor, _) = CreateLoadedEditor();

        var result = editor.TryMoveItem(worldObjectId: 200, destinationInventoryId: 10);

        Assert.False(result.Success);
    }

    [Fact]
    public void TryMoveItem_DestinationNotFound_Fails()
    {
        var (editor, _) = CreateLoadedEditor();

        var result = editor.TryMoveItem(worldObjectId: 200, destinationInventoryId: 12345);

        Assert.False(result.Success);
    }

    [Fact]
    public void TryMoveItem_DestinationFull_Fails_AndDoesNotMutateEitherInventory()
    {
        var (editor, workspace) = CreateLoadedEditor();
        // Inventory 20 (Bob's) has Size=1; fill it first so the next move is rejected as full.
        var fillResult = editor.TryMoveItem(worldObjectId: 202, destinationInventoryId: 20);
        Assert.True(fillResult.Success);

        var result = editor.TryMoveItem(worldObjectId: 200, destinationInventoryId: 20);

        Assert.False(result.Success);
        Assert.Contains("full", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        var save = workspace.Current!;
        Assert.Equal([200, 201], WorldObjectIdsCodec.Parse(save.Inventories.Single(i => i.Id == 10).WorldObjectIds));
        Assert.Equal([202], WorldObjectIdsCodec.Parse(save.Inventories.Single(i => i.Id == 20).WorldObjectIds));
    }
}
