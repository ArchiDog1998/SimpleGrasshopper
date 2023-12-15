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

        if (source.GetType() == type)
        {
            Value = (T)source;
            return true;
        }

        try
        {
            var method = GetOperatorCast(type, type, source.GetType());

            if (method is not null)
            {
                Value = (T)method.Invoke(Value, [Value]);
            }
            else
            {
                Value = (T)source.ChangeType(typeof(T));
            }
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

        if (typeof(Q) == type)
        {
            target = (Q)(object)Value!;
            return true;
        }

        try
        {
            var method = GetOperatorCast(type, typeof(Q), type);

            if (method is not null)
            {
                target = (Q)method.Invoke(Value, [Value]);
            }
            else
            {
                target = (Q)Value!.ChangeType(typeof(Q));
            }

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
            if (m.Name is not "op_Explicit" or "op_Implicit") return false;

            if (m.ReturnType != returnType) return false;

            var parameters = m.GetParameters();

            if (parameters.Length != 1) return false;

            return parameters[0].ParameterType.GetRawType() == paramType;
        });
    }
}