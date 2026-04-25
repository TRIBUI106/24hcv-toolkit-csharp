using Toolkit.Application.ImagePreprocessing.Commands;
using Toolkit.Application.ImagePreprocessing.Interfaces;

namespace Toolkit.Application.ImagePreprocessing.Handlers;

public sealed class PreprocessImagesHandler
{
    private readonly IImageProcessor _processor;
    private readonly IFileSystemService _fileSystem;

    public PreprocessImagesHandler(IImageProcessor processor, IFileSystemService fileSystem)
    {
        _processor = processor;
        _fileSystem = fileSystem;
    }

    public async Task<OperationResult<IReadOnlyList<ProcessedImage>>> HandleAsync(
        PreprocessImagesCommand command,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        if (!_fileSystem.DirectoryExists(command.InputDirectory))
            return OperationResult<IReadOnlyList<ProcessedImage>>.Failure(
                $"Input directory not found: {command.InputDirectory}");

        var patterns = command.SearchPattern.Split(';');
        var allFiles = patterns
            .SelectMany(p => _fileSystem.GetFiles(command.InputDirectory, p, recursive: true))
            .Distinct()
            .ToList();

        if (allFiles.Count == 0)
            return OperationResult<IReadOnlyList<ProcessedImage>>.Success(Array.Empty<ProcessedImage>());

        if (!_fileSystem.DirectoryExists(command.OutputDirectory))
            Directory.CreateDirectory(command.OutputDirectory);

        var results = new List<ProcessedImage>();
        var completed = 0;

        foreach (var file in allFiles)
        {
            ct.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(file);
            var outputPath = Path.Combine(command.OutputDirectory, fileName);

            try
            {
                var processed = await _processor.ProcessAsync(
                    new FilePath(file), new FilePath(outputPath), command.Options, ct);
                lock (results) results.Add(processed);
            }
            catch (Exception ex)
            {
                progress.Report(new BatchProgress(allFiles.Count, completed, fileName,
                    $"Error: {ex.Message}"));
            }

            var count = Interlocked.Increment(ref completed);
            progress.Report(new BatchProgress(allFiles.Count, count, fileName));
        }

        return OperationResult<IReadOnlyList<ProcessedImage>>.Success(results);
    }
}
