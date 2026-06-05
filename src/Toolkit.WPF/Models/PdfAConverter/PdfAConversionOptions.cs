namespace Toolkit.WPF.Models.PdfAConverter;

public sealed record PdfAConversionOptions
{
    /// <summary>When true, overwrite source file. When false, write to OutputDirectory.</summary>
    public bool InPlace { get; init; } = true;

    public string OutputDirectory { get; init; } = string.Empty;

    // Optional metadata overrides — null means "keep original value"
    public string? TitleOverride   { get; init; }
    public string? AuthorOverride  { get; init; }
    public string? SubjectOverride { get; init; }
}
