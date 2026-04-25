using Toolkit.WPF.Models.Common;

namespace Toolkit.WPF.Models.ImagePreprocessing;

public sealed class ProcessedImage
{
    public FilePath SourcePath { get; }
    public FilePath OutputPath { get; }
    public IReadOnlyList<ImagePreprocessStep> AppliedSteps { get; }
    public long ProcessingTimeMs { get; }

    public ProcessedImage(
        FilePath sourcePath,
        FilePath outputPath,
        IReadOnlyList<ImagePreprocessStep> appliedSteps,
        long processingTimeMs)
    {
        SourcePath = sourcePath;
        OutputPath = outputPath;
        AppliedSteps = appliedSteps;
        ProcessingTimeMs = processingTimeMs;
    }
}
