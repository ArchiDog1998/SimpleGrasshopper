namespace SimpleGrasshopper.Attributes;

/// <summary>
/// The short cut keys for the config.
/// </summary>
/// <param name="shortcut"></param>
/// <param name="displayString"></param>
/// <param name="showShortcut"></param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ShortcutAttribute(Keys shortcut, string? displayString = null, bool showShortcut = true) : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public Keys ShortcutKey => shortcut;

    /// <summary>
    /// 
    /// </summary>
    public string? DisplayString => displayString;

    /// <summary>
    /// 
    /// </summary>
    public bool ShowShortcut => showShortcut;
}
