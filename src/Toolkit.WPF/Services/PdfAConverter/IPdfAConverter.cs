using Toolkit.WPF.Models.PdfAConverter;

namespace Toolkit.WPF.Services.PdfAConverter;

public interface IPdfAConverter
{
    Task<PdfAConversionResult> ConvertAsync(
        string sourcePath,
        string outputPath,
        PdfAConversionOptions options,
        CancellationToken ct = default);
}
