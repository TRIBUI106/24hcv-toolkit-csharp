using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.OcrTraining;

namespace Toolkit.WPF.Services.OcrTraining;

public sealed class OcrTrainingService
{
    private readonly ISyntheticDataGenerator _generator;
    private readonly IDatasetManager _datasetManager;
    private readonly ITrainingRunner _runner;

    public OcrTrainingService(
        ISyntheticDataGenerator generator,
        IDatasetManager datasetManager,
        ITrainingRunner runner)
    {
        _generator      = generator;
        _datasetManager = datasetManager;
        _runner         = runner;
    }

    public async Task<OperationResult<bool>> GenerateDataAsync(
        string outputDirectory,
        int sampleCount,
        IReadOnlyList<string> fonts,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        try
        {
            await _generator.GenerateAsync(outputDirectory, sampleCount, fonts, progress, ct);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<DatasetSplit>> SplitDatasetAsync(
        string sourceDirectory,
        string outputDirectory,
        double trainRatio = 0.8,
        double valRatio   = 0.1,
        double testRatio  = 0.1,
        CancellationToken ct = default)
    {
        try
        {
            var split = await _datasetManager.SplitDatasetAsync(
                sourceDirectory, outputDirectory, trainRatio, valRatio, testRatio, ct);
            return OperationResult<DatasetSplit>.Success(split);
        }
        catch (Exception ex)
        {
            return OperationResult<DatasetSplit>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<TrainingRun>> TrainAsync(
        string datasetDirectory,
        string modelName,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        try
        {
            var split = await _datasetManager.SplitDatasetAsync(
                datasetDirectory,
                Path.Combine(datasetDirectory, "split"),
                ct: ct);

            var dataset = new TrainingDataset(
                modelName,
                new FilePath(datasetDirectory),
                split);

            var run = await _runner.StartTrainingAsync(dataset, modelName, progress, ct);
            return OperationResult<TrainingRun>.Success(run);
        }
        catch (Exception ex)
        {
            return OperationResult<TrainingRun>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<EvaluationMetrics>> EvaluateAsync(
        string modelPath,
        string testDataDirectory,
        CancellationToken ct = default)
    {
        try
        {
            var metrics = await _datasetManager.EvaluateAsync(modelPath, testDataDirectory, ct);
            return OperationResult<EvaluationMetrics>.Success(metrics);
        }
        catch (Exception ex)
        {
            return OperationResult<EvaluationMetrics>.Failure(ex.Message);
        }
    }
}
