using Toolkit.Application.OcrTraining.Commands;
using Toolkit.Application.OcrTraining.Interfaces;

namespace Toolkit.Application.OcrTraining.Handlers;

public sealed class StartTrainingHandler
{
    private readonly ITrainingRunner _runner;
    private readonly IDatasetManager _datasetManager;

    public StartTrainingHandler(ITrainingRunner runner, IDatasetManager datasetManager)
    {
        _runner = runner;
        _datasetManager = datasetManager;
    }

    public async Task<OperationResult<TrainingRun>> HandleAsync(
        StartTrainingCommand command,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        try
        {
            var split = await _datasetManager.SplitDatasetAsync(
                command.DatasetDirectory,
                Path.Combine(command.DatasetDirectory, "split"),
                ct: ct);

            var dataset = new TrainingDataset(
                command.ModelName,
                new FilePath(command.DatasetDirectory),
                split);

            var run = await _runner.StartTrainingAsync(dataset, command.ModelName, progress, ct);
            return OperationResult<TrainingRun>.Success(run);
        }
        catch (Exception ex)
        {
            return OperationResult<TrainingRun>.Failure(ex.Message);
        }
    }
}
