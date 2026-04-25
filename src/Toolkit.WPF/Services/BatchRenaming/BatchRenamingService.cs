using Toolkit.WPF.Models.BatchRenaming;
using Toolkit.WPF.Models.Common;

namespace Toolkit.WPF.Services.BatchRenaming;

public sealed class BatchRenamingService
{
    private readonly IFileRenamer _renamer;
    private readonly IFileSystemService _fileSystem;

    public BatchRenamingService(IFileRenamer renamer, IFileSystemService fileSystem)
    {
        _renamer    = renamer;
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Preview what renames would occur without actually renaming anything.
    /// </summary>
    public OperationResult<RenamePreview> Preview(string rootFolder, FolderRenameSpec spec)
    {
        if (!_fileSystem.DirectoryExists(rootFolder))
            return OperationResult<RenamePreview>.Failure($"Directory not found: {rootFolder}");

        var rule         = new RenameRule(spec);
        var originalName = Path.GetFileName(rootFolder)!;
        var previewItems = new List<FileRenamePreviewItem>();

        var subfolders = _fileSystem.GetDirectories(rootFolder);

        foreach (var subfolder in subfolders)
        {
            var subName    = Path.GetFileName(subfolder)!;
            var newSubName = rule.GenerateSubfolderName(subName);
            if (newSubName is null) continue;

            var pdfFiles  = _fileSystem.GetFiles(subfolder, "*.pdf");
            var fileNames = pdfFiles.Select(Path.GetFileName).Where(f => f is not null).Select(f => f!).ToList();
            var renames   = rule.GeneratePdfNames(newSubName, fileNames);

            foreach (var (orig, newName) in renames)
                previewItems.Add(new FileRenamePreviewItem(subName, orig, newName));
        }

        return OperationResult<RenamePreview>.Success(
            new RenamePreview(originalName, originalName, previewItems));
    }

    /// <summary>
    /// Execute the rename of all subfolders and their PDFs.
    /// </summary>
    public OperationResult<IReadOnlyList<FileRenameResult>> Rename(
        string rootFolder,
        FolderRenameSpec spec,
        IProgressReporter progress)
    {
        if (!_fileSystem.DirectoryExists(rootFolder))
            return OperationResult<IReadOnlyList<FileRenameResult>>.Failure(
                $"Directory not found: {rootFolder}");

        var rule       = new RenameRule(spec);
        var results    = new List<FileRenameResult>();
        var subfolders = _fileSystem.GetDirectories(rootFolder);
        var total      = subfolders.Count;

        for (var i = 0; i < total; i++)
        {
            var subfolder  = subfolders[i];
            var subName    = Path.GetFileName(subfolder)!;
            var newSubName = rule.GenerateSubfolderName(subName);

            if (newSubName is null)
            {
                progress.Report(new BatchProgress(total, i + 1, subName));
                continue;
            }

            // Rename PDFs first (while subfolder path is still the old one)
            var pdfFiles  = _fileSystem.GetFiles(subfolder, "*.pdf");
            var fileNames = pdfFiles.Select(Path.GetFileName).Where(f => f is not null).Select(f => f!).ToList();
            var renames   = rule.GeneratePdfNames(newSubName, fileNames);

            foreach (var (originalName, newName) in renames)
            {
                var oldPath = Path.Combine(subfolder, originalName);
                results.Add(_renamer.RenameFile(oldPath, newName));
            }

            // Rename the subfolder itself
            results.Add(_renamer.RenameDirectory(subfolder, newSubName));

            progress.Report(new BatchProgress(total, i + 1, subName));
        }

        return OperationResult<IReadOnlyList<FileRenameResult>>.Success(results);
    }
}

// ── DTOs (previously in Application Queries/Commands) ────────────────────────

public sealed record RenamePreview(
    string OriginalFolderName,
    string NewFolderName,
    IReadOnlyList<FileRenamePreviewItem> Files);

public sealed record FileRenamePreviewItem(
    string SubfolderName,
    string OriginalFileName,
    string NewFileName);
