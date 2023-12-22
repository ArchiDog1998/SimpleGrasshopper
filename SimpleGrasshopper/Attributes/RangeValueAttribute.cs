namespace SimpleGrasshopper.Attributes;

/// <summary>
/// To the property or field that needs to be ranged.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class RangeValueAttribute : Attribute
{
}
