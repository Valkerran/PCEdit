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
