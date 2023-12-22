namespace SimpleGrasshopper.Attributes;

/// <summary>
/// The Base Component for all please use it to the class that inherits from <see cref="SimpleGrasshopper.DocumentObjects.MethodComponent"/> or <see cref="SimpleGrasshopper.DocumentObjects.TypeMethodComponent"/>. 
/// </summary>
/// <param name="fullName">the full name of the base component.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface)]
public class BaseComponentAttribute(string fullName) : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public string FullName => fullName;
}
