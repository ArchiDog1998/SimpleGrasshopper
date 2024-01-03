namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Tag the property that should be saved into the <see cref="GH_Document"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DocDataAttribute : Attribute
{
}
