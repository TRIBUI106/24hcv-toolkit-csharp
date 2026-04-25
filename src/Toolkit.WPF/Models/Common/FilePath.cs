namespace Toolkit.WPF.Models.Common;

public sealed record FilePath
{
    public string Value { get; }

    public FilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("File path cannot be null or empty.", nameof(path));

        Value = Path.GetFullPath(path);
    }

    public override string ToString() => Value;

    public static implicit operator string(FilePath fp) => fp.Value;
}
