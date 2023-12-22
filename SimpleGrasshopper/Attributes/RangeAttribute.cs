namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Add the range for this config.
/// </summary>
/// <param name="min">minimum</param>
/// <param name="max">maximum</param>
/// <param name="place">decimal places</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
public class RangeAttribute(double min, double max, int place = 2) : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public double MinD => min;

    /// <summary>
    /// 
    /// </summary>
    public double MaxD => max;

    /// <summary>
    /// 
    /// </summary>
    public decimal Min => Convert.ToDecimal(min);

    /// <summary>
    /// 
    /// </summary>
    public decimal Max => Convert.ToDecimal(max);

    /// <summary>
    /// 
    /// </summary>
    public int Place => place;

    /// <inheritdoc/>
    public override string ToString() => $"\n(From {MinD} To {MaxD})";
}
