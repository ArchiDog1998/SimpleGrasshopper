using GH_IO.Serialization;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Data;
using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.DocumentObjects;

/// <summary>
/// The property modify component.
/// </summary>
/// <typeparam name="T">the type that it wants to modify.</typeparam>
public abstract class TypePropertyComponent<T>()
    : GH_Component
        (GetName(typeof(T)),
         GetNickName(typeof(T)),
         typeof(T).GetCustomAttribute<PropertyComponentAttribute>()?.Description ?? typeof(T).GetDocObjDescription(),
         typeof(T).GetAssemblyName(),
         GetSubCate(typeof(T)))
{
    private readonly List<PropertyParam> _setProps = [], _getProps = [];
    private readonly Guid _guid = typeof(T).GetDocObjGuid();
    private readonly TypePropertyType _type = typeof(T).GetCustomAttribute<PropertyComponentAttribute>()?.Type ?? TypePropertyType.Property;

    private static string GetName(Type type)
    {
        var attr = type.GetCustomAttribute<PropertyComponentAttribute>();
        if (attr == null) return type.GetDocObjName() + " Property";
        if (attr.Name != null) return attr.Name;

        return type.GetDocObjName() + attr.Type switch
        {
            TypePropertyType.Ctor => " Constructor",
            TypePropertyType.Dtor => " Deconstructor",
            _ => " Property",
        };
    }

    private static string GetNickName(Type type)
    {
        var attr = type.GetCustomAttribute<PropertyComponentAttribute>();
        if (attr == null) return type.GetDocObjNickName() + " Prop";
        if (attr.NickName != null) return attr.NickName;

        return type.GetDocObjNickName() + attr.Type switch
        {
            TypePropertyType.Ctor => " Ctor",
            TypePropertyType.Dtor => " Dtor",
            _ => " Prop",
        };
    }

    private static string GetSubCate(Type type)
    {
        var sub = type.GetCustomAttribute<PropertyComponentAttribute>()?.SubCategory;
        if (sub == null || string.IsNullOrEmpty(sub)) return "Property";
        return sub;
    }

    /// <inheritdoc/>
    public override GH_Exposure Exposure => typeof(T).GetCustomAttribute<PropertyComponentAttribute>()?.Exposure ?? base.Exposure;

    private Bitmap? _icon;
    /// <inheritdoc/>
    protected override Bitmap Icon
    {
        get
        {
            if (_icon != null) return _icon;
            var path = typeof(T).GetCustomAttribute<PropertyComponentAttribute>()?.IconPath;
            if (path == null || string.IsNullOrEmpty(path)) return base.Icon;

            return _icon = GetType().Assembly.GetBitmap(path) ?? base.Icon;
        }
    }


    private IGH_Param CreateTypeParam()
    {
        if (Instances.ComponentServer.EmitObjectProxy(_guid).CreateInstance()
            is not IGH_Param param)
        {
            throw new Exception("The type of this document object is not param!");
        }

        if (_type != TypePropertyType.Dtor)
        {
            param.Optional = true;
        }
        return param;
    }

    private static IEnumerable<PropertyInfo> AllProperties => typeof(T).GetRuntimeProperties().Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null);

    /// <inheritdoc/>
    protected sealed override void RegisterInputParams(GH_InputParamManager pManager)
    {
        int start = 0;
        _setProps.Clear();

        if (_type != TypePropertyType.Ctor)
        {
            var keyParam = CreateTypeParam();
            pManager.AddParameter(keyParam, keyParam.Name, keyParam.NickName, keyParam.Description, GH_ParamAccess.item);
            start++;
        }

        if (_type != TypePropertyType.Dtor)
        {
            var setProperties = AllProperties.Where(p => p.SetMethod != null && !p.SetMethod.IsStatic).ToArray();

            for (int i = 0; i < setProperties.Length; i++)
            {
                var param = new PropertyParam(setProperties[i], i + start);

                _setProps.Add(param);

                param.GetNames($"Prop {i}", $"P {i}",
                    out var name, out var nickName, out var description);

                pManager.AddParameter(param.CreateParam(), name, nickName, description, param.Access);
            }
        }
    }

    /// <inheritdoc/>
    protected sealed override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        int start = 0;
        _getProps.Clear();

        if (_type != TypePropertyType.Dtor)
        {
            var keyParam = CreateTypeParam();
            pManager.AddParameter(keyParam, keyParam.Name, keyParam.NickName, keyParam.Description, GH_ParamAccess.item);
            start++;
        }

        if (_type != TypePropertyType.Ctor)
        {
            var getProperties = AllProperties.Where(p => p.GetMethod != null && !p.GetMethod.IsStatic).ToArray();

            for (int i = 0; i < getProperties.Length; i++)
            {
                var param = new PropertyParam(getProperties[i], i + start);

                _getProps.Add(param);

                param.GetNames($"Prop {i}", $"P {i}",
                    out var name, out var nickName, out var description);

                pManager.AddParameter(param.CreateParam(), name, nickName, description, param.Access);
            }
        }
    }

    /// <inheritdoc/>
    protected sealed override void SolveInstance(IGH_DataAccess DA)
    {
        T obj = default!;
        if (_type == TypePropertyType.Ctor || !DA.GetData(0, ref obj))
        {
            if (typeof(T).IsInterface) return;
            obj = (T)typeof(T).CreateInstance();
        }

        object o = obj!;
        foreach (var prop in _setProps)
        {
            prop.GetValue(DA, ref o, Params.Input[prop.Param.ParamIndex]);
        }

        foreach (var prop in _getProps)
        {
            prop.SetValue(DA, o);
        }

        if (_type != TypePropertyType.Dtor)
        {
            DA.SetData(0, o);
        }
    }

    /// <inheritdoc/>
    public override bool Read(GH_IReader reader)
    {
        reader.Read(this);
        return base.Read(reader);
    }

    /// <inheritdoc/>
    public override bool Write(GH_IWriter writer)
    {
        writer.Write(this);
        return base.Write(writer);
    }
}
