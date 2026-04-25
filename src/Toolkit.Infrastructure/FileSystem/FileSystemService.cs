namespace Toolkit.Infrastructure.FileSystem;

public sealed class FileSystemService : IFileSystemService
{
    public IReadOnlyList<string> GetFiles(string directory, string searchPattern, bool recursive = false)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetFiles(directory, searchPattern, option);
    }

    public IReadOnlyList<string> GetDirectories(string directory) =>
        Directory.GetDirectories(directory);

    public void RenameFile(string oldPath, string newPath) =>
        File.Move(oldPath, newPath);

    public void RenameDirectory(string oldPath, string newPath) =>
        Directory.Move(oldPath, newPath);

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public long GetFileSizeBytes(string path) => new FileInfo(path).Length;
}
