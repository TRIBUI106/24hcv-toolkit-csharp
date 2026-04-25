using Toolkit.Application.Ocr.Commands;
using Toolkit.Application.Ocr.Interfaces;

namespace Toolkit.Application.Ocr.Handlers;

public sealed class RunOcrHandler
{
    private readonly IOcrEngine _ocrEngine;

    public RunOcrHandler(IOcrEngine ocrEngine)
    {
        _ocrEngine = ocrEngine;
    }

    public async Task<OperationResult<IReadOnlyList<OcrResult>>> HandleAsync(
        RunOcrCommand command,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        var results = new List<OcrResult>();
        var total = command.ImagePaths.Count;

        for (var i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            var path = command.ImagePaths[i];
            var fileName = Path.GetFileName(path);

            try
            {
                var result = await _ocrEngine.RecognizeAsync(new FilePath(path), command.Config, ct);
                lock (results) results.Add(result);
            }
            catch (Exception ex)
            {
                progress.Report(new BatchProgress(total, i + 1, fileName, $"Error: {ex.Message}"));
                continue;
            }

            progress.Report(new BatchProgress(total, i + 1, fileName));
        }

        return OperationResult<IReadOnlyList<OcrResult>>.Success(results);
    }
}
