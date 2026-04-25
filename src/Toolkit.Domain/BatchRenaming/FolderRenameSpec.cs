namespace Toolkit.Domain.BatchRenaming;

public sealed record FolderRenameSpec(
    string MaDinhDanh,
    string MaPhong,
    string MaMucLuc,
    string MaHoSo)
{
    public string ToFolderName() =>
        $"{MaDinhDanh}_{MaPhong}_{MaMucLuc}_{MaHoSo}";
}
