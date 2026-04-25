using System.Diagnostics;
using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.OcrTraining;

namespace Toolkit.WPF.Services.OcrTraining;

public sealed class TesseractTrainingRunner : ITrainingRunner
{
    private readonly string _tesseractBinPath;

    public TesseractTrainingRunner(string tesseractBinPath = "tesseract")
    {
        _tesseractBinPath = tesseractBinPath;
    }

    public async Task<TrainingRun> StartTrainingAsync(
        TrainingDataset dataset,
        string modelName,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        var run = new TrainingRun
        {
            ModelName = modelName,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            var outputDir = Path.Combine(dataset.RootDirectory.Value, "output");
            Directory.CreateDirectory(outputDir);

            // Generate .lstmf files from training images
            var trainDir    = Path.Combine(dataset.RootDirectory.Value, "split", "train");
            var trainImages = Directory.GetFiles(trainDir, "*.png");
            var total       = trainImages.Length;

            progress.Report(new BatchProgress(total, 0, "Generating LSTM training files..."));

            await Task.Run(async () =>
            {
                for (var i = 0; i < trainImages.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var img      = trainImages[i];
                    var baseName = Path.GetFileNameWithoutExtension(img);
                    var dirName  = Path.GetDirectoryName(img)!;

                    await RunProcessAsync(
                        _tesseractBinPath,
                        $"\"{img}\" \"{Path.Combine(dirName, baseName)}\" --psm 7 lstm.train",
                        ct);

                    progress.Report(new BatchProgress(total, i + 1, baseName));
                }
            }, ct);

            // Combine .lstmf files list
            var lstmfFiles = Directory.GetFiles(trainDir, "*.lstmf");
            var listFile   = Path.Combine(outputDir, "training.list");
            await File.WriteAllLinesAsync(listFile, lstmfFiles, ct);

            progress.Report(new BatchProgress(1, 0, "Starting LSTM training...",
                $"Training {lstmfFiles.Length} files → model '{modelName}'"));

            // Run lstmtraining
            var checkpointDir = Path.Combine(outputDir, "checkpoints");
            Directory.CreateDirectory(checkpointDir);

            await RunProcessAsync(
                "lstmtraining",
                $"--model_output \"{Path.Combine(checkpointDir, modelName)}\" " +
                $"--traineddata \"{Path.Combine("tessdata", "vie.traineddata")}\" " +
                $"--train_listfile \"{listFile}\" " +
                $"--max_iterations 400",
                ct);

            // Combine checkpoint to traineddata
            await RunProcessAsync(
                "lstmtraining",
                $"--stop_training " +
                $"--continue_from \"{Path.Combine(checkpointDir, modelName)}_checkpoint\" " +
                $"--traineddata \"{Path.Combine("tessdata", "vie.traineddata")}\" " +
                $"--model_output \"{Path.Combine(outputDir, modelName + ".traineddata")}\"",
                ct);

            run.CompletedAt = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            run.CompletedAt   = DateTime.UtcNow;
            run.ErrorMessage  = "Training cancelled.";
        }
        catch (Exception ex)
        {
            run.CompletedAt  = DateTime.UtcNow;
            run.ErrorMessage = ex.Message;
        }

        return run;
    }

    private static async Task RunProcessAsync(string exe, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {exe}");

        await proc.WaitForExitAsync(ct);

        if (proc.ExitCode != 0)
        {
            var err = await proc.StandardError.ReadToEndAsync(ct);
            throw new InvalidOperationException($"{exe} exited with code {proc.ExitCode}: {err}");
        }
    }
}
