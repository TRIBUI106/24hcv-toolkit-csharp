using PdfSharpCore.Pdf.IO;
using PdfSharpCorePdf = PdfSharpCore.Pdf;

namespace Toolkit.Infrastructure.Pdf;

public sealed class PdfSharpReader : IPdfReader
{
    private const double PointsToMm = 25.4 / 72.0;

    public Task<Toolkit.Domain.PdfAnalysis.PdfDocument> ReadAsync(FilePath filePath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var document = PdfReader.Open(filePath.Value, PdfDocumentOpenMode.InformationOnly);

        var pages = new List<Toolkit.Domain.PdfAnalysis.PdfPage>(document.PageCount);
        for (var i = 0; i < document.PageCount; i++)
        {
            ct.ThrowIfCancellationRequested();
            var page = document.Pages[i];
            var widthMm = page.Width.Point * PointsToMm;
            var heightMm = page.Height.Point * PointsToMm;
            pages.Add(new Toolkit.Domain.PdfAnalysis.PdfPage(i + 1, widthMm, heightMm));
        }

        var info = document.Info;
        var metadata = new PdfMetadata(
            string.IsNullOrWhiteSpace(info.Title) ? null : info.Title,
            string.IsNullOrWhiteSpace(info.Author) ? null : info.Author,
            string.IsNullOrWhiteSpace(info.Subject) ? null : info.Subject);

        return Task.FromResult(new Toolkit.Domain.PdfAnalysis.PdfDocument(filePath, pages, metadata));
    }
}
