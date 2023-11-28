namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Hidden the <see cref="IGH_PreviewObject"/>
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public class HiddenAttribute : Attribute
{
}
