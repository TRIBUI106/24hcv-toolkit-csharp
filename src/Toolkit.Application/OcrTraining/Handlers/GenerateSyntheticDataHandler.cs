using Toolkit.Application.OcrTraining.Commands;
using Toolkit.Application.OcrTraining.Interfaces;

namespace Toolkit.Application.OcrTraining.Handlers;

public sealed class GenerateSyntheticDataHandler
{
    private readonly ISyntheticDataGenerator _generator;

    public GenerateSyntheticDataHandler(ISyntheticDataGenerator generator)
    {
        _generator = generator;
    }

    public async Task<OperationResult<bool>> HandleAsync(
        GenerateSyntheticDataCommand command,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        try
        {
            await _generator.GenerateAsync(
                command.OutputDirectory,
                command.SampleCount,
                command.Fonts,
                progress,
                ct);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure(ex.Message);
        }
    }
}
