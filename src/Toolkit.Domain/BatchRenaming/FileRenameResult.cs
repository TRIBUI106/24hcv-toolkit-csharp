namespace Toolkit.Domain.BatchRenaming;

public sealed record FileRenameResult(
    string OriginalPath,
    string NewPath,
    bool WasRenamed,
    string? ErrorMessage);
