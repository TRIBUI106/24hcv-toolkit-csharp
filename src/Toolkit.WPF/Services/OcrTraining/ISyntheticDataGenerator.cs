using Toolkit.WPF.Models.Common;

namespace Toolkit.WPF.Services.OcrTraining;

public interface ISyntheticDataGenerator
{
    Task GenerateAsync(
        string outputDirectory,
        int sampleCount,
        IReadOnlyList<string> fonts,
        IProgressReporter progress,
        CancellationToken ct = default);
}
