using PCEdit.App.Core.ViewModels;

namespace PCEdit.App.Core.Presentation;

/// <summary>
/// Maps status / vital classifications to a theme-resource key. Each UI head resolves the key
/// against its own themed brush dictionary (keys: <c>StatusInfoText</c>, <c>StatusSuccessText</c>,
/// <c>StatusWarningText</c>, <c>StatusErrorText</c>, <c>TextPrimary</c>).
/// </summary>
public static class StatusPalette
{
    public static string KeyFor(StatusKind kind) => kind switch
    {
        StatusKind.Success => "StatusSuccessText",
        StatusKind.Error => "StatusErrorText",
        _ => "StatusInfoText",
    };

    public static string KeyFor(VitalLevel level) => level switch
    {
        VitalLevel.Critical => "StatusErrorText",
        VitalLevel.Low => "StatusWarningText",
        _ => "TextPrimary",
    };
}
