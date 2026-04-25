namespace Toolkit.Application.PdfAnalysis.Commands;

public sealed record ApplyPdfMetadataCommand(
    IReadOnlyList<string> FilePaths,
    PdfMetadata Metadata);
