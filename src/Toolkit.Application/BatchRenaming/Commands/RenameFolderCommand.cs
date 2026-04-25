namespace Toolkit.Application.BatchRenaming.Commands;

public sealed record RenameFolderCommand(string RootFolder, FolderRenameSpec Spec);
