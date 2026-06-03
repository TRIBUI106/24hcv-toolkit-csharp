using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Toolkit.WPF.Common;

namespace Toolkit.WPF.Features.BatchRenaming;

public sealed partial class BatchRenamingViewModel : ViewModelBase
{
    private readonly BatchRenamingService _service;

    [ObservableProperty] private string _maDinhDanh   = string.Empty;
    [ObservableProperty] private string _maPhong      = string.Empty;
    [ObservableProperty] private string _maMucLuc     = string.Empty;
    [ObservableProperty] private bool   _isRunning;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _statusMessage = "Ready";

    public ObservableCollection<string>         RootFolders  { get; } = [];
    public ObservableCollection<FilePreviewRow> PreviewItems { get; } = [];

    public BatchRenamingViewModel(BatchRenamingService service)
    {
        _service = service;
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Chọn folder root cần rename"
        };
        var owner = new System.Windows.Forms.NativeWindow();
        owner.AssignHandle(new System.Windows.Interop.WindowInteropHelper(
            System.Windows.Application.Current.MainWindow).Handle);
        if (dialog.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK)
            AddFolder(dialog.SelectedPath);
    }

    public void AddFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!System.IO.Directory.Exists(path)) return;
        if (RootFolders.Contains(path)) return;
        RootFolders.Add(path);
        PreviewCommand.NotifyCanExecuteChanged();
        RenameCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void RemoveFolder(string path)
    {
        RootFolders.Remove(path);
        PreviewCommand.NotifyCanExecuteChanged();
        RenameCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanPreview))]
    private void Preview()
    {
        var spec = BuildSpec();
        if (spec is null) return;

        PreviewItems.Clear();

        foreach (var folder in RootFolders)
        {
            var result = _service.Preview(folder, spec);
            if (!result.IsSuccess)
            {
                StatusMessage = $"Error ({System.IO.Path.GetFileName(folder)}): {result.ErrorMessage}";
                return;
            }
            foreach (var item in result.Value!.Files)
                PreviewItems.Add(new FilePreviewRow(
                    System.IO.Path.GetFileName(folder),
                    item.SubfolderName,
                    item.OriginalFileName,
                    item.NewFileName));
        }

        StatusMessage = $"Preview sẵn sàng — {PreviewItems.Count} file(s) từ {RootFolders.Count} folder(s).";
        RenameCommand.NotifyCanExecuteChanged();
    }

    private bool CanPreview() => !IsRunning && RootFolders.Count > 0;

    [RelayCommand(CanExecute = nameof(CanRename))]
    private void Rename()
    {
        var spec = BuildSpec();
        if (spec is null) return;

        if (System.Windows.MessageBox.Show(
            $"Thao tác này sẽ đổi tên các subfolder và tất cả PDF bên trong {RootFolders.Count} folder(s).\n\nTiếp tục?",
            "Xác nhận Rename",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        IsRunning     = true;
        StatusMessage = "Đang rename...";
        ProgressPercent = 0;

        var totalSucceeded = 0;

        for (var i = 0; i < RootFolders.Count; i++)
        {
            var folder = RootFolders[i];
            var reporter = new WpfProgressReporter(p =>
            {
                var basePercent = (double)i / RootFolders.Count * 100;
                ProgressPercent = basePercent + p.PercentComplete / RootFolders.Count;
                StatusMessage   = $"[{System.IO.Path.GetFileName(folder)}] [{p.CompletedItems}/{p.TotalItems}] {p.CurrentItemName}";
            });

            var result = _service.Rename(folder, spec, reporter);
            if (!result.IsSuccess)
            {
                IsRunning     = false;
                StatusMessage = $"Error ({System.IO.Path.GetFileName(folder)}): {result.ErrorMessage}";
                return;
            }
            totalSucceeded += result.Value!.Count(r => r.WasRenamed);
        }

        IsRunning       = false;
        ProgressPercent = 100;
        StatusMessage   = $"Hoàn tất — {totalSucceeded} item(s) đã được đổi tên từ {RootFolders.Count} folder(s).";
        PreviewItems.Clear();
        RootFolders.Clear();
    }

    private bool CanRename() => !IsRunning && PreviewItems.Count > 0;

    private FolderRenameSpec? BuildSpec()
    {
        if (string.IsNullOrWhiteSpace(MaDinhDanh) ||
            string.IsNullOrWhiteSpace(MaPhong) ||
            string.IsNullOrWhiteSpace(MaMucLuc))
        {
            StatusMessage = "Vui lòng nhập đầy đủ: Mã định danh, Mã phông, Mã mục lục.";
            return null;
        }
        return new FolderRenameSpec(MaDinhDanh, MaPhong, MaMucLuc);
    }

    partial void OnIsRunningChanged(bool value)
    {
        PreviewCommand.NotifyCanExecuteChanged();
        RenameCommand.NotifyCanExecuteChanged();
    }

}

public sealed record FilePreviewRow(string RootFolder, string Subfolder, string OldName, string NewName);
