using Toolkit.WPF.Models.BatchRenaming;

namespace Toolkit.WPF.Services.BatchRenaming;

public sealed class FileRenamer : IFileRenamer
{
    public FileRenameResult RenameFile(string sourcePath, string newName)
    {
        try
        {
            var dir = Path.GetDirectoryName(sourcePath)!;
            var newPath = Path.Combine(dir, newName);
            File.Move(sourcePath, newPath);
            return new FileRenameResult(sourcePath, newPath, true, null);
        }
        catch (Exception ex)
        {
            return new FileRenameResult(sourcePath, sourcePath, false, ex.Message);
        }
    }

    public FileRenameResult RenameDirectory(string sourcePath, string newName)
    {
        try
        {
            var parent = Path.GetDirectoryName(sourcePath)!;
            var newPath = Path.Combine(parent, newName);
            Directory.Move(sourcePath, newPath);
            return new FileRenameResult(sourcePath, newPath, true, null);
        }
        catch (Exception ex)
        {
            return new FileRenameResult(sourcePath, sourcePath, false, ex.Message);
        }
    }
}
