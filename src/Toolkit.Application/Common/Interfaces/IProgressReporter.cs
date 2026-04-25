namespace Toolkit.Application.Common.Interfaces;

public interface IProgressReporter
{
    void Report(BatchProgress progress);
}

public sealed record BatchProgress(
    int TotalItems,
    int CompletedItems,
    string CurrentItemName,
    string? StatusMessage = null)
{
    public double PercentComplete =>
        TotalItems == 0 ? 0 : (double)CompletedItems / TotalItems * 100;
}
