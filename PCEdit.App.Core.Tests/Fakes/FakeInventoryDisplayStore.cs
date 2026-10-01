using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.Tests.Fakes;

/// <summary>In-memory <see cref="IInventoryDisplayStore"/> that records what was saved.</summary>
internal sealed class FakeInventoryDisplayStore : IInventoryDisplayStore
{
    public StackingMode Stored { get; set; } = StackingMode.Automatic;

    public List<StackingMode> Saved { get; } = [];

    public StackingMode GetStackingMode() => Stored;

    public void SaveStackingMode(StackingMode mode)
    {
        Saved.Add(mode);
        Stored = mode;
    }
}
