using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Toolkit.WPF.Common;

namespace Toolkit.WPF.Features.ImagePreprocessing;

public sealed partial class ImagePreprocessingViewModel : ViewModelBase
{
    private readonly PreprocessImagesHandler _handler;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _inputDirectory = string.Empty;
    [ObservableProperty] private string _outputDirectory = string.Empty;
    [ObservableProperty] private bool _deskew = true;
    [ObservableProperty] private bool _denoise = true;
    [ObservableProperty] private bool _applyClahe = true;
    [ObservableProperty] private bool _applyOtsu;
    [ObservableProperty] private int _targetDpi = 300;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _statusMessage = "Ready";

    public ObservableCollection<ProcessedImageRow> Results { get; } = [];

    public ImagePreprocessingViewModel(PreprocessImagesHandler handler)
    {
        _handler = handler;
    }

    [RelayCommand]
    private void BrowseInput()
    {
        var d = new System.Windows.Forms.FolderBrowserDialog { Description = "Select input folder" };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            InputDirectory = d.SelectedPath;
    }

    [RelayCommand]
    private void BrowseOutput()
    {
        var d = new System.Windows.Forms.FolderBrowserDialog { Description = "Select output folder" };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            OutputDirectory = d.SelectedPath;
    }

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private async Task ProcessAsync()
    {
        _cts = new CancellationTokenSource();
        IsRunning = true;
        Results.Clear();
        StatusMessage = "Processing...";

        var options = new PreprocessingOptions
        {
            Deskew = Deskew,
            Denoise = Denoise,
            ApplyClahe = ApplyClahe,
            ApplyOtsu = ApplyOtsu,
            TargetDpi = TargetDpi
        };

        var reporter = new WpfProgressReporter(p =>
        {
            ProgressPercent = p.PercentComplete;
            StatusMessage = $"[{p.CompletedItems}/{p.TotalItems}] {p.CurrentItemName}";
        });

        var result = await _handler.HandleAsync(
            new PreprocessImagesCommand(InputDirectory, OutputDirectory, options),
            reporter, _cts.Token);

        IsRunning = false;

        if (result.IsSuccess)
        {
            foreach (var img in result.Value!)
                Results.Add(new ProcessedImageRow(img));
            StatusMessage = $"Done — {result.Value.Count} image(s) processed.";
            ProgressPercent = 100;
        }
        else
        {
            StatusMessage = $"Error: {result.ErrorMessage}";
        }
    }

    private bool CanProcess() =>
        !IsRunning &&
        !string.IsNullOrWhiteSpace(InputDirectory) &&
        !string.IsNullOrWhiteSpace(OutputDirectory);

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    partial void OnIsRunningChanged(bool value) => ProcessCommand.NotifyCanExecuteChanged();
    partial void OnInputDirectoryChanged(string value) => ProcessCommand.NotifyCanExecuteChanged();
    partial void OnOutputDirectoryChanged(string value) => ProcessCommand.NotifyCanExecuteChanged();
}

public sealed class ProcessedImageRow(ProcessedImage img)
{
    public string FileName   => System.IO.Path.GetFileName(img.OutputPath.Value);
    public string Steps      => string.Join(", ", img.AppliedSteps);
    public long ProcessingMs => img.ProcessingTimeMs;
}
