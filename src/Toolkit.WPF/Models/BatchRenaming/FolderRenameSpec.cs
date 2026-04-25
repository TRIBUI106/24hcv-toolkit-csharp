namespace Toolkit.WPF.Models.BatchRenaming;

public sealed record FolderRenameSpec(
    string MaDinhDanh,
    string MaPhong,
    string MaMucLuc)
{
    public string PaddedMaPhong  => PadIfDigits(MaPhong,  3);
    public string PaddedMaMucLuc => PadIfDigits(MaMucLuc, 2);

    // Format: {MaDinhDanh}-{MaPhong(3)}-{MaMucLuc(2)}-   (caller appends subfolder part)
    public string BasePrefix =>
        $"{MaDinhDanh}-{PaddedMaPhong}-{PaddedMaMucLuc}-";

    private static string PadIfDigits(string value, int width) =>
        value.Length > 0 && value.All(char.IsDigit)
            ? value.PadLeft(width, '0')
            : value;
}
