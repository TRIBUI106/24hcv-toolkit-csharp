using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Pdfa;
using System.Diagnostics;
using System.Reflection;
using Toolkit.WPF.Models.PdfAConverter;
using ITextPdfDocument = iText.Kernel.Pdf.PdfDocument;

namespace Toolkit.WPF.Services.PdfAConverter;

public sealed class IText7PdfAConverter : IPdfAConverter
{
    private const string ConvertedTag = "[PDFA-CONVERTED]";

    public Task<PdfAConversionResult> ConvertAsync(
        string sourcePath,
        string outputPath,
        PdfAConversionOptions options,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var sw = Stopwatch.StartNew();

        try
        {
            using var iccStream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("Toolkit.WPF.Resources.sRGB2014.icc")
                ?? throw new InvalidOperationException("Embedded ICC profile not found. Ensure Resources/sRGB2014.icc is marked as EmbeddedResource.");

            var intent = new PdfOutputIntent(
                "Custom", "", "http://www.color.org",
                "sRGB IEC61966-2.1", iccStream);

            var tempPath = outputPath + ".tmp";

            using var reader  = new PdfReader(sourcePath);
            using var srcDoc  = new ITextPdfDocument(reader);
            using var writer  = new PdfWriter(tempPath);
            using var pdfADoc = new PdfADocument(writer, PdfAConformanceLevel.PDF_A_2B, intent);

            srcDoc.CopyPagesTo(1, srcDoc.GetNumberOfPages(), pdfADoc);

            var srcInfo      = srcDoc.GetDocumentInfo();
            var origTitle    = srcInfo.GetTitle();
            var origAuthor   = srcInfo.GetAuthor();
            var origSubject  = srcInfo.GetSubject();
            var origKeywords = srcInfo.GetKeywords() ?? string.Empty;

            var dstInfo = pdfADoc.GetDocumentInfo();
            dstInfo.SetTitle(options.TitleOverride   ?? origTitle   ?? string.Empty);
            dstInfo.SetAuthor(options.AuthorOverride  ?? origAuthor  ?? string.Empty);
            dstInfo.SetSubject(options.SubjectOverride ?? origSubject ?? string.Empty);

            var newKeywords = string.IsNullOrWhiteSpace(origKeywords)
                ? ConvertedTag
                : origKeywords.Contains(ConvertedTag)
                    ? origKeywords
                    : $"{origKeywords}; {ConvertedTag}";
            dstInfo.SetKeywords(newKeywords);

            // Ensure at least one embedded font exists in the output document
            EmbedFallbackFont(pdfADoc);

            if (File.Exists(outputPath)) File.Delete(outputPath);
            File.Move(tempPath, outputPath);

            sw.Stop();
            return Task.FromResult(new PdfAConversionResult
            {
                SourcePath   = sourcePath,
                OutputPath   = outputPath,
                Status       = ConversionStatus.Converted,
                ProcessingMs = sw.ElapsedMilliseconds
            });
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            var tempPath = outputPath + ".tmp";
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            // Clean up temp file if it exists
            var tempPath = outputPath + ".tmp";
            if (File.Exists(tempPath)) File.Delete(tempPath);

            return Task.FromResult(new PdfAConversionResult
            {
                SourcePath   = sourcePath,
                OutputPath   = outputPath,
                Status       = ConversionStatus.Error,
                ProcessingMs = sw.ElapsedMilliseconds,
                ErrorMessage = ex.Message
            });
        }
    }

    private static void EmbedFallbackFont(PdfADocument doc)
    {
        if (doc.GetNumberOfPages() == 0) return;
        var font = PdfFontFactory.CreateFont(
            iText.IO.Font.Constants.StandardFonts.HELVETICA,
            PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
        doc.GetPage(1).GetResources().AddFont(doc, font);
    }
}
