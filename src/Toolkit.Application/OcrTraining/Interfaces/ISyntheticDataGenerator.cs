namespace Toolkit.Application.OcrTraining.Interfaces;

public interface ISyntheticDataGenerator
{
    Task GenerateAsync(
        string outputDirectory,
        int sampleCount,
        IReadOnlyList<string> fonts,
        IProgressReporter progress,
        CancellationToken ct = default);
}
