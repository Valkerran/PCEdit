using PCEdit.App.Core.Models;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// Where a placed object came from - built by the player, placed in the map by the developers, or
/// spawned in a wreck - from what the save records (issue #64).
/// </summary>
/// <remarks>
/// Measured on four real saves:
/// <list type="bullet">
/// <item>Map-placed objects have ids from 100,000,000 to 199,999,999 - the same ids in unrelated
/// saves - while everything created at runtime is 200,000,000 or above. Exact.</item>
/// <item>Wreck-only types, and any object a procedural instance (section 9) lists as generated, are
/// wreck content. Exact, but section 9 is incomplete on some saves.</item>
/// <item>So a runtime object is "built" only if its type never spawns in wrecks. Countertops,
/// fridges and the vault turn up as wreck furniture too; without a section-9 record they are
/// <see cref="ContainerOrigin.Unknown"/> rather than a guess.</item>
/// </list>
/// </remarks>
public static class ContainerOrigins
{
    private const int FirstMapId = 100_000_000;
    private const int FirstRuntimeId = 200_000_000;

    private static readonly string[] WreckOnlyPrefixes = ["ProceduralWreck", "WreckEntryLocked", "WreckFusionGenerator"];

    // Seen as wreck furniture in a real save's section 9. Widen as phase 4's knowledge base grows.
    private static readonly HashSet<string> AlsoSpawnsInWrecks = new(StringComparer.Ordinal) { "Counter1", "Counter2", "Fridge1", "Vault1" };

    /// <summary>Every object any procedural instance records as generated, skipping entries that
    /// are not ids.</summary>
    public static IReadOnlySet<int> WreckIds(PlanetCrafterSaveFile save) =>
        save.ProceduralInstances.SelectMany(p => WorldObjectIdsCodec.Parse(p.WorldObjectIdsGenerated)).ToHashSet();

    public static ContainerOrigin Of(WorldObject owner, IReadOnlySet<int> wreckIds)
    {
        if (owner.Id is >= FirstMapId and < FirstRuntimeId)
        {
            return ContainerOrigin.Map;
        }

        if (wreckIds.Contains(owner.Id) || WreckOnlyPrefixes.Any(p => owner.GId.StartsWith(p, StringComparison.Ordinal)))
        {
            return ContainerOrigin.Wreck;
        }

        return owner.Id >= FirstRuntimeId && !AlsoSpawnsInWrecks.Contains(owner.GId)
            ? ContainerOrigin.Built
            : ContainerOrigin.Unknown;
    }
}
