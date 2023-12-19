namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Hidden the <see cref="IGH_PreviewObject"/>
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property)]
public class HiddenAttribute : Attribute
{
}
