namespace PCEdit.App.Core.ViewModels;

/// <summary>
/// Presentation-shaped view of one <c>PlayerData</c> for the Overview page. Location/progress are
/// pre-formatted single sentences (no fragment concatenation in XAML); the four gauge values stay
/// <see cref="decimal"/> so the shared VitalStatus converters classify them with the same thresholds.
/// </summary>
public sealed record PlayerOverviewRow(
    string Name,
    bool IsHost,
    string LocationText,
    string ProgressText,
    decimal Oxygen,
    decimal Thirst,
    decimal Health,
    decimal Toxicity);
