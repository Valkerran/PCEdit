using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace PCEdit.Desktop;

/// <summary>
/// Maps a shared <c>PCEdit.App.Core.ViewModels.XxxViewModel</c> to
/// <c>PCEdit.Desktop.Views.XxxView</c> so a <c>ContentControl</c> can render page view models.
/// </summary>
/// <remarks>
/// Views are cached per view-model instance. The page view-models are DI singletons, so navigating
/// back to a page reuses its existing control instead of rebuilding the visual tree and re-running
/// the theme's style pass over it.
/// </remarks>
public sealed class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Type?> ViewTypeByViewModel = new();

    private readonly ConditionalWeakTable<object, Control> _viewCache = new();

    public Control Build(object? data) =>
        data is null
            ? new TextBlock { Text = "null view model" }
            : _viewCache.GetValue(data, static key => CreateView(key));

    private static Control CreateView(object data)
    {
        var viewModelType = data.GetType();

        if (!ViewTypeByViewModel.TryGetValue(viewModelType, out var viewType))
        {
            var viewName = viewModelType.FullName!
                .Replace("PCEdit.App.Core.ViewModels.", "PCEdit.Desktop.Views.", StringComparison.Ordinal)
                .Replace("ViewModel", "View", StringComparison.Ordinal);
            viewType = Type.GetType(viewName);
            ViewTypeByViewModel[viewModelType] = viewType;
        }

        return viewType is not null
            ? (Control)Activator.CreateInstance(viewType)!
            : new TextBlock { Text = $"View not found for {viewModelType.Name}" };
    }

    public bool Match(object? data) =>
        data?.GetType().Namespace == "PCEdit.App.Core.ViewModels";
}
