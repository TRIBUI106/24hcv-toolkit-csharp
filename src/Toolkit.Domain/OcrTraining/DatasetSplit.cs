namespace Toolkit.Domain.OcrTraining;

public sealed record DatasetSplit(int TrainCount, int ValidationCount, int TestCount)
{
    public int Total => TrainCount + ValidationCount + TestCount;
}
