namespace SimpleGrasshopper.Attributes;

/// <summary>
/// The tag on the params.
/// </summary>
/// <param name="principal"></param>
/// <param name="mapping"></param>
/// <param name="reverse"></param>
/// <param name="simplify"></param>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
public class ParamTagAttribute(bool principal = false, GH_DataMapping mapping = GH_DataMapping.None, bool reverse = false, bool simplify = false) : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public bool Principal => principal;

    /// <summary>
    /// 
    /// </summary>
    public GH_DataMapping Mapping => mapping;

    /// <summary>
    /// 
    /// </summary>
    public bool Reverse => reverse;

    /// <summary>
    /// 
    /// </summary>
    public bool Simplify => simplify;

}
