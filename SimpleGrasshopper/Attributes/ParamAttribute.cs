namespace SimpleGrasshopper.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class ParamAttribute(string guid) : Attribute
{
    public Guid Guid => new(guid);
}
