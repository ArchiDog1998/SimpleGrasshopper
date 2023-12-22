namespace SimpleGrasshopper.Attributes;

/// <summary>
/// This is for <see cref="IGH_InstanceDescription.SubCategory"/>
/// </summary>
/// <param name="subCategory">the value of <see cref="IGH_InstanceDescription.SubCategory"/></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class SubCategoryAttribute(string subCategory) : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public string SubCategory => subCategory;
}
