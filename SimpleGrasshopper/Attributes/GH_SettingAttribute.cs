namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Tag the class to add the setting that you want.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class GH_SettingAttribute() : Attribute
{
}
