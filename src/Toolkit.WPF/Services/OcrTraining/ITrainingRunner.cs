using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.OcrTraining;

namespace Toolkit.WPF.Services.OcrTraining;

public interface ITrainingRunner
{
    Task<TrainingRun> StartTrainingAsync(
        TrainingDataset dataset,
        string modelName,
        IProgressReporter progress,
        CancellationToken ct = default);
}
