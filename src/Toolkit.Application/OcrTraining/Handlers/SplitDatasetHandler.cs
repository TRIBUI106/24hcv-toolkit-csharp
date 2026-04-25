using Toolkit.Application.OcrTraining.Commands;
using Toolkit.Application.OcrTraining.Interfaces;

namespace Toolkit.Application.OcrTraining.Handlers;

public sealed class SplitDatasetHandler
{
    private readonly IDatasetManager _datasetManager;

    public SplitDatasetHandler(IDatasetManager datasetManager)
    {
        _datasetManager = datasetManager;
    }

    public async Task<OperationResult<DatasetSplit>> HandleAsync(
        SplitDatasetCommand command,
        CancellationToken ct = default)
    {
        try
        {
            var split = await _datasetManager.SplitDatasetAsync(
                command.SourceDirectory,
                command.OutputDirectory,
                command.TrainRatio,
                command.ValRatio,
                command.TestRatio,
                ct);
            return OperationResult<DatasetSplit>.Success(split);
        }
        catch (Exception ex)
        {
            return OperationResult<DatasetSplit>.Failure(ex.Message);
        }
    }
}
