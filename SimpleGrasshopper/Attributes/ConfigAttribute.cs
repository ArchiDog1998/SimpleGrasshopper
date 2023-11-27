namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Make the field to a configuration.
/// </summary>
/// <param name="name">the name of the config</param>
/// <param name="description">the description of the config</param>
/// <param name="icon">the name or the path of the icon which is embedded in your project.</param>
/// <param name="parent">the name of parent config.</param>
/// <param name="section">the section of this config.</param>
/// <param name="order">the order in that section.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ConfigAttribute(string name, string description = "", string icon = "", string parent = "", byte section = 0, ushort order = 0) : Attribute
{
    internal string Name => name;
    internal string Description => description;
    internal string Icon => icon;
    internal string Parent => parent;
    internal byte Section => section;
    internal ushort Order => order;
}
