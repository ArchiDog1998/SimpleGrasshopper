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
         GetSubCate(typeof(T)))
{
    private readonly List<PropertyParam> _setProps = [], _getProps = [];
    private readonly Guid _guid = typeof(T).GetDocObjGuid();
    private readonly TypePropertyType _type = typeof(T).GetCustomAttribute<PropertyComponentAttribute>()?.Type ?? TypePropertyType.Property;

    private string[] _setPropsName = AllSetProperties.Select(p => p.Name).ToArray(), _getPropsName = AllGetProperties.Select(p => p.Name).ToArray();

    [DocData]
    internal string[] SetPropsName 
    {
        get => _setPropsName;
        set 
        {
            _setPropsName = value;
            ClearInfo();
        }
    }

    [DocData]
    internal string[] GetPropsName
    {
        get => _getPropsName;
        set
        {
            _getPropsName = value;
            ClearInfo();
        }
    }

    private void ClearInfo()
    {
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
    private static IEnumerable<PropertyInfo> AllSetProperties => AllProperties.Where(p => p.SetMethod != null && !p.SetMethod.IsStatic);
    private static IEnumerable<PropertyInfo> AllGetProperties => AllProperties.Where(p => p.GetMethod != null && !p.GetMethod.IsStatic);

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
            var setProperties = AllSetProperties.Where(p => SetPropsName.Contains(p.Name)).ToArray();

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
            var getProperties = AllGetProperties.Where(p => GetPropsName.Contains(p.Name)).ToArray();

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
    public override void AppendAdditionalMenuItems(ToolStripDropDown mainMenu)
    {
        base.AppendAdditionalMenuItems(mainMenu);

        mainMenu.Items.Add(GetItem("Set Properties", nameof(SetPropsName), AllSetProperties.ToArray(), SetPropsName, i => SetPropsName = i));
        mainMenu.Items.Add(GetItem("Get Properties", nameof(GetPropsName), AllGetProperties.ToArray(), GetPropsName, i => GetPropsName = i));
    }

    private ToolStripMenuItem GetItem(string name, string propertyName, PropertyInfo[] properties, string[] props, Action<string[]> changed)
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
                    Text = prop.Name,
                    Checked = props.Contains(prop.Name),
                };

                item.Click += (sender, e) =>
                {
                    this.RecordDocumentObjectMember(propertyName, Undo.AfterUndo.None);

                    if (item.Checked)
                    {
                        changed([.. props.Where(i => i != item.Text)]);
                    }
                    else
                    {
                        changed([.. props, item.Text]);
                    }
                };

                result.DropDown.Items.Add(item);
            }
        };

        for (int i = 0; i < count; i++)
        {
            var prop = properties[i];

            var item = new ToolStripMenuItem
            {
                Text = prop.Name,
                Checked = props.Contains(prop.Name),
            };

            item.Click += (sender, e) =>
            {
                this.RecordDocumentObjectMember(propertyName, Undo.AfterUndo.None);

                if (item.Checked)
                {
                    changed([.. props.Where(i => i != item.Text)]);
                }
                else
                {
                    changed([.. props, item.Text]);
                }
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
        return base.Read(reader);
    }

    /// <inheritdoc/>
    public override bool Write(GH_IWriter writer)
    {
        writer.Write(this);
        return base.Write(writer);
    }
}
