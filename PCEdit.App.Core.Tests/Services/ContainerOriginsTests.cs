using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Tests.Services;

public sealed class ContainerOriginsTests
{
    private static readonly IReadOnlySet<int> NoWrecks = new HashSet<int>();

    private static ContainerOrigin Of(int id, string gId, IReadOnlySet<int>? wreckIds = null) =>
        ContainerOrigins.Of(new WorldObject { Id = id, GId = gId }, wreckIds ?? NoWrecks);

    [Theory]
    [InlineData(107_090_832)] // the same Storage Crate is in three unrelated real saves
    [InlineData(101_464_942)]
    public void AnIdInTheMapRange_IsMapPlaced(int id)
    {
        Assert.Equal(ContainerOrigin.Map, Of(id, "Container1"));
    }

    [Theory]
    [InlineData("ProceduralWreckContainer1")]
    [InlineData("ProceduralWreckSafe")]
    [InlineData("WreckEntryLocked2")]
    [InlineData("WreckFusionGenerator")]
    public void AWreckOnlyType_IsWreckContent(string gId)
    {
        Assert.Equal(ContainerOrigin.Wreck, Of(204_000_000, gId));
    }

    [Fact]
    public void AnObjectAWreckSpawned_IsWreckContent_WhateverItsType()
    {
        Assert.Equal(ContainerOrigin.Wreck, Of(204_000_000, "Counter1", new HashSet<int> { 204_000_000 }));
    }

    [Theory]
    [InlineData("Container1")]
    [InlineData("Container3")]
    [InlineData("OreExtractor2")]
    public void ARuntimeObjectOfAPlayerOnlyType_IsBuiltByThePlayer(string gId)
    {
        Assert.Equal(ContainerOrigin.Built, Of(205_000_000, gId));
    }

    [Theory]
    [InlineData("Counter1")] // all four turn up as wreck furniture in a real save's section 9
    [InlineData("Counter2")]
    [InlineData("Fridge1")]
    [InlineData("Vault1")]
    public void ATypeThatAlsoSpawnsInWrecks_WithNoEvidence_IsUnknown(string gId)
    {
        // Section 9 is incomplete on some saves, so "not listed" is not "built by the player".
        Assert.Equal(ContainerOrigin.Unknown, Of(205_000_000, gId));
    }

    [Fact]
    public void WreckIds_AreReadFromEveryProceduralInstance()
    {
        var save = PCEdit.App.Core.Tests.Fixtures.WorkspaceFixtures.Create();
        save.ProceduralInstances.Add(new ProceduralInstance { Position = "0,0,0", Rotation = "0,0,0,1", WorldObjectIdsGenerated = "201,202", WorldObjectIdsDropped = "" });
        save.ProceduralInstances.Add(new ProceduralInstance { Position = "0,0,0", Rotation = "0,0,0,1", WorldObjectIdsGenerated = "203,junk", WorldObjectIdsDropped = "" });

        Assert.Equal([201, 202, 203], ContainerOrigins.WreckIds(save).Order().ToArray());
    }
}
