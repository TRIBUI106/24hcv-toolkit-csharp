namespace Toolkit.Application.ImagePreprocessing.Commands;

public sealed record PreprocessImagesCommand(
    string InputDirectory,
    string OutputDirectory,
    PreprocessingOptions Options,
    string SearchPattern = "*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp");
