namespace SimpleGrasshopper.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]

public class IconAttribute(string iconPath) : Attribute
{
    public string IconPath => iconPath;
}
