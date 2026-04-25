using Microsoft.Extensions.DependencyInjection;
using Toolkit.Application.PdfAnalysis.Interfaces;
using Toolkit.Application.BatchRenaming.Interfaces;
using Toolkit.Application.ImagePreprocessing.Interfaces;
using Toolkit.Application.Ocr.Interfaces;
using Toolkit.Application.OcrTraining.Interfaces;
using Toolkit.Infrastructure.FileSystem;
using Toolkit.Infrastructure.ImageProcessing;
using Toolkit.Infrastructure.Ocr;
using Toolkit.Infrastructure.Pdf;
using Toolkit.Infrastructure.Training;

namespace Toolkit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string tessDataPath = "tessdata",
        string tesseractBinPath = "tesseract")
    {
        // File system
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddTransient<IFileRenamer, FileRenamer>();

        // PDF
        services.AddTransient<IPdfReader, PdfSharpReader>();
        services.AddTransient<IPdfMetadataWriter, PdfSharpMetadataWriter>();

        // Image processing
        services.AddTransient<IImageProcessor, OpenCvImageProcessor>();

        // OCR
        services.AddSingleton<IOcrEngine>(_ => new TesseractOcrEngine(tessDataPath));

        // Training
        services.AddTransient<ISyntheticDataGenerator, SyntheticDataGenerator>();
        services.AddTransient<IDatasetManager, FileSystemDatasetManager>();
        services.AddTransient<ITrainingRunner>(_ => new TesseractTrainingRunner(tesseractBinPath));

        return services;
    }
}
