using PCEdit.App.Core.Models;
using PCEdit.SaveFileHandler;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Services;

/// <summary>
/// The free slots in the player's own storage crates, and where excess items go when a repair
/// moves them there instead of removing them (issue #64, phase 6b).
/// </summary>
/// <remarks>
/// A destination is a Storage Crate, Locker Storage or T2 Locker Storage the player placed - matched
/// by type id, since a wreck crate is also called "Storage Crate", and never one the developers
/// placed in the map (see <see cref="ContainerOrigins"/>) - with free slots against its stored size. Never a logistics container (drones would carry the items straight back out), and only
/// on the same planet as the item came from. An item goes to a crate already holding that type,
/// keeping stacks together, or else to the crate with the most free slots.
/// </remarks>
internal sealed class FreeStorage
{
    // Containers that take any item. Widen once phase 4's knowledge base records what each
    // container accepts - a Fridge, for one, may take food only.
    private static readonly HashSet<string> GeneralStorage = new(StringComparer.Ordinal) { "Container1", "Container2", "Container3" };

    private readonly Dictionary<int, int?> _planetOfInventory;
    // Keyed by PlanetKey: a dictionary cannot hold a null key, and "no planet" is a real case
    // (an unowned inventory, a crate the game wrote without one).
    private readonly Dictionary<long, PriorityQueue<Crate, int>> _mostFreeByPlanet = [];
    private readonly Dictionary<(int? Planet, string GId), List<Crate>> _holding = [];
    private readonly List<Crate> _crates = [];

    private FreeStorage(Dictionary<int, int?> planetOfInventory) => _planetOfInventory = planetOfInventory;

    /// <summary>Free slots across every destination, before anything is placed.</summary>
    public int FreeSlots { get; private set; }

    /// <summary>Each destination that received items, with them in the order they arrived.</summary>
    public IEnumerable<(int InventoryId, IReadOnlyList<int> Items)> Received =>
        _crates.Where(c => c.Received.Count > 0).Select(c => (c.InventoryId, (IReadOnlyList<int>)c.Received));

    public static FreeStorage Find(PlanetCrafterSaveFile save, IReadOnlyDictionary<int, WorldObject> worldObjects)
    {
        var storage = new FreeStorage(PlanetOfEachInventory(save));
        var inventories = save.Inventories.GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First());
        var wreckIds = ContainerOrigins.WreckIds(save);
        foreach (var owner in save.WorldObjects)
        {
            if (owner.LinkedInventoryId is { } inventoryId && GeneralStorage.Contains(owner.GId)
                && !PlacedByTheGame(owner, wreckIds)
                && inventories.TryGetValue(inventoryId, out var inventory) && inventory.Priority is null)
            {
                storage.Add(inventory, owner.Planet, worldObjects);
            }
        }

        return storage;
    }

    /// <summary>Puts one item from <paramref name="sourceInventoryId"/> into a crate on the same
    /// planet, or returns false when there is no room left there.</summary>
    public bool TryPlace(int sourceInventoryId, int itemId, string gId)
    {
        var planet = _planetOfInventory.GetValueOrDefault(sourceInventoryId);
        var crate = FirstWithRoom(_holding.GetValueOrDefault((planet, gId))) ?? MostFree(planet);
        if (crate is null)
        {
            return false;
        }

        crate.Free--;
        crate.Received.Add(itemId);
        Hold(crate, gId);
        if (crate.Free > 0)
        {
            _mostFreeByPlanet[PlanetKey(crate.Planet)].Enqueue(crate, -crate.Free);
        }

        return true;
    }

    private void Add(Inventory inventory, int? planet, IReadOnlyDictionary<int, WorldObject> worldObjects)
    {
        var items = WorldObjectIdsCodec.Parse(inventory.WorldObjectIds).Where(worldObjects.ContainsKey).ToList();
        var crate = new Crate(inventory.Id, planet) { Free = inventory.Size - items.Count };
        if (crate.Free <= 0)
        {
            return;
        }

        _crates.Add(crate);
        FreeSlots += crate.Free;
        foreach (var gId in items.Select(id => worldObjects[id].GId).Distinct())
        {
            Hold(crate, gId);
        }

        if (!_mostFreeByPlanet.TryGetValue(PlanetKey(planet), out var queue))
        {
            _mostFreeByPlanet[PlanetKey(planet)] = queue = new PriorityQueue<Crate, int>();
        }

        queue.Enqueue(crate, -crate.Free);
    }

    private void Hold(Crate crate, string gId)
    {
        if (!crate.Holds.Add(gId))
        {
            return;
        }

        var key = (crate.Planet, gId);
        if (!_holding.TryGetValue(key, out var holders))
        {
            _holding[key] = holders = [];
        }

        holders.Add(crate);
    }

    /// <summary>A crate the developers placed in the map, or one a wreck spawned: not the player's
    /// own storage, so items are not moved into it.</summary>
    private static bool PlacedByTheGame(WorldObject owner, IReadOnlySet<int> wreckIds) =>
        ContainerOrigins.Of(owner, wreckIds) is ContainerOrigin.Map or ContainerOrigin.Wreck;

    private static long PlanetKey(int? planet) => planet ?? long.MinValue;

    private static Crate? FirstWithRoom(List<Crate>? crates) => crates?.FirstOrDefault(c => c.Free > 0);

    /// <summary>The crate with the most free slots on the planet. The queue is updated lazily: an
    /// entry whose recorded room no longer matches the crate is stale and skipped.</summary>
    private Crate? MostFree(int? planet)
    {
        if (!_mostFreeByPlanet.TryGetValue(PlanetKey(planet), out var queue))
        {
            return null;
        }

        while (queue.TryDequeue(out var crate, out var negativeFree))
        {
            if (crate.Free > 0 && -negativeFree == crate.Free)
            {
                return crate;
            }
        }

        return null;
    }

    /// <summary>Where each inventory is: its owning object's planet, or its player's for a
    /// backpack or equipment. Null when it has no owner.</summary>
    private static Dictionary<int, int?> PlanetOfEachInventory(PlanetCrafterSaveFile save)
    {
        var planets = new Dictionary<int, int?>();
        foreach (var player in save.Players)
        {
            int? planet = string.IsNullOrWhiteSpace(player.PlanetId) ? null : PlanetHash.Of(player.PlanetId);
            planets.TryAdd(player.InventoryId, planet);
            planets.TryAdd(player.EquipmentId, planet);
        }

        foreach (var owner in save.WorldObjects)
        {
            if (owner.LinkedInventoryId is { } inventoryId)
            {
                planets.TryAdd(inventoryId, owner.Planet);
            }

            foreach (var spawned in WorldObjectIdsCodec.Parse(owner.SpawnedInstanceIds))
            {
                planets.TryAdd(spawned, owner.Planet);
            }
        }

        return planets;
    }

    private sealed class Crate(int inventoryId, int? planet)
    {
        public int InventoryId { get; } = inventoryId;

        public int? Planet { get; } = planet;

        public int Free { get; set; }

        public HashSet<string> Holds { get; } = new(StringComparer.Ordinal);

        public List<int> Received { get; } = [];
    }
}
