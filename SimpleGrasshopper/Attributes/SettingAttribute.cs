namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Tag the class to add the setting that you want. 
/// If you want to use it into your own class, please add something in <see cref="Newtonsoft.Json"/> to your own class!
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SettingAttribute() : Attribute
{
}
