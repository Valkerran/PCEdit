using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;
using PCEdit.SaveFileHandler.Models;

namespace PCEdit.App.Core.ViewModels;

public sealed partial class TeleportViewModel(
    ISaveFileWorkspace workspace,
    IScreenReaderAnnouncer announcer,
    ILocalizer localizer,
    INavigationService navigation) : ObservableObject, ILoadable
{
    private readonly ISaveFileWorkspace _workspace = workspace;
    private readonly IScreenReaderAnnouncer _announcer = announcer;
    private readonly ILocalizer _localizer = localizer;
    private readonly INavigationService _navigation = navigation;

    private string OtherPlanetOption => _localizer[LocKeys.Teleport_OtherPlanet];

    public bool IsLoaded => _workspace.IsLoaded;

    public ObservableCollection<PlayerOption> Players { get; } = [];

    public ObservableCollection<string> PlanetIdOptions { get; } = [];

    public ObservableCollection<LandmarkOption> Landmarks { get; } = [];

    [ObservableProperty]
    private PlayerOption? _selectedPlayer;

    [ObservableProperty]
    private string? _selectedPlanetId;

    [ObservableProperty]
    private string _customPlanetId = string.Empty;

    [ObservableProperty]
    private string _x = "0";

    [ObservableProperty]
    private string _y = "0";

    [ObservableProperty]
    private string _z = "0";

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private StatusKind _statusKind;

    public bool IsCustomPlanetId => SelectedPlanetId == OtherPlanetOption;

    public void Load()
    {
        Players.Clear();
        PlanetIdOptions.Clear();
        Landmarks.Clear();
        StatusMessage = null;
        StatusKind = StatusKind.Info;
        OnPropertyChanged(nameof(IsLoaded));

        var save = _workspace.Current;
        if (save is null)
        {
            return;
        }

        foreach (var player in save.Players)
        {
            Players.Add(new PlayerOption(player.Id, player.Name));
        }

        var distinctPlanetIds = new[] { save.Metadata.PlanetId }
            .Concat(save.Terraformations.Select(t => t.PlanetId))
            .Concat(save.Players.Select(p => p.PlanetId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase);

        foreach (var planetId in distinctPlanetIds)
        {
            PlanetIdOptions.Add(planetId);
        }

        PlanetIdOptions.Add(OtherPlanetOption);

        foreach (var landmark in FindLandmarks(save))
        {
            Landmarks.Add(landmark);
        }

        SelectedPlayer = Players.FirstOrDefault();
    }

    partial void OnSelectedPlayerChanged(PlayerOption? value)
    {
        if (value is null)
        {
            return;
        }

        var player = _workspace.Current?.Players.FirstOrDefault(p => p.Id == value.PlayerId);
        if (player is null)
        {
            return;
        }

        SelectedPlanetId = PlanetIdOptions.Contains(player.PlanetId) ? player.PlanetId : OtherPlanetOption;
        CustomPlanetId = player.PlanetId;

        ApplyPlayerPosition(player);
    }

    private void ApplyPlayerPosition(PlayerData player)
    {
        var (x, y, z) = PositionCodec.Parse(player.PlayerPosition);
        X = x.ToString(CultureInfo.InvariantCulture);
        Y = y.ToString(CultureInfo.InvariantCulture);
        Z = z.ToString(CultureInfo.InvariantCulture);
    }

    partial void OnSelectedPlanetIdChanged(string? value)
    {
        OnPropertyChanged(nameof(IsCustomPlanetId));
    }

    [RelayCommand]
    private void ApplyLandmark(LandmarkOption landmark)
    {
        var (x, y, z) = PositionCodec.Parse(landmark.Position);
        X = x.ToString(CultureInfo.InvariantCulture);
        Y = y.ToString(CultureInfo.InvariantCulture);
        Z = z.ToString(CultureInfo.InvariantCulture);
        SetStatus(StatusKind.Success, _localizer.Format(LocKeys.Teleport_PositionFromLandmark, landmark.Label, X, Y, Z));
    }

    [RelayCommand]
    private void UseCurrentPosition()
    {
        if (SelectedPlayer is null)
        {
            SetStatus(StatusKind.Error, _localizer[LocKeys.Teleport_ChoosePlayer]);
            return;
        }

        var player = _workspace.Current?.Players.FirstOrDefault(p => p.Id == SelectedPlayer.PlayerId);
        if (player is null)
        {
            return;
        }

        ApplyPlayerPosition(player);
        SetStatus(StatusKind.Success, _localizer.Format(LocKeys.Teleport_PositionReset, SelectedPlayer.Name));
    }

    [RelayCommand]
    private Task OpenFile() => _navigation.GoToOpenFileAsync();

    [RelayCommand]
    private void Teleport()
    {
        if (SelectedPlayer is null)
        {
            SetStatus(StatusKind.Error, _localizer[LocKeys.Teleport_ChoosePlayer]);
            return;
        }

        var planetId = IsCustomPlanetId ? CustomPlanetId : SelectedPlanetId;
        if (string.IsNullOrWhiteSpace(planetId))
        {
            SetStatus(StatusKind.Error, _localizer[LocKeys.Teleport_ChoosePlanet]);
            return;
        }

        if (!decimal.TryParse(X, NumberStyles.Number, CultureInfo.InvariantCulture, out var x) ||
            !decimal.TryParse(Y, NumberStyles.Number, CultureInfo.InvariantCulture, out var y) ||
            !decimal.TryParse(Z, NumberStyles.Number, CultureInfo.InvariantCulture, out var z))
        {
            SetStatus(StatusKind.Error, _localizer[LocKeys.Teleport_InvalidCoords]);
            return;
        }

        var playerId = SelectedPlayer.PlayerId;
        var position = PositionCodec.Format(x, y, z);

        _workspace.ReplacePlayer(playerId, player => new PlayerData
        {
            Id = player.Id,
            Name = player.Name,
            InventoryId = player.InventoryId,
            EquipmentId = player.EquipmentId,
            PlayerPosition = position,
            PlayerRotation = player.PlayerRotation,
            PlayerGaugeOxygen = player.PlayerGaugeOxygen,
            PlayerGaugeThirst = player.PlayerGaugeThirst,
            PlayerGaugeHealth = player.PlayerGaugeHealth,
            PlayerGaugeToxic = player.PlayerGaugeToxic,
            Host = player.Host,
            PlanetId = planetId,
            TotalCraftedObjects = player.TotalCraftedObjects,
            TotalTerraTokenEarned = player.TotalTerraTokenEarned,
            CameraView = player.CameraView
        });

        SetStatus(StatusKind.Success, _localizer.Format(LocKeys.Teleport_Done, SelectedPlayer.Name, planetId, position));
    }

    private void SetStatus(StatusKind kind, string message)
    {
        StatusKind = kind;
        StatusMessage = message;
        _announcer.Announce(kind == StatusKind.Error ? _localizer.Format(LocKeys.Announce_ErrorPrefix, message) : message);
    }

    private List<LandmarkOption> FindLandmarks(PlanetCrafterSaveFile save)
    {
        return save.WorldObjects
            .Where(w => w.Position is not null &&
                        (w.GId.Contains("pod", StringComparison.OrdinalIgnoreCase) ||
                         w.GId.Contains("teleport", StringComparison.OrdinalIgnoreCase)))
            .Select(w => new LandmarkOption(w.Id, w.GId, w.Position!, DescribePlanetHint(w.Planet)))
            .ToList();
    }

    private string DescribePlanetHint(int? planet) =>
        planet is { } value
            ? _localizer.Format(LocKeys.Teleport_LandmarkPlanetHash, value)
            : _localizer[LocKeys.Teleport_LandmarkNoPlanetHash];
}
