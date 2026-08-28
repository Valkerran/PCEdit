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

    void GrantTerraTokens(long playerId, int amount);
}
