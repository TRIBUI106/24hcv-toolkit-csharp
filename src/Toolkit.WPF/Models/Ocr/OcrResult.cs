using Toolkit.WPF.Models.Common;

namespace Toolkit.WPF.Models.Ocr;

public sealed class OcrResult
{
    public FilePath SourceImage { get; }
    public string RecognizedText { get; }
    public ConfidenceScore Confidence { get; }
    public long ProcessingTimeMs { get; }

    public OcrResult(
        FilePath sourceImage,
        string recognizedText,
        ConfidenceScore confidence,
        long processingTimeMs)
    {
        SourceImage = sourceImage;
        RecognizedText = recognizedText;
        Confidence = confidence;
        ProcessingTimeMs = processingTimeMs;
    }
}
