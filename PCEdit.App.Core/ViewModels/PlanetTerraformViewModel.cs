using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.ViewModels;

public sealed partial class PlanetTerraformViewModel : ObservableObject
{
    private const double NotApplicable = -1;

    private readonly ISaveFileWorkspace _workspace;
    private readonly IScreenReaderAnnouncer _announcer;
    private readonly ILocalizer _localizer;
    // The levels as the save holds them now: refreshed after every Apply, so "unchanged" in
    // TryParseLevel never means a value that has since been replaced.
    private PlanetTerraformation _saved;

    public PlanetTerraformViewModel(
        ISaveFileWorkspace workspace,
        IScreenReaderAnnouncer announcer,
        ILocalizer localizer,
        PlanetTerraformation terraformation)
    {
        _workspace = workspace;
        _announcer = announcer;
        _localizer = localizer;
        PlanetId = terraformation.PlanetId;
        _saved = terraformation;
        HasPurification = terraformation.UnitPurificationLevel.Value != NotApplicable;

        OxygenLevel = terraformation.UnitOxygenLevel.ToString();
        HeatLevel = terraformation.UnitHeatLevel.ToString();
        PressureLevel = terraformation.UnitPressureLevel.ToString();
        PlantsLevel = terraformation.UnitPlantsLevel.ToString();
        InsectsLevel = terraformation.UnitInsectsLevel.ToString();
        AnimalsLevel = terraformation.UnitAnimalsLevel.ToString();
        PurificationLevel = HasPurification
            ? terraformation.UnitPurificationLevel.ToString()
            : string.Empty;
    }

    public string PlanetId { get; }

    public bool HasPurification { get; }

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private string _oxygenLevel = "0";

    [ObservableProperty]
    private string _heatLevel = "0";

    [ObservableProperty]
    private string _pressureLevel = "0";

    [ObservableProperty]
    private string _plantsLevel = "0";

    [ObservableProperty]
    private string _insectsLevel = "0";

    [ObservableProperty]
    private string _animalsLevel = "0";

    [ObservableProperty]
    private string _purificationLevel = "0";

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private StatusKind _statusKind;

    /// <summary>Visible text on the accordion header button — just the planet id; the chevron
    /// glyph carries the expand/collapse affordance.</summary>
    public string HeaderText => PlanetId;

    /// <summary>Chevron rotation (degrees): points right when collapsed, down when expanded.</summary>
    public double ChevronRotation => IsExpanded ? 90 : 0;

    /// <summary>Spoken hint describing the expander's current state (SC 4.1.2).</summary>
    public string ExpandHint => _localizer[
        IsExpanded ? LocKeys.Terraform_HintExpanded : LocKeys.Terraform_HintCollapsed];

    partial void OnIsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(ExpandHint));
        OnPropertyChanged(nameof(ChevronRotation));
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    [RelayCommand]
    private void Apply()
    {
        if (!TryParseLevels(out var levels, out var error))
        {
            SetStatus(StatusKind.Error, error!);
            return;
        }

        _workspace.ReplaceTerraformation(PlanetId, terraformation => terraformation with
        {
            UnitOxygenLevel = KeepIfUnchanged(terraformation.UnitOxygenLevel, levels.Oxygen),
            UnitHeatLevel = KeepIfUnchanged(terraformation.UnitHeatLevel, levels.Heat),
            UnitPressureLevel = KeepIfUnchanged(terraformation.UnitPressureLevel, levels.Pressure),
            UnitPlantsLevel = KeepIfUnchanged(terraformation.UnitPlantsLevel, levels.Plants),
            UnitInsectsLevel = KeepIfUnchanged(terraformation.UnitInsectsLevel, levels.Insects),
            UnitAnimalsLevel = KeepIfUnchanged(terraformation.UnitAnimalsLevel, levels.Animals),
            UnitPurificationLevel = KeepIfUnchanged(terraformation.UnitPurificationLevel, levels.Purification)
        });
        _saved = _workspace.Current!.Terraformations.First(terraformation => terraformation.PlanetId == PlanetId);

        SetStatus(StatusKind.Success, _localizer.Format(LocKeys.Terraform_Updated, PlanetId));
    }

    private void SetStatus(StatusKind kind, string message)
    {
        StatusKind = kind;
        StatusMessage = message;
        _announcer.Announce(_localizer, kind, message);
    }

    private bool TryParseLevels(
        out (double Oxygen, double Heat, double Pressure, double Plants, double Insects, double Animals, double Purification) levels,
        out string? error)
    {
        levels = default;

        if (!TryParseLevel(OxygenLevel, _saved.UnitOxygenLevel, out var oxygen) ||
            !TryParseLevel(HeatLevel, _saved.UnitHeatLevel, out var heat) ||
            !TryParseLevel(PressureLevel, _saved.UnitPressureLevel, out var pressure) ||
            !TryParseLevel(PlantsLevel, _saved.UnitPlantsLevel, out var plants) ||
            !TryParseLevel(InsectsLevel, _saved.UnitInsectsLevel, out var insects) ||
            !TryParseLevel(AnimalsLevel, _saved.UnitAnimalsLevel, out var animals))
        {
            error = _localizer[LocKeys.Terraform_InvalidLevels];
            return false;
        }

        var purification = _saved.UnitPurificationLevel.Value;
        if (HasPurification && !TryParseLevel(PurificationLevel, _saved.UnitPurificationLevel, out purification))
        {
            error = _localizer[LocKeys.Terraform_InvalidPurification];
            return false;
        }

        levels = (oxygen, heat, pressure, plants, insects, animals, purification);
        error = null;
        return true;
    }

    /// <summary>A level is a finite number of 0 or more. A negative one is accepted only when it
    /// is what the save already holds, so a save with such a value does not lock the planet's
    /// other levels; -1 in particular is how a save marks "no purification" (issue #72).</summary>
    private static bool TryParseLevel(string text, GameNumber original, out double level) =>
        NumberInput.TryParseDouble(text, out level) && (level >= 0 || level.Equals(original.Value));

    /// <summary>The level as it was, when the player left it alone, so its exact text is kept and
    /// the save diff shows only the edit (issue #85).</summary>
    private static GameNumber KeepIfUnchanged(GameNumber current, double edited) =>
        current.Value.Equals(edited) ? current : edited;

    /// <summary>Called by the Overview page on a language change: the typed levels stay.</summary>
    public void Relocalize()
    {
        StatusMessage = null;
        OnPropertyChanged(nameof(ExpandHint));
    }
}
