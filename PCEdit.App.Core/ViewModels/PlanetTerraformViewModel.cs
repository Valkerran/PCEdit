using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.ViewModels;

public sealed partial class PlanetTerraformViewModel : ObservableObject
{
    private const double NotApplicable = -1;
    private const NumberStyles ParseStyle = NumberStyles.Number | NumberStyles.AllowExponent;

    private readonly ISaveFileWorkspace _workspace;
    private readonly IScreenReaderAnnouncer _announcer;
    private readonly ILocalizer _localizer;
    private readonly GameNumber _originalPurificationLevel;

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
        _originalPurificationLevel = terraformation.UnitPurificationLevel;
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

        SetStatus(StatusKind.Success, _localizer.Format(LocKeys.Terraform_Updated, PlanetId));
    }

    private void SetStatus(StatusKind kind, string message)
    {
        StatusKind = kind;
        StatusMessage = message;
        _announcer.Announce(kind == StatusKind.Error ? _localizer.Format(LocKeys.Announce_ErrorPrefix, message) : message);
    }

    private bool TryParseLevels(
        out (double Oxygen, double Heat, double Pressure, double Plants, double Insects, double Animals, double Purification) levels,
        out string? error)
    {
        levels = default;

        if (!TryParseLevel(OxygenLevel, out var oxygen) ||
            !TryParseLevel(HeatLevel, out var heat) ||
            !TryParseLevel(PressureLevel, out var pressure) ||
            !TryParseLevel(PlantsLevel, out var plants) ||
            !TryParseLevel(InsectsLevel, out var insects) ||
            !TryParseLevel(AnimalsLevel, out var animals))
        {
            error = _localizer[LocKeys.Terraform_InvalidLevels];
            return false;
        }

        var purification = _originalPurificationLevel.Value;
        if (HasPurification && !TryParseLevel(PurificationLevel, out purification))
        {
            error = _localizer[LocKeys.Terraform_InvalidPurification];
            return false;
        }

        levels = (oxygen, heat, pressure, plants, insects, animals, purification);
        error = null;
        return true;
    }

    /// <summary>A level must be a finite number: NaN, Infinity and an overflowing "1e999" parse
    /// but could not be written to a save.</summary>
    private static bool TryParseLevel(string text, out double level) =>
        double.TryParse(text, ParseStyle, CultureInfo.InvariantCulture, out level) && double.IsFinite(level);

    /// <summary>The level as it was, when the player left it alone, so its exact text is kept and
    /// the save diff shows only the edit (issue #85).</summary>
    private static GameNumber KeepIfUnchanged(GameNumber current, double edited) =>
        current.Value.Equals(edited) ? current : edited;
}
