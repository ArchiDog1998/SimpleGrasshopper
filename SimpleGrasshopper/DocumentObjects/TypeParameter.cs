using GH_IO.Serialization;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Data;
using SimpleGrasshopper.Util;
using System;

namespace SimpleGrasshopper.DocumentObjects;

/// <summary>
/// A simple <see cref="GH_PersistentParam{T}"/> for one object.
/// </summary>
/// <typeparam name="T">the object that it contains.</typeparam>
public abstract class TypeParameter<T>()
    : GH_PersistentParam<SimpleGoo<T>>(typeof(T).GetDocObjName(),
                   typeof(T).GetDocObjNickName(),
                   typeof(T).GetDocObjDescription(),
                   typeof(T).GetAssemblyName(),
                   "Parameters")
{
    /// <inheritdoc/>
    public override GH_Exposure Exposure => typeof(T).GetCustomAttribute<ExposureAttribute>()?.Exposure ?? base.Exposure;

    private Bitmap? _icon;
    /// <inheritdoc/>
    protected override Bitmap Icon
    {
        get
        {
            if (_icon != null) return _icon;
            var path = typeof(T).GetCustomAttribute<IconAttribute>()?.IconPath;
            if (path == null) return base.Icon;

            return _icon = GetType().Assembly.GetBitmap(path) ?? base.Icon;
        }
    }

    /// <inheritdoc/>
    protected override GH_GetterResult Prompt_Plural(ref List<SimpleGoo<T>> values)
    {
        try
        {
            values ??= [];
            values.Add(new SimpleGoo<T>((T)typeof(T).CreateInstance()));
            return GH_GetterResult.success;
        }
        catch
        {
            return GH_GetterResult.cancel;
        }
    }

    /// <inheritdoc/>
    protected override GH_GetterResult Prompt_Singular(ref SimpleGoo<T> value)
    {
        try
        {
            value = new SimpleGoo<T>((T)typeof(T).CreateInstance());
            return GH_GetterResult.success;
        }
        catch
        {
            return GH_GetterResult.cancel;
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

    /// <inheritdoc/>
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
        if (!AssemblyPriority.PropertyComponentsGuid.TryGetValue(typeof(T), out var guid))
        {
            base.AppendAdditionalMenuItems(menu);
            return;
        }

        switch (Kind)
        {
            case GH_ParamKind.floating:
                menu.Items.Add(GetConstructor(guid));
                menu.Items.Add(GetDeconstructor(guid));
                break;

            case GH_ParamKind.input:
                menu.Items.Add(GetConstructor(guid));
                break;

            case GH_ParamKind.output:
                menu.Items.Add(GetDeconstructor(guid));
                break;
        }

        Menu_AppendSeparator(menu);
        base.AppendAdditionalMenuItems(menu);
        return;
    }

    private ToolStripMenuItem GetDeconstructor(Guid guid)
    {
        var item = new ToolStripMenuItem("Deconstructor");
        item.Click += (s, e) =>
        {
            var point = this.Attributes.Pivot;
            point.X += 200;

            Instances.ActiveCanvas.Document_ObjectsAdded += ModifyInput;
            Instances.ActiveCanvas.InstantiateNewObject(guid, point, false);
            Instances.ActiveCanvas.Document_ObjectsAdded -= ModifyInput;

            void ModifyInput(GH_Document sender, GH_DocObjectEventArgs e)
            {
                foreach (var item in e.Objects)
                {
                    if (item is not IGH_Component comp) continue;
                    comp.Params.Input[0].AddSource(this);
                    this.ExpireSolution(true);
                }
            }
        };
        return item;
    }

    private ToolStripMenuItem GetConstructor(Guid guid)
    {
        var item = new ToolStripMenuItem("Constructor");
        item.Click += (s, e) =>
        {
            var point = this.Attributes.Pivot;
            point.X -= 200;

            Instances.ActiveCanvas.Document_ObjectsAdded += ModifyInput;
            Instances.ActiveCanvas.InstantiateNewObject(guid, point, false);
            Instances.ActiveCanvas.Document_ObjectsAdded -= ModifyInput;

            void ModifyInput(GH_Document sender, GH_DocObjectEventArgs e)
            {
                foreach (var item in e.Objects)
                {
                    if (item is not IGH_Component comp) continue;
                    this.AddSource(comp.Params.Output[0]);
                    comp.ExpireSolution(true);
                }
            }
        };
        return item;
    }
}