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
    public override string ToString() => Value?.ToString() ?? TypeName;

    /// <inheritdoc/>
    public override bool CastFrom(object source)
    {
        try
        {
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
        try
        {
            target = (Q)Value!.ChangeType(typeof(Q));
            return true;
        }
        catch
        {
            return false;
        }
    }
}