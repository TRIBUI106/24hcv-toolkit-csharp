using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.Ocr;

namespace Toolkit.WPF.Services.Ocr;

public sealed class OcrService
{
    private readonly IOcrEngine _ocrEngine;

    public OcrService(IOcrEngine ocrEngine)
    {
        _ocrEngine = ocrEngine;
    }

    public async Task<OperationResult<IReadOnlyList<OcrResult>>> RunAsync(
        IReadOnlyList<string> imagePaths,
        OcrConfiguration config,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        var results = new List<OcrResult>();
        var total   = imagePaths.Count;

        for (var i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            var path     = imagePaths[i];
            var fileName = Path.GetFileName(path);

            try
            {
                var result = await _ocrEngine.RecognizeAsync(new FilePath(path), config, ct);
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
