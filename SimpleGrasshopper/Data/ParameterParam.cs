using Grasshopper.Kernel.Data;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.Data;

internal readonly struct ParameterParam(ParameterInfo info, int index, int methodIndex)
{
    public int MethodParamIndex => methodIndex;

    public TypeParam Param { get; } = new(info.ParameterType, index);

    public ParameterInfo ParamInfo => info;

    public GH_ParamAccess Access => Param.Access;

    public void GetNames(string defaultName, string defaultNickName, out string name, out string nickName, out string description)
    {
        var attr = ParamInfo.GetCustomAttribute<DocObjAttribute>();
        var defaultValue = ParamInfo.Name;
        if (defaultValue is "value" or "obj" or "@object")
        {
            defaultValue = null;
        }

        defaultName = defaultValue ?? defaultName;
        defaultNickName = defaultValue ?? defaultNickName;

        defaultName = defaultName.SpaceStr();
        defaultNickName = defaultNickName.UpperStr();

        name = attr?.Name ?? defaultName;
        nickName = attr?.NickName ?? defaultNickName;
        description = attr?.Description ?? defaultName;
        if (ParamInfo.GetCustomAttribute<RangeAttribute>() is RangeAttribute range)
        {
            description += $"\nFrom {range.MinD} To {range.MaxD}";
        }
    }

    public IGH_Param CreateParam(out bool principal, Type? owner = null)
    {
        var proxy = Instances.ComponentServer.EmitObjectProxy(
            ParamInfo.GetCustomAttribute<ParamAttribute>()?.Guid ?? Param.ComponentGuid);

        if (proxy.CreateInstance() is not IGH_Param param)
        {
            throw new ArgumentException("The guid is not valid for creating a IGH_Param!");
        }

        SimpleUtils.SetSpecial(ref param, Param.RawInnerTypeNoGoo,
            ParamInfo.GetCustomAttribute<AngleAttribute>() != null,
            ParamInfo.GetCustomAttribute<HiddenAttribute>() != null);

        if (owner != null)
        {
            SetOptional(ParamInfo, owner, param);
        }

        principal = ParamInfo.GetCustomAttribute<ParamTagAttribute>() is ParamTagAttribute tag
            && SetTags(param, tag);

        param.CreateAttributes();

        return param;

        static void SetOptional(ParameterInfo info, Type owner, IGH_Param param)
        {
            var data = info.HasDefaultValue ? info.DefaultValue : null;

            if (info.GetCustomAttribute<PersistentDataAttribute>()
                is PersistentDataAttribute persist)
            {
                data = persist.GetValue(owner) ?? data;
            }

            if (data != null)
            {
                SimpleUtils.SetPersistentData(ref param, data);
            }
            else if (info.HasDefaultValue)
            {
                param.Optional = true;
            }
        }

        static bool SetTags(IGH_Param param, ParamTagAttribute tag)
        {
            var props = param.GetType().GetAllRuntimeProperties();
            props.FirstOrDefault(p => p.Name == "Reverse")?.SetValue(param, tag.Reverse);
            props.FirstOrDefault(p => p.Name == "Simplify")?.SetValue(param, tag.Simplify);
            props.FirstOrDefault(p => p.Name == "DataMapping")?.SetValue(param, tag.Mapping);

            return tag.Principal;
        }
    }

    public bool GetValue(IGH_DataAccess DA, out object value, IGH_Param param)
    {
        if (!Param.GetValue(DA, out value))
        {
            return false;
        }

        //Modify range
        var messages = ParamInfo.GetCustomAttribute<RangeAttribute>() is RangeAttribute range
            ? SimpleUtils.ModifyRange(ref value, range, Access) : [];

        SimpleUtils.ModifyAngle(ref value, param);

        param.AddRuntimeMessages(messages);

        return true;
    }

    public bool SetValue(IGH_DataAccess DA, object value) => Param.SetValue(DA, value);
}
