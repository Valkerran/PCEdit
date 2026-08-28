using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.ViewModels;

public sealed partial class PlanetTerraformViewModel : ObservableObject
{
    private const decimal NotApplicable = -1m;

    private readonly ISaveFileWorkspace _workspace;
    private readonly IScreenReaderAnnouncer _announcer;
    private readonly ILocalizer _localizer;
    private readonly decimal _originalPurificationLevel;

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
        HasPurification = terraformation.UnitPurificationLevel != NotApplicable;

        OxygenLevel = terraformation.UnitOxygenLevel.ToString(CultureInfo.InvariantCulture);
        HeatLevel = terraformation.UnitHeatLevel.ToString(CultureInfo.InvariantCulture);
        PressureLevel = terraformation.UnitPressureLevel.ToString(CultureInfo.InvariantCulture);
        PlantsLevel = terraformation.UnitPlantsLevel.ToString(CultureInfo.InvariantCulture);
        InsectsLevel = terraformation.UnitInsectsLevel.ToString(CultureInfo.InvariantCulture);
        AnimalsLevel = terraformation.UnitAnimalsLevel.ToString(CultureInfo.InvariantCulture);
        PurificationLevel = HasPurification
            ? terraformation.UnitPurificationLevel.ToString(CultureInfo.InvariantCulture)
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

        _workspace.ReplaceTerraformation(PlanetId, terraformation => new PlanetTerraformation
        {
            PlanetId = terraformation.PlanetId,
            UnitOxygenLevel = levels.Oxygen,
            UnitHeatLevel = levels.Heat,
            UnitPressureLevel = levels.Pressure,
            UnitPlantsLevel = levels.Plants,
            UnitInsectsLevel = levels.Insects,
            UnitAnimalsLevel = levels.Animals,
            UnitPurificationLevel = levels.Purification
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
        out (decimal Oxygen, decimal Heat, decimal Pressure, decimal Plants, decimal Insects, decimal Animals, decimal Purification) levels,
        out string? error)
    {
        levels = default;

        if (!decimal.TryParse(OxygenLevel, NumberStyles.Number, CultureInfo.InvariantCulture, out var oxygen) ||
            !decimal.TryParse(HeatLevel, NumberStyles.Number, CultureInfo.InvariantCulture, out var heat) ||
            !decimal.TryParse(PressureLevel, NumberStyles.Number, CultureInfo.InvariantCulture, out var pressure) ||
            !decimal.TryParse(PlantsLevel, NumberStyles.Number, CultureInfo.InvariantCulture, out var plants) ||
            !decimal.TryParse(InsectsLevel, NumberStyles.Number, CultureInfo.InvariantCulture, out var insects) ||
            !decimal.TryParse(AnimalsLevel, NumberStyles.Number, CultureInfo.InvariantCulture, out var animals))
        {
            error = _localizer[LocKeys.Terraform_InvalidLevels];
            return false;
        }

        var purification = _originalPurificationLevel;
        if (HasPurification && !decimal.TryParse(PurificationLevel, NumberStyles.Number, CultureInfo.InvariantCulture, out purification))
        {
            error = _localizer[LocKeys.Terraform_InvalidPurification];
            return false;
        }

        levels = (oxygen, heat, pressure, plants, insects, animals, purification);
        error = null;
        return true;
    }
}
