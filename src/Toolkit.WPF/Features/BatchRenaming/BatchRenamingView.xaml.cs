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
        if (DataContext is not BatchRenamingViewModel vm) return;
        foreach (var path in paths.Where(System.IO.Directory.Exists))
            vm.AddFolder(path);
    }

    private void RootFolderTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter) return;
        if (DataContext is not BatchRenamingViewModel vm) return;
        vm.AddFolder(RootFolderTextBox.Text.Trim());
        RootFolderTextBox.Clear();
    }

    private void DropZone_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        var hasFolder = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop);
        e.Effects = hasFolder ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
        e.Handled = true;

        DropZone.BorderBrush = hasFolder
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(99, 102, 241))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
        DropZone.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(238, 242, 255));
        DropZoneText.Text = hasFolder ? "Thả folder vào đây" : "Chỉ chấp nhận folder";
    }

    private void DropZone_DragLeave(object sender, System.Windows.DragEventArgs e) => ResetDropZone();

    private void DropZone_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;
        var paths = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
        if (DataContext is not BatchRenamingViewModel vm) return;
        foreach (var path in paths.Where(System.IO.Directory.Exists))
            vm.AddFolder(path);
        ResetDropZone();
    }

    private void ResetDropZone()
    {
        DropZone.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225));
        DropZone.Background  = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252));
        DropZoneText.Text    = "Kéo một hoặc nhiều folder vào đây";
    }
}
