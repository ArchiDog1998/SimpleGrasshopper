using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using SimpleGrasshopper.Attributes;
using System;
using System.Drawing.Imaging;
using GH_DigitScroller = Grasshopper.GUI.GH_DigitScroller;

namespace SimpleGrasshopper.Util;

/// <summary>
/// The assembly priority for adding category icon.
/// </summary>
public abstract class AssemblyPriority : GH_AssemblyPriority
{
    private static MethodInfo? _oldKeyDown = null;
    private static EventInfo? _keydownEvent = null;

    /// <summary>
    /// The working document.
    /// </summary>
    [ThreadStatic]
    public static Func<GH_Document> GetDocument = () => Instances.ActiveCanvas.Document;

    /// <summary>
    /// All your custom shortcut for the grasshopper.
    /// </summary>
    public static Dictionary<Keys, Action> CustomShortcuts { get; } = [];

    /// <summary>
    /// Default way to get the document.
    /// </summary>
    public static readonly Func<GH_Document> GetDocumentDefault = () => Instances.ActiveCanvas.Document;

    private static Bitmap? _bitmap = null;
    private static Bitmap ResetIcon => _bitmap ??= typeof(AssemblyPriority).Assembly.GetBitmap("ResetIcons_24.png")!;

    /// <summary>
    /// The index of the menu to insert your config menu item.
    /// </summary>
    protected virtual int? MenuIndex { get; } = 3;

    /// <summary>
    /// The insert index of the item;
    /// </summary>
    protected virtual int InsertIndex { get; } = 3;

    /// <summary>
    /// the opacity of the default config icon for all configs.
    /// </summary>
    protected virtual float DefaultIconOpacity { get; } = 0.7f;

    /// <summary>
    /// The custom menu item creators.
    /// </summary>
    protected virtual Dictionary<Type, Func<PropertyInfo, ToolStripMenuItem?>> CustomItemsCreators { get; } = [];

    /// <summary>
    /// The display string about reseting the value.
    /// </summary>
    protected virtual string ResetValueString => "Reset Value";

    /// <summary>
    /// The format about <see cref="DateTime"/> showing.
    /// </summary>
    protected virtual string DateTimePickerCustomFormat => "yyyy/MM/dd hh:mm:ss";

    /// <summary>
    /// Modify the doc data in one way.
    /// </summary>
    /// <param name="doc">the document.</param>
    /// <param name="action">the action.</param>
    public static void ModifyDocData(GH_Document doc, Action action)
    {
        ModifyDocData(() => doc, action);
    }

    /// <summary>
    /// Modify the doc data in one way.
    /// </summary>
    /// <typeparam name="T">the return type</typeparam>
    /// <param name="doc">the document.</param>
    /// <param name="function">the function</param>
    public static T? ModifyDocData<T>(GH_Document doc, Func<T> function)
    {
        return ModifyDocData(() => doc, function);
    }

    /// <summary>
    /// Modify the doc data in one way.
    /// </summary>
    /// <param name="getDoc">how to get the doc.</param>
    /// <param name="action">the action.</param>
    public static void ModifyDocData(Func<GH_Document> getDoc, Action action)
    {
        GetDocument = getDoc;
        try
        {
            action();
        }
        finally 
        {
            GetDocument = GetDocumentDefault;
        }
    }

    /// <summary>
    /// Modify the doc data in one way.
    /// </summary>
    /// <typeparam name="T">the return type</typeparam>
    /// <param name="getDoc">how to get the doc.</param>
    /// <param name="function">the function</param>
    /// <returns></returns>
    public static T? ModifyDocData<T>(Func<GH_Document> getDoc, Func<T> function)
    {
        GetDocument = getDoc;
        try
        {
            return function();
        }
        finally
        {
            GetDocument = GetDocumentDefault;
        }
    }

    /// <inheritdoc/>
    public override GH_LoadingInstruction PriorityLoad()
    {
        Instances.CanvasCreated += Instances_CanvasCreated;
        return GH_LoadingInstruction.Proceed;
    }

    private void Instances_CanvasCreated(GH_Canvas canvas)
    {
        Instances.CanvasCreated -= Instances_CanvasCreated;

        GH_DocumentEditor editor = Instances.DocumentEditor;
        if (editor == null)
        {
            Instances.ActiveCanvas.DocumentChanged += ActiveCanvas_DocumentChanged;
            return;
        }
        DoSomethingFirst(editor);
    }

    private void ActiveCanvas_DocumentChanged(GH_Canvas sender, GH_CanvasDocumentChangedEventArgs e)
    {
        Instances.ActiveCanvas.DocumentChanged -= ActiveCanvas_DocumentChanged;

        GH_DocumentEditor editor = Instances.DocumentEditor;
        if (editor == null)
        {
            return;
        }
        DoSomethingFirst(editor);
    }

    private void DoSomethingFirst(GH_DocumentEditor editor)
    {
        _oldKeyDown ??= typeof(GH_DocumentEditor).GetAllRuntimeMethods().First(m => m.Name == "EditorKeyDown");
        _keydownEvent ??= typeof(GH_DocumentEditor).GetEvent("KeyDown")!;

        try
        {
            _keydownEvent.RemoveEventHandler(editor, Delegate.CreateDelegate(typeof(KeyEventHandler), editor, _oldKeyDown));
        }
        catch { }

        editor.KeyDown -= Editor_KeyDown;
        editor.KeyDown += Editor_KeyDown;

        DoWithEditor(editor);
    }

    private static void Editor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (CustomShortcuts.TryGetValue(e.KeyCode, out var act))
        {
            act?.Invoke();
            return;
        }
        _oldKeyDown?.Invoke(sender, [sender, e]);
    }

    /// <summary>
    /// Creeate the things when editor is loading.
    /// </summary>
    /// <param name="editor"></param>
    protected virtual void DoWithEditor(GH_DocumentEditor editor)
    {
        var assembly = GetType().Assembly;
        var icon = assembly.GetAssemblyIcon();
        if (icon != null)
        {
            Instances.ComponentServer.AddCategoryIcon(assembly.GetAssemblyName(), icon);
        }

        CreateMajorMenu(editor);
        CreateToolbar(editor);
    }

    /// <summary>
    /// Create the buttons on the toolbar.
    /// </summary>
    /// <param name="editor"></param>
    protected void CreateToolbar(GH_DocumentEditor editor)
    {
        if (editor.Controls[0].Controls[1] is not ToolStrip canvasToolbar) return;

        var assembly = GetType().Assembly;
        if (assembly == null) return;

        var properties = assembly.GetTypes()
            .SelectMany(t => t.GetRuntimeProperties())
            .Where(p => p.CanWrite && p.CanRead && p.GetMethod!.IsStatic
                && p.PropertyType.GetRawType() == typeof(bool)
                && p.GetCustomAttribute<ToolButtonAttribute>() != null)
            .ToArray();

        if (properties.Length == 0) return;

        var separator = new ToolStripSeparator()
        {
            Margin = new Padding(2, 0, 2, 0),
            Size = new Size(6, 40)
        };
        canvasToolbar.Items.Add(separator);

        foreach (var property in properties)
        {
            var button = CreateToolButton(property);
            if (button == null) continue;
            canvasToolbar.Items.Add(button);
        }
    }

    private ToolStripButton? CreateToolButton(PropertyInfo property)
    {
        var attr = property.GetCustomAttribute<ToolButtonAttribute>();
        if (attr == null) return null;

        var iconName = attr.Icon;
        if (string.IsNullOrEmpty(iconName)) return null;

        var icon = GetType().Assembly.GetBitmap(iconName);
        if (icon == null) return null;

        if (property.GetValue(null) is not bool b)
        {
            return null;
        }

        var button = new ToolStripButton(icon)
        {
            Checked = b,
        };

        var desc = attr.Description;
        if (!string.IsNullOrEmpty(desc))
        {
            button.ToolTipText = desc;
        }

        button.Click += (sender, e) =>
        {
            if (sender is not ToolStripButton i) return;
            property.SetValue(null, !i.Checked);
        };

        AddPropertyChangedEvent(property, (bool b) =>
        {
            button.Checked = b;
        });

        return button;
    }

    #region Major Menu Item
    /// <summary>
    /// Create the menus on the menu
    /// </summary>
    /// <param name="editor"></param>
    protected void CreateMajorMenu(GH_DocumentEditor editor)
    {
        var toolItems = MenuIndex.HasValue
            ? (editor.MainMenuStrip?.Items[MenuIndex.Value] as ToolStripMenuItem)?.DropDownItems
            : editor.MainMenuStrip?.Items;

        if (toolItems == null) return;

        var major = CreateMajorMenuItem();
        if (major == null) return;
        toolItems.Insert(InsertIndex, major);
    }

    /// <summary>
    /// Get the major menu item from this repo.
    /// </summary>
    /// <returns>the major menu item</returns>
    protected ToolStripMenuItem? CreateMajorMenuItem()
    {
        var assembly = GetType().Assembly;
        if (assembly == null) return null;

        var assemblyName = assembly.GetAssemblyName();
        var icon = assembly.GetAssemblyIcon();

        var properties = assembly.GetTypes()
            .SelectMany(t => t.GetRuntimeProperties())
            .Where(p => p.CanWrite && p.CanRead && p.GetMethod!.IsStatic
                && p.GetCustomAttribute<ConfigAttribute>() != null)
            .ToList();

        var majorProperty = properties.FirstOrDefault(p => p.PropertyType.GetRawType() == typeof(bool)
            && p.GetCustomAttribute<ConfigAttribute>()?.Name == assemblyName);

        if (majorProperty != null)
        {
            properties.Remove(majorProperty);
        }

        var items = GetAllItems(properties!);

        if (items.Length == 0) return null;

        var major = new ToolStripMenuItem(assemblyName);
        if (icon != null)
        {
            major.Image = icon;
        }

        var desc = assembly.GetAssemblyDescription();
        if (!string.IsNullOrEmpty(desc))
        {
            major.ToolTipText = desc;
        }

        if (majorProperty != null)
        {
            major = ToBoolItem(major, majorProperty);
        }

        major.DropDown.Closing += (sender, e) =>
        {
            e.Cancel = e.CloseReason is ToolStripDropDownCloseReason.ItemClicked;
        };

        foreach (var grp in items.GroupBy(c => c.Item2).OrderBy(g => g.Key))
        {
            if (major.HasDropDownItems)
            {
                GH_Component.Menu_AppendSeparator(major.DropDown);
            }
            foreach (var item in grp.OrderBy(c => c.Item3))
            {
                major.DropDownItems.Add(item.Item1);
            }
        }

        return major;
    }

    private (ToolStripItem, byte, ushort)[] GetAllItems(List<PropertyInfo?> propertyInfos)
    {
        var parentList = new List<ToolStripMenuItem>(propertyInfos.Count);
        var flattenList = new List<(ToolStripItem, string, byte, ushort)>(propertyInfos.Count);
        foreach (var property in propertyInfos)
        {
            if (property == null) continue;

            var item = CreateItem(property);
            if (item == null) continue;

            var attr = property.GetCustomAttribute<ConfigAttribute>();
            var parent = attr?.Parent ?? string.Empty;
            var section = attr?.Section ?? 0;
            var order = attr?.Order ?? 0;
            flattenList.Add((item, parent, section, order));

            if (item is ToolStripMenuItem menuItem)
            {
                parentList.Add(menuItem);
            }
        }

        var result = new List<(ToolStripItem, byte, ushort)>(flattenList.Count);
        var sectionDict = new Dictionary<ToolStripMenuItem, List<(ToolStripItem, byte, ushort)>>();
        foreach (var (item, parent, section, order) in flattenList)
        {
            if (!string.IsNullOrEmpty(parent))
            {
                var parentItem = parentList.FirstOrDefault(i => i.Text == parent);
                if (parentItem != null)
                {
                    if (!sectionDict.TryGetValue(parentItem, out var children)) children = [];
                    children.Add((item, section, order));
                    sectionDict[parentItem] = children;
                    continue;
                }
            }
            result.Add((item, section, order));
        }

        foreach (var pair in sectionDict)
        {
            var parentItem = pair.Key;
            var childrenList = pair.Value;
            foreach (var grp in childrenList.GroupBy(c => c.Item2).OrderBy(g => g.Key))
            {
                if (parentItem.HasDropDownItems)
                {
                    GH_Component.Menu_AppendSeparator(parentItem.DropDown);
                }
                foreach (var item in grp.OrderBy(c => c.Item3))
                {
                    parentItem.DropDownItems.Add(item.Item1);
                }
            }
        }

        return [.. result];
    }

    private ToolStripItem? CreateItem(PropertyInfo propertyInfo)
    {
        var type = propertyInfo.PropertyType.GetRawType();
        ToolStripItem? item;
        if (CustomItemsCreators.TryGetValue(type, out var creator))
        {
            item = creator?.Invoke(propertyInfo);
        }
        else if (type == typeof(bool))
        {
            item = CreateBoolItem(propertyInfo);
        }
        else if (type == typeof(string))
        {
            item = CreateStringItem(propertyInfo);
        }
        else if (type == typeof(Color))
        {
            item = CreateColorItem(propertyInfo);
        }
        else if (type == typeof(int))
        {
            item = CreateIntegerItem<int>(propertyInfo, int.MinValue, int.MaxValue);
        }
        else if (type == typeof(byte))
        {
            item = CreateIntegerItem<byte>(propertyInfo, byte.MinValue, byte.MaxValue);
        }
        else if (type == typeof(sbyte))
        {
            item = CreateIntegerItem<sbyte>(propertyInfo, sbyte.MinValue, sbyte.MaxValue);
        }
        else if (type == typeof(short))
        {
            item = CreateIntegerItem<short>(propertyInfo, short.MinValue, short.MaxValue);
        }
        else if (type == typeof(ushort))
        {
            item = CreateIntegerItem<ushort>(propertyInfo, ushort.MinValue, ushort.MaxValue);
        }
        else if (type == typeof(uint))
        {
            item = CreateIntegerItem<uint>(propertyInfo, uint.MinValue, uint.MaxValue);
        }
        else if (type == typeof(long))
        {
            item = CreateIntegerItem<long>(propertyInfo, long.MinValue, long.MaxValue);
        }
        else if (type == typeof(ulong))
        {
            item = CreateIntegerItem<ulong>(propertyInfo, ulong.MinValue, ulong.MaxValue);
        }
        else if (type == typeof(double))
        {
            item = CreateNumberItem<double>(propertyInfo);
        }
        else if (type == typeof(float))
        {
            item = CreateNumberItem<float>(propertyInfo);
        }
        else if (type == typeof(decimal))
        {
            item = CreateNumberItem<decimal>(propertyInfo);
        }
        else if (type == typeof(DateTime))
        {
            item = CreateDateTimeItem(propertyInfo);
        }
        else if (type.IsEnum)
        {
            item = CreateEnumItem(propertyInfo);
        }
        else
        {
            var isObject = type == typeof(object);
            item = CreateBaseItem(propertyInfo, isObject ? null
                : new Param_GenericObject().Icon_24x24);
            if (isObject && item != null)
            {
                item.Click += (s, e) =>
                {
                    propertyInfo.SetValue(null, null);
                };
            }
        }

        return item;
    }

    private ToolStripItem? CreateDateTimeItem(PropertyInfo propertyInfo)
    {
        var item = CreateBaseItem(propertyInfo, new Param_Time().Icon_24x24);
        if (item == null) return null;

        if (propertyInfo.GetValue(null) is not DateTime time)
        {
            return null;
        }

        var ctrl = new DateTimePicker()
        {
            Value = time,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = DateTimePickerCustomFormat,
        };

        ctrl.ValueChanged += (sender, e) =>
        {
            if (sender is not DateTimePicker picker) return;
            propertyInfo.SetValue(null, picker.Value);
        };

        AddPropertyChangedEvent(propertyInfo, (DateTime b) =>
        {
            ctrl.Value = b;
        });

        GH_DocumentObject.Menu_AppendCustomItem(item.DropDown, ctrl);
        AddResetItem(item.DropDownItems, propertyInfo);
        return item;
    }

    private ToolStripItem? CreateEnumItem(PropertyInfo propertyInfo)
    {
        var item = CreateBaseItem(propertyInfo, new GH_ValueList().Icon_24x24);
        if (item == null) return null;

        var i = propertyInfo.GetValue(null);
        if (i == null) return null;

        var type = propertyInfo.PropertyType;
        var e = Enum.ToObject(type, i);

        var box = new ToolStripComboBox()
        {
            FlatStyle = FlatStyle.System,
        };
        ;
        var array = Enum.GetValues(type);
        var objs = new List<object>(array.Length);
        foreach (var enumItem in array)
        {
            objs.Add(new EnumRelay()
            {
                Value = enumItem,
                Name = ((Enum)enumItem).GetDescription(),
            });
        }
        box.Items.AddRange([.. objs]);
        box.SelectedIndex = Array.IndexOf(array, e);

        box.SelectedIndexChanged += (sender, e) =>
        {
            if (sender is not ToolStripComboBox b) return;
            propertyInfo.SetValue(null, ((EnumRelay)b.SelectedItem).Value);
        };

        var method = typeof(AssemblyPriority).GetAllRuntimeMethods()
            .First(m => m.Name == nameof(GetSettingDelegate))
            .MakeGenericMethod(type);

        var dele = method.Invoke(null, [array, box]);

        method = typeof(AssemblyPriority).GetAllRuntimeMethods()
            .First(m => m.Name == nameof(AddPropertyChangedEvent))
            .MakeGenericMethod(type);

        method.Invoke(null, [propertyInfo, dele]);

        item.DropDownItems.Add(box);
        AddResetItem(item.DropDownItems, propertyInfo);
        return item;
    }

    private static Action<T> GetSettingDelegate<T>(Array array, ToolStripComboBox box)
    {
        return (b) => box.SelectedIndex = Array.IndexOf(array, b);
    }

    private struct EnumRelay
    {
        public string Name;
        public object Value;
        public readonly override string ToString() => Name;
    }

    private ToolStripItem? CreateIntegerItem<T>(PropertyInfo propertyInfo, decimal min, decimal max)
    {
        int place = 0;

        var range = propertyInfo.GetCustomAttribute<RangeAttribute>();
        if (range != null)
        {
            min = Math.Max(min, range.Min);
            max = Math.Min(max, range.Max);
            place = Math.Min(place, range.Place);
        }
        var item = CreateScrollerItem<T>(propertyInfo, min, max, place, new Param_Integer().Icon_24x24);
        return item;
    }

    private ToolStripItem? CreateNumberItem<T>(PropertyInfo propertyInfo)
    {
        decimal min = decimal.MinValue;
        decimal max = decimal.MaxValue;
        int place = 1;

        var range = propertyInfo.GetCustomAttribute<RangeAttribute>();
        if (range != null)
        {
            min = Math.Max(min, range.Min);
            max = Math.Min(max, range.Max);
            place = Math.Max(place, range.Place);
        }
        var item = CreateScrollerItem<T>(propertyInfo, min, max, place, new Param_Number().Icon_24x24);
        return item;
    }

    private ToolStripItem? CreateScrollerItem<T>(PropertyInfo propertyInfo, decimal min, decimal max, int place, Image? defaultImage)
    {
        if (propertyInfo.GetValue(null) is not T i)
        {
            return null;
        }

        var item = CreateBaseItem(propertyInfo, defaultImage);
        if (item == null) return null;

        var slider = CreateScroller(min, max, place, Convert.ToDecimal(i),
            v => propertyInfo.SetValue(null, Convert.ChangeType(v, typeof(T))));

        AddPropertyChangedEvent(propertyInfo, (T b) =>
        {
            slider.Value = Convert.ToDecimal(b);
        });

        GH_DocumentObject.Menu_AppendCustomItem(item.DropDown, slider);

        AddResetItem(item.DropDownItems, propertyInfo);
        return item;
    }

    private static GH_DigitScroller CreateScroller(decimal min, decimal max, int decimalPlace, decimal originValue, Action<decimal> setValue)
    {
        GH_DigitScroller slider = new()
        {
            MinimumValue = min,
            MaximumValue = max,
            DecimalPlaces = decimalPlace,
            Value = originValue,
            Size = new Size(150, 24),
        };

        slider.ValueChanged += (sender, e) =>
        {
            var result = e.Value;
            result = result >= min ? result : min;
            result = result <= max ? result : max;
            slider.Value = result;
            setValue(result);
        };

        return slider;
    }

    private ToolStripItem? CreateColorItem(PropertyInfo propertyInfo)
    {
        if (propertyInfo.GetValue(null) is not Color c)
        {
            return null;
        }

        var item = CreateBaseItem(propertyInfo, new Param_Colour().Icon_24x24);
        if (item == null) return null;

        GH_ColourPicker picker = GH_DocumentObject.Menu_AppendColourPicker(item.DropDown, c, (sender, e) =>
        {
            propertyInfo.SetValue(null, e.Colour);
        });

        AddPropertyChangedEvent(propertyInfo, (Color b) =>
        {
            picker.Colour = b;
        });

        AddResetItem(item.DropDownItems, propertyInfo);
        return item;
    }

    private ToolStripItem? CreateStringItem(PropertyInfo propertyInfo)
    {
        if (propertyInfo.GetValue(null) is not string s)
        {
            return null;
        }

        var item = CreateBaseItem(propertyInfo, new Param_String().Icon_24x24);
        if (item == null) return null;

        var textItem = new ToolStripTextBox
        {
            Text = s,
            BorderStyle = BorderStyle.FixedSingle,
        };

        textItem.TextChanged += (sender, e) =>
        {
            propertyInfo.SetValue(null, textItem.Text);
        };

        AddPropertyChangedEvent(propertyInfo, (string b) =>
        {
            textItem.Text = b;
        });

        item.DropDownItems.Add(textItem);
        AddResetItem(item.DropDownItems, propertyInfo);
        return item;
    }

    private ToolStripItem? CreateBoolItem(PropertyInfo propertyInfo)
    {
        var item = CreateBaseItem(propertyInfo, new Param_Boolean().Icon_24x24);
        if (item == null) return null;

        item = ToBoolItem(item, propertyInfo);
        return item;
    }

    private static ToolStripMenuItem ToBoolItem(ToolStripMenuItem item, PropertyInfo propertyInfo)
    {
        if (propertyInfo.GetValue(null) is not bool b)
        {
            return item;
        }

        item.Checked = b;
        item.Click += (sender, e) =>
        {
            if (sender is not ToolStripMenuItem i) return;
            propertyInfo.SetValue(null, !i.Checked);

            if (i.HasDropDownItems)
            {
                foreach (ToolStripItem it in i.DropDownItems)
                {
                    it.Enabled = i.Checked;
                }
            }
        };

        AddPropertyChangedEvent(propertyInfo, (bool b) =>
        {
            item.Checked = b;
        });

        item.DropDownOpening += (sender, e) =>
        {
            if (sender is not ToolStripMenuItem i) return;

            if (i.HasDropDownItems)
            {
                foreach (ToolStripItem it in i.DropDownItems)
                {
                    it.Enabled = i.Checked;
                }
            }
        };

        return item;
    }

    /// <summary>
    /// Add the reset value item to the items collection.
    /// </summary>
    /// <param name="items">collection</param>
    /// <param name="propertyInfo">the property to reset.</param>
    protected void AddResetItem(ToolStripItemCollection items, PropertyInfo propertyInfo)
    {
        var type = propertyInfo.DeclaringType;
        if (type == null) return;
        var method = type.GetRuntimeMethod($"Reset{propertyInfo.Name}", []);
        if (method == null) return;

        items.Add(new ToolStripMenuItem(ResetValueString, ResetIcon, (sender, e) =>
        {
            method.Invoke(null, []);
        }));
    }

    /// <summary>
    /// Create the base item from a <see cref="PropertyInfo"/>
    /// </summary>
    /// <param name="propertyInfo">the property</param>
    /// <param name="defaultImage">the default config image.</param>
    /// <returns>the item.</returns>
    protected ToolStripMenuItem? CreateBaseItem(PropertyInfo propertyInfo, Image? defaultImage)
    {
        var attribute = propertyInfo.GetCustomAttribute<ConfigAttribute>();
        if (attribute == null) return null;
        
        var major = new ToolStripMenuItem(attribute.Name);

        if (propertyInfo.GetCustomAttribute<ShortcutAttribute>() is ShortcutAttribute shortcut)
        {
            major.ShortcutKeyDisplayString = shortcut.DisplayString ?? shortcut.ShortcutKey.ToString();
            major.ShortcutKeys = shortcut.ShortcutKey;
            major.ShowShortcutKeys = shortcut.ShowShortcut;
        }

        var iconName = attribute.Icon;
        if (!string.IsNullOrEmpty(iconName))
        {
            var icon = GetType().Assembly.GetBitmap(iconName);
            if (icon != null)
            {
                major.Image = icon;
            }
        }
        if (major.Image == null && DefaultIconOpacity > 0 && defaultImage != null)
        {
            major.Image = SetImageOpacity(defaultImage, DefaultIconOpacity);
        }

        var desc = attribute.Description;
        if (!string.IsNullOrEmpty(desc))
        {
            major.ToolTipText = desc;
        }

        //No closing when changing value.
        major.DropDown.Closing += (sender, e) =>
        {
            e.Cancel = e.CloseReason is ToolStripDropDownCloseReason.ItemClicked;
        };

        return major;

        static Image SetImageOpacity(Image image, float opacity)
        {
            try
            {
                var bmp = new Bitmap(image.Width, image.Height);
                using var gfx = Graphics.FromImage(bmp);

                var attributes = new ImageAttributes();
                attributes.SetColorMatrix(new ColorMatrix()
                {
                    Matrix33 = opacity,
                }, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                gfx.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);

                return bmp;
            }
            catch
            {
                return image;
            }
        }
    }

    /// <summary>
    /// Add the event about this property changed.
    /// </summary>
    /// <param name="propertyInfo">the property</param>
    /// <param name="action">the action you want to do,</param>
    protected static void AddPropertyChangedEvent<T>(PropertyInfo propertyInfo, Action<T> action)
    {
        var propertyChanged = propertyInfo.DeclaringType?.GetRuntimeEvent($"On{propertyInfo.Name}Changed");
        propertyChanged?.AddEventHandler(null, action);
    }
    #endregion
}
