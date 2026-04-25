namespace Toolkit.Application.OcrTraining.Commands;

public sealed record SplitDatasetCommand(
    string SourceDirectory,
    string OutputDirectory,
    double TrainRatio = 0.8,
    double ValRatio = 0.1,
    double TestRatio = 0.1);
