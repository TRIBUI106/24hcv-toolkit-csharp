using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Toolkit.WPF.Common;

namespace Toolkit.WPF.Features.OcrTraining;

public sealed partial class OcrTrainingViewModel : ViewModelBase
{
    private readonly GenerateSyntheticDataHandler _generateHandler;
    private readonly StartTrainingHandler _trainingHandler;
    private readonly SplitDatasetHandler _splitHandler;
    private readonly EvaluateModelHandler _evaluateHandler;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _generateOutputDir = string.Empty;
    [ObservableProperty] private int _sampleCount = 500;
    [ObservableProperty] private string _fontList = "Arial, Times New Roman, Courier New";
    [ObservableProperty] private string _datasetDirectory = string.Empty;
    [ObservableProperty] private string _modelName = "vie_custom";
    [ObservableProperty] private string _modelPath = string.Empty;
    [ObservableProperty] private string _testDataDirectory = string.Empty;
    [ObservableProperty] private string _evaluationResult = string.Empty;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private string _trainingLog = string.Empty;

    public OcrTrainingViewModel(
        GenerateSyntheticDataHandler generateHandler,
        StartTrainingHandler trainingHandler,
        SplitDatasetHandler splitHandler,
        EvaluateModelHandler evaluateHandler)
    {
        _generateHandler = generateHandler;
        _trainingHandler = trainingHandler;
        _splitHandler = splitHandler;
        _evaluateHandler = evaluateHandler;
    }

    [RelayCommand]
    private void BrowseGenerateOutput()
    {
        var d = new System.Windows.Forms.FolderBrowserDialog { Description = "Output folder for generated data" };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) GenerateOutputDir = d.SelectedPath;
    }

    [RelayCommand]
    private void BrowseDataset()
    {
        var d = new System.Windows.Forms.FolderBrowserDialog { Description = "Select dataset folder" };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) DatasetDirectory = d.SelectedPath;
    }

    [RelayCommand]
    private void BrowseModel()
    {
        var d = new System.Windows.Forms.OpenFileDialog
        {
            Filter = "Tesseract Model|*.traineddata",
            Title = "Select trained model"
        };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) ModelPath = d.FileName;
    }

    [RelayCommand]
    private void BrowseTestData()
    {
        var d = new System.Windows.Forms.FolderBrowserDialog { Description = "Select test data folder" };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) TestDataDirectory = d.SelectedPath;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task GenerateDataAsync()
    {
        if (string.IsNullOrWhiteSpace(GenerateOutputDir)) return;

        var fonts = FontList
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();

        _cts = new CancellationTokenSource();
        IsRunning = true;
        StatusMessage = "Generating synthetic data...";
        TrainingLog = string.Empty;

        var reporter = new WpfProgressReporter(p =>
        {
            ProgressPercent = p.PercentComplete;
            StatusMessage = $"[{p.CompletedItems}/{p.TotalItems}] {p.CurrentItemName}";
            TrainingLog += $"{p.CurrentItemName}\n";
        });

        var result = await _generateHandler.HandleAsync(
            new GenerateSyntheticDataCommand(GenerateOutputDir, SampleCount, fonts),
            reporter, _cts.Token);

        IsRunning = false;
        StatusMessage = result.IsSuccess
            ? $"Generated {SampleCount} samples in {GenerateOutputDir}"
            : $"Error: {result.ErrorMessage}";
        ProgressPercent = 100;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task TrainAsync()
    {
        if (string.IsNullOrWhiteSpace(DatasetDirectory) || string.IsNullOrWhiteSpace(ModelName)) return;

        _cts = new CancellationTokenSource();
        IsRunning = true;
        StatusMessage = "Starting training...";
        TrainingLog = string.Empty;

        var reporter = new WpfProgressReporter(p =>
        {
            ProgressPercent = p.PercentComplete;
            StatusMessage = p.StatusMessage ?? $"[{p.CompletedItems}/{p.TotalItems}] {p.CurrentItemName}";
            TrainingLog += $"{p.CurrentItemName}\n";
        });

        var result = await _trainingHandler.HandleAsync(
            new StartTrainingCommand(DatasetDirectory, ModelName),
            reporter, _cts.Token);

        IsRunning = false;

        if (result.IsSuccess)
        {
            var run = result.Value!;
            StatusMessage = run.ErrorMessage is null
                ? $"Training complete. Duration: {(run.CompletedAt - run.StartedAt)?.TotalMinutes:F1} min"
                : $"Training ended: {run.ErrorMessage}";
        }
        else
        {
            StatusMessage = $"Error: {result.ErrorMessage}";
        }
        ProgressPercent = 100;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task EvaluateAsync()
    {
        if (string.IsNullOrWhiteSpace(ModelPath) || string.IsNullOrWhiteSpace(TestDataDirectory)) return;

        _cts = new CancellationTokenSource();
        IsRunning = true;
        StatusMessage = "Evaluating model...";

        var result = await _evaluateHandler.HandleAsync(
            new EvaluateModelQuery(ModelPath, TestDataDirectory), _cts.Token);

        IsRunning = false;

        if (result.IsSuccess)
        {
            var m = result.Value!;
            EvaluationResult =
                $"Samples: {m.TotalSamples}\n" +
                $"CER:     {m.Cer:P2}\n" +
                $"WER:     {m.Wer:P2}\n" +
                $"Accuracy:{m.Accuracy:P2}";
            StatusMessage = "Evaluation complete.";
        }
        else
        {
            StatusMessage = $"Error: {result.ErrorMessage}";
        }
        ProgressPercent = 100;
    }

    private bool CanRun() => !IsRunning;

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    partial void OnIsRunningChanged(bool value)
    {
        GenerateDataCommand.NotifyCanExecuteChanged();
        TrainCommand.NotifyCanExecuteChanged();
        EvaluateCommand.NotifyCanExecuteChanged();
    }
}
