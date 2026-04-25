namespace Toolkit.WPF.Models.OcrTraining;

public sealed record DatasetSplit(int TrainCount, int ValidationCount, int TestCount)
{
    public int Total => TrainCount + ValidationCount + TestCount;
}
