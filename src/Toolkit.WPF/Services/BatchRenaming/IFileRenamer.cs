using Toolkit.WPF.Models.BatchRenaming;

namespace Toolkit.WPF.Services.BatchRenaming;

public interface IFileRenamer
{
    FileRenameResult RenameFile(string sourcePath, string newName);
    FileRenameResult RenameDirectory(string sourcePath, string newName);
}
