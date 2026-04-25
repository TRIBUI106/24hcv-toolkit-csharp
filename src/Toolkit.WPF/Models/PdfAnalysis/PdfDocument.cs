using Toolkit.WPF.Models.Common;

namespace Toolkit.WPF.Models.PdfAnalysis;

public sealed class PdfDocument
{
    public FilePath FilePath { get; }
    public IReadOnlyList<PdfPage> Pages { get; }
    public int PageCount => Pages.Count;
    public PdfMetadata Metadata { get; }

    public PdfDocument(FilePath filePath, IReadOnlyList<PdfPage> pages, PdfMetadata metadata)
    {
        FilePath = filePath;
        Pages = pages;
        Metadata = metadata;
    }
}
