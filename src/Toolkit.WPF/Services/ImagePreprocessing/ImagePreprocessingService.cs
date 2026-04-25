using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.ImagePreprocessing;

namespace Toolkit.WPF.Services.ImagePreprocessing;

public sealed class ImagePreprocessingService
{
    private readonly IImageProcessor _processor;
    private readonly Services.BatchRenaming.IFileSystemService _fileSystem;

    public ImagePreprocessingService(
        IImageProcessor processor,
        Services.BatchRenaming.IFileSystemService fileSystem)
    {
        _processor  = processor;
        _fileSystem = fileSystem;
    }

    public async Task<OperationResult<IReadOnlyList<ProcessedImage>>> ProcessAsync(
        string inputDirectory,
        string outputDirectory,
        PreprocessingOptions options,
        IProgressReporter progress,
        string searchPattern = "*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp",
        CancellationToken ct = default)
    {
        if (!_fileSystem.DirectoryExists(inputDirectory))
            return OperationResult<IReadOnlyList<ProcessedImage>>.Failure(
                $"Input directory not found: {inputDirectory}");

        var patterns = searchPattern.Split(';');
        var allFiles = patterns
            .SelectMany(p => _fileSystem.GetFiles(inputDirectory, p, recursive: true))
            .Distinct()
            .ToList();

        if (allFiles.Count == 0)
            return OperationResult<IReadOnlyList<ProcessedImage>>.Success(Array.Empty<ProcessedImage>());

        if (!_fileSystem.DirectoryExists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var results   = new List<ProcessedImage>();
        var completed = 0;

        foreach (var file in allFiles)
        {
            ct.ThrowIfCancellationRequested();
            var fileName   = Path.GetFileName(file);
            var outputPath = Path.Combine(outputDirectory, fileName);

            try
            {
                var processed = await _processor.ProcessAsync(
                    new FilePath(file), new FilePath(outputPath), options, ct);
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
