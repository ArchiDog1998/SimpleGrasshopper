namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Make this component parallel as default.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]
public class ParallelAttribute : Attribute
{
}
