namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Tag the property that should be saved into the <see cref="GH_Document"/>.
/// If you want to use it into your own class, please add something in <see cref="Newtonsoft.Json"/> to your own class!
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DocDataAttribute : Attribute
{
}
