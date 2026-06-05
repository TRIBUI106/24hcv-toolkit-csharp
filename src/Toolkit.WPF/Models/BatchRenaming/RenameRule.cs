using System.Text.RegularExpressions;

namespace Toolkit.WPF.Models.BatchRenaming;

public sealed class RenameRule
{
    // Matches optional "hs" prefix, then digits, then optional letter suffix. e.g. "hs427A", "427", "015b"
    private static readonly Regex SubfolderPattern = new(@"^(?:hs)?(\d+)([A-Za-z]*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FileNumberPattern = new(@"(\d+)", RegexOptions.Compiled);

    public FolderRenameSpec Spec { get; }

    public RenameRule(FolderRenameSpec spec) => Spec = spec;

    // Returns new subfolder name for a given original subfolder name.
    // Extracts leading digits + optional letter suffix, pads digits to 3.
    // Returns null if no numeric part found.
    public string? GenerateSubfolderName(string originalSubfolderName)
    {
        var m = SubfolderPattern.Match(originalSubfolderName.Trim());
        if (!m.Success) return null;

        var num    = m.Groups[1].Value.PadLeft(3, '0');
        var suffix = m.Groups[2].Value; // e.g. "A" or ""
        return $"{Spec.BasePrefix}{num}{suffix}";
    }

    // Given a new subfolder name and list of PDF file names,
    // returns (original, new) pairs. Files with no numeric part are skipped.
    public IReadOnlyList<(string OriginalName, string NewName)> GeneratePdfNames(
        string newSubfolderName,
        IReadOnlyList<string> pdfFileNames)
    {
        var results = new List<(string, string)>();

        foreach (var file in pdfFileNames)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(file);
            var ext            = Path.GetExtension(file);

            var m = FileNumberPattern.Match(nameWithoutExt);
            if (!m.Success) continue; // skip non-numeric files

            var padded  = m.Value.PadLeft(3, '0');
            var newName = $"{newSubfolderName}-{padded}{ext}";
            results.Add((file, newName));
        }

        return results;
    }
}
