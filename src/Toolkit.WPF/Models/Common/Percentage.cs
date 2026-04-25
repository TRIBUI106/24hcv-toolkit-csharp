namespace Toolkit.WPF.Models.Common;

public sealed record Percentage
{
    public double Value { get; }

    public Percentage(double value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), "Percentage must be between 0 and 100.");

        Value = value;
    }

    public override string ToString() => $"{Value:F2}%";
}
