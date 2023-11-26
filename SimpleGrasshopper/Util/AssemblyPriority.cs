using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using SimpleGrasshopper.Attributes;
using System;
using System.Reflection;

namespace SimpleGrasshopper.Util;

/// <summary>
/// The assembly priority for adding category icon.
/// </summary>
public abstract class AssemblyPriority : GH_AssemblyPriority
{
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
        DoWithEditor(editor);
    }

    private void ActiveCanvas_DocumentChanged(GH_Canvas sender, GH_CanvasDocumentChangedEventArgs e)
    {
        Instances.ActiveCanvas.DocumentChanged -= ActiveCanvas_DocumentChanged;

        GH_DocumentEditor editor = Instances.DocumentEditor;
        if (editor == null)
        {
            return;
        }
        DoWithEditor(editor);
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

        AddEvent(property, (bool b) =>
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
        if(assembly == null) return null;

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

        major.DropDownItems.AddRange(items);

        return major;
    }

    private ToolStripItem[] GetAllItems(List<PropertyInfo?> propertyInfos)
    {
        var parentList = new List<ToolStripMenuItem>(propertyInfos.Count);
        var flattenList = new List<(ToolStripItem, string)>(propertyInfos.Count);
        foreach (var property in propertyInfos)
        {
            if (property == null) continue;

            var item = CreateItem(property);
            if (item == null) continue;

            var parent = property.GetCustomAttribute<ConfigAttribute>()?.Parent ?? string.Empty;
            flattenList.Add((item, parent));

            if (item is ToolStripMenuItem menuItem)
            {
                parentList.Add(menuItem);
            }
        }

        var result = new List<ToolStripItem>(flattenList.Count);
        foreach (var (item, parent) in flattenList)
        {
            if (!string.IsNullOrEmpty(parent))
            {
                var parentItem = parentList.FirstOrDefault(i => i.Text == parent);
                if (parentItem != null)
                {
                    parentItem.DropDownItems.Add(item);
                    continue;
                }
            }
            result.Add(item);
        }

        return [.. result];
    }

    private ToolStripItem? CreateItem(PropertyInfo propertyInfo)
    {
        var type = propertyInfo.PropertyType.GetRawType();
        ToolStripItem? item;
        if (type == typeof(bool))
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
        //TODO: More types of items!
        else
        {
            item = CreateBaseItem(propertyInfo);
        }

        return item;
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
        return CreateScrollerItem<T>(propertyInfo, min, max, place);
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
        return CreateScrollerItem<T>(propertyInfo, min, max, place);
    }

    private ToolStripItem? CreateScrollerItem<T>(PropertyInfo propertyInfo, decimal min, decimal max, int place)
    {
        if (propertyInfo.GetValue(null) is not T i)
        {
            return null;
        }

        var item = CreateBaseItem(propertyInfo);
        if (item == null) return null;

        var slider = CreateScroller(min, max, place, Convert.ToDecimal(i),
            v => propertyInfo.SetValue(null, Convert.ChangeType(v, typeof(T))));

        AddEvent(propertyInfo, (T b) =>
        {
            slider.Value = Convert.ToDecimal(b);
        });

        GH_DocumentObject.Menu_AppendCustomItem(item.DropDown, slider);

        item.DropDownItems.Add(GetResetItem(propertyInfo,
            () => slider.Value = Convert.ToDecimal(propertyInfo.GetValue(null))));
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

        var item = CreateBaseItem(propertyInfo);
        if (item == null) return null;

        GH_ColourPicker picker = GH_DocumentObject.Menu_AppendColourPicker(item.DropDown, c, (sender, e) =>
        {
            propertyInfo.SetValue(null, e.Colour);
        });

        AddEvent(propertyInfo, (Color b) =>
        {
            picker.Colour = b;
        });

        item.DropDownItems.Add(GetResetItem(propertyInfo,
            () => picker.Colour = (Color)propertyInfo.GetValue(null)!));
        return item;
    }

    private ToolStripItem? CreateStringItem(PropertyInfo propertyInfo)
    {
        if (propertyInfo.GetValue(null) is not string s)
        {
            return null;
        }

        var item = CreateBaseItem(propertyInfo);
        if (item == null) return null;

        var textItem = new ToolStripTextBox
        {
            Text = s,
        };

        textItem.TextChanged += (sender, e) =>
        {
            propertyInfo.SetValue(null, textItem.Text);
        };

        AddEvent(propertyInfo, (string b) =>
        {
            textItem.Text = b;
        });

        item.DropDownItems.Add(textItem);
        item.DropDownItems.Add(GetResetItem(propertyInfo,
            () => textItem.Text = propertyInfo.GetValue(null) as string));
        return item;
    }

    private static ToolStripMenuItem GetResetItem(PropertyInfo propertyInfo, Action? reset = null)
    {
        return new ToolStripMenuItem("Reset Value", ResetIcon, (sender, e) =>
        {
            var type = propertyInfo.DeclaringType;
            if (type == null) return;
            var method = type.GetRuntimeMethod($"Reset{propertyInfo.Name}", []);
            if (method == null) return;
            method.Invoke(null, []);
            reset?.Invoke();
        });
    }

    private ToolStripItem? CreateBoolItem(PropertyInfo propertyInfo)
    {
        var item = CreateBaseItem(propertyInfo);
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

        AddEvent(propertyInfo, (bool b) =>
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

    private ToolStripMenuItem? CreateBaseItem(PropertyInfo propertyInfo)
    {
        var attribute = propertyInfo.GetCustomAttribute<ConfigAttribute>();
        if (attribute == null) return null;

        var major = new ToolStripMenuItem(attribute.Name);

        var iconName = attribute.Icon;
        if (!string.IsNullOrEmpty(iconName))
        {
            var icon = GetType().Assembly.GetBitmap(iconName);
            if (icon != null)
            {
                major.Image = icon;
            }
        }

        var desc = attribute.Description;
        if (!string.IsNullOrEmpty(desc))
        {
            major.ToolTipText = desc;
        }

        //No closing when changing value.
        major.DropDown.Closing += (sender, e) =>
        {
            e.Cancel = e.CloseReason == ToolStripDropDownCloseReason.ItemClicked;
        };

        return major;
    }
    #endregion

    private static void AddEvent(PropertyInfo propertyInfo, Delegate @delegate)
    {
        var propertyChanged = propertyInfo.DeclaringType?.GetRuntimeEvent($"On{propertyInfo.Name}Changed");
        propertyChanged?.AddEventHandler(null, @delegate);
    }
}
