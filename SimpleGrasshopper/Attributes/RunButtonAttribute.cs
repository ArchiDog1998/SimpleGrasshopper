namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Should this component add the button about Run button.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]
public class RunButtonAttribute : Attribute
{
}
