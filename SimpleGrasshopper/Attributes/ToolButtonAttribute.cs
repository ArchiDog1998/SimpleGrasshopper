namespace SimpleGrasshopper.Attributes;

/// <summary>
/// The tool button config.
/// </summary>
/// <param name="icon">the icons of the tool button.</param>
/// <param name="description">the description of the tool button.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ToolButtonAttribute(string icon, string description = "") : Attribute
{
    internal string Icon => icon;
    internal string Description => description;
}
