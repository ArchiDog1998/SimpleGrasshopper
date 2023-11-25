namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Tag the class to add the setting that you want.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SettingAttribute() : Attribute
{
}
