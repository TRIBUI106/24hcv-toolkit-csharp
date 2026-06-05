using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Toolkit.WPF.Common;
using Toolkit.WPF.Models.PdfAConverter;
using Toolkit.WPF.Services.PdfAConverter;

namespace Toolkit.WPF.Features.PdfAConverter;

public sealed partial class PdfAConverterViewModel : ViewModelBase
{
    private readonly PdfAConversionService _service;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _rootFolder      = string.Empty;
    [ObservableProperty] private bool   _inPlace         = true;
    [ObservableProperty] private string _outputFolder    = string.Empty;
    [ObservableProperty] private string _titleOverride   = string.Empty;
    [ObservableProperty] private string _authorOverride  = string.Empty;
    [ObservableProperty] private string _subjectOverride = string.Empty;
    [ObservableProperty] private bool   _isRunning;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _statusMessage   = "Ready";

    public ObservableCollection<PdfAResultRow> Results { get; } = [];

    public PdfAConverterViewModel(PdfAConversionService service)
    {
        _service = service;
    }

    [RelayCommand]
    private void BrowseRoot()
    {
        var d = new System.Windows.Forms.FolderBrowserDialog
            { Description = "Select root folder containing PDFs" };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            RootFolder = d.SelectedPath;
    }

    [RelayCommand]
    private void BrowseOutput()
    {
        var d = new System.Windows.Forms.FolderBrowserDialog
            { Description = "Select output folder" };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            OutputFolder = d.SelectedPath;
    }

    [RelayCommand(CanExecute = nameof(CanConvert))]
    private async Task ConvertAsync()
    {
        _cts = new CancellationTokenSource();
        IsRunning = true;
        Results.Clear();
        StatusMessage = "Converting...";

        var options = new PdfAConversionOptions
        {
            InPlace         = InPlace,
            OutputDirectory = OutputFolder,
            TitleOverride   = string.IsNullOrWhiteSpace(TitleOverride)   ? null : TitleOverride,
            AuthorOverride  = string.IsNullOrWhiteSpace(AuthorOverride)  ? null : AuthorOverride,
            SubjectOverride = string.IsNullOrWhiteSpace(SubjectOverride) ? null : SubjectOverride
        };

        var reporter = new WpfProgressReporter(p =>
        {
            ProgressPercent = p.PercentComplete;
            StatusMessage   = $"[{p.CompletedItems}/{p.TotalItems}] {p.CurrentItemName}";
        });

        var result = await _service.ConvertFolderAsync(RootFolder, options, reporter, _cts.Token);

        IsRunning = false;

        if (result.IsSuccess)
        {
            foreach (var r in result.Value!)
                Results.Add(new PdfAResultRow(r));

            var converted = result.Value.Count(r => r.Status == ConversionStatus.Converted);
            var skipped   = result.Value.Count(r => r.Status == ConversionStatus.Skipped);
            var errors    = result.Value.Count(r => r.Status == ConversionStatus.Error);
            StatusMessage   = $"Done — {converted} converted, {skipped} skipped, {errors} error(s).";
            ProgressPercent = 100;
        }
        else
        {
            StatusMessage = $"Error: {result.ErrorMessage}";
        }
    }

    private bool CanConvert() =>
        !IsRunning &&
        !string.IsNullOrWhiteSpace(RootFolder) &&
        (InPlace || !string.IsNullOrWhiteSpace(OutputFolder));

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    partial void OnIsRunningChanged(bool value)      => ConvertCommand.NotifyCanExecuteChanged();
    partial void OnRootFolderChanged(string value)   => ConvertCommand.NotifyCanExecuteChanged();
    partial void OnInPlaceChanged(bool value)        => ConvertCommand.NotifyCanExecuteChanged();
    partial void OnOutputFolderChanged(string value) => ConvertCommand.NotifyCanExecuteChanged();
}

public sealed class PdfAResultRow(PdfAConversionResult r)
{
    public string FileName      => Path.GetFileName(r.SourcePath);
    public string Status        => r.Status.ToString();
    public long   ProcessingMs  => r.ProcessingMs;
    public string Error         => r.ErrorMessage ?? string.Empty;
}
