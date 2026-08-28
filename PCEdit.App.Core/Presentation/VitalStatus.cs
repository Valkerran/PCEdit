namespace PCEdit.App.Core.Presentation;

/// <summary>Severity band for a player vital gauge.</summary>
public enum VitalLevel
{
    Ok,
    Low,
    Critical
}

/// <summary>
/// Shared threshold logic for the player vital gauges so the colour converter and
/// the text converter always agree.
/// </summary>
public static class VitalStatus
{
    public const string HighIsBadParameter = "HighIsBad";

    public static VitalLevel Classify(object? value, object? parameter)
    {
        if (value is not decimal amount)
        {
            return VitalLevel.Ok;
        }

        var highIsBad = string.Equals(parameter as string, HighIsBadParameter, StringComparison.OrdinalIgnoreCase);

        return highIsBad
            ? amount switch
            {
                >= 50 => VitalLevel.Critical,
                >= 20 => VitalLevel.Low,
                _ => VitalLevel.Ok
            }
            : amount switch
            {
                < 20 => VitalLevel.Critical,
                < 50 => VitalLevel.Low,
                _ => VitalLevel.Ok
            };
    }
}
