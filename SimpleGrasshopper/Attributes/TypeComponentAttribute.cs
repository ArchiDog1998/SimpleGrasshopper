namespace SimpleGrasshopper.Attributes;

/// <summary>
/// The <see cref="GH_Component"/> that about a type. Please don't use it too much.
/// </summary>
/// <param name="name"></param>
/// <param name="nickName"></param>
/// <param name="description"></param>
/// <param name="subCategory"></param>
/// <param name="iconPath"></param>
/// <param name="exposure"></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public class TypeComponentAttribute(string? name = null,
        string? nickName = null,
        string? description = null,
        string? subCategory = null,
        string? iconPath = null,
        GH_Exposure exposure = GH_Exposure.primary) : Attribute
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
    public string? SubCategory => subCategory;

    /// <summary>
    /// 
    /// </summary>
    public string? IconPath => iconPath;

    /// <summary>
    /// 
    /// </summary>
    public GH_Exposure Exposure => exposure;
}
