using Microsoft.Extensions.DependencyInjection;
using Toolkit.WPF.Features.BatchRenaming;
using Toolkit.WPF.Features.ImagePreprocessing;
using Toolkit.WPF.Features.Ocr;
using Toolkit.WPF.Features.OcrTraining;
using Toolkit.WPF.Features.PdfAConverter;
using Toolkit.WPF.Features.PdfAnalysis;
using Toolkit.WPF.Navigation;
using Toolkit.WPF.Services.BatchRenaming;
using Toolkit.WPF.Services.ImagePreprocessing;
using Toolkit.WPF.Services.Ocr;
using Toolkit.WPF.Services.OcrTraining;
using Toolkit.WPF.Services.PdfAConverter;
using Toolkit.WPF.Services.PdfAnalysis;

namespace Toolkit.WPF;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        string tessDataPath     = "tessdata",
        string tesseractBinPath = "tesseract")
    {
        // ── Infrastructure / Service implementations ─────────────────────────

        // File system (shared across features)
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddTransient<IFileRenamer, FileRenamer>();

        // PDF
        services.AddTransient<IPdfReader, PdfSharpReader>();
        services.AddTransient<IPdfMetadataWriter, PdfSharpMetadataWriter>();

        // PDF/A Conversion
        services.AddTransient<IPdfAConverter, IText7PdfAConverter>();
        services.AddTransient<PdfAConversionService>();

        // Image processing
        services.AddTransient<IImageProcessor, OpenCvImageProcessor>();

        // OCR
        services.AddSingleton<IOcrEngine>(_ => new TesseractOcrEngine(tessDataPath));

        // OCR Training
        services.AddTransient<ISyntheticDataGenerator, SyntheticDataGenerator>();
        services.AddTransient<IDatasetManager, FileSystemDatasetManager>();
        services.AddTransient<ITrainingRunner>(_ => new TesseractTrainingRunner(tesseractBinPath));

        // ── Aggregate service classes (merged Application + Infrastructure) ──

        services.AddTransient<BatchRenamingService>();
        services.AddTransient<PdfAnalysisService>();
        services.AddTransient<ImagePreprocessingService>();
        services.AddTransient<OcrService>();
        services.AddTransient<OcrTrainingService>();

        // ── ViewModels ────────────────────────────────────────────────────────

        services.AddSingleton<PdfAnalysisViewModel>();
        services.AddSingleton<BatchRenamingViewModel>();
        services.AddSingleton<ImagePreprocessingViewModel>();
        services.AddSingleton<OcrViewModel>();
        services.AddSingleton<OcrTrainingViewModel>();
        services.AddSingleton<PdfAConverterViewModel>();
        services.AddSingleton<MainViewModel>();

        // ── Windows ───────────────────────────────────────────────────────────

        services.AddSingleton<MainWindow>();

        return services;
    }
}
