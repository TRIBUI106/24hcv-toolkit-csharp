using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.ImagePreprocessing;

namespace Toolkit.WPF.Services.ImagePreprocessing;

public interface IImageProcessor
{
    Task<ProcessedImage> ProcessAsync(
        FilePath inputPath,
        FilePath outputPath,
        PreprocessingOptions options,
        CancellationToken ct = default);
}
