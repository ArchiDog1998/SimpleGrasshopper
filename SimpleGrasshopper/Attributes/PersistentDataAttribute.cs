using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Add this to the parameter that you need to add persistent data.
/// </summary>
/// <param name="propertyNameOrFieldName"></param>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class PersistentDataAttribute(string propertyNameOrFieldName) : Attribute
{
    /// <summary>
    /// The name of the property or field.
    /// </summary>
    public string Name => propertyNameOrFieldName;

    internal object? GetValue(Type owner)
    {
        var field = owner.GetAllRuntimeFields().FirstOrDefault(f => f.Name == Name);

        if (field != null && field.IsStatic)
        {
            return field.GetValue(null);
        }

        var property = owner.GetAllRuntimeProperties().FirstOrDefault(p => p.Name == Name);
        if (property != null && (property.GetMethod?.IsStatic ?? false))
        {
           return property.GetValue(null);
        }

        return null;
    }
}
