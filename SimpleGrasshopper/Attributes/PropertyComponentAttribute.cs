namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Generate the modify property component.
/// </summary>
/// <param name="type"></param>
/// <param name="name"></param>
/// <param name="nickName"></param>
/// <param name="description"></param>
/// <param name="iconPath">the path or name of the icon that is embedded in your project</param>
/// <param name="exposure"></param>
/// <param name="subCategory">the value of <see cref="IGH_InstanceDescription.SubCategory"/></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public class PropertyComponentAttribute(
    TypePropertyType type = TypePropertyType.Property,
    string? name = null,
    string? nickName = null,
    string? description = null,
    string iconPath = "",
    GH_Exposure exposure = GH_Exposure.primary,
    string subCategory = "")
    : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public string? Name => name;

    /// <summary>
    /// 
    /// </summary>
    public string? NickName => nickName;

    /// <summary>
    /// 
    /// </summary>
    public string? Description => description;

    /// <summary>
    /// 
    /// </summary>
    public GH_Exposure Exposure => exposure;

    /// <summary>
    /// 
    /// </summary>
    public string SubCategory => subCategory;

    /// <summary>
    /// 
    /// </summary>
    public string IconPath => iconPath;

    /// <summary>
    /// 
    /// </summary>
    public TypePropertyType Type => type;
}

/// <summary>
/// 
/// </summary>
public enum TypePropertyType : byte
{
    /// <summary>
    /// 
    /// </summary>
    Ctor,

    /// <summary>
    /// 
    /// </summary>
    Dtor,

    /// <summary>
    /// 
    /// </summary>
    Property,
}