namespace Toolkit.Application.OcrTraining.Commands;

public sealed record GenerateSyntheticDataCommand(
    string OutputDirectory,
    int SampleCount,
    IReadOnlyList<string> Fonts);
