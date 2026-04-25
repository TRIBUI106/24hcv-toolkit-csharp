namespace Toolkit.Application.PdfAnalysis.Interfaces;

public interface IPdfReader
{
    Task<PdfDocument> ReadAsync(FilePath filePath, CancellationToken ct = default);
}
