namespace PCEdit.SaveFileHandler.Models;

public sealed class SaveFileUnlocks
{
    public int TerraTokens { get; init; }

    public int AllTimeTerraTokens { get; init; }

    public required string UnlockedGroups { get; init; }

    public int OpenedInstanceSeed { get; init; }

    public int OpenedInstanceTimeLeft { get; init; }
}
