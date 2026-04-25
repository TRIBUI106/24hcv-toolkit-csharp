using Toolkit.Application.PdfAnalysis.Commands;
using Toolkit.Application.PdfAnalysis.Interfaces;

namespace Toolkit.Application.PdfAnalysis.Handlers;

public sealed class ApplyPdfMetadataHandler
{
    private readonly IPdfMetadataWriter _metadataWriter;

    public ApplyPdfMetadataHandler(IPdfMetadataWriter metadataWriter)
    {
        _metadataWriter = metadataWriter;
    }

    public async Task<OperationResult<int>> HandleAsync(
        ApplyPdfMetadataCommand command,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        var succeeded = 0;
        var total = command.FilePaths.Count;

        for (var i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            var path = command.FilePaths[i];
            try
            {
                await _metadataWriter.WriteMetadataAsync(new FilePath(path), command.Metadata, ct);
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
