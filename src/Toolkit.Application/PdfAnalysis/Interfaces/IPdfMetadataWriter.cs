namespace Toolkit.Application.PdfAnalysis.Interfaces;

public interface IPdfMetadataWriter
{
    Task WriteMetadataAsync(FilePath filePath, PdfMetadata metadata, CancellationToken ct = default);
}
