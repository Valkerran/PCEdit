namespace PCEdit.App.Core.Models;

/// <summary>
/// A logistics container's priority. The game stores it as an int (<c>Inventory.Priority</c>)
/// from -3 to 3 and shows a named level; this is that fixed set. <see cref="Normal"/> (0) is the
/// default the game writes for a freshly placed container.
/// </summary>
public enum LogisticsPriority
{
    Lowest = -3,
    VeryLow = -2,
    Low = -1,
    Normal = 0,
    High = 1,
    VeryHigh = 2,
    Highest = 3,
}

public static class LogisticsPriorityLevels
{
    /// <summary>Every level, lowest first.</summary>
    public static readonly IReadOnlyList<LogisticsPriority> All =
    [
        LogisticsPriority.Lowest,
        LogisticsPriority.VeryLow,
        LogisticsPriority.Low,
        LogisticsPriority.Normal,
        LogisticsPriority.High,
        LogisticsPriority.VeryHigh,
        LogisticsPriority.Highest,
    ];

    /// <summary>
    /// The raw save value mapped to a level. A value outside -3..3 (a future game build, or a
    /// mod) is clamped to the nearest level rather than rejected.
    /// </summary>
    public static LogisticsPriority FromRaw(int raw) => (LogisticsPriority)Math.Clamp(raw, -3, 3);

    public static int ToRaw(this LogisticsPriority priority) => (int)priority;

    /// <summary>The <c>Strings.resx</c> key for a level's friendly name.</summary>
    public static string ResourceKey(this LogisticsPriority priority) => priority switch
    {
        LogisticsPriority.Lowest => "Logistics_PriorityLowest",
        LogisticsPriority.VeryLow => "Logistics_PriorityVeryLow",
        LogisticsPriority.Low => "Logistics_PriorityLow",
        LogisticsPriority.Normal => "Logistics_PriorityNormal",
        LogisticsPriority.High => "Logistics_PriorityHigh",
        LogisticsPriority.VeryHigh => "Logistics_PriorityVeryHigh",
        LogisticsPriority.Highest => "Logistics_PriorityHighest",
        _ => "Logistics_PriorityNormal",
    };
}
