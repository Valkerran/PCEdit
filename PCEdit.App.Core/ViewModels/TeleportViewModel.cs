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
    INavigationService navigation,
    IPlanetIndex planetIndex) : ObservableObject, ILoadable, IRelocalizable
{
    private readonly ISaveFileWorkspace _workspace = workspace;
    private readonly IScreenReaderAnnouncer _announcer = announcer;
    private readonly ILocalizer _localizer = localizer;
    private readonly INavigationService _navigation = navigation;
    private readonly IPlanetIndex _planetIndex = planetIndex;

    // The "Other…" entry as it stands in PlanetIdOptions. A field, not a lookup: comparing the
    // selection with the current translation made a language change turn a custom-planet
    // selection into a literal planet id (issue #93).
    private string _otherPlanetOption = localizer[LocKeys.Teleport_OtherPlanet];

    public bool IsLoaded => _workspace.IsLoaded;

    public ObservableCollection<PlayerOption> Players { get; } = [];

    public ObservableCollection<string> PlanetIdOptions { get; } = [];

    public ObservableCollection<LandmarkOption> Landmarks { get; } = [];

    private readonly List<LandmarkOption> _allLandmarks = [];

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

    /// <summary>When false (default) the landmark list is trimmed to the currently selected
    /// destination planet; when true every world's landmarks are shown.</summary>
    [ObservableProperty]
    private bool _showAllWorldLandmarks;

    public bool IsCustomPlanetId => SelectedPlanetId == _otherPlanetOption;

    /// <summary>A multiplayer save — the per-player / host-only caveat is worth showing.</summary>
    public bool HasMultiplePlayers => Players.Count > 1;

    /// <summary>Only worth offering the "all worlds" landmark toggle on a multi-world save.</summary>
    public bool ShowWorldLandmarkFilter => PlanetIdOptions.Count(o => o != _otherPlanetOption) > 1;

    public void Load()
    {
        Players.Clear();
        PlanetIdOptions.Clear();
        Landmarks.Clear();
        _allLandmarks.Clear();
        ShowAllWorldLandmarks = false;
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

        foreach (var planetId in _planetIndex.KnownPlanetIds())
        {
            PlanetIdOptions.Add(planetId);
        }

        _otherPlanetOption = _localizer[LocKeys.Teleport_OtherPlanet];
        PlanetIdOptions.Add(_otherPlanetOption);
        OnPropertyChanged(nameof(ShowWorldLandmarkFilter));
        OnPropertyChanged(nameof(HasMultiplePlayers));

        _allLandmarks.AddRange(FindLandmarks(save));
        FilterLandmarks();

        // Cleared first: the option for the same player is equal to the one already selected, and
        // an equal value would skip the change handler that resets the form (issue #93).
        SelectedPlayer = null;
        SelectedPlayer = Players.FirstOrDefault();
    }

    /// <summary>Rebuilds <see cref="Landmarks"/> from <see cref="_allLandmarks"/>, keeping only the
    /// ones on the selected destination planet unless "all worlds" is on (or the destination is a
    /// custom/blank id, where there is nothing to match against).</summary>
    private void FilterLandmarks()
    {
        var restrictTo = !ShowWorldLandmarkFilter || ShowAllWorldLandmarks || IsCustomPlanetId || string.IsNullOrWhiteSpace(SelectedPlanetId)
            ? null
            : SelectedPlanetId;

        Landmarks.Clear();
        foreach (var landmark in _allLandmarks)
        {
            if (restrictTo is null || string.Equals(landmark.PlanetId, restrictTo, StringComparison.OrdinalIgnoreCase))
            {
                Landmarks.Add(landmark);
            }
        }
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

        ClearStatus();
        ResetToPlayer(player);
    }

    /// <summary>Points the world and X/Y/Z at where the player currently is. False when the
    /// player's saved position cannot be read; the boxes are then emptied, because whatever they
    /// held belongs to another player or world and must not be teleported to (issue #92).</summary>
    private bool ResetToPlayer(PlayerData player)
    {
        SelectedPlanetId = PlanetIdOptions.Contains(player.PlanetId) ? player.PlanetId : _otherPlanetOption;
        CustomPlanetId = player.PlanetId;
        return ShowPlayerPosition(player);
    }

    /// <summary>
    /// Puts the player's saved position into X/Y/Z. A position the game did not write - a
    /// hand-edited or corrupt save - must not bring the page down (issue #37), and must not leave
    /// other coordinates standing either: the boxes are emptied and the status says why.
    /// </summary>
    private bool ShowPlayerPosition(PlayerData player)
    {
        if (ApplyPosition(player.PlayerPosition))
        {
            return true;
        }

        X = Y = Z = string.Empty;
        var message = _localizer.Format(LocKeys.Teleport_PositionUnreadable, player.Name);
        if (StatusMessage != message)
        {
            // One reset can reach here twice, through the destination change and then directly.
            SetStatus(StatusKind.Error, message);
        }

        return false;
    }

    private void ClearStatus()
    {
        StatusMessage = null;
        StatusKind = StatusKind.Info;
    }

    /// <summary>Puts a save-file position into the X/Y/Z boxes, ignoring one it cannot read.</summary>
    private bool ApplyPosition(string? position)
    {
        if (!PositionCodec.TryParse(position, out var parsed))
        {
            return false;
        }

        X = parsed.X.ToString(CultureInfo.InvariantCulture);
        Y = parsed.Y.ToString(CultureInfo.InvariantCulture);
        Z = parsed.Z.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    partial void OnSelectedPlanetIdChanged(string? value)
    {
        // Whatever the status said was about the previous destination.
        ClearStatus();
        OnPropertyChanged(nameof(IsCustomPlanetId));
        FilterLandmarks();
        AlignCoordinatesToDestination();
    }

    /// <summary>
    /// Keeps X/Y/Z sensible for the chosen destination world: the player's own position when the
    /// destination is the world they are already on, otherwise that world's arrival point (its
    /// interplanetary escape pod / a teleporter) so a bare "pick a world → Teleport" lands them
    /// somewhere real instead of at coordinates from a different planet. Left untouched for a
    /// custom/blank id or a world with no known landmark.
    /// </summary>
    private void AlignCoordinatesToDestination()
    {
        if (IsCustomPlanetId || string.IsNullOrWhiteSpace(SelectedPlanetId))
        {
            return;
        }

        var player = _workspace.Current?.Players.FirstOrDefault(p => p.Id == SelectedPlayer?.PlayerId);
        if (player is null)
        {
            return;
        }

        if (string.Equals(SelectedPlanetId, player.PlanetId, StringComparison.OrdinalIgnoreCase))
        {
            ShowPlayerPosition(player);
            return;
        }

        var arrival = _allLandmarks
            .Where(l => string.Equals(l.PlanetId, SelectedPlanetId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(l => l.GId.Contains("EscapePodInterplanetary", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(l => l.GId.Contains("EscapePod", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(l => l.GId.Contains("teleport", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
        if (arrival is null)
        {
            return;
        }

        if (!ApplyPosition(arrival.Position))
        {
            return;
        }

        SetStatus(StatusKind.Info, _localizer.Format(LocKeys.Teleport_AimedAtWorld, SelectedPlanetId));
    }

    partial void OnShowAllWorldLandmarksChanged(bool value)
    {
        FilterLandmarks();
    }

    [RelayCommand]
    private void ApplyLandmark(LandmarkOption landmark)
    {
        if (!ApplyPosition(landmark.Position))
        {
            // FindLandmarks already drops these, so this is belt-and-braces against a future
            // change there rather than something a user can reach today.
            SetStatus(StatusKind.Error, _localizer[LocKeys.Teleport_InvalidCoords]);
            return;
        }

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

        ClearStatus();
        if (!ResetToPlayer(player))
        {
            return;
        }

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

        var planetId = (IsCustomPlanetId ? CustomPlanetId : SelectedPlanetId)?.Trim();
        if (string.IsNullOrWhiteSpace(planetId))
        {
            SetStatus(StatusKind.Error, _localizer[LocKeys.Teleport_ChoosePlanet]);
            return;
        }

        if (!NumberInput.TryParseDecimal(X, out var x) ||
            !NumberInput.TryParseDecimal(Y, out var y) ||
            !NumberInput.TryParseDecimal(Z, out var z))
        {
            SetStatus(StatusKind.Error, _localizer[LocKeys.Teleport_InvalidCoords]);
            return;
        }

        var playerId = SelectedPlayer.PlayerId;
        var position = PositionCodec.Format(x, y, z);

        _workspace.ReplacePlayer(playerId, player => player with
        {
            PlayerPosition = position,
            PlanetId = planetId
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
            // A landmark whose position cannot be read is useless as a teleport target, so drop
            // it here rather than letting it fail when the user picks it.
            .Where(w => PositionCodec.TryParse(w.Position, out _) &&
                        (w.GId.Contains("pod", StringComparison.OrdinalIgnoreCase) ||
                         w.GId.Contains("teleport", StringComparison.OrdinalIgnoreCase)))
            .Select(w =>
            {
                var planetId = _planetIndex.ResolvePlanetId(w.Planet);
                return new LandmarkOption(w.Id, w.GId, w.Position!, DescribePlanetHint(w.Planet, planetId), planetId);
            })
            .ToList();
    }

    private string DescribePlanetHint(int? planet, string? planetId) =>
        planetId is not null ? planetId
        : planet is { } value ? _localizer.Format(LocKeys.Teleport_LandmarkPlanetHash, value)
        : _localizer[LocKeys.Teleport_LandmarkNoPlanetHash];

    /// <summary>Swaps the translated "Other…" entry and the landmark labels; X/Y/Z, the custom
    /// planet id and the selections stay as the user left them.</summary>
    public void Relocalize()
    {
        var wasCustom = IsCustomPlanetId;
        var index = PlanetIdOptions.IndexOf(_otherPlanetOption);
        _otherPlanetOption = _localizer[LocKeys.Teleport_OtherPlanet];
        if (index >= 0)
        {
            PlanetIdOptions[index] = _otherPlanetOption;
        }

        if (wasCustom)
        {
            SelectedPlanetId = _otherPlanetOption;
        }

        ClearStatus();
        if (_workspace.Current is { } save)
        {
            _allLandmarks.Clear();
            _allLandmarks.AddRange(FindLandmarks(save));
            FilterLandmarks();
        }
    }
}
