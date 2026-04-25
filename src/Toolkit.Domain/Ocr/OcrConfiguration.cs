namespace Toolkit.Domain.Ocr;

public sealed record OcrConfiguration(
    IReadOnlyList<string> Languages,
    OcrEngineMode EngineMode = OcrEngineMode.LstmOnly,
    PageSegmentationMode Psm = PageSegmentationMode.Auto);
