namespace SimpleGrasshopper.Attributes;

/// <summary>
/// The Base Component for all please use it to the class that inherits from <see cref="SimpleGrasshopper.DocumentObjects.MethodComponent"/> or <see cref="SimpleGrasshopper.DocumentObjects.TypeMethodComponent"/>. 
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BaseComponentAttribute : Attribute
{
}
