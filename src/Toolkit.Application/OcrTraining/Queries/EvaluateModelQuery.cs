namespace Toolkit.Application.OcrTraining.Queries;

public sealed record EvaluateModelQuery(string ModelPath, string TestDataDirectory);
