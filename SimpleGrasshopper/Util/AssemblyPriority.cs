using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using SimpleGrasshopper.Attributes;

namespace SimpleGrasshopper.Util;

/// <summary>
/// The assembly priority for adding category icon.
/// </summary>
public abstract class AssemblyPriority : GH_AssemblyPriority
{
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

        var toolItems = MenuIndex.HasValue
            ? (editor.MainMenuStrip?.Items[MenuIndex.Value] as ToolStripMenuItem)?.DropDownItems
            : editor.MainMenuStrip?.Items;

        if (toolItems == null) return;

        var major = CreateMajorMenuItem();
        if (major == null) return;
        toolItems.Insert(InsertIndex, major);
    }

    #region Major Menu Item
    /// <summary>
    /// Get the major menu item from this repo.
    /// </summary>
    /// <returns>the major menu item</returns>
    protected ToolStripMenuItem? CreateMajorMenuItem()
    {
        var assembly = GetType().Assembly;
        var assemblyName = assembly.GetAssemblyName();
        var icon = assembly.GetAssemblyIcon();

        var items = GetAllItems(assembly.GetTypes()
            .SelectMany(t => t.GetRuntimeProperties())
            .Where(p => p.CanWrite && p.CanRead && p.GetMethod!.IsStatic 
                && p.GetCustomAttribute<ConfigAttribute>() != null)
            .ToArray());

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
        major.DropDownItems.AddRange(items);

        return major;
    }

    private ToolStripItem[] GetAllItems(PropertyInfo?[] propertyInfos)
    {
        var parentList = new List<ToolStripMenuItem>(propertyInfos.Length);
        var flattenList = new List<(ToolStripItem, string)>(propertyInfos.Length);
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
        //else if (type == typeof(string))
        //{
        //    item = CreateStringItem(propertyInfo);
        //}
        //TODO: More types of items!
        else
        {
            item = CreateBaseItem(propertyInfo);
        }

        return item;
    }

    //private ToolStripItem? CreateStringItem(PropertyInfo propertyInfo)
    //{
    //}

    private ToolStripItem? CreateBoolItem(PropertyInfo propertyInfo)
    {
        if (propertyInfo.GetValue(null) is not bool b)
        {
            return null;
        }

        var item = CreateBaseItem(propertyInfo);
        if (item == null) return null;

        item.Checked = b;
        item.Click += (sender, e) =>
        {
            if (sender is not ToolStripMenuItem i) return;
            item.Checked = !item.Checked;
            propertyInfo.SetValue(null, item.Checked);

            if (i.HasDropDownItems)
            {
                foreach (ToolStripItem it in i.DropDownItems)
                {
                    it.Enabled = item.Checked;
                }
            }
        };

        item.DropDownOpening += (sender, e) =>
        {
            if (sender is not ToolStripMenuItem i) return;

            if (i.HasDropDownItems)
            {
                foreach (ToolStripItem it in i.DropDownItems)
                {
                    it.Enabled = item.Checked;
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
        if(!string.IsNullOrEmpty(iconName))
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
}
