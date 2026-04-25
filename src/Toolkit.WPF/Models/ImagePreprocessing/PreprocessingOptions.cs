namespace Toolkit.WPF.Models.ImagePreprocessing;

public sealed record PreprocessingOptions
{
    public bool Deskew { get; init; } = true;
    public bool Denoise { get; init; } = true;
    public bool ApplyClahe { get; init; } = true;
    public bool ApplyOtsu { get; init; } = false;
    public int TargetDpi { get; init; } = 300;
    public double ClaheClipLimit { get; init; } = 2.0;
    public int ClaheTileSize { get; init; } = 8;
}
