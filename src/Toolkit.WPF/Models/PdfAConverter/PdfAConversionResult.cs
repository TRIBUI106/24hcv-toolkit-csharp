namespace Toolkit.WPF.Models.PdfAConverter;

public enum ConversionStatus { Converted, Skipped, Error }

public sealed class PdfAConversionResult
{
    public string SourcePath       { get; init; } = string.Empty;
    public string OutputPath       { get; init; } = string.Empty;
    public ConversionStatus Status { get; init; }
    public long ProcessingMs       { get; init; }
    public string? ErrorMessage    { get; init; }
}
