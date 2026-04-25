namespace Toolkit.WPF.Features.BatchRenaming;

public partial class BatchRenamingView : System.Windows.Controls.UserControl
{
    public BatchRenamingView() => InitializeComponent();

    private void RootFolderTextBox_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
            ? System.Windows.DragDropEffects.Copy
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void RootFolderTextBox_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;

        var paths = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
        var folder = paths.FirstOrDefault(System.IO.Directory.Exists);
        if (folder is null) return;

        if (DataContext is BatchRenamingViewModel vm)
            vm.RootFolder = folder;
    }
}
