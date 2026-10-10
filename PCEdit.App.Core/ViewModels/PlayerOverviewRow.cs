namespace PCEdit.App.Core.ViewModels;

/// <summary>
/// Presentation-shaped view of one <c>PlayerData</c> for the Overview page. Location/progress are
/// pre-formatted single sentences (no fragment concatenation in XAML); the four gauge values are
/// <see cref="double"/> so the shared VitalStatus converters classify them with the same thresholds.
/// </summary>
public sealed record PlayerOverviewRow(
    string Name,
    bool IsHost,
    string LocationText,
    string ProgressText,
    double Oxygen,
    double Thirst,
    double Health,
    double Toxicity);
