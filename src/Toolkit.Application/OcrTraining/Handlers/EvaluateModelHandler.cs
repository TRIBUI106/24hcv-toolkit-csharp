using Toolkit.Application.OcrTraining.Interfaces;
using Toolkit.Application.OcrTraining.Queries;

namespace Toolkit.Application.OcrTraining.Handlers;

public sealed class EvaluateModelHandler
{
    private readonly IDatasetManager _datasetManager;

    public EvaluateModelHandler(IDatasetManager datasetManager)
    {
        _datasetManager = datasetManager;
    }

    public async Task<OperationResult<EvaluationMetrics>> HandleAsync(
        EvaluateModelQuery query,
        CancellationToken ct = default)
    {
        try
        {
            var metrics = await _datasetManager.EvaluateAsync(
                query.ModelPath, query.TestDataDirectory, ct);
            return OperationResult<EvaluationMetrics>.Success(metrics);
        }
        catch (Exception ex)
        {
            return OperationResult<EvaluationMetrics>.Failure(ex.Message);
        }
    }
}
