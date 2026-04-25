namespace Toolkit.Application.ImagePreprocessing.Interfaces;

public interface IImageProcessor
{
    Task<ProcessedImage> ProcessAsync(
        FilePath inputPath,
        FilePath outputPath,
        PreprocessingOptions options,
        CancellationToken ct = default);
}
