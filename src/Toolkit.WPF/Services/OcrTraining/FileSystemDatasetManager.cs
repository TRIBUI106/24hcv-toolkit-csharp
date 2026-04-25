using System.Diagnostics;
using Toolkit.WPF.Models.OcrTraining;

namespace Toolkit.WPF.Services.OcrTraining;

public sealed class FileSystemDatasetManager : IDatasetManager
{
    public async Task<DatasetSplit> SplitDatasetAsync(
        string sourceDirectory,
        string outputDirectory,
        double trainRatio = 0.8,
        double valRatio   = 0.1,
        double testRatio  = 0.1,
        CancellationToken ct = default)
    {
        if (Math.Abs(trainRatio + valRatio + testRatio - 1.0) > 0.001)
            throw new ArgumentException("Ratios must sum to 1.0");

        var images = Directory.GetFiles(sourceDirectory, "*.png")
            .Where(f => File.Exists(Path.ChangeExtension(f, ".gt.txt")))
            .OrderBy(f => f)
            .ToList();

        if (images.Count == 0)
            throw new InvalidOperationException("No image+gt.txt pairs found in source directory.");

        var trainCount = (int)(images.Count * trainRatio);
        var valCount   = (int)(images.Count * valRatio);
        var testCount  = images.Count - trainCount - valCount;

        var splits = new[]
        {
            ("train", images.Take(trainCount)),
            ("val",   images.Skip(trainCount).Take(valCount)),
            ("test",  images.Skip(trainCount + valCount))
        };

        await Task.Run(() =>
        {
            foreach (var (splitName, files) in splits)
            {
                ct.ThrowIfCancellationRequested();
                var splitDir = Path.Combine(outputDirectory, splitName);
                Directory.CreateDirectory(splitDir);

                foreach (var imgFile in files)
                {
                    ct.ThrowIfCancellationRequested();
                    var gtFile = Path.ChangeExtension(imgFile, ".gt.txt");
                    File.Copy(imgFile, Path.Combine(splitDir, Path.GetFileName(imgFile)), overwrite: true);
                    File.Copy(gtFile,  Path.Combine(splitDir, Path.GetFileName(gtFile)),  overwrite: true);
                }
            }
        }, ct);

        return new DatasetSplit(trainCount, valCount, testCount);
    }

    public async Task<EvaluationMetrics> EvaluateAsync(
        string modelPath,
        string testDataDirectory,
        CancellationToken ct = default)
    {
        var testImages = Directory.GetFiles(testDataDirectory, "*.png")
            .Where(f => File.Exists(Path.ChangeExtension(f, ".gt.txt")))
            .ToList();

        if (testImages.Count == 0)
            throw new InvalidOperationException("No test image+gt.txt pairs found.");

        double totalCer = 0;
        double totalWer = 0;

        using var engine = new Tesseract.TesseractEngine(modelPath, "vie", Tesseract.EngineMode.LstmOnly);

        await Task.Run(() =>
        {
            foreach (var imgPath in testImages)
            {
                ct.ThrowIfCancellationRequested();

                var gt = File.ReadAllText(Path.ChangeExtension(imgPath, ".gt.txt")).Trim();
                using var img       = Tesseract.Pix.LoadFromFile(imgPath);
                using var page      = engine.Process(img);
                var predicted = (page.GetText() ?? string.Empty).Trim();

                totalCer += ComputeCer(gt, predicted);
                totalWer += ComputeWer(gt, predicted);
            }
        }, ct);

        var n = testImages.Count;
        return new EvaluationMetrics(totalCer / n, totalWer / n, n);
    }

    private static double ComputeCer(string reference, string hypothesis)
    {
        var d = EditDistance(reference, hypothesis);
        return reference.Length == 0 ? 0 : (double)d / reference.Length;
    }

    private static double ComputeWer(string reference, string hypothesis)
    {
        var refWords = reference.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var hypWords = hypothesis.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var d = EditDistance(refWords, hypWords);
        return refWords.Length == 0 ? 0 : (double)d / refWords.Length;
    }

    private static int EditDistance(string a, string b)
    {
        var m  = a.Length;
        var n  = b.Length;
        var dp = new int[m + 1, n + 1];
        for (var i = 0; i <= m; i++) dp[i, 0] = i;
        for (var j = 0; j <= n; j++) dp[0, j] = j;
        for (var i = 1; i <= m; i++)
            for (var j = 1; j <= n; j++)
                dp[i, j] = a[i - 1] == b[j - 1]
                    ? dp[i - 1, j - 1]
                    : 1 + Math.Min(dp[i - 1, j], Math.Min(dp[i, j - 1], dp[i - 1, j - 1]));
        return dp[m, n];
    }

    private static int EditDistance(string[] a, string[] b)
    {
        var m  = a.Length;
        var n  = b.Length;
        var dp = new int[m + 1, n + 1];
        for (var i = 0; i <= m; i++) dp[i, 0] = i;
        for (var j = 0; j <= n; j++) dp[0, j] = j;
        for (var i = 1; i <= m; i++)
            for (var j = 1; j <= n; j++)
                dp[i, j] = a[i - 1] == b[j - 1]
                    ? dp[i - 1, j - 1]
                    : 1 + Math.Min(dp[i - 1, j], Math.Min(dp[i, j - 1], dp[i - 1, j - 1]));
        return dp[m, n];
    }
}
