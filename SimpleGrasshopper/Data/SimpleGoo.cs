using Grasshopper.Kernel.Types;
using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.Data;

public class SimpleGoo<T> : GH_Goo<T>
{
    public override bool IsValid => true;

    public override string TypeName => typeof(T).Name;

    public override string TypeDescription => TypeName;

    public SimpleGoo(T value) : base(value) { }

    public SimpleGoo() : base() { }

    public override IGH_Goo Duplicate() => new SimpleGoo<T>(Value);

    public override string ToString() => Value?.ToString() ?? TypeName;

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