namespace Toolkit.WPF.Models.Common;

public sealed record BatchProgress(
    int TotalItems,
    int CompletedItems,
    string CurrentItemName,
    string? StatusMessage = null)
{
    public double PercentComplete =>
        TotalItems == 0 ? 0 : (double)CompletedItems / TotalItems * 100;
}
