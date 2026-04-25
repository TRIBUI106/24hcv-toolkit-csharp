namespace Toolkit.Application.Ocr.Commands;

public sealed record RunOcrCommand(
    IReadOnlyList<string> ImagePaths,
    OcrConfiguration Config);
