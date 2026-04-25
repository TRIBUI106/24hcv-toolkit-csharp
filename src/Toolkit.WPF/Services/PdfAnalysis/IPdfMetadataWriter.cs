using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.PdfAnalysis;

namespace Toolkit.WPF.Services.PdfAnalysis;

public interface IPdfMetadataWriter
{
    Task WriteMetadataAsync(FilePath filePath, PdfMetadata metadata, CancellationToken ct = default);
}
