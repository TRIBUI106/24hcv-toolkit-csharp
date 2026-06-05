using Toolkit.WPF.Models.Common;
using Toolkit.WPF.Models.PdfAConverter;
using Toolkit.WPF.Services.BatchRenaming;
using Toolkit.WPF.Services.Common;

namespace Toolkit.WPF.Services.PdfAConverter;

public sealed class PdfAConversionService
{
    private const string ConvertedTag = "[PDFA-CONVERTED]";

    private readonly IPdfAConverter _converter;
    private readonly IFileSystemService _fileSystem;

    public PdfAConversionService(IPdfAConverter converter, IFileSystemService fileSystem)
    {
        _converter  = converter;
        _fileSystem = fileSystem;
    }

    public async Task<OperationResult<IReadOnlyList<PdfAConversionResult>>> ConvertFolderAsync(
        string rootFolder,
        PdfAConversionOptions options,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        if (!_fileSystem.DirectoryExists(rootFolder))
            return OperationResult<IReadOnlyList<PdfAConversionResult>>.Failure(
                $"Directory not found: {rootFolder}");

        var files = _fileSystem.GetFiles(rootFolder, "*.pdf", recursive: true);
        if (files.Count == 0)
            return OperationResult<IReadOnlyList<PdfAConversionResult>>.Success(
                Array.Empty<PdfAConversionResult>());

        if (!options.InPlace && !string.IsNullOrWhiteSpace(options.OutputDirectory))
            Directory.CreateDirectory(options.OutputDirectory);

        var results   = new List<PdfAConversionResult>();
        var completed = 0;

        foreach (var filePath in files)
        {
            ct.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(filePath);

            if (IsAlreadyConverted(filePath))
            {
                var skipped = new PdfAConversionResult
                {
                    SourcePath = filePath,
                    OutputPath = filePath,
                    Status     = ConversionStatus.Skipped
                };
                lock (results) results.Add(skipped);
                var s = Interlocked.Increment(ref completed);
                progress.Report(new BatchProgress(files.Count, s, fileName));
                continue;
            }

            var outputPath = options.InPlace
                ? filePath
                : Path.Combine(options.OutputDirectory, fileName);

            var result = await _converter.ConvertAsync(filePath, outputPath, options, ct);
            lock (results) results.Add(result);

            var count = Interlocked.Increment(ref completed);
            progress.Report(new BatchProgress(files.Count, count, fileName,
                result.Status == ConversionStatus.Error ? result.ErrorMessage : null));
        }

        return OperationResult<IReadOnlyList<PdfAConversionResult>>.Success(results);
    }

    private static bool IsAlreadyConverted(string filePath)
    {
        try
        {
            using var reader = new iText.Kernel.Pdf.PdfReader(filePath);
            using var doc    = new iText.Kernel.Pdf.PdfDocument(reader);
            var keywords = doc.GetDocumentInfo().GetKeywords() ?? string.Empty;
            return keywords.Contains(ConvertedTag);
        }
        catch
        {
            return false;
        }
    }
}
