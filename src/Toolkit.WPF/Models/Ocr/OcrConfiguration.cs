using Toolkit.WPF.Models.Common;

namespace Toolkit.WPF.Models.Ocr;

public sealed record OcrConfiguration(
    IReadOnlyList<string> Languages,
    OcrEngineMode EngineMode = OcrEngineMode.LstmOnly,
    PageSegmentationMode Psm = PageSegmentationMode.Auto);
