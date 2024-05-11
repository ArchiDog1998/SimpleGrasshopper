using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.Kernel.Attributes;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Data;
using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.DocumentObjects;

/// <summary>
/// The <see cref="GH_Component"/> that targets to a <see cref="System.Reflection.MethodInfo"/>.
/// </summary>
public abstract class MethodComponent(
        MethodInfo[] methodInfos,
        string? name = null,
        string? nickName = null,
        string? description = null,
        string? subCategory = null,
        string? iconPath = null,
        GH_Exposure? exposure = null,
        string? message = null,
        bool isParallel = false)
    : GH_TaskCapableComponent<(object, object?[])>(
        name ?? methodInfos[0].GetDocObjName(),
        nickName ?? methodInfos[0].GetDocObjNickName(),
        description ?? methodInfos[0].GetDocObjDescription(),
        string.Empty,
        subCategory ?? methodInfos[0].GetDeclaringClassName())
{
    private TypeParam? _declarationParam = null;
    private readonly List<ParameterParam> _inputParams = [], _outputParams = [];
    private readonly List<MemberParam> _memberParams = [];
    private ParameterParam? _resultParam = null;

    private int _methodIndex = -1;

    /// <summary>
    /// The index of the method.
    /// </summary>
    public int MethodIndex
    {
        get => _methodIndex;
        set
        {
            if (value == _methodIndex) return;
            var count = methodInfos.Length;
            value = (value + count) % count;

            if (value == _methodIndex) return;

            _methodIndex = value;

            if (name == null)
            {
                Name = MethodInfo.GetDocObjName();
            }
            if (nickName == null)
            {
                NickName = MethodInfo.GetDocObjNickName();
            }
            if (description == null)
            {
                Description = MethodInfo.GetDocObjDescription();
            }

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

    /// <summary>
    /// Add the button about if the component should add the run button.
    /// </summary>
    public virtual bool AddShouldRun => MethodInfo.GetCustomAttribute<RunButtonAttribute>() != null;

    private bool _shouldRun = false;

    /// <summary>
    /// Should this component run the instances.
    /// </summary>
    public virtual bool ShouldRun 
    {
        get
        {
            if (!AddShouldRun) return true;
            return _shouldRun;
        }
        set => _shouldRun = value;
    } 

    /// <summary>
    /// The declaring type of this method.
    /// </summary>
    protected virtual Type? DeclaringType { get; } = null;

    /// <summary>
    /// The method that this component is using.
    /// </summary>
    public MethodInfo MethodInfo => methodInfos[Math.Max(0, MethodIndex)];

    /// <inheritdoc/>
    public override string Category
    {
        get => GetType().GetAssemblyName();
        set => base.Category = value;
    }

    /// <inheritdoc/>
    public override GH_Exposure Exposure
    {
        get
        {
            if (exposure.HasValue) return exposure.Value;

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

            var path = MethodIndex < 0
                ? iconPath ?? MethodInfo.GetCustomAttribute<IconAttribute>()?.IconPath
                : MethodInfo.GetCustomAttribute<IconAttribute>()?.IconPath ?? iconPath;

            if (path == null) return base.Icon;

            return _icons[MethodIndex] = GetType().Assembly.GetBitmap(path) ?? base.Icon;
        }
    }
    /// <inheritdoc/>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        var paramIndex = pManager.ParamCount;
        if (CanCreateDeclaringType(out var param))
        {
            _declarationParam = param;

            param.GetNames("Object", "Obj",
                out var name, out var nickName, out var description);

            pManager.AddParameter(param.CreateParam(), name, nickName, description, param.Access);
            paramIndex++;
        }

        _inputParams.Clear();
        _memberParams.Clear();
        var parameters = MethodInfo.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (MemberParam.IsMemberParam(parameter))
            {
                _memberParams.Add(new MemberParam(parameter.Name.Substring(2), this.GetType(), i, parameter.IsIn(), parameter.IsOut()));
                continue;
            }

            if (!parameter.IsIn()) continue;

            var p = new ParameterParam(parameter, paramIndex++, i);

            _inputParams.Add(p);
            p.GetNames(p.Param.RawInnerType.Name, p.Param.RawInnerType.Name,
                out var name, out var nickName, out var description);

            pManager.AddParameter(p.CreateParam(out var principal, MethodInfo.DeclaringType), name, nickName, description, p.Access);
            if (principal)
            {
                this.PrincipalParameterIndex = paramIndex - 1;
            }
        }

        this.Message = message ?? MethodInfo.GetCustomAttribute<MessageAttribute>()?.Message;
        this.UseTasks = isParallel || MethodInfo.GetCustomAttribute<ParallelAttribute>() != null;
    }

    /// <inheritdoc/>
    public override void AddedToDocument(GH_Document document)
    {
        this.UseTasks = isParallel || MethodInfo.GetCustomAttribute<ParallelAttribute>() != null;
        if (MethodIndex < 0)
        {
            _methodIndex = 0;
        }
        base.AddedToDocument(document);
    }

    /// <inheritdoc/>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        var paramIndex = pManager.ParamCount;
        if (CanCreateDeclaringType(out var tp))
        {
            _declarationParam = tp;

            tp.GetNames(tp.RawInnerType.Name, tp.RawInnerType.Name,
                out var name, out var nickName, out var description);

            pManager.AddParameter(tp.CreateParam(), name, nickName, description, tp.Access);
            paramIndex++;
        }

        if (MethodInfo.ReturnParameter is ParameterInfo paramInfo
            && paramInfo.ParameterType != typeof(void)
            && paramInfo.ParameterType != typeof(RuntimeData))
        {
            var param = new ParameterParam(paramInfo, paramIndex++, 0);

            _resultParam = param;

            param.GetNames(param.Param.RawInnerType.Name, param.Param.RawInnerType.Name,
                out var name, out var nickName, out var description);

            pManager.AddParameter(param.CreateParam(out var principal), name, nickName, description, param.Access);
            if (principal)
            {
                this.PrincipalParameterIndex = paramIndex - 1;
            }
        }
        else
        {
            _resultParam = null;
        }

        _outputParams.Clear();
        var parameters = MethodInfo.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (MemberParam.IsMemberParam(parameter)) continue;
            if (!parameters[i].IsOut()) continue;

            var p = new ParameterParam(parameters[i], paramIndex++, i);

            _outputParams.Add(p);

            p.GetNames(p.Param.RawInnerType.Name, p.Param.RawInnerType.Name,
                out var name, out var nickName, out var description);

            pManager.AddParameter(p.CreateParam(out var principal), name, nickName, description, p.Access);
            if (principal)
            {
                this.PrincipalParameterIndex = paramIndex - 1;
            }
        }
    }

    private bool CanCreateDeclaringType(out TypeParam param)
    {
        param = default!;
        if (MethodInfo.IsStatic) return false;
        if (MethodInfo.DeclaringType is not Type t) return false;
        param = new(t, 0);
        return true;
    }

    private object?[] GetParameters(IGH_DataAccess DA, out object obj)
    {
        var count = -1;
        if (_inputParams.Count > 0)
        {
            count = Math.Max(count, _inputParams.Max(p => p.MethodParamIndex));
        }
        if (_outputParams.Count > 0)
        {
            count = Math.Max(count, _outputParams.Max(p => p.MethodParamIndex));
        }
        if (_memberParams.Count > 0)
        {
            count = Math.Max(count, _memberParams.Max(p => p.MethodParamIndex));
        }
        var result = new object?[count + 1];

        foreach (var param in _outputParams)
        {
            result[param.MethodParamIndex] = param.Param.Type.CreateInstance();
        }
        foreach (var param in _inputParams)
        {
            var ghParam = Params.Input[param.Param.ParamIndex];
            result[param.MethodParamIndex] = param.GetValue(DA, out var value, ghParam)
                ? value : param.Param.Type.CreateInstance(true);
        }
        foreach (var param in _memberParams)
        {
            result[param.MethodParamIndex] = param.GetValue(this);
        }

        if (_declarationParam is null || !_declarationParam.GetValue(DA, out obj)) obj = null!;
        return result;
    }

    private void SetParameters(IGH_DataAccess DA, object obj, object result, object?[] parameters)
    {
        _declarationParam?.SetValue(DA, obj);


        if (_resultParam.HasValue)
        {
            _resultParam.Value.SetValue(DA, result);
        }

        foreach (var param in _outputParams)
        {
            param.SetValue(DA, parameters[param.MethodParamIndex]!);
        }
        foreach (var param in _memberParams)
        {
            param.SetValue(this, parameters[param.MethodParamIndex]!);
        }
    }

    /// <inheritdoc/>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        if (Instances.DocumentEditor == null) return;
        if (!ShouldRun) return;

        try
        {
            var parameters = GetParameters(DA, out var obj);

            if (InPreSolve)
            {
                TaskList.Add(Task.Run(() =>
                {
                    return (MethodInfo.Invoke(obj, parameters), parameters);
                }));
                return;
            }

            if (!GetSolveResults(DA, out var resultParams))
            {
                resultParams = (MethodInfo.Invoke(obj, parameters), parameters);
            }

            if (resultParams.Item1 is RuntimeData data)
            {
                if (data.Message != null)
                {
                    this.Message = data.Message;
                }
                if (data.RuntimeMessages != null)
                {
                    this.AddRuntimeMessages(data.RuntimeMessages);
                }
            }

            SetParameters(DA, obj, resultParams.Item1, resultParams.Item2);
        }
        catch (AggregateException ex) when (ex.InnerException is TargetInvocationException)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.InnerException.InnerException.Message);
        }
        catch (TargetInvocationException ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.InnerException.Message);
        }
        catch (Exception ex) when (ex.Message.StartsWith("%SimpleGrasshopper_RangeSetting"))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message.Substring(31));
        }
        catch (Exception? ex)
        {
            var messageString = GetMessage(ex);
            ex = ex.InnerException;

            while (ex != null)
            {
                messageString += "\n" + GetMessage(ex);
                ex = ex.InnerException;
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, messageString);

            static string GetMessage(Exception ex)
            {
                var message = ex.Message;
                if (ex.StackTrace != null)
                {
                    message += "\n" + ex.StackTrace;
                }
                return message;
            }
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

    private readonly struct MemberShowing(MethodInfo method, int index)
    {
        public MethodInfo Method => method;
        public int Index => index;

        public static string GetString(MethodInfo method)
        {
            var name = method.GetDocObjName();
            var nickName = method.GetDocObjNickName();
            if (name == nickName) return name;
            return $"{name} ({nickName})";
        }

        public override string ToString() => GetString(Method);
    }

    /// <inheritdoc/>
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalMenuItems(menu);

        var count = methodInfos.Length;

        if (count < 2)
        {
            return;
        }
        else if (count > 10)
        {
            List<MemberShowing> items = [];
            for (int i = 0; i < count; i++)
            {
                var method = methodInfos[i];
                items.Add(new MemberShowing(method, i));
            }

            SimpleUtils.SearchDropdown(menu, [.. items], i => MethodIndex == i.Index, SelectionMode.One, (s, e) =>
            {
                if (s is not ListBox listBox
                    || listBox.SelectedItem is not MemberShowing member) return;

                this.RecordDocumentObjectMember(nameof(MethodIndex), Undo.AfterUndo.None);

                MethodIndex = member.Index;
            });
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                var method = methodInfos[i];

                var item = new ToolStripMenuItem
                {
                    Text = MemberShowing.GetString(method),
                    ToolTipText = method.GetDocObjDescription(),
                    Checked = i == MethodIndex,
                    Tag = i,
                };

                item.Click += (sender, e) =>
                {
                    this.RecordDocumentObjectMember(nameof(MethodIndex), Undo.AfterUndo.None);

                    MethodIndex = (int)((ToolStripMenuItem)sender!).Tag;
                };

                menu.Items.Add(item);
            }
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
        return new SimpleComponentAttribute(this);
    }

    /// <inheritdoc/>
    protected override void BeforeSolveInstance()
    {
        AssemblyPriority.GetDocument = OnPingDocument;
        base.BeforeSolveInstance();
    }

    /// <inheritdoc/>
    protected override void AfterSolveInstance()
    {
        AssemblyPriority.GetDocument = AssemblyPriority.GetDocumentDefault;
        base.AfterSolveInstance();
    }
}
