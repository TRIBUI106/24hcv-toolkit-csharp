namespace Toolkit.Domain.PdfAnalysis;

public sealed class PdfPage
{
    public int PageNumber { get; }
    public double WidthMm { get; }
    public double HeightMm { get; }
    public PageSize DetectedSize { get; }

    public PdfPage(int pageNumber, double widthMm, double heightMm)
    {
        PageNumber = pageNumber;
        WidthMm = widthMm;
        HeightMm = heightMm;
        DetectedSize = DetectFromMediaBox(widthMm, heightMm);
    }

    public static PageSize DetectFromMediaBox(double widthMm, double heightMm)
    {
        const double tolerance = 3.0;

        // Normalize so width <= height (portrait orientation for comparison)
        double w = Math.Min(widthMm, heightMm);
        double h = Math.Max(widthMm, heightMm);

        // A4: 210 x 297 mm
        if (Math.Abs(w - 210.0) <= tolerance && Math.Abs(h - 297.0) <= tolerance)
            return PageSize.A4;

        // A3: 297 x 420 mm
        if (Math.Abs(w - 297.0) <= tolerance && Math.Abs(h - 420.0) <= tolerance)
            return PageSize.A3;

        // A2: 420 x 594 mm
        if (Math.Abs(w - 420.0) <= tolerance && Math.Abs(h - 594.0) <= tolerance)
            return PageSize.A2;

        // A1: 594 x 841 mm
        if (Math.Abs(w - 594.0) <= tolerance && Math.Abs(h - 841.0) <= tolerance)
            return PageSize.A1;

        // A0: 841 x 1189 mm
        if (Math.Abs(w - 841.0) <= tolerance && Math.Abs(h - 1189.0) <= tolerance)
            return PageSize.A0;

        return PageSize.Unknown;
    }
}
