using System.Reflection;
using PCEdit.App.Core.Services;

namespace PCEdit.Desktop.Platform;

public sealed class AvaloniaAppVersionInfo : IAppVersionInfo
{
    public string Version =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";
}
