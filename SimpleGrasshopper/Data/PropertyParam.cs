using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.Data;

internal readonly struct PropertyParam(FieldPropInfo info, int index)
{
    public TypeParam Param { get; } = new(info.DataType, index);
    public FieldPropInfo PropInfo => info;
    public GH_ParamAccess Access => Param.Access;

    public void GetNames(out string name, out string nickName, out string description)
    {
        var attr = PropInfo.GetCustomAttribute<DocObjAttribute>();

        var propName = PropInfo.Name.Split('.').LastOrDefault();

        var defaultName = propName.SpaceStr();
        var defaultNickName = propName.UpperStr();

        name = attr?.Name ?? defaultName;
        nickName = attr?.NickName ?? defaultNickName;
        description = attr?.Description ?? defaultName;

        if (PropInfo.GetCustomAttribute<RangeAttribute>() is RangeAttribute range)
        {
            description += $"\nFrom {range.MinD} To {range.MaxD}";
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

        SimpleUtils.SetSpecial(ref param, Param.RawInnerTypeNoGoo,
            PropInfo.GetCustomAttribute<AngleAttribute>() != null,
            PropInfo.GetCustomAttribute<HiddenAttribute>() != null);

        param.Optional = true;

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
            ? SimpleUtils.ModifyRange(ref value, range, Access) : [];

        SimpleUtils.ModifyAngle(ref value, param);

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
