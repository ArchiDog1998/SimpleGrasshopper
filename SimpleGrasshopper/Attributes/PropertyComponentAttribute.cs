namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Generate the modify property component.
/// </summary>
/// <param name="iconPath">the path or name of the icon that is embedded in your project</param>
/// <param name="exposure"></param>
/// <param name="subCategory">the value of <see cref="IGH_InstanceDescription.SubCategory"/></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class PropertyComponentAttribute(string iconPath = "", GH_Exposure exposure = GH_Exposure.primary, string subCategory = "") : Attribute
{
    internal GH_Exposure Exposure => exposure;
    internal string SubCategory => subCategory;
    internal string IconPath => iconPath;
}

