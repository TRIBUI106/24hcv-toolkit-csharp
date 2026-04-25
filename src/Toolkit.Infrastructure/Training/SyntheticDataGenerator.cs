using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace Toolkit.Infrastructure.Training;

public sealed class SyntheticDataGenerator : ISyntheticDataGenerator
{
    private static readonly string[] VietnameseSamples =
    [
        "Cộng hòa xã hội chủ nghĩa Việt Nam",
        "Độc lập - Tự do - Hạnh phúc",
        "Ủy ban nhân dân tỉnh",
        "Sở Tài nguyên và Môi trường",
        "Quyết định về việc phê duyệt",
        "Căn cứ Luật Đất đai năm 2013",
        "Thực hiện theo quy định hiện hành",
        "Hà Nội, ngày tháng năm",
        "Kính gửi: Ủy ban nhân dân",
        "Trân trọng kính trình"
    ];

    public async Task GenerateAsync(
        string outputDirectory,
        int sampleCount,
        IReadOnlyList<string> fonts,
        IProgressReporter progress,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var fontList = fonts.Count > 0 ? fonts : ["Arial", "Times New Roman", "Courier New"];
        var rng = new Random(42);

        await Task.Run(() =>
        {
            for (var i = 0; i < sampleCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                var text = VietnameseSamples[rng.Next(VietnameseSamples.Length)];
                var fontName = fontList[rng.Next(fontList.Count)];
                var fontSize = rng.Next(18, 36);

                var imagePath = Path.Combine(outputDirectory, $"sample_{i:D5}.png");
                var gtPath = Path.Combine(outputDirectory, $"sample_{i:D5}.gt.txt");

                GenerateImage(text, fontName, fontSize, imagePath);
                File.WriteAllText(gtPath, text, System.Text.Encoding.UTF8);

                progress.Report(new BatchProgress(sampleCount, i + 1, $"sample_{i:D5}.png"));
            }
        }, ct);
    }

    private static void GenerateImage(string text, string fontName, int fontSize, string outputPath)
    {
        using var font = new Font(fontName, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        using var tempBmp = new Bitmap(1, 1);
        using var tempG = Graphics.FromImage(tempBmp);
        var size = tempG.MeasureString(text, font);

        var width = (int)Math.Ceiling(size.Width) + 20;
        var height = (int)Math.Ceiling(size.Height) + 20;

        using var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = TextRenderingHint.AntiAlias;
        g.Clear(Color.White);
        g.DrawString(text, font, Brushes.Black, new PointF(10, 10));
        bmp.Save(outputPath, ImageFormat.Png);
    }
}
