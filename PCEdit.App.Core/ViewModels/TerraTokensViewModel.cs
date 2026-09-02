using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Models;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.ViewModels;

public sealed partial class TerraTokensViewModel(
    ISaveFileWorkspace workspace,
    IScreenReaderAnnouncer announcer,
    ILocalizer localizer,
    INavigationService navigation) : ObservableObject, ILoadable
{
    private readonly ISaveFileWorkspace _workspace = workspace;
    private readonly IScreenReaderAnnouncer _announcer = announcer;
    private readonly ILocalizer _localizer = localizer;
    private readonly INavigationService _navigation = navigation;

    public bool IsLoaded => _workspace.IsLoaded;

    public ObservableCollection<PlayerOption> Players { get; } = [];

    [ObservableProperty]
    private PlayerOption? _selectedPlayer;

    [ObservableProperty]
    private string _amount = "0";

    [ObservableProperty]
    private int _terraTokens;

    [ObservableProperty]
    private int _allTimeTerraTokens;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private StatusKind _statusKind;

    public void Load()
    {
        Players.Clear();
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

        SelectedPlayer = Players.FirstOrDefault();
        TerraTokens = save.Unlocks.TerraTokens;
        AllTimeTerraTokens = save.Unlocks.AllTimeTerraTokens;
    }

    [RelayCommand]
    private void Grant()
    {
        if (SelectedPlayer is null)
        {
            SetStatus(StatusKind.Error, _localizer[LocKeys.TerraTokens_ChoosePlayer]);
            return;
        }

        if (!int.TryParse(Amount, out var amount) || amount <= 0)
        {
            SetStatus(StatusKind.Error, _localizer[LocKeys.TerraTokens_InvalidAmount]);
            return;
        }

        // What the balance could actually take - a grant that hit the int ceiling gives less
        // than was asked for, and the status line should say so rather than the requested figure.
        var granted = _workspace.GrantTerraTokens(SelectedPlayer.PlayerId, amount);

        var save = _workspace.Current!;
        TerraTokens = save.Unlocks.TerraTokens;
        AllTimeTerraTokens = save.Unlocks.AllTimeTerraTokens;
        SetStatus(StatusKind.Success, _localizer.Format(LocKeys.TerraTokens_Granted, granted, SelectedPlayer.Name));
    }

    private void SetStatus(StatusKind kind, string message)
    {
        StatusKind = kind;
        StatusMessage = message;
        _announcer.Announce(kind == StatusKind.Error ? _localizer.Format(LocKeys.Announce_ErrorPrefix, message) : message);
    }

    [RelayCommand]
    private Task OpenFile() => _navigation.GoToOpenFileAsync();
}
