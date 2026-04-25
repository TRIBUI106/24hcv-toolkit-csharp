namespace Toolkit.Application.OcrTraining.Interfaces;

public interface ITrainingRunner
{
    Task<TrainingRun> StartTrainingAsync(
        TrainingDataset dataset,
        string modelName,
        IProgressReporter progress,
        CancellationToken ct = default);
}

public sealed class TrainingRun
{
    public string ModelName { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
    public EvaluationMetrics? Metrics { get; set; }
    public bool IsRunning => CompletedAt is null;
    public string? ErrorMessage { get; set; }
}
