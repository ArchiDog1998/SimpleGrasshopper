using Grasshopper.Kernel.Data;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Util;
using System.Collections;

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
    private static string GetSubCate(Type type)
    {
        var sub = type.GetCustomAttribute<PropertyComponentAttribute>()?.SubCategory;
        if (!string.IsNullOrEmpty(sub)) return sub;
        return "Property";
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
            if (string.IsNullOrEmpty(path)) return base.Icon;

            return _icon = GetType().Assembly.GetBitmap(path) ?? base.Icon;
        }
    }

    private static IGH_Param GetTypeParam(Type t, Guid? guid = null)
    {
        if (Instances.ComponentServer.EmitObjectProxy(guid ?? t.GetDocObjGuid()).CreateInstance()
            is not IGH_Param param)
        {
            throw new Exception("The type of this document object is not param!");
        }

        param.Optional = true;
        return param;
    }

    /// <inheritdoc/>
    protected sealed override void RegisterInputParams(GH_InputParamManager pManager)
    {
        var keyParam = GetTypeParam(typeof(T));
        pManager.AddParameter(keyParam, keyParam.Name, keyParam.NickName, keyParam.Description, GH_ParamAccess.item);

        foreach (var prop in typeof(T).GetRuntimeProperties().Where(p => p.SetMethod != null && !p.SetMethod.IsStatic))
        {
            var attr = prop.GetCustomAttribute<DocObjAttribute>();
            var param = GetTypeParam(prop.PropertyType, prop.GetCustomAttribute<ParamAttribute>()?.Guid);
            prop.PropertyType.GetAccessAndType(out var access);

            pManager.AddParameter(param, attr?.Name ?? prop.Name, attr?.NickName ?? prop.Name, attr?.Description ?? prop.Name, access);
        }
    }

    /// <inheritdoc/>
    protected sealed override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        var keyParam = GetTypeParam(typeof(T));
        pManager.AddParameter(keyParam, keyParam.Name, keyParam.NickName, keyParam.Description, GH_ParamAccess.item);

        foreach (var prop in typeof(T).GetRuntimeProperties().Where(p => p.GetMethod != null && !p.GetMethod.IsStatic))
        {
            var attr = prop.GetCustomAttribute<DocObjAttribute>();
            var param = GetTypeParam(prop.PropertyType, prop.GetCustomAttribute<ParamAttribute>()?.Guid);
            prop.PropertyType.GetAccessAndType(out var access);

            pManager.AddParameter(param, attr?.Name ?? prop.Name, attr?.NickName ?? prop.Name, attr?.Description ?? prop.Name, access);
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

        var setProps = typeof(T).GetRuntimeProperties().Where(p => p.SetMethod != null && !p.SetMethod.IsStatic).ToArray();

        for (int i = 0; i < setProps.Length; i++)
        {
            var prop = setProps[i];
            var type = prop.PropertyType.GetRawType();
            type = type.GetAccessAndType(out var access);
            if (DA.GetValue(i + 1, type, access, out var value))
            {
                prop.SetValue(obj, value);
            }
        }

        DA.SetData(0, obj);

        var getProps = typeof(T).GetRuntimeProperties().Where(p => p.GetMethod != null && !p.GetMethod.IsStatic).ToArray();

        for (int i = 0; i < getProps.Length; i++)
        {
            var prop = getProps[i];
            var type = prop.PropertyType.GetRawType();
            var value = prop.GetValue(obj);
            type.GetAccessAndType(out var access);
            switch (access)
            {
                case GH_ParamAccess.item:
                    DA.SetData(i + 1, value);
                    break;
                case GH_ParamAccess.list:
                    DA.SetDataList(i + 1, value as IEnumerable);
                    break;
                case GH_ParamAccess.tree:
                    DA.SetDataTree(i + 1, value as IGH_Structure);
                    break;
            }
        }
    }
}
