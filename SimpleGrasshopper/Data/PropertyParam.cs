using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.Data;
internal readonly struct PropertyParam(PropertyInfo info, int index)
{
    public TypeParam Param { get; } = new(info.PropertyType, index);
    public PropertyInfo PropInfo => info;
    public GH_ParamAccess Access => Param.Access;

    public void GetNames(string defaultName, string defaultNickName, out string name, out string nickName, out string description)
    {
        var attr = PropInfo.GetCustomAttribute<DocObjAttribute>();
        defaultName = PropInfo.Name ?? defaultName;
        defaultNickName = PropInfo.Name ?? defaultNickName;

        name = attr?.Name ?? defaultName;
        nickName = attr?.NickName ?? defaultNickName;
        description = attr?.Description ?? defaultName;

        if (PropInfo.GetCustomAttribute<RangeAttribute>() is RangeAttribute range)
        {
            description += $"\nFrom {range.Min} To {range.Max}";
        }
    }

    public IGH_Param CreateParam()
    {
        var proxy = Instances.ComponentServer.EmitObjectProxy(
            PropInfo.GetCustomAttribute<ParamAttribute>()?.Guid ?? Param.ComponentGuid);

        if (proxy.CreateInstance() is not IGH_Param param)
        {
            throw new ArgumentException("The guid is not valid for creating a IGH_Param!");
        }

        param.Optional = true;

        Utils.SetSpecial(ref param, Param.RawInnerType,
            PropInfo.GetCustomAttribute<AngleAttribute>() != null,
            PropInfo.GetCustomAttribute<HiddenAttribute>() != null);
        return param;
    }

    public bool GetValue(IGH_DataAccess DA, ref object obj, IGH_Param param)
    {
        if (!Param.GetValue(DA, out var value))
        {
            return false;
        }

        //Modify range
        var messages = PropInfo.GetCustomAttribute<RangeAttribute>() is RangeAttribute range
            ? Utils.ModifyRange(ref value, range, Access) : [];

        Utils.ModifyAngle(ref value, param);

        PropInfo.SetValue(obj, value);

        param.AddRuntimeMessages(messages);

        return true;
    }

    public bool SetValue(IGH_DataAccess DA, object obj)
    {
        var value = PropInfo.GetValue(obj);
        return Param.SetValue(DA, value!);
    }
}
