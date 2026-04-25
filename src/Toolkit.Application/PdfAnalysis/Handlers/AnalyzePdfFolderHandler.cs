using Toolkit.Application.PdfAnalysis.Interfaces;
using Toolkit.Application.PdfAnalysis.Queries;

namespace Toolkit.Application.PdfAnalysis.Handlers;

public sealed class AnalyzePdfFolderHandler
{
    private readonly IPdfReader _pdfReader;
    private readonly IFileSystemService _fileSystem;

    public AnalyzePdfFolderHandler(IPdfReader pdfReader, IFileSystemService fileSystem)
    {
        _pdfReader = pdfReader;
        _fileSystem = fileSystem;
    }

    public async Task<OperationResult<IReadOnlyList<PdfDocument>>> HandleAsync(
        AnalyzePdfFolderQuery query,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        if (!_fileSystem.DirectoryExists(query.FolderPath))
            return OperationResult<IReadOnlyList<PdfDocument>>.Failure($"Directory not found: {query.FolderPath}");

        var files = _fileSystem.GetFiles(query.FolderPath, "*.pdf", recursive: true);
        if (files.Count == 0)
            return OperationResult<IReadOnlyList<PdfDocument>>.Success(Array.Empty<PdfDocument>());

        var results = new PdfDocument[files.Count];
        var errors = new List<string>();
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
}
