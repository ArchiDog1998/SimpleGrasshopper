namespace SimpleGrasshopper.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class SubCategoryAttribute(string subCategory) : Attribute
{
    public string SubCategory => subCategory;
}
