namespace Toolkit.Application.BatchRenaming.Queries;

public sealed record PreviewRenameQuery(string RootFolder, FolderRenameSpec Spec);

public sealed record RenamePreview(
    string OriginalFolderName,
    string NewFolderName,
    IReadOnlyList<FileRenamePreviewItem> Files);

public sealed record FileRenamePreviewItem(
    string SubfolderName,
    string OriginalFileName,
    string NewFileName);
