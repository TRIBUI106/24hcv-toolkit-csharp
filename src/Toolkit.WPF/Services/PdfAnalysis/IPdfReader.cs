using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.PdfAnalysis;

namespace Toolkit.WPF.Services.PdfAnalysis;

public interface IPdfReader
{
    Task<PdfDocument> ReadAsync(FilePath filePath, CancellationToken ct = default);
}
