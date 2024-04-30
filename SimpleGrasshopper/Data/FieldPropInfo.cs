using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.Data;
internal class FieldPropInfo
{
    private readonly PropertyInfo? _property;
    private readonly FieldInfo? _field;

    public Type DataType => _property?.PropertyType ?? _field?.FieldType!;

    public string Name => _property?.Name ?? _field?.Name!;

    public FieldPropInfo(PropertyInfo info)
    {
        _property = info;
    }

    public FieldPropInfo(FieldInfo info)
    {
        _field = info;
    }

    public string GetDocObjName()
    {
        return _property?.GetDocObjName() ?? _field?.GetDocObjName()!;
    }

    public void SetValue(object obj, object value)
    {
        _property?.SetValue(obj, value);
        _field?.SetValue(obj, value);
    }

    public object? GetValue(object obj)
    {
        return _property?.GetValue(obj) ?? _field?.GetValue(obj);
    }

    public T GetCustomAttribute<T>() where T : Attribute
    {
        return _property?.GetCustomAttribute<T>() ?? _field?.GetCustomAttribute<T>()!;
    }

    public static implicit operator FieldPropInfo(PropertyInfo info) => new (info);
    public static implicit operator FieldPropInfo(FieldInfo info) => new (info);
}
