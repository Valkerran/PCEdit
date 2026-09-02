using System.ComponentModel;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.Services;

public interface ISaveFileWorkspace : INotifyPropertyChanged
{
    PlanetCrafterSaveFile? Current { get; }

    string? FilePath { get; }

    bool IsLoaded { get; }

    bool IsDirty { get; }

    string? SaveStatus { get; }

    void Load(string path);

    void Save();

    void MutateUnlocks(Func<SaveFileUnlocks, SaveFileUnlocks> mutate);

    void ReplaceTerraformation(string planetId, Func<PlanetTerraformation, PlanetTerraformation> mutate);

    void ReplacePlayer(long playerId, Func<PlayerData, PlayerData> mutate);

    void ReplaceInventory(int inventoryId, Func<Inventory, Inventory> mutate);

    /// <summary>
    /// Adds <paramref name="amount"/> tokens to the unlocks totals and the player's earned total,
    /// saturating rather than overflowing.
    /// </summary>
    /// <returns>
    /// What the spendable balance could actually take - less than <paramref name="amount"/> when
    /// the grant hit the ceiling. Callers report this, not the requested figure.
    /// </returns>
    int GrantTerraTokens(long playerId, int amount);
}
