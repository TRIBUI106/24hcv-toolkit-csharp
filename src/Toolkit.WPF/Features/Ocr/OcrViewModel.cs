using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Toolkit.WPF.Common;

namespace Toolkit.WPF.Features.Ocr;

public sealed partial class OcrViewModel : ViewModelBase
{
    private readonly RunOcrHandler _handler;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _imagePaths = string.Empty;
    [ObservableProperty] private OcrEngineMode _engineMode = OcrEngineMode.LstmOnly;
    [ObservableProperty] private PageSegmentationMode _psm = PageSegmentationMode.Auto;
    [ObservableProperty] private bool _useVietnamese = true;
    [ObservableProperty] private bool _useEnglish;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _statusMessage = "Ready";

    public ObservableCollection<OcrResultRow> Results { get; } = [];

    public IReadOnlyList<OcrEngineMode> EngineModes { get; } =
        Enum.GetValues<OcrEngineMode>().ToList();

    public IReadOnlyList<PageSegmentationMode> PsmModes { get; } =
        Enum.GetValues<PageSegmentationMode>().ToList();

    public OcrViewModel(RunOcrHandler handler)
    {
        _handler = handler;
    }

    [RelayCommand]
    private void BrowseImages()
    {
        var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Multiselect = true,
            Filter = "Images|*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp",
            Title = "Select images to OCR"
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            ImagePaths = string.Join(Environment.NewLine, dialog.FileNames);
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        var paths = ImagePaths
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (paths.Count == 0) return;

        var langs = new List<string>();
        if (UseVietnamese) langs.Add("vie");
        if (UseEnglish) langs.Add("eng");
        if (langs.Count == 0) langs.Add("vie");

        var config = new OcrConfiguration(langs, EngineMode, Psm);

        _cts = new CancellationTokenSource();
        IsRunning = true;
        Results.Clear();
        StatusMessage = "Running OCR...";

        var reporter = new WpfProgressReporter(p =>
        {
            ProgressPercent = p.PercentComplete;
            StatusMessage = $"[{p.CompletedItems}/{p.TotalItems}] {p.CurrentItemName}";
        });

        var result = await _handler.HandleAsync(new RunOcrCommand(paths, config), reporter, _cts.Token);

        IsRunning = false;

        if (result.IsSuccess)
        {
            foreach (var r in result.Value!)
                Results.Add(new OcrResultRow(r));
            StatusMessage = $"Done — {result.Value.Count} image(s) processed.";
            ProgressPercent = 100;
        }
        else
        {
            StatusMessage = $"Error: {result.ErrorMessage}";
        }
    }

    private bool CanRun() => !IsRunning && !string.IsNullOrWhiteSpace(ImagePaths);

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    partial void OnIsRunningChanged(bool value) => RunCommand.NotifyCanExecuteChanged();
    partial void OnImagePathsChanged(string value) => RunCommand.NotifyCanExecuteChanged();
}

public sealed class OcrResultRow(OcrResult result)
{
    public string FileName   => System.IO.Path.GetFileName(result.SourceImage.Value);
    public string Text       => result.RecognizedText;
    public float Confidence  => result.Confidence.Value;
    public long ProcessingMs => result.ProcessingTimeMs;
}
