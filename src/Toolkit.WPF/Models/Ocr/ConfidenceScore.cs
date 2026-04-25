namespace Toolkit.WPF.Models.Ocr;

public sealed record ConfidenceScore(float Value)
{
    public bool IsHighConfidence => Value >= 80f;
}
