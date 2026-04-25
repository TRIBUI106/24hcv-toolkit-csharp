namespace Toolkit.Domain.BatchRenaming;

public sealed class RenameRule
{
    public FolderRenameSpec FolderRenameSpec { get; }

    public RenameRule(FolderRenameSpec folderRenameSpec)
    {
        FolderRenameSpec = folderRenameSpec;
    }

    public string GenerateFolderName() => FolderRenameSpec.ToFolderName();

    public IReadOnlyList<(string OriginalName, string NewName)> GenerateSequentialNames(
        IReadOnlyList<string> fileNames)
    {
        var sorted = fileNames
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new List<(string OriginalName, string NewName)>(sorted.Count);
        for (int i = 0; i < sorted.Count; i++)
        {
            string extension = Path.GetExtension(sorted[i]);
            string newName = $"{(i + 1):D3}{extension}";
            results.Add((sorted[i], newName));
        }

        return results;
    }
}
