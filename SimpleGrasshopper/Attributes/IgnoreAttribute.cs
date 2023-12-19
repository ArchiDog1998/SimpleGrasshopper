namespace SimpleGrasshopper.Attributes;


/// <summary>
/// Ignore this member in <see cref="SimpleGrasshopper.DocumentObjects.TypeMethodComponent{T}"/> or <see cref="SimpleGrasshopper.DocumentObjects.TypePropertyComponent{T}"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Method)]
public class IgnoreAttribute : Attribute
{
}
