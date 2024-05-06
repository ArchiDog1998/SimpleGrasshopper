using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.Kernel.Attributes;
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
         GetSubCate(typeof(T))), IGH_VariableParameterComponent
{
    private readonly List<PropertyParam> _setProps = [], _getProps = [];
    private readonly Guid _guid = typeof(T).GetDocObjGuid();

    /// <summary>
    /// The type of this property component.
    /// </summary>
    protected virtual TypePropertyType Type { get; } = typeof(T).GetCustomAttribute<PropertyComponentAttribute>()?.Type ?? TypePropertyType.Property;

    /// <summary>
    /// Set the set value.
    /// </summary>
    protected virtual bool SetProperty => true;

    /// <summary>
    ///
    /// </summary>
    protected static Func<FieldPropInfo, bool> DefaultSetProp = prop => prop.DeclaringType == typeof(T) && !prop.Name.Contains(".");

    /// <summary>
    ///
    /// </summary>
    protected static Func<FieldPropInfo, bool> DefaultGetProp = DefaultSetProp;

    private static IEnumerable<PropertyInfo> AllProperties { get; } = typeof(T).GetRuntimeProperties().Where(prop => prop.GetCustomAttribute<IgnoreAttribute>() == null);
    private static IEnumerable<FieldInfo> AllFields { get; } = typeof(T).GetRuntimeFields().Where(prop => prop.GetCustomAttribute<IgnoreAttribute>() == null && prop.IsPublic);
    private static FieldPropInfo[] AllSetProperties { get; } = [.. AllProperties.Where(p => p.SetMethod != null && !p.SetMethod.IsStatic), .. AllFields];
    private static FieldPropInfo[] AllGetProperties { get; } = [.. AllProperties.Where(p => p.GetMethod != null && !p.GetMethod.IsStatic), .. AllFields];
    [DocData]
    internal List<string> SetPropsName { get; set; } = [.. AllSetProperties.Where(DefaultSetProp).Select(p => p.Name)];

    [DocData]
    internal List<string> GetPropsName { get; set; } = [.. AllGetProperties.Where(DefaultGetProp).Select(p => p.Name)];

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

        if (Type != TypePropertyType.Dtor)
        {
            param.Optional = true;
        }
        return param;
    }

    /// <inheritdoc/>
    protected sealed override void RegisterInputParams(GH_InputParamManager pManager)
    {
        if (Instances.DocumentEditor == null)
        {
            AssemblyPriority.PropertyComponentsGuid[typeof(T)] = ComponentGuid;
            return;
        }

        int start = 0;
        _setProps.Clear();

        if (Type != TypePropertyType.Ctor)
        {
            var keyParam = CreateTypeParam();
            pManager.AddParameter(keyParam, keyParam.Name, keyParam.NickName, keyParam.Description, GH_ParamAccess.item);
            start++;
        }

        if (Type != TypePropertyType.Dtor)
        {
            var setProperties = SetPropsName.Select(n => AllSetProperties.FirstOrDefault(p => p.Name == n)).Where(i => i is not null).ToArray();

            for (int i = 0; i < setProperties.Length; i++)
            {
                var param = new PropertyParam(setProperties[i], i + start);

                _setProps.Add(param);

                param.GetNames(out var name, out var nickName, out var description);

                pManager.AddParameter(param.CreateParam(), name, nickName, description, param.Access);
            }
        }
    }

    /// <inheritdoc/>
    protected sealed override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        if (Instances.DocumentEditor == null) return;

        int start = 0;
        _getProps.Clear();

        if (Type != TypePropertyType.Dtor)
        {
            var keyParam = CreateTypeParam();
            pManager.AddParameter(keyParam, keyParam.Name, keyParam.NickName, keyParam.Description, GH_ParamAccess.item);
            start++;
        }

        if (Type != TypePropertyType.Ctor)
        {
            var getProperties = GetPropsName.Select(n => AllGetProperties.FirstOrDefault(p => p.Name == n)).Where(i => i is not null).ToArray();

            for (int i = 0; i < getProperties.Length; i++)
            {
                var param = new PropertyParam(getProperties[i], i + start);

                _getProps.Add(param);

                param.GetNames(out var name, out var nickName, out var description);

                pManager.AddParameter(param.CreateParam(), name, nickName, description, param.Access);
            }
        }
    }

    /// <inheritdoc/>
    protected sealed override void SolveInstance(IGH_DataAccess DA)
    {
        if (Instances.DocumentEditor == null) return;

        T obj = default!;
        if (Type == TypePropertyType.Ctor || !DA.GetData(0, ref obj))
        {
            if (typeof(T).IsInterface) return;
            obj = (T)typeof(T).CreateInstance();
        }

        if (!BeforePropertyChange(obj)) return;

        object o = obj!;
        if (SetProperty)
        {
            foreach (var prop in _setProps)
            {
                prop.GetValue(DA, ref o, Params.Input[prop.Param.ParamIndex]);
            }
        }

        if (obj != null)
        {
            foreach (var prop in _getProps)
            {
                prop.SetValue(DA, o);
            }
        }

        obj = (T)o;
        AfterPropertyChanged(obj);

        if (Type != TypePropertyType.Dtor)
        {
            DA.SetData(0, o);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value">Can do changes.</param>
    /// <returns></returns>
    protected virtual bool BeforePropertyChange(T? value)
    {
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    protected virtual void AfterPropertyChanged(T? value) { }

    /// <inheritdoc/>
    public override void AppendAdditionalMenuItems(ToolStripDropDown mainMenu)
    {
        base.AppendAdditionalMenuItems(mainMenu);

        if (Instances.DocumentEditor == null) return;

        mainMenu.Items.Add(GetItem("Set Properties", AllSetProperties, SetPropsName, p =>
        {
            var index = this.Params.Input.Count;
            var param = new PropertyParam(p, index);

            _setProps.Add(param);
            SetPropsName.Add(param.PropInfo.Name);

            param.GetNames(out var name, out var nickName, out var description);

            var result = param.CreateParam();
            result.Name = name;
            result.NickName = nickName;
            result.Description = description;
            result.Access = param.Access;
            this.Params.RegisterInputParam(result);
        }, p =>
        {
            var index = SetPropsName.IndexOf(p.Name);
            RemoveSetProps(index);
        }));
        mainMenu.Items.Add(GetItem("Get Properties", AllGetProperties, GetPropsName, p =>
        {
            var index = this.Params.Output.Count;
            var param = new PropertyParam(p, index);

            _getProps.Add(param);
            GetPropsName.Add(param.PropInfo.Name);

            param.GetNames(out var name, out var nickName, out var description);

            var result = param.CreateParam();
            result.Name = name;
            result.NickName = nickName;
            result.Description = description;
            result.Access = param.Access;
            this.Params.RegisterOutputParam(result);
        }, p =>
        {
            var index = GetPropsName.IndexOf(p.Name);
            RemoveGetProps(index);
        }));

        var clear = new ToolStripMenuItem("Remove all unused properties.");
        clear.Click += (s, e) =>
        {
            int i = 0;
            while (i < _setProps.Count)
            {
                var prop = _setProps[i];
                var param = this.Params.Input[prop.Param.ParamIndex];
                if (param.SourceCount == 0)
                {
                    RemoveSetProps(i);
                    continue;
                }
                i++;
            }

            i = 0;
            while (i < _getProps.Count)
            {
                var prop = _getProps[i];
                var param = this.Params.Output[prop.Param.ParamIndex];
                if (param.Recipients.Count == 0)
                {
                    RemoveGetProps(i);
                    continue;
                }
                i++;
            }

            this.Params.OnParametersChanged();
            this.ExpireSolution(true);
        };
        mainMenu.Items.Add(clear);
    }

    private void RemoveSetProps(int index)
    {
        var prop = _setProps[index];
        Params.UnregisterInputParameter(Params.Input[prop.Param.ParamIndex]);
        _setProps.RemoveAt(index);
        SetPropsName.RemoveAt(index);
        for (int i = index; i < _setProps.Count; i++)
        {
            _setProps[i].Param.ParamIndex--;
        }
    }

    private void RemoveGetProps(int index)
    {
        var prop = _getProps[index];
        Params.UnregisterOutputParameter(Params.Output[prop.Param.ParamIndex]);
        _getProps.RemoveAt(index);
        GetPropsName.RemoveAt(index);
        for (int i = index; i < _getProps.Count; i++)
        {
            _getProps[i].Param.ParamIndex--;
        }
    }

    private ToolStripMenuItem GetItem(string name, FieldPropInfo[] properties, List<string> props, Action<FieldPropInfo> add, Action<FieldPropInfo> remove)
    {
        var result = new ToolStripMenuItem(name);
        var count = properties.Length;

        var width = (int)Math.Round(220f * GH_GraphicsUtil.UiScale);

        var textItem = new ToolStripTextBox
        {
            Text = string.Empty,
            BorderStyle = BorderStyle.FixedSingle,
            Width = width,
            AutoSize = false,
            ToolTipText = "Searching...",
        };

        result.DropDown.Items.Add(textItem);

        textItem.TextChanged += (sender, e) =>
        {
            while (result.DropDown.Items.Count > 1)
            {
                result.DropDown.Items.RemoveAt(1);
            }
            for (int i = 0; i < count; i++)
            {
                var prop = properties[i];

                if (!prop.Name.StartsWith(textItem.Text, StringComparison.OrdinalIgnoreCase)) continue;

                var item = new ToolStripMenuItem
                {
                    Text = prop.GetDocObjName(),
                    Checked = props.Contains(prop.Name),
                    Tag = prop,
                };

                item.Click += (sender, e) =>
                {
                    var property = (FieldPropInfo)((ToolStripMenuItem)sender).Tag;
                    if (item.Checked)
                    {
                        remove(property);
                    }
                    else
                    {
                        add(property);
                    }
                    this.Params.OnParametersChanged();
                    this.ExpireSolution(true);
                };

                result.DropDown.Items.Add(item);
            }
        };

        for (int i = 0; i < count; i++)
        {
            var prop = properties[i];

            var item = new ToolStripMenuItem
            {
                Text = prop.GetDocObjName(),
                Checked = props.Contains(prop.Name),
                Tag = prop,
            };

            item.Click += (sender, e) =>
            {
                var property = (FieldPropInfo)((ToolStripMenuItem)sender).Tag;
                if (item.Checked)
                {
                    remove(property);
                }
                else
                {
                    add(property);
                }
                this.Params.OnParametersChanged();
                this.ExpireSolution(true);
            };

            result.DropDown.Items.Add(item);
        }

        result.DropDown.MaximumSize = new(500, 600);

        return result;
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
    public override bool Read(GH_IReader reader)
    {
        reader.Read(this);

        //Destroy
        Params.Clear();
        DestroyIconCache();

        //Build
        _changing = true;
        PostConstructor();
        _changing = false;

        return base.Read(reader);
    }

    /// <inheritdoc/>
    public override bool Write(GH_IWriter writer)
    {
        writer.Write(this);
        return base.Write(writer);
    }

    /// <inheritdoc/>
    public virtual bool CanInsertParameter(GH_ParameterSide side, int index)
    {
        switch (side)
        {
            case GH_ParameterSide.Input:
                var count = Params.Input.Count;
                if (index < count) return false;
                if (SetPropsName.Count == AllSetProperties.Length) return false;
                return true;

            case GH_ParameterSide.Output:
                count = Params.Output.Count;
                if (index < count) return false;
                if (GetPropsName.Count == AllGetProperties.Length) return false;
                return true;

            default:
                return false;
        }
    }

    /// <inheritdoc/>
    public virtual bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
        switch (side)
        {
            case GH_ParameterSide.Input:
                if (index > 0) return true;
                return !Type.HasFlag(TypePropertyType.Dtor);

            case GH_ParameterSide.Output:
                if (index > 0) return true;
                return !Type.HasFlag(TypePropertyType.Ctor);
            default:
                return false;
        }
    }

    /// <inheritdoc/>
    public virtual IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
        switch (side)
        {
            case GH_ParameterSide.Input:
                var param = new PropertyParam(AllSetProperties.FirstOrDefault(p => !SetPropsName.Contains(p.Name)), index);

                _setProps.Add(param);
                SetPropsName.Add(param.PropInfo.Name);

                param.GetNames(out var name, out var nickName, out var description);

                var result = param.CreateParam();
                result.Name = name;
                result.NickName = nickName;
                result.Description = description;
                result.Access = param.Access;
                return result;

            case GH_ParameterSide.Output:
                param = new PropertyParam(AllGetProperties.FirstOrDefault(p => !GetPropsName.Contains(p.Name)), index);

                _getProps.Add(param);
                GetPropsName.Add(param.PropInfo.Name);

                param.GetNames(out name, out nickName, out description);

                result = param.CreateParam();
                result.Name = name;
                result.NickName = nickName;
                result.Description = description;
                result.Access = param.Access;
                return result;
            default:
                throw new ArgumentException("The value side is not valid!");
        }
    }

    /// <inheritdoc/>
    public virtual bool DestroyParameter(GH_ParameterSide side, int index)
    {
        switch (side)
        {
            case GH_ParameterSide.Input:
                if (Type.HasFlag(TypePropertyType.Dtor)) index--;
                _setProps.RemoveAt(index);
                SetPropsName.RemoveAt(index);
                for (int i = index; i < _setProps.Count; i++)
                {
                    _setProps[i].Param.ParamIndex--;
                }
                return true;

            case GH_ParameterSide.Output:
                if (Type.HasFlag(TypePropertyType.Ctor)) index--;
                _getProps.RemoveAt(index);
                GetPropsName.RemoveAt(index);
                for (int i = index; i < _getProps.Count; i++)
                {
                    _getProps[i].Param.ParamIndex--;
                }
                return true;

            default:
                return false;
        }
    }

    /// <inheritdoc/>
    public virtual void VariableParameterMaintenance()
    {
    }
}