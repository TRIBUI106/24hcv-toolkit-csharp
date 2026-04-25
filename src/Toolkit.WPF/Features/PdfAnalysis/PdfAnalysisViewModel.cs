using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using Toolkit.WPF.Common;

namespace Toolkit.WPF.Features.PdfAnalysis;

public sealed partial class PdfAnalysisViewModel : ViewModelBase
{
    private readonly PdfAnalysisService _service;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _folderPath      = string.Empty;
    [ObservableProperty] private bool   _isRunning;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _statusMessage   = "Ready";
    [ObservableProperty] private string _metadataTitle   = string.Empty;
    [ObservableProperty] private string _metadataAuthor  = string.Empty;
    [ObservableProperty] private string _metadataSubject = string.Empty;

    public ObservableCollection<PdfDocumentRow> Documents { get; } = [];

    public PdfAnalysisViewModel(PdfAnalysisService service)
    {
        _service = service;
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select a folder containing PDF files"
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            FolderPath = dialog.SelectedPath;
    }

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        _cts = new CancellationTokenSource();
        IsRunning = true;
        Documents.Clear();
        StatusMessage = "Analyzing...";

        var reporter = new WpfProgressReporter(p =>
        {
            ProgressPercent = p.PercentComplete;
            StatusMessage   = $"[{p.CompletedItems}/{p.TotalItems}] {p.CurrentItemName}";
        });

        var result = await Task.Run(() =>
            _service.AnalyzeFolderAsync(FolderPath, reporter, _cts.Token));

        IsRunning = false;

        if (result.IsSuccess)
        {
            foreach (var doc in result.Value!)
                Documents.Add(new PdfDocumentRow(doc));
            StatusMessage   = $"Done — {result.Value.Count} PDF(s) found.";
            ProgressPercent = 100;
        }
        else
        {
            StatusMessage = $"Error: {result.ErrorMessage}";
        }
    }

    private bool CanAnalyze() => !IsRunning && !string.IsNullOrWhiteSpace(FolderPath);

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private async Task ApplyMetadataAsync()
    {
        var selected = Documents.Where(d => d.IsSelected).Select(d => d.FilePath).ToList();
        if (selected.Count == 0)
        {
            System.Windows.MessageBox.Show("Select at least one PDF first.", "No selection",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var metadata = new PdfMetadata(
            string.IsNullOrWhiteSpace(MetadataTitle)   ? null : MetadataTitle,
            string.IsNullOrWhiteSpace(MetadataAuthor)  ? null : MetadataAuthor,
            string.IsNullOrWhiteSpace(MetadataSubject) ? null : MetadataSubject);

        IsRunning     = true;
        StatusMessage = "Writing metadata...";
        _cts          = new CancellationTokenSource();

        var reporter = new WpfProgressReporter(p =>
        {
            ProgressPercent = p.PercentComplete;
            StatusMessage   = p.StatusMessage ?? $"Writing {p.CurrentItemName}";
        });

        var result = await Task.Run(() =>
            _service.ApplyMetadataAsync(selected, metadata, reporter, _cts.Token));

        IsRunning     = false;
        StatusMessage = result.IsSuccess
            ? $"Metadata applied to {result.Value} file(s)."
            : $"Error: {result.ErrorMessage}";
    }

    partial void OnIsRunningChanged(bool value)
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    partial void OnFolderPathChanged(string value) =>
        AnalyzeCommand.NotifyCanExecuteChanged();
}

public sealed class PdfDocumentRow(PdfDocument doc)
{
    public bool   IsSelected { get; set; }
    public string FileName   => System.IO.Path.GetFileName(doc.FilePath.Value);
    public string FilePath   => doc.FilePath.Value;
    public int    PageCount  => doc.PageCount;
    public string Title      => doc.Metadata.Title   ?? string.Empty;
    public string Author     => doc.Metadata.Author  ?? string.Empty;
    public string Subject    => doc.Metadata.Subject ?? string.Empty;
    public string PageSizes  => string.Join(", ", doc.Pages
        .GroupBy(p => p.DetectedSize)
        .Select(g => $"{g.Key}×{g.Count()}"));
}
