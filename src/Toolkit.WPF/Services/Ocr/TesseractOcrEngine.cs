using Tesseract;
using System.Diagnostics;
using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.Ocr;
using TesseractEngineMode = Tesseract.EngineMode;
using TesseractPageSegMode = Tesseract.PageSegMode;

namespace Toolkit.WPF.Services.Ocr;

public sealed class TesseractOcrEngine : IOcrEngine, IDisposable
{
    private readonly string _tessDataPath;
    private TesseractEngine? _engine;
    private OcrConfiguration? _currentConfig;
    private readonly object _lock = new();

    public TesseractOcrEngine(string tessDataPath = "tessdata")
    {
        _tessDataPath = tessDataPath;
    }

    public Task<OcrResult> RecognizeAsync(
        FilePath imagePath,
        OcrConfiguration config,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            EnsureEngine(config);

            var sw = Stopwatch.StartNew();
            using var img  = Pix.LoadFromFile(imagePath.Value);
            using var page = _engine!.Process(img, MapPsm(config.Psm));

            var text       = page.GetText() ?? string.Empty;
            var confidence = page.GetMeanConfidence();
            sw.Stop();

            return Task.FromResult(new OcrResult(
                imagePath,
                text.Trim(),
                new ConfidenceScore(confidence * 100f),
                sw.ElapsedMilliseconds));
        }
    }

    private void EnsureEngine(OcrConfiguration config)
    {
        if (_engine is not null && _currentConfig == config) return;

        _engine?.Dispose();
        var lang = string.Join("+", config.Languages);
        _engine = new TesseractEngine(_tessDataPath, lang, MapEngineMode(config.EngineMode));
        _currentConfig = config;
    }

    private static TesseractEngineMode MapEngineMode(OcrEngineMode mode) => mode switch
    {
        OcrEngineMode.TesseractOnly => TesseractEngineMode.TesseractOnly,
        OcrEngineMode.LstmOnly      => TesseractEngineMode.LstmOnly,
        OcrEngineMode.Combined      => TesseractEngineMode.TesseractAndLstm,
        OcrEngineMode.Auto          => TesseractEngineMode.Default,
        _                           => TesseractEngineMode.Default
    };

    private static TesseractPageSegMode MapPsm(PageSegmentationMode psm) => psm switch
    {
        PageSegmentationMode.Auto        => TesseractPageSegMode.Auto,
        PageSegmentationMode.SingleBlock => TesseractPageSegMode.SingleBlock,
        PageSegmentationMode.SingleLine  => TesseractPageSegMode.SingleLine,
        PageSegmentationMode.SingleWord  => TesseractPageSegMode.SingleWord,
        _                               => TesseractPageSegMode.Auto
    };

    public void Dispose() => _engine?.Dispose();
}
