using GH_IO.Serialization;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Util;
using System.Collections;

namespace SimpleGrasshopper.DocumentObjects;

/// <summary>
/// The <see cref="GH_Component"/> that targets to a <see cref="System.Reflection.MethodInfo"/>.
/// </summary>
/// <param name="methodInfos">the method.</param>
public abstract class MethodComponent(params MethodInfo[] methodInfos)
    : GH_TaskCapableComponent<object?[]>
        (methodInfos[0].GetDocObjName(),
         methodInfos[0].GetDocObjNickName(),
         methodInfos[0].GetDocObjDescription(),
         methodInfos[0].GetAssemblyName(),
         methodInfos[0].GetDeclaringClassName())
    , IGH_VariableParameterComponent
{
    private readonly record struct OutputData(string Name, int Index, GH_ParamAccess Access);

    private int _methodIndex = 0;
    private int MethodIndex
    {
        get => _methodIndex;
        set
        {
            if (value == _methodIndex) return;
            var count = methodInfos.Length;
            value = (value + count) % count;

            if (value == _methodIndex) return;
            _methodIndex = value;

            Name = MethodInfo.GetDocObjName();
            NickName = MethodInfo.GetDocObjNickName();
            Description = MethodInfo.GetDocObjDescription();

            //Destroy
            Params.Clear();
            DestroyIconCache();

            //Build
            _changing = true;
            PostConstructor();
            _changing = false;

            //Update
            ExpireSolution(true);
            Attributes.ExpireLayout();
            Instances.ActiveCanvas.Refresh();
        }
    }

    private MethodInfo MethodInfo => methodInfos[MethodIndex];

    /// <inheritdoc/>
    public override GH_Exposure Exposure
    {
        get
        {
            foreach (var method in methodInfos)
            {
                var ex = method.GetCustomAttribute<ExposureAttribute>()?.Exposure;
                if (ex.HasValue) return ex.Value;
            }
            return base.Exposure;
        }
    }

    private readonly Dictionary<int, Bitmap> _icons = [];

    /// <inheritdoc/>
    protected override Bitmap Icon
    {
        get
        {
            if (_icons.TryGetValue(MethodIndex, out Bitmap? icon)) return icon;

            var path = MethodInfo.GetCustomAttribute<IconAttribute>()?.IconPath;
            if (path == null) return base.Icon;

            return _icons[MethodIndex] = GetType().Assembly.GetBitmap(path) ?? base.Icon;
        }
    }

    /// <inheritdoc/>
    protected sealed override void RegisterInputParams(GH_InputParamManager pManager)
    {
        foreach (var param in MethodInfo.GetParameters().Where(p => !p.IsOut))
        {
            if (GetParameter(param, out var access)
                is not IGH_Param gh_param) continue;

            var attr = param.GetCustomAttribute<DocObjAttribute>();
            var defaultName = param.Name ?? string.Empty;

            pManager.AddParameter(gh_param, attr?.Name ?? defaultName, attr?.NickName ?? defaultName, attr?.Description ?? defaultName, access);
        }
    }

    /// <inheritdoc/>
    protected sealed override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        foreach (var param in MethodInfo.GetParameters().Where(p => p.IsOut))
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
            var underType = Enum.GetUnderlyingType(type);
            foreach (object obj in Enum.GetValues(type))
            {
                var v = Convert.ToInt32(Convert.ChangeType(obj, underType));
                integerParam.AddNamedValue(names[index++], v);
            }
        }
        else if (param is Param_Number numberParam && info.GetCustomAttribute<AngleAttribute>() != null)
        {
            numberParam.AngleParameter = true;
        }

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

    /// <inheritdoc/>
    protected sealed override void SolveInstance(IGH_DataAccess DA)
    {
        var ps = MethodInfo.GetParameters();
        if (ps == null) return;

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
                outParams.Add(new(name, index, access));
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

        if (InPreSolve)
        {
            TaskList.Add(Task.Run(() =>
            {
                MethodInfo.Invoke(null, parameters);
                return parameters;
            }));
            return;
        }

        if (!GetSolveResults(DA, out var resultParams))
        {
            MethodInfo.Invoke(null, parameters);
            resultParams = parameters;
        }

        foreach (var param in outParams)
        {
            var result = resultParams[param.Index];
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

            object[] pms = [name,
                access switch
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

    /// <inheritdoc/>
    public override bool Read(GH_IReader reader)
    {
        return reader.TryGetInt32(nameof(_methodIndex), ref _methodIndex)
            && base.Read(reader);
    }

    /// <inheritdoc/>
    public override bool Write(GH_IWriter writer)
    {
        writer.SetInt32(nameof(_methodIndex), _methodIndex);
        return base.Write(writer);
    }

    /// <inheritdoc/>
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalMenuItems(menu);

        var count = methodInfos.Length;
        if (count < 2) return;

        for (int i = 0; i < count; i++)
        {
            var method = methodInfos[i];

            var item = new ToolStripMenuItem
            {
                Text = $"{method.GetDocObjName()} ({method.GetDocObjNickName()})",
                ToolTipText = method.GetDocObjDescription(),
                Checked = i == MethodIndex,
                Tag = i,
            };

            item.Click += (sender, e) =>
            {
                MethodIndex = (int)((ToolStripMenuItem)sender!).Tag;
            };

            menu.Items.Add(item);
        }
    }

    private bool _changing = false;

    /// <inheritdoc/>
    public sealed override void CreateAttributes()
    {
        if (!_changing || m_attributes == null)
        {
            m_attributes = CreateAttribute();
        }
    }

    /// <summary>
    /// Your custom <see cref="IGH_Attributes"/>
    /// </summary>
    /// <returns>the attribute you want.</returns>
    public virtual IGH_Attributes CreateAttribute()
    {
        return new GH_ComponentAttributes(this);
    }

    /// <inheritdoc/>
    public virtual bool CanInsertParameter(GH_ParameterSide side, int index) => false;

    /// <inheritdoc/>
    public virtual bool CanRemoveParameter(GH_ParameterSide side, int index) => false;

    /// <inheritdoc/>
    public virtual IGH_Param CreateParameter(GH_ParameterSide side, int index) => null!;

    /// <inheritdoc/>
    public virtual bool DestroyParameter(GH_ParameterSide side, int index) => false;

    /// <inheritdoc/>
    public virtual void VariableParameterMaintenance()
    {
    }
}
