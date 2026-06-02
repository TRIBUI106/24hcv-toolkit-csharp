using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Toolkit.WPF.Common;

namespace Toolkit.WPF.Features.BatchRenaming;

public sealed partial class BatchRenamingViewModel : ViewModelBase
{
    private readonly BatchRenamingService _service;

    [ObservableProperty] private string _rootFolder    = string.Empty;
    [ObservableProperty] private string _maDinhDanh   = string.Empty;
    [ObservableProperty] private string _maPhong      = string.Empty;
    [ObservableProperty] private string _maMucLuc     = string.Empty;
    [ObservableProperty] private string _previewFolderName = string.Empty;
    [ObservableProperty] private bool   _isRunning;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _statusMessage = "Ready";

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
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            RootFolder = dialog.SelectedPath;
    }

    [RelayCommand(CanExecute = nameof(CanPreview))]
    private void Preview()
    {
        var spec = BuildSpec();
        if (spec is null) return;

        var result = _service.Preview(RootFolder, spec);
        if (!result.IsSuccess)
        {
            StatusMessage = $"Error: {result.ErrorMessage}";
            return;
        }

        var preview = result.Value!;
        PreviewFolderName = System.IO.Path.GetFileName(RootFolder);
        PreviewItems.Clear();
        foreach (var item in preview.Files)
            PreviewItems.Add(new FilePreviewRow(item.SubfolderName, item.OriginalFileName, item.NewFileName));

        StatusMessage = $"Preview sẵn sàng — {PreviewItems.Count} file(s) sẽ được đổi tên.";
        RenameCommand.NotifyCanExecuteChanged();
    }

    private bool CanPreview() =>
        !IsRunning &&
        !string.IsNullOrWhiteSpace(RootFolder) &&
        !string.IsNullOrWhiteSpace(MaDinhDanh) &&
        !string.IsNullOrWhiteSpace(MaPhong) &&
        !string.IsNullOrWhiteSpace(MaMucLuc);

    [RelayCommand(CanExecute = nameof(CanRename))]
    private void Rename()
    {
        var spec = BuildSpec();
        if (spec is null) return;

        if (System.Windows.MessageBox.Show(
            "Thao tác này sẽ đổi tên các subfolder và tất cả PDF bên trong.\n\nTiếp tục?",
            "Xác nhận Rename",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        IsRunning     = true;
        StatusMessage = "Đang rename...";

        var reporter = new WpfProgressReporter(p =>
        {
            ProgressPercent = p.PercentComplete;
            StatusMessage   = $"[{p.CompletedItems}/{p.TotalItems}] {p.CurrentItemName}";
        });

        var result = _service.Rename(RootFolder, spec, reporter);

        IsRunning       = false;
        ProgressPercent = 100;

        if (result.IsSuccess)
        {
            var succeeded = result.Value!.Count(r => r.WasRenamed);
            StatusMessage     = $"Hoàn tất — {succeeded} item(s) đã được đổi tên.";
            PreviewItems.Clear();
            PreviewFolderName = string.Empty;
            RootFolder        = string.Empty;
        }
        else
        {
            StatusMessage = $"Error: {result.ErrorMessage}";
        }
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

    partial void OnRootFolderChanged(string value)
    {
        PreviewCommand.NotifyCanExecuteChanged();
        RenameCommand.NotifyCanExecuteChanged();
    }

    partial void OnMaDinhDanhChanged(string value) => PreviewCommand.NotifyCanExecuteChanged();
    partial void OnMaPhongChanged(string value)    => PreviewCommand.NotifyCanExecuteChanged();
    partial void OnMaMucLucChanged(string value)   => PreviewCommand.NotifyCanExecuteChanged();
}

public sealed record FilePreviewRow(string Subfolder, string OldName, string NewName);
