using OpenCvSharp;
using System.Diagnostics;

namespace Toolkit.Infrastructure.ImageProcessing;

public sealed class OpenCvImageProcessor : IImageProcessor
{
    public Task<ProcessedImage> ProcessAsync(
        FilePath inputPath,
        FilePath outputPath,
        PreprocessingOptions options,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var sw = Stopwatch.StartNew();
        var appliedSteps = new List<ImagePreprocessStep>();

        using var src = Cv2.ImRead(inputPath.Value, ImreadModes.Grayscale);
        if (src.Empty())
            throw new InvalidOperationException($"Cannot read image: {inputPath.Value}");

        var current = src.Clone();

        if (options.Deskew)
        {
            current = Deskew(current);
            appliedSteps.Add(ImagePreprocessStep.Deskew);
        }

        if (options.Denoise)
        {
            current = Denoise(current);
            appliedSteps.Add(ImagePreprocessStep.Denoise);
        }

        if (options.ApplyClahe)
        {
            current = ApplyClahe(current, options.ClaheClipLimit, options.ClaheTileSize);
            appliedSteps.Add(ImagePreprocessStep.Clahe);
        }

        if (options.ApplyOtsu)
        {
            current = ApplyOtsu(current);
            appliedSteps.Add(ImagePreprocessStep.Otsu);
        }

        current = NormalizeDpi(current, options.TargetDpi);
        appliedSteps.Add(ImagePreprocessStep.DpiNormalize);

        var dir = Path.GetDirectoryName(outputPath.Value);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        Cv2.ImWrite(outputPath.Value, current);
        current.Dispose();

        sw.Stop();
        return Task.FromResult(new ProcessedImage(inputPath, outputPath, appliedSteps, sw.ElapsedMilliseconds));
    }

    private static Mat Deskew(Mat src)
    {
        using var edges = new Mat();
        Cv2.Canny(src, edges, 50, 150, apertureSize: 3);

        var lines = Cv2.HoughLines(edges, 1, Math.PI / 180, threshold: 100);
        if (lines is null || lines.Length == 0) return src.Clone();

        var angles = lines
            .Select(l => l.Theta * 180.0 / Math.PI - 90.0)
            .Where(a => Math.Abs(a) < 45)
            .ToArray();

        if (angles.Length == 0) return src.Clone();

        var medianAngle = angles.OrderBy(a => a).ElementAt(angles.Length / 2);
        if (Math.Abs(medianAngle) < 0.1) return src.Clone();

        var center = new Point2f(src.Width / 2f, src.Height / 2f);
        using var rot = Cv2.GetRotationMatrix2D(center, medianAngle, 1.0);
        var dst = new Mat();
        Cv2.WarpAffine(src, dst, rot, src.Size(),
            flags: InterpolationFlags.Linear,
            borderMode: BorderTypes.Replicate);
        return dst;
    }

    private static Mat Denoise(Mat src)
    {
        var dst = new Mat();
        Cv2.FastNlMeansDenoising(src, dst, h: 10, templateWindowSize: 7, searchWindowSize: 21);
        return dst;
    }

    private static Mat ApplyClahe(Mat src, double clipLimit, int tileSize)
    {
        using var clahe = Cv2.CreateCLAHE(clipLimit, new Size(tileSize, tileSize));
        var dst = new Mat();
        clahe.Apply(src, dst);
        return dst;
    }

    private static Mat ApplyOtsu(Mat src)
    {
        var dst = new Mat();
        Cv2.Threshold(src, dst, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        return dst;
    }

    private static Mat NormalizeDpi(Mat src, int targetDpi)
    {
        // Without EXIF DPI metadata, assume 72 DPI as baseline and scale up
        const int assumedSourceDpi = 72;
        if (targetDpi <= assumedSourceDpi) return src.Clone();

        var scale = (double)targetDpi / assumedSourceDpi;
        var dst = new Mat();
        Cv2.Resize(src, dst, new Size(), scale, scale, InterpolationFlags.Cubic);
        return dst;
    }
}
