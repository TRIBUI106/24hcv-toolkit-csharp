namespace Toolkit.Application.OcrTraining.Interfaces;

public interface IDatasetManager
{
    Task<DatasetSplit> SplitDatasetAsync(
        string sourceDirectory,
        string outputDirectory,
        double trainRatio = 0.8,
        double valRatio = 0.1,
        double testRatio = 0.1,
        CancellationToken ct = default);

    Task<EvaluationMetrics> EvaluateAsync(
        string modelPath,
        string testDataDirectory,
        CancellationToken ct = default);
}
