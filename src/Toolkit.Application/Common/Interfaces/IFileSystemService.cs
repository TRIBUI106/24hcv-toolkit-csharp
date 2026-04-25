namespace Toolkit.Application.Common.Interfaces;

public interface IFileSystemService
{
    IReadOnlyList<string> GetFiles(string directory, string searchPattern, bool recursive = false);
    IReadOnlyList<string> GetDirectories(string directory);
    void RenameFile(string oldPath, string newPath);
    void RenameDirectory(string oldPath, string newPath);
    bool FileExists(string path);
    bool DirectoryExists(string path);
    long GetFileSizeBytes(string path);
}
