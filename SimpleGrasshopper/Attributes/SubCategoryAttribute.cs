namespace SimpleGrasshopper.Attributes;

/// <summary>
/// This is for <see cref="IGH_InstanceDescription.SubCategory"/>
/// </summary>
/// <param name="subCategory">the value of <see cref="IGH_InstanceDescription.SubCategory"/></param>
[AttributeUsage(AttributeTargets.Class)]
public class SubCategoryAttribute(string subCategory) : Attribute
{
    internal string SubCategory => subCategory;
}
