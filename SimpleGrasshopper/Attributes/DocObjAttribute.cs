namespace SimpleGrasshopper.Attributes;

/// <summary>
/// The attribute to add some basic description for <seealso cref="IGH_DocumentObject"/>.
/// </summary>
/// <param name="name">This is for <see cref="IGH_InstanceDescription.Name"/></param>
/// <param name="nickName">This is for <see cref="IGH_InstanceDescription.NickName"/></param>
/// <param name="description">This is for <see cref="IGH_InstanceDescription.Description"/></param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
    AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
public class DocObjAttribute(string name, string nickName, string description) : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public string Name => name;

    /// <summary>
    /// 
    /// </summary>
    public string NickName => nickName;

    /// <summary>
    /// 
    /// </summary>
    public string Description => description;
}
