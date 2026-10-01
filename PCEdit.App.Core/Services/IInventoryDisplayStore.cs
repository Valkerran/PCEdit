using PCEdit.App.Core.Models;

namespace PCEdit.App.Core.Services;

/// <summary>Remembers how the Inventories page shows identical items across runs. The UI head
/// supplies storage.</summary>
public interface IInventoryDisplayStore
{
    /// <summary>The saved choice, or <see cref="StackingMode.Automatic"/> when none (or an unreadable
    /// one) is stored.</summary>
    StackingMode GetStackingMode();

    void SaveStackingMode(StackingMode mode);
}
