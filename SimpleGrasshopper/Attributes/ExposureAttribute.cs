namespace SimpleGrasshopper.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]
public class ExposureAttribute(GH_Exposure exposure) : Attribute
{
    public GH_Exposure Exposure => exposure;
}
