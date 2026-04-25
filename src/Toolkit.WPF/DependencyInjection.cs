using Microsoft.Extensions.DependencyInjection;
using Toolkit.Application.PdfAnalysis.Handlers;
using Toolkit.Application.BatchRenaming.Handlers;
using Toolkit.Application.ImagePreprocessing.Handlers;
using Toolkit.Application.Ocr.Handlers;
using Toolkit.Application.OcrTraining.Handlers;
using Toolkit.WPF.Features.PdfAnalysis;
using Toolkit.WPF.Features.BatchRenaming;
using Toolkit.WPF.Features.ImagePreprocessing;
using Toolkit.WPF.Features.Ocr;
using Toolkit.WPF.Features.OcrTraining;
using Toolkit.WPF.Navigation;

namespace Toolkit.WPF;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // Handlers (Application layer — registered here to keep Application DI-free)
        services.AddTransient<AnalyzePdfFolderHandler>();
        services.AddTransient<ApplyPdfMetadataHandler>();
        services.AddTransient<PreviewRenameHandler>();
        services.AddTransient<RenameFolderHandler>();
        services.AddTransient<PreprocessImagesHandler>();
        services.AddTransient<RunOcrHandler>();
        services.AddTransient<GenerateSyntheticDataHandler>();
        services.AddTransient<StartTrainingHandler>();
        services.AddTransient<SplitDatasetHandler>();
        services.AddTransient<EvaluateModelHandler>();

        // ViewModels
        services.AddSingleton<PdfAnalysisViewModel>();
        services.AddSingleton<BatchRenamingViewModel>();
        services.AddSingleton<ImagePreprocessingViewModel>();
        services.AddSingleton<OcrViewModel>();
        services.AddSingleton<OcrTrainingViewModel>();
        services.AddSingleton<MainViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();

        return services;
    }
}
