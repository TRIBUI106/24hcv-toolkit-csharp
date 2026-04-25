namespace Toolkit.WPF.Models.OcrTraining;

public sealed record EvaluationMetrics(double Cer, double Wer, int TotalSamples)
{
    public double Accuracy => 1.0 - Cer;
}
