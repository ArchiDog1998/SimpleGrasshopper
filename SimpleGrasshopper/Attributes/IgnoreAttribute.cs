using Grasshopper.Kernel.Types;

namespace SimpleGrasshopper.Attributes;


/// <summary>
/// Ignore this member in <see cref="SimpleGrasshopper.DocumentObjects.TypeMethodComponent"/> or <see cref="SimpleGrasshopper.DocumentObjects.TypePropertyComponent{T}"/>.
/// Or ignore this convert method in the <see cref="IGH_Goo"/> casting.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
public class IgnoreAttribute : Attribute
{
}
