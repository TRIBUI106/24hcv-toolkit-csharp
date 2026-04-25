namespace Toolkit.Application.BatchRenaming.Interfaces;

public interface IFileRenamer
{
    FileRenameResult RenameFile(string sourcePath, string newName);
    FileRenameResult RenameDirectory(string sourcePath, string newName);
}
