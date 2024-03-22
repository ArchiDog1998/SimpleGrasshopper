using Grasshopper.Kernel.Attributes;

namespace SimpleGrasshopper.Attributes;

/// <summary>
/// The <see cref="GH_ComponentAttributes"/> for this <see cref="SimpleGrasshopper.DocumentObjects.MethodComponent"/> or <see cref="SimpleGrasshopper.DocumentObjects.TypeMethodComponent"/>. 
/// </summary>
/// <param name="fullName">the full name of the base component.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface)]
public class DocObjAttrAttribute(string fullName) : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public string FullName => fullName;
}
