using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.Data;

internal readonly struct MemberParam(string name, Type type, int index, bool isIn, bool isOut)
{
    private readonly FieldInfo? _field = type.GetRuntimeFields().FirstOrDefault(f => f.Name == name);
    private readonly PropertyInfo? _prop = type.GetRuntimeProperties().FirstOrDefault(f => f.Name == name);

    public int MethodParamIndex => index;

    public Type? Type => _field?.FieldType ?? _prop?.PropertyType;

    public object? GetValue(object obj)
    {
        return isIn 
            ? _field?.GetValue(obj) ?? _prop?.GetValue(obj) 
            : Type?.CreateInstance() ?? null;
    }

    public void SetValue(object obj, object value)
    {
        if (!isOut) return;
        _field?.SetValue(obj, value);
        _prop?.SetValue(obj, value);
    }

    public static bool IsMemberParam(ParameterInfo parameterInfo)
    {
        return parameterInfo.Name.StartsWith("__");
    }
}
