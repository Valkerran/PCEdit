using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCEdit.App.Core.Localization;
using PCEdit.App.Core.Services;

namespace PCEdit.App.Core.ViewModels;

public sealed partial class OpenFileViewModel : ObservableObject
{
    private readonly ISaveFileWorkspace _workspace;
    private readonly IFilePickerService _filePicker;
    private readonly IScreenReaderAnnouncer _announcer;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly ILocalizer _localizer;

    private string? _statusKey;
    private object?[] _statusArgs = [];

    public OpenFileViewModel(
        ISaveFileWorkspace workspace,
        IFilePickerService filePicker,
        IScreenReaderAnnouncer announcer,
        INavigationService navigation,
        IDialogService dialogs,
        ILocalizer localizer)
    {
        _workspace = workspace;
        _filePicker = filePicker;
        _announcer = announcer;
        _navigation = navigation;
        _dialogs = dialogs;
        _localizer = localizer;

        // The status line stays visible across a language change, so re-render it from its key.
        _localizer.CultureChanged += (_, _) => OnPropertyChanged(nameof(StatusMessage));
    }

    [ObservableProperty]
    private StatusKind _statusKind;

    [ObservableProperty]
    private bool _isBusy;

    public ISaveFileWorkspace Workspace => _workspace;

    public string? StatusMessage =>
        _statusKey is null ? null : _localizer.Format(_statusKey, _statusArgs);

    [RelayCommand]
    private async Task PickAndLoadAsync()
    {
        if (_workspace.IsDirty && !await ConfirmDiscardAsync())
        {
            return;
        }

        IsBusy = true;
        SetStatus(StatusKind.Info, null);
        _announcer.Announce(_localizer[LocKeys.OpenFile_LoadingAnnounce]);
        try
        {
            await PickAndOpenAsync();
        }
        catch (Exception ex)
        {
            ReportLoadFailure(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PickAndOpenAsync()
    {
        var path = await _filePicker.PickSaveFileAsync(_localizer[LocKeys.OpenFile_PickerTitle]);
        if (path is null)
        {
            return;
        }

        _workspace.Load(path);
        SetStatus(StatusKind.Success, LocKeys.OpenFile_Loaded, Path.GetFileName(path));
        await _navigation.GoToOverviewAsync();
    }

    private Task<bool> ConfirmDiscardAsync() => _dialogs.ConfirmAsync(
        _localizer[LocKeys.OpenFile_DiscardTitle],
        _localizer[LocKeys.OpenFile_DiscardBody],
        _localizer[LocKeys.Common_Discard],
        _localizer[LocKeys.Common_Cancel]);

    private void ReportLoadFailure(Exception ex)
    {
        System.Diagnostics.Trace.WriteLine($"Could not load the save file: {ex}");

        // Name what is broken: a refused save is otherwise a dead end (issue #115). Only an
        // InvalidDataException's text is shown; the save layer writes it and it holds no paths.
        if (ex is InvalidDataException)
        {
            SetStatus(StatusKind.Error, LocKeys.OpenFile_LoadFailedDetail, ex.Message);
        }
        else
        {
            SetStatus(StatusKind.Error, LocKeys.OpenFile_LoadFailed);
        }
    }

    private void SetStatus(StatusKind kind, string? key, params object?[] args)
    {
        StatusKind = kind;
        _statusKey = key;
        _statusArgs = args ?? [];
        OnPropertyChanged(nameof(StatusMessage));

        if (key is null)
        {
            return;
        }

        var message = _localizer.Format(key, _statusArgs);
        _announcer.Announce(kind == StatusKind.Error
            ? _localizer.Format(LocKeys.Announce_ErrorPrefix, message)
            : message);
    }
}
