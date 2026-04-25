using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.Ocr;

namespace Toolkit.WPF.Services.Ocr;

public interface IOcrEngine
{
    Task<OcrResult> RecognizeAsync(
        FilePath imagePath,
        OcrConfiguration config,
        CancellationToken ct = default);
}
