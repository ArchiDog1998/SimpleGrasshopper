namespace SimpleGrasshopper.Attributes;

/// <summary>
/// This is for <see cref="IGH_DocumentObject.Exposure"/>
/// </summary>
/// <param name="exposure">the value for <see cref="IGH_DocumentObject.Exposure"/>.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]
public class ExposureAttribute(GH_Exposure exposure) : Attribute
{
    internal GH_Exposure Exposure => exposure;
}
