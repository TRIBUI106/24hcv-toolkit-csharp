using PdfSharpCore.Pdf.IO;
using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.PdfAnalysis;

namespace Toolkit.WPF.Services.PdfAnalysis;

public sealed class PdfSharpMetadataWriter : IPdfMetadataWriter
{
    public Task WriteMetadataAsync(FilePath filePath, PdfMetadata metadata, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var document = PdfReader.Open(filePath.Value, PdfDocumentOpenMode.Modify);

        if (metadata.Title   is not null) document.Info.Title   = metadata.Title;
        if (metadata.Author  is not null) document.Info.Author  = metadata.Author;
        if (metadata.Subject is not null) document.Info.Subject = metadata.Subject;

        document.Save(filePath.Value);

        return Task.CompletedTask;
    }
}
