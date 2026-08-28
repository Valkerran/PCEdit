namespace PCEdit.App.Core.Services;

public sealed record MoveItemResult(bool Success, string? ErrorMessage)
{
    public static MoveItemResult Ok()
    {
        return new MoveItemResult(true, null);
    }

    public static MoveItemResult Fail(string message)
    {
        return new MoveItemResult(false, message);
    }
}
