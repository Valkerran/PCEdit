namespace PCEdit.App.Core.Localization;

/// <summary>
/// String-catalog keys referenced from C# (ViewModels, services, converters). XAML uses the
/// key literally via the head's <c>Translate</c> markup extension. A unit test asserts every
/// value here exists in <c>Strings.resx</c>.
/// </summary>
public static class LocKeys
{
    public const string Common_Cancel = "Common_Cancel";
    public const string Common_Discard = "Common_Discard";
    public const string Common_Ok = "Common_Ok";
    public const string Common_CloseWithoutSaving = "Common_CloseWithoutSaving";
    public const string Common_KeepEditing = "Common_KeepEditing";

    public const string Quit_DiscardTitle = "Quit_DiscardTitle";
    public const string Quit_DiscardBody = "Quit_DiscardBody";

    public const string Shell_Title = "Shell_Title";
    public const string Nav_OpenFile = "Nav_OpenFile";
    public const string Nav_Overview = "Nav_Overview";
    public const string Nav_Inventories = "Nav_Inventories";
    public const string Nav_TerraTokens = "Nav_TerraTokens";
    public const string Nav_Teleport = "Nav_Teleport";
    public const string Nav_About = "Nav_About";

    public const string Announce_ErrorPrefix = "Announce_ErrorPrefix";

    public const string Dirty_Unsaved = "Dirty_Unsaved";
    public const string Dirty_Saved = "Dirty_Saved";

    public const string Save_Ok = "Save_Ok";
    public const string Save_OkAnnounce = "Save_OkAnnounce";
    public const string Save_Failed = "Save_Failed";
    public const string Save_FailedAnnounce = "Save_FailedAnnounce";

    public const string OpenFile_DiscardTitle = "OpenFile_DiscardTitle";
    public const string OpenFile_DiscardBody = "OpenFile_DiscardBody";
    public const string OpenFile_PickerTitle = "OpenFile_PickerTitle";
    public const string OpenFile_LoadingAnnounce = "OpenFile_LoadingAnnounce";
    public const string OpenFile_Loaded = "OpenFile_Loaded";
    public const string OpenFile_LoadFailed = "OpenFile_LoadFailed";

    public const string Overview_PlayerLocation = "Overview_PlayerLocation";
    public const string Overview_PlayerProgress = "Overview_PlayerProgress";

    public const string Vital_Ok = "Vital_Ok";
    public const string Vital_Low = "Vital_Low";
    public const string Vital_Critical = "Vital_Critical";

    public const string Terraform_HintCollapsed = "Terraform_HintCollapsed";
    public const string Terraform_HintExpanded = "Terraform_HintExpanded";
    public const string Terraform_InvalidLevels = "Terraform_InvalidLevels";
    public const string Terraform_InvalidPurification = "Terraform_InvalidPurification";
    public const string Terraform_Updated = "Terraform_Updated";

    public const string Inventories_MoveA11y = "Inventories_MoveA11y";

    public const string Inv_PlayerInventory = "Inv_PlayerInventory";
    public const string Inv_PlayerEquipment = "Inv_PlayerEquipment";
    public const string Inv_Container = "Inv_Container";
    public const string Inv_Fallback = "Inv_Fallback";
    public const string Inv_NotInInventory = "Inv_NotInInventory";
    public const string Inv_AlreadyThere = "Inv_AlreadyThere";
    public const string Inv_DestNotFound = "Inv_DestNotFound";
    public const string Inv_DestFull = "Inv_DestFull";

    public const string SelectInv_MoveFailedTitle = "SelectInv_MoveFailedTitle";
    public const string SelectInv_MoveIncomplete = "SelectInv_MoveIncomplete";
    public const string SelectInv_Moved = "SelectInv_Moved";

    public const string TerraTokens_ChoosePlayer = "TerraTokens_ChoosePlayer";
    public const string TerraTokens_InvalidAmount = "TerraTokens_InvalidAmount";
    public const string TerraTokens_Granted = "TerraTokens_Granted";

    public const string Teleport_PositionFromLandmark = "Teleport_PositionFromLandmark";
    public const string Teleport_PositionReset = "Teleport_PositionReset";
    public const string Teleport_ChoosePlayer = "Teleport_ChoosePlayer";
    public const string Teleport_ChoosePlanet = "Teleport_ChoosePlanet";
    public const string Teleport_InvalidCoords = "Teleport_InvalidCoords";
    public const string Teleport_Done = "Teleport_Done";
    public const string Teleport_OtherPlanet = "Teleport_OtherPlanet";
    public const string Teleport_LandmarkPlanetHash = "Teleport_LandmarkPlanetHash";
    public const string Teleport_LandmarkNoPlanetHash = "Teleport_LandmarkNoPlanetHash";

    public const string About_Version = "About_Version";

    public const string Disclaimer_Title = "Disclaimer_Title";
    public const string Disclaimer_Body = "Disclaimer_Body";
    public const string Disclaimer_Acknowledge = "Disclaimer_Acknowledge";
}
