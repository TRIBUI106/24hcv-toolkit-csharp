using Toolkit.WPF.Models.Common;

namespace Toolkit.WPF.Models.OcrTraining;

public sealed class TrainingDataset
{
    public string Name { get; }
    public FilePath RootDirectory { get; }
    public DatasetSplit Split { get; }

    public TrainingDataset(string name, FilePath rootDirectory, DatasetSplit split)
    {
        Name = name;
        RootDirectory = rootDirectory;
        Split = split;
    }
}
