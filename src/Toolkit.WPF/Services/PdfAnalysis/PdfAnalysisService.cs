using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.PdfAnalysis;

namespace Toolkit.WPF.Services.PdfAnalysis;

public sealed class PdfAnalysisService
{
    private readonly IPdfReader _pdfReader;
    private readonly IPdfMetadataWriter _metadataWriter;
    private readonly Services.BatchRenaming.IFileSystemService _fileSystem;

    public PdfAnalysisService(
        IPdfReader pdfReader,
        IPdfMetadataWriter metadataWriter,
        Services.BatchRenaming.IFileSystemService fileSystem)
    {
        _pdfReader      = pdfReader;
        _metadataWriter = metadataWriter;
        _fileSystem     = fileSystem;
    }

    /// <summary>
    /// Read and analyse all PDFs found recursively under <paramref name="folderPath"/>.
    /// </summary>
    public async Task<OperationResult<IReadOnlyList<PdfDocument>>> AnalyzeFolderAsync(
        string folderPath,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        if (!_fileSystem.DirectoryExists(folderPath))
            return OperationResult<IReadOnlyList<PdfDocument>>.Failure($"Directory not found: {folderPath}");

        var files = _fileSystem.GetFiles(folderPath, "*.pdf", recursive: true);
        if (files.Count == 0)
            return OperationResult<IReadOnlyList<PdfDocument>>.Success(Array.Empty<PdfDocument>());

        var results   = new PdfDocument[files.Count];
        var errors    = new List<string>();
        var completed = 0;

        await Parallel.ForEachAsync(
            files.Select((path, index) => (path, index)),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = ct },
            async (item, token) =>
            {
                try
                {
                    var doc = await _pdfReader.ReadAsync(new FilePath(item.path), token);
                    results[item.index] = doc;
                }
                catch (Exception ex)
                {
                    lock (errors) errors.Add($"{Path.GetFileName(item.path)}: {ex.Message}");
                }
                finally
                {
                    var count = Interlocked.Increment(ref completed);
                    progress.Report(new BatchProgress(files.Count, count, Path.GetFileName(item.path)));
                }
            });

        var successful = results.Where(d => d is not null).ToList();
        return OperationResult<IReadOnlyList<PdfDocument>>.Success(successful);
    }

    /// <summary>
    /// Write PDF metadata to a list of files.
    /// </summary>
    public async Task<OperationResult<int>> ApplyMetadataAsync(
        IReadOnlyList<string> filePaths,
        PdfMetadata metadata,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        var succeeded = 0;
        var total     = filePaths.Count;

        for (var i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            var path = filePaths[i];
            try
            {
                await _metadataWriter.WriteMetadataAsync(new FilePath(path), metadata, ct);
                Interlocked.Increment(ref succeeded);
            }
            catch (Exception ex)
            {
                progress.Report(new BatchProgress(total, i + 1, Path.GetFileName(path),
                    $"Error: {ex.Message}"));
                continue;
            }
            progress.Report(new BatchProgress(total, i + 1, Path.GetFileName(path)));
        }

        return OperationResult<int>.Success(succeeded);
    }
}
