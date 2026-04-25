using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using Toolkit.WPF.Common;

namespace Toolkit.WPF.Features.BatchRenaming;

public sealed partial class BatchRenamingViewModel : ViewModelBase
{
    private readonly PreviewRenameHandler _previewHandler;
    private readonly RenameFolderHandler _renameHandler;

    [ObservableProperty] private string _rootFolder = string.Empty;
    [ObservableProperty] private string _maDinhDanh = string.Empty;
    [ObservableProperty] private string _maPhong = string.Empty;
    [ObservableProperty] private string _maMucLuc = string.Empty;
    [ObservableProperty] private string _maHoSo = string.Empty;
    [ObservableProperty] private string _previewFolderName = string.Empty;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _statusMessage = "Ready";

    public ObservableCollection<FilePreviewRow> PreviewItems { get; } = [];

    public BatchRenamingViewModel(PreviewRenameHandler previewHandler, RenameFolderHandler renameHandler)
    {
        _previewHandler = previewHandler;
        _renameHandler = renameHandler;
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select the root folder to rename"
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            RootFolder = dialog.SelectedPath;
    }

    [RelayCommand(CanExecute = nameof(CanPreview))]
    private void Preview()
    {
        var spec = BuildSpec();
        if (spec is null) return;

        var result = _previewHandler.Handle(new PreviewRenameQuery(RootFolder, spec));
        if (!result.IsSuccess)
        {
            StatusMessage = $"Error: {result.ErrorMessage}";
            return;
        }

        var preview = result.Value!;
        PreviewFolderName = $"{System.IO.Path.GetFileName(RootFolder)} → {preview.NewFolderName}";
        PreviewItems.Clear();
        foreach (var item in preview.Files)
            PreviewItems.Add(new FilePreviewRow(item.SubfolderName, item.OriginalFileName, item.NewFileName));

        StatusMessage = $"Preview ready — {PreviewItems.Count} file(s) will be renamed.";
    }

    private bool CanPreview() =>
        !IsRunning &&
        !string.IsNullOrWhiteSpace(RootFolder) &&
        !string.IsNullOrWhiteSpace(MaDinhDanh);

    [RelayCommand(CanExecute = nameof(CanRename))]
    private void Rename()
    {
        var spec = BuildSpec();
        if (spec is null) return;

        if (System.Windows.MessageBox.Show(
            "This will rename the folder and all PDFs inside.\n\nProceed?",
            "Confirm Rename", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        IsRunning = true;
        StatusMessage = "Renaming...";

        var reporter = new WpfProgressReporter(p =>
        {
            ProgressPercent = p.PercentComplete;
            StatusMessage = $"[{p.CompletedItems}/{p.TotalItems}] {p.CurrentItemName}";
        });

        var result = _renameHandler.Handle(new RenameFolderCommand(RootFolder, spec), reporter);

        IsRunning = false;
        ProgressPercent = 100;

        if (result.IsSuccess)
        {
            var succeeded = result.Value!.Count(r => r.WasRenamed);
            StatusMessage = $"Done — {succeeded} item(s) renamed.";
            PreviewItems.Clear();
            PreviewFolderName = string.Empty;
            RootFolder = string.Empty;
        }
        else
        {
            StatusMessage = $"Error: {result.ErrorMessage}";
        }
    }

    private bool CanRename() => !IsRunning && PreviewItems.Count > 0;

    private FolderRenameSpec? BuildSpec()
    {
        if (string.IsNullOrWhiteSpace(MaDinhDanh))
        {
            StatusMessage = "Mã định danh is required.";
            return null;
        }
        return new FolderRenameSpec(MaDinhDanh, MaPhong, MaMucLuc, MaHoSo);
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
}

public sealed record FilePreviewRow(string Subfolder, string OldName, string NewName);
