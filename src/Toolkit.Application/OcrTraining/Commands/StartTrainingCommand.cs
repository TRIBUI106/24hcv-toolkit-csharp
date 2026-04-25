namespace Toolkit.Application.OcrTraining.Commands;

public sealed record StartTrainingCommand(
    string DatasetDirectory,
    string ModelName,
    string BaseModel = "vie");
