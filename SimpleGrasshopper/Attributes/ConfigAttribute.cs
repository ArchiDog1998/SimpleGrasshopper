namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Make the field to a configuration.
/// </summary>
/// <param name="name">the name of the config</param>
/// <param name="description">the description of the config</param>
/// <param name="icon">the name or the path of the icon which is embedded in your project.</param>
/// <param name="parent">the name of parent config.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ConfigAttribute(string name, string description = "", string icon = "", string parent = "") : Attribute
{
    internal string Name => name;
    internal string Description => description;
    internal string Icon => icon;
    internal string Parent => parent;
}
