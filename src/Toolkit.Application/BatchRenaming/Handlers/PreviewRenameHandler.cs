using Toolkit.Application.BatchRenaming.Interfaces;
using Toolkit.Application.BatchRenaming.Queries;

namespace Toolkit.Application.BatchRenaming.Handlers;

public sealed class PreviewRenameHandler
{
    private readonly IFileSystemService _fileSystem;

    public PreviewRenameHandler(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public OperationResult<RenamePreview> Handle(PreviewRenameQuery query)
    {
        if (!_fileSystem.DirectoryExists(query.RootFolder))
            return OperationResult<RenamePreview>.Failure($"Directory not found: {query.RootFolder}");

        var rule = new RenameRule(query.Spec);
        var originalName = Path.GetFileName(query.RootFolder)!;
        var newFolderName = rule.GenerateFolderName();

        var subfolders = _fileSystem.GetDirectories(query.RootFolder);
        var previewItems = new List<FileRenamePreviewItem>();

        foreach (var subfolder in subfolders)
        {
            var subName = Path.GetFileName(subfolder)!;
            var pdfFiles = _fileSystem.GetFiles(subfolder, "*.pdf");
            var fileNames = pdfFiles.Select(Path.GetFileName).Where(f => f is not null).Select(f => f!).ToList();
            var renames = rule.GenerateSequentialNames(fileNames);

            foreach (var (orig, newName) in renames)
                previewItems.Add(new FileRenamePreviewItem(subName, orig, newName));
        }

        return OperationResult<RenamePreview>.Success(
            new RenamePreview(originalName, newFolderName, previewItems));
    }
}
