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
    public decimal Min
    {
        get
        {
            try
            {
                return Convert.ToDecimal(min);
            }
            catch
            {
                return decimal.MinValue;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public decimal Max
    {
        get
        {
            try
            {
                return Convert.ToDecimal(max);
            }
            catch
            {
                return decimal.MaxValue;
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public int Place => place;

    /// <inheritdoc/>
    public override string ToString() => $"\n(From {MinD} To {MaxD})";
}
