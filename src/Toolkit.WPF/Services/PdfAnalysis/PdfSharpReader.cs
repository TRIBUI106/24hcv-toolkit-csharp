using PdfSharpCore.Pdf.IO;
using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.PdfAnalysis;

namespace Toolkit.WPF.Services.PdfAnalysis;

public sealed class PdfSharpReader : IPdfReader
{
    private const double PointsToMm = 25.4 / 72.0;

    public Task<PdfDocument> ReadAsync(FilePath filePath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var document = PdfReader.Open(filePath.Value, PdfDocumentOpenMode.InformationOnly);

        var pages = new List<PdfPage>(document.PageCount);
        for (var i = 0; i < document.PageCount; i++)
        {
            ct.ThrowIfCancellationRequested();
            var page = document.Pages[i];
            var widthMm  = page.Width.Point  * PointsToMm;
            var heightMm = page.Height.Point * PointsToMm;
            pages.Add(new PdfPage(i + 1, widthMm, heightMm));
        }

        var info = document.Info;
        var metadata = new PdfMetadata(
            string.IsNullOrWhiteSpace(info.Title)   ? null : info.Title,
            string.IsNullOrWhiteSpace(info.Author)  ? null : info.Author,
            string.IsNullOrWhiteSpace(info.Subject) ? null : info.Subject);

        return Task.FromResult(new PdfDocument(filePath, pages, metadata));
    }
}
