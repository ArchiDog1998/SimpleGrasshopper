namespace SimpleGrasshopper.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Parameter)]
public class DocObjAttribute(string name, string nickName, string description) : Attribute
{
    public string Name => name;
    public string NickName => nickName;
    public string Description => description;
}
