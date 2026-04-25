using Toolkit.Application.BatchRenaming.Commands;
using Toolkit.Application.BatchRenaming.Interfaces;

namespace Toolkit.Application.BatchRenaming.Handlers;

public sealed class RenameFolderHandler
{
    private readonly IFileRenamer _renamer;
    private readonly IFileSystemService _fileSystem;

    public RenameFolderHandler(IFileRenamer renamer, IFileSystemService fileSystem)
    {
        _renamer = renamer;
        _fileSystem = fileSystem;
    }

    public OperationResult<IReadOnlyList<FileRenameResult>> Handle(
        RenameFolderCommand command,
        IProgressReporter progress)
    {
        if (!_fileSystem.DirectoryExists(command.RootFolder))
            return OperationResult<IReadOnlyList<FileRenameResult>>.Failure(
                $"Directory not found: {command.RootFolder}");

        var rule = new RenameRule(command.Spec);
        var results = new List<FileRenameResult>();

        // Rename PDFs inside each subfolder first
        var subfolders = _fileSystem.GetDirectories(command.RootFolder);
        var totalSubfolders = subfolders.Count;

        for (var i = 0; i < totalSubfolders; i++)
        {
            var subfolder = subfolders[i];
            var pdfFiles = _fileSystem.GetFiles(subfolder, "*.pdf");
            var fileNames = pdfFiles.Select(Path.GetFileName).Where(f => f is not null).Select(f => f!).ToList();
            var renames = rule.GenerateSequentialNames(fileNames);

            foreach (var (originalName, newName) in renames)
            {
                var oldPath = Path.Combine(subfolder, originalName);
                var result = _renamer.RenameFile(oldPath, newName);
                results.Add(result);
            }

            progress.Report(new BatchProgress(totalSubfolders, i + 1, Path.GetFileName(subfolder)!));
        }

        // Rename the root folder last
        var folderResult = _renamer.RenameDirectory(command.RootFolder, rule.GenerateFolderName());
        results.Add(folderResult);

        return OperationResult<IReadOnlyList<FileRenameResult>>.Success(results);
    }
}
