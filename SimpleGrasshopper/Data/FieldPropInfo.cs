using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.Data;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class FieldPropInfo
{
    public readonly PropertyInfo? Property;
    public readonly FieldInfo? Field;
    public Type DeclaringType => Property?.DeclaringType ?? Field?.DeclaringType!;

    public Type DataType => Property?.PropertyType ?? Field?.FieldType!;

    public string Name => Property?.Name ?? Field?.Name!;

    public FieldPropInfo(PropertyInfo info)
    {
        Property = info;
    }

    public FieldPropInfo(FieldInfo info)
    {
        Field = info;
    }

    public string GetDocObjName()
    {
        return Property?.GetDocObjName() ?? Field?.GetDocObjName()!;
    }

    public void SetValue(object obj, object value)
    {
        Property?.SetValue(obj, value);
        Field?.SetValue(obj, value);
    }

    public object? GetValue(object obj)
    {
        return Property?.GetValue(obj) ?? Field?.GetValue(obj);
    }

    public T GetCustomAttribute<T>() where T : Attribute
    {
        return Property?.GetCustomAttribute<T>() ?? Field?.GetCustomAttribute<T>()!;
    }

    public static implicit operator FieldPropInfo(PropertyInfo info) => new (info);
    public static implicit operator FieldPropInfo(FieldInfo info) => new (info);

    public override string ToString() => GetDocObjName();
}
