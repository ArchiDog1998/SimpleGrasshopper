using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Util;
using System.Collections;
using System.Drawing;

namespace SimpleGrasshopper.DocumentObjects;

public abstract class MethodComponent(MethodInfo methodInfo) 
    : GH_Component(methodInfo.GetDocObjName(), 
                   methodInfo.GetDocObjNickName(), 
                   methodInfo.GetDocObjDescription(), 
                   methodInfo.GetAssemblyName(),
                   methodInfo.GetDeclaringClassName())
{
    public override GH_Exposure Exposure => methodInfo.GetCustomAttribute< ExposureAttribute>()?.Exposure ?? base.Exposure;

    private Bitmap? _icon;
    protected override Bitmap Icon
    {
        get
        {
            if (_icon != null) return _icon;
            var path = methodInfo.GetCustomAttribute<IconAttribute>()?.IconPath;
            if (path == null) return base.Icon;

            var assembly = GetType().Assembly;
            var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(path));
            if(name == null) return base.Icon;
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null) return base.Icon;
            return _icon = new (stream);
        }
    }

    protected sealed override void RegisterInputParams(GH_InputParamManager pManager)
    {
        foreach (var param in methodInfo.GetParameters().Where(p => !p.IsOut))
        {
            if (GetParameter(param, out var access)
                is not IGH_Param gh_param) continue;

            var attr = param.GetCustomAttribute<DocObjAttribute>();
            var defaultName = param.Name ?? string.Empty;

            pManager.AddParameter(gh_param, attr?.Name ?? defaultName, attr?.NickName ?? defaultName, attr?.Description ?? defaultName, access);
        }
    }

    protected sealed override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        foreach (var param in methodInfo.GetParameters().Where(p => p.IsOut))
        {
            if (GetParameter(param, out var access)
                is not IGH_Param gh_param) continue;

            var attr = param.GetCustomAttribute<DocObjAttribute>();
            var defaultName = param.Name ?? string.Empty;

            pManager.AddParameter(gh_param, attr?.Name ?? defaultName, attr?.NickName ?? defaultName, attr?.Description ?? defaultName, access);
        }
    }

    private static void GetAccessAndType(ref Type type, out GH_ParamAccess access)
    {
        access = GH_ParamAccess.item;

        if (type.IsGeneralType(typeof(GH_Structure<>)) is Type treeType)
        {
            type = treeType;
            access = GH_ParamAccess.tree;
        }
        else if (type.IsGeneralType(typeof(List<>)) is Type listType)
        {
            type = listType;
            access = GH_ParamAccess.list;
        }
    }

    private static IGH_Param? GetParameter(ParameterInfo info, out GH_ParamAccess access)
    {
        access = GH_ParamAccess.item;

        var type = info.ParameterType.GetRawType();
        if (type == null) return null;

        GetAccessAndType(ref type, out access);

        var proxy = Instances.ComponentServer.EmitObjectProxy(
            info.GetCustomAttribute<ParamAttribute>()?.Guid ?? type.GetDocObjGuid());

        if (proxy.CreateInstance() is not IGH_Param param) return null;

        SetOptional(info, param, access);

        if (type.IsEnum && param is Param_Integer integerParam)
        {
            var names = Enum.GetNames(type);
            int index = 0;
            foreach (int v in Enum.GetValues(type))
            {
                integerParam.AddNamedValue(names[index++], v);
            }
        }
        else if(param is Param_Number numberParam && info.GetCustomAttribute<AngleAttribute>() != null)
        {
            numberParam.AngleParameter = true;
        }

        //TODO: Path Type modify!

        return param;

        static void SetOptional(ParameterInfo info, IGH_Param param, GH_ParamAccess access)
        {
            if (access == GH_ParamAccess.item && info.DefaultValue != null)
            {
                SetPersistentData(ref param, info.DefaultValue);
            }
            else if (info.HasDefaultValue)
            {
                param.Optional = true;
            }

            static void SetPersistentData(ref IGH_Param param, object data)
            {
                var persistType = typeof(GH_PersistentParam<>);
                if (param.GetType().IsGeneralType(persistType) is not Type persistParam) return;

                var method = persistType.MakeGenericType(persistParam).GetRuntimeMethod("SetPersistentData", [typeof(object[])]);

                if (method == null) return;
                method.Invoke(param, [new object[] { data }]);
            }
        }
    }

    protected sealed override void SolveInstance(IGH_DataAccess DA)
    {
        var ps = methodInfo.GetParameters();
        if(ps == null) return;

        var outParams = new List<OutputData>(ps.Length);

        int index = -1;
        var parameters = ps.Select(param =>
        {
            index++;

            var type = param.ParameterType.GetRawType();
            if (type == null) return null;

            GetAccessAndType(ref type, out var access);

            var name = param.GetCustomAttribute<DocObjAttribute>()?.Name
                ?? param.Name ?? string.Empty;

            if (param.IsOut)
            {
                outParams.Add(new (name, index, access));
                return access switch
                {
                    GH_ParamAccess.list => new List<object>(),
                    GH_ParamAccess.tree => new GH_Structure<IGH_Goo>(),
                    _ => null,
                };
            }
            else
            {
                return GetValue(DA, name, type, access);
            }
        }).ToArray();

        methodInfo.Invoke(null, parameters);

        foreach (var param in outParams)
        {
            var result = parameters[param.Index];
            switch (param.Access)
            {
                case GH_ParamAccess.tree:
                    DA.SetDataTree(Params.Output.FindIndex(p => p.Name == param.Name), result as IGH_Structure);
                    break;

                case GH_ParamAccess.list:
                    DA.SetDataList(param.Name, result as IEnumerable);
                    break;

                default:
                    DA.SetData(param.Name, result);
                    break;
            }
        }

        static object GetValue(IGH_DataAccess DA, string name, Type type, GH_ParamAccess access)
        {
            MethodInfo method = access switch
            {
                GH_ParamAccess.list => GetDaMethod(DA, nameof(DA.GetDataList)),
                GH_ParamAccess.tree => GetDaMethod(DA, nameof(DA.GetDataTree)),
                _ => GetDaMethod(DA, nameof(DA.GetData)),
            };

            object[] pms = [name, access switch
                {
                    GH_ParamAccess.list => Activator.CreateInstance(typeof(List<>).MakeGenericType(type))!,
                    GH_ParamAccess.tree => Activator.CreateInstance(typeof(GH_Structure<>).MakeGenericType(type))!,
                    _ => type.IsEnum ? 0 : type.IsValueType ? Activator.CreateInstance(type)! : null!,
                }];

            method.MakeGenericMethod(type.IsEnum ? typeof(int) : type).Invoke(DA, pms);
            return access != GH_ParamAccess.item ? pms[1] : pms[1].ChangeType(type);

            static MethodInfo GetDaMethod(IGH_DataAccess DA, string name)
            {
                return DA.GetType().GetRuntimeMethods().First(m =>
                {
                    if (m.Name != name) return false;
                    var pms = m.GetParameters();
                    if (pms.Length != 2) return false;
                    if (pms[0].ParameterType.GetRawType() != typeof(string)) return false;
                    return true;
                });
            }
        }
    }

    private readonly record struct OutputData(string Name, int Index, GH_ParamAccess Access);
}
