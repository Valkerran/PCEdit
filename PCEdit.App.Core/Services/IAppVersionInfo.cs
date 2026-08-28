namespace PCEdit.App.Core.Services;

/// <summary>Read-only application metadata supplied by the UI head.</summary>
public interface IAppVersionInfo
{
    /// <summary>Display version string, e.g. <c>"1.0"</c>.</summary>
    string Version { get; }
}
