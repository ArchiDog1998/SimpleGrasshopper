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
        (typeof(T).GetDocObjName() + " Property",
         typeof(T).GetDocObjNickName() + " Prop",
         typeof(T).GetDocObjDescription(),
         typeof(T).GetAssemblyName(),
         GetSubCate(typeof(T)))
    where T : new()
{
    private readonly List<PropertyParam> _setProps = [], _getProps = [];
    private readonly Guid _guid = typeof(T).GetDocObjGuid();

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

        param.Optional = true;
        return param;
    }

    private static IEnumerable<PropertyInfo> AllProperties => typeof(T).GetRuntimeProperties().Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null);

    /// <inheritdoc/>
    protected sealed override void RegisterInputParams(GH_InputParamManager pManager)
    {
        var keyParam = CreateTypeParam();
        pManager.AddParameter(keyParam, keyParam.Name, keyParam.NickName, keyParam.Description, GH_ParamAccess.item);

        var setProperties = AllProperties.Where(p => p.SetMethod != null && !p.SetMethod.IsStatic).ToArray();

        for (int i = 0; i < setProperties.Length; i++)
        {
            var param = new PropertyParam(setProperties[i], i + 1);

            _setProps.Add(param);

            param.GetNames($"Prop {i}", $"P {i}",
                out var name, out var nickName, out var description);

            pManager.AddParameter(param.CreateParam(), name, nickName, description, param.Access);
        }
    }

    /// <inheritdoc/>
    protected sealed override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        var keyParam = CreateTypeParam();
        pManager.AddParameter(keyParam, keyParam.Name, keyParam.NickName, keyParam.Description, GH_ParamAccess.item);

        var getProperties = AllProperties.Where(p => p.GetMethod != null && !p.GetMethod.IsStatic).ToArray();

        for (int i = 0; i < getProperties.Length; i++)
        {
            var param = new PropertyParam(getProperties[i], i + 1);

            _getProps.Add(param);

            param.GetNames($"Prop {i}", $"P {i}",
                out var name, out var nickName, out var description);

            pManager.AddParameter(param.CreateParam(), name, nickName, description, param.Access);
        }
    }

    /// <inheritdoc/>
    protected sealed override void SolveInstance(IGH_DataAccess DA)
    {
        T obj = default!;
        if (!DA.GetData(0, ref obj))
        {
            obj = new T();
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

        DA.SetData(0, o);
    }
}
