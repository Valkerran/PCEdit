namespace PCEdit.SaveFileHandler.Models;

public sealed class ReadMessage
{
    public required string StringId { get; init; }

    public bool IsRead { get; init; }
}
