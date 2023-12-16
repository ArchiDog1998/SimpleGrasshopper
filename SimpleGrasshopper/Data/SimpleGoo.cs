using Grasshopper.Kernel.Types;
using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.Data;

/// <summary>
/// A simple type of the <see cref="GH_Goo{T}"/>. It just contains the basic of the goo members.
/// </summary>
/// <typeparam name="T"></typeparam>
public class SimpleGoo<T> : GH_Goo<T>
{
    /// <inheritdoc/>
    public override bool IsValid => true;

    /// <inheritdoc/>
    public override string TypeName => typeof(T).Name;

    /// <inheritdoc/>
    public override string TypeDescription => TypeName;

    /// <inheritdoc/>
    public SimpleGoo(T value) : base(value) { }

    /// <inheritdoc/>
    public SimpleGoo() : base() { }

    /// <inheritdoc/>
    public override IGH_Goo Duplicate() => new SimpleGoo<T>(Value);

    /// <inheritdoc/>
    public override string ToString() => Value?.ToString() ?? TypeName + " <Null>";

    /// <inheritdoc/>
    public override bool CastFrom(object source)
    {
        var type = typeof(T);
        var sType = source.GetType();

        if (sType == type)
        {
            Value = (T)source;
            return true;
        }

        try
        {
            if (GetOperatorCast(type, type, sType) is MethodInfo method)
            {
                Value = (T)method.Invoke(null, [source]);
                return true;
            }

            if (source is IGH_Goo
                && sType.GetRuntimeProperty("Value") is PropertyInfo property
                && GetOperatorCast(type, type, property.PropertyType) is MethodInfo method1)
            {
                var v = property.GetValue(source);
                Value = (T)method1.Invoke(null, [v]);
                return true;
            }

            Value = (T)source.ChangeType(typeof(T));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public override bool CastTo<Q>(ref Q target)
    {
        var type = typeof(T);
        var QType = typeof(Q);

        if (QType == type)
        {
            target = (Q)(object)Value!;
            return true;
        }

        try
        {
            if (GetOperatorCast(type, QType, type) is MethodInfo method)
            {
                target = (Q)method.Invoke(null, [Value]);
                return true;
            }

            if (target is IGH_Goo 
                && QType.GetRuntimeProperty("Value") is PropertyInfo property
                && GetOperatorCast(type, property.PropertyType, type) is MethodInfo method1)
            {
                var v = method1.Invoke(null, [Value]);
                property.SetValue(target, v);
                return true;
            }

            target = (Q)Value!.ChangeType(QType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static MethodInfo? GetOperatorCast(Type type, Type returnType, Type paramType)
    {
        return type.GetRuntimeMethods().FirstOrDefault(m =>
        {
            if (!m.IsSpecialName) return false;
            if (m.Name is not "op_Explicit" and not "op_Implicit") return false;

            if (m.ReturnType != returnType) return false;

            var parameters = m.GetParameters();

            if (parameters.Length != 1) return false;

            return parameters[0].ParameterType.GetRawType() == paramType;
        });
    }
}