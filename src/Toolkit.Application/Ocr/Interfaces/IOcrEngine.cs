namespace Toolkit.Application.Ocr.Interfaces;

public interface IOcrEngine
{
    Task<OcrResult> RecognizeAsync(
        FilePath imagePath,
        OcrConfiguration config,
        CancellationToken ct = default);
}
