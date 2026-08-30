namespace PCEdit.App.Core.Models;

/// <summary>A priority level plus its localized name, for the editor's priority dropdown.</summary>
public sealed record LogisticsPriorityChoice(LogisticsPriority Value, string DisplayName);
